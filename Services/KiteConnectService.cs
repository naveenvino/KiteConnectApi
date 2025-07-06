using KiteConnect;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using KiteConnectApi.Repositories;
using KiteConnectApi.Models.Trading;

namespace KiteConnectApi.Services
{
    public class KiteConnectService
    {
        private readonly Kite _kite;
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly IOrderRepository _orderRepository;

        public KiteConnectService(IConfiguration configuration, IOrderRepository orderRepository)
        {
            _apiKey = configuration["KiteConnect:ApiKey"];
            _apiSecret = configuration["KiteConnect:ApiSecret"];
            _kite = new Kite(_apiKey);
            _orderRepository = orderRepository;
        }

        public string GetLoginUrl() => _kite.GetLoginURL();

        public async Task<User> GenerateSessionAsync(string requestToken)
        {
            return await Task.Run(() => _kite.GenerateSession(requestToken, _apiSecret));
        }

        public async Task<Dictionary<string, dynamic>> PlaceOrderAsync(string exchange, string tradingsymbol, string transaction_type, int quantity, string product, string order_type, decimal? price = null, decimal? trigger_price = null, int? disclosed_quantity = null, string validity = "DAY", string tag = null, string positionId = null)
        {
            var response = await Task.Run(() => _kite.PlaceOrder(
                Exchange: exchange,
                TradingSymbol: tradingsymbol,
                TransactionType: transaction_type,
                Quantity: quantity,
                Product: product,
                OrderType: order_type,
                Price: price,
                TriggerPrice: trigger_price,
                DisclosedQuantity: disclosed_quantity,
                Validity: validity,
                Tag: tag
            ));

            // Save order to database
            var order = new KiteConnectApi.Models.Trading.Order
            {
                OrderId = response["order_id"].ToString(),
                TradingSymbol = tradingsymbol,
                Exchange = exchange,
                TransactionType = transaction_type,
                Quantity = quantity,
                Product = product,
                OrderType = order_type,
                Price = (double)(price ?? 0), // Use 0 if price is null, cast to double
                AveragePrice = 0, // Will be updated later upon fill
                Status = "PENDING", // Initial status
                PlacedTime = DateTime.UtcNow,
                UpdatedTime = DateTime.UtcNow,
                StatusMessage = "Order placed with broker",
                PositionId = positionId // Link to the trade position
            };
            await _orderRepository.AddOrderAsync(order);

            return response;
        }

        public async Task UpdateOrderStatusAsync(string orderId, string status, double averagePrice = 0, string statusMessage = null)
        {
            var order = await _orderRepository.GetOrderByIdAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                order.AveragePrice = averagePrice;
                order.UpdatedTime = DateTime.UtcNow;
                order.StatusMessage = statusMessage ?? order.StatusMessage;
                await _orderRepository.UpdateOrderAsync(order);
            }
        }

        public async Task<Dictionary<string, dynamic>> ModifyOrderAsync(string order_id, string exchange = null, string tradingsymbol = null, int? quantity = null, string order_type = null, decimal? price = null, decimal? trigger_price = null, int? disclosed_quantity = null, string validity = null)
        {
            return await Task.Run(() => _kite.ModifyOrder(
                OrderId: order_id,
                Exchange: exchange,
                TradingSymbol: tradingsymbol,
                Quantity: quantity?.ToString(),
                OrderType: order_type,
                Price: price,
                TriggerPrice: trigger_price,
                DisclosedQuantity: disclosed_quantity,
                Validity: validity
            ));
        }

        public async Task<Dictionary<string, dynamic>> CancelOrderAsync(string order_id, string variety = "regular")
        {
            return await Task.Run(() => _kite.CancelOrder(OrderId: order_id, Variety: variety));
        }

        public async Task<List<KiteConnect.Order>> GetOrdersAsync() => await Task.Run(() => _kite.GetOrders());

        public async Task<List<Trade>> GetOrderTradesAsync(string orderId) => await Task.Run(() => _kite.GetOrderTrades(orderId));

        public async Task<List<Instrument>> GetInstrumentsAsync(string exchange = null) => await Task.Run(() => _kite.GetInstruments(exchange));

        public async Task<Dictionary<string, Quote>> GetQuoteAsync(string[] instruments) => await Task.Run(() => _kite.GetQuote(instruments));

        public async Task<List<Historical>> GetHistoricalDataAsync(string instrumentToken, DateTime from, DateTime to, string interval)
        {
            return await Task.Run(() => _kite.GetHistoricalData(instrumentToken, from, to, interval));
        }

        public async Task<Dictionary<string, OHLC>> GetOHLCAsync(string[] instruments) => await Task.Run(() => _kite.GetOHLC(instruments));
    }
}