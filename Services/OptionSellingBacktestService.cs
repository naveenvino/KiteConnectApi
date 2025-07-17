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
    public class OptionSellingBacktestService
    {
        private readonly ApplicationDbContext _context;
        private readonly TradingViewIndicatorService _indicatorService;
        private readonly ILogger<OptionSellingBacktestService> _logger;

        public OptionSellingBacktestService(
            ApplicationDbContext context,
            TradingViewIndicatorService indicatorService,
            ILogger<OptionSellingBacktestService> logger)
        {
            _context = context;
            _indicatorService = indicatorService;
            _logger = logger;
        }

        public async Task<OptionSellingBacktestResult> RunOptionSellingBacktestAsync(OptionSellingBacktestRequest request)
        {
            _logger.LogInformation("Starting option selling backtest from {FromDate} to {ToDate}", 
                request.FromDate, request.ToDate);

            var result = new OptionSellingBacktestResult
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                InitialCapital = request.InitialCapital,
                LotSize = request.LotSize,
                Trades = new List<OptionSellingTrade>()
            };

            var current = request.FromDate;
            
            while (current <= request.ToDate)
            {
                // Process each week (Monday to Friday)
                if (current.DayOfWeek == DayOfWeek.Monday)
                {
                    try
                    {
                        var weekTrades = await ProcessWeekAsync(current, request);
                        result.Trades.AddRange(weekTrades);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing week starting {Date}", current);
                    }
                }
                
                current = current.AddDays(1);
            }

            // Calculate performance metrics
            CalculatePerformanceMetrics(result);

            return result;
        }

        private async Task<List<OptionSellingTrade>> ProcessWeekAsync(DateTime weekStart, OptionSellingBacktestRequest request)
        {
            var trades = new List<OptionSellingTrade>();
            
            // Get Thursday expiry for this week
            var thursdayExpiry = GetThursdayExpiry(weekStart);
            
            // Generate signals for this week
            var signals = await _indicatorService.ProcessWeeklyLevelsAndBiasAsync("NIFTY");
            
            // Process each signal (but only first valid signal per week)
            foreach (var signal in signals.Take(1)) // Only first signal per week
            {
                var trade = await ExecuteOptionSellingTradeAsync(signal, weekStart, thursdayExpiry, request);
                if (trade != null)
                {
                    trades.Add(trade);
                    break; // Only one trade per week
                }
            }

            return trades;
        }

        private async Task<OptionSellingTrade?> ExecuteOptionSellingTradeAsync(
            TradingSignalResult signal, 
            DateTime weekStart, 
            DateTime thursdayExpiry, 
            OptionSellingBacktestRequest request)
        {
            try
            {
                // Generate trading symbols
                var mainSymbol = GenerateTradingSymbol("NIFTY", thursdayExpiry, (int)signal.StrikePrice, signal.OptionType);
                var hedgeStrike = CalculateHedgeStrike((int)signal.StrikePrice, signal.OptionType, request.HedgePoints);
                var hedgeSymbol = GenerateTradingSymbol("NIFTY", thursdayExpiry, hedgeStrike, signal.OptionType);

                // Get entry prices
                var mainEntryData = await GetOptionsDataAtTimestampAsync(mainSymbol, signal.Timestamp);
                var hedgeEntryData = await GetOptionsDataAtTimestampAsync(hedgeSymbol, signal.Timestamp);

                if (mainEntryData == null || hedgeEntryData == null)
                {
                    _logger.LogWarning("No entry data found for {MainSymbol} or {HedgeSymbol}", mainSymbol, hedgeSymbol);
                    return null;
                }

                // Create entry positions
                var mainEntry = new OptionPosition
                {
                    Symbol = mainSymbol,
                    Strike = (int)signal.StrikePrice,
                    OptionType = signal.OptionType,
                    TransactionType = "SELL", // We sell the main option
                    Quantity = request.LotSize,
                    EntryPrice = mainEntryData.LastPrice,
                    EntryTime = signal.Timestamp
                };

                var hedgeEntry = new OptionPosition
                {
                    Symbol = hedgeSymbol,
                    Strike = hedgeStrike,
                    OptionType = signal.OptionType,
                    TransactionType = "BUY", // We buy the hedge
                    Quantity = request.LotSize,
                    EntryPrice = hedgeEntryData.LastPrice,
                    EntryTime = signal.Timestamp
                };

                // Calculate stop loss level
                var stopLossLevel = CalculateStopLossLevel(mainEntry.EntryPrice, request.StopLossPercentage);

                // Monitor for stop loss or expiry
                var exitResult = await MonitorPositionAsync(mainEntry, hedgeEntry, stopLossLevel, thursdayExpiry);

                // Calculate P&L
                var pnl = CalculateOptionSellingPnL(mainEntry, hedgeEntry, exitResult);

                return new OptionSellingTrade
                {
                    SignalId = signal.SignalId,
                    SignalName = signal.SignalName,
                    WeekStart = weekStart,
                    ThursdayExpiry = thursdayExpiry,
                    MainPosition = mainEntry,
                    HedgePosition = hedgeEntry,
                    ExitReason = exitResult.ExitReason,
                    ExitTime = exitResult.ExitTime,
                    MainExitPrice = exitResult.MainExitPrice,
                    HedgeExitPrice = exitResult.HedgeExitPrice,
                    GrossPnL = pnl.GrossPnL,
                    NetPnL = pnl.NetPnL,
                    StopLossLevel = stopLossLevel,
                    DaysHeld = (exitResult.ExitTime - signal.Timestamp).Days,
                    Success = pnl.NetPnL > 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing option selling trade for signal {SignalId}", signal.SignalId);
                return null;
            }
        }

        private async Task<PositionExitResult> MonitorPositionAsync(
            OptionPosition mainPosition, 
            OptionPosition hedgePosition, 
            decimal stopLossLevel, 
            DateTime thursdayExpiry)
        {
            var current = mainPosition.EntryTime.AddMinutes(5); // Start monitoring 5 minutes after entry

            while (current <= thursdayExpiry)
            {
                // Get current prices
                var mainData = await GetOptionsDataAtTimestampAsync(mainPosition.Symbol, current);
                var hedgeData = await GetOptionsDataAtTimestampAsync(hedgePosition.Symbol, current);

                if (mainData != null && hedgeData != null)
                {
                    // Check for stop loss hit (main option price increased beyond stop loss)
                    if (mainData.LastPrice >= stopLossLevel)
                    {
                        return new PositionExitResult
                        {
                            ExitReason = "STOP_LOSS",
                            ExitTime = current,
                            MainExitPrice = mainData.LastPrice,
                            HedgeExitPrice = hedgeData.LastPrice
                        };
                    }
                }

                current = current.AddHours(1); // Check every hour
            }

            // Exit at expiry
            var finalMainData = await GetOptionsDataAtTimestampAsync(mainPosition.Symbol, thursdayExpiry);
            var finalHedgeData = await GetOptionsDataAtTimestampAsync(hedgePosition.Symbol, thursdayExpiry);

            return new PositionExitResult
            {
                ExitReason = "EXPIRY",
                ExitTime = thursdayExpiry,
                MainExitPrice = finalMainData?.LastPrice ?? 0,
                HedgeExitPrice = finalHedgeData?.LastPrice ?? 0
            };
        }

        private OptionSellingPnL CalculateOptionSellingPnL(
            OptionPosition mainPosition, 
            OptionPosition hedgePosition, 
            PositionExitResult exitResult)
        {
            // Main position P&L (SELL to open, BUY to close)
            var mainPnL = (mainPosition.EntryPrice - exitResult.MainExitPrice) * mainPosition.Quantity;

            // Hedge position P&L (BUY to open, SELL to close)
            var hedgePnL = (exitResult.HedgeExitPrice - hedgePosition.EntryPrice) * hedgePosition.Quantity;

            var grossPnL = mainPnL + hedgePnL;
            var brokerage = CalculateBrokerage(mainPosition.Quantity);
            var netPnL = grossPnL - brokerage;

            return new OptionSellingPnL
            {
                MainPnL = mainPnL,
                HedgePnL = hedgePnL,
                GrossPnL = grossPnL,
                Brokerage = brokerage,
                NetPnL = netPnL
            };
        }

        private void CalculatePerformanceMetrics(OptionSellingBacktestResult result)
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
            var totalProfit = result.Trades.Where(t => t.Success).Sum(t => t.NetPnL);
            var totalLoss = Math.Abs(result.Trades.Where(t => !t.Success).Sum(t => t.NetPnL));
            result.ProfitFactor = totalLoss > 0 ? (double)(totalProfit / totalLoss) : 0;

            // Calculate max drawdown
            var runningPnL = 0m;
            var peak = 0m;
            var maxDrawdown = 0m;

            foreach (var trade in result.Trades.OrderBy(t => t.ExitTime))
            {
                runningPnL += trade.NetPnL;
                if (runningPnL > peak) peak = runningPnL;
                var drawdown = peak - runningPnL;
                if (drawdown > maxDrawdown) maxDrawdown = drawdown;
            }

            result.MaxDrawdown = maxDrawdown;
            result.FinalCapital = result.InitialCapital + result.TotalPnL;
        }

        private async Task<OptionsHistoricalData?> GetOptionsDataAtTimestampAsync(string tradingSymbol, DateTime timestamp)
        {
            return await _context.OptionsHistoricalData
                .Where(d => d.TradingSymbol == tradingSymbol && d.Timestamp <= timestamp)
                .OrderBy(d => d.Timestamp)
                .LastOrDefaultAsync();
        }

        private DateTime GetThursdayExpiry(DateTime weekStart)
        {
            var daysToThursday = ((int)DayOfWeek.Thursday - (int)weekStart.DayOfWeek + 7) % 7;
            return weekStart.AddDays(daysToThursday);
        }

        private int CalculateHedgeStrike(int mainStrike, string optionType, int hedgePoints)
        {
            return optionType == "CE" ? mainStrike + hedgePoints : mainStrike - hedgePoints;
        }

        private decimal CalculateStopLossLevel(decimal entryPrice, decimal stopLossPercentage)
        {
            return entryPrice * (1 + stopLossPercentage / 100);
        }

        private decimal CalculateBrokerage(int quantity)
        {
            // Simplified brokerage calculation
            return quantity * 20; // Rs.20 per lot
        }

        private string GenerateTradingSymbol(string underlying, DateTime expiry, int strike, string optionType)
        {
            var year = expiry.ToString("yy");
            var month = expiry.Month.ToString();
            var day = expiry.Day.ToString("D2");
            
            return $"{underlying}{year}{month}{day}{strike}{optionType.ToUpper()}";
        }
    }

    // Supporting classes
    public class OptionSellingBacktestRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal InitialCapital { get; set; } = 100000;
        public int LotSize { get; set; } = 50;
        public int HedgePoints { get; set; } = 300;
        public decimal StopLossPercentage { get; set; } = 50; // 50% of entry premium
    }

    public class OptionSellingBacktestResult
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal InitialCapital { get; set; }
        public int LotSize { get; set; }
        public List<OptionSellingTrade> Trades { get; set; } = new();
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public double WinRate { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal AveragePnL { get; set; }
        public decimal MaxProfit { get; set; }
        public decimal MaxLoss { get; set; }
        public decimal MaxDrawdown { get; set; }
        public decimal FinalCapital { get; set; }
        public double ProfitFactor { get; set; }
    }

    public class OptionSellingTrade
    {
        public string SignalId { get; set; } = string.Empty;
        public string SignalName { get; set; } = string.Empty;
        public DateTime WeekStart { get; set; }
        public DateTime ThursdayExpiry { get; set; }
        public OptionPosition MainPosition { get; set; } = new();
        public OptionPosition HedgePosition { get; set; } = new();
        public string ExitReason { get; set; } = string.Empty;
        public DateTime ExitTime { get; set; }
        public decimal MainExitPrice { get; set; }
        public decimal HedgeExitPrice { get; set; }
        public decimal GrossPnL { get; set; }
        public decimal NetPnL { get; set; }
        public decimal StopLossLevel { get; set; }
        public int DaysHeld { get; set; }
        public bool Success { get; set; }
    }

    public class OptionPosition
    {
        public string Symbol { get; set; } = string.Empty;
        public int Strike { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public string TransactionType { get; set; } = string.Empty; // BUY or SELL
        public int Quantity { get; set; }
        public decimal EntryPrice { get; set; }
        public DateTime EntryTime { get; set; }
    }

    public class PositionExitResult
    {
        public string ExitReason { get; set; } = string.Empty;
        public DateTime ExitTime { get; set; }
        public decimal MainExitPrice { get; set; }
        public decimal HedgeExitPrice { get; set; }
    }

    public class OptionSellingPnL
    {
        public decimal MainPnL { get; set; }
        public decimal HedgePnL { get; set; }
        public decimal GrossPnL { get; set; }
        public decimal Brokerage { get; set; }
        public decimal NetPnL { get; set; }
    }
}