using System;

namespace KiteConnectApi.Models.Trading
{
    public class SimulatedHistoricalData
    {
        public DateTime TimeStamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public ulong Volume { get; set; }
        public string? TradingSymbol { get; set; }
    }
}
