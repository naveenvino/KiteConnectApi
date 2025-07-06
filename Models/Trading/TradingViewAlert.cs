namespace KiteConnectApi.Models.Trading
{
    /// <summary>
    /// Represents an alert received from TradingView.
    /// </summary>
    public class TradingViewAlert
    {
        /// <summary>
        /// The strike price for the option trade.
        /// </summary>
        public int Strike { get; set; }

        /// <summary>
        /// The option type, either "CE" (Call Option) or "PE" (Put Option).
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// An optional signal to indicate the action, e.g., "entry" or "stop_loss".
        /// </summary>
        public string Signal { get; set; }

        /// <summary>
        /// The unique identifier for an open position, used for stop-loss alerts.
        /// </summary>
        public string PositionId { get; set; }

        /// <summary>
        /// The action associated with the alert, e.g., "Entry" or "Stoploss".
        /// </summary>
        public string Action { get; set; }
    }
}
