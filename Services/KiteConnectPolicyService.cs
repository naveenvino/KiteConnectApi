using KiteConnect;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using KiteConnectApi.Models.Trading;

namespace KiteConnectApi.Services
{
    public class KiteConnectPolicyService : IKiteConnectService
    {
        private readonly IKiteConnectService _innerKiteConnectService;
        private readonly ILogger<KiteConnectPolicyService> _logger;
        private readonly ResiliencePipeline _pipeline;

        public KiteConnectPolicyService(IKiteConnectService innerKiteConnectService, ILogger<KiteConnectPolicyService> logger)
        {
            _innerKiteConnectService = innerKiteConnectService;
            _logger = logger;

            _pipeline = new ResiliencePipelineBuilder()
                .AddRetry(new RetryStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    OnRetry = args =>
                    {
                        _logger.LogWarning(args.Outcome.Exception, "Retry {RetryCount} for KiteConnectService call. Waiting {TimeSpan} before next retry.", args.AttemptNumber, args.RetryDelay);
                        return default;
                    }
                })
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                    FailureRatio = 0.5,
                    MinimumThroughput = 5,
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    BreakDuration = TimeSpan.FromMinutes(1),
                    OnOpened = args =>
                    {
                        _logger.LogError(args.Outcome.Exception, "Circuit breaker opened for KiteConnectService for {BreakDuration} due to {ExceptionType}.", args.BreakDuration, args.Outcome.Exception?.GetType().Name);
                        return default;
                    },
                    OnClosed = args =>
                    {
                        _logger.LogInformation("Circuit breaker reset for KiteConnectService.");
                        return default;
                    },
                    OnHalfOpened = args =>
                    {
                        _logger.LogInformation("Circuit breaker half-opened for KiteConnectService.");
                        return default;
                    }
                })
                .Build();
        }

        private async Task<T> ExecutePolicyAsync<T>(Func<Task<T>> action)
        {
            return await _pipeline.ExecuteAsync(async token => await action());
        }

        // Implement IKiteConnectService methods, wrapping calls with policies
        public Task<Dictionary<string, dynamic>> PlaceOrderAsync(string? exchange, string? tradingsymbol, string? transaction_type, int quantity, string? product, string? order_type, decimal? price = null, decimal? trigger_price = null, int? disclosed_quantity = null, string? validity = "DAY", string? tag = null, string? positionId = null)
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.PlaceOrderAsync(exchange, tradingsymbol, transaction_type, quantity, product, order_type, price, trigger_price, disclosed_quantity, validity, tag, positionId));
        }

        public Task<List<KiteConnect.Order>> GetOrdersAsync()
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.GetOrdersAsync());
        }

        public Task<List<TradePosition>> GetPositionsAsync()
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.GetPositionsAsync());
        }

        

        public Task<List<Trade>> GetOrderTradesAsync(string orderId)
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.GetOrderTradesAsync(orderId));
        }

        public Task UpdateOrderStatusAsync(string orderId, string status, double averagePrice = 0, string? statusMessage = null)
        {
            return _pipeline.ExecuteAsync(async token => await _innerKiteConnectService.UpdateOrderStatusAsync(orderId, status, averagePrice, statusMessage)).AsTask();
        }

        public Task<Dictionary<string, dynamic>> CancelOrderAsync(string order_id, string? variety = "regular")
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.CancelOrderAsync(order_id, variety));
        }

        public Task<List<Instrument>> GetInstrumentsAsync(string? exchange = null)
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.GetInstrumentsAsync(exchange));
        }

        public Task<Dictionary<string, Quote>> GetQuotesAsync(string[] instruments)
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.GetQuotesAsync(instruments));
        }

        public Task<List<Historical>> GetHistoricalDataAsync(string instrumentToken, DateTime from, DateTime to, string interval, bool continuous = false, int? oi = null)
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.GetHistoricalDataAsync(instrumentToken, from, to, interval, continuous, oi));
        }

        public string GetLoginUrl()
        {
            return _innerKiteConnectService.GetLoginUrl();
        }

        public Task<User> GenerateSessionAsync(string requestToken)
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.GenerateSessionAsync(requestToken));
        }

        public Task<Dictionary<string, dynamic>> ModifyOrderAsync(string order_id, string? exchange = null, string? tradingsymbol = null, int? quantity = null, string? order_type = null, decimal? price = null, decimal? trigger_price = null, int? disclosed_quantity = null, string? validity = null)
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.ModifyOrderAsync(order_id, exchange, tradingsymbol, quantity, order_type, price, trigger_price, disclosed_quantity, validity));
        }

        public Task<List<KiteConnect.Order>> GetOrderHistoryAsync(string orderId)
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.GetOrderHistoryAsync(orderId));
        }

        public Task CancelAndReplaceWithMarketOrder(string orderId, string tradingSymbol, int quantity, string transactionType)
        {
            return _pipeline.ExecuteAsync(async token => await _innerKiteConnectService.CancelAndReplaceWithMarketOrder(orderId, tradingSymbol, quantity, transactionType)).AsTask();
        }

        public Task<Dictionary<string, OHLC>> GetOHLCAsync(string[] instruments)
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.GetOHLCAsync(instruments));
        }

        public Task<List<KiteConnect.Holding>> GetHoldingsAsync()
        {
            return ExecutePolicyAsync(() => _innerKiteConnectService.GetHoldingsAsync());
        }
    }
}
