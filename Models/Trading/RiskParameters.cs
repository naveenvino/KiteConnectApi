namespace KiteConnectApi.Models.Trading
{
    public class RiskParameters
    {
        public decimal MaxDailyLossPercentage { get; set; } = 0.01m; // 1% of capital
        public decimal MaxPerTradeLossPercentage { get; set; } = 0.005m; // 0.5% of capital
        public int MaxOpenPositions { get; set; } = 5;
        public decimal TotalCapital { get; set; } = 100000.00m; // Example capital
    }
}
