using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Globalization;

namespace KiteConnectApi.Services
{
    public class ApiTradingDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly ILogger<ApiTradingDashboardService> _logger;
        private readonly INotificationService _notificationService;

        public ApiTradingDashboardService(
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            ILogger<ApiTradingDashboardService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _kiteConnectService = kiteConnectService;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<ApiAlertDetails> LogTradeAsync(TradingSignalResult signal)
        {
            try
            {
                var weekStartTimestamp = GetWeekStartTimestamp(signal.Timestamp);
                var weekStartDateStr = weekStartTimestamp.ToString("yyyy-MM-dd");
                var expiryDate = GetExpiryDate(signal.Timestamp, "Thursday");
                var tradingSymbol = GenerateTradingSymbol("NIFTY", expiryDate, (int)signal.StrikePrice, signal.OptionType);
                
                // Get current market price
                var currentPrice = await GetCurrentPriceAsync(tradingSymbol);
                
                // Create JSON alert message (same format as TradingView)
                var jsonAlert = JsonSerializer.Serialize(new
                {
                    strike = (int)signal.StrikePrice,
                    type = signal.OptionType,
                    signal = signal.SignalId,
                    action = signal.Action
                });

                // Create trade log entry
                var tradeLog = new ApiTradeLog
                {
                    WeekStartDate = weekStartDateStr,
                    SignalId = signal.SignalId,
                    Direction = signal.Direction,
                    StopLoss = signal.StopLossPrice,
                    EntryTime = signal.Timestamp,
                    Outcome = "OPEN",
                    TradingSymbol = tradingSymbol,
                    Strike = (int)signal.StrikePrice,
                    OptionType = signal.OptionType,
                    EntryPrice = currentPrice,
                    Quantity = 1, // Default quantity
                    ExpiryDay = "Thursday",
                    Confidence = signal.Confidence,
                    Source = "API",
                    Notes = signal.Description
                };

                _context.ApiTradeLog.Add(tradeLog);
                await _context.SaveChangesAsync();

                // Create alert details response
                var alertDetails = new ApiAlertDetails
                {
                    SignalId = signal.SignalId,
                    SignalName = signal.SignalName,
                    Timestamp = signal.Timestamp,
                    Strike = (int)signal.StrikePrice,
                    OptionType = signal.OptionType,
                    Action = signal.Action,
                    Direction = signal.Direction,
                    StopLossPrice = signal.StopLossPrice,
                    Confidence = signal.Confidence,
                    Description = signal.Description,
                    TradingSymbol = tradingSymbol,
                    CurrentPrice = currentPrice,
                    WeekStartDate = weekStartDateStr,
                    JsonAlert = jsonAlert,
                    IsActive = true,
                    ExpiryDay = "Thursday",
                    ExpiryDate = expiryDate,
                    AlertMessage = $"API Signal {signal.SignalId}: {signal.SignalName} triggered at {signal.Timestamp:HH:mm} for {tradingSymbol}"
                };

                _logger.LogInformation("API Trade logged: {SignalId} - {TradingSymbol} at {Price}", 
                    signal.SignalId, tradingSymbol, currentPrice);

                return alertDetails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging API trade for signal {SignalId}", signal.SignalId);
                throw;
            }
        }

        public async Task<List<TradeManagementStatus>> MonitorActiveTradesAsync()
        {
            try
            {
                var activeTradesQuery = _context.ApiTradeLog
                    .Where(t => t.Outcome == "OPEN")
                    .OrderBy(t => t.EntryTime);

                var activeTrades = await activeTradesQuery.ToListAsync();
                var managementStatuses = new List<TradeManagementStatus>();

                foreach (var trade in activeTrades)
                {
                    var status = await CheckTradeStatusAsync(trade);
                    managementStatuses.Add(status);

                    // Update trade if status changed
                    if (status.Status != "MONITORING")
                    {
                        await UpdateTradeOutcomeAsync(trade, status);
                    }
                }

                return managementStatuses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring active trades");
                return new List<TradeManagementStatus>();
            }
        }

        public async Task<ApiDashboardData> GetDashboardDataAsync(DashboardRequest request)
        {
            try
            {
                var dashboardData = new ApiDashboardData
                {
                    Year = request.Year,
                    Month = request.Month
                };

                // Get month range
                var (startMonth, endMonth) = GetQuarterMonths(request.Month);
                
                // Get filtered trades
                var query = _context.ApiTradeLog
                    .Where(t => t.EntryTime.Year == request.Year);

                if (request.Month != "All")
                {
                    query = query.Where(t => t.EntryTime.Month >= startMonth && t.EntryTime.Month <= endMonth);
                }

                if (!string.IsNullOrEmpty(request.FilterBySignal))
                {
                    query = query.Where(t => t.SignalId == request.FilterBySignal);
                }

                if (!string.IsNullOrEmpty(request.FilterByOutcome))
                {
                    query = query.Where(t => t.Outcome == request.FilterByOutcome);
                }

                if (!request.ShowOpenTrades)
                {
                    query = query.Where(t => t.Outcome != "OPEN");
                }

                if (!request.ShowClosedTrades)
                {
                    query = query.Where(t => t.Outcome == "OPEN");
                }

                var filteredTrades = await query.OrderBy(t => t.EntryTime).ToListAsync();
                dashboardData.FilteredTrades = filteredTrades;

                if (!filteredTrades.Any())
                {
                    dashboardData.NoTradesMessage = $"No trades found for {(request.Month == "All" ? "" : request.Month + " ")}{request.Year}";
                    return dashboardData;
                }

                // Calculate signal performance
                dashboardData.SignalPerformance = CalculateSignalPerformance(filteredTrades);
                dashboardData.OverallStats = CalculateOverallStats(filteredTrades);
                dashboardData.WeeklyBreakdown = CalculateWeeklyBreakdown(filteredTrades);
                dashboardData.MonthlyBreakdown = CalculateMonthlyBreakdown(filteredTrades);

                return dashboardData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                throw;
            }
        }

        public async Task<List<ApiAlertDetails>> GetRecentAlertsAsync(int count = 10)
        {
            try
            {
                var recentTrades = await _context.ApiTradeLog
                    .OrderByDescending(t => t.EntryTime)
                    .Take(count)
                    .ToListAsync();

                var alerts = new List<ApiAlertDetails>();

                foreach (var trade in recentTrades)
                {
                    var currentPrice = await GetCurrentPriceAsync(trade.TradingSymbol);
                    
                    var alert = new ApiAlertDetails
                    {
                        SignalId = trade.SignalId,
                        SignalName = GetSignalName(trade.SignalId),
                        Timestamp = trade.EntryTime,
                        Strike = trade.Strike,
                        OptionType = trade.OptionType,
                        Action = "Entry",
                        Direction = trade.Direction,
                        StopLossPrice = trade.StopLoss,
                        Confidence = trade.Confidence,
                        Description = trade.Notes ?? "",
                        TradingSymbol = trade.TradingSymbol,
                        CurrentPrice = currentPrice,
                        WeekStartDate = trade.WeekStartDate,
                        JsonAlert = JsonSerializer.Serialize(new
                        {
                            strike = trade.Strike,
                            type = trade.OptionType,
                            signal = trade.SignalId,
                            action = "Entry"
                        }),
                        IsActive = trade.Outcome == "OPEN",
                        ExpiryDay = trade.ExpiryDay,
                        ExpiryDate = GetExpiryDate(trade.EntryTime, trade.ExpiryDay),
                        AlertMessage = $"API Signal {trade.SignalId}: {GetSignalName(trade.SignalId)} - {trade.Outcome}"
                    };

                    alerts.Add(alert);
                }

                return alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent alerts");
                return new List<ApiAlertDetails>();
            }
        }

        private async Task<TradeManagementStatus> CheckTradeStatusAsync(ApiTradeLog trade)
        {
            var status = new TradeManagementStatus
            {
                TradeId = trade.Id,
                SignalId = trade.SignalId,
                Status = "MONITORING",
                LastChecked = DateTime.UtcNow,
                StopLossPrice = trade.StopLoss,
                ExpiryDay = trade.ExpiryDay,
                ExpiryDate = GetExpiryDate(trade.EntryTime, trade.ExpiryDay)
            };

            try
            {
                // Get current price
                var currentPrice = await GetCurrentPriceAsync(trade.TradingSymbol);
                status.CurrentPrice = currentPrice;

                // Check if before expiry day
                var expiryDayOfWeek = GetExpiryDayOfWeek(trade.ExpiryDay);
                status.IsBeforeExpiry = DateTime.Now.DayOfWeek <= expiryDayOfWeek;

                // Check trade week
                var tradeWeek = GetWeekOfYear(trade.EntryTime);
                var tradeYear = trade.EntryTime.Year;
                var currentWeek = GetWeekOfYear(DateTime.Now);
                var currentYear = DateTime.Now.Year;

                // Check if trade is still in same week
                if (tradeYear == currentYear && tradeWeek == currentWeek)
                {
                    if (status.IsBeforeExpiry)
                    {
                        // Check for stop loss hit
                        bool slHit = (trade.Direction == 1 && currentPrice <= trade.StopLoss) ||
                                    (trade.Direction == -1 && currentPrice >= trade.StopLoss);

                        if (slHit)
                        {
                            status.Status = "SL_HIT";
                            status.AlertMessage = $"Stop Loss Hit: {trade.SignalId} at {currentPrice}";
                            status.ShouldTriggerAlert = true;
                        }
                    }
                }
                else
                {
                    // New week started - trade is a WIN
                    status.Status = "EXPIRED_WIN";
                    status.AlertMessage = $"Trade Won: {trade.SignalId} expired successfully";
                    status.ShouldTriggerAlert = false;
                }

                return status;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking trade status for {TradeId}", trade.Id);
                status.Status = "ERROR";
                return status;
            }
        }

        private async Task UpdateTradeOutcomeAsync(ApiTradeLog trade, TradeManagementStatus status)
        {
            try
            {
                trade.Outcome = status.Status == "SL_HIT" ? "LOSS" : "WIN";
                trade.ExitTime = DateTime.UtcNow;
                trade.ExitPrice = status.CurrentPrice;
                trade.UpdatedAt = DateTime.UtcNow;

                // Calculate P&L
                var pnl = CalculatePnL(trade.EntryPrice, status.CurrentPrice, trade.Direction, trade.Quantity);
                trade.PnL = pnl;

                _context.ApiTradeLog.Update(trade);
                await _context.SaveChangesAsync();

                // Send notification if needed
                if (status.ShouldTriggerAlert && !string.IsNullOrEmpty(status.AlertMessage))
                {
                    await _notificationService.SendNotificationAsync(
                        "Trade Update", 
                        status.AlertMessage);
                }

                _logger.LogInformation("Trade updated: {TradeId} - Outcome: {Outcome}, P&L: {PnL}", 
                    trade.Id, trade.Outcome, pnl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating trade outcome for {TradeId}", trade.Id);
            }
        }

        private Dictionary<string, ApiSignalStats> CalculateSignalPerformance(List<ApiTradeLog> trades)
        {
            var signalStats = new Dictionary<string, ApiSignalStats>();
            var closedTrades = trades.Where(t => t.Outcome != "OPEN").ToList();

            foreach (var signalGroup in closedTrades.GroupBy(t => t.SignalId))
            {
                var signalTrades = signalGroup.ToList();
                var stats = new ApiSignalStats
                {
                    Id = signalGroup.Key,
                    TotalTrades = signalTrades.Count,
                    Wins = signalTrades.Count(t => t.Outcome == "WIN"),
                    Losses = signalTrades.Count(t => t.Outcome == "LOSS"),
                    TotalPnL = signalTrades.Sum(t => t.PnL ?? 0),
                    Trades = signalTrades
                };

                stats.WinRate = stats.TotalTrades > 0 ? (double)stats.Wins / stats.TotalTrades * 100 : 0;
                stats.AveragePnL = stats.TotalTrades > 0 ? stats.TotalPnL / stats.TotalTrades : 0;
                stats.MaxWin = signalTrades.Where(t => t.PnL.HasValue).Max(t => t.PnL.Value);
                stats.MaxLoss = signalTrades.Where(t => t.PnL.HasValue).Min(t => t.PnL.Value);

                signalStats[signalGroup.Key] = stats;
            }

            return signalStats;
        }

        private ApiSignalStats CalculateOverallStats(List<ApiTradeLog> trades)
        {
            var closedTrades = trades.Where(t => t.Outcome != "OPEN").ToList();
            
            var stats = new ApiSignalStats
            {
                Id = "OVERALL",
                TotalTrades = closedTrades.Count,
                Wins = closedTrades.Count(t => t.Outcome == "WIN"),
                Losses = closedTrades.Count(t => t.Outcome == "LOSS"),
                TotalPnL = closedTrades.Sum(t => t.PnL ?? 0),
                Trades = closedTrades
            };

            stats.WinRate = stats.TotalTrades > 0 ? (double)stats.Wins / stats.TotalTrades * 100 : 0;
            stats.AveragePnL = stats.TotalTrades > 0 ? stats.TotalPnL / stats.TotalTrades : 0;
            
            if (closedTrades.Any(t => t.PnL.HasValue))
            {
                stats.MaxWin = closedTrades.Where(t => t.PnL.HasValue).Max(t => t.PnL.Value);
                stats.MaxLoss = closedTrades.Where(t => t.PnL.HasValue).Min(t => t.PnL.Value);
            }

            return stats;
        }

        private List<WeeklyPerformance> CalculateWeeklyBreakdown(List<ApiTradeLog> trades)
        {
            var weeklyStats = new List<WeeklyPerformance>();
            var closedTrades = trades.Where(t => t.Outcome != "OPEN").ToList();

            foreach (var weekGroup in closedTrades.GroupBy(t => t.WeekStartDate))
            {
                var weekTrades = weekGroup.ToList();
                var performance = new WeeklyPerformance
                {
                    WeekStartDate = weekGroup.Key,
                    TotalTrades = weekTrades.Count,
                    Wins = weekTrades.Count(t => t.Outcome == "WIN"),
                    Losses = weekTrades.Count(t => t.Outcome == "LOSS"),
                    TotalPnL = weekTrades.Sum(t => t.PnL ?? 0),
                    SignalsTriggered = weekTrades.Select(t => t.SignalId).Distinct().ToList()
                };

                performance.WinRate = performance.TotalTrades > 0 ? (double)performance.Wins / performance.TotalTrades * 100 : 0;
                weeklyStats.Add(performance);
            }

            return weeklyStats.OrderBy(w => w.WeekStartDate).ToList();
        }

        private List<MonthlyPerformance> CalculateMonthlyBreakdown(List<ApiTradeLog> trades)
        {
            var monthlyStats = new List<MonthlyPerformance>();
            var closedTrades = trades.Where(t => t.Outcome != "OPEN").ToList();

            foreach (var monthGroup in closedTrades.GroupBy(t => new { t.EntryTime.Year, t.EntryTime.Month }))
            {
                var monthTrades = monthGroup.ToList();
                var performance = new MonthlyPerformance
                {
                    Month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthGroup.Key.Month),
                    Year = monthGroup.Key.Year,
                    TotalTrades = monthTrades.Count,
                    Wins = monthTrades.Count(t => t.Outcome == "WIN"),
                    Losses = monthTrades.Count(t => t.Outcome == "LOSS"),
                    TotalPnL = monthTrades.Sum(t => t.PnL ?? 0)
                };

                performance.WinRate = performance.TotalTrades > 0 ? (double)performance.Wins / performance.TotalTrades * 100 : 0;
                performance.AveragePnL = performance.TotalTrades > 0 ? performance.TotalPnL / performance.TotalTrades : 0;
                
                monthlyStats.Add(performance);
            }

            return monthlyStats.OrderBy(m => m.Year).ThenBy(m => DateTime.ParseExact(m.Month, "MMMM", CultureInfo.CurrentCulture).Month).ToList();
        }

        private async Task<decimal> GetCurrentPriceAsync(string tradingSymbol)
        {
            try
            {
                var quotes = await _kiteConnectService.GetQuotesAsync(new[] { tradingSymbol });
                if (quotes.TryGetValue(tradingSymbol, out var quote))
                {
                    return (decimal)quote.LastPrice;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting price for {Symbol}, using fallback", tradingSymbol);
            }
            
            return 100; // Fallback price
        }

        private decimal CalculatePnL(decimal entryPrice, decimal exitPrice, int direction, int quantity)
        {
            if (direction == 1) // Bullish (bought)
            {
                return (exitPrice - entryPrice) * quantity;
            }
            else // Bearish (sold)
            {
                return (entryPrice - exitPrice) * quantity;
            }
        }

        private (int startMonth, int endMonth) GetQuarterMonths(string quarterStr)
        {
            return quarterStr switch
            {
                "Jan-Apr" => (1, 4),
                "May-Aug" => (5, 8),
                "Sep-Dec" => (9, 12),
                _ => (1, 12) // Default for "All"
            };
        }

        private DateTime GetWeekStartTimestamp(DateTime timestamp)
        {
            var daysFromMonday = (int)timestamp.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysFromMonday < 0) daysFromMonday += 7;
            return timestamp.Date.AddDays(-daysFromMonday);
        }

        private DateTime GetExpiryDate(DateTime entryDate, string expiryDay)
        {
            var expiryDayOfWeek = GetExpiryDayOfWeek(expiryDay);
            var daysUntilExpiry = ((int)expiryDayOfWeek - (int)entryDate.DayOfWeek + 7) % 7;
            
            if (daysUntilExpiry == 0 && entryDate.TimeOfDay > TimeSpan.FromHours(15.5))
            {
                daysUntilExpiry = 7;
            }
            
            return entryDate.Date.AddDays(daysUntilExpiry);
        }

        private DayOfWeek GetExpiryDayOfWeek(string day)
        {
            return day switch
            {
                "Monday" => DayOfWeek.Monday,
                "Tuesday" => DayOfWeek.Tuesday,
                "Wednesday" => DayOfWeek.Wednesday,
                "Thursday" => DayOfWeek.Thursday,
                "Friday" => DayOfWeek.Friday,
                _ => DayOfWeek.Thursday
            };
        }

        private int GetWeekOfYear(DateTime date)
        {
            var calendar = CultureInfo.CurrentCulture.Calendar;
            return calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }

        private string GenerateTradingSymbol(string underlying, DateTime expiry, int strike, string optionType)
        {
            var year = expiry.ToString("yy");
            var month = expiry.Month.ToString();
            var day = expiry.Day.ToString("D2");
            
            return $"{underlying}{year}{month}{day}{strike}{optionType.ToUpper()}";
        }

        private string GetSignalName(string signalId)
        {
            return signalId switch
            {
                "S1" => "Bear Trap",
                "S2" => "Support Hold (Bullish)",
                "S3" => "Resistance Hold (Bearish)",
                "S4" => "Bias Failure (Bullish)",
                "S5" => "Bias Failure (Bearish)",
                "S6" => "Weakness Confirmed",
                "S7" => "1H Breakout Confirmed",
                "S8" => "1H Breakdown Confirmed",
                _ => "Unknown Signal"
            };
        }
    }
}