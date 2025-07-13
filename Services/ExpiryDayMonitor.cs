using KiteConnectApi.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using KiteConnect;
using System.Collections.Generic;

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
                        var tradeExecutionService = scope.ServiceProvider.GetRequiredService<ITradeExecutionService>();
                        var strategyConfigRepository = scope.ServiceProvider.GetRequiredService<INiftyOptionStrategyConfigRepository>();
                        var kiteConnectService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();

                        var instruments = await kiteConnectService.GetInstrumentsAsync("NFO");

                        var activeStrategies = await strategyConfigRepository.GetAllAsync();

                        foreach (var strategy in activeStrategies.Where(s => s.IsEnabled))
                        {
                            var expiryDate = GetNearestWeeklyExpiry(instruments, strategy.UnderlyingInstrument);

                            if (expiryDate != default && DateTime.Today == expiryDate.Date && DateTime.Now.Hour >= 16)
                            {
                                _logger.LogWarning($"EXPIRY DAY: Closing all open positions for strategy {strategy.StrategyName}.");
                                await tradeExecutionService.SquareOffAllPositions(strategy.Id);
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

        private DateTime GetNearestWeeklyExpiry(IEnumerable<KiteConnectApi.Models.Dto.InstrumentDto> instruments, string? underlyingInstrument)
        {
            if (string.IsNullOrEmpty(underlyingInstrument)) return default;

            var today = DateTime.Today;
            var nextThursday = today.DayOfWeek <= DayOfWeek.Thursday
                ? today.AddDays(DayOfWeek.Thursday - today.DayOfWeek)
                : today.AddDays(7 - (int)today.DayOfWeek + (int)DayOfWeek.Thursday);

            var weeklyExpiries = instruments
                .Where(i => i.InstrumentType == "CE" && i.Name == underlyingInstrument && i.Expiry.HasValue && i.Expiry.Value.DayOfWeek == DayOfWeek.Thursday)
                .Select(i => i.Expiry.Value)
                .Distinct()
                .OrderBy(d => d);

            return weeklyExpiries.FirstOrDefault(d => d >= nextThursday);
        }

    }
}