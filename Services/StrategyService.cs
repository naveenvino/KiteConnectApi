using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        private readonly NiftyOptionStrategyConfig _strategyConfig;

        public StrategyService(
            IKiteConnectService kiteConnectService,
            IPositionRepository positionRepository,
            ILogger<StrategyService> logger,
            IOptions<NiftyOptionStrategyConfig> strategyConfig)
        {
            _kiteConnectService = kiteConnectService;
            _positionRepository = positionRepository;
            _logger = logger;
            _strategyConfig = strategyConfig.Value;
        }

        public async Task HandleTradingViewAlert(TradingViewAlert alert)
        {
            _logger.LogInformation("Handling TradingView Alert: {Action} for {Signal}", alert.Action, alert.Signal);

            if (alert.Action?.ToUpper() == "ENTRY")
            {
                await HandleEntry(alert);
            }
            else if (alert.Action?.ToUpper() == "STOPLOSS")
            {
                await HandleStoploss(alert);
            }
            else
            {
                _logger.LogWarning("Unknown alert action: {Action}", alert.Action);
            }
        }

        private async Task HandleEntry(TradingViewAlert alert)
        {
            _logger.LogInformation("Executing ENTRY for Strike: {Strike}, Type: {Type}", alert.Strike, alert.Type);

            // 1. Determine the main and hedge strike prices
            int mainStrike = alert.Strike;
            int hedgeStrike;
            string transactionType = "SELL"; // We are shorting the main leg

            if (alert.Type?.ToUpper() == "CE") // Bearish signal, sell a Call
            {
                hedgeStrike = mainStrike + _strategyConfig.HedgeDistancePoints;
            }
            else // Bullish signal, sell a Put
            {
                hedgeStrike = mainStrike - _strategyConfig.HedgeDistancePoints;
            }

            // 2. Get the trading symbols for the current week's expiry
            string mainTradingSymbol = GetNiftyWeeklyOptionSymbol(mainStrike, alert.Type);
            string hedgeTradingSymbol = GetNiftyWeeklyOptionSymbol(hedgeStrike, alert.Type);

            // 3. Place the orders
            try
            {
                // Sell the main option
                await _kiteConnectService.PlaceOrderAsync(
                    exchange: _strategyConfig.Exchange,
                    tradingsymbol: mainTradingSymbol,
                    transaction_type: transactionType,
                    quantity: _strategyConfig.Quantity,
                    product: _strategyConfig.ProductType,
                    order_type: _strategyConfig.OrderType
                );
                _logger.LogInformation("Main leg order placed: SELL {Symbol}", mainTradingSymbol);

                // Buy the hedge option
                await _kiteConnectService.PlaceOrderAsync(
                    exchange: _strategyConfig.Exchange,
                    tradingsymbol: hedgeTradingSymbol,
                    transaction_type: "BUY", // Hedge is always a buy
                    quantity: _strategyConfig.Quantity,
                    product: _strategyConfig.ProductType,
                    order_type: _strategyConfig.OrderType
                );
                _logger.LogInformation("Hedge leg order placed: BUY {Symbol}", hedgeTradingSymbol);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to place orders for entry strategy.");
            }
        }

        private async Task HandleStoploss(TradingViewAlert alert)
        {
            _logger.LogInformation("Executing STOPLOSS for Signal: {Signal}", alert.Signal);
            // In a real scenario, you would fetch the open position associated with this signal.
            // For now, we will assume we need to close any open Nifty option positions.
            await CloseAllOpenPositions();
        }

        public async Task CloseAllOpenPositions()
        {
            var openPositions = await _kiteConnectService.GetPositionsAsync();
            var niftyPositions = openPositions.Where(p => p.TradingSymbol.StartsWith(_strategyConfig.InstrumentPrefix) && p.Quantity != 0).ToList();

            if (!niftyPositions.Any())
            {
                _logger.LogInformation("No open Nifty positions to close.");
                return;
            }

            foreach (var position in niftyPositions)
            {
                string transactionType = position.Quantity > 0 ? "SELL" : "BUY";
                int quantityToClose = Math.Abs(position.Quantity);

                _logger.LogInformation("Closing position: {Type} {Quantity} of {Symbol}", transactionType, quantityToClose, position.TradingSymbol);

                await _kiteConnectService.PlaceOrderAsync(
                    exchange: position.Exchange,
                    tradingsymbol: position.TradingSymbol,
                    transaction_type: transactionType,
                    quantity: quantityToClose,
                    product: position.Product,
                    order_type: "MARKET"
                );
            }
        }

        private string GetNiftyWeeklyOptionSymbol(int strike, string optionType)
        {
            DateTime expiry = GetNextWeeklyExpiry();
            string year = expiry.ToString("yy");
            string month = expiry.ToString("MMM").ToUpper();
            string day = expiry.Day.ToString();

            // Format: NIFTY<YY><MMM><DD><STRIKE><TYPE>
            // Example: NIFTY25JUL1022500CE
            return $"{_strategyConfig.InstrumentPrefix}{year}{month}{day}{strike}{optionType.ToUpper()}";
        }

        public DateTime GetNextWeeklyExpiry()
        {
            DateTime today = DateTime.Today;
            int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilThursday == 0 && DateTime.Now.Hour >= 16)
            {
                // If it's Thursday after market close, get next week's Thursday
                daysUntilThursday = 7;
            }
            return today.AddDays(daysUntilThursday);
        }

        public async Task ClosePositionAsync(string tradingSymbol)
        {
            _logger.LogInformation("Closing position for {Symbol}", tradingSymbol);
            var positions = await _kiteConnectService.GetPositionsAsync();

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
