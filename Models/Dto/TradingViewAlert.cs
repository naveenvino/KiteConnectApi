namespace KiteConnectApi.Models.Dto
{
    public class TradingViewAlert
    {
        public string? StrategyName { get; set; }
        public int Strike { get; set; }
        public string? Type { get; set; } // CE or PE
        public string? Signal { get; set; } // S1, S2, S3, S4, S5, S6, S7, S8
        public string? Action { get; set; } // Entry or Stoploss
        public string? Index { get; set; } // Nifty or BankNifty
        public DateTime? Timestamp { get; set; } = DateTime.Now;
        public string? Source { get; set; } = "TradingView";
    }
}
