namespace KiteConnectApi.Models.Dto
{
    public class PositionPnlDto
    {
        public decimal TotalRealizedPnl { get; set; }
        public decimal TotalUnrealizedPnl { get; set; }
        public decimal OverallPnl { get; set; }
    }
}