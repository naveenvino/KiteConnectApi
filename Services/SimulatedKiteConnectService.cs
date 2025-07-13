using KiteConnect;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.Dto;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public interface ISimulatedKiteConnectService : IKiteConnectService
    {
        void SetHistoricalData(List<SimulatedHistoricalData> historicalData);
    }

    public class SimulatedKiteConnectService : IKiteConnectService, ISimulatedKiteConnectService
    {
        private List<SimulatedHistoricalData> _historicalData = new List<SimulatedHistoricalData>();
        private readonly ILogger<SimulatedKiteConnectService> _logger;

        public SimulatedKiteConnectService(ILogger<SimulatedKiteConnectService> logger)
        {
            _logger = logger;
        }

        public void SetHistoricalData(List<SimulatedHistoricalData> historicalData)
        {
            _historicalData = historicalData;
        }

        public Task<Dictionary<string, dynamic>> PlaceOrderAsync(string? exchange, string? tradingsymbol, string? transaction_type, int quantity, string? product, string? order_type, decimal? price = null, decimal? trigger_price = null, decimal? limit_price = null, int? disclosed_quantity = null, string? validity = "DAY", string? tag = null, string? positionId = null)
        {
            _logger.LogInformation($"SIMULATED PlaceOrder: Symbol={tradingsymbol}, Type={transaction_type}, Qty={quantity}, OrderType={order_type}");
            return Task.FromResult(new Dictionary<string, dynamic> { { "order_id", Guid.NewGuid().ToString() } });
        }

        public Task<Dictionary<string, dynamic>> ModifyOrderAsync(string order_id, string? exchange = null, string? tradingsymbol = null, int? quantity = null, string? order_type = null, decimal? price = null, decimal? trigger_price = null, decimal? limit_price = null, int? disclosed_quantity = null, string? validity = null)
        {
            _logger.LogInformation($"SIMULATED ModifyOrder: OrderId={order_id}, Qty={quantity}, Price={price}, TriggerPrice={trigger_price}, LimitPrice={limit_price}");
            return Task.FromResult(new Dictionary<string, dynamic> { { "order_id", order_id } });
        }

        public Task<Dictionary<string, Quote>> GetQuotesAsync(string[] instruments)
        {
            var quotes = new Dictionary<string, Quote>();
            var random = new Random();

            foreach (var instrument in instruments)
            {
                quotes.Add(instrument, new Quote
                {
                    InstrumentToken = (uint)random.Next(100000, 999999),
                    LastPrice = (decimal)(random.NextDouble() * 2000 + 500),
                    LastQuantity = (uint)random.Next(1, 100),
                    AveragePrice = (decimal)(random.NextDouble() * 2000 + 500),
                    Volume = (uint)random.Next(100000, 1000000),
                    BuyQuantity = (uint)random.Next(1000, 5000),
                    SellQuantity = (uint)random.Next(1000, 5000),
                    Change = (decimal)(random.NextDouble() * 100 - 50),
                    LastTradeTime = DateTime.UtcNow.AddSeconds(-random.Next(0, 60)),
                    OI = (uint)random.Next(10000, 100000),
                    OIDayHigh = (uint)random.Next(100000, 200000),
                    OIDayLow = (uint)random.Next(5000, 10000)
                });
            }
            return Task.FromResult(quotes);
        }

        public Task<List<InstrumentDto>> GetInstrumentsAsync(string? exchange = null)
        {
            // For simulation, return a dummy list of instruments
            var instruments = new List<InstrumentDto>
            {
                new InstrumentDto
                {
                    InstrumentToken = 256265,
                    Exchange = "NFO",
                    TradingSymbol = "NIFTY25JUL24C22500",
                    Name = "NIFTY",
                    Expiry = new DateTime(2024, 07, 25),
                    Strike = 22500,
                    InstrumentType = "CE"
                },
                new InstrumentDto
                {
                    InstrumentToken = 256266,
                    Exchange = "NFO",
                    TradingSymbol = "NIFTY25JUL24P22500",
                    Name = "NIFTY",
                    Expiry = new DateTime(2024, 07, 25),
                    Strike = 22500,
                    InstrumentType = "PE"
                }
            };
            return Task.FromResult(instruments);
        }

        public string GetLoginUrl() => "http://simulated.kiteconnect.com/login";
        public Task<User> GenerateSessionAsync(string requestToken) => Task.FromResult(new User());
        public Task<List<KiteConnect.Order>> GetOrdersAsync() => Task.FromResult(new List<KiteConnect.Order>());
        public Task<List<Trade>> GetOrderTradesAsync(string orderId) => Task.FromResult(new List<Trade>());
        public Task UpdateOrderStatusAsync(string orderId, string status, double averagePrice = 0, string? statusMessage = null) => Task.CompletedTask;
        public Task<Dictionary<string, dynamic>> CancelOrderAsync(string order_id, string? variety = "regular") => Task.FromResult(new Dictionary<string, dynamic>());
        public Task<List<KiteConnect.Order>> GetOrderHistoryAsync(string orderId) => Task.FromResult(new List<KiteConnect.Order>());
        public Task CancelAndReplaceWithMarketOrder(string orderId, string tradingSymbol, int quantity, string transactionType) => Task.CompletedTask;
        public Task<Dictionary<string, OHLC>> GetOHLCAsync(string[] instruments) => Task.FromResult(new Dictionary<string, OHLC>());
        public Task<List<KiteConnect.Holding>> GetHoldingsAsync() => Task.FromResult(new List<KiteConnect.Holding>());
        public Task<List<KiteConnectApi.Models.Trading.SimulatedHistoricalData>> GetHistoricalDataAsync(string instrumentToken, DateTime from, DateTime to, string interval, bool continuous = false, int? oi = null)
        {
            // In a real backtesting scenario, this would fetch actual historical data.
            // For now, return the data set via SetHistoricalData.
            return Task.FromResult(_historicalData.Where(d => d.TimeStamp >= from && d.TimeStamp <= to).ToList());
        }

        public Task<List<TradePosition>> GetPositionsAsync()
        {
            return Task.FromResult(new List<TradePosition>());
        }
    }
}