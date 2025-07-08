using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiteConnectApi.Models.Trading
{
    public class ScreenerCriteria
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string Name { get; set; }
        public required string Exchange { get; set; } // e.g., "NSE", "BSE"
        public required string InstrumentType { get; set; } // e.g., "EQ", "FUT", "OPT"
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public long? MinVolume { get; set; }
        public long? MaxVolume { get; set; }
        [NotMapped]
        public Dictionary<string, string> TechnicalIndicators { get; set; } = new Dictionary<string, string>(); // e.g., {"RSI": "<30"}, {"SMA": "cross_above_price"}
        public List<string> WatchlistSymbols { get; set; } = new List<string>(); // Optional: to screen only specific symbols
    }
}
