using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Models.Trading
{
    public class OptionsTradePosition
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StrategyId { get; set; } = string.Empty;
        public string Signal { get; set; } = string.Empty; // S1, S2, S3, etc.
        public string TradingSymbol { get; set; } = string.Empty;
        public string InstrumentToken { get; set; } = string.Empty;
        public int Strike { get; set; }
        public string OptionType { get; set; } = string.Empty; // CE or PE
        public string TransactionType { get; set; } = string.Empty; // BUY or SELL
        public int Quantity { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal StopLossPrice { get; set; }
        public decimal TargetPrice { get; set; }
        public string Status { get; set; } = "OPEN"; // OPEN, CLOSED, CANCELLED
        public DateTime EntryTime { get; set; } = DateTime.Now;
        public DateTime? ExitTime { get; set; }
        public decimal PnL { get; set; }
        public decimal PnLPercentage { get; set; }
        public string? ExitReason { get; set; } // STOPLOSS, TARGET, MANUAL, EXPIRY
        public string? OrderId { get; set; }
        public string? ExitOrderId { get; set; }
        public DateTime ExpiryDate { get; set; }
        public bool IsHedge { get; set; } = false;
        public string? MainPositionId { get; set; } // Reference to main position if this is a hedge
        public decimal TrailingStopLoss { get; set; }
        public decimal MaxProfit { get; set; }
        public decimal MaxLoss { get; set; }
        public bool ProfitLocked { get; set; } = false;
        public decimal LockedProfitLevel { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}