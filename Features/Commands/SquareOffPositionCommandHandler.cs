using MediatR;
using KiteConnectApi.Data;
using KiteConnectApi.Models.Dto;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using KiteConnectApi.Services;

namespace KiteConnectApi.Features.Commands
{
    public class SquareOffPositionCommandHandler : IRequestHandler<SquareOffPositionCommand, bool>
    {
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly ILogger<SquareOffPositionCommandHandler> _logger;

        public SquareOffPositionCommandHandler(
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            ILogger<SquareOffPositionCommandHandler> logger)
        {
            _context = context;
            _kiteConnectService = kiteConnectService;
            _logger = logger;
        }

        public async Task<bool> Handle(SquareOffPositionCommand request, CancellationToken cancellationToken)
        {
            var alert = request.Alert;
            var config = request.Config;

            _logger.LogInformation($"Handling stop loss alert for strategy: {config.StrategyName}");

            var position = await _context.TradePositions
                .FirstOrDefaultAsync(p => p.StrategyConfigId == config.Id && p.Status == "OPEN");

            if (position == null)
            {
                _logger.LogWarning($"No open position found for strategy {config.StrategyName} to apply stop loss.");
                return false;
            }

            try
            {
                // Close the main position
                var mainOrderResponse = await _kiteConnectService.PlaceOrderAsync(
                    exchange: config.Exchange,
                    tradingsymbol: position.TradingSymbol,
                    transaction_type: "BUY", // Opposite of entry
                    quantity: position.Quantity,
                    product: position.Product,
                    order_type: "MARKET"
                );

                // Close the hedge position
                var hedgeOrderResponse = await _kiteConnectService.PlaceOrderAsync(
                    exchange: config.Exchange,
                    tradingsymbol: position.HedgeTradingSymbol,
                    transaction_type: "SELL", // Opposite of entry
                    quantity: position.Quantity,
                    product: position.Product,
                    order_type: "MARKET"
                );

                position.Status = "CLOSED";
                position.ExitTime = DateTime.UtcNow;
                _context.TradePositions.Update(position);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully squared off position for strategy {config.StrategyName}.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while squaring off position.");
                return false;
            }
        }
    }
}