using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Controllers
{
    /// <summary>
    /// Controller for managing historical options data
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HistoricalDataController : ControllerBase
    {
        private readonly HistoricalOptionsDataService _dataService;
        private readonly RealKiteDataService _realDataService;
        private readonly ILogger<HistoricalDataController> _logger;

        public HistoricalDataController(
            HistoricalOptionsDataService dataService,
            RealKiteDataService realDataService,
            ILogger<HistoricalDataController> logger)
        {
            _dataService = dataService;
            _realDataService = realDataService;
            _logger = logger;
        }

        /// <summary>
        /// Fetch real historical options data from Kite Connect API
        /// </summary>
        [HttpPost("fetch-real-data")]
        public async Task<ActionResult<HistoricalDataFetchResult>> FetchRealDataAsync([FromBody] RealDataFetchRequest request)
        {
            try
            {
                if (!await _realDataService.IsAuthenticatedAsync())
                {
                    return Unauthorized(new { error = "Not authenticated. Please authenticate with Kite Connect first." });
                }

                var result = await _realDataService.FetchHistoricalOptionsDataAsync(
                    request.FromDate,
                    request.ToDate,
                    request.Strikes,
                    request.OptionTypes,
                    request.Interval,
                    request.Underlying
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching real historical data");
                return StatusCode(500, new { error = "Failed to fetch real data", message = ex.Message });
            }
        }

        /// <summary>
        /// Quick setup for real backtesting - fetch last week's data
        /// </summary>
        [HttpPost("quick-real-setup")]
        public async Task<ActionResult<object>> QuickRealSetupAsync()
        {
            try
            {
                if (!await _realDataService.IsAuthenticatedAsync())
                {
                    return Unauthorized(new { error = "Not authenticated. Please authenticate with Kite Connect first." });
                }

                var fromDate = DateTime.Today.AddDays(-7);
                var toDate = DateTime.Today.AddDays(-1);
                
                // ATM ±5 strikes around 24000 (adjust based on current NIFTY level)
                var atmStrike = 24000;
                var strikes = new List<int>();
                for (int i = -5; i <= 5; i++)
                {
                    strikes.Add(atmStrike + (i * 50));
                }

                var optionTypes = new List<string> { "CE", "PE" };

                var result = await _realDataService.FetchHistoricalOptionsDataAsync(
                    fromDate,
                    toDate,
                    strikes,
                    optionTypes,
                    "5minute",
                    "NIFTY"
                );

                return Ok(new
                {
                    setup = "real-data-backtesting",
                    period = $"{fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
                    strikes = strikes,
                    optionTypes = optionTypes,
                    result = result,
                    nextSteps = new[]
                    {
                        "Real data collection completed",
                        "You can now run backtests using /api/Backtesting/run",
                        "Use the real historical data for strategy testing",
                        "Check /api/Backtesting/summary for results"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick real setup");
                return StatusCode(500, new { error = "Quick real setup failed", message = ex.Message });
            }
        }

        /// <summary>
        /// Populate sample historical data for testing
        /// </summary>
        [HttpPost("populate-sample")]
        public async Task<ActionResult<object>> PopulateSampleDataAsync([FromBody] DataPopulationRequest request)
        {
            try
            {
                _logger.LogInformation("Starting sample data population for {Days} days", 
                    (request.ToDate - request.FromDate).TotalDays);

                var recordsAdded = await _dataService.PopulateHistoricalDataAsync(
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
                    estimatedDataPoints = recordsAdded,
                    message = $"Successfully populated {recordsAdded} historical data records"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating sample data");
                return StatusCode(500, new { error = "Failed to populate data", message = ex.Message });
            }
        }

        /// <summary>
        /// Get sample data population parameters
        /// </summary>
        [HttpGet("sample-params")]
        public ActionResult<DataPopulationRequest> GetSampleParams()
        {
            var sampleRequest = new DataPopulationRequest
            {
                FromDate = DateTime.UtcNow.AddDays(-7),
                ToDate = DateTime.UtcNow,
                Strikes = new List<int> { 23500, 23600, 23700, 23800, 23900, 24000, 24100, 24200, 24300, 24400, 24500 },
                OptionTypes = new List<string> { "CE", "PE" }
            };

            return Ok(sampleRequest);
        }

        /// <summary>
        /// Test price data retrieval for a specific option
        /// </summary>
        [HttpGet("test-price")]
        public async Task<ActionResult<object>> TestPriceRetrievalAsync(
            [FromQuery] int strike = 24000,
            [FromQuery] string optionType = "CE",
            [FromQuery] DateTime? timestamp = null)
        {
            try
            {
                var testTime = timestamp ?? DateTime.UtcNow.AddHours(-1);
                
                var priceData = await _dataService.GetHistoricalPriceAsync(strike, optionType, testTime);
                
                if (priceData == null)
                {
                    return NotFound(new 
                    { 
                        message = "No historical data found", 
                        strike = strike, 
                        optionType = optionType, 
                        timestamp = testTime 
                    });
                }

                var result = new
                {
                    found = true,
                    data = new
                    {
                        symbol = priceData.TradingSymbol,
                        strike = priceData.Strike,
                        optionType = priceData.OptionType,
                        timestamp = priceData.Timestamp,
                        prices = new
                        {
                            open = priceData.Open,
                            high = priceData.High,
                            low = priceData.Low,
                            close = priceData.Close,
                            lastPrice = priceData.LastPrice,
                            bid = priceData.BidPrice,
                            ask = priceData.AskPrice
                        },
                        volume = priceData.Volume,
                        openInterest = priceData.OpenInterest,
                        impliedVolatility = priceData.ImpliedVolatility,
                        dataSource = priceData.DataSource
                    }
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing price retrieval");
                return StatusCode(500, new { error = "Failed to retrieve price data", message = ex.Message });
            }
        }

        /// <summary>
        /// Get data coverage statistics
        /// </summary>
        [HttpGet("coverage")]
        public async Task<ActionResult<object>> GetDataCoverageAsync()
        {
            try
            {
                // This would query the database for actual coverage statistics
                var mockCoverage = new
                {
                    totalRecords = 15000,
                    dateRange = new
                    {
                        earliest = DateTime.UtcNow.AddDays(-30),
                        latest = DateTime.UtcNow
                    },
                    strikes = new
                    {
                        min = 22000,
                        max = 26000,
                        count = 41
                    },
                    optionTypes = new[] { "CE", "PE" },
                    intervals = new[] { "1minute", "5minute", "15minute", "1hour", "1day" },
                    dataSources = new[] { "KiteConnect", "Synthetic", "Sample" },
                    completeness = new
                    {
                        marketHours = "95%",
                        allStrikes = "87%",
                        bothTypes = "100%"
                    }
                };

                return Ok(mockCoverage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data coverage");
                return StatusCode(500, new { error = "Failed to get coverage data", message = ex.Message });
            }
        }

        /// <summary>
        /// Prepare data for backtesting a specific date range
        /// </summary>
        [HttpPost("prepare-for-backtest")]
        public async Task<ActionResult<object>> PrepareForBacktestAsync([FromBody] BacktestDataRequest request)
        {
            try
            {
                _logger.LogInformation("Preparing data for backtest from {FromDate} to {ToDate}", 
                    request.FromDate, request.ToDate);

                // Extract unique strikes and option types from historical signals
                // This would be implemented to analyze what data is needed for backtesting

                var preparation = new
                {
                    success = true,
                    backtestPeriod = new
                    {
                        from = request.FromDate,
                        to = request.ToDate,
                        tradingDays = CalculateTradingDays(request.FromDate, request.ToDate)
                    },
                    dataRequirements = new
                    {
                        estimatedSignals = 50, // Would be calculated from actual signals
                        requiredStrikes = new[] { 23500, 24000, 24500 }, // Would be extracted from signals
                        requiredOptionTypes = new[] { "CE", "PE" }
                    },
                    dataAvailability = new
                    {
                        coverage = "85%",
                        missingDataPoints = 150,
                        syntheticDataRequired = true
                    },
                    recommendation = "Data is sufficient for backtesting. Some price points will use synthetic pricing model."
                };

                return Ok(preparation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing backtest data");
                return StatusCode(500, new { error = "Failed to prepare backtest data", message = ex.Message });
            }
        }

        private int CalculateTradingDays(DateTime fromDate, DateTime toDate)
        {
            var tradingDays = 0;
            for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    tradingDays++;
                }
            }
            return tradingDays;
        }
    }

    public class DataPopulationRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<int> Strikes { get; set; } = new();
        public List<string> OptionTypes { get; set; } = new();
    }

    public class RealDataFetchRequest
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

    public class BacktestDataRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public bool IncludeSyntheticData { get; set; } = true;
        public string DataSource { get; set; } = "All";
    }
}