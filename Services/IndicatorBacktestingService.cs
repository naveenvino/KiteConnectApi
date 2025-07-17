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
    public class IndicatorBacktestingService
    {
        private readonly ApplicationDbContext _context;
        private readonly TradingViewIndicatorService _indicatorService;
        private readonly ILogger<IndicatorBacktestingService> _logger;

        public IndicatorBacktestingService(
            ApplicationDbContext context,
            TradingViewIndicatorService indicatorService,
            ILogger<IndicatorBacktestingService> logger)
        {
            _context = context;
            _indicatorService = indicatorService;
            _logger = logger;
        }

        public async Task<ComparisonBacktestResult> RunComparisonBacktestAsync(ComparisonBacktestRequest request)
        {
            _logger.LogInformation("Starting comparison backtest: API vs TradingView signals from {FromDate} to {ToDate}", 
                request.FromDate, request.ToDate);

            var result = new ComparisonBacktestResult
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                ApiResults = new SignalBacktestResult(),
                TradingViewResults = new SignalBacktestResult(),
                ComparisonMetrics = new ComparisonMetrics()
            };

            try
            {
                // Get historical TradingView signals from database
                var tradingViewSignals = await GetTradingViewSignalsAsync(request.FromDate, request.ToDate);
                
                // Generate API signals for the same period
                var apiSignals = await GenerateApiSignalsForPeriodAsync(request.FromDate, request.ToDate);
                
                // Run backtest for both signal sources
                result.ApiResults = await RunSignalBacktestAsync(apiSignals, request, "API");
                result.TradingViewResults = await RunSignalBacktestAsync(tradingViewSignals, request, "TradingView");
                
                // Calculate comparison metrics
                result.ComparisonMetrics = CalculateComparisonMetrics(result.ApiResults, result.TradingViewResults);
                
                // Generate signal accuracy comparison
                result.SignalAccuracy = CompareSignalAccuracy(apiSignals, tradingViewSignals);
                
                _logger.LogInformation("Comparison backtest completed. API Win Rate: {ApiWinRate}%, TradingView Win Rate: {TvWinRate}%", 
                    result.ApiResults.WinRate, result.TradingViewResults.WinRate);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running comparison backtest");
                throw;
            }
        }

        private async Task<List<BacktestSignal>> GetTradingViewSignalsAsync(DateTime fromDate, DateTime toDate)
        {
            // Get historical TradingView alerts from database
            var tvAlerts = await _context.ManualTradingViewAlerts
                .Where(a => a.ReceivedTime >= fromDate && a.ReceivedTime <= toDate)
                .OrderBy(a => a.ReceivedTime)
                .ToListAsync();

            var signals = new List<BacktestSignal>();
            
            foreach (var alert in tvAlerts)
            {
                try
                {
                    var signal = new BacktestSignal
                    {
                        SignalId = alert.Signal ?? "Unknown",
                        Timestamp = alert.ReceivedTime,
                        Strike = alert.Strike,
                        OptionType = alert.Type ?? "CE",
                        Action = alert.Action ?? "Entry",
                        Direction = DetermineDirection(alert.Type, alert.Action),
                        Source = "TradingView",
                        StopLossPrice = CalculateStopLossFromAlert(alert),
                        Confidence = 1.0m // TradingView signals are considered 100% confident
                    };
                    
                    signals.Add(signal);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing TradingView alert {AlertId}", alert.Id);
                }
            }

            return signals;
        }

        public async Task<List<BacktestSignal>> GenerateApiSignalsForPeriodAsync(DateTime fromDate, DateTime toDate)
        {
            var signals = new List<BacktestSignal>();
            var current = fromDate;
            
            while (current <= toDate)
            {
                try
                {
                    // Simulate processing for each day
                    var weekStart = GetWeekStart(current);
                    
                    // Only process Monday to Friday
                    if (current.DayOfWeek >= DayOfWeek.Monday && current.DayOfWeek <= DayOfWeek.Friday)
                    {
                        // Generate signals for this timestamp
                        var daySignals = await _indicatorService.ProcessWeeklyLevelsAndBiasAsync("NIFTY");
                        
                        foreach (var signal in daySignals)
                        {
                            signals.Add(new BacktestSignal
                            {
                                SignalId = signal.SignalId,
                                Timestamp = current,
                                Strike = (int)signal.StrikePrice,
                                OptionType = signal.OptionType,
                                Action = signal.Action,
                                Direction = signal.Direction,
                                Source = "API",
                                StopLossPrice = signal.StopLossPrice,
                                Confidence = signal.Confidence
                            });
                        }
                    }
                    
                    current = current.AddDays(1);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error generating API signals for {Date}", current);
                    current = current.AddDays(1);
                }
            }

            return signals;
        }

        public async Task<SignalBacktestResult> RunSignalBacktestAsync(List<BacktestSignal> signals, ComparisonBacktestRequest request, string source)
        {
            var result = new SignalBacktestResult
            {
                Source = source,
                TotalSignals = signals.Count,
                Trades = new List<SignalBacktestTrade>()
            };

            var signalGroups = signals.GroupBy(s => s.SignalId).ToList();
            
            foreach (var signalGroup in signalGroups)
            {
                var signalResults = new SignalPerformance
                {
                    SignalId = signalGroup.Key,
                    Trades = new List<SignalBacktestTrade>()
                };

                foreach (var signal in signalGroup)
                {
                    try
                    {
                        var trade = await ExecuteSignalTradeAsync(signal, request);
                        if (trade != null)
                        {
                            signalResults.Trades.Add(trade);
                            result.Trades.Add(trade);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error executing trade for signal {SignalId}", signal.SignalId);
                    }
                }

                signalResults.TotalTrades = signalResults.Trades.Count;
                signalResults.WinningTrades = signalResults.Trades.Count(t => t.PnL > 0);
                signalResults.LosingTrades = signalResults.Trades.Count(t => t.PnL < 0);
                signalResults.TotalPnL = signalResults.Trades.Sum(t => t.PnL);
                signalResults.WinRate = signalResults.TotalTrades > 0 ? (double)signalResults.WinningTrades / signalResults.TotalTrades * 100 : 0;
                signalResults.AveragePnL = signalResults.TotalTrades > 0 ? signalResults.TotalPnL / signalResults.TotalTrades : 0;
                
                result.SignalPerformances.Add(signalResults);
            }

            // Calculate overall metrics
            result.TotalTrades = result.Trades.Count;
            result.WinningTrades = result.Trades.Count(t => t.PnL > 0);
            result.LosingTrades = result.Trades.Count(t => t.PnL < 0);
            result.TotalPnL = result.Trades.Sum(t => t.PnL);
            result.WinRate = result.TotalTrades > 0 ? (double)result.WinningTrades / result.TotalTrades * 100 : 0;
            result.AveragePnL = result.TotalTrades > 0 ? result.TotalPnL / result.TotalTrades : 0;
            result.MaxDrawdown = CalculateMaxDrawdown(result.Trades);
            result.SharpeRatio = CalculateSharpeRatio(result.Trades);

            return result;
        }

        private async Task<SignalBacktestTrade?> ExecuteSignalTradeAsync(BacktestSignal signal, ComparisonBacktestRequest request)
        {
            try
            {
                // Get the options symbol for the signal
                var expiryDate = GetNearestWeeklyExpiry(signal.Timestamp);
                var tradingSymbol = GenerateTradingSymbol("NIFTY", expiryDate, signal.Strike, signal.OptionType);
                
                // Get entry price from historical data
                var entryData = await GetOptionsDataAtTimestampAsync(tradingSymbol, signal.Timestamp);
                if (entryData == null)
                {
                    _logger.LogWarning("No entry data found for {Symbol} at {Timestamp}", tradingSymbol, signal.Timestamp);
                    return null;
                }

                // Simulate holding until expiry or stop loss
                var exitData = await SimulateTradeExitAsync(signal, entryData, expiryDate);
                if (exitData == null)
                {
                    _logger.LogWarning("No exit data found for {Symbol}", tradingSymbol);
                    return null;
                }

                // Calculate P&L for OPTION SELLING strategy
                var entryPrice = entryData.LastPrice;
                var exitPrice = exitData.LastPrice;
                var quantity = request.Quantity;
                
                // For option selling: Entry = Sell (receive premium), Exit = Buy back (pay premium)
                // P&L = Premium Received - Premium Paid (without hedge calculation for now)
                var pnl = (entryPrice - exitPrice) * quantity; // Sell high, buy low = profit

                return new SignalBacktestTrade
                {
                    SignalId = signal.SignalId,
                    EntryTime = signal.Timestamp,
                    ExitTime = exitData.Timestamp,
                    TradingSymbol = tradingSymbol,
                    Strike = signal.Strike,
                    OptionType = signal.OptionType,
                    Direction = signal.Direction,
                    EntryPrice = entryPrice,
                    ExitPrice = exitPrice,
                    Quantity = quantity,
                    PnL = pnl,
                    StopLossPrice = signal.StopLossPrice,
                    ExitReason = exitData.ExitReason,
                    Source = signal.Source,
                    Confidence = signal.Confidence
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing signal trade for {SignalId}", signal.SignalId);
                return null;
            }
        }

        private async Task<OptionsHistoricalData?> GetOptionsDataAtTimestampAsync(string tradingSymbol, DateTime timestamp)
        {
            return await _context.OptionsHistoricalData
                .Where(d => d.TradingSymbol == tradingSymbol && d.Timestamp <= timestamp)
                .OrderBy(d => d.Timestamp)
                .LastOrDefaultAsync();
        }

        private async Task<TradeExitData?> SimulateTradeExitAsync(BacktestSignal signal, OptionsHistoricalData entryData, DateTime expiryDate)
        {
            var tradingSymbol = entryData.TradingSymbol;
            var entryTime = signal.Timestamp;
            var stopLossPrice = signal.StopLossPrice;
            
            // Get subsequent data points
            var subsequentData = await _context.OptionsHistoricalData
                .Where(d => d.TradingSymbol == tradingSymbol && 
                           d.Timestamp > entryTime && 
                           d.Timestamp <= expiryDate)
                .OrderBy(d => d.Timestamp)
                .ToListAsync();

            // Check for stop loss hit (OPTION SELLING LOGIC)
            foreach (var data in subsequentData)
            {
                var shouldExit = false;
                var exitReason = "";
                
                // For option selling: Stop loss triggers when option price INCREASES
                // (We sold at entry price, if price goes up, we lose money when buying back)
                if (data.LastPrice >= stopLossPrice)
                {
                    shouldExit = true;
                    exitReason = "STOP_LOSS";
                }
                
                if (shouldExit)
                {
                    return new TradeExitData
                    {
                        Timestamp = data.Timestamp,
                        LastPrice = data.LastPrice,
                        ExitReason = exitReason
                    };
                }
            }

            // Exit at expiry if no stop loss hit
            var expiryData = subsequentData.LastOrDefault();
            if (expiryData != null)
            {
                return new TradeExitData
                {
                    Timestamp = expiryData.Timestamp,
                    LastPrice = expiryData.LastPrice,
                    ExitReason = "EXPIRY"
                };
            }

            return null;
        }

        private ComparisonMetrics CalculateComparisonMetrics(SignalBacktestResult apiResults, SignalBacktestResult tvResults)
        {
            return new ComparisonMetrics
            {
                WinRateDifference = apiResults.WinRate - tvResults.WinRate,
                PnLDifference = apiResults.TotalPnL - tvResults.TotalPnL,
                SharpeRatioDifference = apiResults.SharpeRatio - tvResults.SharpeRatio,
                MaxDrawdownDifference = apiResults.MaxDrawdown - tvResults.MaxDrawdown,
                TotalTradesDifference = apiResults.TotalTrades - tvResults.TotalTrades,
                AveragePnLDifference = apiResults.AveragePnL - tvResults.AveragePnL,
                ApiSuperiority = CalculateSuperiority(apiResults, tvResults)
            };
        }

        private SignalAccuracyComparison CompareSignalAccuracy(List<BacktestSignal> apiSignals, List<BacktestSignal> tvSignals)
        {
            var comparison = new SignalAccuracyComparison();
            
            // Group signals by day and signal type
            var apiGrouped = apiSignals.GroupBy(s => new { s.Timestamp.Date, s.SignalId }).ToList();
            var tvGrouped = tvSignals.GroupBy(s => new { s.Timestamp.Date, s.SignalId }).ToList();
            
            var matchingSignals = 0;
            var totalApiSignals = apiGrouped.Count;
            var totalTvSignals = tvGrouped.Count;
            
            foreach (var apiGroup in apiGrouped)
            {
                var matchingTvGroup = tvGrouped.FirstOrDefault(tv => 
                    tv.Key.Date == apiGroup.Key.Date && 
                    tv.Key.SignalId == apiGroup.Key.SignalId);
                
                if (matchingTvGroup != null)
                {
                    matchingSignals++;
                    
                    // Compare strike prices
                    var apiStrike = apiGroup.First().Strike;
                    var tvStrike = matchingTvGroup.First().Strike;
                    var strikeDifference = Math.Abs(apiStrike - tvStrike);
                    
                    comparison.StrikeDifferences.Add(strikeDifference);
                }
            }
            
            comparison.MatchingSignals = matchingSignals;
            comparison.TotalApiSignals = totalApiSignals;
            comparison.TotalTvSignals = totalTvSignals;
            comparison.SignalMatchRate = totalApiSignals > 0 ? (double)matchingSignals / totalApiSignals * 100 : 0;
            comparison.AverageStrikeDifference = comparison.StrikeDifferences.Any() ? comparison.StrikeDifferences.Average() : 0;
            
            return comparison;
        }

        private double CalculateSuperiority(SignalBacktestResult apiResults, SignalBacktestResult tvResults)
        {
            var score = 0.0;
            
            // Weight different metrics
            if (apiResults.WinRate > tvResults.WinRate) score += 0.3;
            if (apiResults.TotalPnL > tvResults.TotalPnL) score += 0.3;
            if (apiResults.SharpeRatio > tvResults.SharpeRatio) score += 0.2;
            if (apiResults.MaxDrawdown < tvResults.MaxDrawdown) score += 0.2;
            
            return score;
        }

        private decimal CalculateMaxDrawdown(List<SignalBacktestTrade> trades)
        {
            if (!trades.Any()) return 0;

            var runningPnL = 0m;
            var peak = 0m;
            var maxDrawdown = 0m;

            foreach (var trade in trades.OrderBy(t => t.ExitTime))
            {
                runningPnL += trade.PnL;
                if (runningPnL > peak)
                {
                    peak = runningPnL;
                }
                
                var drawdown = peak - runningPnL;
                if (drawdown > maxDrawdown)
                {
                    maxDrawdown = drawdown;
                }
            }

            return maxDrawdown;
        }

        private double CalculateSharpeRatio(List<SignalBacktestTrade> trades)
        {
            if (!trades.Any()) return 0;

            var returns = trades.Select(t => (double)t.PnL).ToList();
            var avgReturn = returns.Average();
            var stdDev = Math.Sqrt(returns.Select(r => Math.Pow(r - avgReturn, 2)).Sum() / returns.Count);
            
            return stdDev == 0 ? 0 : avgReturn / stdDev;
        }

        private int DetermineDirection(string optionType, string action)
        {
            // Determine if signal is bullish or bearish
            if (action?.ToUpper() == "ENTRY")
            {
                return optionType?.ToUpper() == "PE" ? 1 : -1; // PE entry = bullish, CE entry = bearish
            }
            return 0;
        }

        private decimal CalculateStopLossFromAlert(ManualTradingViewAlert alert)
        {
            // Basic stop loss calculation - can be enhanced
            return alert.Strike * 0.95m; // 5% below strike as default
        }

        private DateTime GetNearestWeeklyExpiry(DateTime date)
        {
            var daysUntilThursday = ((int)DayOfWeek.Thursday - (int)date.DayOfWeek + 7) % 7;
            if (daysUntilThursday == 0 && date.TimeOfDay > TimeSpan.FromHours(15.5))
            {
                daysUntilThursday = 7;
            }
            return date.Date.AddDays(daysUntilThursday);
        }

        private string GenerateTradingSymbol(string underlying, DateTime expiry, int strike, string optionType)
        {
            var year = expiry.ToString("yy");
            var month = expiry.Month.ToString();
            var day = expiry.Day.ToString("D2");
            
            return $"{underlying}{year}{month}{day}{strike}{optionType.ToUpper()}";
        }

        private DateTime GetWeekStart(DateTime date)
        {
            var daysFromMonday = (int)date.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysFromMonday < 0) daysFromMonday += 7;
            return date.Date.AddDays(-daysFromMonday);
        }
    }

    // Supporting classes for comparison backtesting
    public class ComparisonBacktestRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Quantity { get; set; } = 1;
        public string Symbol { get; set; } = "NIFTY";
    }

    public class ComparisonBacktestResult
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public SignalBacktestResult ApiResults { get; set; } = new();
        public SignalBacktestResult TradingViewResults { get; set; } = new();
        public ComparisonMetrics ComparisonMetrics { get; set; } = new();
        public SignalAccuracyComparison SignalAccuracy { get; set; } = new();
    }

    public class SignalBacktestResult
    {
        public string Source { get; set; } = string.Empty;
        public int TotalSignals { get; set; }
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal TotalPnL { get; set; }
        public double WinRate { get; set; }
        public decimal AveragePnL { get; set; }
        public decimal MaxDrawdown { get; set; }
        public double SharpeRatio { get; set; }
        public List<SignalBacktestTrade> Trades { get; set; } = new();
        public List<SignalPerformance> SignalPerformances { get; set; } = new();
    }

    public class SignalBacktestTrade
    {
        public string SignalId { get; set; } = string.Empty;
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
        public string TradingSymbol { get; set; } = string.Empty;
        public int Strike { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public int Direction { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal PnL { get; set; }
        public decimal StopLossPrice { get; set; }
        public string ExitReason { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
    }

    public class SignalPerformance
    {
        public string SignalId { get; set; } = string.Empty;
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal TotalPnL { get; set; }
        public double WinRate { get; set; }
        public decimal AveragePnL { get; set; }
        public List<SignalBacktestTrade> Trades { get; set; } = new();
    }

    public class BacktestSignal
    {
        public string SignalId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int Strike { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int Direction { get; set; }
        public string Source { get; set; } = string.Empty;
        public decimal StopLossPrice { get; set; }
        public decimal Confidence { get; set; }
    }

    public class ComparisonMetrics
    {
        public double WinRateDifference { get; set; }
        public decimal PnLDifference { get; set; }
        public double SharpeRatioDifference { get; set; }
        public decimal MaxDrawdownDifference { get; set; }
        public int TotalTradesDifference { get; set; }
        public decimal AveragePnLDifference { get; set; }
        public double ApiSuperiority { get; set; } // 0-1 score
    }

    public class SignalAccuracyComparison
    {
        public int MatchingSignals { get; set; }
        public int TotalApiSignals { get; set; }
        public int TotalTvSignals { get; set; }
        public double SignalMatchRate { get; set; }
        public List<int> StrikeDifferences { get; set; } = new();
        public double AverageStrikeDifference { get; set; }
    }

    public class TradeExitData
    {
        public DateTime Timestamp { get; set; }
        public decimal LastPrice { get; set; }
        public string ExitReason { get; set; } = string.Empty;
    }
}