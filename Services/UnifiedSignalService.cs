using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace KiteConnectApi.Services
{
    public class UnifiedSignalService
    {
        private readonly ApplicationDbContext _context;
        private readonly TradingViewIndicatorService _apiIndicatorService;
        private readonly OptionsTradeService _optionsTradeService;
        private readonly ILogger<UnifiedSignalService> _logger;

        public UnifiedSignalService(
            ApplicationDbContext context,
            TradingViewIndicatorService apiIndicatorService,
            OptionsTradeService optionsTradeService,
            ILogger<UnifiedSignalService> logger)
        {
            _context = context;
            _apiIndicatorService = apiIndicatorService;
            _optionsTradeService = optionsTradeService;
            _logger = logger;
        }

        public async Task<List<UnifiedSignal>> GetSignalsAsync(SignalSource source = SignalSource.Auto)
        {
            try
            {
                switch (source)
                {
                    case SignalSource.API:
                        return await GetApiSignalsAsync();
                    case SignalSource.TradingView:
                        return await GetTradingViewSignalsAsync();
                    case SignalSource.Auto:
                        return await GetAutoSignalsAsync();
                    default:
                        throw new ArgumentException($"Unknown signal source: {source}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting signals from source {Source}", source);
                return new List<UnifiedSignal>();
            }
        }

        private async Task<List<UnifiedSignal>> GetApiSignalsAsync()
        {
            var signals = await _apiIndicatorService.ProcessWeeklyLevelsAndBiasAsync("NIFTY");
            
            return signals.Select(s => new UnifiedSignal
            {
                SignalId = s.SignalId,
                SignalName = s.SignalName,
                Source = SignalSource.API,
                Timestamp = s.Timestamp,
                Strike = (int)s.StrikePrice,
                OptionType = s.OptionType,
                Action = s.Action,
                Direction = s.Direction,
                StopLossPrice = s.StopLossPrice,
                Confidence = s.Confidence,
                Description = s.Description,
                IsActive = true
            }).ToList();
        }

        private async Task<List<UnifiedSignal>> GetTradingViewSignalsAsync()
        {
            var recentAlerts = await _context.ManualTradingViewAlerts
                .Where(a => a.ReceivedTime >= DateTime.Now.AddHours(-1)) // Recent alerts
                .OrderByDescending(a => a.ReceivedTime)
                .Take(10)
                .ToListAsync();

            return recentAlerts.Select(a => new UnifiedSignal
            {
                SignalId = a.Signal ?? "Unknown",
                SignalName = GetSignalName(a.Signal),
                Source = SignalSource.TradingView,
                Timestamp = a.ReceivedTime,
                Strike = a.Strike,
                OptionType = a.Type ?? "CE",
                Action = a.Action ?? "Entry",
                Direction = DetermineDirection(a.Type, a.Action),
                StopLossPrice = CalculateStopLoss(a.Strike, a.Type),
                Confidence = 1.0m, // TradingView signals are assumed to be 100% confident
                Description = GetSignalDescription(a.Signal),
                IsActive = true,
                TradingViewAlertId = a.Id
            }).ToList();
        }

        private async Task<List<UnifiedSignal>> GetAutoSignalsAsync()
        {
            // Get signals from both sources
            var apiSignals = await GetApiSignalsAsync();
            var tvSignals = await GetTradingViewSignalsAsync();

            // Combine and deduplicate
            var combinedSignals = new List<UnifiedSignal>();
            
            // Add API signals
            combinedSignals.AddRange(apiSignals);
            
            // Add TradingView signals that don't conflict with API signals
            foreach (var tvSignal in tvSignals)
            {
                var conflictingApiSignal = apiSignals.FirstOrDefault(api => 
                    api.SignalId == tvSignal.SignalId && 
                    Math.Abs((api.Timestamp - tvSignal.Timestamp).TotalMinutes) < 30);
                
                if (conflictingApiSignal == null)
                {
                    combinedSignals.Add(tvSignal);
                }
                else
                {
                    // Create a consensus signal
                    var consensusSignal = CreateConsensusSignal(conflictingApiSignal, tvSignal);
                    combinedSignals.Remove(conflictingApiSignal);
                    combinedSignals.Add(consensusSignal);
                }
            }

            return combinedSignals.OrderByDescending(s => s.Confidence).ThenByDescending(s => s.Timestamp).ToList();
        }

        public async Task<bool> ProcessUnifiedSignalAsync(UnifiedSignal signal)
        {
            try
            {
                // Convert to TradingView alert format for existing processing
                var tradingViewAlert = new TradingViewAlert
                {
                    Signal = signal.SignalId,
                    Strike = signal.Strike,
                    Type = signal.OptionType,
                    Action = signal.Action,
                    Index = "Nifty"
                };

                // Process using existing options trade service
                var success = await _optionsTradeService.ProcessOptionsAlertAsync(tradingViewAlert);
                
                if (success)
                {
                    // Log successful signal processing
                    await LogSignalProcessingAsync(signal, success);
                    
                    _logger.LogInformation("Successfully processed {Source} signal {SignalId} with confidence {Confidence}", 
                        signal.Source, signal.SignalId, signal.Confidence);
                }
                else
                {
                    _logger.LogWarning("Failed to process {Source} signal {SignalId}", signal.Source, signal.SignalId);
                }
                
                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing unified signal {SignalId}", signal.SignalId);
                return false;
            }
        }

        public async Task<SignalComparisonResult> CompareSignalSourcesAsync(DateTime fromDate, DateTime toDate)
        {
            try
            {
                _logger.LogInformation("Comparing signal sources from {FromDate} to {ToDate}", fromDate, toDate);
                
                var result = new SignalComparisonResult
                {
                    FromDate = fromDate,
                    ToDate = toDate
                };

                // Get historical API signals (simulated)
                var apiSignals = await GetHistoricalApiSignalsAsync(fromDate, toDate);
                
                // Get historical TradingView signals
                var tvSignals = await GetHistoricalTradingViewSignalsAsync(fromDate, toDate);

                result.ApiSignalCount = apiSignals.Count;
                result.TradingViewSignalCount = tvSignals.Count;
                
                // Compare signal generation frequency
                result.ApiSignalsByType = apiSignals.GroupBy(s => s.SignalId)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                result.TradingViewSignalsByType = tvSignals.GroupBy(s => s.SignalId)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Calculate accuracy metrics
                result.SignalMatchRate = CalculateSignalMatchRate(apiSignals, tvSignals);
                result.TimingAccuracy = CalculateTimingAccuracy(apiSignals, tvSignals);
                result.StrikeAccuracy = CalculateStrikeAccuracy(apiSignals, tvSignals);
                
                // Performance comparison
                result.ApiPerformance = await CalculateSignalPerformanceAsync(apiSignals);
                result.TradingViewPerformance = await CalculateSignalPerformanceAsync(tvSignals);
                
                result.Recommendation = GenerateRecommendation(result);
                
                _logger.LogInformation("Signal comparison completed. API signals: {ApiCount}, TradingView signals: {TvCount}, Match rate: {MatchRate}%", 
                    result.ApiSignalCount, result.TradingViewSignalCount, result.SignalMatchRate);
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing signal sources");
                throw;
            }
        }

        private UnifiedSignal CreateConsensusSignal(UnifiedSignal apiSignal, UnifiedSignal tvSignal)
        {
            return new UnifiedSignal
            {
                SignalId = apiSignal.SignalId,
                SignalName = apiSignal.SignalName,
                Source = SignalSource.Consensus,
                Timestamp = apiSignal.Timestamp,
                Strike = (int)Math.Round((apiSignal.Strike + tvSignal.Strike) / 2.0), // Average strike
                OptionType = apiSignal.OptionType,
                Action = apiSignal.Action,
                Direction = apiSignal.Direction,
                StopLossPrice = (apiSignal.StopLossPrice + tvSignal.StopLossPrice) / 2, // Average stop loss
                Confidence = (apiSignal.Confidence + tvSignal.Confidence) / 2 + 0.1m, // Bonus for consensus
                Description = $"Consensus: {apiSignal.Description}",
                IsActive = true,
                TradingViewAlertId = tvSignal.TradingViewAlertId
            };
        }

        private Task<List<UnifiedSignal>> GetHistoricalApiSignalsAsync(DateTime fromDate, DateTime toDate)
        {
            // This would be implemented to generate historical API signals
            // For now, return empty list as it requires historical data processing
            return Task.FromResult(new List<UnifiedSignal>());
        }

        private async Task<List<UnifiedSignal>> GetHistoricalTradingViewSignalsAsync(DateTime fromDate, DateTime toDate)
        {
            var alerts = await _context.ManualTradingViewAlerts
                .Where(a => a.ReceivedTime >= fromDate && a.ReceivedTime <= toDate)
                .OrderBy(a => a.ReceivedTime)
                .ToListAsync();

            return alerts.Select(a => new UnifiedSignal
            {
                SignalId = a.Signal ?? "Unknown",
                SignalName = GetSignalName(a.Signal),
                Source = SignalSource.TradingView,
                Timestamp = a.ReceivedTime,
                Strike = a.Strike,
                OptionType = a.Type ?? "CE",
                Action = a.Action ?? "Entry",
                Direction = DetermineDirection(a.Type, a.Action),
                StopLossPrice = CalculateStopLoss(a.Strike, a.Type),
                Confidence = 1.0m,
                Description = GetSignalDescription(a.Signal),
                IsActive = true,
                TradingViewAlertId = a.Id
            }).ToList();
        }

        private async Task LogSignalProcessingAsync(UnifiedSignal signal, bool success)
        {
            var logEntry = new SignalProcessingLog
            {
                SignalId = signal.SignalId,
                Source = signal.Source.ToString(),
                Timestamp = signal.Timestamp,
                Strike = signal.Strike,
                OptionType = signal.OptionType,
                Action = signal.Action,
                Direction = signal.Direction,
                Confidence = signal.Confidence,
                ProcessingSuccess = success,
                ProcessedAt = DateTime.UtcNow
            };

            // This would be saved to a logging table
            // For now, just log to console
            _logger.LogInformation("Signal processed: {SignalId}, Source: {Source}, Success: {Success}", 
                signal.SignalId, signal.Source, success);
        }

        private double CalculateSignalMatchRate(List<UnifiedSignal> apiSignals, List<UnifiedSignal> tvSignals)
        {
            if (!apiSignals.Any() || !tvSignals.Any()) return 0;

            var matches = 0;
            foreach (var apiSignal in apiSignals)
            {
                var matchingTvSignal = tvSignals.FirstOrDefault(tv => 
                    tv.SignalId == apiSignal.SignalId && 
                    Math.Abs((tv.Timestamp - apiSignal.Timestamp).TotalMinutes) < 60);
                
                if (matchingTvSignal != null) matches++;
            }

            return (double)matches / Math.Max(apiSignals.Count, tvSignals.Count) * 100;
        }

        private double CalculateTimingAccuracy(List<UnifiedSignal> apiSignals, List<UnifiedSignal> tvSignals)
        {
            var timingDifferences = new List<double>();
            
            foreach (var apiSignal in apiSignals)
            {
                var matchingTvSignal = tvSignals.FirstOrDefault(tv => 
                    tv.SignalId == apiSignal.SignalId && 
                    Math.Abs((tv.Timestamp - apiSignal.Timestamp).TotalMinutes) < 60);
                
                if (matchingTvSignal != null)
                {
                    timingDifferences.Add(Math.Abs((matchingTvSignal.Timestamp - apiSignal.Timestamp).TotalMinutes));
                }
            }

            return timingDifferences.Any() ? timingDifferences.Average() : 0;
        }

        private double CalculateStrikeAccuracy(List<UnifiedSignal> apiSignals, List<UnifiedSignal> tvSignals)
        {
            var strikeDifferences = new List<int>();
            
            foreach (var apiSignal in apiSignals)
            {
                var matchingTvSignal = tvSignals.FirstOrDefault(tv => 
                    tv.SignalId == apiSignal.SignalId && 
                    Math.Abs((tv.Timestamp - apiSignal.Timestamp).TotalMinutes) < 60);
                
                if (matchingTvSignal != null)
                {
                    strikeDifferences.Add(Math.Abs(matchingTvSignal.Strike - apiSignal.Strike));
                }
            }

            return strikeDifferences.Any() ? strikeDifferences.Average() : 0;
        }

        private Task<SignalPerformanceMetrics> CalculateSignalPerformanceAsync(List<UnifiedSignal> signals)
        {
            // This would calculate actual performance metrics
            // For now, return basic metrics
            var metrics = new SignalPerformanceMetrics
            {
                TotalSignals = signals.Count,
                AverageConfidence = signals.Any() ? signals.Average(s => s.Confidence) : 0,
                SignalsByType = signals.GroupBy(s => s.SignalId).ToDictionary(g => g.Key, g => g.Count())
            };
            return Task.FromResult(metrics);
        }

        private string GenerateRecommendation(SignalComparisonResult result)
        {
            if (result.SignalMatchRate > 80)
            {
                return "High correlation between API and TradingView signals. Consider using API for faster processing.";
            }
            else if (result.SignalMatchRate > 60)
            {
                return "Moderate correlation. Consider using Consensus mode for better accuracy.";
            }
            else
            {
                return "Low correlation. Review signal logic and consider manual validation.";
            }
        }

        private string GetSignalName(string signalId)
        {
            return signalId switch
            {
                "S1" => "Bear Trap",
                "S2" => "Support Hold (Bullish)",
                "S3" => "Resistance Hold (Bearish)",
                "S4" => "Bias Failure (Bullish)",
                "S5" => "Bias Failure (Bearish)",
                "S6" => "Weakness Confirmed",
                "S7" => "1H Breakout Confirmed",
                "S8" => "1H Breakdown Confirmed",
                _ => "Unknown Signal"
            };
        }

        private string GetSignalDescription(string signalId)
        {
            return signalId switch
            {
                "S1" => "Triggers after a fake breakdown if the second bar recovers to close above the first bar's low",
                "S2" => "Shows the bullish confirmation signal at support",
                "S3" => "Shows the bearish confirmation signal at resistance when the prior week closed near the zone",
                "S4" => "Shows the contrarian bullish signal after a gap up against a bearish bias",
                "S5" => "Shows the contrarian bearish signal after a gap down against a bullish bias",
                "S6" => "Triggers if bias is bearish, the first bar tests/fails at resistance, and the second bar confirms weakness",
                "S7" => "Shows a pure 1H breakout based on the S4 engine, but without bias or gap conditions",
                "S8" => "Shows a pure 1H breakdown, the bearish counterpart to S7",
                _ => "Unknown signal description"
            };
        }

        private int DetermineDirection(string optionType, string action)
        {
            if (action?.ToUpper() == "ENTRY")
            {
                return optionType?.ToUpper() == "PE" ? 1 : -1;
            }
            return 0;
        }

        private decimal CalculateStopLoss(int strike, string optionType)
        {
            return strike * (optionType?.ToUpper() == "PE" ? 0.95m : 1.05m);
        }
    }

    // Supporting enums and classes
    public enum SignalSource
    {
        API,
        TradingView,
        Consensus,
        Auto
    }

    public class UnifiedSignal
    {
        public string SignalId { get; set; } = string.Empty;
        public string SignalName { get; set; } = string.Empty;
        public SignalSource Source { get; set; }
        public DateTime Timestamp { get; set; }
        public int Strike { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int Direction { get; set; }
        public decimal StopLossPrice { get; set; }
        public decimal Confidence { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string? TradingViewAlertId { get; set; }
    }

    public class SignalComparisonResult
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int ApiSignalCount { get; set; }
        public int TradingViewSignalCount { get; set; }
        public Dictionary<string, int> ApiSignalsByType { get; set; } = new();
        public Dictionary<string, int> TradingViewSignalsByType { get; set; } = new();
        public double SignalMatchRate { get; set; }
        public double TimingAccuracy { get; set; }
        public double StrikeAccuracy { get; set; }
        public SignalPerformanceMetrics ApiPerformance { get; set; } = new();
        public SignalPerformanceMetrics TradingViewPerformance { get; set; } = new();
        public string Recommendation { get; set; } = string.Empty;
    }

    public class SignalPerformanceMetrics
    {
        public int TotalSignals { get; set; }
        public decimal AverageConfidence { get; set; }
        public Dictionary<string, int> SignalsByType { get; set; } = new();
    }

    public class SignalProcessingLog
    {
        public string SignalId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int Strike { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int Direction { get; set; }
        public decimal Confidence { get; set; }
        public bool ProcessingSuccess { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}