using KiteConnectApi.Models.Trading;
using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApiTradingDashboardController : ControllerBase
    {
        private readonly ApiTradingDashboardService _dashboardService;
        private readonly TradingViewIndicatorService _indicatorService;
        private readonly ILogger<ApiTradingDashboardController> _logger;

        public ApiTradingDashboardController(
            ApiTradingDashboardService dashboardService,
            TradingViewIndicatorService indicatorService,
            ILogger<ApiTradingDashboardController> logger)
        {
            _dashboardService = dashboardService;
            _indicatorService = indicatorService;
            _logger = logger;
        }

        /// <summary>
        /// Get current API-generated signals with alert details (equivalent to TradingView alerts)
        /// </summary>
        [HttpGet("current-signals")]
        public async Task<ActionResult<List<ApiAlertDetails>>> GetCurrentSignalsAsync()
        {
            try
            {
                // Get current signals from API
                var signals = await _indicatorService.ProcessWeeklyLevelsAndBiasAsync("NIFTY");
                var alertDetails = new List<ApiAlertDetails>();

                foreach (var signal in signals)
                {
                    // Log the trade and get alert details
                    var alertDetail = await _dashboardService.LogTradeAsync(signal);
                    alertDetails.Add(alertDetail);
                }

                return Ok(alertDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current signals");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get recent API alerts (equivalent to TradingView alert history)
        /// </summary>
        [HttpGet("recent-alerts")]
        public async Task<ActionResult<List<ApiAlertDetails>>> GetRecentAlertsAsync([FromQuery] int count = 10)
        {
            try
            {
                var alerts = await _dashboardService.GetRecentAlertsAsync(count);
                return Ok(alerts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent alerts");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get dashboard data - equivalent to TradingView indicator dashboard
        /// </summary>
        [HttpPost("dashboard")]
        public async Task<ActionResult<ApiDashboardData>> GetDashboardAsync([FromBody] DashboardRequest request)
        {
            try
            {
                var dashboardData = await _dashboardService.GetDashboardDataAsync(request);
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get trade log view - equivalent to TradingView "Trade Log" dashboard
        /// </summary>
        [HttpGet("trade-log")]
        public async Task<ActionResult<ApiDashboardData>> GetTradeLogAsync(
            [FromQuery] int year = 0,
            [FromQuery] string month = "All",
            [FromQuery] string? filterBySignal = null,
            [FromQuery] string? filterByOutcome = null)
        {
            try
            {
                var request = new DashboardRequest
                {
                    Year = year == 0 ? DateTime.Now.Year : year,
                    Month = month,
                    View = "Trade Log",
                    FilterBySignal = filterBySignal,
                    FilterByOutcome = filterByOutcome
                };

                var dashboardData = await _dashboardService.GetDashboardDataAsync(request);
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting trade log");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get performance summary - equivalent to TradingView "Performance Summary" dashboard
        /// </summary>
        [HttpGet("performance-summary")]
        public async Task<ActionResult<ApiDashboardData>> GetPerformanceSummaryAsync(
            [FromQuery] int year = 0,
            [FromQuery] string month = "All")
        {
            try
            {
                var request = new DashboardRequest
                {
                    Year = year == 0 ? DateTime.Now.Year : year,
                    Month = month,
                    View = "Performance Summary"
                };

                var dashboardData = await _dashboardService.GetDashboardDataAsync(request);
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting performance summary");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get weekly breakdown - additional view for weekly performance
        /// </summary>
        [HttpGet("weekly-breakdown")]
        public async Task<ActionResult<ApiDashboardData>> GetWeeklyBreakdownAsync(
            [FromQuery] int year = 0,
            [FromQuery] string month = "All")
        {
            try
            {
                var request = new DashboardRequest
                {
                    Year = year == 0 ? DateTime.Now.Year : year,
                    Month = month,
                    View = "Weekly Breakdown"
                };

                var dashboardData = await _dashboardService.GetDashboardDataAsync(request);
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting weekly breakdown");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Monitor active trades - equivalent to TradingView trade management logic
        /// </summary>
        [HttpGet("monitor-trades")]
        public async Task<ActionResult<List<TradeManagementStatus>>> MonitorActiveTradesAsync()
        {
            try
            {
                var tradeStatuses = await _dashboardService.MonitorActiveTradesAsync();
                return Ok(tradeStatuses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring trades");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get signal statistics - equivalent to TradingView signal performance table
        /// </summary>
        [HttpGet("signal-stats")]
        public async Task<ActionResult<Dictionary<string, ApiSignalStats>>> GetSignalStatsAsync(
            [FromQuery] int year = 0,
            [FromQuery] string month = "All")
        {
            try
            {
                var request = new DashboardRequest
                {
                    Year = year == 0 ? DateTime.Now.Year : year,
                    Month = month
                };

                var dashboardData = await _dashboardService.GetDashboardDataAsync(request);
                return Ok(dashboardData.SignalPerformance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signal stats");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Test API signals generation - for first level testing
        /// </summary>
        [HttpPost("test-signals")]
        public async Task<ActionResult<List<ApiAlertDetails>>> TestSignalsAsync([FromBody] TestSignalsRequest? request = null)
        {
            try
            {
                var symbol = request?.Symbol ?? "NIFTY";
                var includeHistory = request?.IncludeHistory ?? false;
                
                _logger.LogInformation("Testing API signals for {Symbol}", symbol);

                // Generate current signals
                var signals = await _indicatorService.ProcessWeeklyLevelsAndBiasAsync(symbol);
                var alertDetails = new List<ApiAlertDetails>();

                foreach (var signal in signals)
                {
                    var alertDetail = await _dashboardService.LogTradeAsync(signal);
                    alertDetails.Add(alertDetail);
                    
                    _logger.LogInformation("Generated API signal: {SignalId} - {SignalName} at {Strike} {OptionType}", 
                        signal.SignalId, signal.SignalName, signal.StrikePrice, signal.OptionType);
                }

                // Include recent history if requested
                if (includeHistory)
                {
                    var recentAlerts = await _dashboardService.GetRecentAlertsAsync(5);
                    alertDetails.AddRange(recentAlerts.Where(a => !alertDetails.Any(ad => ad.SignalId == a.SignalId && ad.Timestamp == a.Timestamp)));
                }

                return Ok(new
                {
                    Timestamp = DateTime.Now,
                    Symbol = symbol,
                    TotalSignals = alertDetails.Count,
                    ActiveSignals = alertDetails.Count(a => a.IsActive),
                    Alerts = alertDetails.OrderByDescending(a => a.Timestamp).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API signals");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get live dashboard data - real-time equivalent of TradingView dashboard
        /// </summary>
        [HttpGet("live-dashboard")]
        public async Task<ActionResult<object>> GetLiveDashboardAsync()
        {
            try
            {
                // Get current signals
                var currentSignals = await _indicatorService.ProcessWeeklyLevelsAndBiasAsync("NIFTY");
                
                // Get recent alerts
                var recentAlerts = await _dashboardService.GetRecentAlertsAsync(10);
                
                // Get active trades status
                var activeTradesStatus = await _dashboardService.MonitorActiveTradesAsync();
                
                // Get current year performance
                var performanceData = await _dashboardService.GetDashboardDataAsync(new DashboardRequest
                {
                    Year = DateTime.Now.Year,
                    Month = "All",
                    View = "Performance Summary"
                });

                return Ok(new
                {
                    Timestamp = DateTime.Now,
                    CurrentSignals = currentSignals.Select(s => new
                    {
                        s.SignalId,
                        s.SignalName,
                        s.StrikePrice,
                        s.OptionType,
                        s.Direction,
                        s.Confidence,
                        s.Description
                    }),
                    RecentAlerts = recentAlerts.Take(5),
                    ActiveTrades = activeTradesStatus.Where(t => t.Status == "MONITORING"),
                    PerformanceOverview = new
                    {
                        TotalTrades = performanceData.OverallStats.TotalTrades,
                        WinRate = performanceData.OverallStats.WinRate,
                        TotalPnL = performanceData.OverallStats.TotalPnL,
                        TopPerformingSignal = performanceData.SignalPerformance
                            .Where(s => s.Value.TotalTrades > 0)
                            .OrderByDescending(s => s.Value.WinRate)
                            .FirstOrDefault().Key ?? "N/A"
                    },
                    SignalBreakdown = performanceData.SignalPerformance.Select(s => new
                    {
                        SignalId = s.Key,
                        SignalName = GetSignalName(s.Key),
                        s.Value.TotalTrades,
                        s.Value.WinRate,
                        s.Value.TotalPnL
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting live dashboard");
                return StatusCode(500, "Internal server error");
            }
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

    public class TestSignalsRequest
    {
        public string Symbol { get; set; } = "NIFTY";
        public bool IncludeHistory { get; set; } = false;
    }
}