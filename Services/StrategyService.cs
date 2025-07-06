using KiteConnect;
using KiteConnectApi.Models.Trading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KiteConnectApi.Repositories;

namespace KiteConnectApi.Services
{
    public class StrategyService
    {
        private readonly KiteConnectService _kiteConnectService;
        private readonly ILogger<StrategyService> _logger;
        private readonly NiftyOptionStrategyConfig _strategyConfig;
        private readonly IPositionRepository _positionRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly RiskManagementService _riskManagementService;

        public StrategyService(KiteConnectService kiteConnectService, IConfiguration configuration, ILogger<StrategyService> logger, IPositionRepository positionRepository, IOrderRepository orderRepository, RiskManagementService riskManagementService)
        {
            _kiteConnectService = kiteConnectService;
            _logger = logger;
            _strategyConfig = configuration.GetSection("NiftyOptionStrategy").Get<NiftyOptionStrategyConfig>();
            _positionRepository = positionRepository;
            _orderRepository = orderRepository;
            _riskManagementService = riskManagementService;
        }

        public async Task HandleTradingViewAlert(TradingViewAlert alert)
        {
            _logger.LogInformation($"Received alert. Signal: {alert.Signal}, Type: {alert.Type}, Strike: {alert.Strike}, Action: {alert.Action}");

            if (string.Equals(alert.Action, "Entry", StringComparison.OrdinalIgnoreCase))
            {
                if (await _riskManagementService.CheckTradeAllowed("Entry"))
                {
                    await EnterHedgedPosition(alert);
                }
                else
                {
                    _logger.LogWarning($"Entry trade not allowed by risk management for alert: {alert.Signal} {alert.Type} {alert.Strike}");
                }
            }
            else if (string.Equals(alert.Action, "Stoploss", StringComparison.OrdinalIgnoreCase))
            {
                await ClosePositionAsync(alert.PositionId);
            }
            else
            {
                _logger.LogWarning($"Unknown action type received: {alert.Action}");
            }
        }

        private async Task EnterHedgedPosition(TradingViewAlert alert)
        {
            try
            {
                var expiryDate = GetNextWeeklyExpiry();
                _logger.LogInformation($"Target expiry date: {expiryDate:yyyy-MM-dd}");

                var instruments = await _kiteConnectService.GetInstrumentsAsync("NFO");

                var optionToSell = FindInstrument(instruments, "NIFTY", expiryDate, alert.Strike, alert.Type);

                // For a CE sell, the hedge is a CE buy at a higher strike.
                // For a PE sell, the hedge is a PE buy at a lower strike.
                int hedgeStrike = alert.Type == "CE" ? alert.Strike + _strategyConfig.HedgeDistancePoints : alert.Strike - _strategyConfig.HedgeDistancePoints;
                var optionToBuy = FindInstrument(instruments, "NIFTY", expiryDate, hedgeStrike, alert.Type);

                if (optionToSell.InstrumentToken == 0 || optionToBuy.InstrumentToken == 0)
                {
                    _logger.LogError($"Could not find one or both instruments. Sell: {alert.Type} {alert.Strike}, Buy: {alert.Type} {hedgeStrike}");
                    return;
                }

                _logger.LogInformation($"Instrument to Sell: {optionToSell.TradingSymbol}");
                _logger.LogInformation($"Instrument to Buy (Hedge): {optionToBuy.TradingSymbol}");

                var position = new TradePosition
                {
                    PositionId = Guid.NewGuid().ToString(),
                    EntryInstrumentToken = (int)optionToSell.InstrumentToken,
                    EntryTradingSymbol = optionToSell.TradingSymbol,
                    HedgeInstrumentToken = (int)optionToBuy.InstrumentToken,
                    HedgeTradingSymbol = optionToBuy.TradingSymbol,
                    Quantity = _strategyConfig.Quantity,
                    Strike = alert.Strike,
                    OptionType = alert.Type,
                    Expiry = expiryDate,
                    Status = "Open"
                };

                await _positionRepository.AddPositionAsync(position);

                // Place sell order (main position)
                var sellOrderResponse = await _kiteConnectService.PlaceOrderAsync(
                    exchange: _strategyConfig.Exchange,
                    tradingsymbol: optionToSell.TradingSymbol,
                    transaction_type: "SELL",
                    quantity: _strategyConfig.Quantity,
                    product: _strategyConfig.ProductType,
                    order_type: _strategyConfig.OrderType,
                    positionId: position.PositionId
                );
                var sellOrderId = sellOrderResponse["order_id"].ToString();
                _logger.LogInformation($"Sell order placed. Order ID: {sellOrderId}");

                // Place buy order (hedge)
                var buyOrderResponse = await _kiteConnectService.PlaceOrderAsync(
                    exchange: _strategyConfig.Exchange,
                    tradingsymbol: optionToBuy.TradingSymbol,
                    transaction_type: "BUY",
                    quantity: _strategyConfig.Quantity,
                    product: _strategyConfig.ProductType,
                    order_type: _strategyConfig.OrderType,
                    positionId: position.PositionId
                );
                var buyOrderId = buyOrderResponse["order_id"].ToString();
                _logger.LogInformation($"Buy (hedge) order placed. Order ID: {buyOrderId}");

                // Update position with order IDs (assuming immediate fill for market orders for now)
                position.EntryOrderId = sellOrderId;
                position.HedgeOrderId = buyOrderId;

                // In a real scenario, you would fetch the actual traded price from order status updates
                // For now, we'll assume a placeholder or fetch from a quote service if available
                // For demonstration, let's assume a dummy price for now.
                position.EntryPrice = 0.0; // Placeholder, update with actual fill price
                position.HedgePrice = 0.0; // Placeholder, update with actual fill price

                await _positionRepository.UpdatePositionAsync(position);
                _logger.LogInformation($"Position opened successfully and saved. Position ID: {position.PositionId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while entering the hedged position.");
            }
        }

