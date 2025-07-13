using KiteConnect;
using KiteConnectApi.Models.Trading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using KiteConnectApi.Models.Dto;

namespace KiteConnectApi.Services
{
    public interface IKiteConnectService
    {
        string GetLoginUrl();
        Task<User> GenerateSessionAsync(string requestToken);
        Task<Dictionary<string, dynamic>> PlaceOrderAsync(string? exchange, string? tradingsymbol, string? transaction_type, int quantity, string? product, string? order_type, decimal? price = null, decimal? trigger_price = null, decimal? limit_price = null, int? disclosed_quantity = null, string? validity = "DAY", string? tag = null, string? positionId = null);
        Task UpdateOrderStatusAsync(string orderId, string status, double averagePrice = 0, string? statusMessage = null);
        Task<Dictionary<string, dynamic>> ModifyOrderAsync(string order_id, string? exchange = null, string? tradingsymbol = null, int? quantity = null, string? order_type = null, decimal? price = null, decimal? trigger_price = null, decimal? limit_price = null, int? disclosed_quantity = null, string? validity = null);
        Task<Dictionary<string, dynamic>> CancelOrderAsync(string order_id, string? variety = "regular");
        Task<List<KiteConnect.Order>> GetOrdersAsync();
        Task<List<Trade>> GetOrderTradesAsync(string orderId);
        Task<List<InstrumentDto>> GetInstrumentsAsync(string? exchange = null);
        Task<Dictionary<string, Quote>> GetQuotesAsync(string[] instruments);
        Task<List<KiteConnectApi.Models.Trading.SimulatedHistoricalData>> GetHistoricalDataAsync(string instrumentToken, DateTime from, DateTime to, string interval, bool continuous = false, int? oi = null);
        Task<Dictionary<string, OHLC>> GetOHLCAsync(string[] instruments);
        Task<List<KiteConnect.Order>> GetOrderHistoryAsync(string orderId);
        Task CancelAndReplaceWithMarketOrder(string orderId, string tradingSymbol, int quantity, string transactionType);
        Task<List<TradePosition>> GetPositionsAsync();
        Task<List<KiteConnect.Holding>> GetHoldingsAsync();
    }
}
