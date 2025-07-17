using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiteConnectApi.Models.Trading
{
    /// <summary>
    /// Dedicated table for NIFTY 50 Index historical data
    /// Separate from options data for better organization
    /// </summary>
    [Table("NiftyIndexHistoricalData")]
    public class NiftyIndexHistoricalData
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Trading symbol (e.g., "NIFTY_INDEX")
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// Timestamp of the candle
        /// </summary>
        [Required]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Opening price
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Open { get; set; }

        /// <summary>
        /// Highest price
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal High { get; set; }

        /// <summary>
        /// Lowest price
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Low { get; set; }

        /// <summary>
        /// Closing price
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Close { get; set; }

        /// <summary>
        /// Volume (usually 0 for index data)
        /// </summary>
        public long Volume { get; set; }

        /// <summary>
        /// Time interval (e.g., "60minute", "day")
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Interval { get; set; } = string.Empty;

        /// <summary>
        /// Data source (e.g., "Kite Connect API")
        /// </summary>
        [StringLength(100)]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// When this record was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Index for efficient querying
        /// </summary>
        public static void ConfigureIndexes(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NiftyIndexHistoricalData>()
                .HasIndex(e => new { e.Symbol, e.Timestamp, e.Interval })
                .IsUnique();

            modelBuilder.Entity<NiftyIndexHistoricalData>()
                .HasIndex(e => e.Timestamp);
        }
    }
}