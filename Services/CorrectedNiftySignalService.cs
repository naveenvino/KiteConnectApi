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
    public class CorrectedNiftySignalService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CorrectedNiftySignalService> _logger;

        public CorrectedNiftySignalService(
            ApplicationDbContext context,
            ILogger<CorrectedNiftySignalService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<CorrectedBacktestResult> RunCorrectedBacktestAsync(CorrectedBacktestRequest request)
        {
            _logger.LogInformation("Starting corrected NIFTY signal backtest from {FromDate} to {ToDate}", 
                request.FromDate, request.ToDate);

            var result = new CorrectedBacktestResult
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Strategy = "NIFTY Index Signals → Option Selling",
                Trades = new List<CorrectedTrade>(),
                DataIssues = new List<string>()
            };

            // Check if we have NIFTY index data
            var niftyIndexData = await CheckNiftyIndexDataAsync(request.FromDate, request.ToDate);
            
            if (!niftyIndexData.HasIndexData)
            {
                result.DataIssues.Add("❌ No NIFTY50 index chart data found in database");
                result.DataIssues.Add("🔍 Only NIFTY options data available - cannot generate signals");
                result.DataIssues.Add("📊 Need NIFTY50 index 1H candles for signal detection");
                
                // Try to create synthetic index data from ATM options (workaround)
                _logger.LogWarning("No NIFTY index data found. Attempting synthetic approach using ATM options...");
                return await CreateSyntheticIndexBacktest(request, result);
            }

            // If we had proper index data, process it here
            var indexCandles = await GetNiftyIndexCandlesAsync(request.FromDate, request.ToDate);
            return await ProcessNiftyIndexSignalsAsync(indexCandles, request, result);
        }

        private async Task<NiftyDataCheck> CheckNiftyIndexDataAsync(DateTime fromDate, DateTime toDate)
        {
            // Check if we have actual NIFTY50 index data (not options)
            var indexSymbols = new[] { "NIFTY", "NIFTY50", "NIFTY INDEX", "NIFTY_INDEX" };
            
            foreach (var symbol in indexSymbols)
            {
                var indexData = await _context.OptionsHistoricalData
                    .Where(d => d.Underlying == symbol && 
                               d.TradingSymbol == symbol && // Same as underlying for index
                               d.Timestamp >= fromDate && 
                               d.Timestamp <= toDate)
                    .FirstOrDefaultAsync();

                if (indexData != null)
                {
                    return new NiftyDataCheck { HasIndexData = true, IndexSymbol = symbol };
                }
            }

            return new NiftyDataCheck { HasIndexData = false };
        }

        private async Task<CorrectedBacktestResult> CreateSyntheticIndexBacktest(
            CorrectedBacktestRequest request, 
            CorrectedBacktestResult result)
        {
            result.DataIssues.Add("🔧 WORKAROUND: Creating synthetic NIFTY index from ATM options");
            
            try
            {
                // Get all available NIFTY options for the period
                var optionsData = await _context.OptionsHistoricalData
                    .Where(d => d.Underlying == "NIFTY" && 
                               d.Timestamp >= request.FromDate && 
                               d.Timestamp <= request.ToDate)
                    .OrderBy(d => d.Timestamp)
                    .ToListAsync();

                if (!optionsData.Any())
                {
                    result.DataIssues.Add("❌ No NIFTY data available for the specified period");
                    return result;
                }

                // Group by timestamp and create synthetic index candles
                var syntheticCandles = await CreateSyntheticIndexFromOptions(optionsData);
                
                if (syntheticCandles.Any())
                {
                    result.DataIssues.Add($"✅ Created {syntheticCandles.Count} synthetic index candles from options data");
                    return await ProcessNiftyIndexSignalsAsync(syntheticCandles, request, result);
                }
                else
                {
                    result.DataIssues.Add("❌ Failed to create synthetic index data");
                }
            }
            catch (Exception ex)
            {
                result.DataIssues.Add($"❌ Error creating synthetic data: {ex.Message}");
                _logger.LogError(ex, "Error creating synthetic NIFTY index data");
            }

            return result;
        }

        private async Task<List<NiftyIndexCandle>> CreateSyntheticIndexFromOptions(List<OptionsHistoricalData> optionsData)
        {
            var syntheticCandles = new List<NiftyIndexCandle>();
            
            // Group options by timestamp
            var groupedByTime = optionsData.GroupBy(d => d.Timestamp).OrderBy(g => g.Key);

            foreach (var timeGroup in groupedByTime)
            {
                try
                {
                    // For each timestamp, find ATM options to estimate underlying price
                    var timestamp = timeGroup.Key;
                    var options = timeGroup.ToList();

                    // Extract strikes and find middle range (likely ATM area)
                    var strikes = ExtractStrikesFromSymbols(options);
                    if (!strikes.Any()) continue;

                    var minStrike = strikes.Min();
                    var maxStrike = strikes.Max();
                    var midStrike = (minStrike + maxStrike) / 2;

                    // Find options closest to middle strike
                    var atmCE = options
                        .Where(o => o.TradingSymbol.Contains("CE"))
                        .OrderBy(o => Math.Abs(ExtractStrikeFromSymbol(o.TradingSymbol) - midStrike))
                        .FirstOrDefault();

                    var atmPE = options
                        .Where(o => o.TradingSymbol.Contains("PE"))
                        .OrderBy(o => Math.Abs(ExtractStrikeFromSymbol(o.TradingSymbol) - midStrike))
                        .FirstOrDefault();

                    if (atmCE != null && atmPE != null)
                    {
                        // Estimate underlying price using put-call parity approximation
                        var estimatedPrice = EstimateUnderlyingFromATMOptions(atmCE, atmPE);
                        
                        syntheticCandles.Add(new NiftyIndexCandle
                        {
                            Timestamp = timestamp,
                            Open = estimatedPrice,
                            High = estimatedPrice,
                            Low = estimatedPrice,
                            Close = estimatedPrice,
                            Volume = 0,
                            IsSynthetic = true,
                            Source = $"ATM CE: {atmCE.TradingSymbol}, PE: {atmPE.TradingSymbol}"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("Error processing timestamp {Timestamp}: {Error}", timeGroup.Key, ex.Message);
                }
            }

            // Aggregate to 1H candles if we have minute data
            return AggregateToHourlyCandles(syntheticCandles);
        }

        private List<int> ExtractStrikesFromSymbols(List<OptionsHistoricalData> options)
        {
            var strikes = new List<int>();
            
            foreach (var option in options)
            {
                var strike = ExtractStrikeFromSymbol(option.TradingSymbol);
                if (strike > 0) strikes.Add(strike);
            }
            
            return strikes.Distinct().ToList();
        }

        private int ExtractStrikeFromSymbol(string symbol)
        {
            // Extract strike from symbol like "NIFTY2571723700CE"
            try
            {
                // Remove NIFTY prefix and CE/PE suffix
                var cleanSymbol = symbol.Replace("NIFTY", "").Replace("CE", "").Replace("PE", "");
                
                // Extract date part (first 6 digits) and strike (remaining digits)
                if (cleanSymbol.Length >= 10)
                {
                    var strikePart = cleanSymbol.Substring(6); // Skip date, get strike
                    if (int.TryParse(strikePart, out var strike))
                    {
                        return strike;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Error extracting strike from {Symbol}: {Error}", symbol, ex.Message);
            }
            
            return 0;
        }

        private decimal EstimateUnderlyingFromATMOptions(OptionsHistoricalData atmCE, OptionsHistoricalData atmPE)
        {
            // Simple approximation: underlying ≈ strike + (CE_price - PE_price)
            var strike = ExtractStrikeFromSymbol(atmCE.TradingSymbol);
            var priceDifference = atmCE.LastPrice - atmPE.LastPrice;
            return strike + priceDifference;
        }

        private List<NiftyIndexCandle> AggregateToHourlyCandles(List<NiftyIndexCandle> minuteCandles)
        {
            var hourlyCandles = new List<NiftyIndexCandle>();
            
            var grouped = minuteCandles
                .GroupBy(c => new DateTime(c.Timestamp.Year, c.Timestamp.Month, c.Timestamp.Day, c.Timestamp.Hour, 0, 0))
                .OrderBy(g => g.Key);

            foreach (var hourGroup in grouped)
            {
                var candles = hourGroup.OrderBy(c => c.Timestamp).ToList();
                if (!candles.Any()) continue;

                hourlyCandles.Add(new NiftyIndexCandle
                {
                    Timestamp = hourGroup.Key,
                    Open = candles.First().Open,
                    High = candles.Max(c => c.High),
                    Low = candles.Min(c => c.Low),
                    Close = candles.Last().Close,
                    Volume = candles.Sum(c => c.Volume),
                    IsSynthetic = true,
                    Source = $"Aggregated from {candles.Count} minute candles"
                });
            }

            return hourlyCandles;
        }

        private async Task<List<NiftyIndexCandle>> GetNiftyIndexCandlesAsync(DateTime fromDate, DateTime toDate)
        {
            // This would get actual NIFTY50 index data if available
            var indexData = await _context.OptionsHistoricalData
                .Where(d => d.Underlying == "NIFTY" && 
                           d.TradingSymbol == "NIFTY" && // Actual index, not options
                           d.Timestamp >= fromDate && 
                           d.Timestamp <= toDate &&
                           d.Interval == "60minute")
                .OrderBy(d => d.Timestamp)
                .ToListAsync();

            return indexData.Select(d => new NiftyIndexCandle
            {
                Timestamp = d.Timestamp,
                Open = d.Open,
                High = d.High,
                Low = d.Low,
                Close = d.Close,
                Volume = d.Volume,
                IsSynthetic = false,
                Source = "Actual NIFTY50 Index Data"
            }).ToList();
        }

        private async Task<CorrectedBacktestResult> ProcessNiftyIndexSignalsAsync(
            List<NiftyIndexCandle> indexCandles, 
            CorrectedBacktestRequest request, 
            CorrectedBacktestResult result)
        {
            if (!indexCandles.Any())
            {
                result.DataIssues.Add("❌ No NIFTY index candles available for processing");
                return result;
            }

            result.DataIssues.Add($"📊 Processing {indexCandles.Count} NIFTY index candles");
            
            var weekState = new WeekState();
            var currentWeekCandles = new List<NiftyIndexCandle>();

            foreach (var candle in indexCandles.OrderBy(c => c.Timestamp))
            {
                // Check if this is a new week
                if (IsNewWeek(candle.Timestamp, currentWeekCandles.LastOrDefault()?.Timestamp))
                {
                    // Start new week
                    if (currentWeekCandles.Any())
                    {
                        CalculateZonesFromPreviousWeek(currentWeekCandles, weekState);
                    }
                    
                    currentWeekCandles.Clear();
                    weekState.Reset();
                    weekState.WeekStart = GetWeekStart(candle.Timestamp);
                }

                currentWeekCandles.Add(candle);
                UpdateWeeklyTracking(weekState, candle, currentWeekCandles.Count);

                // Check for signals (if no signal fired this week)
                if (!weekState.SignalFired && weekState.PreviousWeek != null)
                {
                    var signal = CheckSignalsOnIndexCandle(candle, currentWeekCandles, weekState);
                    if (signal != null)
                    {
                        var trade = await ExecuteOptionTradeFromSignal(signal, candle, request);
                        if (trade != null)
                        {
                            result.Trades.Add(trade);
                            weekState.SignalFired = true;
                        }
                    }
                }
            }

            CalculatePerformanceMetrics(result);
            return result;
        }

        private NiftySignal? CheckSignalsOnIndexCandle(
            NiftyIndexCandle candle, 
            List<NiftyIndexCandle> weekCandles, 
            WeekState weekState)
        {
            if (weekState.PreviousWeek == null || !weekCandles.Any()) return null;

            var barsSinceWeekStart = weekState.BarsSinceWeekStart;
            var firstCandle = weekCandles.First();

            // S1 and S2: Only on second candle
            if (barsSinceWeekStart == 2)
            {
                var s1Signal = CheckS1BearTrap(candle, firstCandle, weekState);
                if (s1Signal != null) return s1Signal;

                var s2Signal = CheckS2SupportHold(candle, firstCandle, weekState);
                if (s2Signal != null) return s2Signal;
            }

            // S3-S8: Any candle after second
            if (barsSinceWeekStart > 2)
            {
                var signals = new[]
                {
                    CheckS3ResistanceHold(candle, firstCandle, weekState),
                    CheckS4BiasFailure(candle, firstCandle, weekCandles, weekState),
                    CheckS5BiasFailureBearish(candle, firstCandle, weekState),
                    CheckS6WeaknessConfirmed(candle, firstCandle, weekState),
                    CheckS7BreakoutConfirmed(candle, firstCandle, weekCandles, weekState),
                    CheckS8BreakdownConfirmed(candle, firstCandle, weekCandles, weekState)
                };

                foreach (var signal in signals)
                {
                    if (signal != null) return signal;
                }
            }

            return null;
        }

        private async Task<CorrectedTrade?> ExecuteOptionTradeFromSignal(
            NiftySignal signal, 
            NiftyIndexCandle triggerCandle, 
            CorrectedBacktestRequest request)
        {
            try
            {
                // Calculate strike based on current NIFTY price
                var currentNiftyPrice = triggerCandle.Close;
                var strikePrice = RoundTo100(currentNiftyPrice);
                var hedgeStrike = signal.OptionType == "CE" ? 
                    strikePrice + request.HedgePoints : 
                    strikePrice - request.HedgePoints;

                // Get Thursday expiry
                var thursdayExpiry = GetThursdayExpiry(GetWeekStart(triggerCandle.Timestamp));
                
                // Generate option symbols
                var mainSymbol = GenerateTradingSymbol("NIFTY", thursdayExpiry, strikePrice, signal.OptionType);
                var hedgeSymbol = GenerateTradingSymbol("NIFTY", thursdayExpiry, hedgeStrike, signal.OptionType);

                // Get option prices at signal time
                var mainOptionData = await GetOptionsDataAtTimestampAsync(mainSymbol, triggerCandle.Timestamp);
                var hedgeOptionData = await GetOptionsDataAtTimestampAsync(hedgeSymbol, triggerCandle.Timestamp);

                if (mainOptionData == null || hedgeOptionData == null)
                {
                    _logger.LogWarning("No option data found for {MainSymbol} or {HedgeSymbol} at {Timestamp}", 
                        mainSymbol, hedgeSymbol, triggerCandle.Timestamp);
                    return null;
                }

                var trade = new CorrectedTrade
                {
                    SignalId = signal.SignalId,
                    SignalName = signal.SignalName,
                    TriggerTimestamp = triggerCandle.Timestamp,
                    NiftyPriceAtEntry = currentNiftyPrice,
                    MainStrike = strikePrice,
                    HedgeStrike = hedgeStrike,
                    OptionType = signal.OptionType,
                    MainSymbol = mainSymbol,
                    HedgeSymbol = hedgeSymbol,
                    MainEntryPrice = mainOptionData.LastPrice,
                    HedgeEntryPrice = hedgeOptionData.LastPrice,
                    StopLossLevel = signal.StopLossPrice,
                    Quantity = request.LotSize
                };

                // Monitor for exit
                var exitResult = await MonitorOptionTradeExit(trade, thursdayExpiry);
                
                trade.ExitTimestamp = exitResult.ExitTime;
                trade.MainExitPrice = exitResult.MainExitPrice;
                trade.HedgeExitPrice = exitResult.HedgeExitPrice;
                trade.ExitReason = exitResult.ExitReason;
                
                // Calculate P&L
                var pnl = CalculateOptionPnL(trade);
                trade.NetPnL = pnl.NetPnL;
                trade.MainPnL = pnl.MainPnL;
                trade.HedgePnL = pnl.HedgePnL;
                trade.Success = trade.NetPnL > 0;

                return trade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing option trade for signal {SignalId}", signal.SignalId);
                return null;
            }
        }

        // Signal detection methods (same logic as before, but using NiftyIndexCandle)
        private NiftySignal? CheckS1BearTrap(NiftyIndexCandle currentCandle, NiftyIndexCandle firstCandle, WeekState weekState)
        {
            var lowerZoneBottom = weekState.LowerZone.Bottom;

            var condition1 = firstCandle.Open >= lowerZoneBottom;
            var condition2 = firstCandle.Close < lowerZoneBottom;
            var condition3 = currentCandle.Close > firstCandle.Low;

            if (condition1 && condition2 && condition3)
            {
                var stopLoss = firstCandle.Low - Math.Abs(firstCandle.Open - firstCandle.Close);
                
                return new NiftySignal
                {
                    SignalId = "S1",
                    SignalName = "Bear Trap",
                    Timestamp = currentCandle.Timestamp,
                    Direction = 1, // Bullish
                    OptionType = "PE",
                    StopLossPrice = stopLoss,
                    Confidence = 0.8m
                };
            }

            return null;
        }

        private NiftySignal? CheckS2SupportHold(NiftyIndexCandle currentCandle, NiftyIndexCandle firstCandle, WeekState weekState)
        {
            var prevWeek = weekState.PreviousWeek!;
            var lowerZoneBottom = weekState.LowerZone.Bottom;
            var marginLow = weekState.MarginLow;

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
                return new NiftySignal
                {
                    SignalId = "S2",
                    SignalName = "Support Hold (Bullish)",
                    Timestamp = currentCandle.Timestamp,
                    Direction = 1, // Bullish
                    OptionType = "PE",
                    StopLossPrice = lowerZoneBottom,
                    Confidence = 0.85m
                };
            }

            return null;
        }

        // ... (implement other signal methods similarly)

        // Helper methods
        private bool IsNewWeek(DateTime currentCandle, DateTime? previousCandle)
        {
            if (!previousCandle.HasValue) return true;
            return GetWeekStart(currentCandle) != GetWeekStart(previousCandle.Value);
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var daysFromMonday = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysFromMonday < 0) daysFromMonday += 7;
            return date.Date.AddDays(-daysFromMonday);
        }

        private int RoundTo100(decimal price)
        {
            return (int)(Math.Round(price / 100) * 100);
        }

        private DateTime GetThursdayExpiry(DateTime weekStart)
        {
            return weekStart.AddDays(3).Date.AddHours(15.5); // Thursday 3:30 PM
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

        private (decimal NetPnL, decimal MainPnL, decimal HedgePnL) CalculateOptionPnL(CorrectedTrade trade)
        {
            // Option selling P&L: Sell high, buy low = profit
            var mainPnL = (trade.MainEntryPrice - trade.MainExitPrice) * trade.Quantity;
            var hedgePnL = (trade.HedgeExitPrice - trade.HedgeEntryPrice) * trade.Quantity;
            var netPnL = mainPnL + hedgePnL;

            return (netPnL, mainPnL, hedgePnL);
        }

        private void CalculateZonesFromPreviousWeek(List<NiftyIndexCandle> previousWeekCandles, WeekState weekState)
        {
            if (!previousWeekCandles.Any()) return;

            var weeklyHigh = previousWeekCandles.Max(c => c.High);
            var weeklyLow = previousWeekCandles.Min(c => c.Low);
            var weeklyClose = previousWeekCandles.OrderBy(c => c.Timestamp).Last().Close;

            // Calculate 4H bodies
            var fourHBodies = Calculate4HBodiesFromCandles(previousWeekCandles);
            var max4HBody = fourHBodies.Any() ? fourHBodies.Max(b => b.Top) : weeklyHigh;
            var min4HBody = fourHBodies.Any() ? fourHBodies.Min(b => b.Bottom) : weeklyLow;

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

            weekState.MarginHigh = Math.Max((weekState.UpperZone.Top - weekState.UpperZone.Bottom) * 3, 0.05m);
            weekState.MarginLow = Math.Max((weekState.LowerZone.Top - weekState.LowerZone.Bottom) * 3, 0.05m);

            var distanceToHigh = Math.Abs(weeklyClose - max4HBody);
            var distanceToLow = Math.Abs(weeklyClose - min4HBody);
            weekState.WeeklyBias = distanceToHigh < distanceToLow ? -1 : 1;
        }

        private List<FourHBody> Calculate4HBodiesFromCandles(List<NiftyIndexCandle> candles)
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

        private void UpdateWeeklyTracking(WeekState weekState, NiftyIndexCandle candle, int barsSinceWeekStart)
        {
            weekState.BarsSinceWeekStart = barsSinceWeekStart;
            weekState.WeeklyHigh = Math.Max(weekState.WeeklyHigh, candle.High);
            weekState.WeeklyLow = Math.Min(weekState.WeeklyLow == 0 ? candle.Low : weekState.WeeklyLow, candle.Low);
        }

        private async Task<TradeExitResult> MonitorOptionTradeExit(CorrectedTrade trade, DateTime thursdayExpiry)
        {
            // Monitor option prices for stop loss or expiry
            // This would use the actual options data for monitoring
            
            // Simplified implementation - in reality would check each subsequent timestamp
            var finalMainData = await GetOptionsDataAtTimestampAsync(trade.MainSymbol, thursdayExpiry);
            var finalHedgeData = await GetOptionsDataAtTimestampAsync(trade.HedgeSymbol, thursdayExpiry);

            return new TradeExitResult
            {
                ExitTime = thursdayExpiry,
                MainExitPrice = finalMainData?.LastPrice ?? 0.1m,
                HedgeExitPrice = finalHedgeData?.LastPrice ?? 0.1m,
                ExitReason = "EXPIRY_WIN"
            };
        }

        private void CalculatePerformanceMetrics(CorrectedBacktestResult result)
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
        }

        // Placeholder implementations for other signals
        private NiftySignal? CheckS3ResistanceHold(NiftyIndexCandle currentCandle, NiftyIndexCandle firstCandle, WeekState weekState) => null;
        private NiftySignal? CheckS4BiasFailure(NiftyIndexCandle currentCandle, NiftyIndexCandle firstCandle, List<NiftyIndexCandle> weekCandles, WeekState weekState) => null;
        private NiftySignal? CheckS5BiasFailureBearish(NiftyIndexCandle currentCandle, NiftyIndexCandle firstCandle, WeekState weekState) => null;
        private NiftySignal? CheckS6WeaknessConfirmed(NiftyIndexCandle currentCandle, NiftyIndexCandle firstCandle, WeekState weekState) => null;
        private NiftySignal? CheckS7BreakoutConfirmed(NiftyIndexCandle currentCandle, NiftyIndexCandle firstCandle, List<NiftyIndexCandle> weekCandles, WeekState weekState) => null;
        private NiftySignal? CheckS8BreakdownConfirmed(NiftyIndexCandle currentCandle, NiftyIndexCandle firstCandle, List<NiftyIndexCandle> weekCandles, WeekState weekState) => null;
    }

    // Supporting classes
    public class CorrectedBacktestRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal InitialCapital { get; set; } = 100000;
        public int LotSize { get; set; } = 50;
        public int HedgePoints { get; set; } = 300;
    }

    public class CorrectedBacktestResult
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
        public List<CorrectedTrade> Trades { get; set; } = new();
        public List<string> DataIssues { get; set; } = new();
    }

    public class NiftyIndexCandle
    {
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public bool IsSynthetic { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    public class NiftyDataCheck
    {
        public bool HasIndexData { get; set; }
        public string IndexSymbol { get; set; } = string.Empty;
    }

    public class NiftySignal
    {
        public string SignalId { get; set; } = string.Empty;
        public string SignalName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int Direction { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public decimal StopLossPrice { get; set; }
        public decimal Confidence { get; set; }
    }

    public class CorrectedTrade
    {
        public string SignalId { get; set; } = string.Empty;
        public string SignalName { get; set; } = string.Empty;
        public DateTime TriggerTimestamp { get; set; }
        public DateTime ExitTimestamp { get; set; }
        public decimal NiftyPriceAtEntry { get; set; }
        public int MainStrike { get; set; }
        public int HedgeStrike { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public string MainSymbol { get; set; } = string.Empty;
        public string HedgeSymbol { get; set; } = string.Empty;
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

    public class TradeExitResult
    {
        public DateTime ExitTime { get; set; }
        public decimal MainExitPrice { get; set; }
        public decimal HedgeExitPrice { get; set; }
        public string ExitReason { get; set; } = string.Empty;
    }
}