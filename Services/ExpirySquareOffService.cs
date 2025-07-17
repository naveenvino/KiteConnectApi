using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KiteConnectApi.Services
{
    public class ExpirySquareOffService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExpirySquareOffService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5); // Check every 5 minutes

        public ExpirySquareOffService(
            IServiceProvider serviceProvider,
            ILogger<ExpirySquareOffService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExpirySquareOffService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpirySquareOffAsync();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ExpirySquareOffService");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("ExpirySquareOffService stopped");
        }

        private async Task ProcessExpirySquareOffAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var optionsTradeService = scope.ServiceProvider.GetRequiredService<OptionsTradeService>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var today = DateTime.Now.Date;
            var currentTime = DateTime.Now;

            // Check if today is Thursday and it's after market hours or near expiry
            if (currentTime.DayOfWeek == DayOfWeek.Thursday)
            {
                // Get all positions expiring today
                var expiringPositions = await context.OptionsTradePositions
                    .Where(p => p.Status == "OPEN" && p.ExpiryDate.Date == today)
                    .Include(p => p.MainPositionId != null ? 
                        context.OptionsTradePositions.Where(h => h.Id == p.MainPositionId).FirstOrDefault() : 
                        null)
                    .ToListAsync();

                if (expiringPositions.Any())
                {
                    _logger.LogInformation("Found {Count} positions expiring today", expiringPositions.Count);

                    // Get strategy configurations for square-off time
                    var strategyIds = expiringPositions.Select(p => p.StrategyId).Distinct().ToList();
                    var strategies = await context.NiftyOptionStrategyConfigs
                        .Where(s => strategyIds.Contains(s.Id))
                        .ToListAsync();

                    foreach (var strategy in strategies)
                    {
                        if (!strategy.AutoSquareOffOnExpiry)
                        {
                            continue;
                        }

                        // Check if it's time to square off (default 3:30 PM = 330 minutes from midnight)
                        var squareOffTime = TimeSpan.FromMinutes(strategy.ExpirySquareOffTimeMinutes);
                        
                        if (currentTime.TimeOfDay >= squareOffTime)
                        {
                            var strategyPositions = expiringPositions
                                .Where(p => p.StrategyId == strategy.Id)
                                .ToList();

                            await ProcessStrategyExpirySquareOffAsync(
                                strategyPositions, 
                                strategy, 
                                optionsTradeService, 
                                notificationService);
                        }
                    }
                }
            }

            // Also check for any positions that are past their expiry date
            await ProcessOverduePositionsAsync(context, optionsTradeService);
        }

        private async Task ProcessStrategyExpirySquareOffAsync(
            List<OptionsTradePosition> positions,
            NiftyOptionStrategyConfig strategy,
            OptionsTradeService optionsTradeService,
            INotificationService notificationService)
        {
            var squaredOffCount = 0;
            var totalPnL = 0m;

            foreach (var position in positions)
            {
                try
                {
                    var success = await optionsTradeService.ClosePositionAsync(position, "EXPIRY_SQUARE_OFF");
                    if (success)
                    {
                        squaredOffCount++;
                        totalPnL += position.PnL;
                        _logger.LogInformation("Squared off expiring position: {TradingSymbol} | P&L: {PnL}", 
                            position.TradingSymbol, position.PnL);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error squaring off position {PositionId}", position.Id);
                }
            }

            if (squaredOffCount > 0)
            {
                var message = $"Strategy {strategy.StrategyName}: Squared off {squaredOffCount} expiring positions. Total P&L: {totalPnL:C}";
                
                await notificationService.SendNotificationAsync(
                    "Expiry Square Off", 
                    message);

                _logger.LogInformation("Completed expiry square off for strategy {StrategyName}: {Count} positions, P&L: {PnL}", 
                    strategy.StrategyName, squaredOffCount, totalPnL);
            }
        }

        private async Task ProcessOverduePositionsAsync(
            ApplicationDbContext context,
            OptionsTradeService optionsTradeService)
        {
            var overduePositions = await context.OptionsTradePositions
                .Where(p => p.Status == "OPEN" && p.ExpiryDate.Date < DateTime.Now.Date)
                .ToListAsync();

            if (overduePositions.Any())
            {
                _logger.LogWarning("Found {Count} overdue positions", overduePositions.Count);

                foreach (var position in overduePositions)
                {
                    try
                    {
                        await optionsTradeService.ClosePositionAsync(position, "OVERDUE_EXPIRY");
                        _logger.LogInformation("Closed overdue position: {TradingSymbol}", position.TradingSymbol);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error closing overdue position {PositionId}", position.Id);
                    }
                }
            }
        }

        public async Task<ExpirySquareOffStatus> GetExpirySquareOffStatusAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var today = DateTime.Now.Date;
            var currentTime = DateTime.Now;

            var expiringPositions = await context.OptionsTradePositions
                .Where(p => p.Status == "OPEN" && p.ExpiryDate.Date == today)
                .ToListAsync();

            var strategies = await context.NiftyOptionStrategyConfigs
                .Where(s => expiringPositions.Select(p => p.StrategyId).Contains(s.Id))
                .ToListAsync();

            var strategyStatuses = new List<StrategyExpiryStatus>();

            foreach (var strategy in strategies)
            {
                var strategyPositions = expiringPositions.Where(p => p.StrategyId == strategy.Id).ToList();
                var squareOffTime = TimeSpan.FromMinutes(strategy.ExpirySquareOffTimeMinutes);
                var isSquareOffTime = currentTime.TimeOfDay >= squareOffTime;

                strategyStatuses.Add(new StrategyExpiryStatus
                {
                    StrategyId = strategy.Id,
                    StrategyName = strategy.StrategyName ?? "Unknown",
                    PositionCount = strategyPositions.Count,
                    SquareOffTime = squareOffTime,
                    IsSquareOffTime = isSquareOffTime,
                    AutoSquareOffEnabled = strategy.AutoSquareOffOnExpiry,
                    TotalPnL = strategyPositions.Sum(p => p.PnL)
                });
            }

            return new ExpirySquareOffStatus
            {
                IsExpiryDay = currentTime.DayOfWeek == DayOfWeek.Thursday,
                TotalExpiringPositions = expiringPositions.Count,
                StrategyStatuses = strategyStatuses,
                LastChecked = currentTime
            };
        }

        public async Task<bool> ForceSquareOffAllExpiringPositionsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var optionsTradeService = scope.ServiceProvider.GetRequiredService<OptionsTradeService>();

            var today = DateTime.Now.Date;
            var expiringPositions = await context.OptionsTradePositions
                .Where(p => p.Status == "OPEN" && p.ExpiryDate.Date == today)
                .ToListAsync();

            if (!expiringPositions.Any())
            {
                return true;
            }

            var successCount = 0;
            foreach (var position in expiringPositions)
            {
                try
                {
                    var success = await optionsTradeService.ClosePositionAsync(position, "FORCE_EXPIRY_SQUARE_OFF");
                    if (success) successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error force closing expiring position {PositionId}", position.Id);
                }
            }

            _logger.LogInformation("Force squared off {SuccessCount} out of {TotalCount} expiring positions", 
                successCount, expiringPositions.Count);

            return successCount == expiringPositions.Count;
        }
    }

    public class ExpirySquareOffStatus
    {
        public bool IsExpiryDay { get; set; }
        public int TotalExpiringPositions { get; set; }
        public List<StrategyExpiryStatus> StrategyStatuses { get; set; } = new();
        public DateTime LastChecked { get; set; }
    }

    public class StrategyExpiryStatus
    {
        public string StrategyId { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
        public int PositionCount { get; set; }
        public TimeSpan SquareOffTime { get; set; }
        public bool IsSquareOffTime { get; set; }
        public bool AutoSquareOffEnabled { get; set; }
        public decimal TotalPnL { get; set; }
    }
}