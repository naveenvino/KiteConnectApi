namespace KiteConnectApi.Models.Dto
{
    public class BacktestResultDto
    {
        public required string Symbol { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public required string Interval { get; set; }
        public decimal TotalProfitLoss { get; set; }
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal WinRate { get; set; }
        public decimal MaxDrawdown { get; set; }
        public List<SimulatedTrade> SimulatedTrades { get; set; } = new List<SimulatedTrade>();
    }

    public class SimulatedTrade
    {
        public required string OrderId { get; set; }
        public required string TradingSymbol { get; set; }
        public required string TransactionType { get; set; }
        public int Quantity { get; set; }
        public decimal EntryPrice { get; set; }
        public DateTime EntryTime { get; set; }
        public decimal ExitPrice { get; set; }
        public DateTime ExitTime { get; set; }
        public decimal ProfitLoss { get; set; }
        public required string Status { get; set; } // e.g., "Open", "Closed"
    }
}
