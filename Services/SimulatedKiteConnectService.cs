using KiteConnect;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class SimulatedKiteConnectService : IKiteConnectService
    {
        private readonly ILogger<SimulatedKiteConnectService> _logger;
        private List<KiteConnect.Order> _orders = new List<KiteConnect.Order>();
        private List<Position> _positions = new List<Position>();
        private List<Trade> _trades = new List<Trade>();
        private List<Historical> _historicalData = new List<Historical>();


        // --- MODIFICATION ---
        // Added a constructor to get the logger service.
        public SimulatedKiteConnectService(ILogger<SimulatedKiteConnectService> logger)
        {
            _logger = logger;
        }
        // --- END OF MODIFICATION ---

        public void LoadHistoricalData(List<Historical> data)
        {
            _historicalData = data;
        }

        public async Task<Dictionary<string, dynamic>> PlaceOrderAsync(string? exchange, string? tradingsymbol, string? transaction_type, int quantity, string? product, string? order_type, decimal? price = null, decimal? trigger_price = null, int? disclosed_quantity = null, string? validity = "DAY", string? tag = null, string? positionId = null)
        {
            await Task.Delay(10);
            var orderId = Guid.NewGuid().ToString();
            var order = new KiteConnect.Order
            {
                OrderId = orderId,
                Tradingsymbol = tradingsymbol,
                Status = "COMPLETE",
                AveragePrice = price ?? 100m,
                Quantity = quantity,
                TransactionType = transaction_type,
                OrderTimestamp = DateTime.UtcNow,
                Exchange = exchange,
                Product = product,
                OrderType = order_type,
                Price = price ?? 0,
                TriggerPrice = trigger_price ?? 0,
            };
            _orders.Add(order);

            // --- MODIFICATION ---
            // Added a log message to confirm order placement.
            _logger.LogInformation(
                "SIMULATED ORDER PLACED: Symbol={TradingSymbol}, Type={TransactionType}, Qty={Quantity}, OrderId={OrderId}",
                tradingsymbol,
                transaction_type,
                quantity,
                orderId
            );
            // --- END OF MODIFICATION ---

            return new Dictionary<string, dynamic> { { "order_id", orderId } };
        }

        public Task<List<KiteConnect.Order>> GetOrdersAsync()
        {
            return Task.FromResult(_orders);
        }

        public Task<List<Trade>> GetOrderTradesAsync(string orderId)
        {
            return Task.FromResult(_trades.Where(t => t.OrderId == orderId).ToList());
        }

        public Task UpdateOrderStatusAsync(string orderId, string status, double averagePrice = 0, string? statusMessage = null)
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order.OrderId != null)
            {
                order.Status = status;
                order.AveragePrice = (decimal)averagePrice;
                order.OrderTimestamp = DateTime.UtcNow;
            }
            return Task.CompletedTask;
        }

        public Task<Dictionary<string, dynamic>> CancelOrderAsync(string order_id, string? variety = "regular")
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == order_id);
            if (order.OrderId != null)
            {
                order.Status = "CANCELLED";
                return Task.FromResult(new Dictionary<string, dynamic> { { "order_id", order.OrderId } });
            }
            throw new Exception("Order not found");
        }

        public Task<List<Position>> GetPositionsAsync()
        {
            return Task.FromResult(_positions);
        }

        public Task<List<Instrument>> GetInstrumentsAsync(string? exchange = null)
        {
            return Task.FromResult(new List<Instrument>());
        }

        public Task<Dictionary<string, Quote>> GetQuotesAsync(string[] instruments)
        {
            var quotes = new Dictionary<string, Quote>();
            foreach (var instrument in instruments)
            {
                quotes[instrument] = new Quote { LastPrice = new Random().Next(100, 2000) };
            }
            return Task.FromResult(quotes);
        }

        public Task<List<Historical>> GetHistoricalDataAsync(string instrumentToken, DateTime from, DateTime to, string interval, bool continuous = false, int? oi = null)
        {
            return Task.FromResult(_historicalData.Where(h => h.TimeStamp >= from && h.TimeStamp <= to).ToList());
        }

        public string GetLoginUrl()
        {
            return "http://localhost/login";
        }

        public Task<User> GenerateSessionAsync(string requestToken)
        {
            return Task.FromResult(new User());
        }

        public Task<Dictionary<string, dynamic>> ModifyOrderAsync(string order_id, string? exchange = null, string? tradingsymbol = null, int? quantity = null, string? order_type = null, decimal? price = null, decimal? trigger_price = null, int? disclosed_quantity = null, string? validity = null)
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == order_id);
            if (order.OrderId != null)
            {
                if (quantity.HasValue) order.Quantity = quantity.Value;
                if (price.HasValue) order.Price = price.Value;
                if (trigger_price.HasValue) order.TriggerPrice = trigger_price.Value;
                if (!string.IsNullOrEmpty(order_type)) order.OrderType = order_type;
                order.Status = "MODIFIED";
                return Task.FromResult(new Dictionary<string, dynamic> { { "order_id", order.OrderId } });
            }
            throw new Exception("Order not found");
        }

        public Task<List<KiteConnect.Order>> GetOrderHistoryAsync(string orderId)
        {
            return Task.FromResult(_orders.Where(o => o.OrderId == orderId).ToList());
        }

        public Task CancelAndReplaceWithMarketOrder(string orderId, string tradingSymbol, int quantity, string transactionType)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, OHLC>> GetOHLCAsync(string[] instruments)
        {
            throw new NotImplementedException();
        }
    }
}
