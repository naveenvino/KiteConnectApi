using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiteConnectApi.Models.Trading
{
    public class BrokerLevelSettings
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int StrategyId { get; set; }

        [ForeignKey("StrategyId")]
        public Strategy? Strategy { get; set; }

        public double? OverallStopLoss { get; set; } // Nullable if not set

        public double? OverallTarget { get; set; } // Nullable if not set

        // Trail SL
        public double? IncrementProfitBy { get; set; } // X
        public double? TrailSLBy { get; set; } // Y

        // Lock and Trail
        public double? LockProfitAt { get; set; }
        public double? MinimumProfitToLock { get; set; }
    }
}