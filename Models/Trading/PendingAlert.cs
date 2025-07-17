using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Models.Trading
{
    public class PendingAlert
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StrategyId { get; set; } = string.Empty;
        public string AlertJson { get; set; } = string.Empty; // Serialized TradingViewAlert
        public DateTime ReceivedTime { get; set; } = DateTime.Now;
        public string Status { get; set; } = "PENDING"; // PENDING, EXECUTED, CANCELLED, EXPIRED
        public string? ExecutedBy { get; set; }
        public DateTime? ExecutedTime { get; set; }
        public string? ExecutionResult { get; set; }
        public string? ErrorMessage { get; set; }
        public int Strike { get; set; }
        public string OptionType { get; set; } = string.Empty; // CE or PE
        public string Signal { get; set; } = string.Empty; // S1, S2, S3, etc.
        public string Action { get; set; } = string.Empty; // Entry or Stoploss
        public string Index { get; set; } = string.Empty; // Nifty or BankNifty
        public DateTime ExpiryTime { get; set; } = DateTime.Now.AddMinutes(30); // Auto-expire after 30 minutes
        public int Priority { get; set; } = 1; // Higher number = higher priority
        public string? Notes { get; set; }
    }
}