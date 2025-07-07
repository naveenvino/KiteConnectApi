using KiteConnectApi.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class ExpiryDayMonitor : BackgroundService
    {
        private readonly ILogger<ExpiryDayMonitor> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public ExpiryDayMonitor(ILogger<ExpiryDayMonitor> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Check every hour
                _logger.LogInformation("Expiry Day Monitor running at: {time}", DateTimeOffset.Now);

                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var strategyService = scope.ServiceProvider.GetRequiredService<StrategyService>();
                        var positionRepository = scope.ServiceProvider.GetRequiredService<IPositionRepository>();

                        var expiry = strategyService.GetNextWeeklyExpiry();
                        // Check if it's expiry day and after market hours (e.g., 4 PM)
                        if (DateTime.Today == expiry.Date && DateTime.Now.Hour >= 16)
                        {
                            var openPositions = await positionRepository.GetOpenPositionsAsync();
                            if (openPositions.Any())
                            {
                                _logger.LogWarning("EXPIRY DAY: Closing all open positions.");
                                // --- FIX: Call the correct method to exit all positions ---
                                await strategyService.ExitAllPositionsAsync();
                                // --- END OF FIX ---
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the Expiry Day Monitor.");
                }
            }
        }
    }
}
