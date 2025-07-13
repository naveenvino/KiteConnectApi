using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using KiteConnectApi.Grains;

namespace KiteConnectApi.Services
{
    public class TradingStrategyMonitor : BackgroundService
    {
        private readonly ILogger<TradingStrategyMonitor> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IClusterClient _clusterClient;

        public TradingStrategyMonitor(ILogger<TradingStrategyMonitor> logger, IServiceScopeFactory scopeFactory, IClusterClient clusterClient)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _clusterClient = clusterClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken); // Check every 60 seconds
                _logger.LogInformation("Trading Strategy Monitor running at: {time}", DateTimeOffset.Now);

                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var strategyConfigRepository = scope.ServiceProvider.GetRequiredService<INiftyOptionStrategyConfigRepository>();

                        var activeStrategies = (await strategyConfigRepository.GetAllAsync())
                                   .Where(s => s.IsEnabled && s.ToDate >= DateTime.Today)
                                   .ToList();

                        foreach (var strategy in activeStrategies)
                        {
                            var strategyGrain = _clusterClient.GetGrain<IStrategyGrain>(strategy.Id);
                            await strategyGrain.MonitorAndAdjustPositions();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the Trading Strategy Monitor.");
                }
            }
        }
    }
}
