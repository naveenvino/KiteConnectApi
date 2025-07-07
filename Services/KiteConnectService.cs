using KiteConnect;
using KiteConnectApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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

            // --- FIX ---
            // Read both the API Key and the Access Token from the configuration.
            var apiKey = _configuration["KiteConnect:ApiKey"];
            _accessToken = _configuration["KiteConnect:AccessToken"];

            _kite = new Kite(apiKey, Debug: true);

            // Set the access token on the Kite object immediately if it exists.
            if (!string.IsNullOrEmpty(_accessToken) && _accessToken != "YOUR_ACCESS_TOKEN_HERE")
            {
                _kite.SetAccessToken(_accessToken);
                _logger.LogInformation("KiteConnectService initialized with Access Token from configuration.");
            }
            else
            {
                _logger.LogWarning("KiteConnectService initialized WITHOUT an Access Token. Real trading will fail.");
            }
            // --- END OF FIX ---
        }

        public string GetLoginUrl()
        {
            return _kite.GetLoginURL();
        }

        public async Task<User> GenerateSessionAsync(string requestToken)
        {
            await Task.Yield();
            try
            {
                User user = _kite.GenerateSession(requestToken, _configuration["KiteConnect:ApiSecret"]);
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
            try
            {
                return _kite.GetInstruments(exchange);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting instruments.");
                throw;
            }
        }

        public async Task<Dictionary<string, Quote>> GetQuotesAsync(string[] instruments)
        {
            await Task.Yield();
            try
            {
                return _kite.GetQuote(instruments);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting quotes.");
                throw;
            }
        }

        public async Task<List<Position>> GetPositionsAsync()
        {
            await Task.Yield();
            try
            {
                PositionResponse positionResponse = _kite.GetPositions();
                return positionResponse.Net;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting positions.");
                throw;
            }
        }

        public async Task<List<Order>> GetOrdersAsync()
        {
            await Task.Yield();
            try
            {
                return _kite.GetOrders();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting orders.");
                throw;
            }
        }

        public async Task<List<Historical>> GetHistoricalDataAsync(string instrumentToken, DateTime from, DateTime to, string interval, bool continuous, int? oi = 0)
        {
            await Task.Yield();
            try
            {
                bool oiFlag = oi.HasValue && oi.Value == 1;
                return _kite.GetHistoricalData(instrumentToken, from, to, interval, continuous, oiFlag);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting historical data.");
                throw;
            }
        }

        public async Task<Dictionary<string, dynamic>> PlaceOrderAsync(string? exchange, string? tradingsymbol, string? transaction_type, int quantity, string? product, string? order_type, decimal? price = null, decimal? trigger_price = null, int? disclosed_quantity = null, string? validity = "DAY", string? tag = null, string? positionId = null)
        {
            await Task.Yield();
            try
            {
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
            catch (Exception e)
            {
                _logger.LogError(e, "Error placing order.");
                throw;
            }
        }

        public async Task<Dictionary<string, dynamic>> ModifyOrderAsync(string orderId, string? exchange = null, string? tradingSymbol = null, int? quantity = null, string? orderType = null, decimal? price = null, decimal? triggerPrice = null, int? variety = null, string? validity = null)
        {
            return await Task.FromResult(new Dictionary<string, dynamic>());
        }

        public async Task<Dictionary<string, dynamic>> CancelOrderAsync(string orderId, string? variety = null)
        {
            return await Task.FromResult(new Dictionary<string, dynamic>());
        }

        public Task<List<Trade>> GetOrderTradesAsync(string orderId)
        {
            throw new NotImplementedException();
        }

        public Task<List<Order>> GetOrderHistoryAsync(string orderId)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<string, OHLC>> GetOHLCAsync(string[] instrumentTokens)
        {
            throw new NotImplementedException();
        }

        public Task UpdateOrderStatusAsync(string orderId, string status, double price, string? a)
        {
            throw new NotImplementedException();
        }

        public Task CancelAndReplaceWithMarketOrder(string orderId, string exchange, int quantity, string transactionType)
        {
            throw new NotImplementedException();
        }
    }
}
