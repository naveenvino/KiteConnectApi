using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KiteConnectApi.Models.Enums;

namespace KiteConnectApi.Models.Trading
{
    public class ExecutionSettings
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int StrategyId { get; set; }

        [ForeignKey("StrategyId")]
        public Strategy? Strategy { get; set; }

        [Required]
        public TimeSpan EntryTime { get; set; }

        [Required]
        public TimeSpan ExitTime { get; set; }

        [Required]
        public ProductType ProductType { get; set; }

        [Required]
        public OrderType EntryOrderType { get; set; }

        [Required]
        public OrderType ExitOrderType { get; set; }

        public double? LimitBuffer { get; set; } // Nullable for MARKET orders

        [Required]
        public TargetSLRefPrice TargetSLRefPrice { get; set; }

        public int QuantityMultiplier { get; set; }

        public int DelayEntryBySeconds { get; set; }

        // Storing trading days as a comma-separated string or bitmask could be options.
        // For simplicity, let's use a string for now, e.g., "Monday,Tuesday,Friday"
        public string? TradingDays { get; set; }

        public bool AutoSquareoff { get; set; }
    }
}