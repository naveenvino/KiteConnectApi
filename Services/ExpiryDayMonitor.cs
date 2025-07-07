using KiteConnectApi.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class ExpiryDayMonitor : BackgroundService
    {
        private readonly ILogger<ExpiryDayMonitor> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ExpiryDayMonitor(ILogger<ExpiryDayMonitor> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Expiry Day Monitor running at: {time}", DateTimeOffset.Now);
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var strategyService = scope.ServiceProvider.GetRequiredService<StrategyService>();
                        var positionRepository = scope.ServiceProvider.GetRequiredService<IPositionRepository>();

                        var expiry = strategyService.GetNextWeeklyExpiry();
                        if (DateTime.Today == expiry.Date)
                        {
                            var openPositions = await positionRepository.GetOpenPositionsAsync();
                            foreach (var position in openPositions)
                            {
                                // MODIFIED: Changed property to 'TradingSymbol' to match the updated model.
                                if (position.TradingSymbol != null)
                                {
                                    await strategyService.ClosePositionAsync(position.TradingSymbol);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the Expiry Day Monitor.");
                }
                // Check once a day
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
