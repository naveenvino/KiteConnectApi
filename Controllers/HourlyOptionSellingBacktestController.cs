using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HourlyOptionSellingBacktestController : ControllerBase
    {
        private readonly HourlyOptionSellingBacktestService _backtestService;
        private readonly ILogger<HourlyOptionSellingBacktestController> _logger;

        public HourlyOptionSellingBacktestController(
            HourlyOptionSellingBacktestService backtestService,
            ILogger<HourlyOptionSellingBacktestController> logger)
        {
            _backtestService = backtestService;
            _logger = logger;
        }

        /// <summary>
        /// Run hourly option selling backtest for specified period
        /// </summary>
        [HttpPost("run")]
        public async Task<IActionResult> RunHourlyBacktest([FromBody] HourlyBacktestRequest request)
        {
            try
            {
                _logger.LogInformation("Running hourly option selling backtest from {FromDate} to {ToDate}", 
                    request.FromDate, request.ToDate);

                var result = await _backtestService.RunHourlyBacktestAsync(request);
                
                return Ok(new
                {
                    success = true,
                    message = "Hourly backtest completed successfully",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running hourly option selling backtest");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error running backtest",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Run quick hourly backtest with default parameters (last 3 weeks)
        /// </summary>
        [HttpPost("quick-run")]
        public async Task<IActionResult> RunQuickHourlyBacktest()
        {
            try
            {
                var request = new HourlyBacktestRequest
                {
                    FromDate = DateTime.Now.AddDays(-21), // Last 3 weeks
                    ToDate = DateTime.Now,
                    InitialCapital = 100000,
                    LotSize = 50,
                    HedgePoints = 300
                };

                _logger.LogInformation("Running quick hourly backtest for last 3 weeks");

                var result = await _backtestService.RunHourlyBacktestAsync(request);
                
                return Ok(new
                {
                    success = true,
                    message = "Quick hourly backtest completed",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running quick hourly backtest");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error running quick backtest",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Run backtest for specific period with default parameters
        /// </summary>
        [HttpGet("run-period")]
        public async Task<IActionResult> RunPeriodBacktest([FromQuery] string fromDate, [FromQuery] string toDate)
        {
            try
            {
                if (!DateTime.TryParse(fromDate, out var from) || !DateTime.TryParse(toDate, out var to))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid date format. Use YYYY-MM-DD format"
                    });
                }

                var request = new HourlyBacktestRequest
                {
                    FromDate = from,
                    ToDate = to,
                    InitialCapital = 100000,
                    LotSize = 50,
                    HedgePoints = 300
                };

                _logger.LogInformation("Running hourly backtest for period {FromDate} to {ToDate}", from, to);

                var result = await _backtestService.RunHourlyBacktestAsync(request);
                
                return Ok(new
                {
                    success = true,
                    message = $"Hourly backtest completed for {from:yyyy-MM-dd} to {to:yyyy-MM-dd}",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running period backtest");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error running period backtest",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get strategy information and signal descriptions
        /// </summary>
        [HttpGet("strategy-info")]
        public IActionResult GetStrategyInfo()
        {
            var strategyInfo = new
            {
                strategy = "1-Hour NIFTY Option Selling with Hedge",
                description = "Processes 8 TradingView signals on 1-hour timeframe with option selling strategy",
                signals = new[]
                {
                    new { id = "S1", name = "Bear Trap", timing = "2nd hour only", type = "Bullish", option = "PE" },
                    new { id = "S2", name = "Support Hold", timing = "2nd hour only", type = "Bullish", option = "PE" },
                    new { id = "S3", name = "Resistance Hold", timing = "Any hour after 2nd", type = "Bearish", option = "CE" },
                    new { id = "S4", name = "Bias Failure (Bullish)", timing = "Any hour after 2nd", type = "Bullish", option = "PE" },
                    new { id = "S5", name = "Bias Failure (Bearish)", timing = "Any hour after 2nd", type = "Bearish", option = "CE" },
                    new { id = "S6", name = "Weakness Confirmed", timing = "Any hour after 2nd", type = "Bearish", option = "CE" },
                    new { id = "S7", name = "1H Breakout Confirmed", timing = "Any hour after 2nd", type = "Bullish", option = "PE" },
                    new { id = "S8", name = "1H Breakdown Confirmed", timing = "Any hour after 2nd", type = "Bearish", option = "CE" }
                },
                execution = new
                {
                    timeframe = "1-hour",
                    signalTiming = "S1,S2 only on 2nd hour of week (Monday ~10:15 AM), S3-S8 any hour after 2nd",
                    maxSignalsPerWeek = 1,
                    positionStructure = "SELL main option + BUY hedge option (+/-300 points)",
                    expiry = "Thursday 3:30 PM",
                    stopLoss = "Price increase triggers stop loss for option selling",
                    profitScenario = "Expiry without stop loss = profit from premium decay"
                },
                riskManagement = new
                {
                    hedgeProtection = "±300 points from main strike",
                    stopLossLogic = "Not always loss due to hedge protection",
                    weeklyExposure = "Maximum 1 trade per week",
                    expiry = "Thursday automatic square-off"
                }
            };

            return Ok(new
            {
                success = true,
                data = strategyInfo
            });
        }

        /// <summary>
        /// Test endpoint to verify controller is working
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                success = true,
                message = "HourlyOptionSellingBacktest controller is healthy",
                timestamp = DateTime.Now,
                version = "1.0.0 - 1-Hour Timeframe"
            });
        }
    }
}