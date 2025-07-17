using KiteConnect;
using KiteConnectApi.Models.Trading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using KiteConnectApi.Models.Dto;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class KiteConnectService : IKiteConnectService
    {
        private readonly Kite _kite;
        private readonly ILogger<KiteConnectService> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;
        private readonly VaultService _vaultService;
        private string? _accessToken;

        public KiteConnectService(IConfiguration configuration, ILogger<KiteConnectService> logger, ICacheService cacheService, VaultService vaultService)
        {
            _configuration = configuration;
            _logger = logger;
            _cacheService = cacheService;
            _vaultService = vaultService;

            var apiKey = _configuration["KiteConnect:ApiKey"];
            var apiSecret = _configuration["KiteConnect:ApiSecret"];

            // Synchronously fetch from Vault during construction.
            // In a real-world scenario, you might want to handle this asynchronously or ensure Vault is initialized before this service.
            var vaultSecretsTask = _vaultService.GetSecretAsync("kiteconnect/credentials");
            vaultSecretsTask.Wait(); // Block until secrets are fetched
            var vaultSecrets = vaultSecretsTask.Result;

            if (vaultSecrets != null && vaultSecrets.Data != null)
            {
                apiKey = vaultSecrets.Data.Data["ApiKey"].ToString();
                apiSecret = vaultSecrets.Data.Data["ApiSecret"].ToString();
                _accessToken = vaultSecrets.Data.Data["AccessToken"].ToString();
                _logger.LogInformation("KiteConnect credentials loaded from Vault.");
            }
            else
            {
                _logger.LogWarning("KiteConnect credentials not found in Vault. Falling back to appsettings.json.");
            }

            _kite = new Kite(apiKey, Debug: true);

            if (!string.IsNullOrEmpty(_accessToken) && _accessToken != "YOUR_ACCESS_TOKEN_HERE")
            {
                _kite.SetAccessToken(_accessToken);
                _logger.LogInformation("KiteConnectService initialized with Access Token.");
            }
            else
            {
                _logger.LogWarning("KiteConnectService initialized WITHOUT an Access Token. Real trading will fail.");
            }
        }

        public string GetLoginUrl() => _kite.GetLoginURL();

        public async Task<User> GenerateSessionAsync(string requestToken)
        {
            _logger.LogInformation("Attempting to generate session with request token.");
            try
            {
                User user = await Task.Run(() => _kite.GenerateSession(requestToken, _configuration["Kite:ApiSecret"]));
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

        public async Task<List<InstrumentDto>> GetInstrumentsAsync(string? exchange = null)
        {
            var cacheKey = $"KiteInstruments_{(exchange ?? "ALL")}";
            var instrumentsDto = await _cacheService.GetAsync<List<InstrumentDto>>(cacheKey);
            if (instrumentsDto == null)
            {
                _logger.LogInformation($"Fetching instruments for exchange {exchange ?? "ALL"} from Kite API.");
                try
                {
                    var instruments = _kite.GetInstruments(exchange);
                    instrumentsDto = instruments.Select(i => new InstrumentDto
                    {
                        InstrumentToken = i.InstrumentToken,
                        Exchange = i.Exchange,
                        TradingSymbol = i.TradingSymbol,
                        Name = i.Name,
                        Expiry = i.Expiry,
                        Strike = (uint)i.Strike,
                        InstrumentType = i.InstrumentType
                    }).ToList();
                    await _cacheService.SetAsync(cacheKey, instrumentsDto, TimeSpan.FromHours(24)); // Cache for 24 hours
                    _logger.LogInformation($"Successfully fetched and cached {instrumentsDto.Count} instruments.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error fetching instruments for exchange {exchange ?? "ALL"}.");
                    throw;
                }
            }
            else
            {
                _logger.LogInformation($"Instruments for exchange {exchange ?? "ALL"} retrieved from cache.");
            }
            return instrumentsDto;
        }

        public async Task<Dictionary<string, Quote>> GetQuotesAsync(string[] instruments)
        {
            _logger.LogInformation($"Fetching quotes for {string.Join(", ", instruments)} from Kite API.");
            try
            {
                // Validate instruments array
                if (instruments == null || instruments.Length == 0)
                    throw new ArgumentException("Instruments array cannot be null or empty", nameof(instruments));
                
                // Kite API v3 supports up to 500 instruments per request
                if (instruments.Length > 500)
                    throw new ArgumentException("Maximum 500 instruments allowed per request", nameof(instruments));

                return await Task.Run(() => _kite.GetQuote(instruments));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching quotes for {string.Join(", ", instruments)}.");
                
                // Check if it's a token expiry issue
                if (ex.Message.Contains("token") || ex.Message.Contains("session"))
                {
                    _logger.LogWarning("Possible token expiry detected. Consider implementing token refresh.");
                }
                
                throw;
            }
        }

        public async Task<bool> IsTokenValidAsync()
        {
            try
            {
                // Test token validity by making a simple API call
                var profile = await Task.Run(() => _kite.GetProfile());
                return true; // Simplified for now
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token validation failed");
                return false;
            }
        }

        public async Task<Dictionary<string, Quote>> GetQuotesWithRetryAsync(string[] instruments, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await GetQuotesAsync(instruments);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Quote fetch attempt {attempt} failed for {string.Join(", ", instruments)}");
                    
                    if (attempt == maxRetries)
                        throw;
                    
                    // Wait before retry (exponential backoff)
                    await Task.Delay(1000 * attempt);
                }
            }
            
            throw new Exception("Max retries exceeded for quote fetch");
        }

        public async Task<List<TradePosition>> GetPositionsAsync()
        {
            _logger.LogInformation("Fetching positions from Kite API.");
            try
            {
                PositionResponse positionResponse = await Task.Run(() => _kite.GetPositions());
                _logger.LogInformation($"Successfully fetched {positionResponse.Net.Count} positions.");
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching positions.");
                throw;
            }
        }

        public async Task<List<KiteConnect.Order>> GetOrdersAsync()
        {
            _logger.LogInformation("Fetching orders from Kite API.");
            try
            {
                return await Task.Run(() => _kite.GetOrders());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching orders.");
                throw;
            }
        }

        public async Task<List<KiteConnectApi.Models.Trading.SimulatedHistoricalData>> GetHistoricalDataAsync(string instrumentToken, DateTime from, DateTime to, string interval, bool continuous, int? oi = 0)
        {
            _logger.LogInformation($"Fetching historical data for {instrumentToken} from {from} to {to} with interval {interval}.");
            try
            {
                bool oiFlag = oi.HasValue && oi.Value == 1;
                var historicalData = await Task.Run(() => _kite.GetHistoricalData(instrumentToken, from, to, interval, continuous, oiFlag));
                _logger.LogInformation($"Successfully fetched {historicalData.Count} historical data points.");
                return historicalData.Select(h => new KiteConnectApi.Models.Trading.SimulatedHistoricalData
                {
                    TimeStamp = h.TimeStamp,
                    Open = h.Open,
                    High = h.High,
                    Low = h.Low,
                    Close = h.Close,
                    Volume = h.Volume
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching historical data for {instrumentToken}.");
                throw;
            }
        }

        public async Task<Dictionary<string, dynamic>> PlaceOrderAsync(string? exchange, string? tradingsymbol, string? transaction_type, int quantity, string? product, string? order_type, decimal? price = null, decimal? trigger_price = null, decimal? limit_price = null, int? disclosed_quantity = null, string? validity = "DAY", string? tag = null, string? positionId = null)
        {
            _logger.LogInformation($"Placing order: {transaction_type} {quantity} of {tradingsymbol} on {exchange} as {order_type} order.");
            
            // Validate required parameters as per Kite API v3
            if (string.IsNullOrEmpty(tradingsymbol))
                throw new ArgumentException("Trading symbol is required", nameof(tradingsymbol));
            if (string.IsNullOrEmpty(exchange))
                throw new ArgumentException("Exchange is required", nameof(exchange));
            if (string.IsNullOrEmpty(transaction_type))
                throw new ArgumentException("Transaction type is required", nameof(transaction_type));
            if (string.IsNullOrEmpty(product))
                throw new ArgumentException("Product type is required", nameof(product));
            if (string.IsNullOrEmpty(order_type))
                throw new ArgumentException("Order type is required", nameof(order_type));
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

            // Validate price parameters based on order type
            if (order_type == "LIMIT" && (!price.HasValue || price <= 0))
                throw new ArgumentException("Price is required for LIMIT orders", nameof(price));
            if (order_type == "SL" && (!trigger_price.HasValue || trigger_price <= 0))
                throw new ArgumentException("Trigger price is required for SL orders", nameof(trigger_price));

            try
            {
                // Use correct parameter mapping as per Kite API v3
                decimal? orderPrice = order_type == "LIMIT" ? price : null;
                if (order_type == "SL" && price.HasValue)
                    orderPrice = price; // For SL orders, price is the limit price after trigger

                return await Task.Run(() => _kite.PlaceOrder(
                    Exchange: exchange,
                    TradingSymbol: tradingsymbol,
                    TransactionType: transaction_type,
                    Quantity: quantity,
                    Price: orderPrice, // Correct price parameter usage
                    Product: product,
                    OrderType: order_type,
                    Validity: validity ?? "DAY",
                    DisclosedQuantity: disclosed_quantity,
                    TriggerPrice: trigger_price,
                    Tag: tag
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error placing order: {transaction_type} {quantity} of {tradingsymbol}. Error: {ex.Message}");
                throw;
            }
        }

        public async Task<Dictionary<string, dynamic>> ModifyOrderAsync(string orderId, string? exchange = null, string? tradingSymbol = null, int? quantity = null, string? orderType = null, decimal? price = null, decimal? triggerPrice = null, decimal? limitPrice = null, int? disclosedQuantity = null, string? validity = null)
        {
            _logger.LogInformation($"Modifying order {orderId}.");
            try
            {
                return await Task.Run(() => _kite.ModifyOrder(
                    OrderId: orderId,
                    Exchange: exchange,
                    TradingSymbol: tradingSymbol,
                    TransactionType: null, // Not typically modified
                    Quantity: quantity.ToString(),
                    Price: limitPrice, // Use limitPrice for Price parameter
                    Product: null, // Not typically modified
                    OrderType: orderType,
                    Validity: validity,
                    DisclosedQuantity: disclosedQuantity,
                    TriggerPrice: triggerPrice
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error modifying order {orderId}.");
                throw;
            }
        }
        public async Task<Dictionary<string, dynamic>> CancelOrderAsync(string orderId, string? variety = null)
        {
            _logger.LogInformation($"Cancelling order {orderId}.");
            try
            {
                return await Task.Run(() => _kite.CancelOrder(orderId, variety));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error cancelling order {orderId}.");
                throw;
            }
        }
        public async Task<List<Trade>> GetOrderTradesAsync(string orderId) { await Task.CompletedTask; return new List<Trade>(); }
        public async Task<List<KiteConnect.Order>> GetOrderHistoryAsync(string orderId) { await Task.CompletedTask; return new List<KiteConnect.Order>(); }
        public async Task<Dictionary<string, OHLC>> GetOHLCAsync(string[] instrumentTokens) { await Task.CompletedTask; return new Dictionary<string, OHLC>(); }
        public async Task UpdateOrderStatusAsync(string orderId, string status, double price, string? statusMessage) { await Task.CompletedTask; }
        public async Task CancelAndReplaceWithMarketOrder(string orderId, string tradingSymbol, int quantity, string transactionType) { await Task.CompletedTask; }

        public async Task<List<KiteConnect.Holding>> GetHoldingsAsync()
        {
            _logger.LogInformation("Fetching holdings from Kite API.");
            try
            {
                return await Task.Run(() => _kite.GetHoldings());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching holdings.");
                throw;
            }
        }
    }
}
