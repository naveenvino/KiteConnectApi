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
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<StrategyService> _logger;
        private readonly NiftyOptionStrategyConfig _strategyConfig;
        private readonly RiskManagementService _riskManagementService;

        public StrategyService(
            IKiteConnectService kiteConnectService,
            IPositionRepository positionRepository,
            IOrderRepository orderRepository,
            ILogger<StrategyService> logger,
            IOptions<NiftyOptionStrategyConfig> strategyConfig,
            RiskManagementService riskManagementService)
        {
            _kiteConnectService = kiteConnectService;
            _positionRepository = positionRepository;
            _orderRepository = orderRepository;
            _logger = logger;
            _strategyConfig = strategyConfig.Value;
            _riskManagementService = riskManagementService;
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
            // --- FIX: Calling CanPlaceOrder without arguments ---
            if (!await _riskManagementService.CanPlaceOrder())
            {
                _logger.LogWarning("Risk Management Check FAILED: Max open positions reached. Order not placed.");
                return;
            }
            _logger.LogInformation("Risk Management Check PASSED.");
            // --- END OF FIX ---

            _logger.LogInformation("Executing ENTRY for Strike: {Strike}, Type: {Type}", alert.Strike, alert.Type);

            int mainStrike = alert.Strike;
            int hedgeStrike;
            string transactionType = "SELL";

            if (alert.Type?.ToUpper() == "CE")
            {
                hedgeStrike = mainStrike + _strategyConfig.HedgeDistancePoints;
            }
            else
            {
                hedgeStrike = mainStrike - _strategyConfig.HedgeDistancePoints;
            }

            string mainTradingSymbol = GetNiftyWeeklyOptionSymbol(mainStrike, alert.Type);
            string hedgeTradingSymbol = GetNiftyWeeklyOptionSymbol(hedgeStrike, alert.Type);
            string newPositionId = Guid.NewGuid().ToString();

            try
            {
                var newPosition = new TradePosition
                {
                    PositionId = newPositionId,
                    TradingSymbol = mainTradingSymbol,
                    Quantity = _strategyConfig.Quantity,
                    Status = "Open",
                    LastUpdated = DateTime.UtcNow,
                    Signal = alert.Signal,
                    Product = _strategyConfig.ProductType,
                    Exchange = _strategyConfig.Exchange
                };
                await _positionRepository.AddPositionAsync(newPosition);
                _logger.LogInformation("New position created and saved to DB with PositionId: {PositionId} for Signal: {Signal}", newPositionId, alert.Signal);

                await _kiteConnectService.PlaceOrderAsync(_strategyConfig.Exchange, mainTradingSymbol, transactionType, _strategyConfig.Quantity, _strategyConfig.ProductType, _strategyConfig.OrderType, positionId: newPositionId);
                await _kiteConnectService.PlaceOrderAsync(_strategyConfig.Exchange, hedgeTradingSymbol, "BUY", _strategyConfig.Quantity, _strategyConfig.ProductType, _strategyConfig.OrderType, positionId: newPositionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to place orders for entry strategy.");
            }
        }

        private async Task HandleStoploss(TradingViewAlert alert)
        {
            _logger.LogInformation("Executing STOPLOSS for Signal: {Signal}", alert.Signal);

            var positionToClose = await _positionRepository.GetOpenPositionBySignalAsync(alert.Signal);

            if (positionToClose == null || positionToClose.PositionId == null)
            {
                _logger.LogWarning("Could not find an open position for signal {Signal} to apply stoploss.", alert.Signal);
                return;
            }

            _logger.LogInformation("Found open position {PositionId} for signal {Signal}. Closing all associated orders.", positionToClose.PositionId, alert.Signal);

            var ordersToClose = await _orderRepository.GetOrdersByPositionIdAsync(positionToClose.PositionId);

            foreach (var order in ordersToClose)
            {
                if (order.TradingSymbol == null) continue;

                string closingTransactionType = order.TransactionType == "BUY" ? "SELL" : "BUY";
                await _kiteConnectService.PlaceOrderAsync(
                    exchange: order.Exchange,
                    tradingsymbol: order.TradingSymbol,
                    transaction_type: closingTransactionType,
                    quantity: order.Quantity,
                    product: order.Product,
                    order_type: "MARKET"
                );
                _logger.LogInformation("Placed closing order for {TradingSymbol}", order.TradingSymbol);
            }

            positionToClose.Status = "Closed";
            positionToClose.LastUpdated = DateTime.UtcNow;
            await _positionRepository.UpdatePositionAsync(positionToClose);
            _logger.LogInformation("Position {PositionId} status updated to Closed.", positionToClose.PositionId);
        }

        private string GetNiftyWeeklyOptionSymbol(int strike, string optionType)
        {
            DateTime expiry = GetNextWeeklyExpiry();
            string year = expiry.ToString("yy");

            string monthChar;
            switch (expiry.Month)
            {
                case 10: monthChar = "O"; break;
                case 11: monthChar = "N"; break;
                case 12: monthChar = "D"; break;
                default: monthChar = expiry.Month.ToString(); break;
            }

            string day = expiry.Day.ToString("D2");

            return $"{_strategyConfig.InstrumentPrefix}{year}{monthChar}{day}{strike}{optionType.ToUpper()}";
        }

        public DateTime GetNextWeeklyExpiry()
        {
            DateTime today = DateTime.Today;
            int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilThursday == 0 && DateTime.Now.Hour >= 16)
            {
                daysUntilThursday = 7;
            }
            return today.AddDays(daysUntilThursday);
        }

        // --- FIX: Added the missing ClosePositionAsync method ---
        public async Task ClosePositionAsync(string tradingSymbol)
        {
            _logger.LogInformation("Closing position for {Symbol}", tradingSymbol);
            var positions = await _kiteConnectService.GetPositionsAsync();

            var positionToClose = positions.FirstOrDefault(p => p.TradingSymbol == tradingSymbol && p.Quantity != 0);

            if (positionToClose != null)
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
        // --- END OF FIX ---
    }
}
