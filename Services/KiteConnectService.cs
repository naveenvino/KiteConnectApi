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
                return _kite.GetQuote(instruments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching quotes for {string.Join(", ", instruments)}.");
                throw;
            }
        }

        public async Task<List<TradePosition>> GetPositionsAsync()
        {
            _logger.LogInformation("Fetching positions from Kite API.");
            try
            {
                PositionResponse positionResponse = _kite.GetPositions();
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
                return _kite.GetOrders();
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
                var historicalData = _kite.GetHistoricalData(instrumentToken, from, to, interval, continuous, oiFlag);
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
            try
            {
                return _kite.PlaceOrder(
                    Exchange: exchange,
                    TradingSymbol: tradingsymbol,
                    TransactionType: transaction_type,
                    Quantity: quantity,
                    Price: limit_price, // Use limit_price for Price parameter
                    Product: product,
                    OrderType: order_type,
                    Validity: validity,
                    DisclosedQuantity: disclosed_quantity,
                    TriggerPrice: trigger_price,
                    Tag: tag
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error placing order: {transaction_type} {quantity} of {tradingsymbol}.");
                throw;
            }
        }

        public async Task<Dictionary<string, dynamic>> ModifyOrderAsync(string orderId, string? exchange = null, string? tradingSymbol = null, int? quantity = null, string? orderType = null, decimal? price = null, decimal? triggerPrice = null, decimal? limitPrice = null, int? disclosedQuantity = null, string? validity = null)
        {
            _logger.LogInformation($"Modifying order {orderId}.");
            try
            {
                return _kite.ModifyOrder(
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
                );
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
                return _kite.CancelOrder(orderId, variety);
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
                return _kite.GetHoldings();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching holdings.");
                throw;
            }
        }
    }
}
