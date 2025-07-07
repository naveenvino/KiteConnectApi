namespace KiteConnectApi.Models.Trading
{
    public class TradingViewAlert
    {
        // ADDED: These properties were missing.
        public string? Symbol { get; set; }
        public string? Action { get; set; } // e.g., "buy", "sell"
    }
}
