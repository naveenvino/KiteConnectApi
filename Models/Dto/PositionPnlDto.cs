namespace KiteConnectApi.Models.Dto
{
    public class PositionPnlDto
    {
        public string? PositionId { get; set; }
        public double CurrentPnl { get; set; }
        public double NetPremium { get; set; }
        public double CurrentNetPremium { get; set; }
    }
}
