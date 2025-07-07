namespace KiteConnectApi.Models.Trading
{
    public class NiftyOptionStrategyConfig
    {
        public string? StrategyName { get; set; }
        public string? UnderlyingInstrument { get; set; }
        public string? Exchange { get; set; }
        public string? ProductType { get; set; }
        public int Quantity { get; set; }
        public int EntryTime { get; set; }
        public int ExitTime { get; set; }
        public decimal StopLossPercentage { get; set; }
        public decimal TargetPercentage { get; set; }
        public decimal TakeProfitPercentage { get; set; }
        public int MaxTradesPerDay { get; set; }
        public bool IsEnabled { get; set; }
        public int HedgeDistancePoints { get; set; }
        public string? OrderType { get; set; }
        public string? InstrumentPrefix { get; set; }
    }
}
