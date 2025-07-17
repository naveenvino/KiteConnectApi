using KiteConnectApi.Data;
using KiteConnectApi.Models.AI;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace KiteConnectApi.Services.AI
{
    /// <summary>
    /// Advanced pattern recognition service for identifying technical chart patterns
    /// </summary>
    public class PatternRecognitionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PatternRecognitionService> _logger;
        private readonly IKiteConnectService _kiteConnectService;

        public PatternRecognitionService(
            ApplicationDbContext context,
            ILogger<PatternRecognitionService> logger,
            IKiteConnectService kiteConnectService)
        {
            _context = context;
            _logger = logger;
            _kiteConnectService = kiteConnectService;
        }

        /// <summary>
        /// Detect chart patterns for a given trading signal
        /// </summary>
        public async Task<List<DetectedPattern>> DetectPatternsAsync(TradingViewAlert signal)
        {
            try
            {
                _logger.LogInformation("Starting pattern detection for signal {SignalId}", signal.Signal);

                var patterns = new List<DetectedPattern>();
                
                // Get historical price data
                var priceData = await GetHistoricalPriceDataAsync("NIFTY", TimeSpan.FromDays(30));
                
                if (priceData.Count < 20)
                {
                    _logger.LogWarning("Insufficient price data for pattern detection");
                    return patterns;
                }

                // Detect various patterns
                patterns.AddRange(await DetectCandlestickPatternsAsync(priceData));
                patterns.AddRange(await DetectTrendPatternsAsync(priceData));
                patterns.AddRange(await DetectSupportResistancePatternsAsync(priceData));
                patterns.AddRange(await DetectVolumeBasedPatternsAsync(priceData));
                patterns.AddRange(await DetectVolatilityPatternsAsync(priceData));

                // Filter patterns by confidence threshold
                patterns = patterns.Where(p => p.Confidence >= 60).ToList();

                _logger.LogInformation("Pattern detection completed. Found {PatternCount} patterns", patterns.Count);
                return patterns;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting patterns for signal {SignalId}", signal.Signal);
                return new List<DetectedPattern>();
            }
        }

        /// <summary>
        /// Detect candlestick patterns
        /// </summary>
        private async Task<List<DetectedPattern>> DetectCandlestickPatternsAsync(List<CandlestickData> priceData)
        {
            var patterns = new List<DetectedPattern>();

            try
            {
                // Doji pattern
                var dojiPattern = DetectDoji(priceData);
                if (dojiPattern != null) patterns.Add(dojiPattern);

                // Hammer pattern
                var hammerPattern = DetectHammer(priceData);
                if (hammerPattern != null) patterns.Add(hammerPattern);

                // Engulfing pattern
                var engulfingPattern = DetectEngulfing(priceData);
                if (engulfingPattern != null) patterns.Add(engulfingPattern);

                // Morning Star / Evening Star
                var starPattern = DetectStar(priceData);
                if (starPattern != null) patterns.Add(starPattern);

                // Shooting Star
                var shootingStarPattern = DetectShootingStar(priceData);
                if (shootingStarPattern != null) patterns.Add(shootingStarPattern);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting candlestick patterns");
            }

            return patterns;
        }

        /// <summary>
        /// Detect trend patterns
        /// </summary>
        private async Task<List<DetectedPattern>> DetectTrendPatternsAsync(List<CandlestickData> priceData)
        {
            var patterns = new List<DetectedPattern>();

            try
            {
                // Head and Shoulders
                var headShouldersPattern = DetectHeadAndShoulders(priceData);
                if (headShouldersPattern != null) patterns.Add(headShouldersPattern);

                // Double Top/Bottom
                var doubleTopBottomPattern = DetectDoubleTopBottom(priceData);
                if (doubleTopBottomPattern != null) patterns.Add(doubleTopBottomPattern);

                // Triangle patterns
                var trianglePattern = DetectTriangle(priceData);
                if (trianglePattern != null) patterns.Add(trianglePattern);

                // Channel patterns
                var channelPattern = DetectChannel(priceData);
                if (channelPattern != null) patterns.Add(channelPattern);

                // Cup and Handle
                var cupHandlePattern = DetectCupAndHandle(priceData);
                if (cupHandlePattern != null) patterns.Add(cupHandlePattern);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting trend patterns");
            }

            return patterns;
        }

        /// <summary>
        /// Detect support and resistance patterns
        /// </summary>
        private async Task<List<DetectedPattern>> DetectSupportResistancePatternsAsync(List<CandlestickData> priceData)
        {
            var patterns = new List<DetectedPattern>();

            try
            {
                // Key support levels
                var supportLevels = IdentifySupportLevels(priceData);
                foreach (var level in supportLevels)
                {
                    patterns.Add(new DetectedPattern
                    {
                        PatternName = "Support Level",
                        Confidence = level.Confidence,
                        Direction = "Bullish",
                        ExpectedMagnitude = level.Strength,
                        ExpectedDuration = TimeSpan.FromHours(4),
                        PatternMetrics = new Dictionary<string, decimal>
                        {
                            ["SupportLevel"] = level.Price,
                            ["Touches"] = level.Touches,
                            ["Strength"] = level.Strength
                        }
                    });
                }

                // Key resistance levels
                var resistanceLevels = IdentifyResistanceLevels(priceData);
                foreach (var level in resistanceLevels)
                {
                    patterns.Add(new DetectedPattern
                    {
                        PatternName = "Resistance Level",
                        Confidence = level.Confidence,
                        Direction = "Bearish",
                        ExpectedMagnitude = level.Strength,
                        ExpectedDuration = TimeSpan.FromHours(4),
                        PatternMetrics = new Dictionary<string, decimal>
                        {
                            ["ResistanceLevel"] = level.Price,
                            ["Touches"] = level.Touches,
                            ["Strength"] = level.Strength
                        }
                    });
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting support/resistance patterns");
            }

            return patterns;
        }

        /// <summary>
        /// Detect volume-based patterns
        /// </summary>
        private async Task<List<DetectedPattern>> DetectVolumeBasedPatternsAsync(List<CandlestickData> priceData)
        {
            var patterns = new List<DetectedPattern>();

            try
            {
                // Volume breakout
                var volumeBreakout = DetectVolumeBreakout(priceData);
                if (volumeBreakout != null) patterns.Add(volumeBreakout);

                // Volume divergence
                var volumeDivergence = DetectVolumeDivergence(priceData);
                if (volumeDivergence != null) patterns.Add(volumeDivergence);

                // On-Balance Volume pattern
                var obvPattern = DetectOBVPattern(priceData);
                if (obvPattern != null) patterns.Add(obvPattern);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting volume patterns");
            }

            return patterns;
        }

        /// <summary>
        /// Detect volatility patterns
        /// </summary>
        private async Task<List<DetectedPattern>> DetectVolatilityPatternsAsync(List<CandlestickData> priceData)
        {
            var patterns = new List<DetectedPattern>();

            try
            {
                // Volatility squeeze
                var volatilitySqueezePattern = DetectVolatilitySqueeze(priceData);
                if (volatilitySqueezePattern != null) patterns.Add(volatilitySqueezePattern);

                // Volatility expansion
                var volatilityExpansionPattern = DetectVolatilityExpansion(priceData);
                if (volatilityExpansionPattern != null) patterns.Add(volatilityExpansionPattern);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting volatility patterns");
            }

            return patterns;
        }

        // Pattern detection implementations

        private DetectedPattern? DetectDoji(List<CandlestickData> priceData)
        {
            var lastCandle = priceData.LastOrDefault();
            if (lastCandle == null) return null;

            var bodySize = Math.Abs(lastCandle.Close - lastCandle.Open);
            var totalRange = lastCandle.High - lastCandle.Low;
            var bodyRatio = totalRange > 0 ? bodySize / totalRange : 0;

            if (bodyRatio < 0.1m && totalRange > 0) // Small body relative to range
            {
                return new DetectedPattern
                {
                    PatternName = "Doji",
                    Confidence = 80 - (bodyRatio * 100), // Higher confidence for smaller body
                    Direction = "Neutral",
                    ExpectedMagnitude = totalRange * 0.5m,
                    ExpectedDuration = TimeSpan.FromHours(2),
                    PatternMetrics = new Dictionary<string, decimal>
                    {
                        ["BodyRatio"] = bodyRatio,
                        ["TotalRange"] = totalRange,
                        ["UpperShadow"] = lastCandle.High - Math.Max(lastCandle.Open, lastCandle.Close),
                        ["LowerShadow"] = Math.Min(lastCandle.Open, lastCandle.Close) - lastCandle.Low
                    }
                };
            }

            return null;
        }

        private DetectedPattern? DetectHammer(List<CandlestickData> priceData)
        {
            var lastCandle = priceData.LastOrDefault();
            if (lastCandle == null) return null;

            var bodySize = Math.Abs(lastCandle.Close - lastCandle.Open);
            var lowerShadow = Math.Min(lastCandle.Open, lastCandle.Close) - lastCandle.Low;
            var upperShadow = lastCandle.High - Math.Max(lastCandle.Open, lastCandle.Close);

            // Hammer criteria: long lower shadow, small body, small upper shadow
            if (lowerShadow > bodySize * 2 && upperShadow < bodySize * 0.5m && bodySize > 0)
            {
                var confidence = Math.Min(90, 60 + (lowerShadow / bodySize) * 5);
                
                return new DetectedPattern
                {
                    PatternName = "Hammer",
                    Confidence = confidence,
                    Direction = "Bullish",
                    ExpectedMagnitude = lowerShadow * 0.7m,
                    ExpectedDuration = TimeSpan.FromHours(4),
                    PatternMetrics = new Dictionary<string, decimal>
                    {
                        ["LowerShadowRatio"] = lowerShadow / bodySize,
                        ["UpperShadowRatio"] = upperShadow / bodySize,
                        ["BodySize"] = bodySize
                    }
                };
            }

            return null;
        }

        private DetectedPattern? DetectEngulfing(List<CandlestickData> priceData)
        {
            if (priceData.Count < 2) return null;

            var current = priceData[priceData.Count - 1];
            var previous = priceData[priceData.Count - 2];

            var currentBody = Math.Abs(current.Close - current.Open);
            var previousBody = Math.Abs(previous.Close - previous.Open);

            // Bullish engulfing
            if (previous.Close < previous.Open && // Previous bearish
                current.Close > current.Open && // Current bullish
                current.Open < previous.Close && // Current opens below previous close
                current.Close > previous.Open && // Current closes above previous open
                currentBody > previousBody * 1.2m) // Current body is significantly larger
            {
                return new DetectedPattern
                {
                    PatternName = "Bullish Engulfing",
                    Confidence = 75,
                    Direction = "Bullish",
                    ExpectedMagnitude = currentBody * 1.5m,
                    ExpectedDuration = TimeSpan.FromHours(6),
                    PatternMetrics = new Dictionary<string, decimal>
                    {
                        ["BodySizeRatio"] = currentBody / previousBody,
                        ["Engulfment"] = (current.Close - previous.Open) / previousBody
                    }
                };
            }

            // Bearish engulfing
            if (previous.Close > previous.Open && // Previous bullish
                current.Close < current.Open && // Current bearish
                current.Open > previous.Close && // Current opens above previous close
                current.Close < previous.Open && // Current closes below previous open
                currentBody > previousBody * 1.2m) // Current body is significantly larger
            {
                return new DetectedPattern
                {
                    PatternName = "Bearish Engulfing",
                    Confidence = 75,
                    Direction = "Bearish",
                    ExpectedMagnitude = currentBody * 1.5m,
                    ExpectedDuration = TimeSpan.FromHours(6),
                    PatternMetrics = new Dictionary<string, decimal>
                    {
                        ["BodySizeRatio"] = currentBody / previousBody,
                        ["Engulfment"] = (previous.Open - current.Close) / previousBody
                    }
                };
            }

            return null;
        }

        private DetectedPattern? DetectStar(List<CandlestickData> priceData)
        {
            if (priceData.Count < 3) return null;

            var first = priceData[priceData.Count - 3];
            var star = priceData[priceData.Count - 2];
            var third = priceData[priceData.Count - 1];

            var starBody = Math.Abs(star.Close - star.Open);
            var firstBody = Math.Abs(first.Close - first.Open);
            var thirdBody = Math.Abs(third.Close - third.Open);

            // Morning Star (bullish)
            if (first.Close < first.Open && // First candle bearish
                starBody < firstBody * 0.5m && // Star has small body
                star.High < first.Close && // Gap down
                third.Close > third.Open && // Third candle bullish
                third.Close > (first.Open + first.Close) / 2) // Third candle closes above midpoint of first
            {
                return new DetectedPattern
                {
                    PatternName = "Morning Star",
                    Confidence = 80,
                    Direction = "Bullish",
                    ExpectedMagnitude = firstBody * 1.2m,
                    ExpectedDuration = TimeSpan.FromHours(8),
                    PatternMetrics = new Dictionary<string, decimal>
                    {
                        ["StarBodyRatio"] = starBody / firstBody,
                        ["GapSize"] = first.Close - star.High,
                        ["Penetration"] = (third.Close - (first.Open + first.Close) / 2) / firstBody
                    }
                };
            }

            // Evening Star (bearish)
            if (first.Close > first.Open && // First candle bullish
                starBody < firstBody * 0.5m && // Star has small body
                star.Low > first.Close && // Gap up
                third.Close < third.Open && // Third candle bearish
                third.Close < (first.Open + first.Close) / 2) // Third candle closes below midpoint of first
            {
                return new DetectedPattern
                {
                    PatternName = "Evening Star",
                    Confidence = 80,
                    Direction = "Bearish",
                    ExpectedMagnitude = firstBody * 1.2m,
                    ExpectedDuration = TimeSpan.FromHours(8),
                    PatternMetrics = new Dictionary<string, decimal>
                    {
                        ["StarBodyRatio"] = starBody / firstBody,
                        ["GapSize"] = star.Low - first.Close,
                        ["Penetration"] = ((first.Open + first.Close) / 2 - third.Close) / firstBody
                    }
                };
            }

            return null;
        }

        private DetectedPattern? DetectShootingStar(List<CandlestickData> priceData)
        {
            var lastCandle = priceData.LastOrDefault();
            if (lastCandle == null) return null;

            var bodySize = Math.Abs(lastCandle.Close - lastCandle.Open);
            var upperShadow = lastCandle.High - Math.Max(lastCandle.Open, lastCandle.Close);
            var lowerShadow = Math.Min(lastCandle.Open, lastCandle.Close) - lastCandle.Low;

            // Shooting star criteria: long upper shadow, small body, small lower shadow
            if (upperShadow > bodySize * 2 && lowerShadow < bodySize * 0.5m && bodySize > 0)
            {
                var confidence = Math.Min(90, 60 + (upperShadow / bodySize) * 5);
                
                return new DetectedPattern
                {
                    PatternName = "Shooting Star",
                    Confidence = confidence,
                    Direction = "Bearish",
                    ExpectedMagnitude = upperShadow * 0.7m,
                    ExpectedDuration = TimeSpan.FromHours(4),
                    PatternMetrics = new Dictionary<string, decimal>
                    {
                        ["UpperShadowRatio"] = upperShadow / bodySize,
                        ["LowerShadowRatio"] = lowerShadow / bodySize,
                        ["BodySize"] = bodySize
                    }
                };
            }

            return null;
        }

        private DetectedPattern? DetectHeadAndShoulders(List<CandlestickData> priceData)
        {
            if (priceData.Count < 20) return null;

            // Simplified head and shoulders detection
            var recent = priceData.TakeLast(20).ToList();
            var highs = recent.Select(c => c.High).ToList();
            var lows = recent.Select(c => c.Low).ToList();

            // Find potential peaks
            var peaks = new List<(int Index, decimal Price)>();
            for (int i = 2; i < highs.Count - 2; i++)
            {
                if (highs[i] > highs[i-1] && highs[i] > highs[i-2] && 
                    highs[i] > highs[i+1] && highs[i] > highs[i+2])
                {
                    peaks.Add((i, highs[i]));
                }
            }

            if (peaks.Count >= 3)
            {
                // Sort peaks by height
                var sortedPeaks = peaks.OrderByDescending(p => p.Price).ToList();
                var head = sortedPeaks[0];
                var leftShoulder = sortedPeaks.Where(p => p.Index < head.Index).FirstOrDefault();
                var rightShoulder = sortedPeaks.Where(p => p.Index > head.Index).FirstOrDefault();

                if (leftShoulder.Price > 0 && rightShoulder.Price > 0)
                {
                    var shoulderSymmetry = Math.Abs(leftShoulder.Price - rightShoulder.Price) / head.Price;
                    var confidence = Math.Max(60, 90 - shoulderSymmetry * 1000);

                    return new DetectedPattern
                    {
                        PatternName = "Head and Shoulders",
                        Confidence = confidence,
                        Direction = "Bearish",
                        ExpectedMagnitude = head.Price - Math.Min(leftShoulder.Price, rightShoulder.Price),
                        ExpectedDuration = TimeSpan.FromHours(12),
                        PatternMetrics = new Dictionary<string, decimal>
                        {
                            ["HeadPrice"] = head.Price,
                            ["LeftShoulderPrice"] = leftShoulder.Price,
                            ["RightShoulderPrice"] = rightShoulder.Price,
                            ["Symmetry"] = shoulderSymmetry
                        }
                    };
                }
            }

            return null;
        }

        // Helper methods for pattern detection

        private async Task<List<CandlestickData>> GetHistoricalPriceDataAsync(string symbol, TimeSpan period)
        {
            // This would integrate with your existing market data service
            // For now, return dummy data
            var data = new List<CandlestickData>();
            var random = new Random();
            var basePrice = 22500m;

            for (int i = 0; i < 30; i++)
            {
                var open = basePrice + (decimal)(random.NextDouble() - 0.5) * 100;
                var close = open + (decimal)(random.NextDouble() - 0.5) * 50;
                var high = Math.Max(open, close) + (decimal)random.NextDouble() * 25;
                var low = Math.Min(open, close) - (decimal)random.NextDouble() * 25;

                data.Add(new CandlestickData
                {
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = (long)(random.NextDouble() * 1000000),
                    Timestamp = DateTime.UtcNow.AddMinutes(-i * 15)
                });

                basePrice = close;
            }

            return data.OrderBy(d => d.Timestamp).ToList();
        }

        private List<SupportResistanceLevel> IdentifySupportLevels(List<CandlestickData> priceData)
        {
            var levels = new List<SupportResistanceLevel>();
            var lows = priceData.Select(c => c.Low).ToList();

            // Find potential support levels
            for (int i = 2; i < lows.Count - 2; i++)
            {
                if (lows[i] < lows[i-1] && lows[i] < lows[i-2] && 
                    lows[i] < lows[i+1] && lows[i] < lows[i+2])
                {
                    // Count how many times this level was touched
                    var touchCount = lows.Count(l => Math.Abs(l - lows[i]) < lows[i] * 0.01m);
                    
                    if (touchCount >= 2)
                    {
                        levels.Add(new SupportResistanceLevel
                        {
                            Price = lows[i],
                            Touches = touchCount,
                            Strength = Math.Min(100, touchCount * 20),
                            Confidence = Math.Min(90, 50 + touchCount * 10)
                        });
                    }
                }
            }

            return levels.OrderByDescending(l => l.Strength).Take(3).ToList();
        }

        private List<SupportResistanceLevel> IdentifyResistanceLevels(List<CandlestickData> priceData)
        {
            var levels = new List<SupportResistanceLevel>();
            var highs = priceData.Select(c => c.High).ToList();

            // Find potential resistance levels
            for (int i = 2; i < highs.Count - 2; i++)
            {
                if (highs[i] > highs[i-1] && highs[i] > highs[i-2] && 
                    highs[i] > highs[i+1] && highs[i] > highs[i+2])
                {
                    // Count how many times this level was touched
                    var touchCount = highs.Count(h => Math.Abs(h - highs[i]) < highs[i] * 0.01m);
                    
                    if (touchCount >= 2)
                    {
                        levels.Add(new SupportResistanceLevel
                        {
                            Price = highs[i],
                            Touches = touchCount,
                            Strength = Math.Min(100, touchCount * 20),
                            Confidence = Math.Min(90, 50 + touchCount * 10)
                        });
                    }
                }
            }

            return levels.OrderByDescending(l => l.Strength).Take(3).ToList();
        }

        // Placeholder implementations for other pattern detection methods
        private DetectedPattern? DetectDoubleTopBottom(List<CandlestickData> priceData) => null;
        private DetectedPattern? DetectTriangle(List<CandlestickData> priceData) => null;
        private DetectedPattern? DetectChannel(List<CandlestickData> priceData) => null;
        private DetectedPattern? DetectCupAndHandle(List<CandlestickData> priceData) => null;
        private DetectedPattern? DetectVolumeBreakout(List<CandlestickData> priceData) => null;
        private DetectedPattern? DetectVolumeDivergence(List<CandlestickData> priceData) => null;
        private DetectedPattern? DetectOBVPattern(List<CandlestickData> priceData) => null;
        private DetectedPattern? DetectVolatilitySqueeze(List<CandlestickData> priceData) => null;
        private DetectedPattern? DetectVolatilityExpansion(List<CandlestickData> priceData) => null;
    }

    /// <summary>
    /// Candlestick data structure
    /// </summary>
    public class CandlestickData
    {
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public long Volume { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Support/Resistance level
    /// </summary>
    public class SupportResistanceLevel
    {
        public decimal Price { get; set; }
        public int Touches { get; set; }
        public decimal Strength { get; set; }
        public decimal Confidence { get; set; }
    }
}