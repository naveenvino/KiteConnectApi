using KiteConnectApi.Data;
using KiteConnectApi.Models.AI;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace KiteConnectApi.Services.AI
{
    /// <summary>
    /// Adaptive signal weighting service that dynamically adjusts signal weights
    /// based on performance, market conditions, and other factors
    /// </summary>
    public class AdaptiveSignalWeightingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdaptiveSignalWeightingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly Dictionary<string, SignalWeightingContext> _signalContexts;

        public AdaptiveSignalWeightingService(
            ApplicationDbContext context,
            ILogger<AdaptiveSignalWeightingService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _signalContexts = new Dictionary<string, SignalWeightingContext>();
        }

        /// <summary>
        /// Calculate adaptive weight for a trading signal
        /// </summary>
        public async Task<AdaptiveWeightResult> CalculateSignalWeightAsync(EnhancedTradingSignal signal)
        {
            try
            {
                _logger.LogInformation("Calculating adaptive weight for signal {SignalId}", signal.SignalId);

                var result = new AdaptiveWeightResult
                {
                    SignalId = signal.SignalId,
                    Timestamp = DateTime.UtcNow,
                    BaseWeight = 1.0m
                };

                // Get or create signal context
                var context = await GetOrCreateSignalContextAsync(signal.SignalId);

                // Calculate different weight components
                var performanceWeight = await CalculatePerformanceWeightAsync(signal, context);
                var marketConditionWeight = await CalculateMarketConditionWeightAsync(signal, context);
                var timeBasedWeight = CalculateTimeBasedWeight(signal, context);
                var volatilityWeight = await CalculateVolatilityWeightAsync(signal, context);
                var sentimentWeight = CalculateSentimentWeight(signal, context);
                var confidenceWeight = CalculateConfidenceWeight(signal, context);
                var diversificationWeight = await CalculateDiversificationWeightAsync(signal, context);

                // Store individual weights
                result.WeightComponents = new Dictionary<string, decimal>
                {
                    ["Performance"] = performanceWeight,
                    ["MarketCondition"] = marketConditionWeight,
                    ["TimeBased"] = timeBasedWeight,
                    ["Volatility"] = volatilityWeight,
                    ["Sentiment"] = sentimentWeight,
                    ["Confidence"] = confidenceWeight,
                    ["Diversification"] = diversificationWeight
                };

                // Calculate composite weight
                result.AdaptiveWeight = CalculateCompositeWeight(result.WeightComponents);

                // Apply bounds and constraints
                result.AdaptiveWeight = ApplyWeightConstraints(result.AdaptiveWeight, signal, context);

                // Update signal context with new data
                await UpdateSignalContextAsync(signal.SignalId, context, result);

                // Generate reasoning
                result.WeightingReason = GenerateWeightingReason(result);

                _logger.LogInformation("Adaptive weight calculated for signal {SignalId}: {Weight} (Base: {BaseWeight})", 
                    signal.SignalId, result.AdaptiveWeight, result.BaseWeight);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating adaptive weight for signal {SignalId}", signal.SignalId);
                return new AdaptiveWeightResult
                {
                    SignalId = signal.SignalId,
                    Timestamp = DateTime.UtcNow,
                    BaseWeight = 1.0m,
                    AdaptiveWeight = 0.5m, // Conservative fallback
                    WeightingReason = $"Error in weight calculation: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Calculate performance-based weight adjustment
        /// </summary>
        private async Task<decimal> CalculatePerformanceWeightAsync(EnhancedTradingSignal signal, SignalWeightingContext context)
        {
            try
            {
                // Get recent performance data
                var recentTrades = await _context.ApiTradeLog
                    .Where(t => t.SignalId == signal.SignalId && t.EntryTime >= DateTime.UtcNow.AddDays(-30))
                    .OrderByDescending(t => t.EntryTime)
                    .Take(20)
                    .ToListAsync();

                if (!recentTrades.Any())
                {
                    return 1.0m; // Neutral weight for new signals
                }

                // Calculate recent win rate
                var winRate = recentTrades.Count(t => t.Outcome == "WIN") / (decimal)recentTrades.Count;
                
                // Calculate recent average P&L
                var avgPnL = recentTrades.Where(t => t.PnL.HasValue).Average(t => t.PnL.Value);
                
                // Calculate recent Sharpe ratio
                var pnlValues = recentTrades.Where(t => t.PnL.HasValue).Select(t => (double)t.PnL.Value).ToList();
                var sharpeRatio = CalculateSharpeRatio(pnlValues);

                // Weight based on performance metrics
                var performanceWeight = 0.5m; // Base weight
                
                // Adjust for win rate (50% baseline)
                performanceWeight += (winRate - 0.5m) * 0.8m;
                
                // Adjust for P&L (positive adds, negative subtracts)
                performanceWeight += Math.Max(-0.3m, Math.Min(0.3m, avgPnL / 1000m));
                
                // Adjust for Sharpe ratio
                performanceWeight += Math.Max(-0.2m, Math.Min(0.2m, (decimal)sharpeRatio * 0.1m));

                // Apply decay for older performance
                var avgAge = recentTrades.Average(t => (DateTime.UtcNow - t.EntryTime).TotalDays);
                var decayFactor = Math.Max(0.7m, 1 - (decimal)avgAge / 30);
                performanceWeight *= decayFactor;

                return Math.Max(0.1m, Math.Min(2.0m, performanceWeight));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating performance weight for signal {SignalId}", signal.SignalId);
                return 1.0m;
            }
        }

        /// <summary>
        /// Calculate market condition-based weight adjustment
        /// </summary>
        private async Task<decimal> CalculateMarketConditionWeightAsync(EnhancedTradingSignal signal, SignalWeightingContext context)
        {
            try
            {
                var marketConditionWeight = 1.0m;

                // Get historical performance in similar market conditions
                var currentCondition = signal.MarketContext.MarketTrend;
                var currentVolatility = signal.MarketContext.VIXLevel;

                var historicalTrades = await _context.ApiTradeLog
                    .Where(t => t.SignalId == signal.SignalId && t.EntryTime >= DateTime.UtcNow.AddDays(-90))
                    .ToListAsync();

                if (historicalTrades.Any())
                {
                    // Filter trades by similar market conditions (simplified)
                    var similarConditionTrades = historicalTrades
                        .Where(t => IsSimilarMarketCondition(t, currentCondition, currentVolatility))
                        .ToList();

                    if (similarConditionTrades.Any())
                    {
                        var conditionWinRate = similarConditionTrades.Count(t => t.Outcome == "WIN") / (decimal)similarConditionTrades.Count;
                        var conditionAvgPnL = similarConditionTrades.Where(t => t.PnL.HasValue).Average(t => t.PnL.Value);

                        // Adjust weight based on performance in similar conditions
                        marketConditionWeight = 0.5m + (conditionWinRate - 0.5m) * 0.6m;
                        marketConditionWeight += Math.Max(-0.2m, Math.Min(0.2m, conditionAvgPnL / 1000m));
                    }
                }

                // Additional adjustments based on market regime
                marketConditionWeight *= GetMarketRegimeMultiplier(signal.MarketContext.MarketTrend);

                return Math.Max(0.2m, Math.Min(1.8m, marketConditionWeight));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating market condition weight for signal {SignalId}", signal.SignalId);
                return 1.0m;
            }
        }

        /// <summary>
        /// Calculate time-based weight adjustment
        /// </summary>
        private decimal CalculateTimeBasedWeight(EnhancedTradingSignal signal, SignalWeightingContext context)
        {
            try
            {
                var timeWeight = 1.0m;
                var now = DateTime.Now;

                // Hour of day adjustment
                var hourMultiplier = GetHourMultiplier(now.Hour);
                timeWeight *= hourMultiplier;

                // Day of week adjustment
                var dayMultiplier = GetDayOfWeekMultiplier(now.DayOfWeek);
                timeWeight *= dayMultiplier;

                // Time to expiry adjustment
                var timeToExpiry = GetTimeToExpiry(signal.Timestamp);
                var expiryMultiplier = GetExpiryMultiplier(timeToExpiry);
                timeWeight *= expiryMultiplier;

                // Market session adjustment
                var sessionMultiplier = GetMarketSessionMultiplier(now);
                timeWeight *= sessionMultiplier;

                return Math.Max(0.3m, Math.Min(1.5m, timeWeight));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating time-based weight for signal {SignalId}", signal.SignalId);
                return 1.0m;
            }
        }

        /// <summary>
        /// Calculate volatility-based weight adjustment
        /// </summary>
        private async Task<decimal> CalculateVolatilityWeightAsync(EnhancedTradingSignal signal, SignalWeightingContext context)
        {
            try
            {
                var volatilityWeight = 1.0m;
                var currentVIX = signal.MarketContext.VIXLevel;

                // Adjust based on VIX level
                volatilityWeight *= GetVIXMultiplier(currentVIX);

                // Get historical performance at different volatility levels
                var historicalPerformance = await GetHistoricalVolatilityPerformanceAsync(signal.SignalId);
                if (historicalPerformance.Any())
                {
                    var currentVolatilityRegime = GetVolatilityRegime(currentVIX);
                    var regimePerformance = historicalPerformance.FirstOrDefault(p => p.VolatilityRegime == currentVolatilityRegime);
                    
                    if (regimePerformance != null)
                    {
                        // Adjust weight based on historical performance in this volatility regime
                        var performanceMultiplier = Math.Max(0.5m, Math.Min(1.5m, regimePerformance.WinRate / 0.5m));
                        volatilityWeight *= performanceMultiplier;
                    }
                }

                return Math.Max(0.3m, Math.Min(1.7m, volatilityWeight));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating volatility weight for signal {SignalId}", signal.SignalId);
                return 1.0m;
            }
        }

        /// <summary>
        /// Calculate sentiment-based weight adjustment
        /// </summary>
        private decimal CalculateSentimentWeight(EnhancedTradingSignal signal, SignalWeightingContext context)
        {
            try
            {
                var sentimentWeight = 1.0m;
                var sentimentScore = signal.SentimentScore;

                // Adjust based on sentiment alignment
                var signalDirection = DetermineSignalDirection(signal);
                var sentimentDirection = sentimentScore > 0 ? 1 : sentimentScore < 0 ? -1 : 0;

                if (signalDirection != 0 && sentimentDirection != 0)
                {
                    // Reward alignment, penalize divergence
                    var alignment = signalDirection == sentimentDirection ? 1 : -1;
                    var sentimentStrength = Math.Abs(sentimentScore) / 100m; // Normalize to 0-1
                    
                    var sentimentAdjustment = alignment * sentimentStrength * 0.3m;
                    sentimentWeight += sentimentAdjustment;
                }

                // Additional adjustment based on sentiment confidence
                var sentimentConfidence = Math.Abs(sentimentScore) / 100m;
                sentimentWeight *= (0.8m + sentimentConfidence * 0.4m);

                return Math.Max(0.4m, Math.Min(1.6m, sentimentWeight));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating sentiment weight for signal {SignalId}", signal.SignalId);
                return 1.0m;
            }
        }

        /// <summary>
        /// Calculate confidence-based weight adjustment
        /// </summary>
        private decimal CalculateConfidenceWeight(EnhancedTradingSignal signal, SignalWeightingContext context)
        {
            try
            {
                var confidenceWeight = 1.0m;
                var confidenceScore = signal.ConfidenceScore;

                // Linear adjustment based on confidence score
                confidenceWeight = 0.3m + (confidenceScore / 100m) * 0.7m;

                // Bonus for high confidence
                if (confidenceScore > 80)
                {
                    confidenceWeight += 0.2m;
                }

                // Penalty for low confidence
                if (confidenceScore < 40)
                {
                    confidenceWeight *= 0.7m;
                }

                return Math.Max(0.2m, Math.Min(1.3m, confidenceWeight));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating confidence weight for signal {SignalId}", signal.SignalId);
                return 1.0m;
            }
        }

        /// <summary>
        /// Calculate diversification-based weight adjustment
        /// </summary>
        private async Task<decimal> CalculateDiversificationWeightAsync(EnhancedTradingSignal signal, SignalWeightingContext context)
        {
            try
            {
                var diversificationWeight = 1.0m;

                // Get current open positions
                var openPositions = await _context.ApiTradeLog
                    .Where(t => t.Outcome == "OPEN")
                    .ToListAsync();

                if (openPositions.Any())
                {
                    // Check signal concentration
                    var sameSignalCount = openPositions.Count(p => p.SignalId == signal.SignalId);
                    var totalPositions = openPositions.Count;
                    var signalConcentration = (decimal)sameSignalCount / totalPositions;

                    // Penalize over-concentration
                    if (signalConcentration > 0.3m)
                    {
                        diversificationWeight *= Math.Max(0.5m, 1 - (signalConcentration - 0.3m) * 2);
                    }

                    // Check option type and strike diversification
                    var sameTypeCount = openPositions.Count(p => p.OptionType == signal.OriginalSignal.Type);
                    var typeConcentration = (decimal)sameTypeCount / totalPositions;

                    if (typeConcentration > 0.7m)
                    {
                        diversificationWeight *= Math.Max(0.7m, 1 - (typeConcentration - 0.7m) * 1.5m);
                    }
                }

                return Math.Max(0.3m, Math.Min(1.2m, diversificationWeight));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating diversification weight for signal {SignalId}", signal.SignalId);
                return 1.0m;
            }
        }

        /// <summary>
        /// Calculate composite weight from individual components
        /// </summary>
        private decimal CalculateCompositeWeight(Dictionary<string, decimal> components)
        {
            var weights = new Dictionary<string, decimal>
            {
                ["Performance"] = 0.25m,
                ["MarketCondition"] = 0.20m,
                ["TimeBased"] = 0.15m,
                ["Volatility"] = 0.15m,
                ["Sentiment"] = 0.10m,
                ["Confidence"] = 0.10m,
                ["Diversification"] = 0.05m
            };

            var compositeWeight = 0m;
            foreach (var component in components)
            {
                if (weights.TryGetValue(component.Key, out var weight))
                {
                    compositeWeight += component.Value * weight;
                }
            }

            return compositeWeight;
        }

        /// <summary>
        /// Apply constraints and bounds to the calculated weight
        /// </summary>
        private decimal ApplyWeightConstraints(decimal weight, EnhancedTradingSignal signal, SignalWeightingContext context)
        {
            // Global bounds
            weight = Math.Max(0.1m, Math.Min(2.0m, weight));

            // Risk-based constraints
            if (signal.RiskAssessment.OverallRiskScore < 30)
            {
                weight = Math.Min(weight, 0.5m); // Limit high-risk signals
            }

            // Validation-based constraints
            if (signal.ValidationScores.QualityScore < 40)
            {
                weight = Math.Min(weight, 0.7m); // Limit low-quality signals
            }

            // Time-based constraints
            if (IsAfterHours())
            {
                weight *= 0.6m; // Reduce weight for after-hours signals
            }

            return weight;
        }

        // Helper methods

        private async Task<SignalWeightingContext> GetOrCreateSignalContextAsync(string signalId)
        {
            if (!_signalContexts.TryGetValue(signalId, out var context))
            {
                context = new SignalWeightingContext
                {
                    SignalId = signalId,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };
                _signalContexts[signalId] = context;
            }

            return context;
        }

        private async Task UpdateSignalContextAsync(string signalId, SignalWeightingContext context, AdaptiveWeightResult result)
        {
            context.LastUpdated = DateTime.UtcNow;
            context.LastWeight = result.AdaptiveWeight;
            context.WeightHistory.Add(new WeightHistoryEntry
            {
                Timestamp = DateTime.UtcNow,
                Weight = result.AdaptiveWeight,
                Reason = result.WeightingReason
            });

            // Keep only last 100 entries
            if (context.WeightHistory.Count > 100)
            {
                context.WeightHistory.RemoveAt(0);
            }
        }

        private double CalculateSharpeRatio(List<double> returns)
        {
            if (!returns.Any()) return 0;

            var avgReturn = returns.Average();
            var stdDev = Math.Sqrt(returns.Select(r => Math.Pow(r - avgReturn, 2)).Average());
            
            return stdDev == 0 ? 0 : avgReturn / stdDev;
        }

        private bool IsSimilarMarketCondition(ApiTradeLog trade, string currentTrend, decimal currentVIX)
        {
            // Simplified similarity check - in production, use more sophisticated matching
            // This would require storing market conditions with each trade
            return true;
        }

        private decimal GetMarketRegimeMultiplier(string marketTrend)
        {
            return marketTrend switch
            {
                "Strongly Bullish" => 1.2m,
                "Bullish" => 1.1m,
                "Neutral" => 1.0m,
                "Bearish" => 0.9m,
                "Strongly Bearish" => 0.8m,
                _ => 1.0m
            };
        }

        private decimal GetHourMultiplier(int hour)
        {
            return hour switch
            {
                >= 9 and <= 11 => 1.2m,   // Opening hours - high activity
                >= 12 and <= 14 => 1.1m,  // Mid-day - good activity
                >= 15 and <= 16 => 1.0m,  // Closing hours - normal activity
                _ => 0.7m                  // Outside market hours
            };
        }

        private decimal GetDayOfWeekMultiplier(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => 0.9m,     // Monday blues
                DayOfWeek.Tuesday => 1.1m,    // Good trading day
                DayOfWeek.Wednesday => 1.2m,  // Best trading day
                DayOfWeek.Thursday => 1.1m,   // Good trading day
                DayOfWeek.Friday => 0.8m,     // Profit taking
                _ => 0.5m                     // Weekend
            };
        }

        private decimal GetTimeToExpiry(DateTime signalTime)
        {
            var today = signalTime.Date;
            var daysUntilThursday = ((int)DayOfWeek.Thursday - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilThursday == 0 && signalTime.TimeOfDay > TimeSpan.FromHours(15.5))
            {
                daysUntilThursday = 7;
            }
            return daysUntilThursday;
        }

        private decimal GetExpiryMultiplier(decimal daysToExpiry)
        {
            return daysToExpiry switch
            {
                <= 0.5m => 0.6m,  // Very close to expiry
                <= 1 => 0.8m,     // Same day
                <= 2 => 1.0m,     // 1-2 days
                <= 3 => 1.1m,     // 2-3 days
                _ => 0.9m          // Too far
            };
        }

        private decimal GetMarketSessionMultiplier(DateTime time)
        {
            var hour = time.Hour;
            var minute = time.Minute;
            var totalMinutes = hour * 60 + minute;
            
            // Market hours: 9:15 AM to 3:30 PM
            var marketOpen = 9 * 60 + 15;   // 9:15 AM
            var marketClose = 15 * 60 + 30; // 3:30 PM
            
            if (totalMinutes >= marketOpen && totalMinutes <= marketClose)
            {
                return 1.0m; // Full weight during market hours
            }
            else
            {
                return 0.5m; // Reduced weight outside market hours
            }
        }

        private decimal GetVIXMultiplier(decimal vixLevel)
        {
            return vixLevel switch
            {
                < 12 => 0.8m,      // Complacency - reduce weight
                < 20 => 1.0m,      // Normal volatility
                < 30 => 1.1m,      // Elevated volatility - good for options
                _ => 0.9m           // High volatility - risk management
            };
        }

        private async Task<List<VolatilityPerformance>> GetHistoricalVolatilityPerformanceAsync(string signalId)
        {
            // Placeholder - in production, analyze historical performance across volatility regimes
            return new List<VolatilityPerformance>();
        }

        private string GetVolatilityRegime(decimal vixLevel)
        {
            return vixLevel switch
            {
                < 12 => "Low",
                < 20 => "Normal",
                < 30 => "High",
                _ => "Extreme"
            };
        }

        private int DetermineSignalDirection(EnhancedTradingSignal signal)
        {
            // Simplified direction determination based on option type and action
            var optionType = signal.OriginalSignal.Type?.ToUpper();
            var action = signal.OriginalSignal.Action?.ToUpper();
            
            if (action == "ENTRY")
            {
                return optionType == "PE" ? 1 : -1; // PE entry = bullish, CE entry = bearish
            }
            
            return 0; // Neutral
        }

        private bool IsAfterHours()
        {
            var now = DateTime.Now;
            var marketClose = new TimeSpan(15, 30, 0);
            var marketOpen = new TimeSpan(9, 15, 0);
            
            return now.TimeOfDay < marketOpen || now.TimeOfDay > marketClose;
        }

        private string GenerateWeightingReason(AdaptiveWeightResult result)
        {
            var reasons = new List<string>();
            
            foreach (var component in result.WeightComponents)
            {
                if (component.Value > 1.1m)
                {
                    reasons.Add($"Strong {component.Key} factor (+{(component.Value - 1):P1})");
                }
                else if (component.Value < 0.9m)
                {
                    reasons.Add($"Weak {component.Key} factor ({(component.Value - 1):P1})");
                }
            }

            if (result.AdaptiveWeight > 1.2m)
            {
                reasons.Add("Overall strong conviction");
            }
            else if (result.AdaptiveWeight < 0.8m)
            {
                reasons.Add("Overall reduced conviction");
            }

            return reasons.Any() ? string.Join("; ", reasons) : "Neutral weighting across all factors";
        }
    }

    /// <summary>
    /// Result of adaptive weight calculation
    /// </summary>
    public class AdaptiveWeightResult
    {
        public string SignalId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public decimal BaseWeight { get; set; }
        public decimal AdaptiveWeight { get; set; }
        public Dictionary<string, decimal> WeightComponents { get; set; } = new();
        public string WeightingReason { get; set; } = string.Empty;
        public decimal ConfidenceLevel { get; set; }
    }

    /// <summary>
    /// Context for signal weighting decisions
    /// </summary>
    public class SignalWeightingContext
    {
        public string SignalId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public decimal LastWeight { get; set; }
        public List<WeightHistoryEntry> WeightHistory { get; set; } = new();
        public Dictionary<string, decimal> PerformanceMetrics { get; set; } = new();
    }

    /// <summary>
    /// Weight history entry
    /// </summary>
    public class WeightHistoryEntry
    {
        public DateTime Timestamp { get; set; }
        public decimal Weight { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Volatility performance data
    /// </summary>
    public class VolatilityPerformance
    {
        public string VolatilityRegime { get; set; } = string.Empty;
        public decimal WinRate { get; set; }
        public decimal AvgPnL { get; set; }
        public int TradeCount { get; set; }
    }
}