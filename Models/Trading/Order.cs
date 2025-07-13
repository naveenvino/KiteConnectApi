using System;
using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Models.Trading
{
    public class Order
    {
        [Key]
        public int Id { get; set; } // This will be the primary key

        public string? OrderId { get; set; }
        public string? TradingSymbol { get; set; }
        public string? Exchange { get; set; }
        public string? TransactionType { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TriggerPrice { get; set; }
        public decimal? LimitPrice { get; set; }
        public string? Product { get; set; }
        public string? OrderType { get; set; }
        public string? Validity { get; set; }
        public string? Status { get; set; }
        public string? PositionId { get; set; }
        public DateTime OrderTimestamp { get; set; }
    }
}
