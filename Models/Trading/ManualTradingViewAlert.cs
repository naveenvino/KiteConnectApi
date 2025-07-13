using System;
using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Models.Trading
{
    public class ManualTradingViewAlert
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? StrategyName { get; set; }
        public int Strike { get; set; }
        public string? Type { get; set; }
        public string? Signal { get; set; }
        public string? Action { get; set; }
        public DateTime ReceivedTime { get; set; }
        public bool IsExecuted { get; set; }
    }
}
