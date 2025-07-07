using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class TradingStrategyMonitor : BackgroundService
    {
        private readonly ILogger<TradingStrategyMonitor> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public TradingStrategyMonitor(ILogger<TradingStrategyMonitor> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Trading Strategy Monitor running at: {time}", DateTimeOffset.Now);
                try
                {
                    // The MonitorAndExecuteExits method was removed as this logic
                    // is now handled by the Stoploss alert.
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the Trading Strategy Monitor.");
                }
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }
}
