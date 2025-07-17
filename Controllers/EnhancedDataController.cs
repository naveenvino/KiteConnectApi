using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Controllers
{
    /// <summary>
    /// Enhanced controller for comprehensive historical options data collection
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EnhancedDataController : ControllerBase
    {
        private readonly EnhancedKiteDataService _enhancedDataService;
        private readonly ILogger<EnhancedDataController> _logger;

        public EnhancedDataController(
            EnhancedKiteDataService enhancedDataService,
            ILogger<EnhancedDataController> logger)
        {
            _enhancedDataService = enhancedDataService;
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
                var loginUrl = _enhancedDataService.GetLoginUrl();
                
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
                        "5. Use it in POST /api/EnhancedData/authenticate"
                    },
                    nextStep = "POST /api/EnhancedData/authenticate"
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
        public async Task<ActionResult<object>> AuthenticateAsync([FromBody] EnhancedAuthRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.RequestToken))
                {
                    return BadRequest(new { error = "Request token is required" });
                }

                var success = await _enhancedDataService.AuthenticateAsync(request.RequestToken);
                
                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Authentication successful! Ready for enhanced data collection.",
                        nextSteps = new[]
                        {
                            "Use POST /api/EnhancedData/fetch-comprehensive to get 1-minute precision data",
                            "This will collect minute, 5-minute, and 15-minute intervals",
                            "Open Interest data will be included for better analysis"
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
                var isAuthenticated = await _enhancedDataService.IsAuthenticatedAsync();
                
                if (isAuthenticated)
                {
                    return Ok(new
                    {
                        authenticated = true,
                        message = "Ready for enhanced historical data collection!",
                        availableActions = new[]
                        {
                            "POST /api/EnhancedData/fetch-comprehensive - Collect multi-interval data",
                            "Intervals: 1-minute, 5-minute, 15-minute",
                            "Includes: OHLC, Volume, Open Interest, Timestamps"
                        }
                    });
                }
                else
                {
                    return Ok(new
                    {
                        authenticated = false,
                        message = "Not authenticated. Please login first.",
                        loginUrl = _enhancedDataService.GetLoginUrl()
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
        /// STEP 4: Fetch comprehensive historical data with multiple intervals and Open Interest
        /// </summary>
        [HttpPost("fetch-comprehensive")]
        public async Task<ActionResult<ComprehensiveDataResult>> FetchComprehensiveDataAsync(
            [FromBody] ComprehensiveDataRequest request)
        {
            try
            {
                if (!await _enhancedDataService.IsAuthenticatedAsync())
                {
                    return Unauthorized(new { error = "Not authenticated. Please authenticate first." });
                }

                var result = await _enhancedDataService.FetchComprehensiveOptionsDataAsync(
                    request.FromDate,
                    request.ToDate,
                    request.Strikes,
                    request.OptionTypes,
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
                _logger.LogError(ex, "Error fetching comprehensive data");
                return StatusCode(500, new { error = "Failed to fetch comprehensive data", message = ex.Message });
            }
        }

        /// <summary>
        /// STEP 5: Quick comprehensive setup for immediate precision backtesting
        /// </summary>
        [HttpPost("quick-comprehensive-setup")]
        public async Task<ActionResult<object>> QuickComprehensiveSetupAsync()
        {
            try
            {
                if (!await _enhancedDataService.IsAuthenticatedAsync())
                {
                    return Unauthorized(new { error = "Not authenticated. Please authenticate first." });
                }

                // Get current NIFTY level (approximate)
                var currentNifty = 24000; // You can fetch this from live quotes
                
                // Generate ATM ±3 strikes for focused analysis
                var strikes = new List<int>();
                for (int i = -3; i <= 3; i++)
                {
                    strikes.Add(currentNifty + (i * 50));
                }

                var request = new ComprehensiveDataRequest
                {
                    FromDate = DateTime.Today.AddDays(-7),
                    ToDate = DateTime.Today.AddDays(-1),
                    Strikes = strikes,
                    OptionTypes = new List<string> { "CE", "PE" },
                    Underlying = "NIFTY"
                };

                var result = await _enhancedDataService.FetchComprehensiveOptionsDataAsync(
                    request.FromDate,
                    request.ToDate,
                    request.Strikes,
                    request.OptionTypes,
                    request.Underlying);

                return Ok(new
                {
                    setup = "Enhanced Comprehensive Data Collection",
                    period = $"{request.FromDate:yyyy-MM-dd} to {request.ToDate:yyyy-MM-dd}",
                    strikes = request.Strikes,
                    optionTypes = request.OptionTypes,
                    intervals = new[] { "1minute", "5minute", "15minute" },
                    result = result,
                    advantages = new[]
                    {
                        "✅ 1-minute precision for exact entry/exit timing",
                        "✅ Open Interest data for liquidity analysis", 
                        "✅ Multiple timeframes for multi-scale analysis",
                        "✅ Enhanced rate limiting for reliable data collection",
                        "✅ Perfect for high-frequency backtesting"
                    },
                    nextSteps = new[]
                    {
                        "Enhanced precision data collected successfully!",
                        "Now run backtests with minute-level accuracy",
                        "Use POST /api/Backtesting/run for strategy testing",
                        "Check trade execution with exact timing precision"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick comprehensive setup");
                return StatusCode(500, new { error = "Quick comprehensive setup failed", message = ex.Message });
            }
        }

        /// <summary>
        /// Get recommended parameters for precision data collection
        /// </summary>
        [HttpGet("precision-params")]
        public ActionResult<object> GetPrecisionParams()
        {
            var currentNifty = 24000; // Approximate current level
            var strikes = new List<int>();
            
            // ATM ±5 strikes for comprehensive coverage
            for (int i = -5; i <= 5; i++)
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
                    underlying = "NIFTY"
                },
                intervals = new
                {
                    minute = "1-minute intervals for precise entry/exit timing",
                    fiveMinute = "5-minute intervals for strategy signals",
                    fifteenMinute = "15-minute intervals for trend analysis"
                },
                dataPoints = new
                {
                    perDay = new
                    {
                        minute = 375, // 6h 15m * 60 minutes
                        fiveMinute = 75, // 6h 15m / 5 minutes  
                        fifteenMinute = 25 // 6h 15m / 15 minutes
                    },
                    estimated = strikes.Count * 2 * 7 * (375 + 75 + 25) // strikes * types * days * total intervals
                },
                advantages = new[]
                {
                    "Minute-level precision for exact trade timing",
                    "Open Interest for liquidity and market sentiment",
                    "Multiple timeframes for comprehensive analysis",
                    "Rate-limited collection for reliability"
                }
            });
        }
    }

    public class EnhancedAuthRequest
    {
        [Required]
        public string RequestToken { get; set; } = string.Empty;
    }

    public class ComprehensiveDataRequest
    {
        [Required]
        public DateTime FromDate { get; set; }
        
        [Required]
        public DateTime ToDate { get; set; }
        
        [Required]
        public List<int> Strikes { get; set; } = new();
        
        [Required]
        public List<string> OptionTypes { get; set; } = new();
        
        public string Underlying { get; set; } = "NIFTY";
    }
}