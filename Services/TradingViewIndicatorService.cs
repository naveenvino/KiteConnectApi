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
    public class TradingViewIndicatorService
    {
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly ILogger<TradingViewIndicatorService> _logger;

        public TradingViewIndicatorService(
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            ILogger<TradingViewIndicatorService> logger)
        {
            _context = context;
            _kiteConnectService = kiteConnectService;
            _logger = logger;
        }

        public async Task<List<TradingSignalResult>> ProcessWeeklyLevelsAndBiasAsync(string symbol = "NIFTY")
        {
            try
            {
                var signals = new List<TradingSignalResult>();
                
                // Get historical data for analysis
                var weeklyData = await GetWeeklyDataAsync(symbol);
                var hourlyData = await GetHourlyDataAsync(symbol);
                
                if (!weeklyData.Any() || !hourlyData.Any())
                {
                    _logger.LogWarning("Insufficient data for signal processing");
                    return signals;
                }

                // Get current week context
                var currentWeekData = GetCurrentWeekContext(weeklyData, hourlyData);
                
                // Process each signal type
                var s1Signal = ProcessS1BearTrap(currentWeekData);
                var s2Signal = ProcessS2SupportHold(currentWeekData);
                var s3Signal = ProcessS3ResistanceHold(currentWeekData);
                var s4Signal = ProcessS4BiasFailureBullish(currentWeekData);
                var s5Signal = ProcessS5BiasFailureBearish(currentWeekData);
                var s6Signal = ProcessS6WeaknessConfirmed(currentWeekData);
                var s7Signal = ProcessS7BreakoutConfirmed(currentWeekData);
                var s8Signal = ProcessS8BreakdownConfirmed(currentWeekData);

                // Add valid signals to result
                if (s1Signal != null) signals.Add(s1Signal);
                if (s2Signal != null) signals.Add(s2Signal);
                if (s3Signal != null) signals.Add(s3Signal);
                if (s4Signal != null) signals.Add(s4Signal);
                if (s5Signal != null) signals.Add(s5Signal);
                if (s6Signal != null) signals.Add(s6Signal);
                if (s7Signal != null) signals.Add(s7Signal);
                if (s8Signal != null) signals.Add(s8Signal);

                return signals;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing weekly levels and bias signals");
                return new List<TradingSignalResult>();
            }
        }

        private async Task<List<WeeklyPriceData>> GetWeeklyDataAsync(string symbol)
        {
            // Get last 10 weeks of data
            var fromDate = DateTime.Now.AddDays(-70);
            var toDate = DateTime.Now;
            
            var weeklyData = await _context.OptionsHistoricalData
                .Where(d => d.Underlying == symbol && 
                           d.Timestamp >= fromDate && 
                           d.Timestamp <= toDate &&
                           d.Interval == "day")
                .OrderBy(d => d.Timestamp)
                .ToListAsync();

            return ConvertToWeeklyData(weeklyData);
        }

        private async Task<List<HourlyPriceData>> GetHourlyDataAsync(string symbol)
        {
            // Get last 2 weeks of hourly data
            var fromDate = DateTime.Now.AddDays(-14);
            var toDate = DateTime.Now;
            
            var hourlyData = await _context.OptionsHistoricalData
                .Where(d => d.Underlying == symbol && 
                           d.Timestamp >= fromDate && 
                           d.Timestamp <= toDate &&
                           d.Interval == "60minute")
                .OrderBy(d => d.Timestamp)
                .ToListAsync();

            return ConvertToHourlyData(hourlyData);
        }

        private WeekContext GetCurrentWeekContext(List<WeeklyPriceData> weeklyData, List<HourlyPriceData> hourlyData)
        {
            var currentWeek = GetCurrentWeekStart();
            var previousWeek = weeklyData.LastOrDefault(w => w.WeekStart < currentWeek);
            
            if (previousWeek == null)
            {
                _logger.LogWarning("No previous week data found");
                return new WeekContext();
            }

            // Calculate weekly zones
            var upperZoneTop = Math.Max(previousWeek.High, previousWeek.Max4HBody);
            var upperZoneBottom = Math.Min(previousWeek.High, previousWeek.Max4HBody);
            var lowerZoneTop = Math.Max(previousWeek.Low, previousWeek.Min4HBody);
            var lowerZoneBottom = Math.Min(previousWeek.Low, previousWeek.Min4HBody);

            // Calculate weekly bias
            var distanceToHigh = Math.Abs(previousWeek.Close - previousWeek.Max4HBody);
            var distanceToLow = Math.Abs(previousWeek.Close - previousWeek.Min4HBody);
            var weeklySig = distanceToHigh < distanceToLow ? -1 : distanceToLow < distanceToHigh ? 1 : 0;

            // Get current week bars
            var currentWeekBars = hourlyData.Where(h => h.Timestamp >= currentWeek).OrderBy(h => h.Timestamp).ToList();
            var firstBar = currentWeekBars.FirstOrDefault();
            var firstHour = currentWeekBars.FirstOrDefault();
            var currentBar = currentWeekBars.LastOrDefault();

            return new WeekContext
            {
                PreviousWeek = previousWeek,
                CurrentWeekBars = currentWeekBars,
                FirstBar = firstBar,
                FirstHour = firstHour,
                CurrentBar = currentBar,
                UpperZoneTop = (decimal)upperZoneTop,
                UpperZoneBottom = (decimal)upperZoneBottom,
                LowerZoneTop = (decimal)lowerZoneTop,
                LowerZoneBottom = (decimal)lowerZoneBottom,
                WeeklySig = weeklySig,
                MarginLow = (decimal)Math.Max((lowerZoneTop - lowerZoneBottom) * 3, 0.05),
                MarginHigh = (decimal)Math.Max((upperZoneTop - upperZoneBottom) * 3, 0.05),
                CurrentWeekStart = currentWeek
            };
        }

        private TradingSignalResult? ProcessS1BearTrap(WeekContext context)
        {
            if (context.FirstBar == null || context.CurrentBar == null || context.PreviousWeek == null)
                return null;

            var isSecondBar = context.CurrentWeekBars.Count >= 2;
            if (!isSecondBar) return null;

            var firstBar = context.FirstBar;
            var currentBar = context.CurrentBar;

            // S1 Logic: Bear Trap
            var condition1 = firstBar.Open >= (double)context.LowerZoneBottom;
            var condition2 = firstBar.Close < (double)context.LowerZoneBottom;
            var condition3 = currentBar.Close > firstBar.Low;

            if (condition1 && condition2 && condition3)
            {
                var stopLossPrice = firstBar.Low - Math.Abs(firstBar.Open - firstBar.Close);
                var strikePrice = RoundTo100((decimal)stopLossPrice);

                return new TradingSignalResult
                {
                    SignalId = "S1",
                    SignalName = "Bear Trap",
                    Direction = 1, // Bullish
                    StrikePrice = strikePrice,
                    OptionType = "PE",
                    Action = "Entry",
                    StopLossPrice = (decimal)stopLossPrice,
                    Timestamp = DateTime.Now,
                    Confidence = 0.8m,
                    Description = "Triggers after a fake breakdown if the second bar recovers to close above the first bar's low"
                };
            }

            return null;
        }

        private TradingSignalResult? ProcessS2SupportHold(WeekContext context)
        {
            if (context.FirstBar == null || context.CurrentBar == null || context.PreviousWeek == null)
                return null;

            var isSecondBar = context.CurrentWeekBars.Count >= 2;
            if (!isSecondBar) return null;

            var firstBar = context.FirstBar;
            var currentBar = context.CurrentBar;
            var prevWeek = context.PreviousWeek;

            // S2 Logic: Support Hold (Bullish)
            var condition1 = firstBar.Open > prevWeek.Low;
            var condition2 = Math.Abs(prevWeek.Close - (double)context.LowerZoneBottom) <= (double)context.MarginLow;
            var condition3 = Math.Abs(firstBar.Open - (double)context.LowerZoneBottom) <= (double)context.MarginLow;
            var condition4 = firstBar.Close >= (double)context.LowerZoneBottom;
            var condition5 = firstBar.Close >= prevWeek.Close;
            var condition6 = currentBar.Close >= firstBar.Low;
            var condition7 = currentBar.Close > prevWeek.Close;
            var condition8 = currentBar.Close > (double)context.LowerZoneBottom;
            var condition9 = context.WeeklySig == 1;

            if (condition1 && condition2 && condition3 && condition4 && condition5 && condition6 && condition7 && condition8 && condition9)
            {
                var stopLossPrice = context.LowerZoneBottom;
                var strikePrice = RoundTo100(stopLossPrice);

                return new TradingSignalResult
                {
                    SignalId = "S2",
                    SignalName = "Support Hold (Bullish)",
                    Direction = 1, // Bullish
                    StrikePrice = strikePrice,
                    OptionType = "PE",
                    Action = "Entry",
                    StopLossPrice = stopLossPrice,
                    Timestamp = DateTime.Now,
                    Confidence = 0.85m,
                    Description = "Shows the bullish confirmation signal at support"
                };
            }

            return null;
        }

        private TradingSignalResult? ProcessS3ResistanceHold(WeekContext context)
        {
            if (context.FirstBar == null || context.CurrentBar == null || context.PreviousWeek == null)
                return null;

            var firstBar = context.FirstBar;
            var currentBar = context.CurrentBar;
            var prevWeek = context.PreviousWeek;

            // S3 Base conditions
            var baseCondition1 = context.WeeklySig == -1;
            var baseCondition2 = Math.Abs(prevWeek.Close - (double)context.UpperZoneBottom) <= (double)context.MarginHigh;
            var baseCondition3 = Math.Abs(firstBar.Open - (double)context.UpperZoneBottom) <= (double)context.MarginHigh;
            var baseCondition4 = firstBar.Close <= prevWeek.High;

            if (!(baseCondition1 && baseCondition2 && baseCondition3 && baseCondition4))
                return null;

            var isSecondBar = context.CurrentWeekBars.Count >= 2;
            
            // Scenario A: Inside candle on the 2nd bar
            var scenarioA = isSecondBar && 
                           currentBar.Close < firstBar.High && 
                           currentBar.Close < (double)context.UpperZoneBottom && 
                           (firstBar.High >= (double)context.UpperZoneBottom || currentBar.High >= (double)context.UpperZoneBottom);

            // Scenario B: Breakdown below the 1st bar's low
            var scenarioB = currentBar.Close < firstBar.Low && 
                           currentBar.Close < (double)context.UpperZoneBottom;

            if (scenarioA || scenarioB)
            {
                var stopLossPrice = (decimal)prevWeek.High;
                var strikePrice = RoundTo100(stopLossPrice);

                return new TradingSignalResult
                {
                    SignalId = "S3",
                    SignalName = "Resistance Hold (Bearish)",
                    Direction = -1, // Bearish
                    StrikePrice = strikePrice,
                    OptionType = "CE",
                    Action = "Entry",
                    StopLossPrice = stopLossPrice,
                    Timestamp = DateTime.Now,
                    Confidence = 0.82m,
                    Description = "Shows the bearish confirmation signal at resistance when the prior week closed near the zone"
                };
            }

            return null;
        }

        private TradingSignalResult? ProcessS4BiasFailureBullish(WeekContext context)
        {
            if (context.FirstBar == null || context.FirstHour == null || context.PreviousWeek == null)
                return null;

            var firstBar = context.FirstBar;
            var firstHour = context.FirstHour;

            // S4 Logic: Bias Failure (Bullish)
            var s4Trigger = ProcessS4Logic(context);
            var condition1 = s4Trigger;
            var condition2 = context.WeeklySig == -1;
            var condition3 = firstBar.Open > (double)context.UpperZoneTop;

            if (condition1 && condition2 && condition3)
            {
                var stopLossPrice = (decimal)firstHour.Low;
                var strikePrice = RoundTo100(stopLossPrice);

                return new TradingSignalResult
                {
                    SignalId = "S4",
                    SignalName = "Bias Failure (Bullish)",
                    Direction = 1, // Bullish
                    StrikePrice = strikePrice,
                    OptionType = "PE",
                    Action = "Entry",
                    StopLossPrice = stopLossPrice,
                    Timestamp = DateTime.Now,
                    Confidence = 0.78m,
                    Description = "Shows the contrarian bullish signal after a gap up against a bearish bias"
                };
            }

            return null;
        }

        private TradingSignalResult? ProcessS5BiasFailureBearish(WeekContext context)
        {
            if (context.FirstBar == null || context.FirstHour == null || context.PreviousWeek == null)
                return null;

            var firstBar = context.FirstBar;
            var firstHour = context.FirstHour;
            var prevWeek = context.PreviousWeek;
            var currentBar = context.CurrentBar;

            // S5 Logic: Bias Failure (Bearish)
            var condition1 = context.WeeklySig == 1;
            var condition2 = firstBar.Open < (double)context.LowerZoneBottom;
            var condition3 = firstHour.Close < (double)context.LowerZoneBottom;
            var condition4 = firstHour.Close < prevWeek.Low;
            var condition5 = currentBar.Close < firstHour.Low;

            if (condition1 && condition2 && condition3 && condition4 && condition5)
            {
                var stopLossPrice = (decimal)firstHour.High;
                var strikePrice = RoundTo100(stopLossPrice);

                return new TradingSignalResult
                {
                    SignalId = "S5",
                    SignalName = "Bias Failure (Bearish)",
                    Direction = -1, // Bearish
                    StrikePrice = strikePrice,
                    OptionType = "CE",
                    Action = "Entry",
                    StopLossPrice = stopLossPrice,
                    Timestamp = DateTime.Now,
                    Confidence = 0.78m,
                    Description = "Shows the contrarian bearish signal after a gap down against a bullish bias"
                };
            }

            return null;
        }

        private TradingSignalResult? ProcessS6WeaknessConfirmed(WeekContext context)
        {
            if (context.FirstBar == null || context.CurrentBar == null || context.PreviousWeek == null)
                return null;

            var firstBar = context.FirstBar;
            var currentBar = context.CurrentBar;
            var prevWeek = context.PreviousWeek;

            // S6 Base conditions
            var baseCondition1 = context.WeeklySig == -1;
            var baseCondition2 = firstBar.High >= (double)context.UpperZoneBottom;
            var baseCondition3 = firstBar.Close <= (double)context.UpperZoneTop;
            var baseCondition4 = firstBar.Close <= prevWeek.High;

            if (!(baseCondition1 && baseCondition2 && baseCondition3 && baseCondition4))
                return null;

            var isSecondBar = context.CurrentWeekBars.Count >= 2;
            
            // Scenario A: Inside candle on the 2nd bar
            var scenarioA = isSecondBar && 
                           currentBar.Close < firstBar.High && 
                           currentBar.Close < (double)context.UpperZoneBottom;

            // Scenario B: Breakdown below the 1st bar's low
            var scenarioB = currentBar.Close < firstBar.Low && 
                           currentBar.Close < (double)context.UpperZoneBottom;

            if (scenarioA || scenarioB)
            {
                var stopLossPrice = (decimal)prevWeek.High;
                var strikePrice = RoundTo100(stopLossPrice);

                return new TradingSignalResult
                {
                    SignalId = "S6",
                    SignalName = "Weakness Confirmed",
                    Direction = -1, // Bearish
                    StrikePrice = strikePrice,
                    OptionType = "CE",
                    Action = "Entry",
                    StopLossPrice = stopLossPrice,
                    Timestamp = DateTime.Now,
                    Confidence = 0.75m,
                    Description = "Triggers if bias is bearish, the first bar tests/fails at resistance, and the second bar confirms weakness"
                };
            }

            return null;
        }

        private TradingSignalResult? ProcessS7BreakoutConfirmed(WeekContext context)
        {
            if (context.FirstHour == null || context.CurrentBar == null || context.PreviousWeek == null)
                return null;

            var firstHour = context.FirstHour;
            var currentBar = context.CurrentBar;
            var prevWeek = context.PreviousWeek;

            // S7 Logic: 1H Breakout Confirmed
            var s4Trigger = ProcessS4Logic(context);
            var isTooCloseBelowResistance = currentBar.Close < prevWeek.High && 
                                          ((prevWeek.High - currentBar.Close) / currentBar.Close * 100) < 0.40;
            var isValidLocation = !isTooCloseBelowResistance;

            if (s4Trigger && isValidLocation)
            {
                var stopLossPrice = (decimal)firstHour.Low;
                var strikePrice = RoundTo100(stopLossPrice);

                return new TradingSignalResult
                {
                    SignalId = "S7",
                    SignalName = "1H Breakout Confirmed",
                    Direction = 1, // Bullish
                    StrikePrice = strikePrice,
                    OptionType = "PE",
                    Action = "Entry",
                    StopLossPrice = stopLossPrice,
                    Timestamp = DateTime.Now,
                    Confidence = 0.72m,
                    Description = "Shows a pure 1H breakout based on the S4 engine, but without bias or gap conditions"
                };
            }

            return null;
        }

        private TradingSignalResult? ProcessS8BreakdownConfirmed(WeekContext context)
        {
            if (context.FirstHour == null || context.CurrentBar == null)
                return null;

            var firstHour = context.FirstHour;
            var currentBar = context.CurrentBar;

            // S8 Logic: 1H Breakdown Confirmed
            var s8Trigger = ProcessS8Logic(context);
            var touchedUpperZone = currentBar.High >= (double)context.UpperZoneBottom;
            var closedBelowResistance = currentBar.Close < (double)context.UpperZoneBottom;

            if (s8Trigger && touchedUpperZone && closedBelowResistance)
            {
                var stopLossPrice = (decimal)firstHour.High;
                var strikePrice = RoundTo100(stopLossPrice);

                return new TradingSignalResult
                {
                    SignalId = "S8",
                    SignalName = "1H Breakdown Confirmed",
                    Direction = -1, // Bearish
                    StrikePrice = strikePrice,
                    OptionType = "CE",
                    Action = "Entry",
                    StopLossPrice = stopLossPrice,
                    Timestamp = DateTime.Now,
                    Confidence = 0.72m,
                    Description = "Shows a pure 1H breakdown, the bearish counterpart to S7"
                };
            }

            return null;
        }

        private bool ProcessS4Logic(WeekContext context)
        {
            if (context.FirstHour == null || context.CurrentWeekBars.Count < 2)
                return false;

            var firstHour = context.FirstHour;
            var currentBar = context.CurrentBar;
            var weeklyHigh = context.CurrentWeekBars.Max(b => b.High);

            // S4 Logic implementation
            var isDayOne = context.CurrentWeekBars.Count == 1;
            if (isDayOne)
            {
                return currentBar.Close > firstHour.High;
            }
            else
            {
                var isGreen = currentBar.Close > currentBar.Open;
                var isAboveFirstHourHigh = currentBar.Close > firstHour.High;
                var isNewWeeklyHigh = currentBar.High >= weeklyHigh;

                return isGreen && isAboveFirstHourHigh && isNewWeeklyHigh;
            }
        }

        private bool ProcessS8Logic(WeekContext context)
        {
            if (context.FirstHour == null || context.CurrentWeekBars.Count < 2)
                return false;

            var firstHour = context.FirstHour;
            var currentBar = context.CurrentBar;
            var weeklyLow = context.CurrentWeekBars.Min(b => b.Low);

            // S8 Logic implementation
            var isDayOne = context.CurrentWeekBars.Count == 1;
            if (isDayOne)
            {
                return currentBar.Close < firstHour.Low;
            }
            else
            {
                var isRed = currentBar.Close < currentBar.Open;
                var isBelowFirstHourLow = currentBar.Close < firstHour.Low;
                var isNewWeeklyLow = currentBar.Low <= weeklyLow;

                return isRed && isBelowFirstHourLow && isNewWeeklyLow;
            }
        }

        private DateTime GetCurrentWeekStart()
        {
            var today = DateTime.Now.Date;
            var daysFromMonday = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysFromMonday < 0) daysFromMonday += 7;
            return today.AddDays(-daysFromMonday);
        }

        private decimal RoundTo100(decimal price)
        {
            return Math.Round(price / 100) * 100;
        }

        private List<WeeklyPriceData> ConvertToWeeklyData(List<OptionsHistoricalData> data)
        {
            return data.GroupBy(d => GetWeekStart(d.Timestamp))
                      .Select(g => new WeeklyPriceData
                      {
                          WeekStart = g.Key,
                          High = (double)g.Max(x => x.High),
                          Low = (double)g.Min(x => x.Low),
                          Close = (double)g.OrderBy(x => x.Timestamp).Last().Close,
                          Max4HBody = (double)g.Max(x => Math.Max(x.Open, x.Close)),
                          Min4HBody = (double)g.Min(x => Math.Min(x.Open, x.Close))
                      })
                      .OrderBy(w => w.WeekStart)
                      .ToList();
        }

        private List<HourlyPriceData> ConvertToHourlyData(List<OptionsHistoricalData> data)
        {
            return data.Select(d => new HourlyPriceData
            {
                Timestamp = d.Timestamp,
                Open = (double)d.Open,
                High = (double)d.High,
                Low = (double)d.Low,
                Close = (double)d.Close,
                Volume = d.Volume
            }).ToList();
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var daysFromMonday = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysFromMonday < 0) daysFromMonday += 7;
            return date.Date.AddDays(-daysFromMonday);
        }
    }

    // Supporting classes
    public class WeeklyPriceData
    {
        public DateTime WeekStart { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double Max4HBody { get; set; }
        public double Min4HBody { get; set; }
    }

    public class HourlyPriceData
    {
        public DateTime Timestamp { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public long Volume { get; set; }
    }

    public class WeekContext
    {
        public WeeklyPriceData? PreviousWeek { get; set; }
        public List<HourlyPriceData> CurrentWeekBars { get; set; } = new();
        public HourlyPriceData? FirstBar { get; set; }
        public HourlyPriceData? FirstHour { get; set; }
        public HourlyPriceData? CurrentBar { get; set; }
        public decimal UpperZoneTop { get; set; }
        public decimal UpperZoneBottom { get; set; }
        public decimal LowerZoneTop { get; set; }
        public decimal LowerZoneBottom { get; set; }
        public int WeeklySig { get; set; } // -1=bear, 1=bull
        public decimal MarginLow { get; set; }
        public decimal MarginHigh { get; set; }
        public DateTime CurrentWeekStart { get; set; }
    }

    public class TradingSignalResult
    {
        public string SignalId { get; set; } = string.Empty;
        public string SignalName { get; set; } = string.Empty;
        public int Direction { get; set; } // 1=Bullish, -1=Bearish
        public decimal StrikePrice { get; set; }
        public string OptionType { get; set; } = string.Empty; // CE or PE
        public string Action { get; set; } = string.Empty; // Entry or Stoploss
        public decimal StopLossPrice { get; set; }
        public DateTime Timestamp { get; set; }
        public decimal Confidence { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}