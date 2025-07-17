using KiteConnectApi.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Models.AI
{
    /// <summary>
    /// Enhanced trading signal with AI validation and confidence scoring
    /// </summary>
    public class EnhancedTradingSignal
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        /// <summary>
        /// Original TradingView signal
        /// </summary>
        public TradingViewAlert OriginalSignal { get; set; } = new();
        
        /// <summary>
        /// When the signal was processed
        /// </summary>
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Signal identifier (S1, S2, etc.)
        /// </summary>
        public string SignalId { get; set; } = string.Empty;
        
        /// <summary>
        /// AI-calculated confidence score (0-100)
        /// </summary>
        public decimal ConfidenceScore { get; set; }
        
        /// <summary>
        /// AI recommendation for the signal
        /// </summary>
        public SignalRecommendation Recommendation { get; set; }
        
        /// <summary>
        /// Detailed validation scores from different AI models
        /// </summary>
        public SignalValidationScores ValidationScores { get; set; } = new();
        
        /// <summary>
        /// Current market context at the time of signal
        /// </summary>
        public MarketContext MarketContext { get; set; } = new();
        
        /// <summary>
        /// Historical performance context for this signal type
        /// </summary>
        public HistoricalPerformanceContext HistoricalPerformance { get; set; } = new();
        
        /// <summary>
        /// Detected chart patterns at the time of signal
        /// </summary>
        public List<DetectedPattern> DetectedPatterns { get; set; } = new();
        
        /// <summary>
        /// Market sentiment score (-100 to +100)
        /// </summary>
        public decimal SentimentScore { get; set; }
        
        /// <summary>
        /// Adaptive weight assigned to this signal
        /// </summary>
        public decimal AdaptiveWeight { get; set; } = 1.0m;
        
        /// <summary>
        /// Whether the signal passed AI validation
        /// </summary>
        public bool IsValidated { get; set; }
        
        /// <summary>
        /// Risk assessment for the signal
        /// </summary>
        public RiskAssessment RiskAssessment { get; set; } = new();
        
        /// <summary>
        /// Error message if validation failed
        /// </summary>
        public string? ErrorMessage { get; set; }
        
        /// <summary>
        /// Processing time for AI validation
        /// </summary>
        public TimeSpan ProcessingTime { get; set; }
        
        /// <summary>
        /// Additional metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// AI recommendation for trading signal
    /// </summary>
    public enum SignalRecommendation
    {
        StrongBuy = 5,
        Buy = 4,
        WeakBuy = 3,
        Hold = 2,
        Caution = 1,
        Avoid = 0
    }

    /// <summary>
    /// Detailed validation scores from different AI models
    /// </summary>
    public class SignalValidationScores
    {
        /// <summary>
        /// Overall signal quality score (0-100)
        /// </summary>
        public decimal QualityScore { get; set; }
        
        /// <summary>
        /// Confidence in quality assessment
        /// </summary>
        public decimal QualityConfidence { get; set; }
        
        /// <summary>
        /// Predicted outcome probability (0-100)
        /// </summary>
        public decimal OutcomeScore { get; set; }
        
        /// <summary>
        /// Confidence in outcome prediction
        /// </summary>
        public decimal OutcomeConfidence { get; set; }
        
        /// <summary>
        /// Market condition suitability score (0-100)
        /// </summary>
        public decimal MarketConditionScore { get; set; }
        
        /// <summary>
        /// Timing appropriateness score (0-100)
        /// </summary>
        public decimal TimingScore { get; set; }
        
        /// <summary>
        /// Risk assessment score (0-100, higher = lower risk)
        /// </summary>
        public decimal RiskScore { get; set; }
        
        /// <summary>
        /// Pattern recognition score (0-100)
        /// </summary>
        public decimal PatternScore { get; set; }
        
        /// <summary>
        /// Sentiment alignment score (0-100)
        /// </summary>
        public decimal SentimentScore { get; set; }
        
        /// <summary>
        /// Model ensemble agreement score (0-100)
        /// </summary>
        public decimal EnsembleAgreement { get; set; }
    }

    /// <summary>
    /// Current market context
    /// </summary>
    public class MarketContext
    {
        public string MarketTrend { get; set; } = string.Empty;
        public string VolatilityLevel { get; set; } = string.Empty;
        public string LiquidityLevel { get; set; } = string.Empty;
        public string MarketHours { get; set; } = string.Empty;
        public decimal UnderlyingPrice { get; set; }
        public decimal VIXLevel { get; set; }
        public string MarketRegime { get; set; } = string.Empty;
        public Dictionary<string, decimal> SectorPerformance { get; set; } = new();
    }

    /// <summary>
    /// Historical performance context
    /// </summary>
    public class HistoricalPerformanceContext
    {
        public decimal RecentWinRate { get; set; }
        public decimal RecentAvgPnL { get; set; }
        public int TotalTrades { get; set; }
        public DateTime? LastTradeDate { get; set; }
        public decimal BestPerformingTimeSlot { get; set; }
        public decimal WorstPerformingTimeSlot { get; set; }
        public List<string> BestMarketConditions { get; set; } = new();
        public List<string> WorstMarketConditions { get; set; } = new();
    }

    /// <summary>
    /// Detected chart pattern
    /// </summary>
    public class DetectedPattern
    {
        public string PatternName { get; set; } = string.Empty;
        public decimal Confidence { get; set; }
        public string Direction { get; set; } = string.Empty;
        public decimal ExpectedMagnitude { get; set; }
        public TimeSpan ExpectedDuration { get; set; }
        public Dictionary<string, decimal> PatternMetrics { get; set; } = new();
    }

    /// <summary>
    /// Risk assessment for the signal
    /// </summary>
    public class RiskAssessment
    {
        public decimal OverallRiskScore { get; set; }
        public decimal VolatilityRisk { get; set; }
        public decimal LiquidityRisk { get; set; }
        public decimal TimingRisk { get; set; }
        public decimal MarketRisk { get; set; }
        public decimal CounterpartyRisk { get; set; }
        public List<string> RiskFactors { get; set; } = new();
        public string RiskLevel { get; set; } = string.Empty;
        public decimal SuggestedPositionSize { get; set; }
        public decimal MaxLossEstimate { get; set; }
    }

    /// <summary>
    /// Features extracted from signal for ML processing
    /// </summary>
    public class SignalFeatures
    {
        public string SignalId { get; set; } = string.Empty;
        public int Strike { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        
        // Market data features
        public decimal UnderlyingPrice { get; set; }
        public decimal UnderlyingChange { get; set; }
        public decimal VIX { get; set; }
        public string MarketTrend { get; set; } = string.Empty;
        public long Volume { get; set; }
        
        // Technical indicators
        public decimal RSI { get; set; }
        public decimal MACD { get; set; }
        public decimal BollingerBandPosition { get; set; }
        public decimal SMA20 { get; set; }
        public decimal SMA50 { get; set; }
        
        // Time-based features
        public int HourOfDay { get; set; }
        public int DayOfWeek { get; set; }
        public decimal TimeToExpiry { get; set; }
        
        // Historical features
        public decimal HistoricalWinRate { get; set; }
        public decimal HistoricalAvgReturn { get; set; }
        public decimal RecentPerformance { get; set; }
        
        // Market regime features
        public string MarketRegime { get; set; } = string.Empty;
        public string VolatilityRegime { get; set; } = string.Empty;
    }

    /// <summary>
    /// Training data for ML models
    /// </summary>
    public class SignalTrainingData : SignalFeatures
    {
        // Labels for supervised learning
        public int ActualOutcome { get; set; } // 1 = Win, 0 = Loss
        public decimal ActualPnL { get; set; }
        public decimal QualityLabel { get; set; }
        public decimal TimingLabel { get; set; }
        public decimal RiskLabel { get; set; }
    }

    /// <summary>
    /// ML model predictions
    /// </summary>
    public class SignalQualityPrediction
    {
        public decimal Score { get; set; }
        public decimal Confidence { get; set; }
        public string[] Reasons { get; set; } = Array.Empty<string>();
    }

    public class OutcomePrediction
    {
        public decimal Score { get; set; }
        public decimal Confidence { get; set; }
        public decimal ExpectedPnL { get; set; }
        public decimal ProbabilityOfProfit { get; set; }
    }

    /// <summary>
    /// Model training result
    /// </summary>
    public class ModelTrainingResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalSamples { get; set; }
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public double SignalQualityModelAccuracy { get; set; }
        public double OutcomePredictorAccuracy { get; set; }
        public Dictionary<string, double> ModelMetrics { get; set; } = new();
    }

    /// <summary>
    /// Current market data for feature extraction
    /// </summary>
    public class CurrentMarketData
    {
        public decimal UnderlyingPrice { get; set; }
        public decimal UnderlyingChange { get; set; }
        public decimal VIX { get; set; }
        public string MarketTrend { get; set; } = string.Empty;
        public long Volume { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Technical indicators for signal analysis
    /// </summary>
    public class TechnicalIndicators
    {
        public decimal RSI { get; set; }
        public decimal MACD { get; set; }
        public decimal BollingerBandPosition { get; set; }
        public decimal SMA20 { get; set; }
        public decimal SMA50 { get; set; }
        public decimal ATR { get; set; }
        public decimal Stochastic { get; set; }
        public decimal WilliamsR { get; set; }
    }

    /// <summary>
    /// Historical statistics for signal performance
    /// </summary>
    public class HistoricalStats
    {
        public decimal WinRate { get; set; }
        public decimal AvgReturn { get; set; }
        public decimal RecentPerformance { get; set; }
        public decimal MaxDrawdown { get; set; }
        public decimal SharpeRatio { get; set; }
        public int TotalTrades { get; set; }
    }
}