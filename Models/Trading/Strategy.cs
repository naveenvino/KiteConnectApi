using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiteConnectApi.Models.Trading
{
    public class Strategy
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        // Navigation properties
        public ICollection<Leg>? Legs { get; set; }
        public ExecutionSettings? ExecutionSettings { get; set; }
        public BrokerLevelSettings? BrokerLevelSettings { get; set; } // Assuming one-to-one or one-to-many with a specific broker config
    }
}