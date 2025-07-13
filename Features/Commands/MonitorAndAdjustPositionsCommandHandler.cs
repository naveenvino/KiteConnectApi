using MediatR;
using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KiteConnectApi.Services;

namespace KiteConnectApi.Features.Commands
{
    public class MonitorAndAdjustPositionsCommandHandler : IRequestHandler<MonitorAndAdjustPositionsCommand, bool>
    {
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly ILogger<MonitorAndAdjustPositionsCommandHandler> _logger;
        private readonly IMediator _mediator;

        public MonitorAndAdjustPositionsCommandHandler(
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            ILogger<MonitorAndAdjustPositionsCommandHandler> logger,
            IMediator mediator)
        {
            _context = context;
            _kiteConnectService = kiteConnectService;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task<bool> Handle(MonitorAndAdjustPositionsCommand request, CancellationToken cancellationToken)
        {
            var config = request.Config;

            _logger.LogInformation($"Monitoring and adjusting positions for strategy: {config.StrategyName}");

            var position = await _context.TradePositions
                .FirstOrDefaultAsync(p => p.StrategyConfigId == config.Id && p.Status == "OPEN");

            if (position == null)
            {
                return false; // No open position to monitor
            }

            var ltpResponse = await _kiteConnectService.GetQuotesAsync(new[] { $"NFO:{position.TradingSymbol}" });
            if (!ltpResponse.TryGetValue($"NFO:{position.TradingSymbol}", out KiteConnect.Quote quote))
            {
                _logger.LogWarning($"Could not get LTP for {position.TradingSymbol} to monitor position.");
                return false;
            }

            var currentPnl = (position.AveragePrice - quote.LastPrice) * position.Quantity; // For sell position

            // Get the current stop-loss order, if any
            var currentStopLossOrder = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == position.StopLossOrderId);

            // 1. Lock Profit
            if (config.LockProfitAmount > 0 && currentPnl >= config.LockProfitAmount)
            {
                // If profit is locked, ensure SL is at least at the locked profit level
                if (currentStopLossOrder != null)
                {
                    decimal newStopLossPrice = position.AveragePrice - (config.LockProfitAmount / position.Quantity);
                    if (currentStopLossOrder.TriggerPrice == null || newStopLossPrice > currentStopLossOrder.TriggerPrice)
                    {
                        await _kiteConnectService.ModifyOrderAsync(
                            order_id: currentStopLossOrder.OrderId!,
                            trigger_price: newStopLossPrice
                        );
                        _logger.LogInformation($"Profit locked for position {position.PositionId}. SL moved to {newStopLossPrice}.");
                    }
                }
            }

            // 2. Trail Stop Loss
            if (config.TrailStopLossAmount > 0 && currentPnl > 0)
            {
                if (currentStopLossOrder != null)
                {
                    decimal newStopLossPrice = position.AveragePrice - (currentPnl + config.TrailStopLossAmount) / position.Quantity;
                    if (currentStopLossOrder.TriggerPrice == null || newStopLossPrice > currentStopLossOrder.TriggerPrice)
                    {
                        await _kiteConnectService.ModifyOrderAsync(
                            order_id: currentStopLossOrder.OrderId!,
                            trigger_price: newStopLossPrice
                        );
                        _logger.LogInformation($"Trailing stop loss for position {position.PositionId}. New SL: {newStopLossPrice}.");
                    }
                }
            }

            // 3. Move SL to Entry
            if (config.MoveStopLossToEntryPriceAmount > 0 && currentPnl >= config.MoveStopLossToEntryPriceAmount)
            {
                if (currentStopLossOrder != null)
                {
                    decimal entryPrice = position.AveragePrice;
                    if (currentStopLossOrder.TriggerPrice == null || entryPrice > currentStopLossOrder.TriggerPrice)
                    {
                        await _kiteConnectService.ModifyOrderAsync(
                            order_id: currentStopLossOrder.OrderId!,
                            trigger_price: entryPrice
                        );
                        _logger.LogInformation($"Moving stop loss to entry for position {position.PositionId}.");
                    }
                }
            }

            // 4. Exit and Re-enter
            if (config.ExitAndReenterProfitAmount > 0 && currentPnl >= config.ExitAndReenterProfitAmount)
            {
                if (config.LastExitReentryTime == null || (DateTime.UtcNow - config.LastExitReentryTime.Value).TotalMinutes > 60) // 1 hour cooldown
                {
                    await _mediator.Send(new SquareOffAllPositionsCommand(config.Id));
                    // Optionally, re-enter the position immediately or based on a new signal
                    _logger.LogInformation($"Exited and re-entered position for strategy {config.StrategyName}.");
                    config.LastExitReentryTime = DateTime.UtcNow;
                    _context.NiftyOptionStrategyConfigs.Update(config);
                    await _context.SaveChangesAsync();
                }
            }
            return true;
        }
    }
}