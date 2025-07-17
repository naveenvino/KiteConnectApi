using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KiteConnectApi.Controllers
{
    /// <summary>
    /// Controller for AI-enhanced historical backtesting
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HistoricalBacktestingController : ControllerBase
    {
        private readonly HistoricalBacktestingService _backtestingService;
        private readonly ILogger<HistoricalBacktestingController> _logger;

        public HistoricalBacktestingController(
            HistoricalBacktestingService backtestingService,
            ILogger<HistoricalBacktestingController> logger)
        {
            _backtestingService = backtestingService;
            _logger = logger;
        }

        /// <summary>
        /// Run AI-enhanced historical backtest
        /// </summary>
        [HttpPost("run")]
        public async Task<ActionResult<BacktestResult>> RunBacktestAsync([FromBody] BacktestParameters parameters)
        {
            try
            {
                _logger.LogInformation("Starting historical backtest from {FromDate} to {ToDate}", 
                    parameters.FromDate, parameters.ToDate);

                var result = await _backtestingService.RunBacktestAsync(parameters);

                _logger.LogInformation("Backtest completed. Total trades: {TotalTrades}, Win rate: {WinRate:P2}", 
                    result.TradingResults.Count, 
                    result.PerformanceMetrics.WinRate);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running historical backtest");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get sample backtest parameters
        /// </summary>
        [HttpGet("sample-parameters")]
        public ActionResult<BacktestParameters> GetSampleParameters()
        {
            var sampleParams = new BacktestParameters
            {
                FromDate = DateTime.UtcNow.AddDays(-30),
                ToDate = DateTime.UtcNow,
                InitialCapital = 100000m,
                PositionSizePercentage = 0.02m,
                MinConfidenceScore = 60m,
                StopLossPercentage = 0.1m,
                RequirePatternConfirmation = false,
                SignalTypes = new List<string> { "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8" },
                OptionTypes = new List<string> { "CE", "PE" },
                Strategy = "AI-Enhanced"
            };

            return Ok(sampleParams);
        }

        /// <summary>
        /// Get quick AI analysis for last week
        /// </summary>
        [HttpGet("quick-analysis")]
        public async Task<ActionResult<BacktestResult>> GetQuickAnalysisAsync()
        {
            try
            {
                var parameters = new BacktestParameters
                {
                    FromDate = DateTime.UtcNow.AddDays(-7),
                    ToDate = DateTime.UtcNow,
                    InitialCapital = 100000m,
                    PositionSizePercentage = 0.02m,
                    MinConfidenceScore = 50m,
                    StopLossPercentage = 0.1m,
                    RequirePatternConfirmation = false
                };

                var result = await _backtestingService.RunBacktestAsync(parameters);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running quick analysis");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }

        /// <summary>
        /// Get AI performance statistics
        /// </summary>
        [HttpGet("ai-stats")]
        public ActionResult<object> GetAIPerformanceStats()
        {
            try
            {
                var stats = new
                {
                    AICapabilities = new
                    {
                        MLSignalValidation = "Confidence scoring and outcome prediction",
                        PatternRecognition = "Candlestick, trend, volume, and volatility patterns",
                        SentimentAnalysis = "6-source market sentiment analysis",
                        AdaptiveWeighting = "Performance-based signal weighting",
                        RiskManagement = "Dynamic position sizing and risk assessment"
                    },
                    SupportedSignals = new[] { "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8" },
                    SupportedOptionTypes = new[] { "CE", "PE" },
                    AnalysisMetrics = new[]
                    {
                        "Win Rate by Confidence Bucket",
                        "Performance by Market Conditions",
                        "AI vs Non-AI Signal Comparison",
                        "Risk-Adjusted Returns",
                        "Pattern Success Rates",
                        "Sentiment Correlation Analysis"
                    }
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI stats");
                return StatusCode(500, new { error = "Internal server error", message = ex.Message });
            }
        }
    }
}