using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KiteConnectApi.Services
{
    /// <summary>
    /// Service for managing historical options data for backtesting
    /// </summary>
    public class HistoricalOptionsDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HistoricalOptionsDataService> _logger;
        private readonly IKiteConnectService _kiteService;

        public HistoricalOptionsDataService(
            ApplicationDbContext context,
            ILogger<HistoricalOptionsDataService> logger,
            IKiteConnectService kiteService)
        {
            _context = context;
            _logger = logger;
            _kiteService = kiteService;
        }

        /// <summary>
        /// Get historical price for a specific option at a given time
        /// </summary>
        public async Task<OptionsHistoricalData?> GetHistoricalPriceAsync(
            int strike, 
            string optionType, 
            DateTime timestamp, 
            string underlying = "NIFTY")
        {
            try
            {
                // Find the closest available data point
                var data = await _context.OptionsHistoricalData
                    .Where(d => d.Strike == strike && 
                               d.OptionType == optionType && 
                               d.Underlying == underlying &&
                               d.Timestamp <= timestamp)
                    .OrderByDescending(d => d.Timestamp)
                    .FirstOrDefaultAsync();

                if (data == null)
                {
                    _logger.LogWarning("No historical data found for {Strike}{OptionType} at {Timestamp}", 
                        strike, optionType, timestamp);
                    
                    // Try to generate synthetic price if no exact data available
                    return await GenerateSyntheticPriceAsync(strike, optionType, timestamp, underlying);
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting historical price for {Strike}{OptionType}", strike, optionType);
                return null;
            }
        }

        /// <summary>
        /// Calculate simulated entry and exit prices for backtesting
        /// </summary>
        public async Task<BacktestPriceData> GetBacktestPricesAsync(
            TradingViewAlert signal, 
            DateTime entryTime, 
            DateTime? exitTime = null)
        {
            try
            {
                var result = new BacktestPriceData
                {
                    Strike = signal.Strike,
                    OptionType = signal.Type ?? "CE",
                    EntryTime = entryTime,
                    ExitTime = exitTime ?? entryTime.AddMinutes(30) // Default 30-min holding
                };

                // Get entry price
                var entryData = await GetHistoricalPriceAsync(signal.Strike, signal.Type ?? "CE", entryTime);
                if (entryData != null)
                {
                    result.EntryPrice = CalculateRealisticExecutionPrice(entryData, "BUY");
                    result.EntryVolume = entryData.Volume;
                    result.EntryOpenInterest = entryData.OpenInterest;
                    result.EntryImpliedVolatility = entryData.ImpliedVolatility ?? 0;
                }
                else
                {
                    // Fallback to synthetic pricing
                    result.EntryPrice = await GenerateSyntheticOptionPriceAsync(signal.Strike, signal.Type ?? "CE", entryTime);
                }

                // Get exit price
                var exitData = await GetHistoricalPriceAsync(signal.Strike, signal.Type ?? "CE", result.ExitTime);
                if (exitData != null)
                {
                    result.ExitPrice = CalculateRealisticExecutionPrice(exitData, "SELL");
                    result.ExitVolume = exitData.Volume;
                    result.ExitOpenInterest = exitData.OpenInterest;
                    result.ExitImpliedVolatility = exitData.ImpliedVolatility ?? 0;
                }
                else
                {
                    // Fallback to synthetic pricing
                    result.ExitPrice = await GenerateSyntheticOptionPriceAsync(signal.Strike, signal.Type ?? "CE", result.ExitTime);
                }

                // Calculate P&L
                result.PnL = (result.ExitPrice - result.EntryPrice) * 1; // 1 lot size for simplicity
                result.PnLPercentage = result.EntryPrice > 0 ? (result.PnL / result.EntryPrice) : 0;
                result.HoldingPeriodMinutes = (int)(result.ExitTime - result.EntryTime).TotalMinutes;

                _logger.LogDebug("Calculated backtest prices for {Strike}{OptionType}: Entry={EntryPrice}, Exit={ExitPrice}, P&L={PnL}", 
                    signal.Strike, signal.Type, result.EntryPrice, result.ExitPrice, result.PnL);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating backtest prices for {Strike}{OptionType}", signal.Strike, signal.Type);
                return new BacktestPriceData 
                { 
                    Strike = signal.Strike, 
                    OptionType = signal.Type ?? "CE",
                    EntryTime = entryTime,
                    ExitTime = exitTime ?? entryTime.AddMinutes(30)
                };
            }
        }

        /// <summary>
        /// Populate historical data from external sources
        /// </summary>
        public async Task<int> PopulateHistoricalDataAsync(
            DateTime fromDate, 
            DateTime toDate, 
            List<int> strikes, 
            List<string> optionTypes)
        {
            var recordsAdded = 0;
            
            try
            {
                _logger.LogInformation("Starting historical data population from {FromDate} to {ToDate}", fromDate, toDate);

                foreach (var strike in strikes)
                {
                    foreach (var optionType in optionTypes)
                    {
                        try
                        {
                            // Check if data already exists
                            var existingCount = await _context.OptionsHistoricalData
                                .CountAsync(d => d.Strike == strike && 
                                               d.OptionType == optionType && 
                                               d.Timestamp >= fromDate && 
                                               d.Timestamp <= toDate);

                            if (existingCount > 0)
                            {
                                _logger.LogDebug("Skipping {Strike}{OptionType} - {Count} records already exist", 
                                    strike, optionType, existingCount);
                                continue;
                            }

                            // Generate sample data (replace with real data fetching)
                            var sampleData = GenerateSampleHistoricalData(strike, optionType, fromDate, toDate);
                            
                            await _context.OptionsHistoricalData.AddRangeAsync(sampleData);
                            recordsAdded += sampleData.Count;

                            _logger.LogDebug("Added {Count} records for {Strike}{OptionType}", sampleData.Count, strike, optionType);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing {Strike}{OptionType}", strike, optionType);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Historical data population completed. Added {RecordsAdded} records", recordsAdded);

                return recordsAdded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating historical data");
                return recordsAdded;
            }
        }

        /// <summary>
        /// Generate synthetic price when historical data is not available
        /// </summary>
        private async Task<OptionsHistoricalData?> GenerateSyntheticPriceAsync(
            int strike, 
            string optionType, 
            DateTime timestamp, 
            string underlying)
        {
            try
            {
                // Get the nearest available underlying price (simulate NIFTY at ~24000 level)
                var underlyingPrice = 24000m + (decimal)(new Random().NextDouble() * 1000 - 500); // ±500 points variation
                
                // Calculate synthetic option price using Black-Scholes approximation
                var timeToExpiry = GetTimeToExpiry(timestamp);
                var impliedVol = 0.2m; // 20% IV assumption
                var riskFreeRate = 0.06m; // 6% risk-free rate
                
                var syntheticPrice = CalculateBlackScholesPrice(
                    underlyingPrice, strike, timeToExpiry, riskFreeRate, impliedVol, optionType == "CE");

                return new OptionsHistoricalData
                {
                    TradingSymbol = GenerateTradingSymbol(strike, optionType, timestamp),
                    Timestamp = timestamp,
                    Strike = strike,
                    OptionType = optionType,
                    ExpiryDate = GetNextThursday(timestamp),
                    Open = syntheticPrice,
                    High = syntheticPrice * 1.05m,
                    Low = syntheticPrice * 0.95m,
                    Close = syntheticPrice,
                    LastPrice = syntheticPrice,
                    Volume = 1000 + new Random().Next(0, 5000),
                    OpenInterest = 10000 + new Random().Next(0, 50000),
                    ImpliedVolatility = impliedVol,
                    BidPrice = syntheticPrice * 0.98m,
                    AskPrice = syntheticPrice * 1.02m,
                    DataSource = "Synthetic"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating synthetic price");
                return null;
            }
        }

        /// <summary>
        /// Calculate realistic execution price including slippage and bid-ask spread
        /// </summary>
        private decimal CalculateRealisticExecutionPrice(OptionsHistoricalData data, string side)
        {
            var basePrice = data.LastPrice;
            var bidAskSpread = (data.AskPrice ?? basePrice * 1.02m) - (data.BidPrice ?? basePrice * 0.98m);
            
            // Apply slippage and bid-ask spread
            var slippage = basePrice * 0.001m; // 0.1% slippage
            
            return side == "BUY" 
                ? basePrice + (bidAskSpread / 2) + slippage 
                : basePrice - (bidAskSpread / 2) - slippage;
        }

        /// <summary>
        /// Generate synthetic option price using simplified Black-Scholes
        /// </summary>
        private async Task<decimal> GenerateSyntheticOptionPriceAsync(int strike, string optionType, DateTime timestamp)
        {
            // Simplified pricing model - in production, use proper Black-Scholes
            var underlyingPrice = 24000m; // Approximate NIFTY level
            var timeToExpiry = GetTimeToExpiry(timestamp);
            var volatility = 0.2m;

            if (optionType == "CE")
            {
                var intrinsic = Math.Max(0, underlyingPrice - strike);
                var timeValue = (decimal)(Math.Sqrt((double)timeToExpiry) * (double)volatility * (double)underlyingPrice) * 0.1m;
                return intrinsic + timeValue;
            }
            else
            {
                var intrinsic = Math.Max(0, strike - underlyingPrice);
                var timeValue = (decimal)(Math.Sqrt((double)timeToExpiry) * (double)volatility * (double)underlyingPrice) * 0.1m;
                return intrinsic + timeValue;
            }
        }

        /// <summary>
        /// Generate sample historical data for testing
        /// </summary>
        private List<OptionsHistoricalData> GenerateSampleHistoricalData(
            int strike, 
            string optionType, 
            DateTime fromDate, 
            DateTime toDate)
        {
            var data = new List<OptionsHistoricalData>();
            var random = new Random();
            var basePrice = 100m + random.Next(0, 200); // Random base price

            for (var date = fromDate; date <= toDate; date = date.AddMinutes(5))
            {
                // Skip weekends
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                // Only during market hours (9:15 AM to 3:30 PM)
                if (date.Hour < 9 || (date.Hour == 9 && date.Minute < 15) || date.Hour > 15 || 
                    (date.Hour == 15 && date.Minute > 30))
                    continue;

                var price = basePrice + (decimal)(random.NextDouble() * 20 - 10); // ±10 price variation
                
                data.Add(new OptionsHistoricalData
                {
                    TradingSymbol = GenerateTradingSymbol(strike, optionType, date),
                    Timestamp = date,
                    Strike = strike,
                    OptionType = optionType,
                    ExpiryDate = GetNextThursday(date),
                    Open = price,
                    High = price * 1.02m,
                    Low = price * 0.98m,
                    Close = price,
                    LastPrice = price,
                    Volume = 100 + random.Next(0, 1000),
                    OpenInterest = 1000 + random.Next(0, 10000),
                    ImpliedVolatility = 0.15m + (decimal)(random.NextDouble() * 0.3), // 15-45% IV
                    BidPrice = price * 0.99m,
                    AskPrice = price * 1.01m,
                    DataSource = "Sample",
                    Interval = "5minute"
                });
            }

            return data;
        }

        private string GenerateTradingSymbol(int strike, string optionType, DateTime date)
        {
            var expiry = GetNextThursday(date);
            return $"NIFTY{expiry:yyMdd}{strike}{optionType}";
        }

        private DateTime GetNextThursday(DateTime date)
        {
            var daysUntilThursday = ((int)DayOfWeek.Thursday - (int)date.DayOfWeek + 7) % 7;
            if (daysUntilThursday == 0 && date.DayOfWeek == DayOfWeek.Thursday)
                daysUntilThursday = 7; // Next Thursday if today is Thursday
            return date.Date.AddDays(daysUntilThursday);
        }

        private decimal GetTimeToExpiry(DateTime timestamp)
        {
            var expiry = GetNextThursday(timestamp);
            var timeToExpiry = (expiry - timestamp).TotalDays / 365.0;
            return Math.Max(0.001m, (decimal)timeToExpiry); // Minimum 1 day
        }

        private decimal CalculateBlackScholesPrice(
            decimal spotPrice, 
            int strikePrice, 
            decimal timeToExpiry, 
            decimal riskFreeRate, 
            decimal volatility, 
            bool isCall)
        {
            // Simplified Black-Scholes implementation
            // In production, use a proper options pricing library
            
            var d1 = (Math.Log((double)(spotPrice / strikePrice)) + (double)(riskFreeRate + volatility * volatility / 2) * (double)timeToExpiry) 
                     / ((double)volatility * Math.Sqrt((double)timeToExpiry));
            var d2 = d1 - (double)volatility * Math.Sqrt((double)timeToExpiry);
            
            if (isCall)
            {
                return (decimal)((double)spotPrice * NormalCDF(d1) - (double)strikePrice * Math.Exp(-(double)riskFreeRate * (double)timeToExpiry) * NormalCDF(d2));
            }
            else
            {
                return (decimal)((double)strikePrice * Math.Exp(-(double)riskFreeRate * (double)timeToExpiry) * NormalCDF(-d2) - (double)spotPrice * NormalCDF(-d1));
            }
        }

        private double NormalCDF(double x)
        {
            // Approximation of normal cumulative distribution function
            return 0.5 * (1 + Math.Sign(x) * Math.Sqrt(1 - Math.Exp(-2 * x * x / Math.PI)));
        }
    }

    /// <summary>
    /// Backtest pricing data structure
    /// </summary>
    public class BacktestPriceData
    {
        public int Strike { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
        
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public long EntryVolume { get; set; }
        public long ExitVolume { get; set; }
        public long EntryOpenInterest { get; set; }
        public long ExitOpenInterest { get; set; }
        public decimal EntryImpliedVolatility { get; set; }
        public decimal ExitImpliedVolatility { get; set; }
        
        public decimal PnL { get; set; }
        public decimal PnLPercentage { get; set; }
        public int HoldingPeriodMinutes { get; set; }
        
        public bool IsRealisticData { get; set; } = false;
        public string DataSource { get; set; } = "Historical";
    }
}