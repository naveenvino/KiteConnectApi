using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using KiteConnectApi.Models.Enums;

namespace KiteConnectApi.Models.Trading
{
    public class Leg
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int StrategyId { get; set; }

        [ForeignKey("StrategyId")]
        public Strategy? Strategy { get; set; }

        [Required]
        [StringLength(50)]
        public string? UnderlyingAsset { get; set; } // e.g., NIFTY, BANKNIFTY

        [Required]
        public DateTime ExpiryDate { get; set; }

        public double StrikePrice { get; set; }

        [Required]
        public OptionType OptionType { get; set; }

        [Required]
        public Position Position { get; set; } // Buy or Sell

        [Required]
        public int QuantityLots { get; set; }
    }
}