using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.AI;
using KiteConnectApi.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
// Note: ML.NET integration can be added later with proper model training
// using ML.NET;
// using ML.NET.Data;

namespace KiteConnectApi.Services.AI
{
    public class MLSignalValidationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MLSignalValidationService> _logger;
        private readonly IConfiguration _configuration;
        // Simplified implementation without ML.NET for now
        // private readonly MLContext _mlContext;
        // private readonly ITransformer? _signalValidationModel;
        // private readonly ITransformer? _outcomePredictor;

        public MLSignalValidationService(
            ApplicationDbContext context,
            ILogger<MLSignalValidationService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            
            // Simplified initialization - ML models can be added later
            // _mlContext = new MLContext(seed: 0);
            // _signalValidationModel = LoadModel("SignalValidation");
            // _outcomePredictor = LoadModel("OutcomePredictor");
        }

        /// <summary>
        /// Validates a TradingView signal using ML models and returns enhanced signal with confidence score
        /// </summary>
        public async Task<EnhancedTradingSignal> ValidateSignalAsync(TradingViewAlert originalSignal)
        {
            try
            {
                _logger.LogInformation("Starting ML validation for signal {SignalId}", originalSignal.Signal);

                // Create enhanced signal with original data
                var enhancedSignal = new EnhancedTradingSignal
                {
                    OriginalSignal = originalSignal,
                    Timestamp = DateTime.UtcNow,
                    SignalId = originalSignal.Signal ?? "Unknown",
                    ValidationScores = new SignalValidationScores()
                };

                // Step 1: Extract features for ML validation
                var features = await ExtractSignalFeaturesAsync(originalSignal);

                // Step 2: Run ML validation models
                var validationResult = await RunValidationModelsAsync(features);
                enhancedSignal.ValidationScores = validationResult;

                // Step 3: Calculate composite confidence score
                enhancedSignal.ConfidenceScore = CalculateCompositeConfidence(validationResult);

                // Step 4: Determine recommendation
                enhancedSignal.Recommendation = DetermineRecommendation(enhancedSignal.ConfidenceScore, validationResult);

                // Step 5: Add contextual information
                enhancedSignal.MarketContext = await GetMarketContextAsync();
                enhancedSignal.HistoricalPerformance = await GetHistoricalPerformanceAsync(originalSignal);

                _logger.LogInformation("ML validation completed for signal {SignalId}. Confidence: {Confidence}%, Recommendation: {Recommendation}",
                    originalSignal.Signal, enhancedSignal.ConfidenceScore, enhancedSignal.Recommendation);

                return enhancedSignal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating signal {SignalId}", originalSignal.Signal);
                
                // Return original signal with low confidence on error
                return new EnhancedTradingSignal
                {
                    OriginalSignal = originalSignal,
                    ConfidenceScore = 20, // Low confidence due to validation error
                    Recommendation = SignalRecommendation.Caution,
                    ValidationScores = new SignalValidationScores(),
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Extract comprehensive features from the trading signal for ML analysis
        /// </summary>
        private async Task<SignalFeatures> ExtractSignalFeaturesAsync(TradingViewAlert signal)
        {
            var features = new SignalFeatures
            {
                // Basic signal properties
                SignalId = signal.Signal ?? "Unknown",
                Strike = signal.Strike,
                OptionType = signal.Type ?? "CE",
                Action = signal.Action ?? "Entry",
                Timestamp = DateTime.UtcNow
            };

            // Get current market data
            var marketData = await GetCurrentMarketDataAsync();
            if (marketData != null)
            {
                features.UnderlyingPrice = marketData.UnderlyingPrice;
                features.UnderlyingChange = marketData.UnderlyingChange;
                features.VIX = marketData.VIX;
                features.MarketTrend = marketData.MarketTrend;
                features.Volume = marketData.Volume;
            }

            // Calculate technical indicators
            var technicalIndicators = await CalculateTechnicalIndicatorsAsync();
            features.RSI = technicalIndicators.RSI;
            features.MACD = technicalIndicators.MACD;
            features.BollingerBandPosition = technicalIndicators.BollingerBandPosition;
            features.SMA20 = technicalIndicators.SMA20;
            features.SMA50 = technicalIndicators.SMA50;

            // Time-based features
            features.HourOfDay = DateTime.Now.Hour;
            features.DayOfWeek = (int)DateTime.Now.DayOfWeek;
            features.TimeToExpiry = CalculateTimeToExpiry(signal);

            // Historical performance features
            var historicalStats = await GetSignalHistoricalStatsAsync(signal.Signal);
            features.HistoricalWinRate = historicalStats.WinRate;
            features.HistoricalAvgReturn = historicalStats.AvgReturn;
            features.RecentPerformance = historicalStats.RecentPerformance;

            // Market regime features
            features.MarketRegime = await DetermineMarketRegimeAsync();
            features.VolatilityRegime = await DetermineVolatilityRegimeAsync();

            return features;
        }

        /// <summary>
        /// Run ensemble of ML models to validate the signal
        /// </summary>
        private async Task<SignalValidationScores> RunValidationModelsAsync(SignalFeatures features)
        {
            var scores = new SignalValidationScores();

            try
            {
                // Model 1: Signal Quality Predictor (Simplified implementation)
                scores.QualityScore = CalculateQualityScoreSimplified(features);
                scores.QualityConfidence = 75; // Default confidence

                // Model 2: Outcome Predictor (Simplified implementation)
                scores.OutcomeScore = CalculateOutcomeScoreSimplified(features);
                scores.OutcomeConfidence = 70; // Default confidence

                // Model 3: Market Condition Matcher
                scores.MarketConditionScore = await CalculateMarketConditionScore(features);

                // Model 4: Timing Score
                scores.TimingScore = CalculateTimingScore(features);

                // Model 5: Risk Assessment
                scores.RiskScore = CalculateRiskScore(features);

                await Task.CompletedTask; // Placeholder for async operations
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running ML validation models");
                
                // Return default scores on error
                scores.QualityScore = 50;
                scores.OutcomeScore = 50;
                scores.MarketConditionScore = 50;
                scores.TimingScore = 50;
                scores.RiskScore = 50;
            }

            return scores;
        }

        /// <summary>
        /// Calculate composite confidence score from individual model scores
        /// </summary>
        private decimal CalculateCompositeConfidence(SignalValidationScores scores)
        {
            // Weighted average of different scores
            var weights = new Dictionary<string, decimal>
            {
                ["Quality"] = 0.25m,
                ["Outcome"] = 0.25m,
                ["MarketCondition"] = 0.20m,
                ["Timing"] = 0.15m,
                ["Risk"] = 0.15m
            };

            var compositeScore = 
                (scores.QualityScore * weights["Quality"]) +
                (scores.OutcomeScore * weights["Outcome"]) +
                (scores.MarketConditionScore * weights["MarketCondition"]) +
                (scores.TimingScore * weights["Timing"]) +
                (scores.RiskScore * weights["Risk"]);

            // Ensure score is between 0-100
            return Math.Max(0, Math.Min(100, compositeScore));
        }

        /// <summary>
        /// Determine trading recommendation based on confidence score and validation results
        /// </summary>
        private SignalRecommendation DetermineRecommendation(decimal confidenceScore, SignalValidationScores scores)
        {
            // High confidence signals
            if (confidenceScore >= 80 && scores.RiskScore >= 70)
                return SignalRecommendation.StrongBuy;
            
            if (confidenceScore >= 70 && scores.RiskScore >= 60)
                return SignalRecommendation.Buy;
            
            // Medium confidence signals
            if (confidenceScore >= 60 && scores.RiskScore >= 50)
                return SignalRecommendation.WeakBuy;
            
            if (confidenceScore >= 40 && confidenceScore < 60)
                return SignalRecommendation.Hold;
            
            // Low confidence signals
            if (confidenceScore < 40 || scores.RiskScore < 40)
                return SignalRecommendation.Caution;
            
            return SignalRecommendation.Hold;
        }

        /// <summary>
        /// Train ML models using historical signal data
        /// </summary>
        public async Task<ModelTrainingResult> TrainModelsAsync(DateTime fromDate, DateTime toDate)
        {
            _logger.LogInformation("Starting ML model training from {FromDate} to {ToDate}", fromDate, toDate);

            var result = new ModelTrainingResult
            {
                StartTime = DateTime.UtcNow,
                FromDate = fromDate,
                ToDate = toDate
            };

            try
            {
                // Get training data
                var trainingData = await PrepareTrainingDataAsync(fromDate, toDate);
                
                if (trainingData.Count < 100) // Minimum training samples
                {
                    throw new InvalidOperationException($"Insufficient training data: {trainingData.Count} samples. Need at least 100.");
                }

                // Train Signal Quality Model
                var qualityModel = await TrainSignalQualityModelAsync(trainingData);
                result.SignalQualityModelAccuracy = qualityModel.Accuracy;

                // Train Outcome Predictor
                var outcomeModel = await TrainOutcomePredictorAsync(trainingData);
                result.OutcomePredictorAccuracy = outcomeModel.Accuracy;

                // Save models
                await SaveModelsAsync(qualityModel.Model, outcomeModel.Model);

                result.Success = true;
                result.TotalSamples = trainingData.Count;
                result.EndTime = DateTime.UtcNow;

                _logger.LogInformation("ML model training completed successfully. Quality Model Accuracy: {QualityAccuracy}%, Outcome Model Accuracy: {OutcomeAccuracy}%",
                    result.SignalQualityModelAccuracy, result.OutcomePredictorAccuracy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training ML models");
                result.Success = false;
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
            }

            return result;
        }

        /// <summary>
        /// Prepare training data from historical signals and outcomes
        /// </summary>
        private async Task<List<SignalTrainingData>> PrepareTrainingDataAsync(DateTime fromDate, DateTime toDate)
        {
            var trainingData = new List<SignalTrainingData>();

            // Get historical TradingView signals
            var historicalSignals = await _context.ManualTradingViewAlerts
                .Where(a => a.ReceivedTime >= fromDate && a.ReceivedTime <= toDate)
                .OrderBy(a => a.ReceivedTime)
                .ToListAsync();

            // Get corresponding outcomes from ApiTradeLog
            var outcomes = await _context.ApiTradeLog
                .Where(t => t.EntryTime >= fromDate && t.EntryTime <= toDate)
                .ToListAsync();

            foreach (var signal in historicalSignals)
            {
                try
                {
                    // Find matching outcome
                    var outcome = outcomes.FirstOrDefault(o => 
                        o.SignalId == signal.Signal &&
                        Math.Abs((o.EntryTime - signal.ReceivedTime).TotalMinutes) < 30);

                    if (outcome != null)
                    {
                        // Create training sample
                        var sample = new SignalTrainingData
                        {
                            SignalId = signal.Signal ?? "Unknown",
                            Strike = signal.Strike,
                            OptionType = signal.Type ?? "CE",
                            Action = signal.Action ?? "Entry",
                            Timestamp = signal.ReceivedTime,
                            
                            // Features (would be populated with historical market data)
                            HourOfDay = signal.ReceivedTime.Hour,
                            DayOfWeek = (int)signal.ReceivedTime.DayOfWeek,
                            
                            // Labels
                            ActualOutcome = outcome.Outcome == "WIN" ? 1 : 0,
                            ActualPnL = outcome.PnL ?? 0,
                            QualityLabel = CalculateQualityLabel(outcome)
                        };

                        trainingData.Add(sample);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing training sample for signal {SignalId}", signal.Signal);
                }
            }

            return trainingData;
        }

        // Helper methods (simplified implementations)
        private async Task<CurrentMarketData?> GetCurrentMarketDataAsync()
        {
            // Implementation would fetch real market data
            return new CurrentMarketData
            {
                UnderlyingPrice = 22500,
                UnderlyingChange = 0.5m,
                VIX = 15.5m,
                MarketTrend = "Bullish",
                Volume = 1000000
            };
        }

        private async Task<TechnicalIndicators> CalculateTechnicalIndicatorsAsync()
        {
            // Implementation would calculate actual technical indicators
            return new TechnicalIndicators
            {
                RSI = 55,
                MACD = 0.25m,
                BollingerBandPosition = 0.6m,
                SMA20 = 22480,
                SMA50 = 22450
            };
        }

        private async Task<HistoricalStats> GetSignalHistoricalStatsAsync(string? signalId)
        {
            var stats = await _context.ApiTradeLog
                .Where(t => t.SignalId == signalId && t.Outcome != "OPEN")
                .GroupBy(t => t.SignalId)
                .Select(g => new HistoricalStats
                {
                    WinRate = g.Count(t => t.Outcome == "WIN") * 100.0m / g.Count(),
                    AvgReturn = g.Average(t => t.PnL ?? 0),
                    RecentPerformance = g.OrderByDescending(t => t.EntryTime)
                                      .Take(10)
                                      .Average(t => t.PnL ?? 0)
                })
                .FirstOrDefaultAsync();

            return stats ?? new HistoricalStats();
        }

        // Simplified quality score calculation (replaces ML model)
        private decimal CalculateQualityScoreSimplified(SignalFeatures features)
        {
            var score = 50m; // Base score
            
            // Historical performance factor
            if (features.HistoricalWinRate > 60) score += 20;
            else if (features.HistoricalWinRate > 50) score += 10;
            else if (features.HistoricalWinRate < 40) score -= 15;
            
            // Market condition factor
            if (features.VIX < 20 && features.RSI > 30 && features.RSI < 70) score += 15;
            
            // Time factor
            if (features.HourOfDay >= 10 && features.HourOfDay <= 14) score += 10;
            
            return Math.Max(0, Math.Min(100, score));
        }

        // Simplified outcome score calculation (replaces ML model)
        private decimal CalculateOutcomeScoreSimplified(SignalFeatures features)
        {
            var score = 50m; // Base score
            
            // Recent performance factor
            if (features.RecentPerformance > 0) score += 15;
            else if (features.RecentPerformance < 0) score -= 15;
            
            // Market trend alignment
            if (features.MarketTrend == "Bullish" && features.OptionType == "PE") score += 10;
            if (features.MarketTrend == "Bearish" && features.OptionType == "CE") score += 10;
            
            // Volatility factor
            if (features.VIX >= 15 && features.VIX <= 25) score += 10; // Sweet spot for options
            
            return Math.Max(0, Math.Min(100, score));
        }

        private decimal CalculateTimeToExpiry(TradingViewAlert signal)
        {
            // Calculate time to next Thursday (options expiry)
            var today = DateTime.Now.Date;
            var daysUntilThursday = ((int)DayOfWeek.Thursday - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilThursday == 0 && DateTime.Now.TimeOfDay > TimeSpan.FromHours(15.5))
            {
                daysUntilThursday = 7;
            }
            return daysUntilThursday;
        }

        private async Task<string> DetermineMarketRegimeAsync()
        {
            // Implementation would analyze market conditions
            return "Trending";
        }

        private async Task<string> DetermineVolatilityRegimeAsync()
        {
            // Implementation would analyze volatility
            return "Normal";
        }

        private async Task<decimal> CalculateMarketConditionScore(SignalFeatures features)
        {
            // Implementation would score market conditions
            return 75;
        }

        private decimal CalculateTimingScore(SignalFeatures features)
        {
            // Score based on time of day, day of week, etc.
            var score = 50m;
            
            // Prefer trading during market hours
            if (features.HourOfDay >= 9 && features.HourOfDay <= 15)
                score += 20;
            
            // Prefer mid-week trading
            if (features.DayOfWeek >= 2 && features.DayOfWeek <= 4)
                score += 15;
            
            return Math.Min(100, score);
        }

        private decimal CalculateRiskScore(SignalFeatures features)
        {
            // Risk assessment based on volatility, time to expiry, etc.
            var score = 50m;
            
            // Lower risk for longer time to expiry
            if (features.TimeToExpiry > 2)
                score += 20;
            
            // Lower risk in normal volatility regime
            if (features.VIX < 20)
                score += 15;
            
            return Math.Min(100, score);
        }

        private decimal CalculateQualityLabel(ApiTradeLog outcome)
        {
            // Calculate quality score based on outcome
            if (outcome.Outcome == "WIN")
                return outcome.PnL > 0 ? Math.Min(100, 50 + (outcome.PnL.Value / 10)) : 50;
            else
                return Math.Max(0, 50 + (outcome.PnL ?? 0) / 10);
        }

        private async Task<(object Model, double Accuracy)> TrainSignalQualityModelAsync(List<SignalTrainingData> trainingData)
        {
            // Simplified training - analyze historical data to create rule-based model
            var totalSamples = trainingData.Count;
            var correctPredictions = 0;
            
            foreach (var sample in trainingData)
            {
                var predictedQuality = CalculateQualityScoreSimplified(sample);
                if (Math.Abs(predictedQuality - sample.QualityLabel) < 20) // Within 20 points
                    correctPredictions++;
            }
            
            var accuracy = totalSamples > 0 ? (double)correctPredictions / totalSamples : 0.75;
            
            return (new { TrainedAt = DateTime.UtcNow, Samples = totalSamples }, accuracy);
        }

        private async Task<(object Model, double Accuracy)> TrainOutcomePredictorAsync(List<SignalTrainingData> trainingData)
        {
            // Simplified training - analyze historical outcomes
            var totalSamples = trainingData.Count;
            var correctPredictions = 0;
            
            foreach (var sample in trainingData)
            {
                var predictedOutcome = CalculateOutcomeScoreSimplified(sample);
                var actualSuccess = sample.ActualOutcome == 1;
                var predictedSuccess = predictedOutcome > 50;
                
                if (actualSuccess == predictedSuccess)
                    correctPredictions++;
            }
            
            var accuracy = totalSamples > 0 ? (double)correctPredictions / totalSamples : 0.68;
            
            return (new { TrainedAt = DateTime.UtcNow, Samples = totalSamples }, accuracy);
        }

        private async Task SaveModelsAsync(object qualityModel, object outcomeModel)
        {
            // Simplified model saving - just log the training completion
            _logger.LogInformation("Model training completed at {Time}. Quality model: {QualityModel}, Outcome model: {OutcomeModel}", 
                DateTime.UtcNow, qualityModel, outcomeModel);
            
            await Task.CompletedTask;
        }

        private async Task<MarketContext> GetMarketContextAsync()
        {
            return new MarketContext
            {
                MarketTrend = "Bullish",
                VolatilityLevel = "Normal",
                LiquidityLevel = "High",
                MarketHours = IsMarketOpen() ? "Open" : "Closed"
            };
        }

        private async Task<HistoricalPerformanceContext> GetHistoricalPerformanceAsync(TradingViewAlert signal)
        {
            var recentTrades = await _context.ApiTradeLog
                .Where(t => t.SignalId == signal.Signal)
                .OrderByDescending(t => t.EntryTime)
                .Take(20)
                .ToListAsync();

            return new HistoricalPerformanceContext
            {
                RecentWinRate = recentTrades.Any() ? recentTrades.Count(t => t.Outcome == "WIN") * 100.0m / recentTrades.Count : 0,
                RecentAvgPnL = recentTrades.Any() ? recentTrades.Average(t => t.PnL ?? 0) : 0,
                TotalTrades = recentTrades.Count,
                LastTradeDate = recentTrades.FirstOrDefault()?.EntryTime
            };
        }

        private bool IsMarketOpen()
        {
            var now = DateTime.Now;
            return now.DayOfWeek >= DayOfWeek.Monday && 
                   now.DayOfWeek <= DayOfWeek.Friday &&
                   now.TimeOfDay >= TimeSpan.FromHours(9.25) && 
                   now.TimeOfDay <= TimeSpan.FromHours(15.5);
        }
    }
}