        private async Task ClosePositionAsync(string positionId)
        {
            var position = await _positionRepository.GetPositionByIdAsync(positionId);
            if (position == null || position.Status != "Open")
            {
                _logger.LogWarning($"No open position found with ID: {positionId} to close.");
                return;
            }

            try
            {
                _logger.LogInformation($"Attempting to close position {position.PositionId}.");

                // To close the short position, we need to buy it back.
                var closeEntryOrderResponse = await _kiteConnectService.PlaceOrderAsync(
                    exchange: _strategyConfig.Exchange,
                    tradingsymbol: position.EntryTradingSymbol,
                    transaction_type: "BUY",
                    quantity: position.Quantity,
                    product: _strategyConfig.ProductType,
                    order_type: _strategyConfig.OrderType,
                    positionId: position.PositionId
                );
                _logger.LogInformation($"Close order for entry leg placed. Order ID: {closeEntryOrderResponse["order_id"]}");

                // To close the long hedge position, we need to sell it.
                var closeHedgeOrderResponse = await _kiteConnectService.PlaceOrderAsync(
                     exchange: _strategyConfig.Exchange,
                     tradingsymbol: position.HedgeTradingSymbol,
                     transaction_type: "SELL",
                     quantity: position.Quantity,
                     product: _strategyConfig.ProductType,
                     order_type: _strategyConfig.OrderType,
                     positionId: position.PositionId
                 );
                _logger.LogInformation($"Close order for hedge leg placed. Order ID: {closeHedgeOrderResponse["order_id"]}");

                position.Status = "Closed";
                position.ExitTime = DateTime.UtcNow;
                await _positionRepository.UpdatePositionAsync(position);
                _logger.LogInformation($"Position {position.PositionId} has been successfully closed and updated in DB.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while closing position {positionId}.");
            }
        }

        private Instrument FindInstrument(List<Instrument> instruments, string underlying, DateTime expiry, int strike, string instrumentType)
        {
            return instruments.FirstOrDefault(i =>
                i.Name == underlying &&
                i.Expiry.HasValue && i.Expiry.Value.Date == expiry.Date &&
                i.Strike == strike &&
                i.InstrumentType == instrumentType);
        }

        private DateTime GetNextWeeklyExpiry()
        {
            DateTime today = DateTime.Today;
            // DayOfWeek enum: Sunday = 0, Monday = 1, ..., Thursday = 4, ...
            int daysUntilThursday = ((int)DayOfWeek.Thursday - (int)today.DayOfWeek + 7) % 7;

            // If today is Thursday and the market is closed, get next week's Thursday.
            if (daysUntilThursday == 0 && DateTime.Now.TimeOfDay > new TimeSpan(15, 30, 0))
            {
                daysUntilThursday = 7;
            }
            return today.AddDays(daysUntilThursday);
        }
    }
}