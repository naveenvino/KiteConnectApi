using MediatR;
using KiteConnectApi.Data;
using KiteConnectApi.Models.Dto;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using KiteConnect;
using KiteConnectApi.Services;

namespace KiteConnectApi.Features.Commands
{
    public class PlaceEntryOrderCommandHandler : IRequestHandler<PlaceEntryOrderCommand, bool>
    {
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly ILogger<PlaceEntryOrderCommandHandler> _logger;

        public PlaceEntryOrderCommandHandler(
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            ILogger<PlaceEntryOrderCommandHandler> logger)
        {
            _context = context;
            _kiteConnectService = kiteConnectService;
            _logger = logger;
        }

        public async Task<bool> Handle(PlaceEntryOrderCommand request, CancellationToken cancellationToken)
        {
            var alert = request.Alert;
            var config = request.Config;

            _logger.LogInformation($"Handling entry alert for strategy: {config.StrategyName}");

            // 1. Determine the nearest weekly expiry
            var instruments = await _kiteConnectService.GetInstrumentsAsync(config.Exchange);
            var expiryDate = GetNearestWeeklyExpiry(instruments, config.UnderlyingInstrument);

            if (expiryDate == default)
            {
                _logger.LogError("Could not determine the nearest weekly expiry.");
                return false;
            }

            // 2. Construct the trading symbols
            var mainTradingSymbol = $"{config.UnderlyingInstrument}{expiryDate:yyMMM}{alert.Strike}{alert.Type}";
            var hedgeStrike = alert.Strike + (alert.Type == "CE" ? config.HedgeDistancePoints : -config.HedgeDistancePoints);
            var hedgeTradingSymbol = $"{config.UnderlyingInstrument}{expiryDate:yyMMM}{hedgeStrike}{(alert.Type == "CE" ? "CE" : "PE")}";


            // 3. Calculate quantity
            int quantity;
            if (config.Quantity > 0)
            {
                quantity = config.Quantity;
            }
            else
            {
                // Dynamic quantity calculation based on margin
                var ltpResponse = await _kiteConnectService.GetQuotesAsync(new[] { $"NFO:{mainTradingSymbol}" });
                if (ltpResponse.TryGetValue($"NFO:{mainTradingSymbol}", out var quote) && quote.LastPrice > 0)
                {
                    var marginRequired = quote.LastPrice * 50; // Assuming lot size of 50 for Nifty
                    quantity = (int)(config.AllocatedMargin / marginRequired);
                }
                else
                {
                    _logger.LogError($"Could not get LTP for {mainTradingSymbol} to calculate dynamic quantity.");
                    return false;
                }
            }

            
            // 4. Place orders
            try
            {
                // Place the main order (sell)
                var mainOrderResponse = await _kiteConnectService.PlaceOrderAsync(
                    exchange: config.Exchange,
                    tradingsymbol: mainTradingSymbol,
                    transaction_type: "SELL",
                    quantity: quantity,
                    product: config.ProductType,
                    order_type: config.EntryOrderType
                );

                if (mainOrderResponse == null || !mainOrderResponse.ContainsKey("order_id"))
                {
                    _logger.LogError($"Failed to place main order for {mainTradingSymbol}.");
                    return false;
                }

                var mainOrderId = mainOrderResponse["order_id"].ToString();

                // Place the hedge order (buy)
                var hedgeOrderResponse = await _kiteConnectService.PlaceOrderAsync(
                    exchange: config.Exchange,
                    tradingsymbol: hedgeTradingSymbol,
                    transaction_type: "BUY",
                    quantity: quantity,
                    product: config.ProductType,
                    order_type: "MARKET" // Hedge is always a market order
                );

                if (hedgeOrderResponse == null || !hedgeOrderResponse.ContainsKey("order_id"))
                {
                    _logger.LogError($"Failed to place hedge order for {hedgeTradingSymbol}. Main order {mainOrderId} will be cancelled.");
                    await _kiteConnectService.CancelOrderAsync(mainOrderId);
                    return false;
                }

                var hedgeOrderId = hedgeOrderResponse["order_id"].ToString();

                // 5. Save position and orders to the database
                var position = new TradePosition
                {
                    StrategyConfigId = config.Id,
                    TradingSymbol = mainTradingSymbol,
                    Quantity = quantity,
                    AveragePrice = 0, // Will be updated by webhook
                    Product = config.ProductType,
                    Status = "OPEN",
                    EntryTime = DateTime.UtcNow,
                    HedgeTradingSymbol = hedgeTradingSymbol
                };

                _context.TradePositions.Add(position);
                await _context.SaveChangesAsync();

                var mainOrder = new KiteConnectApi.Models.Trading.Order
                {
                    OrderId = mainOrderId,
                    PositionId = position.PositionId,
                    TradingSymbol = mainTradingSymbol,
                    TransactionType = "SELL",
                    Quantity = quantity,
                    OrderType = config.EntryOrderType,
                    Status = "PENDING",
                    OrderTimestamp = DateTime.UtcNow
                };

                var hedgeOrder = new KiteConnectApi.Models.Trading.Order
                {
                    OrderId = hedgeOrderId,
                    PositionId = position.PositionId,
                    TradingSymbol = hedgeTradingSymbol,
                    TransactionType = "BUY",
                    Quantity = quantity,
                    OrderType = "MARKET",
                    Status = "PENDING",
                    OrderTimestamp = DateTime.UtcNow
                };

                _context.Orders.AddRange(mainOrder, hedgeOrder);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully placed entry orders for strategy {config.StrategyName}. Main order: {mainOrderId}, Hedge order: {hedgeOrderId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while placing entry orders.");
                return false;
            }
        }

        private DateTime GetNearestWeeklyExpiry(IEnumerable<KiteConnectApi.Models.Dto.InstrumentDto> instruments, string? underlyingInstrument)
        {
            if (string.IsNullOrEmpty(underlyingInstrument)) return default;

            var today = DateTime.Today;
            var nextThursday = today.DayOfWeek <= DayOfWeek.Thursday
                ? today.AddDays(DayOfWeek.Thursday - today.DayOfWeek)
                : today.AddDays(7 - (int)today.DayOfWeek + (int)DayOfWeek.Thursday);

            var weeklyExpiries = instruments
                .Where(i => i.InstrumentType == "CE" && i.Name == underlyingInstrument && i.Expiry.HasValue && i.Expiry.Value.DayOfWeek == DayOfWeek.Thursday)
                .Select(i => i.Expiry.Value)
                .Distinct()
                .OrderBy(d => d);

            return weeklyExpiries.FirstOrDefault(d => d >= nextThursday);
        }
    }
}