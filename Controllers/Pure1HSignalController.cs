using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class Pure1HSignalController : ControllerBase
    {
        private readonly Pure1HSignalService _pure1HService;
        private readonly ILogger<Pure1HSignalController> _logger;

        public Pure1HSignalController(
            Pure1HSignalService pure1HService,
            ILogger<Pure1HSignalController> logger)
        {
            _pure1HService = pure1HService;
            _logger = logger;
        }

        /// <summary>
        /// Run pure 1H candle-based backtest with all calculations derived from 1H data
        /// </summary>
        [HttpPost("backtest")]
        public async Task<IActionResult> RunPure1HBacktest([FromBody] Pure1HBacktestRequest request)
        {
            try
            {
                _logger.LogInformation("Running pure 1H backtest from {FromDate} to {ToDate}", 
                    request.FromDate, request.ToDate);

                var result = await _pure1HService.RunPure1HBacktestAsync(request);
                
                return Ok(new
                {
                    success = true,
                    message = "Pure 1H backtest completed successfully",
                    data = result,
                    methodology = new
                    {
                        description = "All calculations derived from 1H candles only",
                        weeklyData = "Aggregated from 1H candles (high=max, low=min, close=last)",
                        fourHBodies = "Calculated from every 4 consecutive 1H candles",
                        zones = "Calculated from previous week's 1H aggregations",
                        signalTiming = "S1,S2 on 2nd candle, S3-S8 on any candle after 2nd",
                        processing = "Sequential 1H candle processing with week state management"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running pure 1H backtest");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error running pure 1H backtest",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Quick pure 1H backtest with last 3 weeks of data
        /// </summary>
        [HttpPost("quick-backtest")]
        public async Task<IActionResult> RunQuickPure1HBacktest()
        {
            try
            {
                var request = new Pure1HBacktestRequest
                {
                    FromDate = DateTime.Now.AddDays(-21), // Last 3 weeks
                    ToDate = DateTime.Now,
                    InitialCapital = 100000,
                    LotSize = 50,
                    HedgePoints = 300
                };

                _logger.LogInformation("Running quick pure 1H backtest for last 3 weeks");

                var result = await _pure1HService.RunPure1HBacktestAsync(request);
                
                return Ok(new
                {
                    success = true,
                    message = "Quick pure 1H backtest completed",
                    data = result,
                    note = "All weekly data calculated from 1H candles"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running quick pure 1H backtest");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error running quick pure 1H backtest",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Run pure 1H backtest for specific date range
        /// </summary>
        [HttpGet("backtest-period")]
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

                var request = new Pure1HBacktestRequest
                {
                    FromDate = from,
                    ToDate = to,
                    InitialCapital = 100000,
                    LotSize = 50,
                    HedgePoints = 300
                };

                _logger.LogInformation("Running pure 1H backtest for period {FromDate} to {ToDate}", from, to);

                var result = await _pure1HService.RunPure1HBacktestAsync(request);
                
                return Ok(new
                {
                    success = true,
                    message = $"Pure 1H backtest completed for {from:yyyy-MM-dd} to {to:yyyy-MM-dd}",
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running period pure 1H backtest");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error running period backtest",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get detailed explanation of the pure 1H methodology
        /// </summary>
        [HttpGet("methodology")]
        public IActionResult GetMethodology()
        {
            var methodology = new
            {
                approach = "Pure 1H Candle-Based Processing",
                description = "Everything calculated from 1H candles - no separate daily/weekly feeds",
                
                dataAggregation = new
                {
                    weeklyData = new
                    {
                        high = "max(all 1H highs in week)",
                        low = "min(all 1H lows in week)", 
                        close = "close of last 1H candle in week",
                        calculation = "Real-time aggregation as each 1H candle closes"
                    },
                    fourHBodies = new
                    {
                        method = "Every 4 consecutive 1H candles = 1 four-hour period",
                        bodyCalculation = "open=first1H.open, close=fourth1H.close",
                        tracking = "max4HBody and min4HBody updated weekly"
                    }
                },

                weekDetection = new
                {
                    trigger = "Monday 00:00 or first 1H candle of new week",
                    processing = "Reset weekly variables, calculate zones from previous week",
                    stateManagement = "Track bars since week start for signal timing"
                },

                zoneCalculation = new
                {
                    timing = "At start of each new week using previous week's aggregated data",
                    upperZone = "top=max(prevWeek.high, prevWeek.max4HBody), bottom=min(...)",
                    lowerZone = "top=max(prevWeek.low, prevWeek.min4HBody), bottom=min(...)",
                    margins = "max((zone.top - zone.bottom) * 3, minTick * 5)"
                },

                signalTiming = new
                {
                    realTime = "Checked on every 1H candle close",
                    s1_s2 = "Only on 2nd 1H candle of week (barsSinceWeekStart == 2)",
                    s3_s8 = "Any 1H candle after 2nd (barsSinceWeekStart > 2)",
                    weeklyLimit = "Maximum 1 signal per week, then stop checking"
                },

                weeklyTracking = new
                {
                    variables = "weeklyHigh, weeklyLow, weeklyMaxClose, weeklyMinClose",
                    updates = "Updated with each new 1H candle",
                    purpose = "Support signal conditions requiring weekly min/max values"
                },

                advantages = new[]
                {
                    "Consistent data source - everything from 1H candles",
                    "Real-time processing capability",
                    "Accurate signal timing based on actual 1H closes", 
                    "No dependency on separate daily/weekly data feeds",
                    "Cleaner implementation and easier to maintain"
                }
            };

            return Ok(new
            {
                success = true,
                data = methodology
            });
        }

        /// <summary>
        /// Get current implementation status and signal definitions
        /// </summary>
        [HttpGet("signals")]
        public IActionResult GetSignalDefinitions()
        {
            var signals = new
            {
                implementationStatus = "✅ Pure 1H Candle-Based Processing",
                
                signals = new[]
                {
                    new {
                        id = "S1",
                        name = "Bear Trap",
                        timing = "2nd 1H candle only",
                        conditions = "firstCandle fake breakdown + secondCandle recovery",
                        direction = "Bullish",
                        optionType = "PE",
                        logic = "open >= lowerZone.bottom && close < lowerZone.bottom && currentClose > firstLow"
                    },
                    new {
                        id = "S2", 
                        name = "Support Hold",
                        timing = "2nd 1H candle only",
                        conditions = "bullish bias + zone proximity + support hold pattern",
                        direction = "Bullish", 
                        optionType = "PE",
                        logic = "9 conditions including bias=1 and zone margins"
                    },
                    new {
                        id = "S3",
                        name = "Resistance Hold", 
                        timing = "Any 1H candle after 2nd",
                        conditions = "bearish bias + resistance rejection",
                        direction = "Bearish",
                        optionType = "CE", 
                        logic = "Base conditions + (scenarioA on 2nd candle OR scenarioB anytime)"
                    },
                    new {
                        id = "S4",
                        name = "Bias Failure (Bullish)",
                        timing = "Any 1H candle after 2nd", 
                        conditions = "bearish bias + gap up + breakout logic",
                        direction = "Bullish",
                        optionType = "PE",
                        logic = "Day1: close > firstHigh, Day2+: green candle above firstHigh making new weekly high"
                    },
                    new {
                        id = "S5", 
                        name = "Bias Failure (Bearish)",
                        timing = "Any 1H candle after 2nd",
                        conditions = "bullish bias + gap down + weakness confirmed", 
                        direction = "Bearish",
                        optionType = "CE",
                        logic = "bias=1 + firstOpen < lowerZone.bottom + current close < firstLow"
                    },
                    new {
                        id = "S6",
                        name = "Weakness Confirmed",
                        timing = "Any 1H candle after 2nd",
                        conditions = "bearish bias + resistance test failure",
                        direction = "Bearish", 
                        optionType = "CE",
                        logic = "Same scenarios as S3 but different base conditions"
                    },
                    new {
                        id = "S7",
                        name = "1H Breakout Confirmed", 
                        timing = "Any 1H candle after 2nd",
                        conditions = "S4 breakout logic + location validation",
                        direction = "Bullish",
                        optionType = "PE",
                        logic = "S4 logic + not too close below resistance (0.40% rule)"
                    },
                    new {
                        id = "S8",
                        name = "1H Breakdown Confirmed",
                        timing = "Any 1H candle after 2nd", 
                        conditions = "breakdown logic + zone interaction",
                        direction = "Bearish",
                        optionType = "CE",
                        logic = "Breakdown pattern + touched upperZone + closed below resistance"
                    }
                }
            };

            return Ok(new
            {
                success = true,
                data = signals
            });
        }

        /// <summary>
        /// Health check for pure 1H signal service
        /// </summary>
        [HttpGet("health")]
        public IActionResult HealthCheck()
        {
            return Ok(new
            {
                success = true,
                message = "Pure1HSignal service is healthy",
                implementation = "✅ Pure 1H Candle-Based Processing",
                timestamp = DateTime.Now,
                version = "2.0.0 - Pure 1H Implementation",
                features = new[]
                {
                    "Weekly data aggregated from 1H candles",
                    "4H bodies calculated from consecutive 1H candles", 
                    "Real-time week detection and state management",
                    "Sequential 1H candle processing",
                    "Proper signal timing (S1,S2 on 2nd candle, S3-S8 after)",
                    "Option selling with hedge protection"
                }
            });
        }
    }
}