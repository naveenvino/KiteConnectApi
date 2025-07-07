namespace KiteConnectApi.Models.Trading
{
    public class RiskParameters
    {
        public int MaxOpenPositions { get; set; }
        // ADDED: These properties were missing.
        public decimal MaxExposure { get; set; }
        public decimal MaxLoss { get; set; }
        public decimal MaxProfit { get; set; }
    }
}