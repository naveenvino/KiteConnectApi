// --- Models/Trading/TradePosition.cs ---
// This model was missing the Status and PositionId properties.
using System;
using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Models.Trading
{
    public class TradePosition
    {
        
        [Key]
        public string PositionId { get; set; } = "";
        public int Id { get; set; }
        public string? TradingSymbol { get; set; }
        public int Quantity { get; set; }
        public decimal AveragePrice { get; set; }
        public decimal PnL { get; set; }
        public DateTime LastUpdated { get; set; }

        // ADDED: Status property to track the position's state.
        public string Status { get; set; } = "";

        // ADDED: PositionId to link orders to a specific position.
        
    }
}
