using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace KiteConnectApi.Services
{
    public class NiftyIndexDataCollectionService
    {
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly ILogger<NiftyIndexDataCollectionService> _logger;
        private readonly IConfiguration _configuration;

        // NIFTY 50 Index Constants
        private const int NIFTY_INSTRUMENT_TOKEN = 256265;
        private const string NIFTY_SYMBOL = "NIFTY_INDEX";
        private const string KITE_BASE_URL = "https://api.kite.trade";

        public NiftyIndexDataCollectionService(
            ApplicationDbContext context,
            HttpClient httpClient,
            ILogger<NiftyIndexDataCollectionService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Collect NIFTY 50 index historical data from Kite Connect API
        /// </summary>
        public async Task<NiftyDataCollectionResult> CollectNiftyIndexDataAsync(NiftyDataCollectionRequest request)
        {
            var result = new NiftyDataCollectionResult
            {
                FromDate = request.FromDate,
                ToDate = request.ToDate,
                Interval = request.Interval,
                Success = false,
                CollectedCandles = new List<NiftyIndexRecord>(),
                Issues = new List<string>()
            };

            try
            {
                // Validate API credentials
                var apiKey = _configuration["KiteConnect:ApiKey"];
                var accessToken = _configuration["KiteConnect:AccessToken"];

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(accessToken))
                {
                    result.Issues.Add("❌ Kite Connect API credentials not configured");
                    result.Issues.Add("💡 Set KiteConnect:ApiKey and KiteConnect:AccessToken in configuration");
                    return result;
                }

                // Validate date range against API limits
                var validation = ValidateDateRange(request);
                if (!validation.IsValid)
                {
                    result.Issues.AddRange(validation.Issues);
                    return result;
                }

                _logger.LogInformation("Collecting NIFTY 50 index data from {FromDate} to {ToDate} with {Interval} interval", 
                    request.FromDate, request.ToDate, request.Interval);

                // Fetch data from Kite Connect API
                var kiteData = await FetchHistoricalDataFromKiteAsync(apiKey, accessToken, request);
                
                if (kiteData == null || !kiteData.Any())
                {
                    result.Issues.Add("❌ No data received from Kite Connect API");
                    return result;
                }

                result.Issues.Add($"✅ Received {kiteData.Count} candles from Kite Connect API");

                // Convert to our format
                var indexRecords = ConvertKiteDataToIndexRecords(kiteData);
                result.CollectedCandles = indexRecords;

                // Save to database
                if (request.SaveToDatabase)
                {
                    var savedCount = await SaveIndexDataToDatabaseAsync(indexRecords);
                    result.Issues.Add($"✅ Saved {savedCount} NIFTY index candles to database");
                }

                result.Success = true;
                result.Issues.Add($"🎉 Successfully collected NIFTY 50 index data");

            }
            catch (HttpRequestException ex)
            {
                result.Issues.Add($"❌ API Error: {ex.Message}");
                _logger.LogError(ex, "HTTP error while fetching NIFTY index data");
            }
            catch (JsonException ex)
            {
                result.Issues.Add($"❌ JSON Parse Error: {ex.Message}");
                _logger.LogError(ex, "JSON parsing error while processing Kite API response");
            }
            catch (Exception ex)
            {
                result.Issues.Add($"❌ Unexpected Error: {ex.Message}");
                _logger.LogError(ex, "Unexpected error during NIFTY index data collection");
            }

            return result;
        }

        /// <summary>
        /// Get current NIFTY 50 real-time quote
        /// </summary>
        public async Task<NiftyQuoteResult> GetCurrentNiftyQuoteAsync()
        {
            var result = new NiftyQuoteResult { Success = false };

            try
            {
                var apiKey = _configuration["KiteConnect:ApiKey"];
                var accessToken = _configuration["KiteConnect:AccessToken"];

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(accessToken))
                {
                    result.Error = "API credentials not configured";
                    return result;
                }

                // Setup HTTP headers
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("X-Kite-Version", "3");
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {apiKey}:{accessToken}");

                // Call quote API
                var url = $"{KITE_BASE_URL}/quote?i=NSE:NIFTY+50";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var quoteData = ParseQuoteResponse(jsonContent);
                    
                    result.Success = true;
                    result.Quote = quoteData;
                    result.Timestamp = DateTime.Now;
                }
                else
                {
                    result.Error = $"API call failed: {response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                _logger.LogError(ex, "Error fetching current NIFTY quote");
            }

            return result;
        }

        private DateValidationResult ValidateDateRange(NiftyDataCollectionRequest request)
        {
            var result = new DateValidationResult { IsValid = true, Issues = new List<string>() };
            var daysDifference = (request.ToDate - request.FromDate).TotalDays;

            // Check API limits based on interval
            var limits = new Dictionary<string, int>
            {
                { "minute", 60 },
                { "3minute", 100 },
                { "5minute", 100 },
                { "10minute", 100 },
                { "15minute", 200 },
                { "30minute", 200 },
                { "60minute", 400 },
                { "day", 2000 }
            };

            if (limits.ContainsKey(request.Interval))
            {
                var maxDays = limits[request.Interval];
                if (daysDifference > maxDays)
                {
                    result.IsValid = false;
                    result.Issues.Add($"❌ Date range too large for {request.Interval} interval");
                    result.Issues.Add($"📊 Maximum {maxDays} days allowed, requested {daysDifference:F0} days");
                    result.Issues.Add($"💡 Consider using smaller date ranges or higher intervals");
                }
            }

            return result;
        }

        private async Task<List<decimal[]>> FetchHistoricalDataFromKiteAsync(
            string apiKey, 
            string accessToken, 
            NiftyDataCollectionRequest request)
        {
            // Setup HTTP headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Kite-Version", "3");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {apiKey}:{accessToken}");

            // Format dates for API
            var fromDate = request.FromDate.ToString("yyyy-MM-dd");
            var toDate = request.ToDate.ToString("yyyy-MM-dd");

            // Build API URL
            var url = $"{KITE_BASE_URL}/instruments/historical/{NIFTY_INSTRUMENT_TOKEN}/{request.Interval}?" +
                     $"from={fromDate}&to={toDate}&continuous=0&oi=0";

            _logger.LogInformation("Fetching NIFTY data from Kite API: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Kite API error: {response.StatusCode} - {errorContent}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            return ParseHistoricalDataResponse(jsonContent);
        }

        private List<decimal[]> ParseHistoricalDataResponse(string jsonContent)
        {
            var candles = new List<decimal[]>();

            try
            {
                using var document = JsonDocument.Parse(jsonContent);
                
                if (document.RootElement.TryGetProperty("data", out var dataElement) &&
                    dataElement.TryGetProperty("candles", out var candlesElement))
                {
                    foreach (var candle in candlesElement.EnumerateArray())
                    {
                        var candleData = new decimal[6]; // timestamp, o, h, l, c, v
                        
                        var elements = candle.EnumerateArray().ToArray();
                        if (elements.Length >= 5)
                        {
                            // Parse timestamp (ISO format)
                            if (DateTime.TryParse(elements[0].GetString(), out var timestamp))
                            {
                                candleData[0] = new DateTimeOffset(timestamp).ToUnixTimeSeconds();
                            }
                            
                            candleData[1] = elements[1].GetDecimal(); // Open
                            candleData[2] = elements[2].GetDecimal(); // High
                            candleData[3] = elements[3].GetDecimal(); // Low
                            candleData[4] = elements[4].GetDecimal(); // Close
                            candleData[5] = elements.Length > 5 ? elements[5].GetDecimal() : 0; // Volume
                            
                            candles.Add(candleData);
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing Kite API response");
                throw;
            }

            return candles;
        }

        private NiftyQuote ParseQuoteResponse(string jsonContent)
        {
            using var document = JsonDocument.Parse(jsonContent);
            
            var niftyKey = "256265"; // NIFTY 50 instrument token as string key
            
            if (document.RootElement.TryGetProperty("data", out var dataElement) &&
                dataElement.TryGetProperty(niftyKey, out var niftyElement))
            {
                return new NiftyQuote
                {
                    InstrumentToken = NIFTY_INSTRUMENT_TOKEN,
                    LastPrice = niftyElement.GetProperty("last_price").GetDecimal(),
                    Open = niftyElement.GetProperty("ohlc").GetProperty("open").GetDecimal(),
                    High = niftyElement.GetProperty("ohlc").GetProperty("high").GetDecimal(),
                    Low = niftyElement.GetProperty("ohlc").GetProperty("low").GetDecimal(),
                    Close = niftyElement.GetProperty("ohlc").GetProperty("close").GetDecimal()
                };
            }

            throw new JsonException("NIFTY 50 data not found in quote response");
        }

        private List<NiftyIndexRecord> ConvertKiteDataToIndexRecords(List<decimal[]> kiteData)
        {
            var records = new List<NiftyIndexRecord>();

            foreach (var candle in kiteData)
            {
                if (candle.Length >= 5)
                {
                    var timestamp = DateTimeOffset.FromUnixTimeSeconds((long)candle[0]).DateTime;
                    
                    records.Add(new NiftyIndexRecord
                    {
                        Timestamp = timestamp,
                        Open = candle[1],
                        High = candle[2],
                        Low = candle[3],
                        Close = candle[4],
                        Volume = candle.Length > 5 ? (long)candle[5] : 0,
                        Source = "Kite Connect API",
                        Symbol = NIFTY_SYMBOL
                    });
                }
            }

            return records;
        }

        private async Task<int> SaveIndexDataToDatabaseAsync(List<NiftyIndexRecord> records)
        {
            var savedCount = 0;

            try
            {
                foreach (var record in records)
                {
                    // Check if record already exists in the dedicated NIFTY index table
                    var existing = await _context.NiftyIndexHistoricalData
                        .FirstOrDefaultAsync(d => d.Symbol == NIFTY_SYMBOL &&
                                                 d.Timestamp == record.Timestamp &&
                                                 d.Interval == "60minute");

                    if (existing == null)
                    {
                        // Add new record to dedicated NIFTY index table
                        _context.NiftyIndexHistoricalData.Add(new NiftyIndexHistoricalData
                        {
                            Symbol = NIFTY_SYMBOL,
                            Timestamp = record.Timestamp,
                            Open = record.Open,
                            High = record.High,
                            Low = record.Low,
                            Close = record.Close,
                            Volume = record.Volume,
                            Interval = "60minute",
                            Source = record.Source,
                            CreatedAt = DateTime.UtcNow
                        });
                        savedCount++;
                    }
                }

                if (savedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Saved {Count} new NIFTY index records to dedicated table", savedCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving NIFTY index data to dedicated table");
                throw;
            }

            return savedCount;
        }

        /// <summary>
        /// Check what NIFTY index data we currently have in database
        /// </summary>
        public async Task<NiftyDataInventoryResult> GetNiftyDataInventoryAsync()
        {
            var result = new NiftyDataInventoryResult();

            try
            {
                var indexData = await _context.NiftyIndexHistoricalData
                    .Where(d => d.Symbol == NIFTY_SYMBOL)
                    .OrderBy(d => d.Timestamp)
                    .ToListAsync();

                if (indexData.Any())
                {
                    result.HasData = true;
                    result.TotalRecords = indexData.Count;
                    result.FirstRecord = indexData.First().Timestamp;
                    result.LastRecord = indexData.Last().Timestamp;
                    result.DateRange = $"{result.FirstRecord:yyyy-MM-dd} to {result.LastRecord:yyyy-MM-dd}";
                    
                    // Group by date to show daily coverage
                    result.DailyCoverage = indexData
                        .GroupBy(d => d.Timestamp.Date)
                        .ToDictionary(g => g.Key, g => g.Count());
                }
                else
                {
                    result.HasData = false;
                    result.Message = "No NIFTY index data found in dedicated table";
                }
            }
            catch (Exception ex)
            {
                result.HasData = false;
                result.Message = $"Error checking data inventory: {ex.Message}";
                _logger.LogError(ex, "Error checking NIFTY data inventory");
            }

            return result;
        }
    }

    // Supporting classes
    public class NiftyDataCollectionRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Interval { get; set; } = "60minute"; // Default to 1-hour
        public bool SaveToDatabase { get; set; } = true;
    }

    public class NiftyDataCollectionResult
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Interval { get; set; } = string.Empty;
        public bool Success { get; set; }
        public List<NiftyIndexRecord> CollectedCandles { get; set; } = new();
        public List<string> Issues { get; set; } = new();
    }

    public class NiftyIndexRecord
    {
        public DateTime Timestamp { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
    }

    public class NiftyQuoteResult
    {
        public bool Success { get; set; }
        public NiftyQuote? Quote { get; set; }
        public DateTime Timestamp { get; set; }
        public string Error { get; set; } = string.Empty;
    }

    public class NiftyQuote
    {
        public int InstrumentToken { get; set; }
        public decimal LastPrice { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
    }

    public class DateValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Issues { get; set; } = new();
    }

    public class NiftyDataInventoryResult
    {
        public bool HasData { get; set; }
        public int TotalRecords { get; set; }
        public DateTime FirstRecord { get; set; }
        public DateTime LastRecord { get; set; }
        public string DateRange { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Dictionary<DateTime, int> DailyCoverage { get; set; } = new();
    }
}