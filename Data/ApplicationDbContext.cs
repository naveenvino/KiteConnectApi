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
        public DbSet<NiftyOptionStrategyConfig> NiftyOptionStrategyConfigs { get; set; }
        public DbSet<ManualTradingViewAlert> ManualTradingViewAlerts { get; set; }

        public DbSet<Strategy> Strategies { get; set; }
        public DbSet<Leg> Legs { get; set; }
        public DbSet<ExecutionSettings> ExecutionSettings { get; set; }
        public DbSet<BrokerLevelSettings> BrokerLevelSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StrategyConfig>().OwnsOne(s => s.RiskParameters);

            modelBuilder.Entity<Strategy>()
                .HasMany(s => s.Legs)
                .WithOne(l => l.Strategy)
                .HasForeignKey(l => l.StrategyId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Strategy>()
                .HasOne(s => s.ExecutionSettings)
                .WithOne(es => es.Strategy)
                .HasForeignKey<ExecutionSettings>(es => es.StrategyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Assuming BrokerLevelSettings is linked to a Strategy for now.
            // If it's truly global, this relationship might need to be re-evaluated.
            modelBuilder.Entity<Strategy>()
                .HasOne(s => s.BrokerLevelSettings)
                .WithOne(bls => bls.Strategy)
                .HasForeignKey<BrokerLevelSettings>(bls => bls.StrategyId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
