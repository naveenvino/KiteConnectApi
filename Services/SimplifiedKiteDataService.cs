using KiteConnect;
using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace KiteConnectApi.Services
{
    /// <summary>
    /// Simplified Kite data service that focuses on core functionality
    /// </summary>
    public class SimplifiedKiteDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SimplifiedKiteDataService> _logger;
        private readonly IConfiguration _configuration;

        public SimplifiedKiteDataService(
            ApplicationDbContext context,
            ILogger<SimplifiedKiteDataService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Get authenticated Kite instance
        /// </summary>
        private Kite GetKiteInstance()
        {
            var apiKey = _configuration["KiteConnect:ApiKey"];
            var accessToken = _configuration["KiteConnect:AccessToken"];

            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY_HERE")
            {
                throw new InvalidOperationException("Kite Connect API Key is not configured");
            }

            if (string.IsNullOrEmpty(accessToken) || accessToken == "YOUR_ACCESS_TOKEN_HERE")
            {
                throw new InvalidOperationException("Kite Connect Access Token is not configured");
            }

            var kite = new Kite(apiKey, Debug: true);
            kite.SetAccessToken(accessToken);
            return kite;
        }

        /// <summary>
        /// Test basic Kite Connect connectivity
        /// </summary>
        public async Task<KiteTestResult> TestConnectionAsync()
        {
            var result = new KiteTestResult();
            
            try
            {
                var kite = GetKiteInstance();
                
                // Test basic connectivity by getting margins
                var margins = kite.GetMargins();
                result.IsConnected = true;
                result.Message = "Successfully connected to Kite Connect API";
                
                // Test instruments access
                var instruments = kite.GetInstruments("NSE");
                result.InstrumentCount = instruments?.Count ?? 0;
                
                result.Success = true;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = ex.Message;
                result.IsConnected = false;
                _logger.LogError(ex, "Kite Connect test failed");
            }
            
            return result;
        }

        /// <summary>
        /// Get sample options data using mock generation
        /// This avoids KiteConnect API issues while providing test data
        /// </summary>
        public async Task<int> GenerateSampleOptionsDataAsync(
            DateTime fromDate,
            DateTime toDate,
            List<int> strikes,
            List<string> optionTypes)
        {
            try
            {
                _logger.LogInformation("Generating sample options data for {StrikeCount} strikes from {FromDate} to {ToDate}",
                    strikes.Count, fromDate, toDate);

                var recordsAdded = 0;
                var random = new Random();

                foreach (var strike in strikes)
                {
                    foreach (var optionType in optionTypes)
                    {
                        // Check if data already exists
                        var existingCount = await _context.OptionsHistoricalData
                            .CountAsync(d => d.Strike == strike &&
                                           d.OptionType == optionType &&
                                           d.Timestamp >= fromDate &&
                                           d.Timestamp <= toDate);

                        if (existingCount > 0)
                        {
                            _logger.LogDebug("Skipping {Strike}{OptionType} - {Count} records exist", strike, optionType, existingCount);
                            continue;
                        }

                        // Generate realistic sample data
                        var sampleData = GenerateRealisticOptionsData(strike, optionType, fromDate, toDate, random);
                        await _context.OptionsHistoricalData.AddRangeAsync(sampleData);
                        recordsAdded += sampleData.Count;

                        _logger.LogDebug("Generated {Count} records for {Strike}{OptionType}", sampleData.Count, strike, optionType);
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Sample data generation completed. Added {RecordsAdded} records", recordsAdded);

                return recordsAdded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sample options data");
                return 0;
            }
        }

        /// <summary>
        /// Generate realistic options data for testing
        /// </summary>
        private List<OptionsHistoricalData> GenerateRealisticOptionsData(
            int strike,
            string optionType,
            DateTime fromDate,
            DateTime toDate,
            Random random)
        {
            var data = new List<OptionsHistoricalData>();
            
            // Calculate base option price based on strike and market level
            var niftyLevel = 24000m; // Approximate current NIFTY level
            var basePrice = CalculateBaseOptionPrice(strike, optionType, niftyLevel);
            
            for (var date = fromDate; date <= toDate; date = date.AddMinutes(5))
            {
                // Skip weekends
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                // Only during market hours (9:15 AM to 3:30 PM)
                if (date.Hour < 9 || (date.Hour == 9 && date.Minute < 15) || 
                    date.Hour > 15 || (date.Hour == 15 && date.Minute > 30))
                    continue;

                // Add realistic price variation
                var priceVariation = (decimal)(random.NextDouble() * 0.2 - 0.1); // ±10% variation
                var currentPrice = Math.Max(0.5m, basePrice * (1 + priceVariation));
                
                var record = new OptionsHistoricalData
                {
                    TradingSymbol = GenerateTradingSymbol(strike, optionType, date),
                    Timestamp = date,
                    Exchange = "NFO",
                    Underlying = "NIFTY",
                    Strike = strike,
                    OptionType = optionType,
                    ExpiryDate = GetNextThursday(date),
                    Open = currentPrice,
                    High = currentPrice * 1.05m,
                    Low = currentPrice * 0.95m,
                    Close = currentPrice,
                    LastPrice = currentPrice,
                    Volume = 100 + random.Next(0, 2000),
                    OpenInterest = 1000 + random.Next(0, 20000),
                    ImpliedVolatility = 0.15m + (decimal)(random.NextDouble() * 0.4), // 15-55% IV
                    BidPrice = currentPrice * 0.995m,
                    AskPrice = currentPrice * 1.005m,
                    BidAskSpread = currentPrice * 0.01m,
                    DataSource = "SampleData",
                    Interval = "5minute"
                };

                data.Add(record);
            }

            return data;
        }

        /// <summary>
        /// Calculate base option price using simplified Black-Scholes
        /// </summary>
        private decimal CalculateBaseOptionPrice(int strike, string optionType, decimal underlyingPrice)
        {
            var timeToExpiry = 7m / 365m; // Assume 1 week to expiry
            var impliedVol = 0.25m; // 25% IV
            var riskFreeRate = 0.06m; // 6% risk-free rate

            if (optionType == "CE")
            {
                var intrinsic = Math.Max(0, underlyingPrice - strike);
                var timeValue = (decimal)Math.Sqrt((double)timeToExpiry) * impliedVol * underlyingPrice * 0.4m;
                return Math.Max(0.5m, intrinsic + timeValue);
            }
            else // PE
            {
                var intrinsic = Math.Max(0, strike - underlyingPrice);
                var timeValue = (decimal)Math.Sqrt((double)timeToExpiry) * impliedVol * underlyingPrice * 0.4m;
                return Math.Max(0.5m, intrinsic + timeValue);
            }
        }

        /// <summary>
        /// Generate trading symbol
        /// </summary>
        private string GenerateTradingSymbol(int strike, string optionType, DateTime date)
        {
            var expiry = GetNextThursday(date);
            return $"NIFTY{expiry:yyMdd}{strike}{optionType}";
        }

        /// <summary>
        /// Get next Thursday (standard NIFTY expiry)
        /// </summary>
        private DateTime GetNextThursday(DateTime date)
        {
            var daysUntilThursday = ((int)DayOfWeek.Thursday - (int)date.DayOfWeek + 7) % 7;
            if (daysUntilThursday == 0 && date.DayOfWeek == DayOfWeek.Thursday && date.Hour > 15)
                daysUntilThursday = 7; // Next Thursday if after expiry time
            return date.Date.AddDays(daysUntilThursday);
        }

        /// <summary>
        /// Get login URL for authentication (simplified)
        /// </summary>
        public string GetLoginUrl()
        {
            var apiKey = _configuration["KiteConnect:ApiKey"];
            if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY_HERE")
            {
                return "Please configure your Kite Connect API Key first";
            }

            var redirectUrl = _configuration["KiteConnect:RedirectUrl"] ?? "http://localhost:3000/callback";
            return $"https://kite.trade/connect/login?api_key={apiKey}&v=3";
        }

        /// <summary>
        /// Get configuration status
        /// </summary>
        public async Task<ConfigurationStatus> GetConfigurationStatusAsync()
        {
            var status = new ConfigurationStatus();
            
            var apiKey = _configuration["KiteConnect:ApiKey"];
            var accessToken = _configuration["KiteConnect:AccessToken"];
            
            status.ApiKeyConfigured = !string.IsNullOrEmpty(apiKey) && apiKey != "YOUR_API_KEY_HERE";
            status.AccessTokenConfigured = !string.IsNullOrEmpty(accessToken) && accessToken != "YOUR_ACCESS_TOKEN_HERE";
            status.IsConfigured = status.ApiKeyConfigured && status.AccessTokenConfigured;
            
            if (status.IsConfigured)
            {
                try
                {
                    var testResult = await TestConnectionAsync();
                    status.ConnectionWorking = testResult.Success;
                    status.ErrorMessage = testResult.Message;
                }
                catch (Exception ex)
                {
                    status.ConnectionWorking = false;
                    status.ErrorMessage = ex.Message;
                }
            }
            else
            {
                status.ErrorMessage = "API Key or Access Token not configured";
            }
            
            return status;
        }
    }

    public class KiteTestResult
    {
        public bool Success { get; set; }
        public bool IsConnected { get; set; }
        public string Message { get; set; } = string.Empty;
        public int InstrumentCount { get; set; }
    }

    public class ConfigurationStatus
    {
        public bool ApiKeyConfigured { get; set; }
        public bool AccessTokenConfigured { get; set; }
        public bool IsConfigured { get; set; }
        public bool ConnectionWorking { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }
}