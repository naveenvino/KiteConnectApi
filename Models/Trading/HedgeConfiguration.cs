using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Models.Trading
{
    public class HedgeConfiguration
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StrategyId { get; set; } = string.Empty;
        public string HedgeType { get; set; } = "POINTS"; // POINTS or PERCENTAGE
        public int HedgePoints { get; set; } = 300; // Points away from entry strike
        public decimal HedgePercentage { get; set; } = 30; // Percentage of entry premium
        public bool IsEnabled { get; set; } = true;
        public string HedgeTransactionType { get; set; } = "BUY"; // Usually BUY for protection
        public decimal HedgeRatio { get; set; } = 1.0m; // Ratio of hedge to main position
        public int MaxHedgePrice { get; set; } = 0; // Maximum price to pay for hedge (0 = no limit)
        public int MinHedgePrice { get; set; } = 0; // Minimum price to pay for hedge (0 = no limit)
        public bool AutoAdjustHedge { get; set; } = false; // Auto-adjust hedge as main position moves
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string? Notes { get; set; }
    }
}