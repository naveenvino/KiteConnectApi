using System;
using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Models.Trading
{
    public class Order
    {
        [Key]
        public string OrderId { get; set; } // Kite Connect Order ID
        public string TradingSymbol { get; set; }
        public string Exchange { get; set; }
        public string TransactionType { get; set; } // BUY or SELL
        public int Quantity { get; set; }
        public string Product { get; set; }
        public string OrderType { get; set; }
        public double Price { get; set; } // Trigger price or limit price
        public double AveragePrice { get; set; } // Average traded price
        public string Status { get; set; } // PENDING, COMPLETE, CANCELLED, REJECTED, etc.
        public DateTime PlacedTime { get; set; }
        public DateTime? UpdatedTime { get; set; }
        public string StatusMessage { get; set; } // Reason for rejection/cancellation
        public string PositionId { get; set; } // Link to TradePosition
    }
}
