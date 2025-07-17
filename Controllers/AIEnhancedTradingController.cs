using KiteConnectApi.Models.AI;
using KiteConnectApi.Models.Dto;
using KiteConnectApi.Services.AI;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Controllers
{
    /// <summary>
    /// AI-Enhanced Trading Controller that provides intelligent signal validation and processing
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AIEnhancedTradingController : ControllerBase
    {
        private readonly MLSignalValidationService _validationService;
        private readonly PatternRecognitionService _patternService;
        private readonly MarketSentimentAnalyzer _sentimentAnalyzer;
        private readonly AdaptiveSignalWeightingService _weightingService;
        private readonly ILogger<AIEnhancedTradingController> _logger;

        public AIEnhancedTradingController(
            MLSignalValidationService validationService,
            PatternRecognitionService patternService,
            MarketSentimentAnalyzer sentimentAnalyzer,
            AdaptiveSignalWeightingService weightingService,
            ILogger<AIEnhancedTradingController> logger)
        {
            _validationService = validationService;
            _patternService = patternService;
            _sentimentAnalyzer = sentimentAnalyzer;
            _weightingService = weightingService;
            _logger = logger;
        }

        /// <summary>
        /// Process a TradingView signal through AI enhancement pipeline
        /// </summary>
        [HttpPost("process-signal")]
        public async Task<ActionResult<AIEnhancedSignalResponse>> ProcessSignalAsync([FromBody] TradingViewAlert signal)
        {
            try
            {
                _logger.LogInformation("Processing AI-enhanced signal: {SignalId}", signal.Signal);

                var response = new AIEnhancedSignalResponse
                {
                    ProcessingStartTime = DateTime.UtcNow,
                    OriginalSignal = signal
                };

                // Step 1: ML Signal Validation
                var enhancedSignal = await _validationService.ValidateSignalAsync(signal);
                response.EnhancedSignal = enhancedSignal;

                // Step 2: Pattern Recognition
                var detectedPatterns = await _patternService.DetectPatternsAsync(signal);
                enhancedSignal.DetectedPatterns = detectedPatterns;

                // Step 3: Market Sentiment Analysis
                var sentimentResult = await _sentimentAnalyzer.AnalyzeSentimentAsync("NIFTY");
                enhancedSignal.SentimentScore = sentimentResult.CompositeSentimentScore;
                response.SentimentAnalysis = sentimentResult;

                // Step 4: Adaptive Weight Calculation
                var weightResult = await _weightingService.CalculateSignalWeightAsync(enhancedSignal);
                enhancedSignal.AdaptiveWeight = weightResult.AdaptiveWeight;
                response.WeightingResult = weightResult;

                // Step 5: Final Trading Decision
                var tradingDecision = MakeTradingDecision(enhancedSignal, sentimentResult, weightResult);
                response.TradingDecision = tradingDecision;

                // Step 6: Generate Comprehensive Analysis
                response.Analysis = GenerateComprehensiveAnalysis(enhancedSignal, sentimentResult, weightResult, tradingDecision);

                response.ProcessingEndTime = DateTime.UtcNow;
                response.ProcessingTime = response.ProcessingEndTime - response.ProcessingStartTime;

                _logger.LogInformation("AI-enhanced signal processing completed: {SignalId}, Decision: {Decision}, Confidence: {Confidence}%", 
                    signal.Signal, tradingDecision.Decision, tradingDecision.Confidence);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI-enhanced signal: {SignalId}", signal.Signal);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get AI model performance metrics
        /// </summary>
        [HttpGet("model-performance")]
        public async Task<ActionResult<AIModelPerformanceResponse>> GetModelPerformanceAsync()
        {
            try
            {
                var response = new AIModelPerformanceResponse
                {
                    ReportGeneratedAt = DateTime.UtcNow
                };

                // Get performance metrics from different AI components
                // This would be implemented with actual metrics tracking
                response.SignalValidationAccuracy = 75.5m;
                response.PatternRecognitionAccuracy = 68.2m;
                response.SentimentAnalysisAccuracy = 71.8m;
                response.AdaptiveWeightingEffectiveness = 82.3m;

                response.OverallSystemAccuracy = (response.SignalValidationAccuracy + 
                                                response.PatternRecognitionAccuracy + 
                                                response.SentimentAnalysisAccuracy + 
                                                response.AdaptiveWeightingEffectiveness) / 4;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting model performance metrics");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Train AI models with historical data
        /// </summary>
        [HttpPost("train-models")]
        public async Task<ActionResult<ModelTrainingResponse>> TrainModelsAsync([FromBody] ModelTrainingRequest request)
        {
            try
            {
                _logger.LogInformation("Starting AI model training from {FromDate} to {ToDate}", request.FromDate, request.ToDate);

                var response = new ModelTrainingResponse
                {
                    TrainingStartTime = DateTime.UtcNow,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate
                };

                // Train ML models
                var trainingResult = await _validationService.TrainModelsAsync(request.FromDate, request.ToDate);
                response.TrainingResult = trainingResult;

                response.TrainingEndTime = DateTime.UtcNow;
                response.TrainingDuration = response.TrainingEndTime - response.TrainingStartTime;

                _logger.LogInformation("AI model training completed. Success: {Success}, Duration: {Duration}", 
                    trainingResult.Success, response.TrainingDuration);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error training AI models");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get current market sentiment analysis
        /// </summary>
        [HttpGet("market-sentiment")]
        public async Task<ActionResult<MarketSentimentResult>> GetMarketSentimentAsync([FromQuery] string symbol = "NIFTY")
        {
            try
            {
                var sentiment = await _sentimentAnalyzer.AnalyzeSentimentAsync(symbol);
                return Ok(sentiment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting market sentiment for {Symbol}", symbol);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get pattern recognition analysis for a symbol
        /// </summary>
        [HttpPost("pattern-analysis")]
        public async Task<ActionResult<PatternAnalysisResponse>> GetPatternAnalysisAsync([FromBody] PatternAnalysisRequest request)
        {
            try
            {
                var patterns = await _patternService.DetectPatternsAsync(request.Signal);
                
                var response = new PatternAnalysisResponse
                {
                    Symbol = request.Symbol,
                    AnalysisTimestamp = DateTime.UtcNow,
                    DetectedPatterns = patterns,
                    PatternCount = patterns.Count,
                    HighConfidencePatterns = patterns.Where(p => p.Confidence >= 75).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing patterns for {Symbol}", request.Symbol);
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get comprehensive AI analysis dashboard
        /// </summary>
        [HttpGet("ai-dashboard")]
        public async Task<ActionResult<AIDashboardResponse>> GetAIDashboardAsync()
        {
            try
            {
                var response = new AIDashboardResponse
                {
                    GeneratedAt = DateTime.UtcNow
                };

                // Get current market sentiment
                response.MarketSentiment = await _sentimentAnalyzer.AnalyzeSentimentAsync("NIFTY");

                // Get model performance (placeholder - would be real metrics)
                response.ModelPerformance = new AIModelPerformanceResponse
                {
                    ReportGeneratedAt = DateTime.UtcNow,
                    SignalValidationAccuracy = 75.5m,
                    PatternRecognitionAccuracy = 68.2m,
                    SentimentAnalysisAccuracy = 71.8m,
                    AdaptiveWeightingEffectiveness = 82.3m,
                    OverallSystemAccuracy = 74.5m
                };

                // Get recent AI-enhanced signals (placeholder)
                response.RecentEnhancedSignals = new List<AISignalSummary>();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI dashboard");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Make final trading decision based on AI analysis
        /// </summary>
        private TradingDecision MakeTradingDecision(
            EnhancedTradingSignal signal, 
            MarketSentimentResult sentiment, 
            AdaptiveWeightResult weighting)
        {
            var decision = new TradingDecision
            {
                Timestamp = DateTime.UtcNow,
                SignalId = signal.SignalId
            };

            // Calculate composite decision score
            var decisionScore = 0m;
            var factors = new List<DecisionFactor>();

            // AI confidence factor (40% weight)
            var confidenceFactor = (signal.ConfidenceScore / 100m) * 0.4m;
            decisionScore += confidenceFactor;
            factors.Add(new DecisionFactor { Name = "AI Confidence", Score = confidenceFactor, Weight = 0.4m });

            // Sentiment alignment factor (25% weight)
            var sentimentFactor = CalculateSentimentAlignmentFactor(signal, sentiment) * 0.25m;
            decisionScore += sentimentFactor;
            factors.Add(new DecisionFactor { Name = "Sentiment Alignment", Score = sentimentFactor, Weight = 0.25m });

            // Pattern confirmation factor (20% weight)
            var patternFactor = CalculatePatternConfirmationFactor(signal) * 0.2m;
            decisionScore += patternFactor;
            factors.Add(new DecisionFactor { Name = "Pattern Confirmation", Score = patternFactor, Weight = 0.2m });

            // Risk assessment factor (15% weight)
            var riskFactor = (signal.RiskAssessment.OverallRiskScore / 100m) * 0.15m;
            decisionScore += riskFactor;
            factors.Add(new DecisionFactor { Name = "Risk Assessment", Score = riskFactor, Weight = 0.15m });

            // Apply adaptive weighting
            decisionScore *= weighting.AdaptiveWeight;

            // Convert to percentage
            decision.Confidence = Math.Max(0, Math.Min(100, decisionScore * 100));

            // Determine final decision
            decision.Decision = decision.Confidence switch
            {
                >= 80 => "STRONG_BUY",
                >= 70 => "BUY",
                >= 60 => "WEAK_BUY",
                >= 40 => "HOLD",
                >= 30 => "WEAK_SELL",
                >= 20 => "SELL",
                _ => "STRONG_SELL"
            };

            // Calculate suggested position size
            decision.SuggestedPositionSize = CalculatePositionSize(decision.Confidence, weighting.AdaptiveWeight);

            decision.DecisionFactors = factors;
            decision.RiskLevel = DetermineRiskLevel(decision.Confidence, signal.RiskAssessment);

            return decision;
        }

        /// <summary>
        /// Generate comprehensive analysis report
        /// </summary>
        private ComprehensiveAnalysis GenerateComprehensiveAnalysis(
            EnhancedTradingSignal signal,
            MarketSentimentResult sentiment,
            AdaptiveWeightResult weighting,
            TradingDecision decision)
        {
            var analysis = new ComprehensiveAnalysis
            {
                GeneratedAt = DateTime.UtcNow,
                SignalId = signal.SignalId,
                OverallAssessment = GenerateOverallAssessment(decision),
                KeyStrengths = GenerateKeyStrengths(signal, sentiment, weighting),
                KeyWeaknesses = GenerateKeyWeaknesses(signal, sentiment, weighting),
                TradingRecommendations = GenerateTradingRecommendations(signal, decision),
                RiskConsiderations = GenerateRiskConsiderations(signal),
                MarketContextAnalysis = GenerateMarketContextAnalysis(sentiment),
                TechnicalAnalysis = GenerateTechnicalAnalysis(signal),
                TimeframeSuggestions = GenerateTimeframeSuggestions(signal, decision)
            };

            return analysis;
        }

        // Helper methods for decision making and analysis

        private decimal CalculateSentimentAlignmentFactor(EnhancedTradingSignal signal, MarketSentimentResult sentiment)
        {
            // Determine signal direction
            var signalDirection = signal.OriginalSignal.Type?.ToUpper() == "PE" ? 1 : -1;
            var sentimentDirection = sentiment.CompositeSentimentScore > 0 ? 1 : -1;
            
            // Calculate alignment
            var alignment = signalDirection == sentimentDirection ? 1 : 0;
            var sentimentStrength = Math.Abs(sentiment.CompositeSentimentScore) / 100m;
            
            return alignment * sentimentStrength;
        }

        private decimal CalculatePatternConfirmationFactor(EnhancedTradingSignal signal)
        {
            if (!signal.DetectedPatterns.Any())
                return 0.3m; // Neutral if no patterns
            
            var avgConfidence = signal.DetectedPatterns.Average(p => p.Confidence);
            var strongPatterns = signal.DetectedPatterns.Count(p => p.Confidence >= 75);
            
            return (avgConfidence / 100m) * (1 + strongPatterns * 0.1m);
        }

        private decimal CalculatePositionSize(decimal confidence, decimal adaptiveWeight)
        {
            var baseSize = 1.0m;
            var confidenceMultiplier = Math.Max(0.3m, confidence / 100m);
            var weightMultiplier = Math.Max(0.5m, adaptiveWeight);
            
            return baseSize * confidenceMultiplier * weightMultiplier;
        }

        private string DetermineRiskLevel(decimal confidence, RiskAssessment risk)
        {
            if (risk.OverallRiskScore < 30 || confidence < 40)
                return "HIGH";
            else if (risk.OverallRiskScore < 60 || confidence < 60)
                return "MEDIUM";
            else
                return "LOW";
        }

        private string GenerateOverallAssessment(TradingDecision decision)
        {
            return decision.Decision switch
            {
                "STRONG_BUY" => "Highly favorable conditions for aggressive position entry",
                "BUY" => "Good conditions for position entry with standard size",
                "WEAK_BUY" => "Moderately favorable conditions for small position entry",
                "HOLD" => "Mixed signals suggest holding current positions",
                "WEAK_SELL" => "Slightly unfavorable conditions suggest position reduction",
                "SELL" => "Unfavorable conditions suggest position exit",
                "STRONG_SELL" => "Highly unfavorable conditions require immediate position exit",
                _ => "Neutral assessment"
            };
        }

        private List<string> GenerateKeyStrengths(EnhancedTradingSignal signal, MarketSentimentResult sentiment, AdaptiveWeightResult weighting)
        {
            var strengths = new List<string>();
            
            if (signal.ConfidenceScore >= 80)
                strengths.Add("High AI confidence score");
            
            if (sentiment.CompositeSentimentScore > 20)
                strengths.Add("Strong positive market sentiment");
            
            if (weighting.AdaptiveWeight > 1.2m)
                strengths.Add("Favorable adaptive weighting");
            
            if (signal.DetectedPatterns.Any(p => p.Confidence >= 80))
                strengths.Add("Strong technical patterns detected");
            
            return strengths;
        }

        private List<string> GenerateKeyWeaknesses(EnhancedTradingSignal signal, MarketSentimentResult sentiment, AdaptiveWeightResult weighting)
        {
            var weaknesses = new List<string>();
            
            if (signal.ConfidenceScore < 50)
                weaknesses.Add("Low AI confidence score");
            
            if (sentiment.CompositeSentimentScore < -20)
                weaknesses.Add("Negative market sentiment");
            
            if (weighting.AdaptiveWeight < 0.8m)
                weaknesses.Add("Unfavorable adaptive weighting");
            
            if (signal.RiskAssessment.OverallRiskScore < 40)
                weaknesses.Add("High risk assessment");
            
            return weaknesses;
        }

        private List<string> GenerateTradingRecommendations(EnhancedTradingSignal signal, TradingDecision decision)
        {
            var recommendations = new List<string>();
            
            recommendations.Add($"Suggested action: {decision.Decision}");
            recommendations.Add($"Position size: {decision.SuggestedPositionSize:F2}x standard");
            recommendations.Add($"Risk level: {decision.RiskLevel}");
            
            if (decision.Confidence < 60)
                recommendations.Add("Consider waiting for higher confidence signals");
            
            return recommendations;
        }

        private List<string> GenerateRiskConsiderations(EnhancedTradingSignal signal)
        {
            var considerations = new List<string>();
            
            considerations.Add($"Overall risk score: {signal.RiskAssessment.OverallRiskScore:F1}/100");
            considerations.Add($"Volatility risk: {signal.RiskAssessment.VolatilityRisk:F1}/100");
            considerations.Add($"Timing risk: {signal.RiskAssessment.TimingRisk:F1}/100");
            
            return considerations;
        }

        private List<string> GenerateMarketContextAnalysis(MarketSentimentResult sentiment)
        {
            var analysis = new List<string>();
            
            analysis.Add($"Market sentiment: {sentiment.SentimentDirection}");
            analysis.Add($"Sentiment score: {sentiment.CompositeSentimentScore:F1}/100");
            analysis.Add($"Confidence level: {sentiment.ConfidenceLevel:F1}%");
            
            return analysis;
        }

        private List<string> GenerateTechnicalAnalysis(EnhancedTradingSignal signal)
        {
            var analysis = new List<string>();
            
            analysis.Add($"Detected patterns: {signal.DetectedPatterns.Count}");
            
            if (signal.DetectedPatterns.Any())
            {
                var topPattern = signal.DetectedPatterns.OrderByDescending(p => p.Confidence).First();
                analysis.Add($"Top pattern: {topPattern.PatternName} ({topPattern.Confidence:F1}% confidence)");
            }
            
            return analysis;
        }

        private List<string> GenerateTimeframeSuggestions(EnhancedTradingSignal signal, TradingDecision decision)
        {
            var suggestions = new List<string>();
            
            if (decision.Confidence >= 80)
                suggestions.Add("Suitable for immediate execution");
            else if (decision.Confidence >= 60)
                suggestions.Add("Consider execution within next 30 minutes");
            else
                suggestions.Add("Wait for better timing or higher confidence");
            
            return suggestions;
        }
    }

    // Request/Response DTOs
    public class ModelTrainingRequest
    {
        [Required]
        public DateTime FromDate { get; set; }
        
        [Required]
        public DateTime ToDate { get; set; }
    }

    public class PatternAnalysisRequest
    {
        [Required]
        public string Symbol { get; set; } = string.Empty;
        
        [Required]
        public TradingViewAlert Signal { get; set; } = new();
    }

    public class AIEnhancedSignalResponse
    {
        public DateTime ProcessingStartTime { get; set; }
        public DateTime ProcessingEndTime { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public TradingViewAlert OriginalSignal { get; set; } = new();
        public EnhancedTradingSignal EnhancedSignal { get; set; } = new();
        public MarketSentimentResult SentimentAnalysis { get; set; } = new();
        public AdaptiveWeightResult WeightingResult { get; set; } = new();
        public TradingDecision TradingDecision { get; set; } = new();
        public ComprehensiveAnalysis Analysis { get; set; } = new();
    }

    public class ModelTrainingResponse
    {
        public DateTime TrainingStartTime { get; set; }
        public DateTime TrainingEndTime { get; set; }
        public TimeSpan TrainingDuration { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public ModelTrainingResult TrainingResult { get; set; } = new();
    }

    public class PatternAnalysisResponse
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime AnalysisTimestamp { get; set; }
        public List<DetectedPattern> DetectedPatterns { get; set; } = new();
        public int PatternCount { get; set; }
        public List<DetectedPattern> HighConfidencePatterns { get; set; } = new();
    }

    public class AIDashboardResponse
    {
        public DateTime GeneratedAt { get; set; }
        public MarketSentimentResult MarketSentiment { get; set; } = new();
        public AIModelPerformanceResponse ModelPerformance { get; set; } = new();
        public List<AISignalSummary> RecentEnhancedSignals { get; set; } = new();
    }

    public class AIModelPerformanceResponse
    {
        public DateTime ReportGeneratedAt { get; set; }
        public decimal SignalValidationAccuracy { get; set; }
        public decimal PatternRecognitionAccuracy { get; set; }
        public decimal SentimentAnalysisAccuracy { get; set; }
        public decimal AdaptiveWeightingEffectiveness { get; set; }
        public decimal OverallSystemAccuracy { get; set; }
    }

    public class TradingDecision
    {
        public DateTime Timestamp { get; set; }
        public string SignalId { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public decimal SuggestedPositionSize { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public List<DecisionFactor> DecisionFactors { get; set; } = new();
    }

    public class DecisionFactor
    {
        public string Name { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal Weight { get; set; }
    }

    public class ComprehensiveAnalysis
    {
        public DateTime GeneratedAt { get; set; }
        public string SignalId { get; set; } = string.Empty;
        public string OverallAssessment { get; set; } = string.Empty;
        public List<string> KeyStrengths { get; set; } = new();
        public List<string> KeyWeaknesses { get; set; } = new();
        public List<string> TradingRecommendations { get; set; } = new();
        public List<string> RiskConsiderations { get; set; } = new();
        public List<string> MarketContextAnalysis { get; set; } = new();
        public List<string> TechnicalAnalysis { get; set; } = new();
        public List<string> TimeframeSuggestions { get; set; } = new();
    }

    public class AISignalSummary
    {
        public string SignalId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string Decision { get; set; } = string.Empty;
        public decimal AdaptiveWeight { get; set; }
    }
}