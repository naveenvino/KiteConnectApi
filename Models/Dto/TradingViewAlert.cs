namespace KiteConnectApi.Models.Dto
{
    public class TradingViewAlert
    {
        public string? StrategyName { get; set; }
        public int Strike { get; set; }
        public string? Type { get; set; } // CE or PE
        public string? Signal { get; set; } // S3
        public string? Action { get; set; } // Entry or Stoploss
    }
}
