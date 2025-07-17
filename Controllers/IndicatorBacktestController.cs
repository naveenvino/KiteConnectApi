using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IndicatorBacktestController : ControllerBase
    {
        private readonly IndicatorBacktestingService _backtestingService;
        private readonly TradingViewIndicatorService _indicatorService;
        private readonly ILogger<IndicatorBacktestController> _logger;

        public IndicatorBacktestController(
            IndicatorBacktestingService backtestingService,
            TradingViewIndicatorService indicatorService,
            ILogger<IndicatorBacktestController> logger)
        {
            _backtestingService = backtestingService;
            _indicatorService = indicatorService;
            _logger = logger;
        }

        /// <summary>
        /// Run API indicator backtest on historical data
        /// </summary>
        [HttpPost("run-api-backtest")]
        public async Task<ActionResult<SignalBacktestResult>> RunApiBacktestAsync([FromBody] ApiBacktestRequest request)
        {
            try
            {
                _logger.LogInformation("Starting API indicator backtest from {FromDate} to {ToDate}", 
                    request.FromDate, request.ToDate);

                // Generate API signals for the period
                var signals = await _backtestingService.GenerateApiSignalsForPeriodAsync(request.FromDate, request.ToDate);
                
                // Convert to comparison request format
                var comparisonRequest = new ComparisonBacktestRequest
                {
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    Quantity = request.Quantity,
                    Symbol = request.Symbol
                };

                // Run backtest
                var result = await _backtestingService.RunSignalBacktestAsync(signals, comparisonRequest, "API");
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running API indicator backtest");
                return StatusCode(500, new { error = "Failed to run API backtest", message = ex.Message });
            }
        }

        /// <summary>
        /// Compare API indicator with TradingView signals
        /// </summary>
        [HttpPost("compare-signals")]
        public async Task<ActionResult<ComparisonBacktestResult>> CompareSignalsAsync([FromBody] ComparisonBacktestRequest request)
        {
            try
            {
                var result = await _backtestingService.RunComparisonBacktestAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing signals");
                return StatusCode(500, new { error = "Failed to compare signals", message = ex.Message });
            }
        }

        /// <summary>
        /// Get current API signals (real-time)
        /// </summary>
        [HttpGet("current-signals")]
        public async Task<ActionResult<object>> GetCurrentSignalsAsync()
        {
            try
            {
                var signals = await _indicatorService.ProcessWeeklyLevelsAndBiasAsync("NIFTY");
                
                return Ok(new
                {
                    Timestamp = DateTime.Now,
                    Symbol = "NIFTY",
                    SignalCount = signals.Count,
                    Signals = signals
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current signals");
                return StatusCode(500, new { error = "Failed to get current signals", message = ex.Message });
            }
        }

        /// <summary>
        /// Run quick backtest with last 3 weeks of data
        /// </summary>
        [HttpPost("quick-backtest")]
        public async Task<ActionResult<object>> RunQuickBacktestAsync()
        {
            try
            {
                var toDate = DateTime.Today.AddDays(-1);
                var fromDate = toDate.AddDays(-21); // 3 weeks
                
                var request = new ApiBacktestRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Quantity = 50,
                    Symbol = "NIFTY"
                };

                _logger.LogInformation("Running quick API backtest for last 3 weeks");

                // Generate signals
                var signals = await _backtestingService.GenerateApiSignalsForPeriodAsync(request.FromDate, request.ToDate);
                
                // Run backtest
                var comparisonRequest = new ComparisonBacktestRequest
                {
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    Quantity = request.Quantity,
                    Symbol = request.Symbol
                };

                var result = await _backtestingService.RunSignalBacktestAsync(signals, comparisonRequest, "API");
                
                return Ok(new
                {
                    Period = $"{fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
                    TotalDays = 21,
                    Results = result,
                    Summary = new
                    {
                        TotalSignals = result.TotalSignals,
                        TotalTrades = result.TotalTrades,
                        WinRate = $"{result.WinRate:F2}%",
                        TotalPnL = result.TotalPnL,
                        AveragePnL = result.AveragePnL,
                        MaxDrawdown = result.MaxDrawdown,
                        SharpeRatio = result.SharpeRatio
                    },
                    SignalBreakdown = result.SignalPerformances
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running quick backtest");
                return StatusCode(500, new { error = "Failed to run quick backtest", message = ex.Message });
            }
        }

        /// <summary>
        /// Get sample backtest parameters
        /// </summary>
        [HttpGet("sample-params")]
        public ActionResult<object> GetSampleParams()
        {
            var toDate = DateTime.Today.AddDays(-1);
            var fromDate = toDate.AddDays(-21);

            return Ok(new
            {
                ApiBacktest = new ApiBacktestRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Quantity = 50,
                    Symbol = "NIFTY"
                },
                ComparisonBacktest = new ComparisonBacktestRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    Quantity = 50,
                    Symbol = "NIFTY"
                }
            });
        }
    }

    public class ApiBacktestRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Quantity { get; set; } = 50;
        public string Symbol { get; set; } = "NIFTY";
    }
}