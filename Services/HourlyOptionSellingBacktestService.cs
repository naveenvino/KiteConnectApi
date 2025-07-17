using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class HourlyOptionSellingBacktestService
    {
        private readonly ApplicationDbContext _context;
        private readonly TradingViewIndicatorService _indicatorService;
        private readonly ILogger<HourlyOptionSellingBacktestService> _logger;

        public HourlyOptionSellingBacktestService(
            ApplicationDbContext context,
            TradingViewIndicatorService indicatorService,
            ILogger<HourlyOptionSellingBacktestService> logger)
        {
            _context = context;
            _indicatorService = indicatorService;
            _logger = logger;
        }

        public async Task<HourlyBacktestResult> RunHourlyBacktestAsync(HourlyBacktestRequest request)
        {
            _logger.LogInformation("Starting hourly option selling backtest from {FromDate} to {ToDate}", 
                request.FromDate, request.ToDate);

            var result = new HourlyBacktestResult
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Strategy = "1-Hour Option Selling with Hedge",
                Trades = new List<HourlyTrade>()
            };

            var currentWeekStart = GetWeekStart(request.FromDate);
            var endDate = request.ToDate;

            while (currentWeekStart <= endDate)
            {
                try
                {
                    var weekTrade = await ProcessWeekHourlyAsync(currentWeekStart, request);
                    if (weekTrade != null)
                    {
                        result.Trades.Add(weekTrade);
                        _logger.LogInformation("Week {WeekStart}: Signal {SignalId} - {Outcome}", 
                            weekTrade.WeekStart.ToString("yyyy-MM-dd"), weekTrade.SignalId, weekTrade.Outcome);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing week {WeekStart}", currentWeekStart);
                }

                currentWeekStart = currentWeekStart.AddDays(7); // Next week
            }

            CalculatePerformanceMetrics(result, request);
            return result;
        }

        private async Task<HourlyTrade?> ProcessWeekHourlyAsync(DateTime weekStart, HourlyBacktestRequest request)
        {
            // Get week's trading hours (Monday 9:15 AM to Friday 3:30 PM)
            var mondayStart = weekStart.Date.AddHours(9.25); // 9:15 AM
            var fridayEnd = weekStart.AddDays(4).Date.AddHours(15.5); // 3:30 PM Friday

            // Get weekly data for zones (previous week)
            var weeklyData = await GetWeeklyDataAsync(weekStart);
            if (weeklyData == null) return null;

            // Track week state
            var signalFiredThisWeek = false;
            var firstHourData = new FirstHourData();
            var hourCount = 0;

            // Process each hour of the week
            for (var currentHour = mondayStart; currentHour <= fridayEnd; currentHour = currentHour.AddHours(1))
            {
                // Skip weekends and non-trading hours
                if (currentHour.DayOfWeek == DayOfWeek.Saturday || currentHour.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                // Skip non-trading hours (before 9:15 AM or after 3:30 PM)
                var hour = currentHour.TimeOfDay.TotalHours;
                if (hour < 9.25 || hour > 15.5) continue;

                hourCount++;

                // Get hourly NIFTY data
                var hourlyData = await GetHourlyNiftyDataAsync(currentHour);
                if (hourlyData == null) continue;

                // First hour of the week - store reference data
                if (hourCount == 1)
                {
                    firstHourData = new FirstHourData
                    {
                        Open = hourlyData.Open,
                        High = hourlyData.High,
                        Low = hourlyData.Low,
                        Close = hourlyData.Close,
                        Hour = currentHour
                    };
                    continue; // No signals on first hour
                }

                // Check for signals based on hour
                if (!signalFiredThisWeek)
                {
                    var signal = await CheckSignalsForHourAsync(hourCount, currentHour, hourlyData, firstHourData, weeklyData);
                    if (signal != null)
                    {
                        // Signal fired! Execute trade and monitor
                        var trade = await ExecuteHourlyTradeAsync(signal, currentHour, weekStart, fridayEnd, request);
                        signalFiredThisWeek = true;
                        return trade;
                    }
                }
            }

            return null; // No signal fired this week
        }

        private async Task<TradingSignalResult?> CheckSignalsForHourAsync(
            int hourCount, 
            DateTime currentHour, 
            HourlyNiftyData currentData, 
            FirstHourData firstHour, 
            WeeklyZoneData weeklyData)
        {
            var isSecondHour = hourCount == 2; // 2nd hour of the week (Monday ~10:15 AM)

            // S1: Bear Trap (only on 2nd hour)
            if (isSecondHour)
            {
                var s1Signal = CheckS1BearTrap(currentData, firstHour, weeklyData);
                if (s1Signal != null)
                {
                    s1Signal.Timestamp = currentHour;
                    return s1Signal;
                }

                var s2Signal = CheckS2SupportHold(currentData, firstHour, weeklyData);
                if (s2Signal != null)
                {
                    s2Signal.Timestamp = currentHour;
                    return s2Signal;
                }
            }

            // S3-S8: Can trigger any hour after 2nd hour
            if (hourCount > 2)
            {
                var signals = new[]
                {
                    CheckS3ResistanceHold(currentData, firstHour, weeklyData),
                    CheckS4BiasFailure(currentData, firstHour, weeklyData),
                    CheckS5BiasFailure(currentData, firstHour, weeklyData),
                    CheckS6WeaknessConfirmed(currentData, firstHour, weeklyData),
                    CheckS7BreakoutConfirmed(currentData, firstHour, weeklyData),
                    CheckS8BreakdownConfirmed(currentData, firstHour, weeklyData)
                };

                foreach (var signal in signals)
                {
                    if (signal != null)
                    {
                        signal.Timestamp = currentHour;
                        return signal;
                    }
                }
            }

            return null;
        }

        private async Task<HourlyTrade?> ExecuteHourlyTradeAsync(
            TradingSignalResult signal, 
            DateTime entryHour, 
            DateTime weekStart, 
            DateTime weekEnd, 
            HourlyBacktestRequest request)
        {
            try
            {
                // Get Thursday expiry for this week
                var thursdayExpiry = GetThursdayExpiry(weekStart);

                // Create trading symbols
                var mainSymbol = GenerateTradingSymbol("NIFTY", thursdayExpiry, (int)signal.StrikePrice, signal.OptionType);
                var hedgeStrike = CalculateHedgeStrike((int)signal.StrikePrice, signal.OptionType, request.HedgePoints);
                var hedgeSymbol = GenerateTradingSymbol("NIFTY", thursdayExpiry, hedgeStrike, signal.OptionType);

                // Get entry prices
                var mainEntryData = await GetOptionsDataAtHourAsync(mainSymbol, entryHour);
                var hedgeEntryData = await GetOptionsDataAtHourAsync(hedgeSymbol, entryHour);

                if (mainEntryData == null || hedgeEntryData == null)
                {
                    _logger.LogWarning("No entry data for {MainSymbol} or {HedgeSymbol} at {Hour}", 
                        mainSymbol, hedgeSymbol, entryHour);
                    return null;
                }

                var trade = new HourlyTrade
                {
                    WeekStart = weekStart,
                    SignalId = signal.SignalId,
                    SignalName = signal.SignalName,
                    EntryHour = entryHour,
                    MainSymbol = mainSymbol,
                    HedgeSymbol = hedgeSymbol,
                    MainStrike = (int)signal.StrikePrice,
                    HedgeStrike = hedgeStrike,
                    OptionType = signal.OptionType,
                    MainEntryPrice = mainEntryData.LastPrice,
                    HedgeEntryPrice = hedgeEntryData.LastPrice,
                    StopLossLevel = signal.StopLossPrice,
                    Quantity = request.LotSize
                };

                // Monitor hourly until stop loss or expiry
                var exitResult = await MonitorHourlyPositionAsync(trade, entryHour, weekEnd);
                
                // Calculate P&L
                trade.ExitHour = exitResult.ExitHour;
                trade.MainExitPrice = exitResult.MainExitPrice;
                trade.HedgeExitPrice = exitResult.HedgeExitPrice;
                trade.Outcome = exitResult.Outcome;
                
                var pnl = CalculatePnL(trade);
                trade.NetPnL = pnl.NetPnL;
                trade.MainPnL = pnl.MainPnL;
                trade.HedgePnL = pnl.HedgePnL;
                trade.Success = trade.NetPnL > 0;

                return trade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing hourly trade for {SignalId}", signal.SignalId);
                return null;
            }
        }

        private async Task<HourlyExitResult> MonitorHourlyPositionAsync(HourlyTrade trade, DateTime entryHour, DateTime weekEnd)
        {
            var currentHour = entryHour.AddHours(1); // Start monitoring next hour
            var thursdayExpiry = GetThursdayExpiry(trade.WeekStart);

            while (currentHour <= weekEnd && currentHour <= thursdayExpiry)
            {
                // Skip weekends and non-trading hours
                if (currentHour.DayOfWeek == DayOfWeek.Saturday || currentHour.DayOfWeek == DayOfWeek.Sunday)
                {
                    currentHour = currentHour.AddHours(1);
                    continue;
                }

                var hour = currentHour.TimeOfDay.TotalHours;
                if (hour < 9.25 || hour > 15.5)
                {
                    currentHour = currentHour.AddHours(1);
                    continue;
                }

                // Get current prices
                var mainData = await GetOptionsDataAtHourAsync(trade.MainSymbol, currentHour);
                var hedgeData = await GetOptionsDataAtHourAsync(trade.HedgeSymbol, currentHour);

                if (mainData != null && hedgeData != null)
                {
                    // Check for stop loss hit (option selling logic)
                    // For selling: stop loss hits when price INCREASES above stop loss level
                    if (mainData.LastPrice >= trade.StopLossLevel)
                    {
                        return new HourlyExitResult
                        {
                            ExitHour = currentHour,
                            MainExitPrice = mainData.LastPrice,
                            HedgeExitPrice = hedgeData.LastPrice,
                            Outcome = "STOP_LOSS"
                        };
                    }
                }

                currentHour = currentHour.AddHours(1);
            }

            // If we reach here, new week started without stop loss = WIN
            var finalMainData = await GetOptionsDataAtHourAsync(trade.MainSymbol, thursdayExpiry);
            var finalHedgeData = await GetOptionsDataAtHourAsync(trade.HedgeSymbol, thursdayExpiry);

            return new HourlyExitResult
            {
                ExitHour = thursdayExpiry,
                MainExitPrice = finalMainData?.LastPrice ?? 0.1m, // Expires nearly worthless
                HedgeExitPrice = finalHedgeData?.LastPrice ?? 0.1m,
                Outcome = "EXPIRY_WIN"
            };
        }

        // Signal checking methods (implementing TradingView logic)
        private TradingSignalResult? CheckS1BearTrap(HourlyNiftyData current, FirstHourData first, WeeklyZoneData weekly)
        {
            // S1 Logic: Bear Trap (from TradingView script line 274)
            var condition1 = first.Open >= weekly.LowerZoneBottom;
            var condition2 = first.Close < weekly.LowerZoneBottom;
            var condition3 = current.Close > first.Low;

            if (condition1 && condition2 && condition3)
            {
                var stopLossPrice = first.Low - Math.Abs(first.Open - first.Close);
                var strikePrice = RoundTo100((decimal)stopLossPrice);

                return new TradingSignalResult
                {
                    SignalId = "S1",
                    SignalName = "Bear Trap",
                    Direction = 1, // Bullish
                    StrikePrice = strikePrice,
                    OptionType = "PE",
                    StopLossPrice = (decimal)stopLossPrice,
                    Confidence = 0.8m,
                    Description = "Bear trap recovery signal"
                };
            }
            return null;
        }

        private TradingSignalResult? CheckS2SupportHold(HourlyNiftyData current, FirstHourData first, WeeklyZoneData weekly)
        {
            // S2 Logic: Support Hold (from TradingView script line 288)
            var condition1 = first.Open > weekly.PrevLow;
            var condition2 = Math.Abs(weekly.PrevClose - weekly.LowerZoneBottom) <= weekly.MarginLow;
            var condition3 = Math.Abs(first.Open - weekly.LowerZoneBottom) <= weekly.MarginLow;
            var condition4 = first.Close >= weekly.LowerZoneBottom;
            var condition5 = first.Close >= weekly.PrevClose;
            var condition6 = current.Close >= first.Low;
            var condition7 = current.Close > weekly.PrevClose;
            var condition8 = current.Close > weekly.LowerZoneBottom;
            var condition9 = weekly.WeeklySig == 1; // Bullish bias

            if (condition1 && condition2 && condition3 && condition4 && condition5 && condition6 && condition7 && condition8 && condition9)
            {
                var stopLossPrice = weekly.LowerZoneBottom;
                var strikePrice = RoundTo100(stopLossPrice);

                return new TradingSignalResult
                {
                    SignalId = "S2",
                    SignalName = "Support Hold (Bullish)",
                    Direction = 1, // Bullish
                    StrikePrice = strikePrice,
                    OptionType = "PE",
                    StopLossPrice = stopLossPrice,
                    Confidence = 0.85m,
                    Description = "Support hold confirmation"
                };
            }
            return null;
        }

        private TradingSignalResult? CheckS3ResistanceHold(HourlyNiftyData current, FirstHourData first, WeeklyZoneData weekly)
        {
            // S3 Logic: Resistance Hold (from TradingView script line 323)
            var baseCondition1 = weekly.WeeklySig == -1; // Bearish bias
            var baseCondition2 = Math.Abs(weekly.PrevClose - weekly.UpperZoneBottom) <= weekly.MarginHigh;
            var baseCondition3 = Math.Abs(first.Open - weekly.UpperZoneBottom) <= weekly.MarginHigh;
            var baseCondition4 = first.Close <= weekly.PrevHigh;

            if (!(baseCondition1 && baseCondition2 && baseCondition3 && baseCondition4))
                return null;

            // Scenario A or B (simplified for this implementation)
            var scenarioA = current.Close < first.High && current.Close < weekly.UpperZoneBottom;
            var scenarioB = current.Close < first.Low && current.Close < weekly.UpperZoneBottom;

            if (scenarioA || scenarioB)
            {
                var stopLossPrice = weekly.PrevHigh;
                var strikePrice = RoundTo100(stopLossPrice);

                return new TradingSignalResult
                {
                    SignalId = "S3",
                    SignalName = "Resistance Hold (Bearish)",
                    Direction = -1, // Bearish
                    StrikePrice = strikePrice,
                    OptionType = "CE",
                    StopLossPrice = stopLossPrice,
                    Confidence = 0.82m,
                    Description = "Resistance rejection confirmed"
                };
            }
            return null;
        }

        // Simplified implementations for other signals (S4-S8)
        private TradingSignalResult? CheckS4BiasFailure(HourlyNiftyData current, FirstHourData first, WeeklyZoneData weekly)
        {
            // Simplified S4 logic
            if (weekly.WeeklySig == -1 && first.Open > weekly.UpperZoneTop && current.Close > first.High)
            {
                return new TradingSignalResult
                {
                    SignalId = "S4",
                    SignalName = "Bias Failure (Bullish)",
                    Direction = 1,
                    StrikePrice = RoundTo100((decimal)first.Low),
                    OptionType = "PE",
                    StopLossPrice = (decimal)first.Low,
                    Confidence = 0.78m
                };
            }
            return null;
        }

        private TradingSignalResult? CheckS5BiasFailure(HourlyNiftyData current, FirstHourData first, WeeklyZoneData weekly)
        {
            // Simplified S5 logic
            if (weekly.WeeklySig == 1 && first.Open < weekly.LowerZoneBottom && current.Close < first.Low)
            {
                return new TradingSignalResult
                {
                    SignalId = "S5",
                    SignalName = "Bias Failure (Bearish)",
                    Direction = -1,
                    StrikePrice = RoundTo100((decimal)first.High),
                    OptionType = "CE",
                    StopLossPrice = (decimal)first.High,
                    Confidence = 0.78m
                };
            }
            return null;
        }

        private TradingSignalResult? CheckS6WeaknessConfirmed(HourlyNiftyData current, FirstHourData first, WeeklyZoneData weekly)
        {
            // Simplified S6 logic
            if (weekly.WeeklySig == -1 && current.Close < weekly.UpperZoneBottom)
            {
                return new TradingSignalResult
                {
                    SignalId = "S6",
                    SignalName = "Weakness Confirmed",
                    Direction = -1,
                    StrikePrice = RoundTo100(weekly.PrevHigh),
                    OptionType = "CE",
                    StopLossPrice = weekly.PrevHigh,
                    Confidence = 0.75m
                };
            }
            return null;
        }

        private TradingSignalResult? CheckS7BreakoutConfirmed(HourlyNiftyData current, FirstHourData first, WeeklyZoneData weekly)
        {
            // Simplified S7 logic
            if (current.Close > first.High && current.Close > weekly.PrevHigh)
            {
                return new TradingSignalResult
                {
                    SignalId = "S7",
                    SignalName = "1H Breakout Confirmed",
                    Direction = 1,
                    StrikePrice = RoundTo100((decimal)first.Low),
                    OptionType = "PE",
                    StopLossPrice = (decimal)first.Low,
                    Confidence = 0.72m
                };
            }
            return null;
        }

        private TradingSignalResult? CheckS8BreakdownConfirmed(HourlyNiftyData current, FirstHourData first, WeeklyZoneData weekly)
        {
            // Simplified S8 logic
            if (current.Close < first.Low && current.Close < weekly.PrevLow)
            {
                return new TradingSignalResult
                {
                    SignalId = "S8",
                    SignalName = "1H Breakdown Confirmed",
                    Direction = -1,
                    StrikePrice = RoundTo100((decimal)first.High),
                    OptionType = "CE",
                    StopLossPrice = (decimal)first.High,
                    Confidence = 0.72m
                };
            }
            return null;
        }

        // Helper methods
        private DateTime GetWeekStart(DateTime date)
        {
            var daysFromMonday = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysFromMonday < 0) daysFromMonday += 7;
            return date.Date.AddDays(-daysFromMonday);
        }

        private DateTime GetThursdayExpiry(DateTime weekStart)
        {
            return weekStart.AddDays(3).Date.AddHours(15.5); // Thursday 3:30 PM
        }

        private int CalculateHedgeStrike(int mainStrike, string optionType, int hedgePoints)
        {
            return optionType == "CE" ? mainStrike + hedgePoints : mainStrike - hedgePoints;
        }

        private decimal RoundTo100(decimal price)
        {
            return Math.Round(price / 100) * 100;
        }

        private string GenerateTradingSymbol(string underlying, DateTime expiry, int strike, string optionType)
        {
            var year = expiry.ToString("yy");
            var month = expiry.Month.ToString();
            var day = expiry.Day.ToString("D2");
            return $"{underlying}{year}{month}{day}{strike}{optionType.ToUpper()}";
        }

        private async Task<WeeklyZoneData?> GetWeeklyDataAsync(DateTime weekStart)
        {
            // Get previous week's data for zone calculation
            var prevWeekStart = weekStart.AddDays(-7);
            var prevWeekEnd = weekStart.AddDays(-1);

            var weeklyData = await _context.OptionsHistoricalData
                .Where(d => d.Underlying == "NIFTY" && 
                           d.Timestamp >= prevWeekStart && 
                           d.Timestamp <= prevWeekEnd &&
                           d.Interval == "day")
                .ToListAsync();

            if (!weeklyData.Any()) return null;

            var high = weeklyData.Max(d => d.High);
            var low = weeklyData.Min(d => d.Low);
            var close = weeklyData.OrderBy(d => d.Timestamp).Last().Close;

            // Calculate zones (simplified)
            var upperZoneTop = (decimal)Math.Max((double)high, (double)close);
            var upperZoneBottom = (decimal)Math.Min((double)high, (double)close);
            var lowerZoneTop = (decimal)Math.Max((double)low, (double)close);
            var lowerZoneBottom = (decimal)Math.Min((double)low, (double)close);

            // Calculate bias
            var distanceToHigh = Math.Abs((double)close - (double)upperZoneTop);
            var distanceToLow = Math.Abs((double)close - (double)lowerZoneBottom);
            var weeklySig = distanceToHigh < distanceToLow ? -1 : 1;

            return new WeeklyZoneData
            {
                PrevHigh = (decimal)high,
                PrevLow = (decimal)low,
                PrevClose = close,
                UpperZoneTop = upperZoneTop,
                UpperZoneBottom = upperZoneBottom,
                LowerZoneTop = lowerZoneTop,
                LowerZoneBottom = lowerZoneBottom,
                WeeklySig = weeklySig,
                MarginHigh = Math.Max((upperZoneTop - upperZoneBottom) * 3, 0.05m),
                MarginLow = Math.Max((lowerZoneTop - lowerZoneBottom) * 3, 0.05m)
            };
        }

        private async Task<HourlyNiftyData?> GetHourlyNiftyDataAsync(DateTime hour)
        {
            var data = await _context.OptionsHistoricalData
                .Where(d => d.Underlying == "NIFTY" && 
                           d.Timestamp >= hour.AddMinutes(-30) && 
                           d.Timestamp <= hour.AddMinutes(30) &&
                           d.Interval == "60minute")
                .OrderBy(d => d.Timestamp)
                .FirstOrDefaultAsync();

            if (data == null) return null;

            return new HourlyNiftyData
            {
                Open = data.Open,
                High = data.High,
                Low = data.Low,
                Close = data.Close,
                Hour = hour
            };
        }

        private async Task<OptionsHistoricalData?> GetOptionsDataAtHourAsync(string symbol, DateTime hour)
        {
            return await _context.OptionsHistoricalData
                .Where(d => d.TradingSymbol == symbol && 
                           d.Timestamp >= hour.AddMinutes(-30) && 
                           d.Timestamp <= hour.AddMinutes(30))
                .OrderBy(d => d.Timestamp)
                .FirstOrDefaultAsync();
        }

        private (decimal NetPnL, decimal MainPnL, decimal HedgePnL) CalculatePnL(HourlyTrade trade)
        {
            // Option selling P&L: Sell high, buy low = profit
            var mainPnL = (trade.MainEntryPrice - trade.MainExitPrice) * trade.Quantity;
            var hedgePnL = (trade.HedgeExitPrice - trade.HedgeEntryPrice) * trade.Quantity;
            var netPnL = mainPnL + hedgePnL;

            return (netPnL, mainPnL, hedgePnL);
        }

        private void CalculatePerformanceMetrics(HourlyBacktestResult result, HourlyBacktestRequest request)
        {
            if (!result.Trades.Any()) return;

            result.TotalTrades = result.Trades.Count;
            result.WinningTrades = result.Trades.Count(t => t.Success);
            result.LosingTrades = result.Trades.Count(t => !t.Success);
            result.WinRate = (double)result.WinningTrades / result.TotalTrades * 100;
            result.TotalPnL = result.Trades.Sum(t => t.NetPnL);
            result.AveragePnL = result.TotalPnL / result.TotalTrades;
            result.MaxProfit = result.Trades.Max(t => t.NetPnL);
            result.MaxLoss = result.Trades.Min(t => t.NetPnL);
            result.InitialCapital = request.InitialCapital;
            result.FinalCapital = request.InitialCapital + result.TotalPnL;

            // Signal breakdown
            result.SignalBreakdown = result.Trades
                .GroupBy(t => t.SignalId)
                .ToDictionary(g => g.Key, g => new SignalStats
                {
                    TotalTrades = g.Count(),
                    WinningTrades = g.Count(t => t.Success),
                    WinRate = (double)g.Count(t => t.Success) / g.Count() * 100,
                    TotalPnL = g.Sum(t => t.NetPnL),
                    AveragePnL = g.Average(t => t.NetPnL)
                });
        }
    }

    // Supporting classes
    public class HourlyBacktestRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal InitialCapital { get; set; } = 100000;
        public int LotSize { get; set; } = 50;
        public int HedgePoints { get; set; } = 300;
    }

    public class HourlyBacktestResult
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Strategy { get; set; } = string.Empty;
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public double WinRate { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal AveragePnL { get; set; }
        public decimal MaxProfit { get; set; }
        public decimal MaxLoss { get; set; }
        public decimal InitialCapital { get; set; }
        public decimal FinalCapital { get; set; }
        public List<HourlyTrade> Trades { get; set; } = new();
        public Dictionary<string, SignalStats> SignalBreakdown { get; set; } = new();
    }

    public class HourlyTrade
    {
        public DateTime WeekStart { get; set; }
        public string SignalId { get; set; } = string.Empty;
        public string SignalName { get; set; } = string.Empty;
        public DateTime EntryHour { get; set; }
        public DateTime ExitHour { get; set; }
        public string MainSymbol { get; set; } = string.Empty;
        public string HedgeSymbol { get; set; } = string.Empty;
        public int MainStrike { get; set; }
        public int HedgeStrike { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public decimal MainEntryPrice { get; set; }
        public decimal HedgeEntryPrice { get; set; }
        public decimal MainExitPrice { get; set; }
        public decimal HedgeExitPrice { get; set; }
        public decimal StopLossLevel { get; set; }
        public int Quantity { get; set; }
        public decimal NetPnL { get; set; }
        public decimal MainPnL { get; set; }
        public decimal HedgePnL { get; set; }
        public string Outcome { get; set; } = string.Empty; // STOP_LOSS or EXPIRY_WIN
        public bool Success { get; set; }
    }

    public class FirstHourData
    {
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public DateTime Hour { get; set; }
    }

    public class HourlyNiftyData
    {
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public DateTime Hour { get; set; }
    }

    public class WeeklyZoneData
    {
        public decimal PrevHigh { get; set; }
        public decimal PrevLow { get; set; }
        public decimal PrevClose { get; set; }
        public decimal UpperZoneTop { get; set; }
        public decimal UpperZoneBottom { get; set; }
        public decimal LowerZoneTop { get; set; }
        public decimal LowerZoneBottom { get; set; }
        public int WeeklySig { get; set; } // -1=bear, 1=bull
        public decimal MarginHigh { get; set; }
        public decimal MarginLow { get; set; }
    }

    public class HourlyExitResult
    {
        public DateTime ExitHour { get; set; }
        public decimal MainExitPrice { get; set; }
        public decimal HedgeExitPrice { get; set; }
        public string Outcome { get; set; } = string.Empty;
    }

    public class SignalStats
    {
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public double WinRate { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal AveragePnL { get; set; }
    }
}