using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KiteConnectApi.Controllers
{
    /// <summary>
    /// Simplified Kite Connect controller for basic functionality
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SimpleKiteController : ControllerBase
    {
        private readonly SimplifiedKiteDataService _kiteService;
        private readonly ILogger<SimpleKiteController> _logger;

        public SimpleKiteController(
            SimplifiedKiteDataService kiteService,
            ILogger<SimpleKiteController> logger)
        {
            _kiteService = kiteService;
            _logger = logger;
        }

        /// <summary>
        /// Check Kite Connect configuration status
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult<ConfigurationStatus>> GetStatusAsync()
        {
            try
            {
                var status = await _kiteService.GetConfigurationStatusAsync();
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking Kite status");
                return StatusCode(500, new { error = "Failed to check status", message = ex.Message });
            }
        }

        /// <summary>
        /// Get Kite Connect login URL
        /// </summary>
        [HttpGet("login-url")]
        public ActionResult<object> GetLoginUrl()
        {
            try
            {
                var loginUrl = _kiteService.GetLoginUrl();
                
                return Ok(new
                {
                    success = true,
                    loginUrl = loginUrl,
                    instructions = new[]
                    {
                        "1. Configure your API Key in appsettings.json",
                        "2. Visit the login URL to get access token",
                        "3. Configure the access token in appsettings.json",
                        "4. Test the connection using /test-connection"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting login URL");
                return StatusCode(500, new { error = "Failed to get login URL", message = ex.Message });
            }
        }

        /// <summary>
        /// Test Kite Connect API connection
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<ActionResult<KiteTestResult>> TestConnectionAsync()
        {
            try
            {
                var result = await _kiteService.TestConnectionAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection test failed");
                return StatusCode(500, new { error = "Connection test failed", message = ex.Message });
            }
        }

        /// <summary>
        /// Generate sample options data for testing backtesting
        /// </summary>
        [HttpPost("generate-sample-data")]
        public async Task<ActionResult<object>> GenerateSampleDataAsync([FromBody] SampleDataRequest request)
        {
            try
            {
                _logger.LogInformation("Generating sample data for {Days} days", (request.ToDate - request.FromDate).TotalDays);

                var recordsAdded = await _kiteService.GenerateSampleOptionsDataAsync(
                    request.FromDate,
                    request.ToDate,
                    request.Strikes,
                    request.OptionTypes);

                var result = new
                {
                    success = true,
                    recordsAdded = recordsAdded,
                    period = new
                    {
                        from = request.FromDate,
                        to = request.ToDate,
                        totalDays = (request.ToDate - request.FromDate).TotalDays
                    },
                    coverage = new
                    {
                        strikes = request.Strikes,
                        optionTypes = request.OptionTypes,
                        combinations = request.Strikes.Count * request.OptionTypes.Count
                    },
                    message = $"Successfully generated {recordsAdded} sample records for backtesting",
                    nextSteps = new[]
                    {
                        "Data is now ready for backtesting",
                        "Use /api/HistoricalBacktesting/run to run AI-enhanced backtests",
                        "Use /api/HistoricalData/test-price to verify data quality"
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sample data");
                return StatusCode(500, new { error = "Failed to generate sample data", message = ex.Message });
            }
        }

        /// <summary>
        /// Get sample parameters for data generation
        /// </summary>
        [HttpGet("sample-params")]
        public ActionResult<SampleDataRequest> GetSampleParams()
        {
            var request = new SampleDataRequest
            {
                FromDate = DateTime.Today.AddDays(-7),
                ToDate = DateTime.Today,
                Strikes = GenerateATMStrikes(24000), // Around current NIFTY level
                OptionTypes = new List<string> { "CE", "PE" }
            };

            return Ok(request);
        }

        /// <summary>
        /// Quick setup for backtesting - generates comprehensive sample data
        /// </summary>
        [HttpPost("quick-setup")]
        public async Task<ActionResult<object>> QuickSetupAsync()
        {
            try
            {
                _logger.LogInformation("Running quick setup for backtesting");

                // Generate last 7 days of data with comprehensive strike coverage
                var fromDate = DateTime.Today.AddDays(-7);
                var toDate = DateTime.Today;
                var strikes = GenerateComprehensiveStrikes(23000, 25000, 50);
                var optionTypes = new List<string> { "CE", "PE" };

                var recordsAdded = await _kiteService.GenerateSampleOptionsDataAsync(
                    fromDate, toDate, strikes, optionTypes);

                var result = new
                {
                    success = true,
                    setupType = "Quick Setup for Backtesting",
                    recordsAdded = recordsAdded,
                    coverage = new
                    {
                        period = $"{fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
                        strikeRange = "23000 to 25000 (50-point intervals)",
                        totalStrikes = strikes.Count,
                        totalCombinations = strikes.Count * optionTypes.Count,
                        estimatedDataPoints = recordsAdded
                    },
                    readyFor = new[]
                    {
                        "AI-Enhanced Historical Backtesting",
                        "Pattern Recognition Analysis", 
                        "Market Sentiment Correlation",
                        "Risk Management Validation"
                    },
                    quickTest = new
                    {
                        endpoint = "/api/HistoricalBacktesting/quick-analysis",
                        description = "Run quick AI analysis on generated data"
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quick setup failed");
                return StatusCode(500, new { error = "Quick setup failed", message = ex.Message });
            }
        }

        private List<int> GenerateATMStrikes(int atmLevel)
        {
            var strikes = new List<int>();
            for (int i = atmLevel - 500; i <= atmLevel + 500; i += 50)
            {
                strikes.Add(i);
            }
            return strikes;
        }

        private List<int> GenerateComprehensiveStrikes(int minStrike, int maxStrike, int interval)
        {
            var strikes = new List<int>();
            for (int strike = minStrike; strike <= maxStrike; strike += interval)
            {
                strikes.Add(strike);
            }
            return strikes;
        }
    }

    public class SampleDataRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<int> Strikes { get; set; } = new();
        public List<string> OptionTypes { get; set; } = new();
    }
}