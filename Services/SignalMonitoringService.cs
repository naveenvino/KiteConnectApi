using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class SignalMonitoringService : BackgroundService
    {
        private readonly ILogger<SignalMonitoringService> _logger;
        private readonly SignalGenerationService _signalGenerationService;
        private readonly StrategyService _strategyService;

        public SignalMonitoringService(
            ILogger<SignalMonitoringService> logger,
            SignalGenerationService signalGenerationService,
            StrategyService strategyService)
        {
            _logger = logger;
            _signalGenerationService = signalGenerationService;
            _strategyService = strategyService;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Signal Monitoring Service running at: {time}", DateTimeOffset.Now);
                try
                {
                    var signals = await _signalGenerationService.GenerateSignals();
                    foreach (var signal in signals)
                    {
                        var alert = new Models.Dto.TradingViewAlert { Action = signal.SignalType, Strike = 22500, Type = "PE", Signal = "Generated-Signal" };
                        await _strategyService.HandleTradingViewAlert(alert);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the Signal Monitoring Service.");
                }
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
