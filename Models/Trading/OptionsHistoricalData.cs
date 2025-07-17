using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiteConnectApi.Models.Trading
{
    [Table("OptionsHistoricalData")]
    public class OptionsHistoricalData
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [MaxLength(50)]
        public string TradingSymbol { get; set; } = string.Empty;
        
        [Required]
        public DateTime Timestamp { get; set; }
        
        [Required]
        [MaxLength(10)]
        public string Exchange { get; set; } = "NFO";
        
        [Required]
        [MaxLength(10)]
        public string Underlying { get; set; } = "NIFTY";
        
        [Required]
        public int Strike { get; set; }
        
        [Required]
        [MaxLength(2)]
        public string OptionType { get; set; } = string.Empty; // CE or PE
        
        [Required]
        public DateTime ExpiryDate { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Open { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal High { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Low { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Close { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal LastPrice { get; set; }
        
        [Required]
        public long Volume { get; set; }
        
        [Required]
        public long OpenInterest { get; set; }
        
        // Greeks data (calculated or fetched)
        [Column(TypeName = "decimal(8,4)")]
        public decimal? Delta { get; set; }
        
        [Column(TypeName = "decimal(8,4)")]
        public decimal? Gamma { get; set; }
        
        [Column(TypeName = "decimal(8,4)")]
        public decimal? Theta { get; set; }
        
        [Column(TypeName = "decimal(8,4)")]
        public decimal? Vega { get; set; }
        
        [Column(TypeName = "decimal(8,4)")]
        public decimal? ImpliedVolatility { get; set; }
        
        // Pricing data
        [Column(TypeName = "decimal(18,2)")]
        public decimal? BidPrice { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AskPrice { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? BidAskSpread { get; set; }
        
        // Metadata
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [MaxLength(20)]
        public string? DataSource { get; set; } = "KiteConnect";
        
        [MaxLength(20)]
        public string? Interval { get; set; } = "1minute"; // 1minute, 5minute, 15minute, day
        
        // Composite index for fast queries
        [NotMapped]
        public string CompositeKey => $"{TradingSymbol}_{Timestamp:yyyyMMddHHmm}";
    }
}