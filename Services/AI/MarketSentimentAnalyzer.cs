using KiteConnectApi.Models.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Net.Http;

namespace KiteConnectApi.Services.AI
{
    /// <summary>
    /// Market sentiment analysis service that combines multiple data sources
    /// to provide comprehensive market sentiment scoring
    /// </summary>
    public class MarketSentimentAnalyzer
    {
        private readonly ILogger<MarketSentimentAnalyzer> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, decimal> _sentimentWeights;

        public MarketSentimentAnalyzer(
            ILogger<MarketSentimentAnalyzer> logger,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClient = httpClient;
            
            // Configure sentiment source weights
            _sentimentWeights = new Dictionary<string, decimal>
            {
                ["NewsHeadlines"] = 0.25m,
                ["SocialMedia"] = 0.20m,
                ["MarketData"] = 0.20m,
                ["EconomicIndicators"] = 0.15m,
                ["VIXSentiment"] = 0.10m,
                ["CorporateActions"] = 0.10m
            };
        }

        /// <summary>
        /// Analyze market sentiment for trading signal validation
        /// </summary>
        public async Task<MarketSentimentResult> AnalyzeSentimentAsync(string symbol = "NIFTY")
        {
            try
            {
                _logger.LogInformation("Starting sentiment analysis for {Symbol}", symbol);

                var sentimentResult = new MarketSentimentResult
                {
                    Symbol = symbol,
                    AnalysisTimestamp = DateTime.UtcNow,
                    SentimentSources = new Dictionary<string, SentimentSourceResult>()
                };

                // Analyze different sentiment sources in parallel
                var tasks = new List<Task<(string Source, SentimentSourceResult Result)>>
                {
                    AnalyzeNewsHeadlinesAsync(symbol),
                    AnalyzeSocialMediaAsync(symbol),
                    AnalyzeMarketDataSentimentAsync(symbol),
                    AnalyzeEconomicIndicatorsAsync(symbol),
                    AnalyzeVIXSentimentAsync(),
                    AnalyzeCorporateActionsAsync(symbol)
                };

                var results = await Task.WhenAll(tasks);

                // Compile results
                foreach (var (source, result) in results)
                {
                    sentimentResult.SentimentSources[source] = result;
                }

                // Calculate composite sentiment score
                sentimentResult.CompositeSentimentScore = CalculateCompositeSentiment(sentimentResult.SentimentSources);
                
                // Determine sentiment direction
                sentimentResult.SentimentDirection = DetermineSentimentDirection(sentimentResult.CompositeSentimentScore);
                
                // Calculate confidence level
                sentimentResult.ConfidenceLevel = CalculateConfidenceLevel(sentimentResult.SentimentSources);

                // Generate insights
                sentimentResult.KeyInsights = GenerateKeyInsights(sentimentResult);

                _logger.LogInformation("Sentiment analysis completed for {Symbol}. Score: {Score}, Direction: {Direction}", 
                    symbol, sentimentResult.CompositeSentimentScore, sentimentResult.SentimentDirection);

                return sentimentResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing sentiment for {Symbol}", symbol);
                return new MarketSentimentResult
                {
                    Symbol = symbol,
                    AnalysisTimestamp = DateTime.UtcNow,
                    CompositeSentimentScore = 0,
                    SentimentDirection = "Neutral",
                    ConfidenceLevel = 0,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Analyze news headlines sentiment
        /// </summary>
        private async Task<(string Source, SentimentSourceResult Result)> AnalyzeNewsHeadlinesAsync(string symbol)
        {
            try
            {
                // Fetch recent news headlines
                var headlines = await FetchNewsHeadlinesAsync(symbol);
                
                var sentimentScores = new List<decimal>();
                var analyzedHeadlines = new List<SentimentItem>();

                foreach (var headline in headlines)
                {
                    var sentiment = AnalyzeTextSentiment(headline.Title + " " + headline.Summary);
                    sentimentScores.Add(sentiment.Score);
                    
                    analyzedHeadlines.Add(new SentimentItem
                    {
                        Text = headline.Title,
                        Score = sentiment.Score,
                        Confidence = sentiment.Confidence,
                        Keywords = sentiment.Keywords,
                        Timestamp = headline.PublishedAt
                    });
                }

                var avgScore = sentimentScores.Any() ? sentimentScores.Average() : 0;
                var confidence = sentimentScores.Any() ? 
                    Math.Max(0, 100 - (sentimentScores.Select(s => Math.Abs(s - avgScore)).Average() * 100)) : 0;

                return ("NewsHeadlines", new SentimentSourceResult
                {
                    Score = avgScore,
                    Confidence = confidence,
                    ItemCount = headlines.Count,
                    Items = analyzedHeadlines,
                    LastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing news headlines sentiment");
                return ("NewsHeadlines", new SentimentSourceResult { Score = 0, Confidence = 0 });
            }
        }

        /// <summary>
        /// Analyze social media sentiment
        /// </summary>
        private async Task<(string Source, SentimentSourceResult Result)> AnalyzeSocialMediaAsync(string symbol)
        {
            try
            {
                // Fetch social media posts (Twitter, Reddit, etc.)
                var socialPosts = await FetchSocialMediaPostsAsync(symbol);
                
                var sentimentScores = new List<decimal>();
                var analyzedPosts = new List<SentimentItem>();

                foreach (var post in socialPosts)
                {
                    var sentiment = AnalyzeTextSentiment(post.Content);
                    
                    // Weight by engagement (likes, shares, comments)
                    var weightedScore = sentiment.Score * (1 + post.Engagement / 1000m);
                    sentimentScores.Add(weightedScore);
                    
                    analyzedPosts.Add(new SentimentItem
                    {
                        Text = post.Content,
                        Score = weightedScore,
                        Confidence = sentiment.Confidence,
                        Keywords = sentiment.Keywords,
                        Timestamp = post.CreatedAt,
                        Metadata = new Dictionary<string, object>
                        {
                            ["Platform"] = post.Platform,
                            ["Engagement"] = post.Engagement,
                            ["Author"] = post.Author
                        }
                    });
                }

                var avgScore = sentimentScores.Any() ? sentimentScores.Average() : 0;
                var confidence = CalculateConfidenceFromVariance(sentimentScores);

                return ("SocialMedia", new SentimentSourceResult
                {
                    Score = avgScore,
                    Confidence = confidence,
                    ItemCount = socialPosts.Count,
                    Items = analyzedPosts,
                    LastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing social media sentiment");
                return ("SocialMedia", new SentimentSourceResult { Score = 0, Confidence = 0 });
            }
        }

        /// <summary>
        /// Analyze market data sentiment (price action, volume, etc.)
        /// </summary>
        private async Task<(string Source, SentimentSourceResult Result)> AnalyzeMarketDataSentimentAsync(string symbol)
        {
            try
            {
                // Fetch recent market data
                var marketData = await FetchMarketDataAsync(symbol);
                
                var sentimentScore = 0m;
                var factors = new List<SentimentItem>();

                // Price momentum sentiment
                var priceMomentum = CalculatePriceMomentumSentiment(marketData);
                sentimentScore += priceMomentum * 0.4m;
                factors.Add(new SentimentItem
                {
                    Text = "Price Momentum",
                    Score = priceMomentum,
                    Confidence = 80,
                    Keywords = new[] { "momentum", "price", "trend" }
                });

                // Volume sentiment
                var volumeSentiment = CalculateVolumeSentiment(marketData);
                sentimentScore += volumeSentiment * 0.3m;
                factors.Add(new SentimentItem
                {
                    Text = "Volume Analysis",
                    Score = volumeSentiment,
                    Confidence = 70,
                    Keywords = new[] { "volume", "liquidity", "participation" }
                });

                // Volatility sentiment
                var volatilitySentiment = CalculateVolatilitySentiment(marketData);
                sentimentScore += volatilitySentiment * 0.3m;
                factors.Add(new SentimentItem
                {
                    Text = "Volatility Analysis",
                    Score = volatilitySentiment,
                    Confidence = 75,
                    Keywords = new[] { "volatility", "risk", "uncertainty" }
                });

                return ("MarketData", new SentimentSourceResult
                {
                    Score = sentimentScore,
                    Confidence = 75,
                    ItemCount = factors.Count,
                    Items = factors,
                    LastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing market data sentiment");
                return ("MarketData", new SentimentSourceResult { Score = 0, Confidence = 0 });
            }
        }

        /// <summary>
        /// Analyze economic indicators sentiment
        /// </summary>
        private async Task<(string Source, SentimentSourceResult Result)> AnalyzeEconomicIndicatorsAsync(string symbol)
        {
            try
            {
                var indicators = await FetchEconomicIndicatorsAsync();
                
                var sentimentScore = 0m;
                var factors = new List<SentimentItem>();

                // GDP sentiment
                if (indicators.ContainsKey("GDP"))
                {
                    var gdpSentiment = CalculateGDPSentiment(indicators["GDP"]);
                    sentimentScore += gdpSentiment * 0.3m;
                    factors.Add(new SentimentItem
                    {
                        Text = "GDP Growth",
                        Score = gdpSentiment,
                        Confidence = 90,
                        Keywords = new[] { "gdp", "growth", "economy" }
                    });
                }

                // Inflation sentiment
                if (indicators.ContainsKey("Inflation"))
                {
                    var inflationSentiment = CalculateInflationSentiment(indicators["Inflation"]);
                    sentimentScore += inflationSentiment * 0.3m;
                    factors.Add(new SentimentItem
                    {
                        Text = "Inflation Rate",
                        Score = inflationSentiment,
                        Confidence = 85,
                        Keywords = new[] { "inflation", "cpi", "prices" }
                    });
                }

                // Interest rates sentiment
                if (indicators.ContainsKey("InterestRates"))
                {
                    var ratesSentiment = CalculateInterestRatesSentiment(indicators["InterestRates"]);
                    sentimentScore += ratesSentiment * 0.4m;
                    factors.Add(new SentimentItem
                    {
                        Text = "Interest Rates",
                        Score = ratesSentiment,
                        Confidence = 95,
                        Keywords = new[] { "interest", "rates", "monetary", "policy" }
                    });
                }

                return ("EconomicIndicators", new SentimentSourceResult
                {
                    Score = sentimentScore,
                    Confidence = 85,
                    ItemCount = factors.Count,
                    Items = factors,
                    LastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing economic indicators sentiment");
                return ("EconomicIndicators", new SentimentSourceResult { Score = 0, Confidence = 0 });
            }
        }

        /// <summary>
        /// Analyze VIX sentiment
        /// </summary>
        private async Task<(string Source, SentimentSourceResult Result)> AnalyzeVIXSentimentAsync()
        {
            try
            {
                var vixData = await FetchVIXDataAsync();
                
                var vixLevel = vixData.CurrentLevel;
                var vixChange = vixData.Change;
                
                // VIX sentiment calculation
                var vixSentiment = CalculateVIXSentiment(vixLevel, vixChange);
                
                var factors = new List<SentimentItem>
                {
                    new SentimentItem
                    {
                        Text = $"VIX Level: {vixLevel:F2}",
                        Score = vixSentiment,
                        Confidence = 80,
                        Keywords = new[] { "vix", "volatility", "fear", "uncertainty" },
                        Metadata = new Dictionary<string, object>
                        {
                            ["VIXLevel"] = vixLevel,
                            ["VIXChange"] = vixChange,
                            ["VIXRegime"] = GetVIXRegime(vixLevel)
                        }
                    }
                };

                return ("VIXSentiment", new SentimentSourceResult
                {
                    Score = vixSentiment,
                    Confidence = 80,
                    ItemCount = 1,
                    Items = factors,
                    LastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing VIX sentiment");
                return ("VIXSentiment", new SentimentSourceResult { Score = 0, Confidence = 0 });
            }
        }

        /// <summary>
        /// Analyze corporate actions sentiment
        /// </summary>
        private async Task<(string Source, SentimentSourceResult Result)> AnalyzeCorporateActionsAsync(string symbol)
        {
            try
            {
                var corporateActions = await FetchCorporateActionsAsync(symbol);
                
                var sentimentScore = 0m;
                var factors = new List<SentimentItem>();

                foreach (var action in corporateActions)
                {
                    var actionSentiment = CalculateCorporateActionSentiment(action);
                    sentimentScore += actionSentiment;
                    
                    factors.Add(new SentimentItem
                    {
                        Text = $"{action.Type}: {action.Description}",
                        Score = actionSentiment,
                        Confidence = 70,
                        Keywords = new[] { action.Type.ToLower(), "corporate", "action" },
                        Timestamp = action.Date
                    });
                }

                sentimentScore = corporateActions.Any() ? sentimentScore / corporateActions.Count : 0;

                return ("CorporateActions", new SentimentSourceResult
                {
                    Score = sentimentScore,
                    Confidence = 70,
                    ItemCount = corporateActions.Count,
                    Items = factors,
                    LastUpdated = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing corporate actions sentiment");
                return ("CorporateActions", new SentimentSourceResult { Score = 0, Confidence = 0 });
            }
        }

        /// <summary>
        /// Calculate composite sentiment from multiple sources
        /// </summary>
        private decimal CalculateCompositeSentiment(Dictionary<string, SentimentSourceResult> sources)
        {
            var weightedSum = 0m;
            var totalWeight = 0m;

            foreach (var source in sources)
            {
                if (_sentimentWeights.TryGetValue(source.Key, out var weight))
                {
                    // Apply confidence weighting
                    var confidenceWeight = source.Value.Confidence / 100m;
                    var adjustedWeight = weight * confidenceWeight;
                    
                    weightedSum += source.Value.Score * adjustedWeight;
                    totalWeight += adjustedWeight;
                }
            }

            return totalWeight > 0 ? weightedSum / totalWeight : 0;
        }

        /// <summary>
        /// Analyze text sentiment using NLP techniques
        /// </summary>
        private (decimal Score, decimal Confidence, string[] Keywords) AnalyzeTextSentiment(string text)
        {
            // Simplified sentiment analysis - in production, use ML models or APIs
            var positiveWords = new[] { "bullish", "positive", "growth", "strong", "buy", "up", "rise", "gain", "profit", "good", "excellent", "optimistic" };
            var negativeWords = new[] { "bearish", "negative", "decline", "weak", "sell", "down", "fall", "loss", "bad", "poor", "pessimistic" };
            
            var words = text.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var positiveCount = words.Count(w => positiveWords.Contains(w));
            var negativeCount = words.Count(w => negativeWords.Contains(w));
            
            var totalSentimentWords = positiveCount + negativeCount;
            var score = totalSentimentWords == 0 ? 0 : (positiveCount - negativeCount) * 100m / totalSentimentWords;
            
            var confidence = totalSentimentWords == 0 ? 30 : Math.Min(90, 40 + totalSentimentWords * 10);
            
            var keywords = words.Where(w => positiveWords.Contains(w) || negativeWords.Contains(w)).Distinct().ToArray();
            
            return (score, confidence, keywords);
        }

        // Helper methods and data fetching (simplified implementations)
        private async Task<List<NewsHeadline>> FetchNewsHeadlinesAsync(string symbol)
        {
            // Simplified - in production, integrate with news APIs
            return new List<NewsHeadline>
            {
                new NewsHeadline
                {
                    Title = "Market shows strong bullish sentiment",
                    Summary = "Positive economic indicators drive market optimism",
                    PublishedAt = DateTime.UtcNow.AddHours(-2)
                },
                new NewsHeadline
                {
                    Title = "Technology sector leads gains",
                    Summary = "Tech stocks show strong performance",
                    PublishedAt = DateTime.UtcNow.AddHours(-4)
                }
            };
        }

        private async Task<List<SocialMediaPost>> FetchSocialMediaPostsAsync(string symbol)
        {
            // Simplified - in production, integrate with social media APIs
            return new List<SocialMediaPost>
            {
                new SocialMediaPost
                {
                    Content = "Market looking bullish today!",
                    Platform = "Twitter",
                    Engagement = 150,
                    Author = "TraderX",
                    CreatedAt = DateTime.UtcNow.AddMinutes(-30)
                }
            };
        }

        private async Task<MarketDataPoint[]> FetchMarketDataAsync(string symbol)
        {
            // Simplified - in production, use real market data
            return new MarketDataPoint[0];
        }

        private async Task<Dictionary<string, decimal>> FetchEconomicIndicatorsAsync()
        {
            // Simplified - in production, integrate with economic data APIs
            return new Dictionary<string, decimal>
            {
                ["GDP"] = 6.5m,
                ["Inflation"] = 4.2m,
                ["InterestRates"] = 6.5m
            };
        }

        private async Task<VIXData> FetchVIXDataAsync()
        {
            // Simplified - in production, fetch real VIX data
            return new VIXData
            {
                CurrentLevel = 15.5m,
                Change = -0.8m
            };
        }

        private async Task<List<CorporateAction>> FetchCorporateActionsAsync(string symbol)
        {
            // Simplified - in production, integrate with corporate actions APIs
            return new List<CorporateAction>();
        }

        // Sentiment calculation methods
        private decimal CalculatePriceMomentumSentiment(MarketDataPoint[] data) => 25m; // Simplified
        private decimal CalculateVolumeSentiment(MarketDataPoint[] data) => 15m; // Simplified
        private decimal CalculateVolatilitySentiment(MarketDataPoint[] data) => -10m; // Simplified
        private decimal CalculateGDPSentiment(decimal gdpRate) => gdpRate > 6 ? 20 : gdpRate > 4 ? 10 : -10;
        private decimal CalculateInflationSentiment(decimal inflationRate) => inflationRate < 4 ? 10 : inflationRate < 6 ? 0 : -15;
        private decimal CalculateInterestRatesSentiment(decimal rate) => rate < 7 ? 5 : rate < 8 ? 0 : -10;
        private decimal CalculateVIXSentiment(decimal vixLevel, decimal vixChange)
        {
            var levelSentiment = vixLevel < 15 ? 20 : vixLevel < 20 ? 10 : vixLevel < 25 ? 0 : -15;
            var changeSentiment = vixChange < 0 ? 10 : vixChange > 0 ? -10 : 0;
            return (levelSentiment + changeSentiment) / 2;
        }
        private decimal CalculateCorporateActionSentiment(CorporateAction action) => 0; // Simplified

        private string DetermineSentimentDirection(decimal score)
        {
            return score switch
            {
                > 20 => "Strongly Bullish",
                > 10 => "Bullish",
                > -10 => "Neutral",
                > -20 => "Bearish",
                _ => "Strongly Bearish"
            };
        }

        private decimal CalculateConfidenceLevel(Dictionary<string, SentimentSourceResult> sources)
        {
            var avgConfidence = sources.Values.Average(s => s.Confidence);
            var scoreVariance = sources.Values.Select(s => Math.Abs(s.Score)).Average();
            return Math.Max(0, avgConfidence - scoreVariance);
        }

        private decimal CalculateConfidenceFromVariance(List<decimal> scores)
        {
            if (!scores.Any()) return 0;
            
            var mean = scores.Average();
            var variance = scores.Select(s => Math.Pow((double)(s - mean), 2)).Average();
            var stdDev = Math.Sqrt(variance);
            
            return Math.Max(0, 100 - (decimal)stdDev);
        }

        private string GetVIXRegime(decimal vixLevel)
        {
            return vixLevel switch
            {
                < 12 => "Complacency",
                < 20 => "Normal",
                < 30 => "Elevated",
                _ => "Panic"
            };
        }

        private List<string> GenerateKeyInsights(MarketSentimentResult result)
        {
            var insights = new List<string>();
            
            // Add insights based on sentiment score
            if (result.CompositeSentimentScore > 20)
                insights.Add("Strong bullish sentiment across multiple indicators");
            else if (result.CompositeSentimentScore < -20)
                insights.Add("Strong bearish sentiment across multiple indicators");
            
            // Add insights based on source analysis
            var bestSource = result.SentimentSources.OrderByDescending(s => s.Value.Score).FirstOrDefault();
            if (bestSource.Key != null)
                insights.Add($"Strongest positive sentiment from {bestSource.Key}");
            
            return insights;
        }
    }

    // Supporting data structures
    public class MarketSentimentResult
    {
        public string Symbol { get; set; } = string.Empty;
        public DateTime AnalysisTimestamp { get; set; }
        public decimal CompositeSentimentScore { get; set; }
        public string SentimentDirection { get; set; } = string.Empty;
        public decimal ConfidenceLevel { get; set; }
        public Dictionary<string, SentimentSourceResult> SentimentSources { get; set; } = new();
        public List<string> KeyInsights { get; set; } = new();
        public string? ErrorMessage { get; set; }
    }

    public class SentimentSourceResult
    {
        public decimal Score { get; set; }
        public decimal Confidence { get; set; }
        public int ItemCount { get; set; }
        public List<SentimentItem> Items { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class SentimentItem
    {
        public string Text { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public decimal Confidence { get; set; }
        public string[] Keywords { get; set; } = Array.Empty<string>();
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    public class NewsHeadline
    {
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
    }

    public class SocialMediaPost
    {
        public string Content { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public int Engagement { get; set; }
        public string Author { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class MarketDataPoint
    {
        public decimal Price { get; set; }
        public long Volume { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class VIXData
    {
        public decimal CurrentLevel { get; set; }
        public decimal Change { get; set; }
    }

    public class CorporateAction
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
    }
}