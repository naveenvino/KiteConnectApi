namespace KiteConnectApi.Models.Trading
{
    public class TradingViewAlert
    {
        public int Strike { get; set; }
        public string? Type { get; set; } // "CE" or "PE"
        public string? Signal { get; set; }
        public string? Action { get; set; } // "Entry" or "Stoploss"
    }
}
