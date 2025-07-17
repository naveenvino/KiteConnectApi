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
    public class Pure1HSignalService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<Pure1HSignalService> _logger;

        public Pure1HSignalService(
            ApplicationDbContext context,
            ILogger<Pure1HSignalService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<Pure1HBacktestResult> RunPure1HBacktestAsync(Pure1HBacktestRequest request)
        {
            _logger.LogInformation("Starting pure 1H backtest from {FromDate} to {ToDate}", 
                request.FromDate, request.ToDate);

            var result = new Pure1HBacktestResult
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Strategy = "Pure 1H Candle-Based Option Selling",
                Trades = new List<Pure1HTrade>()
            };

            // Get all 1H candles for the period
            var candles = await Get1HCandlesAsync("NIFTY", request.FromDate, request.ToDate);
            if (!candles.Any())
            {
                _logger.LogWarning("No 1H candle data found for the specified period");
                return result;
            }

            // Process each candle sequentially
            var weekState = new WeekState();
            var currentWeekCandles = new List<Candle1H>();

            foreach (var candle in candles.OrderBy(c => c.Timestamp))
            {
                // Check if this is a new week
                if (IsNewWeek(candle.Timestamp, currentWeekCandles.LastOrDefault()?.Timestamp))
                {
                    // Process previous week if we have data
                    if (currentWeekCandles.Any())
                    {
                        var weekTrade = await ProcessCompletedWeekAsync(currentWeekCandles, weekState, request);
                        if (weekTrade != null)
                        {
                            result.Trades.Add(weekTrade);
                        }
                    }

                    // Start new week
                    StartNewWeek(currentWeekCandles, weekState, candle);
                }

                // Add candle to current week
                currentWeekCandles.Add(candle);
                
                // Update weekly tracking with this candle
                UpdateWeeklyTracking(weekState, candle, currentWeekCandles.Count);

                // Check for signals on this candle (if no signal fired this week)
                if (!weekState.SignalFired)
                {
                    var signal = CheckSignalsOnCandle(candle, currentWeekCandles, weekState);
                    if (signal != null)
                    {
                        var trade = await ExecuteSignalTradeAsync(signal, candle, currentWeekCandles, weekState, request);
                        if (trade != null)
                        {
                            result.Trades.Add(trade);
                            weekState.SignalFired = true;
                        }
                    }
                }
            }

            // Process final week if exists
            if (currentWeekCandles.Any() && !weekState.SignalFired)
            {
                var weekTrade = await ProcessCompletedWeekAsync(currentWeekCandles, weekState, request);
                if (weekTrade != null)
                {
                    result.Trades.Add(weekTrade);
                }
            }

            CalculatePerformanceMetrics(result);
            return result;
        }

        private async Task<List<Candle1H>> Get1HCandlesAsync(string symbol, DateTime fromDate, DateTime toDate)
        {
            var data = await _context.OptionsHistoricalData
                .Where(d => d.Underlying == symbol && 
                           d.Timestamp >= fromDate && 
                           d.Timestamp <= toDate &&
                           d.Interval == "60minute")
                .OrderBy(d => d.Timestamp)
                .ToListAsync();

            return data.Select(d => new Candle1H
            {
                Timestamp = d.Timestamp,
                Open = d.Open,
                High = d.High,
                Low = d.Low,
                Close = d.Close,
                Volume = d.Volume
            }).ToList();
        }

        private bool IsNewWeek(DateTime currentCandle, DateTime? previousCandle)
        {
            if (!previousCandle.HasValue) return true;

            var currentWeekStart = GetWeekStart(currentCandle);
            var previousWeekStart = GetWeekStart(previousCandle.Value);

            return currentWeekStart != previousWeekStart;
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var daysFromMonday = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysFromMonday < 0) daysFromMonday += 7;
            return date.Date.AddDays(-daysFromMonday);
        }

        private void StartNewWeek(List<Candle1H> currentWeekCandles, WeekState weekState, Candle1H firstCandle)
        {
            // Calculate zones from previous week data if available
            if (currentWeekCandles.Any())
            {
                CalculateZonesFromPreviousWeek(currentWeekCandles, weekState);
            }

            // Reset week state
            currentWeekCandles.Clear();
            weekState.Reset();
            weekState.FirstCandle = firstCandle;
            weekState.WeekStart = GetWeekStart(firstCandle.Timestamp);
        }

        private void CalculateZonesFromPreviousWeek(List<Candle1H> previousWeekCandles, WeekState weekState)
        {
            if (!previousWeekCandles.Any()) return;

            // Aggregate weekly data from 1H candles
            var weeklyHigh = previousWeekCandles.Max(c => c.High);
            var weeklyLow = previousWeekCandles.Min(c => c.Low);
            var weeklyClose = previousWeekCandles.OrderBy(c => c.Timestamp).Last().Close;

            // Calculate 4H bodies from 1H candles
            var fourHBodies = Calculate4HBodiesFrom1H(previousWeekCandles);
            var max4HBody = fourHBodies.Any() ? fourHBodies.Max(b => b.Top) : weeklyHigh;
            var min4HBody = fourHBodies.Any() ? fourHBodies.Min(b => b.Bottom) : weeklyLow;

            // Calculate zones
            weekState.PreviousWeek = new WeeklyData
            {
                High = weeklyHigh,
                Low = weeklyLow,
                Close = weeklyClose,
                Max4HBody = max4HBody,
                Min4HBody = min4HBody
            };

            weekState.UpperZone = new Zone
            {
                Top = Math.Max(weeklyHigh, max4HBody),
                Bottom = Math.Min(weeklyHigh, max4HBody)
            };

            weekState.LowerZone = new Zone
            {
                Top = Math.Max(weeklyLow, min4HBody),
                Bottom = Math.Min(weeklyLow, min4HBody)
            };

            // Calculate margins
            weekState.MarginHigh = Math.Max((weekState.UpperZone.Top - weekState.UpperZone.Bottom) * 3, 0.05m);
            weekState.MarginLow = Math.Max((weekState.LowerZone.Top - weekState.LowerZone.Bottom) * 3, 0.05m);

            // Calculate weekly bias
            var distanceToHigh = Math.Abs(weeklyClose - max4HBody);
            var distanceToLow = Math.Abs(weeklyClose - min4HBody);
            weekState.WeeklyBias = distanceToHigh < distanceToLow ? -1 : 1; // -1=bearish, 1=bullish
        }

        private List<FourHBody> Calculate4HBodiesFrom1H(List<Candle1H> candles)
        {
            var bodies = new List<FourHBody>();
            
            for (int i = 0; i < candles.Count; i += 4)
            {
                var fourHCandles = candles.Skip(i).Take(4).ToList();
                if (fourHCandles.Count == 4)
                {
                    var open4H = fourHCandles.First().Open;
                    var close4H = fourHCandles.Last().Close;
                    
                    bodies.Add(new FourHBody
                    {
                        Top = Math.Max(open4H, close4H),
                        Bottom = Math.Min(open4H, close4H)
                    });
                }
            }
            
            return bodies;
        }

        private void UpdateWeeklyTracking(WeekState weekState, Candle1H candle, int barsSinceWeekStart)
        {
            weekState.BarsSinceWeekStart = barsSinceWeekStart;
            weekState.WeeklyHigh = Math.Max(weekState.WeeklyHigh, candle.High);
            weekState.WeeklyLow = Math.Min(weekState.WeeklyLow == 0 ? candle.Low : weekState.WeeklyLow, candle.Low);
            weekState.WeeklyMaxClose = Math.Max(weekState.WeeklyMaxClose, candle.Close);
            weekState.WeeklyMinClose = Math.Min(weekState.WeeklyMinClose == 0 ? candle.Close : weekState.WeeklyMinClose, candle.Close);
        }

        private Pure1HSignal? CheckSignalsOnCandle(Candle1H candle, List<Candle1H> weekCandles, WeekState weekState)
        {
            if (weekState.PreviousWeek == null || weekState.FirstCandle == null) return null;

            var barsSinceWeekStart = weekState.BarsSinceWeekStart;

            // S1 and S2: Only on second candle (barsSinceWeekStart == 2)
            if (barsSinceWeekStart == 2)
            {
                var s1Signal = CheckS1BearTrap(candle, weekState);
                if (s1Signal != null) return s1Signal;

                var s2Signal = CheckS2SupportHold(candle, weekState);
                if (s2Signal != null) return s2Signal;
            }

            // S3-S8: Any candle after second (barsSinceWeekStart > 2)
            if (barsSinceWeekStart > 2)
            {
                var signals = new[]
                {
                    CheckS3ResistanceHold(candle, weekCandles, weekState),
                    CheckS4BiasFailure(candle, weekCandles, weekState),
                    CheckS5BiasFailureBearish(candle, weekCandles, weekState),
                    CheckS6WeaknessConfirmed(candle, weekCandles, weekState),
                    CheckS7BreakoutConfirmed(candle, weekCandles, weekState),
                    CheckS8BreakdownConfirmed(candle, weekCandles, weekState)
                };

                foreach (var signal in signals)
                {
                    if (signal != null) return signal;
                }
            }

            return null;
        }

        private Pure1HSignal? CheckS1BearTrap(Candle1H currentCandle, WeekState weekState)
        {
            var firstCandle = weekState.FirstCandle!;
            var lowerZoneBottom = weekState.LowerZone.Bottom;

            // S1 Logic: Bear Trap
            var condition1 = firstCandle.Open >= lowerZoneBottom;
            var condition2 = firstCandle.Close < lowerZoneBottom;
            var condition3 = currentCandle.Close > firstCandle.Low;

            if (condition1 && condition2 && condition3)
            {
                var stopLoss = firstCandle.Low - Math.Abs(firstCandle.Open - firstCandle.Close);
                var strike = RoundTo100(stopLoss);

                return new Pure1HSignal
                {
                    SignalId = "S1",
                    SignalName = "Bear Trap",
                    Timestamp = currentCandle.Timestamp,
                    Direction = 1, // Bullish
                    OptionType = "PE",
                    StrikePrice = strike,
                    StopLossPrice = stopLoss,
                    Confidence = 0.8m
                };
            }

            return null;
        }

        private Pure1HSignal? CheckS2SupportHold(Candle1H currentCandle, WeekState weekState)
        {
            var firstCandle = weekState.FirstCandle!;
            var prevWeek = weekState.PreviousWeek!;
            var lowerZoneBottom = weekState.LowerZone.Bottom;
            var marginLow = weekState.MarginLow;

            // S2 Logic: Support Hold (Bullish)
            var condition1 = firstCandle.Open > prevWeek.Low;
            var condition2 = Math.Abs(prevWeek.Close - lowerZoneBottom) <= marginLow;
            var condition3 = Math.Abs(firstCandle.Open - lowerZoneBottom) <= marginLow;
            var condition4 = firstCandle.Close >= lowerZoneBottom;
            var condition5 = firstCandle.Close >= prevWeek.Close;
            var condition6 = currentCandle.Close >= firstCandle.Low;
            var condition7 = currentCandle.Close > prevWeek.Close;
            var condition8 = currentCandle.Close > lowerZoneBottom;
            var condition9 = weekState.WeeklyBias == 1; // Bullish bias

            if (condition1 && condition2 && condition3 && condition4 && condition5 && 
                condition6 && condition7 && condition8 && condition9)
            {
                return new Pure1HSignal
                {
                    SignalId = "S2",
                    SignalName = "Support Hold (Bullish)",
                    Timestamp = currentCandle.Timestamp,
                    Direction = 1, // Bullish
                    OptionType = "PE",
                    StrikePrice = RoundTo100(lowerZoneBottom),
                    StopLossPrice = lowerZoneBottom,
                    Confidence = 0.85m
                };
            }

            return null;
        }

        private Pure1HSignal? CheckS3ResistanceHold(Candle1H currentCandle, List<Candle1H> weekCandles, WeekState weekState)
        {
            var firstCandle = weekState.FirstCandle!;
            var prevWeek = weekState.PreviousWeek!;
            var upperZoneBottom = weekState.UpperZone.Bottom;
            var marginHigh = weekState.MarginHigh;

            // S3 Base conditions
            var baseCondition1 = weekState.WeeklyBias == -1; // Bearish bias
            var baseCondition2 = Math.Abs(prevWeek.Close - upperZoneBottom) <= marginHigh;
            var baseCondition3 = Math.Abs(firstCandle.Open - upperZoneBottom) <= marginHigh;
            var baseCondition4 = firstCandle.Close <= prevWeek.High;

            if (!(baseCondition1 && baseCondition2 && baseCondition3 && baseCondition4))
                return null;

            var isSecondCandle = weekState.BarsSinceWeekStart == 2;

            // Scenario A: Inside candle on the 2nd bar
            var scenarioA = isSecondCandle && 
                           currentCandle.Close < firstCandle.High && 
                           currentCandle.Close < upperZoneBottom && 
                           (firstCandle.High >= upperZoneBottom || currentCandle.High >= upperZoneBottom);

            // Scenario B: Breakdown below the 1st bar's low
            var scenarioB = currentCandle.Close < firstCandle.Low && 
                           currentCandle.Close < upperZoneBottom;

            if (scenarioA || scenarioB)
            {
                return new Pure1HSignal
                {
                    SignalId = "S3",
                    SignalName = "Resistance Hold (Bearish)",
                    Timestamp = currentCandle.Timestamp,
                    Direction = -1, // Bearish
                    OptionType = "CE",
                    StrikePrice = RoundTo100(prevWeek.High),
                    StopLossPrice = prevWeek.High,
                    Confidence = 0.82m
                };
            }

            return null;
        }

        private Pure1HSignal? CheckS4BiasFailure(Candle1H currentCandle, List<Candle1H> weekCandles, WeekState weekState)
        {
            var firstCandle = weekState.FirstCandle!;
            var upperZoneTop = weekState.UpperZone.Top;

            // S4 Logic: Bias Failure (Bullish)
            var condition1 = weekState.WeeklyBias == -1; // Bearish bias
            var condition2 = firstCandle.Open > upperZoneTop; // Gap up

            if (!(condition1 && condition2)) return null;

            // Breakout logic based on day
            var isDay1 = weekState.BarsSinceWeekStart <= 24; // First 24 hours
            bool breakoutTriggered = false;

            if (isDay1)
            {
                breakoutTriggered = currentCandle.Close > firstCandle.High;
            }
            else
            {
                // Day 2+: More complex breakout logic
                var isGreen = currentCandle.Close > currentCandle.Open;
                var isAboveFirstHigh = currentCandle.Close > firstCandle.High;
                var isNewWeeklyHigh = currentCandle.High >= weekState.WeeklyHigh;

                breakoutTriggered = isGreen && isAboveFirstHigh && isNewWeeklyHigh;
            }

            if (breakoutTriggered)
            {
                return new Pure1HSignal
                {
                    SignalId = "S4",
                    SignalName = "Bias Failure (Bullish)",
                    Timestamp = currentCandle.Timestamp,
                    Direction = 1, // Bullish
                    OptionType = "PE",
                    StrikePrice = RoundTo100(firstCandle.Low),
                    StopLossPrice = firstCandle.Low,
                    Confidence = 0.78m
                };
            }

            return null;
        }

        private Pure1HSignal? CheckS5BiasFailureBearish(Candle1H currentCandle, List<Candle1H> weekCandles, WeekState weekState)
        {
            var firstCandle = weekState.FirstCandle!;
            var prevWeek = weekState.PreviousWeek!;
            var lowerZoneBottom = weekState.LowerZone.Bottom;

            // S5 Logic: Bias Failure (Bearish)
            var condition1 = weekState.WeeklyBias == 1; // Bullish bias
            var condition2 = firstCandle.Open < lowerZoneBottom; // Gap down
            var condition3 = firstCandle.Close < lowerZoneBottom;
            var condition4 = firstCandle.Close < prevWeek.Low;
            var condition5 = currentCandle.Close < firstCandle.Low;

            if (condition1 && condition2 && condition3 && condition4 && condition5)
            {
                return new Pure1HSignal
                {
                    SignalId = "S5",
                    SignalName = "Bias Failure (Bearish)",
                    Timestamp = currentCandle.Timestamp,
                    Direction = -1, // Bearish
                    OptionType = "CE",
                    StrikePrice = RoundTo100(firstCandle.High),
                    StopLossPrice = firstCandle.High,
                    Confidence = 0.78m
                };
            }

            return null;
        }

        private Pure1HSignal? CheckS6WeaknessConfirmed(Candle1H currentCandle, List<Candle1H> weekCandles, WeekState weekState)
        {
            var firstCandle = weekState.FirstCandle!;
            var prevWeek = weekState.PreviousWeek!;
            var upperZoneBottom = weekState.UpperZone.Bottom;
            var upperZoneTop = weekState.UpperZone.Top;

            // S6 Base conditions
            var baseCondition1 = weekState.WeeklyBias == -1; // Bearish bias
            var baseCondition2 = firstCandle.High >= upperZoneBottom;
            var baseCondition3 = firstCandle.Close <= upperZoneTop;
            var baseCondition4 = firstCandle.Close <= prevWeek.High;

            if (!(baseCondition1 && baseCondition2 && baseCondition3 && baseCondition4))
                return null;

            var isSecondCandle = weekState.BarsSinceWeekStart == 2;

            // Same scenarios as S3
            var scenarioA = isSecondCandle && 
                           currentCandle.Close < firstCandle.High && 
                           currentCandle.Close < upperZoneBottom;

            var scenarioB = currentCandle.Close < firstCandle.Low && 
                           currentCandle.Close < upperZoneBottom;

            if (scenarioA || scenarioB)
            {
                return new Pure1HSignal
                {
                    SignalId = "S6",
                    SignalName = "Weakness Confirmed",
                    Timestamp = currentCandle.Timestamp,
                    Direction = -1, // Bearish
                    OptionType = "CE",
                    StrikePrice = RoundTo100(prevWeek.High),
                    StopLossPrice = prevWeek.High,
                    Confidence = 0.75m
                };
            }

            return null;
        }

        private Pure1HSignal? CheckS7BreakoutConfirmed(Candle1H currentCandle, List<Candle1H> weekCandles, WeekState weekState)
        {
            var firstCandle = weekState.FirstCandle!;
            var prevWeek = weekState.PreviousWeek!;

            // S7 Logic: Same as S4 but with location validation
            var s4Logic = CheckS4BiasFailure(currentCandle, weekCandles, weekState);
            if (s4Logic == null) return null;

            // Location validation
            var isTooCloseBelowResistance = currentCandle.Close < prevWeek.High && 
                                          ((prevWeek.High - currentCandle.Close) / currentCandle.Close * 100) < 0.40m;

            if (!isTooCloseBelowResistance)
            {
                return new Pure1HSignal
                {
                    SignalId = "S7",
                    SignalName = "1H Breakout Confirmed",
                    Timestamp = currentCandle.Timestamp,
                    Direction = 1, // Bullish
                    OptionType = "PE",
                    StrikePrice = RoundTo100(firstCandle.Low),
                    StopLossPrice = firstCandle.Low,
                    Confidence = 0.72m
                };
            }

            return null;
        }

        private Pure1HSignal? CheckS8BreakdownConfirmed(Candle1H currentCandle, List<Candle1H> weekCandles, WeekState weekState)
        {
            var firstCandle = weekState.FirstCandle!;
            var upperZoneBottom = weekState.UpperZone.Bottom;

            // S8 Logic: Breakdown with zone interaction
            var isDay1 = weekState.BarsSinceWeekStart <= 24;
            bool breakdownTriggered = false;

            if (isDay1)
            {
                breakdownTriggered = currentCandle.Close < firstCandle.Low;
            }
            else
            {
                var isRed = currentCandle.Close < currentCandle.Open;
                var isBelowFirstLow = currentCandle.Close < firstCandle.Low;
                var isNewWeeklyLow = currentCandle.Low <= weekState.WeeklyLow;

                breakdownTriggered = isRed && isBelowFirstLow && isNewWeeklyLow;
            }

            // Zone interaction requirements
            var touchedUpperZone = weekCandles.Any(c => c.High >= upperZoneBottom);
            var closedBelowResistance = currentCandle.Close < upperZoneBottom;

            if (breakdownTriggered && touchedUpperZone && closedBelowResistance)
            {
                return new Pure1HSignal
                {
                    SignalId = "S8",
                    SignalName = "1H Breakdown Confirmed",
                    Timestamp = currentCandle.Timestamp,
                    Direction = -1, // Bearish
                    OptionType = "CE",
                    StrikePrice = RoundTo100(firstCandle.High),
                    StopLossPrice = firstCandle.High,
                    Confidence = 0.72m
                };
            }

            return null;
        }

        private async Task<Pure1HTrade?> ExecuteSignalTradeAsync(
            Pure1HSignal signal, 
            Candle1H triggerCandle, 
            List<Candle1H> weekCandles, 
            WeekState weekState, 
            Pure1HBacktestRequest request)
        {
            try
            {
                var thursdayExpiry = GetThursdayExpiry(weekState.WeekStart);
                
                // Generate trading symbols
                var mainSymbol = GenerateTradingSymbol("NIFTY", thursdayExpiry, (int)signal.StrikePrice, signal.OptionType);
                var hedgeStrike = CalculateHedgeStrike((int)signal.StrikePrice, signal.OptionType, request.HedgePoints);
                var hedgeSymbol = GenerateTradingSymbol("NIFTY", thursdayExpiry, hedgeStrike, signal.OptionType);

                // Get entry prices
                var mainEntryData = await GetOptionsDataAtTimestampAsync(mainSymbol, signal.Timestamp);
                var hedgeEntryData = await GetOptionsDataAtTimestampAsync(hedgeSymbol, signal.Timestamp);

                if (mainEntryData == null || hedgeEntryData == null)
                {
                    _logger.LogWarning("No entry data for {MainSymbol} or {HedgeSymbol} at {Timestamp}", 
                        mainSymbol, hedgeSymbol, signal.Timestamp);
                    return null;
                }

                var trade = new Pure1HTrade
                {
                    WeekStart = weekState.WeekStart,
                    SignalId = signal.SignalId,
                    SignalName = signal.SignalName,
                    TriggerCandle = signal.Timestamp,
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

                // Monitor for exit (continue processing remaining candles in the dataset)
                var exitResult = await MonitorTradeExit(trade, weekCandles, signal.Timestamp, thursdayExpiry);
                
                trade.ExitCandle = exitResult.ExitTime;
                trade.MainExitPrice = exitResult.MainExitPrice;
                trade.HedgeExitPrice = exitResult.HedgeExitPrice;
                trade.ExitReason = exitResult.ExitReason;
                
                // Calculate P&L
                var pnl = CalculatePnL(trade);
                trade.NetPnL = pnl.NetPnL;
                trade.MainPnL = pnl.MainPnL;
                trade.HedgePnL = pnl.HedgePnL;
                trade.Success = trade.NetPnL > 0;

                return trade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing 1H trade for {SignalId}", signal.SignalId);
                return null;
            }
        }

        private async Task<Pure1HExitResult> MonitorTradeExit(
            Pure1HTrade trade, 
            List<Candle1H> weekCandles, 
            DateTime entryTime, 
            DateTime thursdayExpiry)
        {
            // Find candles after entry for monitoring
            var monitoringCandles = weekCandles.Where(c => c.Timestamp > entryTime).ToList();

            foreach (var candle in monitoringCandles)
            {
                // Get current option prices
                var mainData = await GetOptionsDataAtTimestampAsync(trade.MainSymbol, candle.Timestamp);
                var hedgeData = await GetOptionsDataAtTimestampAsync(trade.HedgeSymbol, candle.Timestamp);

                if (mainData != null && hedgeData != null)
                {
                    // Check for stop loss (price increase for option selling)
                    if (mainData.LastPrice >= trade.StopLossLevel)
                    {
                        return new Pure1HExitResult
                        {
                            ExitTime = candle.Timestamp,
                            MainExitPrice = mainData.LastPrice,
                            HedgeExitPrice = hedgeData.LastPrice,
                            ExitReason = "STOP_LOSS"
                        };
                    }
                }

                // Check if we've reached Thursday expiry
                if (candle.Timestamp >= thursdayExpiry)
                {
                    break;
                }
            }

            // Exit at expiry
            var finalMainData = await GetOptionsDataAtTimestampAsync(trade.MainSymbol, thursdayExpiry);
            var finalHedgeData = await GetOptionsDataAtTimestampAsync(trade.HedgeSymbol, thursdayExpiry);

            return new Pure1HExitResult
            {
                ExitTime = thursdayExpiry,
                MainExitPrice = finalMainData?.LastPrice ?? 0.1m, // Expires worthless
                HedgeExitPrice = finalHedgeData?.LastPrice ?? 0.1m,
                ExitReason = "EXPIRY_WIN"
            };
        }

        private async Task<Pure1HTrade?> ProcessCompletedWeekAsync(
            List<Candle1H> weekCandles, 
            WeekState weekState, 
            Pure1HBacktestRequest request)
        {
            // This method would handle any end-of-week processing if needed
            return null;
        }

        private decimal RoundTo100(decimal price)
        {
            return Math.Round(price / 100) * 100;
        }

        private DateTime GetThursdayExpiry(DateTime weekStart)
        {
            return weekStart.AddDays(3).Date.AddHours(15.5); // Thursday 3:30 PM
        }

        private int CalculateHedgeStrike(int mainStrike, string optionType, int hedgePoints)
        {
            return optionType == "CE" ? mainStrike + hedgePoints : mainStrike - hedgePoints;
        }

        private string GenerateTradingSymbol(string underlying, DateTime expiry, int strike, string optionType)
        {
            var year = expiry.ToString("yy");
            var month = expiry.Month.ToString();
            var day = expiry.Day.ToString("D2");
            return $"{underlying}{year}{month}{day}{strike}{optionType.ToUpper()}";
        }

        private async Task<OptionsHistoricalData?> GetOptionsDataAtTimestampAsync(string tradingSymbol, DateTime timestamp)
        {
            return await _context.OptionsHistoricalData
                .Where(d => d.TradingSymbol == tradingSymbol && 
                           d.Timestamp <= timestamp.AddMinutes(30) && 
                           d.Timestamp >= timestamp.AddMinutes(-30))
                .OrderBy(d => Math.Abs((d.Timestamp - timestamp).Ticks))
                .FirstOrDefaultAsync();
        }

        private (decimal NetPnL, decimal MainPnL, decimal HedgePnL) CalculatePnL(Pure1HTrade trade)
        {
            // Option selling P&L: Sell high, buy low = profit
            var mainPnL = (trade.MainEntryPrice - trade.MainExitPrice) * trade.Quantity;
            var hedgePnL = (trade.HedgeExitPrice - trade.HedgeEntryPrice) * trade.Quantity;
            var netPnL = mainPnL + hedgePnL;

            return (netPnL, mainPnL, hedgePnL);
        }

        private void CalculatePerformanceMetrics(Pure1HBacktestResult result)
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
            result.InitialCapital = 100000; // Default
            result.FinalCapital = result.InitialCapital + result.TotalPnL;

            // Signal breakdown
            result.SignalBreakdown = result.Trades
                .GroupBy(t => t.SignalId)
                .ToDictionary(g => g.Key, g => new Pure1HSignalStats
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
    public class Pure1HBacktestRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal InitialCapital { get; set; } = 100000;
        public int LotSize { get; set; } = 50;
        public int HedgePoints { get; set; } = 300;
    }

    public class Pure1HBacktestResult
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
        public List<Pure1HTrade> Trades { get; set; } = new();
        public Dictionary<string, Pure1HSignalStats> SignalBreakdown { get; set; } = new();
    }

    public class Candle1H
    {
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
    }

    public class WeeklyData
    {
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Max4HBody { get; set; }
        public decimal Min4HBody { get; set; }
    }

    public class Zone
    {
        public decimal Top { get; set; }
        public decimal Bottom { get; set; }
    }

    public class FourHBody
    {
        public decimal Top { get; set; }
        public decimal Bottom { get; set; }
    }

    public class WeekState
    {
        public DateTime WeekStart { get; set; }
        public WeeklyData? PreviousWeek { get; set; }
        public Candle1H? FirstCandle { get; set; }
        public Zone UpperZone { get; set; } = new();
        public Zone LowerZone { get; set; } = new();
        public decimal MarginHigh { get; set; }
        public decimal MarginLow { get; set; }
        public int WeeklyBias { get; set; } // -1=bearish, 1=bullish
        public int BarsSinceWeekStart { get; set; }
        public decimal WeeklyHigh { get; set; }
        public decimal WeeklyLow { get; set; }
        public decimal WeeklyMaxClose { get; set; }
        public decimal WeeklyMinClose { get; set; }
        public bool SignalFired { get; set; }

        public void Reset()
        {
            BarsSinceWeekStart = 0;
            WeeklyHigh = 0;
            WeeklyLow = 0;
            WeeklyMaxClose = 0;
            WeeklyMinClose = 0;
            SignalFired = false;
        }
    }

    public class Pure1HSignal
    {
        public string SignalId { get; set; } = string.Empty;
        public string SignalName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int Direction { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public decimal StrikePrice { get; set; }
        public decimal StopLossPrice { get; set; }
        public decimal Confidence { get; set; }
    }

    public class Pure1HTrade
    {
        public DateTime WeekStart { get; set; }
        public string SignalId { get; set; } = string.Empty;
        public string SignalName { get; set; } = string.Empty;
        public DateTime TriggerCandle { get; set; }
        public DateTime ExitCandle { get; set; }
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
        public string ExitReason { get; set; } = string.Empty;
        public bool Success { get; set; }
    }

    public class Pure1HExitResult
    {
        public DateTime ExitTime { get; set; }
        public decimal MainExitPrice { get; set; }
        public decimal HedgeExitPrice { get; set; }
        public string ExitReason { get; set; } = string.Empty;
    }

    public class Pure1HSignalStats
    {
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public double WinRate { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal AveragePnL { get; set; }
    }
}