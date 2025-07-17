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
        
        // New enhanced options trading models
        public DbSet<OptionsTradePosition> OptionsTradePositions { get; set; }
        public DbSet<PendingAlert> PendingAlerts { get; set; }
        public DbSet<HedgeConfiguration> HedgeConfigurations { get; set; }
        public DbSet<RiskManagementRule> RiskManagementRules { get; set; }
        
        // Historical data for backtesting
        public DbSet<OptionsHistoricalData> OptionsHistoricalData { get; set; }
        
        // NIFTY Index historical data (separate from options)
        public DbSet<NiftyIndexHistoricalData> NiftyIndexHistoricalData { get; set; }
        
        // API Trading Dashboard
        public DbSet<ApiTradeLog> ApiTradeLog { get; set; }

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

            // Configure relationships for new options trading models
            modelBuilder.Entity<OptionsTradePosition>()
                .HasIndex(p => new { p.StrategyId, p.Signal, p.Status })
                .HasDatabaseName("IX_OptionsTradePosition_Strategy_Signal_Status");

            modelBuilder.Entity<OptionsTradePosition>()
                .HasIndex(p => p.ExpiryDate)
                .HasDatabaseName("IX_OptionsTradePosition_ExpiryDate");

            modelBuilder.Entity<PendingAlert>()
                .HasIndex(p => new { p.StrategyId, p.Status })
                .HasDatabaseName("IX_PendingAlert_Strategy_Status");

            modelBuilder.Entity<PendingAlert>()
                .HasIndex(p => p.ExpiryTime)
                .HasDatabaseName("IX_PendingAlert_ExpiryTime");

            modelBuilder.Entity<HedgeConfiguration>()
                .HasIndex(h => h.StrategyId)
                .HasDatabaseName("IX_HedgeConfiguration_StrategyId");

            modelBuilder.Entity<RiskManagementRule>()
                .HasIndex(r => new { r.StrategyId, r.IsEnabled })
                .HasDatabaseName("IX_RiskManagementRule_Strategy_Enabled");

            // Configure NIFTY Index historical data indexes
            modelBuilder.Entity<NiftyIndexHistoricalData>()
                .HasIndex(e => new { e.Symbol, e.Timestamp, e.Interval })
                .IsUnique();

            modelBuilder.Entity<NiftyIndexHistoricalData>()
                .HasIndex(e => e.Timestamp);

            // Configure decimal precision for financial fields
            modelBuilder.Entity<OptionsTradePosition>()
                .Property(p => p.EntryPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<OptionsTradePosition>()
                .Property(p => p.CurrentPrice)
                .HasPrecision(18, 4);

            modelBuilder.Entity<OptionsTradePosition>()
                .Property(p => p.PnL)
                .HasPrecision(18, 4);

            // Configure indexes for OptionsHistoricalData
            modelBuilder.Entity<OptionsHistoricalData>()
                .HasIndex(d => new { d.TradingSymbol, d.Timestamp })
                .HasDatabaseName("IX_OptionsHistoricalData_Symbol_Timestamp");

            modelBuilder.Entity<OptionsHistoricalData>()
                .HasIndex(d => new { d.Underlying, d.Strike, d.OptionType, d.ExpiryDate })
                .HasDatabaseName("IX_OptionsHistoricalData_Underlying_Strike_Type_Expiry");

            modelBuilder.Entity<OptionsHistoricalData>()
                .HasIndex(d => d.Timestamp)
                .HasDatabaseName("IX_OptionsHistoricalData_Timestamp");

        }
    }
}
