namespace KiteConnectApi.Models.Trading
{
    public class NiftyOptionStrategyConfig
    {
        public int Quantity { get; set; }
        public int HedgeDistancePoints { get; set; }
        public string InstrumentPrefix { get; set; }
        public string Exchange { get; set; }
        public string ProductType { get; set; }
        public string OrderType { get; set; }
    }
}