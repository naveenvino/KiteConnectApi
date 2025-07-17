using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Controllers
{
    /// <summary>
    /// Controller for real historical options data from Kite Connect
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class RealDataController : ControllerBase
    {
        private readonly RealKiteDataService _realDataService;
        private readonly ILogger<RealDataController> _logger;

        public RealDataController(
            RealKiteDataService realDataService,
            ILogger<RealDataController> logger)
        {
            _realDataService = realDataService;
            _logger = logger;
        }

        /// <summary>
        /// STEP 1: Get Kite Connect login URL
        /// </summary>
        [HttpGet("login-url")]
        public ActionResult<object> GetLoginUrl()
        {
            try
            {
                var loginUrl = _realDataService.GetLoginUrl();
                
                return Ok(new
                {
                    success = true,
                    loginUrl = loginUrl,
                    instructions = new[]
                    {
                        "1. Click on the login URL above",
                        "2. Login to your Kite account",
                        "3. Authorize the app",
                        "4. Copy the 'request_token' from the callback URL",
                        "5. Use it in POST /api/RealData/authenticate"
                    },
                    nextStep = "POST /api/RealData/authenticate"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating login URL");
                return StatusCode(500, new { error = "Failed to generate login URL", message = ex.Message });
            }
        }

        /// <summary>
        /// STEP 2: Authenticate using request token from Kite Connect
        /// </summary>
        [HttpPost("authenticate")]
        public async Task<ActionResult<object>> AuthenticateAsync([FromBody] RealDataAuthRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RequestToken))
                {
                    return BadRequest(new { error = "Request token is required" });
                }

                var success = await _realDataService.AuthenticateAsync(request.RequestToken);
                
                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Authentication successful! You can now fetch historical data.",
                        nextSteps = new[]
                        {
                            "Use GET /api/RealData/instruments to see available options",
                            "Use POST /api/RealData/fetch-historical to get real data",
                            "Use POST /api/RealData/quick-setup for immediate backtesting"
                        }
                    });
                }
                else
                {
                    return BadRequest(new { error = "Authentication failed. Please check your request token." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication failed");
                return StatusCode(500, new { error = "Authentication failed", message = ex.Message });
            }
        }

        /// <summary>
        /// STEP 3: Check authentication status
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<object>> GetStatusAsync()
        {
            try
            {
                var isAuthenticated = await _realDataService.IsAuthenticatedAsync();
                
                if (isAuthenticated)
                {
                    return Ok(new
                    {
                        authenticated = true,
                        message = "Ready to fetch real historical data!",
                        availableActions = new[]
                        {
                            "GET /api/RealData/instruments - View available options",
                            "POST /api/RealData/fetch-historical - Fetch historical data",
                            "POST /api/RealData/quick-setup - Quick backtesting setup"
                        }
                    });
                }
                else
                {
                    return Ok(new
                    {
                        authenticated = false,
                        message = "Not authenticated. Please login first.",
                        loginUrl = _realDataService.GetLoginUrl()
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking status");
                return StatusCode(500, new { error = "Failed to check status", message = ex.Message });
            }
        }

        /// <summary>
        /// STEP 4: Get available options instruments
        /// </summary>
        [HttpGet("instruments")]
        public async Task<ActionResult<object>> GetInstrumentsAsync([FromQuery] string underlying = "NIFTY")
        {
            try
            {
                var instruments = await _realDataService.GetAvailableInstrumentsAsync(underlying);
                
                if (!instruments.Any())
                {
                    return Ok(new
                    {
                        message = "No instruments found. Please check authentication.",
                        authenticated = await _realDataService.IsAuthenticatedAsync()
                    });
                }

                var groupedByExpiry = instruments
                    .GroupBy(i => i.Expiry.Date)
                    .OrderBy(g => g.Key)
                    .Take(3) // Next 3 expiries
                    .Select(g => new
                    {
                        expiry = g.Key,
                        strikes = g.Select(i => i.Strike).Distinct().OrderBy(s => s).ToList(),
                        totalInstruments = g.Count()
                    })
                    .ToList();

                return Ok(new
                {
                    underlying = underlying,
                    totalInstruments = instruments.Count,
                    expiries = groupedByExpiry,
                    availableStrikes = instruments.Select(i => i.Strike).Distinct().OrderBy(s => s).ToList(),
                    optionTypes = instruments.Select(i => i.OptionType).Distinct().ToList(),
                    recommendation = new
                    {
                        suggestedStrikes = "Select ATM ±5 strikes for backtesting",
                        suggestedPeriod = "Last 7-30 days for initial testing",
                        dataInterval = "5minute recommended for options"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching instruments");
                return StatusCode(500, new { error = "Failed to fetch instruments", message = ex.Message });
            }
        }

        /// <summary>
        /// STEP 5: Fetch real historical options data
        /// </summary>
        [HttpPost("fetch-historical")]
        public async Task<ActionResult<HistoricalDataFetchResult>> FetchHistoricalDataAsync(
            [FromBody] RealHistoricalDataRequest request)
        {
            try
            {
                var result = await _realDataService.FetchHistoricalOptionsDataAsync(
                    request.FromDate,
                    request.ToDate,
                    request.Strikes,
                    request.OptionTypes,
                    request.Interval,
                    request.Underlying);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(new { error = result.ErrorMessage, details = result });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching historical data");
                return StatusCode(500, new { error = "Failed to fetch historical data", message = ex.Message });
            }
        }

        /// <summary>
        /// STEP 6: Quick setup for immediate backtesting
        /// </summary>
        [HttpPost("quick-setup")]
        public async Task<ActionResult<object>> QuickSetupAsync()
        {
            try
            {
                // Get current NIFTY level (approximate)
                var currentNifty = 24000; // You can fetch this from live quotes
                
                // Generate ATM ±5 strikes
                var strikes = new List<int>();
                for (int i = -5; i <= 5; i++)
                {
                    strikes.Add(currentNifty + (i * 50));
                }

                var request = new RealHistoricalDataRequest
                {
                    FromDate = DateTime.Today.AddDays(-7),
                    ToDate = DateTime.Today.AddDays(-1),
                    Strikes = strikes,
                    OptionTypes = new List<string> { "CE", "PE" },
                    Interval = "5minute",
                    Underlying = "NIFTY"
                };

                var result = await _realDataService.FetchHistoricalOptionsDataAsync(
                    request.FromDate,
                    request.ToDate,
                    request.Strikes,
                    request.OptionTypes,
                    request.Interval,
                    request.Underlying);

                return Ok(new
                {
                    setup = "Real Data Quick Setup",
                    period = $"{request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}",
                    strikes = request.Strikes,
                    optionTypes = request.OptionTypes,
                    result = result,
                    nextSteps = new[]
                    {
                        "Real historical data collected successfully!",
                        "Now run backtests with: POST /api/Backtesting/run",
                        "Data is stored in database for fast backtesting",
                        "Check /api/Backtesting/summary for results"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick setup");
                return StatusCode(500, new { error = "Quick setup failed", message = ex.Message });
            }
        }

        /// <summary>
        /// Helper: Get suggested parameters for data fetching
        /// </summary>
        [HttpGet("suggested-params")]
        public ActionResult<object> GetSuggestedParams()
        {
            var currentNifty = 24000; // Approximate current level
            var strikes = new List<int>();
            
            // ATM ±10 strikes
            for (int i = -10; i <= 10; i++)
            {
                strikes.Add(currentNifty + (i * 50));
            }

            return Ok(new
            {
                recommended = new
                {
                    fromDate = DateTime.Today.AddDays(-7),
                    toDate = DateTime.Today.AddDays(-1),
                    strikes = strikes,
                    optionTypes = new[] { "CE", "PE" },
                    interval = "5minute",
                    underlying = "NIFTY"
                },
                notes = new[]
                {
                    "Start with last 7 days for testing",
                    "Use ATM ±5 strikes for focused backtesting",
                    "5minute interval is optimal for options",
                    "Each day = ~75 data points per strike per option type"
                },
                estimatedDataPoints = strikes.Count * 2 * 7 * 75 // strikes * types * days * intervals
            });
        }
    }

    public class RealDataAuthRequest
    {
        [Required]
        public string RequestToken { get; set; } = string.Empty;
    }

    public class RealHistoricalDataRequest
    {
        [Required]
        public DateTime FromDate { get; set; }
        
        [Required]
        public DateTime ToDate { get; set; }
        
        [Required]
        public List<int> Strikes { get; set; } = new();
        
        [Required]
        public List<string> OptionTypes { get; set; } = new();
        
        public string Interval { get; set; } = "5minute";
        
        public string Underlying { get; set; } = "NIFTY";
    }
}