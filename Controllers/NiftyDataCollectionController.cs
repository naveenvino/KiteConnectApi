using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NiftyDataCollectionController : ControllerBase
    {
        private readonly NiftyIndexDataCollectionService _dataCollectionService;
        private readonly ILogger<NiftyDataCollectionController> _logger;

        public NiftyDataCollectionController(
            NiftyIndexDataCollectionService dataCollectionService,
            ILogger<NiftyDataCollectionController> logger)
        {
            _dataCollectionService = dataCollectionService;
            _logger = logger;
        }

        /// <summary>
        /// Collect NIFTY 50 index historical data from Kite Connect API
        /// </summary>
        [HttpPost("collect")]
        public async Task<IActionResult> CollectNiftyIndexData([FromBody] NiftyDataCollectionRequest request)
        {
            try
            {
                _logger.LogInformation("Starting NIFTY index data collection from {FromDate} to {ToDate}", 
                    request.FromDate, request.ToDate);

                var result = await _dataCollectionService.CollectNiftyIndexDataAsync(request);
                
                return Ok(new
                {
                    success = result.Success,
                    message = result.Success ? "NIFTY index data collection completed" : "Data collection failed",
                    data = new
                    {
                        fromDate = result.FromDate,
                        toDate = result.ToDate,
                        interval = result.Interval,
                        collectedCandles = result.CollectedCandles.Count,
                        issues = result.Issues
                    },
                    candles = result.CollectedCandles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in NIFTY data collection");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error collecting NIFTY index data",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Quick collection for standard backtesting period (3 weeks)
        /// </summary>
        [HttpPost("collect-backtest-period")]
        public async Task<IActionResult> CollectBacktestPeriod()
        {
            try
            {
                var request = new NiftyDataCollectionRequest
                {
                    FromDate = new DateTime(2024, 6, 24), // Your backtest start
                    ToDate = new DateTime(2024, 7, 15),   // Your backtest end
                    Interval = "60minute",
                    SaveToDatabase = true
                };

                _logger.LogInformation("Collecting NIFTY data for standard backtesting period");

                var result = await _dataCollectionService.CollectNiftyIndexDataAsync(request);
                
                return Ok(new
                {
                    success = result.Success,
                    message = "Backtesting period NIFTY data collection completed",
                    data = new
                    {
                        period = "June 24 - July 15, 2024",
                        interval = "60minute (1-hour candles)",
                        collectedCandles = result.CollectedCandles.Count,
                        expectedCandles = "~315 (21 days × 6.5 hours)",
                        issues = result.Issues
                    },
                    summary = new
                    {
                        dataQuality = result.CollectedCandles.Count > 200 ? "✅ Good" : "⚠️ Check data gaps",
                        readyForBacktest = result.Success && result.CollectedCandles.Count > 0,
                        nextStep = result.Success ? "Run corrected signal backtest with real NIFTY index data" : "Fix API credentials and retry"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting backtesting period data");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error collecting backtesting period data",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Get current NIFTY 50 real-time quote
        /// </summary>
        [HttpGet("current-quote")]
        public async Task<IActionResult> GetCurrentNiftyQuote()
        {
            try
            {
                var result = await _dataCollectionService.GetCurrentNiftyQuoteAsync();
                
                if (result.Success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Current NIFTY 50 quote retrieved",
                        data = new
                        {
                            instrumentToken = result.Quote!.InstrumentToken,
                            lastPrice = result.Quote.LastPrice,
                            ohlc = new
                            {
                                open = result.Quote.Open,
                                high = result.Quote.High,
                                low = result.Quote.Low,
                                close = result.Quote.Close
                            },
                            timestamp = result.Timestamp,
                            notes = new[]
                            {
                                "This is live NIFTY 50 index price",
                                "Use this to verify your API connection",
                                "Strike prices will be calculated by signal logic"
                            }
                        }
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Failed to get current NIFTY quote",
                        error = result.Error
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current NIFTY quote");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error retrieving current quote",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Check what NIFTY index data we currently have in database
        /// </summary>
        [HttpGet("inventory")]
        public async Task<IActionResult> GetDataInventory()
        {
            try
            {
                var result = await _dataCollectionService.GetNiftyDataInventoryAsync();
                
                return Ok(new
                {
                    success = true,
                    message = "NIFTY index data inventory",
                    data = new
                    {
                        hasData = result.HasData,
                        totalRecords = result.TotalRecords,
                        dateRange = result.DateRange,
                        firstRecord = result.HasData ? result.FirstRecord : (DateTime?)null,
                        lastRecord = result.HasData ? result.LastRecord : (DateTime?)null,
                        dailyCoverage = result.DailyCoverage,
                        message = result.Message
                    },
                    analysis = result.HasData ? new
                    {
                        dataQuality = result.TotalRecords > 300 ? "✅ Excellent" : 
                                     result.TotalRecords > 200 ? "✅ Good" : 
                                     result.TotalRecords > 100 ? "⚠️ Partial" : "❌ Insufficient",
                        readyForBacktest = result.TotalRecords > 200,
                        recommendation = result.TotalRecords > 200 ? 
                            "Ready for signal backtesting" : 
                            "Collect more data for reliable backtesting"
                    } : new
                    {
                        dataQuality = "❌ No Data",
                        readyForBacktest = false,
                        recommendation = "Collect NIFTY index data first using /collect-backtest-period"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking data inventory");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error checking data inventory",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Test API connection to Kite Connect
        /// </summary>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestApiConnection()
        {
            try
            {
                // Test by getting current quote
                var quoteResult = await _dataCollectionService.GetCurrentNiftyQuoteAsync();
                
                return Ok(new
                {
                    success = quoteResult.Success,
                    message = quoteResult.Success ? "✅ Kite Connect API connection successful" : "❌ API connection failed",
                    connection = new
                    {
                        apiEndpoint = "https://api.kite.trade",
                        instrumentToken = 256265,
                        symbol = "NSE:NIFTY 50",
                        status = quoteResult.Success ? "Connected" : "Failed"
                    },
                    testResult = quoteResult.Success ? new
                    {
                        currentPrice = (decimal?)quoteResult.Quote!.LastPrice,
                        timestamp = (DateTime?)quoteResult.Timestamp,
                        error = (string?)null,
                        note = "API credentials are working correctly"
                    } : new
                    {
                        currentPrice = (decimal?)null,
                        timestamp = (DateTime?)null,
                        error = quoteResult.Error,
                        note = "Check API credentials in configuration"
                    },
                    nextSteps = quoteResult.Success ? new[]
                    {
                        "✅ API connection is working",
                        "🔄 Ready to collect historical data",
                        "📊 Use /collect-backtest-period to get data"
                    } : new[]
                    {
                        "❌ Fix API credentials in appsettings.json",
                        "🔑 Ensure KiteConnect:ApiKey is set",
                        "🎫 Ensure KiteConnect:AccessToken is set",
                        "💳 Verify Kite Connect subscription is active"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing API connection");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error testing API connection",
                    error = ex.Message,
                    troubleshooting = new[]
                    {
                        "Check internet connection",
                        "Verify API credentials",
                        "Ensure Kite Connect subscription is active",
                        "Check firewall settings"
                    }
                });
            }
        }

        /// <summary>
        /// Get API configuration guide
        /// </summary>
        [HttpGet("setup-guide")]
        public IActionResult GetSetupGuide()
        {
            return Ok(new
            {
                success = true,
                message = "NIFTY Index Data Collection Setup Guide",
                setup = new
                {
                    step1 = new
                    {
                        title = "🔑 Get Kite Connect API Access",
                        instructions = new[]
                        {
                            "1. Login to Zerodha Kite",
                            "2. Go to Apps section",
                            "3. Purchase Kite Connect API (₹2000/month)",
                            "4. Generate API Key and Secret"
                        }
                    },
                    step2 = new
                    {
                        title = "🎫 Generate Access Token",
                        python_code = @"
from kiteconnect import KiteConnect
api_key = 'your_api_key'
api_secret = 'your_api_secret'
kite = KiteConnect(api_key=api_key)

# Get login URL and complete login
login_url = kite.login_url()
# After login, get request_token from callback
request_token = 'from_callback_url'

# Generate access token
session = kite.generate_session(request_token, api_secret=api_secret)
access_token = session['access_token']
print(f'Access Token: {access_token}')
"
                    },
                    step3 = new
                    {
                        title = "⚙️ Configure API Credentials",
                        config = @"
// appsettings.json
{
  ""KiteConnect"": {
    ""ApiKey"": ""your_api_key_here"",
    ""AccessToken"": ""your_access_token_here""
  }
}
"
                    },
                    step4 = new
                    {
                        title = "🧪 Test & Collect Data",
                        endpoints = new[]
                        {
                            "GET /api/NiftyDataCollection/test-connection",
                            "POST /api/NiftyDataCollection/collect-backtest-period",
                            "GET /api/NiftyDataCollection/inventory"
                        }
                    }
                },
                niftyDetails = new
                {
                    instrumentToken = 256265,
                    symbol = "NSE:NIFTY 50",
                    dataLimit = "400 days for 60minute interval",
                    expectedCandles = "~315 for 3-week backtesting period"
                },
                correction = new
                {
                    title = "📊 IMPORTANT: Strike Price Calculation",
                    note = "Strike prices are determined by TradingView signal logic, not current NIFTY price",
                    workflow = new[]
                    {
                        "1. NIFTY Index 1H candles → Signal Detection (S1-S8)",
                        "2. Signal logic calculates → Strike Price (part of signal)",
                        "3. Strike price + option type → Options data lookup",
                        "4. Options entry/exit prices → P&L calculation"
                    },
                    example = new
                    {
                        signal = "S3 Resistance Hold triggers",
                        strikeCalculation = "Signal logic determines strike = 22500",
                        optionSelection = "22500CE (from signal) + 22800CE (hedge)",
                        trading = "Use options historical data for entry/exit prices"
                    }
                }
            });
        }
    }
}