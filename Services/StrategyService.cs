using KiteConnectApi.Models.Dto;
using KiteConnectApi.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KiteConnectApi.Models.Trading;
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
            if (!await _riskManagementService.CanPlaceOrder())
            {
                _logger.LogWarning("Risk Management Check FAILED: Max open positions reached. Order not placed.");
                return;
            }
            _logger.LogInformation("Risk Management Check PASSED.");

            _logger.LogInformation("Executing ENTRY for Strike: {Strike}, Type: {Type}", alert.Strike, alert.Type);

            int mainStrike = alert.Strike;
            int hedgeStrike;
            if (alert.Type?.ToUpper() == "CE")
            {
                hedgeStrike = mainStrike + _strategyConfig.HedgeDistancePoints;
            }
            else
            {
                hedgeStrike = mainStrike - _strategyConfig.HedgeDistancePoints;
            }

            if (alert.Type == null) { _logger.LogWarning("Alert type is null."); return; }
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

                await _kiteConnectService.PlaceOrderAsync(_strategyConfig.Exchange, mainTradingSymbol, "SELL", _strategyConfig.Quantity, _strategyConfig.ProductType, _strategyConfig.OrderType, positionId: newPositionId);
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

            if (alert.Signal == null) { _logger.LogWarning("Alert signal is null."); return; }
            var positionToClose = await _positionRepository.GetOpenPositionBySignalAsync(alert.Signal);

            if (positionToClose == null || positionToClose.PositionId == null)
            {
                _logger.LogWarning("Could not find an open position for signal {Signal} to apply stoploss.", alert.Signal);
                return;
            }

            await ExitPositionByIdAsync(positionToClose.PositionId);
        }

        public async Task ExitPositionByIdAsync(string positionId)
        {
            _logger.LogInformation("Attempting to exit position {PositionId}", positionId);
            var positionToClose = await _positionRepository.GetPositionByIdAsync(positionId);

            if (positionToClose == null || positionToClose.Status != "Open")
            {
                _logger.LogWarning("Position {PositionId} not found or is not open.", positionId);
                return;
            }

            var ordersToClose = await _orderRepository.GetOrdersByPositionIdAsync(positionId);

            // --- FIX: Create a copy of the list before iterating to prevent modification errors ---
            foreach (var order in ordersToClose.ToList())
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
            // --- END OF FIX ---

            positionToClose.Status = "Closed";
            positionToClose.LastUpdated = DateTime.UtcNow;
            await _positionRepository.UpdatePositionAsync(positionToClose);
            _logger.LogInformation("Position {PositionId} status updated to Closed.", positionToClose.PositionId);
        }

        public async Task ExitAllPositionsAsync()
        {
            _logger.LogWarning("Executing MANUAL EXIT for ALL open positions.");
            var openPositions = await _positionRepository.GetOpenPositionsAsync();
            foreach (var position in openPositions.ToList())
            {
                if (position.PositionId != null)
                {
                    await ExitPositionByIdAsync(position.PositionId);
                }
            }
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
    }
}
