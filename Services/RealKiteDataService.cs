using KiteConnect;
using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KiteConnectApi.Services
{
    /// <summary>
    /// Real Kite Connect data service that works with current API version
    /// </summary>
    public class RealKiteDataService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RealKiteDataService> _logger;
        private readonly ApplicationDbContext _context;
        private Kite? _kite;
        private string? _accessToken;
        private readonly string _tokensFilePath;

        public RealKiteDataService(
            IConfiguration configuration,
            ILogger<RealKiteDataService> logger,
            ApplicationDbContext context)
        {
            _configuration = configuration;
            _logger = logger;
            _context = context;
            _tokensFilePath = Path.Combine(Environment.CurrentDirectory, "kite_tokens.json");
        }

        /// <summary>
        /// Initialize Kite Connect API
        /// </summary>
        private async Task<bool> InitializeKiteAsync()
        {
            try
            {
                var apiKey = _configuration["KiteConnect:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogError("KiteConnect ApiKey not configured");
                    return false;
                }

                _kite = new Kite(apiKey, Debug: false);

                // Try to load saved access token
                if (await LoadSavedTokenAsync())
                {
                    return true;
                }

                _logger.LogWarning("No valid access token found. Please authenticate first.");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Kite Connect API");
                return false;
            }
        }

        /// <summary>
        /// Load saved access token
        /// </summary>
        private async Task<bool> LoadSavedTokenAsync()
        {
            try
            {
                if (!File.Exists(_tokensFilePath))
                {
                    return false;
                }

                var json = await File.ReadAllTextAsync(_tokensFilePath);
                var tokens = JsonSerializer.Deserialize<RealKiteTokens>(json);

                if (tokens?.AccessToken == null)
                {
                    return false;
                }

                _kite!.SetAccessToken(tokens.AccessToken);
                _accessToken = tokens.AccessToken;

                // Test the connection
                try
                {
                    var profile = _kite.GetProfile();
                    if (!string.IsNullOrEmpty(profile.UserName))
                    {
                        _logger.LogInformation("Loaded saved tokens successfully");
                        return true;
                    }
                }
                catch
                {
                    // Token might be expired
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load saved tokens");
                return false;
            }
        }

        /// <summary>
        /// Get login URL for authentication
        /// </summary>
        public string GetLoginUrl()
        {
            var apiKey = _configuration["KiteConnect:ApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new InvalidOperationException("KiteConnect ApiKey not configured");
            }

            var kite = new Kite(apiKey, Debug: false);
            return kite.GetLoginURL();
        }

        /// <summary>
        /// Authenticate using request token
        /// </summary>
        public async Task<bool> AuthenticateAsync(string requestToken)
        {
            try
            {
                var apiKey = _configuration["KiteConnect:ApiKey"];
                var apiSecret = _configuration["KiteConnect:ApiSecret"];

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiSecret))
                {
                    _logger.LogError("KiteConnect API credentials not configured");
                    return false;
                }

                _kite = new Kite(apiKey, Debug: false);
                var user = _kite.GenerateSession(requestToken, apiSecret);

                _accessToken = user.AccessToken;
                _kite.SetAccessToken(_accessToken);

                // Save tokens
                await SaveTokensAsync(user.AccessToken, user.RefreshToken);

                _logger.LogInformation("Authentication successful");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Authentication failed");
                return false;
            }
        }

        /// <summary>
        /// Check if authenticated
        /// </summary>
        public async Task<bool> IsAuthenticatedAsync()
        {
            if (_kite == null || string.IsNullOrEmpty(_accessToken))
            {
                return await InitializeKiteAsync();
            }

            try
            {
                var profile = _kite.GetProfile();
                return !string.IsNullOrEmpty(profile.UserName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Authentication check failed");
                return false;
            }
        }

        /// <summary>
        /// Fetch real historical options data
        /// </summary>
        public async Task<HistoricalDataFetchResult> FetchHistoricalOptionsDataAsync(
            DateTime fromDate,
            DateTime toDate,
            List<int> strikes,
            List<string> optionTypes,
            string interval = "5minute",
            string underlying = "NIFTY")
        {
            var result = new HistoricalDataFetchResult
            {
                StartTime = DateTime.UtcNow,
                Success = false,
                RequestedStrikes = strikes,
                RequestedOptionTypes = optionTypes,
                FromDate = fromDate,
                ToDate = toDate
            };

            try
            {
                if (!await IsAuthenticatedAsync())
                {
                    result.ErrorMessage = "Not authenticated with Kite Connect";
                    return result;
                }

                _logger.LogInformation("Fetching historical data for {StrikeCount} strikes from {FromDate} to {ToDate}",
                    strikes.Count, fromDate, toDate);

                // Get instruments
                var instruments = _kite!.GetInstruments("NFO");
                var niftyOptions = instruments
                    .Where(i => i.Name == underlying && 
                               (i.InstrumentType == "CE" || i.InstrumentType == "PE"))
                    .ToList();

                result.AvailableInstruments = niftyOptions.Count;
                var recordsAdded = 0;

                foreach (var strike in strikes)
                {
                    foreach (var optionType in optionTypes)
                    {
                        try
                        {
                            // Find instrument for this strike and option type
                            var instrument = niftyOptions
                                .Where(i => i.Strike == strike && i.InstrumentType == optionType)
                                .Where(i => i.Expiry >= fromDate.Date)
                                .OrderBy(i => i.Expiry)
                                .FirstOrDefault();

                            if (instrument.InstrumentToken == 0)
                            {
                                result.Errors.Add($"No instrument found for {strike}{optionType}");
                                continue;
                            }

                            // Check if data already exists
                            var existingCount = await _context.OptionsHistoricalData
                                .CountAsync(d => d.TradingSymbol == instrument.TradingSymbol &&
                                               d.Timestamp >= fromDate &&
                                               d.Timestamp <= toDate);

                            if (existingCount > 0)
                            {
                                _logger.LogDebug("Skipping {Symbol} - {Count} records already exist", 
                                    instrument.TradingSymbol, existingCount);
                                result.SkippedInstruments++;
                                continue;
                            }

                            // Fetch historical data
                            var historicalData = _kite.GetHistoricalData(
                                instrument.InstrumentToken.ToString(),
                                fromDate,
                                toDate,
                                interval,
                                false);

                            if (historicalData?.Count > 0)
                            {
                                var optionsData = historicalData.Select(h => new OptionsHistoricalData
                                {
                                    TradingSymbol = instrument.TradingSymbol,
                                    Timestamp = h.TimeStamp,
                                    Exchange = instrument.Exchange,
                                    Underlying = instrument.Name,
                                    Strike = (int)instrument.Strike,
                                    OptionType = instrument.InstrumentType,
                                    ExpiryDate = instrument.Expiry ?? DateTime.UtcNow.AddDays(7),
                                    Open = h.Open,
                                    High = h.High,
                                    Low = h.Low,
                                    Close = h.Close,
                                    LastPrice = h.Close,
                                    Volume = (long)h.Volume,
                                    OpenInterest = (long)h.OI,
                                    DataSource = "KiteConnect",
                                    Interval = interval,
                                    CreatedAt = DateTime.UtcNow
                                }).ToList();

                                await _context.OptionsHistoricalData.AddRangeAsync(optionsData);
                                recordsAdded += optionsData.Count;
                                result.ProcessedInstruments++;

                                _logger.LogDebug("Added {Count} records for {Symbol}", 
                                    optionsData.Count, instrument.TradingSymbol);
                            }

                            // Rate limiting
                            await Task.Delay(100);
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Error fetching {strike}{optionType}: {ex.Message}");
                            _logger.LogError(ex, "Error fetching data for {Strike}{OptionType}", strike, optionType);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                result.Success = true;
                result.RecordsAdded = recordsAdded;
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;

                _logger.LogInformation("Historical data fetch completed. Added {RecordsAdded} records", recordsAdded);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
                _logger.LogError(ex, "Fatal error during historical data fetch");
                return result;
            }
        }

        /// <summary>
        /// Get available option instruments
        /// </summary>
        public async Task<List<OptionsInstrumentInfo>> GetAvailableInstrumentsAsync(string underlying = "NIFTY")
        {
            try
            {
                if (!await IsAuthenticatedAsync())
                {
                    return new List<OptionsInstrumentInfo>();
                }

                var instruments = _kite!.GetInstruments("NFO");
                var niftyOptions = instruments
                    .Where(i => i.Name == underlying && 
                               (i.InstrumentType == "CE" || i.InstrumentType == "PE"))
                    .Select(i => new OptionsInstrumentInfo
                    {
                        TradingSymbol = i.TradingSymbol,
                        InstrumentToken = i.InstrumentToken,
                        Strike = (int)i.Strike,
                        OptionType = i.InstrumentType,
                        Expiry = i.Expiry ?? DateTime.Today,
                        LotSize = i.LotSize
                    })
                    .OrderBy(i => i.Expiry)
                    .ThenBy(i => i.Strike)
                    .ThenBy(i => i.OptionType)
                    .ToList();

                return niftyOptions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching instruments");
                return new List<OptionsInstrumentInfo>();
            }
        }

        /// <summary>
        /// Save tokens to file
        /// </summary>
        private async Task SaveTokensAsync(string accessToken, string refreshToken)
        {
            try
            {
                var tokens = new RealKiteTokens
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    SavedAt = DateTime.UtcNow
                };

                var json = JsonSerializer.Serialize(tokens, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_tokensFilePath, json);
                
                _logger.LogInformation("Tokens saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save tokens");
            }
        }
    }

    // Supporting classes
    public class HistoricalDataFetchResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<int> RequestedStrikes { get; set; } = new();
        public List<string> RequestedOptionTypes { get; set; } = new();
        public int AvailableInstruments { get; set; }
        public int ProcessedInstruments { get; set; }
        public int SkippedInstruments { get; set; }
        public int RecordsAdded { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    public class OptionsInstrumentInfo
    {
        public string TradingSymbol { get; set; } = string.Empty;
        public uint InstrumentToken { get; set; }
        public int Strike { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public DateTime Expiry { get; set; }
        public uint LotSize { get; set; }
    }

    public class RealKiteTokens
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; }
    }
}