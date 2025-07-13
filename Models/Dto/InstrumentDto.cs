namespace KiteConnectApi.Models.Dto
{
    public class InstrumentDto
    {
        public uint InstrumentToken { get; set; }
        public string? Exchange { get; set; }
        public string? TradingSymbol { get; set; }
        public string? Name { get; set; }
        public DateTime? Expiry { get; set; }
        public uint Strike { get; set; }
        public string? InstrumentType { get; set; }
    }
}