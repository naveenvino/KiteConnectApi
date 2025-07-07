// --- Services/StrategyService.cs ---
// This file had an incorrect null check for a struct.
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class StrategyService
    {
        private readonly IKiteConnectService _kiteConnectService;
        private readonly IPositionRepository _positionRepository;
        private readonly ILogger<StrategyService> _logger;

        public StrategyService(IKiteConnectService kiteConnectService, IPositionRepository positionRepository, ILogger<StrategyService> logger)
        {
            _kiteConnectService = kiteConnectService;
            _positionRepository = positionRepository;
            _logger = logger;
        }

        public async Task ExecuteStrategy()
        {
            _logger.LogInformation("Executing strategy...");
            await Task.CompletedTask;
        }

        public async Task HandleTradingViewAlert(TradingViewAlert alert)
        {
            _logger.LogInformation("Handling TradingView Alert for {Symbol}", alert.Symbol);
            if (alert.Symbol != null)
            {
                await _kiteConnectService.PlaceOrderAsync(
                    exchange: "NFO",
                    tradingsymbol: alert.Symbol,
                    transaction_type: alert.Action?.ToUpper() ?? "",
                    quantity: 1,
                    product: "MIS",
                    order_type: "MARKET"
                );
            }
        }

        public async Task MonitorAndExecuteExits()
        {
            _logger.LogInformation("Monitoring and executing exits...");
            var openPositions = await _positionRepository.GetOpenPositionsAsync();
            foreach (var position in openPositions)
            {
                // TODO: Implement exit logic (e.g., stop loss, take profit).
            }
            await Task.CompletedTask;
        }

        public DateTime GetNextWeeklyExpiry()
        {
            _logger.LogInformation("Getting next weekly expiry...");
            DateTime today = DateTime.Today;
            int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)today.DayOfWeek + 7) % 7;
            return today.AddDays(daysUntilThursday);
        }

        public async Task ClosePositionAsync(string tradingSymbol)
        {
            _logger.LogInformation("Closing position for {Symbol}", tradingSymbol);
            var positions = await _kiteConnectService.GetPositionsAsync();
            
            // FIXED: KiteConnect.Position is a struct. Check a property instead of the object itself.
            // The property name is 'Tradingsymbol' (all lowercase except T).
            var positionToClose = positions.FirstOrDefault(p => p.TradingSymbol == tradingSymbol && p.Quantity != 0);

            if (positionToClose.TradingSymbol != null)
            {
                string transactionType = positionToClose.Quantity > 0 ? "SELL" : "BUY";
                await _kiteConnectService.PlaceOrderAsync(
                    exchange: positionToClose.Exchange,
                    tradingsymbol: positionToClose.TradingSymbol,
                    transaction_type: transactionType,
                    quantity: Math.Abs(positionToClose.Quantity),
                    product: positionToClose.Product,
                    order_type: "MARKET"
                );
            }
        }
    }
}
