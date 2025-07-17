using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Models.Trading
{
    public class RiskManagementRule
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StrategyId { get; set; } = string.Empty;
        public string RuleType { get; set; } = string.Empty; // PROFIT_LOCK, TRAIL_SL, MOVE_SL_TO_ENTRY, EXIT_REENTER
        public bool IsEnabled { get; set; } = true;
        public decimal TriggerPercentage { get; set; } = 0;
        public decimal TriggerAmount { get; set; } = 0;
        public decimal ActionPercentage { get; set; } = 0;
        public decimal ActionAmount { get; set; } = 0;
        public string TriggerCondition { get; set; } = "GREATER_THAN"; // GREATER_THAN, LESS_THAN, EQUAL_TO
        public string ActionType { get; set; } = string.Empty; // MODIFY_SL, SQUARE_OFF, REENTER
        public int Priority { get; set; } = 1;
        public bool IsRecurring { get; set; } = false; // Can trigger multiple times
        public DateTime? LastTriggered { get; set; }
        public int MaxTriggers { get; set; } = 1;
        public int TriggerCount { get; set; } = 0;
        public string? Notes { get; set; }
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}