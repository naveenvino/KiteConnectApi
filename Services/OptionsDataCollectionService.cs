using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class OptionsDataCollectionService : BackgroundService
    {
        private readonly ILogger<OptionsDataCollectionService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly Timer _marketHoursTimer;
        private readonly Timer _dailyCleanupTimer;
        private bool _isMarketOpen;

        public OptionsDataCollectionService(
            ILogger<OptionsDataCollectionService> logger,
            IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            
            // Check market hours every minute
            _marketHoursTimer = new Timer(CheckMarketHours, null, 0, 60000); // 0ms delay, 60s interval
            
            // Daily cleanup at 4 AM
            var nextCleanup = GetNextCleanupTime();
            var cleanupDelay = (int)(nextCleanup - DateTime.Now).TotalMilliseconds;
            _dailyCleanupTimer = new Timer(DailyCleanup, null, cleanupDelay, 24 * 60 * 60 * 1000); // Daily
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Options Data Collection Service started");
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (_isMarketOpen)
                    {
                        await CollectOptionsDataAsync();
                        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Collect every minute during market hours
                    }
                    else
                    {
                        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Check every 5 minutes when market is closed
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Options Data Collection Service");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
        }

        private async Task CollectOptionsDataAsync()
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var kiteService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();
                
                // Get active strategies to determine which options to collect
                var activeStrategies = await context.NiftyOptionStrategyConfigs
                    .Where(s => s.IsEnabled && s.ToDate >= DateTime.Today)
                    .ToListAsync();
                
                var optionsToCollect = new HashSet<string>();
                
                foreach (var strategy in activeStrategies)
                {
                    var expiryDate = GetNearestWeeklyExpiry();
                    var strikes = GenerateStrikesForCollection(strategy);
                    
                    foreach (var strike in strikes)
                    {
                        var ceSymbol = GenerateTradingSymbol("NIFTY", expiryDate, strike, "CE");
                        var peSymbol = GenerateTradingSymbol("NIFTY", expiryDate, strike, "PE");
                        optionsToCollect.Add(ceSymbol);
                        optionsToCollect.Add(peSymbol);
                    }
                }
                
                // Collect data in batches (max 500 per request)
                var symbolBatches = optionsToCollect.Chunk(500);
                
                foreach (var batch in symbolBatches)
                {
                    try
                    {
                        var quotes = await kiteService.GetQuotesAsync(batch.ToArray());
                        await SaveOptionsDataAsync(quotes, context);
                        
                        // Rate limiting - wait 1 second between batches
                        await Task.Delay(1000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error collecting data for batch: {Batch}", string.Join(", ", batch));
                    }
                }
                
                _logger.LogInformation("Collected data for {Count} options", optionsToCollect.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CollectOptionsDataAsync");
            }
        }

        private async Task SaveOptionsDataAsync(Dictionary<string, KiteConnect.Quote> quotes, ApplicationDbContext context)
        {
            var dataToSave = new List<OptionsHistoricalData>();
            var timestamp = DateTime.Now;
            
            foreach (var quote in quotes)
            {
                try
                {
                    var symbolInfo = ParseTradingSymbol(quote.Key);
                    if (symbolInfo == null) continue;
                    
                    var optionData = new OptionsHistoricalData
                    {
                        TradingSymbol = quote.Key,
                        Timestamp = timestamp,
                        Exchange = "NFO",
                        Underlying = symbolInfo.Underlying,
                        Strike = symbolInfo.Strike,
                        OptionType = symbolInfo.OptionType,
                        ExpiryDate = symbolInfo.ExpiryDate,
                        Open = (decimal)quote.Value.LastPrice, // Simplified for now
                        High = (decimal)quote.Value.LastPrice,
                        Low = (decimal)quote.Value.LastPrice,
                        Close = (decimal)quote.Value.LastPrice,
                        LastPrice = (decimal)quote.Value.LastPrice,
                        Volume = 0, // Will be populated later
                        OpenInterest = 0, // Will be populated later
                        BidPrice = null, // Will be populated later
                        AskPrice = null, // Will be populated later
                        DataSource = "KiteConnect",
                        Interval = "1minute"
                    };
                    
                    if (optionData.BidPrice.HasValue && optionData.AskPrice.HasValue)
                    {
                        optionData.BidAskSpread = optionData.AskPrice.Value - optionData.BidPrice.Value;
                    }
                    
                    dataToSave.Add(optionData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing quote for {Symbol}", quote.Key);
                }
            }
            
            if (dataToSave.Any())
            {
                context.OptionsHistoricalData.AddRange(dataToSave);
                await context.SaveChangesAsync();
                _logger.LogDebug("Saved {Count} options data records", dataToSave.Count);
            }
        }

        private void CheckMarketHours(object? state)
        {
            var now = DateTime.Now;
            var marketStart = new TimeSpan(9, 15, 0);
            var marketEnd = new TimeSpan(15, 30, 0);
            
            _isMarketOpen = now.DayOfWeek >= DayOfWeek.Monday && 
                           now.DayOfWeek <= DayOfWeek.Friday &&
                           now.TimeOfDay >= marketStart && 
                           now.TimeOfDay <= marketEnd;
        }

        private void DailyCleanup(object? state)
        {
            Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    
                    // Keep only last 30 days of minute data
                    var cutoffDate = DateTime.Now.AddDays(-30);
                    var oldData = await context.OptionsHistoricalData
                        .Where(d => d.Timestamp < cutoffDate && d.Interval == "1minute")
                        .ToListAsync();
                    
                    if (oldData.Any())
                    {
                        context.OptionsHistoricalData.RemoveRange(oldData);
                        await context.SaveChangesAsync();
                        _logger.LogInformation("Cleaned up {Count} old data records", oldData.Count);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in daily cleanup");
                }
            });
        }

        private DateTime GetNextCleanupTime()
        {
            var tomorrow = DateTime.Today.AddDays(1);
            return tomorrow.AddHours(4); // 4 AM
        }

        private DateTime GetNearestWeeklyExpiry()
        {
            var today = DateTime.Now.Date;
            var daysUntilThursday = ((int)DayOfWeek.Thursday - (int)today.DayOfWeek + 7) % 7;
            
            if (daysUntilThursday == 0 && DateTime.Now.TimeOfDay > TimeSpan.FromHours(15.5))
            {
                daysUntilThursday = 7;
            }
            
            return today.AddDays(daysUntilThursday);
        }

        private List<int> GenerateStrikesForCollection(NiftyOptionStrategyConfig strategy)
        {
            var strikes = new List<int>();
            var baseStrike = 22500; // Current ATM approximate
            
            // Generate strikes around ATM (±2000 points, every 50 points)
            for (int strike = baseStrike - 2000; strike <= baseStrike + 2000; strike += 50)
            {
                strikes.Add(strike);
            }
            
            return strikes;
        }

        private string GenerateTradingSymbol(string underlying, DateTime expiry, int strike, string optionType)
        {
            var year = expiry.ToString("yy");
            var month = expiry.Month.ToString();
            var day = expiry.Day.ToString("D2");
            
            return $"{underlying}{year}{month}{day}{strike}{optionType.ToUpper()}";
        }

        private SymbolInfo? ParseTradingSymbol(string tradingSymbol)
        {
            try
            {
                // Parse symbol like NIFTY2471722500CE
                if (tradingSymbol.Length < 10) return null;
                
                var underlying = "NIFTY";
                var optionType = tradingSymbol.Substring(tradingSymbol.Length - 2);
                var strikeStr = tradingSymbol.Substring(underlying.Length + 5, tradingSymbol.Length - underlying.Length - 7);
                var strike = int.Parse(strikeStr);
                
                var yearStr = tradingSymbol.Substring(underlying.Length, 2);
                var monthStr = tradingSymbol.Substring(underlying.Length + 2, 1);
                var dayStr = tradingSymbol.Substring(underlying.Length + 3, 2);
                
                var year = 2000 + int.Parse(yearStr);
                var month = int.Parse(monthStr);
                var day = int.Parse(dayStr);
                
                return new SymbolInfo
                {
                    Underlying = underlying,
                    Strike = strike,
                    OptionType = optionType,
                    ExpiryDate = new DateTime(year, month, day)
                };
            }
            catch
            {
                return null;
            }
        }

        private class SymbolInfo
        {
            public string Underlying { get; set; } = string.Empty;
            public int Strike { get; set; }
            public string OptionType { get; set; } = string.Empty;
            public DateTime ExpiryDate { get; set; }
        }

        public override void Dispose()
        {
            _marketHoursTimer?.Dispose();
            _dailyCleanupTimer?.Dispose();
            base.Dispose();
        }
    }
}