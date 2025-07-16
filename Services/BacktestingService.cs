using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KiteConnect;
using KiteConnectApi.Repositories;

namespace KiteConnectApi.Services
{
    public class BacktestingService
    {
        private readonly IKiteConnectService _kiteConnectService;
        private readonly StrategyService _strategyService;
        private readonly TechnicalAnalysisService _technicalAnalysisService;
        private readonly IStrategyConfigRepository _strategyConfigRepository; // Added

        public BacktestingService(
            IKiteConnectService kiteConnectService,
            StrategyService strategyService,
            TechnicalAnalysisService technicalAnalysisService,
            IStrategyConfigRepository strategyConfigRepository) // Added
        {
            _kiteConnectService = kiteConnectService;
            _strategyService = strategyService;
            _technicalAnalysisService = technicalAnalysisService;
            _strategyConfigRepository = strategyConfigRepository; // Added
        }

        public async Task<BacktestResultDto> RunBacktest(string strategyId)
        {
            // 1. Retrieve StrategyConfig based on strategyId
            var strategyConfig = await _strategyConfigRepository.GetStrategyConfigByIdAsync(strategyId);
            if (strategyConfig == null)
            {
                throw new ArgumentException($"Strategy with ID {strategyId} not found.");
            }

            // 2. Extract basic backtesting parameters from strategyConfig.Parameters
            //    Note: In a real scenario, these would be part of a more structured DTO
            //    or directly on StrategyConfig if they are core backtest parameters.
            string symbol = strategyConfig.Parameters.GetValueOrDefault("symbol", "NIFTY"); // Default if not found
            DateTime fromDate = DateTime.Parse(strategyConfig.Parameters.GetValueOrDefault("activeFrom", DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd")));
            DateTime toDate = DateTime.Parse(strategyConfig.Parameters.GetValueOrDefault("activeTo", DateTime.Now.ToString("yyyy-MM-dd")));
            string interval = strategyConfig.Parameters.GetValueOrDefault("interval", "day");

            // 3. Use extracted parameters to get historical data
            var historicalData = await _kiteConnectService.GetHistoricalDataAsync(symbol, fromDate, toDate, interval);
            var simulatedHistoricalData = historicalData.Select(h => new SimulatedHistoricalData
            {
                TimeStamp = h.TimeStamp,
                Open = h.Open,
                High = h.High,
                Low = h.Low,
                Close = h.Close,
                Volume = h.Volume
            }).ToList();

            var result = new BacktestResultDto
            {
                Symbol = symbol,
                FromDate = fromDate,
                ToDate = toDate,
                Interval = interval
            };

            SimulatedTrade? currentTrade = null;
            decimal totalProfitLoss = 0;
            int winningTrades = 0;
            int losingTrades = 0;
            decimal maxDrawdown = 0;
            decimal peakEquity = 0;
            decimal currentEquity = 0;

            // --- Placeholder for advanced backtesting logic based on StrategyConfig ---
            // In a real enhanced backtesting engine, this section would be significantly more complex.
            // It would interpret strategyConfig.Parameters (e.g., sizingMethod, hedgeSettings, riskManagement)
            // to simulate trades more accurately.
            // For example:
            // decimal initialCapital = strategyConfig.AllocatedCapital;
            // var riskManagementSettings = strategyConfig.RiskParameters; // Or from parameters dictionary
            // var sizingMethod = strategyConfig.Parameters["sizingMethod"];
            // ... and so on.
            // The current simple strategy (Close > Open) is used for demonstration.

            foreach (var dataPoint in simulatedHistoricalData.OrderBy(d => d.TimeStamp))
            {
                // Simple strategy: Buy if close > open, Sell if close < open
                if (dataPoint.Close > dataPoint.Open && currentTrade == null)
                {
                    // Simulate a buy order
                    currentTrade = new SimulatedTrade
                    {
                        OrderId = Guid.NewGuid().ToString(),
                        TradingSymbol = symbol,
                        TransactionType = Constants.TRANSACTION_TYPE_BUY,
                        Quantity = 1, // Assuming 1 unit for simplicity
                        EntryPrice = dataPoint.Close,
                        EntryTime = dataPoint.TimeStamp,
                        Status = "Open"
                    };
                    result.SimulatedTrades.Add(currentTrade);
                }
                else if (dataPoint.Close < dataPoint.Open && currentTrade != null && currentTrade.Status == "Open")
                {
                    // Simulate a sell order (closing the position)
                    currentTrade.ExitPrice = dataPoint.Close;
                    currentTrade.ExitTime = dataPoint.TimeStamp;
                    currentTrade.ProfitLoss = (currentTrade.ExitPrice - currentTrade.EntryPrice) * currentTrade.Quantity;
                    currentTrade.Status = "Closed";

                    totalProfitLoss += currentTrade.ProfitLoss;
                    currentEquity += currentTrade.ProfitLoss;

                    if (currentTrade.ProfitLoss > 0)
                    {
                        winningTrades++;
                    }
                    else
                    {
                        losingTrades++;
                    }

                    // Calculate drawdown
                    peakEquity = Math.Max(peakEquity, currentEquity);
                    maxDrawdown = Math.Max(maxDrawdown, peakEquity - currentEquity);

                    currentTrade = null; // Reset for next trade
                }
            }

            // If there's an open trade at the end, close it at the last data point's close price
            if (currentTrade != null && currentTrade.Status == "Open")
            {
                var lastDataPoint = simulatedHistoricalData.OrderByDescending(d => d.TimeStamp).FirstOrDefault();
                if (lastDataPoint != null && lastDataPoint.TimeStamp != default(DateTime))
                {
                    currentTrade.ExitPrice = lastDataPoint.Close;
                    currentTrade.ExitTime = lastDataPoint.TimeStamp;
                    currentTrade.ProfitLoss = (currentTrade.ExitPrice - currentTrade.EntryPrice) * currentTrade.Quantity;
                    currentTrade.Status = "Closed";

                    totalProfitLoss += currentTrade.ProfitLoss;
                    currentEquity += currentTrade.ProfitLoss;

                    if (currentTrade.ProfitLoss > 0)
                    {
                        winningTrades++;
                    }
                    else
                    {
                        losingTrades++;
                    }

                    peakEquity = Math.Max(peakEquity, currentEquity);
                    maxDrawdown = Math.Max(maxDrawdown, peakEquity - currentEquity);
                }
            }

            result.TotalProfitLoss = totalProfitLoss;
            result.TotalTrades = result.SimulatedTrades.Count(t => t.Status == "Closed");
            result.WinningTrades = winningTrades;
            result.LosingTrades = losingTrades;
            result.WinRate = result.TotalTrades > 0 ? (decimal)winningTrades / result.TotalTrades : 0;
            result.MaxDrawdown = maxDrawdown;

            return result;
        }
    }
}
