using Microsoft.EntityFrameworkCore;
using KiteConnectApi.Models.Trading;

namespace KiteConnectApi.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<TradePosition> TradePositions { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<StrategyConfig> StrategyConfigs { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }
        public DbSet<ScreenerCriteria> ScreenerCriterias { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StrategyConfig>().OwnsOne(s => s.RiskParameters);
            // Configure your model here if needed
            // For example, to set primary keys or relationships
        }
    }
}
