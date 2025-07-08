using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace KiteConnectApi.Models.Trading
{
    public class StrategyConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string Name { get; set; }
        public bool IsActive { get; set; }
        public decimal AllocatedCapital { get; set; }
        public required string StrategyType { get; set; } // e.g., "NiftyOptionStrategy", "RSIStrategy"
        [NotMapped]
        public Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        public required RiskParameters RiskParameters { get; set; } // Reference to existing RiskParameters
    }
}
