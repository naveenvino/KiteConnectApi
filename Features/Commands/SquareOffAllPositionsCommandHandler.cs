using MediatR;
using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KiteConnectApi.Services;

namespace KiteConnectApi.Features.Commands
{
    public class SquareOffAllPositionsCommandHandler : IRequestHandler<SquareOffAllPositionsCommand, bool>
    {
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly ILogger<SquareOffAllPositionsCommandHandler> _logger;

        public SquareOffAllPositionsCommandHandler(
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            ILogger<SquareOffAllPositionsCommandHandler> logger)
        {
            _context = context;
            _kiteConnectService = kiteConnectService;
            _logger = logger;
        }

        public async Task<bool> Handle(SquareOffAllPositionsCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Squaring off all positions for strategy: {request.StrategyId}");

            var positions = await _context.TradePositions
                .Where(p => p.StrategyConfigId == request.StrategyId && p.Status == "OPEN")
                .ToListAsync();

            foreach (var position in positions)
            {
                try
                {
                    // Close the main position
                    await _kiteConnectService.PlaceOrderAsync(
                        exchange: "NFO", // Assuming NFO, should be dynamic
                        tradingsymbol: position.TradingSymbol,
                        transaction_type: "BUY", // Opposite of entry
                        quantity: position.Quantity,
                        product: position.Product,
                        order_type: "MARKET"
                    );

                    // Close the hedge position
                    await _kiteConnectService.PlaceOrderAsync(
                        exchange: "NFO",
                        tradingsymbol: position.HedgeTradingSymbol,
                        transaction_type: "SELL", // Opposite of entry
                        quantity: position.Quantity,
                        product: position.Product,
                        order_type: "MARKET"
                    );

                    position.Status = "CLOSED";
                    position.ExitTime = DateTime.UtcNow;
                    _context.TradePositions.Update(position);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"An error occurred while squaring off position {position.PositionId}.");
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Finished squaring off all positions for strategy: {request.StrategyId}");
            return true;
        }
    }
}