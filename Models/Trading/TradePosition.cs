using System;
using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Models.Trading
{
    public class TradePosition
    {
        [Key]
        public int Id { get; set; }
        public string? PositionId { get; set; }
        public string? TradingSymbol { get; set; }
        public int Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal PnL { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Status { get; set; } = "";
        public string? Product { get; set; }
        public string? Exchange { get; set; }
        public string? Signal { get; set; }

    }
}
