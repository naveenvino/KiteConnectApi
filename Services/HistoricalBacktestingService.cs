using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.AI;
using KiteConnectApi.Models.Dto;
using KiteConnectApi.Services.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace KiteConnectApi.Services
{
    /// <summary>
    /// Historical backtesting service for validating AI signal performance
    /// </summary>
    public class HistoricalBacktestingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HistoricalBacktestingService> _logger;
        private readonly IConfiguration _configuration;
        private readonly MLSignalValidationService _mlValidationService;
        private readonly PatternRecognitionService _patternService;
        private readonly MarketSentimentAnalyzer _sentimentAnalyzer;
        private readonly AdaptiveSignalWeightingService _weightingService;
        private readonly HistoricalOptionsDataService _optionsDataService;

        public HistoricalBacktestingService(
            ApplicationDbContext context,
            ILogger<HistoricalBacktestingService> logger,
            IConfiguration configuration,
            MLSignalValidationService mlValidationService,
            PatternRecognitionService patternService,
            MarketSentimentAnalyzer sentimentAnalyzer,
            AdaptiveSignalWeightingService weightingService,
            HistoricalOptionsDataService optionsDataService)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
            _mlValidationService = mlValidationService;
            _patternService = patternService;
            _sentimentAnalyzer = sentimentAnalyzer;
            _weightingService = weightingService;
            _optionsDataService = optionsDataService;
        }

        /// <summary>
        /// Run comprehensive backtest on historical data
        /// </summary>
        public async Task<BacktestResult> RunBacktestAsync(BacktestParameters parameters)
        {
            _logger.LogInformation("Starting backtest from {FromDate} to {ToDate}", 
                parameters.FromDate, parameters.ToDate);

            var result = new BacktestResult
            {
                StartTime = DateTime.UtcNow,
                Parameters = parameters,
                TradingResults = new List<BacktestTrade>(),
                PerformanceMetrics = new BacktestPerformanceMetrics()
            };

            try
            {
                // Get historical signals
                var historicalSignals = await GetHistoricalSignalsAsync(parameters);
                _logger.LogInformation("Found {Count} historical signals for backtesting", historicalSignals.Count);

                // Initialize capital tracking
                var currentCapital = parameters.InitialCapital;
                var peakCapital = parameters.InitialCapital;
                var maxDrawdown = 0m;

                // Process each signal
                foreach (var signal in historicalSignals)
                {
                    try
                    {
                        var trade = await ProcessSignalForBacktestAsync(signal, currentCapital, parameters);
                        
                        if (trade != null)
                        {
                            result.TradingResults.Add(trade);
                            
                            // Update capital tracking
                            currentCapital += trade.PnL;
                            
                            if (currentCapital > peakCapital)
                            {
                                peakCapital = currentCapital;
                            }
                            else
                            {
                                var drawdown = (peakCapital - currentCapital) / peakCapital;
                                if (drawdown > maxDrawdown)
                                {
                                    maxDrawdown = drawdown;
                                }
                            }

                            // Check if we hit stop loss
                            if (parameters.StopLossPercentage > 0)
                            {
                                var totalLoss = (parameters.InitialCapital - currentCapital) / parameters.InitialCapital;
                                if (totalLoss >= parameters.StopLossPercentage)
                                {
                                    _logger.LogWarning("Backtest stopped due to stop loss at {Date}. Loss: {Loss:P2}", 
                                        signal.ReceivedTime, totalLoss);
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing signal {SignalId} for backtest", signal.Signal);
                    }
                }

                // Calculate performance metrics
                result.PerformanceMetrics = CalculatePerformanceMetrics(result.TradingResults, parameters);
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;

                // Generate detailed analysis
                result.Analysis = GenerateBacktestAnalysis(result);

                _logger.LogInformation("Backtest completed. Total trades: {Trades}, Win rate: {WinRate:P2}, Total return: {Return:P2}", 
                    result.TradingResults.Count, 
                    result.PerformanceMetrics.WinRate, 
                    result.PerformanceMetrics.TotalReturn);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running backtest");
                result.ErrorMessage = ex.Message;
                result.EndTime = DateTime.UtcNow;
                return result;
            }
        }

        /// <summary>
        /// Get historical signals for backtesting
        /// </summary>
        private async Task<List<ManualTradingViewAlert>> GetHistoricalSignalsAsync(BacktestParameters parameters)
        {
            var query = _context.ManualTradingViewAlerts
                .Where(a => a.ReceivedTime >= parameters.FromDate && a.ReceivedTime <= parameters.ToDate);

            if (parameters.SignalTypes?.Any() == true)
            {
                query = query.Where(a => parameters.SignalTypes.Contains(a.Signal));
            }

            if (parameters.OptionTypes?.Any() == true)
            {
                query = query.Where(a => parameters.OptionTypes.Contains(a.Type));
            }

            return await query
                .OrderBy(a => a.ReceivedTime)
                .ToListAsync();
        }

        /// <summary>
        /// Process a single signal for backtesting
        /// </summary>
        private async Task<BacktestTrade?> ProcessSignalForBacktestAsync(
            ManualTradingViewAlert signal, 
            decimal currentCapital, 
            BacktestParameters parameters)
        {
            try
            {
                // Convert to TradingViewAlert for AI processing
                var tradingViewAlert = new TradingViewAlert
                {
                    Signal = signal.Signal,
                    Strike = signal.Strike,
                    Type = signal.Type,
                    Action = signal.Action,
                    Index = "NIFTY", // Default to NIFTY since ManualTradingViewAlert doesn't have Index field
                    StrategyName = signal.StrategyName,
                    Timestamp = signal.ReceivedTime
                };

                // Process through AI enhancement pipeline
                var enhancedSignal = await _mlValidationService.ValidateSignalAsync(tradingViewAlert);
                var patterns = await _patternService.DetectPatternsAsync(tradingViewAlert);
                var sentiment = await _sentimentAnalyzer.AnalyzeSentimentAsync("NIFTY");
                var weighting = await _weightingService.CalculateSignalWeightAsync(enhancedSignal);

                // Apply filters
                if (enhancedSignal.ConfidenceScore < parameters.MinConfidenceScore)
                {
                    return null; // Skip low confidence signals
                }

                if (parameters.RequirePatternConfirmation && !patterns.Any(p => p.Confidence >= 70))
                {
                    return null; // Skip signals without pattern confirmation
                }

                // Calculate position size
                var positionSize = CalculatePositionSize(enhancedSignal, currentCapital, parameters);

                // Get historical price data for realistic backtesting
                var priceData = await _optionsDataService.GetBacktestPricesAsync(
                    tradingViewAlert, 
                    signal.ReceivedTime, 
                    signal.ReceivedTime.AddMinutes(30)); // Default 30-minute holding period

                if (priceData.EntryPrice == 0)
                {
                    return null; // Skip signals without valid price data
                }

                // Calculate position-adjusted P&L
                var totalPnL = priceData.PnL * positionSize;
                var isWin = totalPnL > 0;

                // Create backtest trade with real price data
                var trade = new BacktestTrade
                {
                    SignalId = signal.Signal ?? "Unknown",
                    Timestamp = signal.ReceivedTime,
                    Strike = signal.Strike,
                    OptionType = signal.Type ?? "CE",
                    Action = signal.Action ?? "Entry",
                    
                    // AI Enhancement Data
                    AIConfidenceScore = enhancedSignal.ConfidenceScore,
                    DetectedPatterns = patterns.Count,
                    SentimentScore = sentiment.CompositeSentimentScore,
                    AdaptiveWeight = weighting.AdaptiveWeight,
                    
                    // Position Details
                    PositionSize = positionSize,
                    EntryPrice = priceData.EntryPrice,
                    ExitPrice = priceData.ExitPrice,
                    
                    // Outcome with real price-based P&L
                    PnL = totalPnL,
                    IsWin = isWin,
                    HoldingPeriodMinutes = priceData.HoldingPeriodMinutes,
                    
                    // Risk Metrics
                    RiskScore = enhancedSignal.RiskAssessment.OverallRiskScore,
                    MaxDrawdown = 0, // Would be calculated in real-time
                    
                    // Additional Data with price information
                    MarketConditions = GetMarketConditions(signal.ReceivedTime),
                    Metadata = new Dictionary<string, object>
                    {
                        ["OriginalSignal"] = JsonSerializer.Serialize(signal),
                        ["EnhancedSignal"] = JsonSerializer.Serialize(enhancedSignal),
                        ["Patterns"] = JsonSerializer.Serialize(patterns),
                        ["Sentiment"] = JsonSerializer.Serialize(sentiment),
                        ["Weighting"] = JsonSerializer.Serialize(weighting),
                        ["PriceData"] = JsonSerializer.Serialize(priceData),
                        ["EntryVolume"] = priceData.EntryVolume,
                        ["ExitVolume"] = priceData.ExitVolume,
                        ["EntryIV"] = priceData.EntryImpliedVolatility,
                        ["ExitIV"] = priceData.ExitImpliedVolatility,
                        ["DataSource"] = priceData.DataSource
                    }
                };

                return trade;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing signal {SignalId} for backtest", signal.Signal);
                return null;
            }
        }

        /// <summary>
        /// Get historical outcome for a signal
        /// </summary>
        private async Task<ApiTradeLog?> GetHistoricalOutcomeAsync(ManualTradingViewAlert signal)
        {
            // Look for trade log entries within 30 minutes of signal
            var outcome = await _context.ApiTradeLog
                .Where(t => t.SignalId == signal.Signal && 
                           Math.Abs(EF.Functions.DateDiffMinute(t.EntryTime, signal.ReceivedTime)) <= 30)
                .OrderBy(t => Math.Abs(EF.Functions.DateDiffMinute(t.EntryTime, signal.ReceivedTime)))
                .FirstOrDefaultAsync();

            return outcome;
        }

        /// <summary>
        /// Calculate position size based on AI confidence and risk parameters
        /// </summary>
        private decimal CalculatePositionSize(EnhancedTradingSignal signal, decimal currentCapital, BacktestParameters parameters)
        {
            var baseSize = currentCapital * parameters.PositionSizePercentage;
            var confidenceMultiplier = signal.ConfidenceScore / 100m;
            var adaptiveMultiplier = Math.Max(0.5m, Math.Min(2.0m, signal.AdaptiveWeight));
            
            return baseSize * confidenceMultiplier * adaptiveMultiplier;
        }

        /// <summary>
        /// Calculate holding period in minutes
        /// </summary>
        private int CalculateHoldingPeriod(ApiTradeLog outcome)
        {
            if (outcome.ExitTime.HasValue)
            {
                return (int)(outcome.ExitTime.Value - outcome.EntryTime).TotalMinutes;
            }
            return 0;
        }

        /// <summary>
        /// Get market conditions at signal time
        /// </summary>
        private string GetMarketConditions(DateTime signalTime)
        {
            var hour = signalTime.Hour;
            var dayOfWeek = signalTime.DayOfWeek;
            
            if (hour < 9 || hour > 15)
                return "PrePost Market";
            else if (hour >= 9 && hour <= 10)
                return "Opening Hour";
            else if (hour >= 14 && hour <= 15)
                return "Closing Hour";
            else if (dayOfWeek == DayOfWeek.Monday || dayOfWeek == DayOfWeek.Friday)
                return "Week Start/End";
            else
                return "Regular Trading";
        }

        /// <summary>
        /// Calculate comprehensive performance metrics
        /// </summary>
        private BacktestPerformanceMetrics CalculatePerformanceMetrics(List<BacktestTrade> trades, BacktestParameters parameters)
        {
            if (!trades.Any())
            {
                return new BacktestPerformanceMetrics();
            }

            var totalPnL = trades.Sum(t => t.PnL);
            var winningTrades = trades.Where(t => t.IsWin).ToList();
            var losingTrades = trades.Where(t => !t.IsWin).ToList();

            var metrics = new BacktestPerformanceMetrics
            {
                TotalTrades = trades.Count,
                WinningTrades = winningTrades.Count,
                LosingTrades = losingTrades.Count,
                WinRate = winningTrades.Count / (decimal)trades.Count,
                
                TotalReturn = totalPnL / parameters.InitialCapital,
                TotalPnL = totalPnL,
                
                AverageWin = winningTrades.Any() ? winningTrades.Average(t => t.PnL) : 0,
                AverageLoss = losingTrades.Any() ? losingTrades.Average(t => t.PnL) : 0,
                
                LargestWin = winningTrades.Any() ? winningTrades.Max(t => t.PnL) : 0,
                LargestLoss = losingTrades.Any() ? losingTrades.Min(t => t.PnL) : 0,
                
                ProfitFactor = losingTrades.Any() ? 
                    Math.Abs(winningTrades.Sum(t => t.PnL) / losingTrades.Sum(t => t.PnL)) : 0,
                
                AverageHoldingPeriod = (decimal)trades.Average(t => t.HoldingPeriodMinutes),
                
                Sharpe = CalculateSharpeRatio(trades),
                MaxDrawdown = CalculateMaxDrawdown(trades, parameters.InitialCapital),
                
                // AI-specific metrics
                AverageConfidenceScore = trades.Average(t => t.AIConfidenceScore),
                AverageSentimentScore = trades.Average(t => t.SentimentScore),
                AverageAdaptiveWeight = trades.Average(t => t.AdaptiveWeight),
                
                // Performance by confidence buckets
                HighConfidenceWinRate = CalculateWinRateByConfidence(trades, 80, 100),
                MediumConfidenceWinRate = CalculateWinRateByConfidence(trades, 60, 80),
                LowConfidenceWinRate = CalculateWinRateByConfidence(trades, 0, 60),
                
                // Performance by market conditions
                PerformanceByMarketConditions = trades
                    .GroupBy(t => t.MarketConditions)
                    .ToDictionary(g => g.Key, g => new MarketConditionPerformance
                    {
                        TradeCount = g.Count(),
                        WinRate = g.Count(t => t.IsWin) / (decimal)g.Count(),
                        TotalPnL = g.Sum(t => t.PnL),
                        AverageConfidence = g.Average(t => t.AIConfidenceScore)
                    })
            };

            return metrics;
        }

        /// <summary>
        /// Calculate Sharpe ratio
        /// </summary>
        private decimal CalculateSharpeRatio(List<BacktestTrade> trades)
        {
            if (trades.Count < 2) return 0;

            var returns = trades.Select(t => t.PnL).ToList();
            var avgReturn = returns.Average();
            var stdDev = CalculateStandardDeviation(returns);
            
            return stdDev == 0 ? 0 : avgReturn / stdDev;
        }

        /// <summary>
        /// Calculate maximum drawdown
        /// </summary>
        private decimal CalculateMaxDrawdown(List<BacktestTrade> trades, decimal initialCapital)
        {
            var capital = initialCapital;
            var peak = initialCapital;
            var maxDrawdown = 0m;

            foreach (var trade in trades)
            {
                capital += trade.PnL;
                if (capital > peak)
                {
                    peak = capital;
                }
                else
                {
                    var drawdown = (peak - capital) / peak;
                    if (drawdown > maxDrawdown)
                    {
                        maxDrawdown = drawdown;
                    }
                }
            }

            return maxDrawdown;
        }

        /// <summary>
        /// Calculate win rate by confidence bucket
        /// </summary>
        private decimal CalculateWinRateByConfidence(List<BacktestTrade> trades, decimal minConfidence, decimal maxConfidence)
        {
            var filteredTrades = trades.Where(t => t.AIConfidenceScore >= minConfidence && t.AIConfidenceScore < maxConfidence).ToList();
            if (!filteredTrades.Any()) return 0;
            
            return filteredTrades.Count(t => t.IsWin) / (decimal)filteredTrades.Count;
        }

        /// <summary>
        /// Calculate standard deviation
        /// </summary>
        private decimal CalculateStandardDeviation(List<decimal> values)
        {
            if (values.Count < 2) return 0;
            
            var mean = values.Average();
            var variance = values.Average(v => (decimal)Math.Pow((double)(v - mean), 2));
            return (decimal)Math.Sqrt((double)variance);
        }

        /// <summary>
        /// Generate detailed backtest analysis
        /// </summary>
        private BacktestAnalysis GenerateBacktestAnalysis(BacktestResult result)
        {
            return new BacktestAnalysis
            {
                Summary = GenerateSummary(result),
                KeyFindings = GenerateKeyFindings(result),
                Recommendations = GenerateRecommendations(result),
                RiskAnalysis = GenerateRiskAnalysis(result),
                AIPerformanceAnalysis = GenerateAIPerformanceAnalysis(result),
                OptimizationSuggestions = GenerateOptimizationSuggestions(result)
            };
        }

        private string GenerateSummary(BacktestResult result)
        {
            var metrics = result.PerformanceMetrics;
            return $"Backtest completed with {metrics.TotalTrades} trades over {result.Duration.TotalDays:F0} days. " +
                   $"Win rate: {metrics.WinRate:P2}, Total return: {metrics.TotalReturn:P2}, " +
                   $"Sharpe ratio: {metrics.Sharpe:F2}, Max drawdown: {metrics.MaxDrawdown:P2}";
        }

        private List<string> GenerateKeyFindings(BacktestResult result)
        {
            var findings = new List<string>();
            var metrics = result.PerformanceMetrics;
            
            if (metrics.WinRate > 0.6m)
                findings.Add("High win rate indicates strong signal quality");
            
            if (metrics.HighConfidenceWinRate > metrics.LowConfidenceWinRate)
                findings.Add("AI confidence scores are positively correlated with success");
            
            if (metrics.ProfitFactor > 1.5m)
                findings.Add("Strong profit factor indicates good risk-reward ratio");
            
            if (metrics.MaxDrawdown > 0.2m)
                findings.Add("High drawdown suggests need for better risk management");
            
            return findings;
        }

        private List<string> GenerateRecommendations(BacktestResult result)
        {
            var recommendations = new List<string>();
            var metrics = result.PerformanceMetrics;
            
            if (metrics.WinRate < 0.5m)
                recommendations.Add("Consider increasing minimum confidence threshold");
            
            if (metrics.MaxDrawdown > 0.15m)
                recommendations.Add("Implement stricter position sizing rules");
            
            if (metrics.AverageConfidenceScore < 70)
                recommendations.Add("Focus on higher confidence signals only");
            
            return recommendations;
        }

        private string GenerateRiskAnalysis(BacktestResult result)
        {
            var metrics = result.PerformanceMetrics;
            return $"Risk analysis: Max drawdown {metrics.MaxDrawdown:P2}, " +
                   $"Largest loss {metrics.LargestLoss:C}, " +
                   $"Average loss {metrics.AverageLoss:C}";
        }

        private string GenerateAIPerformanceAnalysis(BacktestResult result)
        {
            var metrics = result.PerformanceMetrics;
            return $"AI Performance: Average confidence {metrics.AverageConfidenceScore:F1}%, " +
                   $"High confidence win rate {metrics.HighConfidenceWinRate:P2}, " +
                   $"Average sentiment {metrics.AverageSentimentScore:F1}";
        }

        private List<string> GenerateOptimizationSuggestions(BacktestResult result)
        {
            var suggestions = new List<string>();
            var metrics = result.PerformanceMetrics;
            
            if (metrics.HighConfidenceWinRate > metrics.MediumConfidenceWinRate + 0.1m)
                suggestions.Add("Increase position size for high confidence signals");
            
            if (metrics.AverageAdaptiveWeight < 0.8m)
                suggestions.Add("Review adaptive weighting algorithm");
            
            return suggestions;
        }
    }

    // Supporting classes
    public class BacktestParameters
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal InitialCapital { get; set; } = 100000m;
        public decimal PositionSizePercentage { get; set; } = 0.02m; // 2% of capital per trade
        public decimal MinConfidenceScore { get; set; } = 60m;
        public decimal StopLossPercentage { get; set; } = 0.1m; // 10% total loss
        public bool RequirePatternConfirmation { get; set; } = false;
        public List<string>? SignalTypes { get; set; }
        public List<string>? OptionTypes { get; set; }
        public string? Strategy { get; set; }
    }

    public class BacktestResult
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public BacktestParameters Parameters { get; set; } = new();
        public List<BacktestTrade> TradingResults { get; set; } = new();
        public BacktestPerformanceMetrics PerformanceMetrics { get; set; } = new();
        public BacktestAnalysis Analysis { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class BacktestTrade
    {
        public string SignalId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int Strike { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        
        // AI Enhancement Data
        public decimal AIConfidenceScore { get; set; }
        public int DetectedPatterns { get; set; }
        public decimal SentimentScore { get; set; }
        public decimal AdaptiveWeight { get; set; }
        
        // Position Details
        public decimal PositionSize { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        
        // Outcome
        public decimal PnL { get; set; }
        public bool IsWin { get; set; }
        public int HoldingPeriodMinutes { get; set; }
        
        // Risk Metrics
        public decimal RiskScore { get; set; }
        public decimal MaxDrawdown { get; set; }
        
        // Additional Data
        public string MarketConditions { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class BacktestPerformanceMetrics
    {
        public int TotalTrades { get; set; }
        public int WinningTrades { get; set; }
        public int LosingTrades { get; set; }
        public decimal WinRate { get; set; }
        
        public decimal TotalReturn { get; set; }
        public decimal TotalPnL { get; set; }
        
        public decimal AverageWin { get; set; }
        public decimal AverageLoss { get; set; }
        public decimal LargestWin { get; set; }
        public decimal LargestLoss { get; set; }
        
        public decimal ProfitFactor { get; set; }
        public decimal AverageHoldingPeriod { get; set; }
        
        public decimal Sharpe { get; set; }
        public decimal MaxDrawdown { get; set; }
        
        // AI-specific metrics
        public decimal AverageConfidenceScore { get; set; }
        public decimal AverageSentimentScore { get; set; }
        public decimal AverageAdaptiveWeight { get; set; }
        
        // Performance by confidence buckets
        public decimal HighConfidenceWinRate { get; set; }
        public decimal MediumConfidenceWinRate { get; set; }
        public decimal LowConfidenceWinRate { get; set; }
        
        // Performance by market conditions
        public Dictionary<string, MarketConditionPerformance> PerformanceByMarketConditions { get; set; } = new();
    }

    public class MarketConditionPerformance
    {
        public int TradeCount { get; set; }
        public decimal WinRate { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal AverageConfidence { get; set; }
    }

    public class BacktestAnalysis
    {
        public string Summary { get; set; } = string.Empty;
        public List<string> KeyFindings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public string RiskAnalysis { get; set; } = string.Empty;
        public string AIPerformanceAnalysis { get; set; } = string.Empty;
        public List<string> OptimizationSuggestions { get; set; } = new();
    }
}