using KiteConnect;
using KiteConnectApi.Models.Trading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class KiteConnectService : IKiteConnectService
    {
        private readonly Kite _kite;
        private readonly ILogger<KiteConnectService> _logger;
        private readonly IConfiguration _configuration;
        private string? _accessToken;

        public KiteConnectService(IConfiguration configuration, ILogger<KiteConnectService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            var apiKey = _configuration["KiteConnect:ApiKey"];
            _accessToken = _configuration["KiteConnect:AccessToken"];

            _kite = new Kite(apiKey, Debug: true);

            if (!string.IsNullOrEmpty(_accessToken) && _accessToken != "YOUR_ACCESS_TOKEN_HERE")
            {
                _kite.SetAccessToken(_accessToken);
                _logger.LogInformation("KiteConnectService initialized with Access Token from configuration.");
            }
            else
            {
                _logger.LogWarning("KiteConnectService initialized WITHOUT an Access Token. Real trading will fail.");
            }
        }

        public string GetLoginUrl() => _kite.GetLoginURL();

        public async Task<User> GenerateSessionAsync(string requestToken)
        {
            await Task.Yield();
            try
            {
                User user = _kite.GenerateSession(requestToken, _configuration["Kite:ApiSecret"]);
                _accessToken = user.AccessToken;
                _logger.LogInformation("Session generated successfully.");
                return user;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error generating session.");
                throw;
            }
        }

        public void SetAccessToken(string accessToken)
        {
            _accessToken = accessToken;
            _kite.SetAccessToken(accessToken);
            _logger.LogInformation("Access token set.");
        }

        public async Task<List<Instrument>> GetInstrumentsAsync(string? exchange = null)
        {
            await Task.Yield();
            return _kite.GetInstruments(exchange);
        }

        public async Task<Dictionary<string, Quote>> GetQuotesAsync(string[] instruments)
        {
            await Task.Yield();
            return _kite.GetQuote(instruments);
        }

        public async Task<List<TradePosition>> GetPositionsAsync()
        {
            await Task.Yield();
            PositionResponse positionResponse = _kite.GetPositions();
            return positionResponse.Net.Select(p => new TradePosition
            {
                TradingSymbol = p.TradingSymbol,
                Quantity = p.Quantity,
                AveragePrice = p.AveragePrice,
                PnL = p.Unrealised + p.Realised, // Corrected property names
                LastUpdated = DateTime.UtcNow,
                Status = p.Quantity == 0 ? "Closed" : "Open",
                Product = p.Product,
                Exchange = p.Exchange
            }).ToList();
        }

        public async Task<List<KiteConnect.Order>> GetOrdersAsync()
        {
            await Task.Yield();
            return _kite.GetOrders();
        }

        public async Task<List<Historical>> GetHistoricalDataAsync(string instrumentToken, DateTime from, DateTime to, string interval, bool continuous, int? oi = 0)
        {
            await Task.Yield();
            bool oiFlag = oi.HasValue && oi.Value == 1;
            return _kite.GetHistoricalData(instrumentToken, from, to, interval, continuous, oiFlag);
        }

        public async Task<Dictionary<string, dynamic>> PlaceOrderAsync(string? exchange, string? tradingsymbol, string? transaction_type, int quantity, string? product, string? order_type, decimal? price = null, decimal? trigger_price = null, int? disclosed_quantity = null, string? validity = "DAY", string? tag = null, string? positionId = null)
        {
            await Task.Yield();
            return _kite.PlaceOrder(
                Exchange: exchange,
                TradingSymbol: tradingsymbol,
                TransactionType: transaction_type,
                Quantity: quantity,
                Price: price,
                Product: product,
                OrderType: order_type,
                Validity: validity,
                DisclosedQuantity: disclosed_quantity,
                TriggerPrice: trigger_price,
                Tag: tag
            );
        }

        public Task<Dictionary<string, dynamic>> ModifyOrderAsync(string orderId, string? exchange = null, string? tradingSymbol = null, int? quantity = null, string? orderType = null, decimal? price = null, decimal? triggerPrice = null, int? disclosedQuantity = null, string? validity = null) => Task.FromResult(new Dictionary<string, dynamic>());
        public Task<Dictionary<string, dynamic>> CancelOrderAsync(string orderId, string? variety = null) => Task.FromResult(new Dictionary<string, dynamic>());
        public Task<List<Trade>> GetOrderTradesAsync(string orderId) => Task.FromResult(new List<Trade>());
        public Task<List<KiteConnect.Order>> GetOrderHistoryAsync(string orderId) => Task.FromResult(new List<KiteConnect.Order>());
        public Task<Dictionary<string, OHLC>> GetOHLCAsync(string[] instrumentTokens) => Task.FromResult(new Dictionary<string, OHLC>());
        public Task UpdateOrderStatusAsync(string orderId, string status, double price, string? statusMessage) => Task.CompletedTask;
        public Task CancelAndReplaceWithMarketOrder(string orderId, string tradingSymbol, int quantity, string transactionType) => Task.CompletedTask;

        public async Task<List<KiteConnect.Holding>> GetHoldingsAsync()
        {
            await Task.Yield();
            return _kite.GetHoldings();
        }
    }
}
