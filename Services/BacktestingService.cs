using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using KiteConnect; // Added this line

namespace KiteConnectApi.Services
{
    public class BacktestingService
    {
        private readonly IKiteConnectService _kiteConnectService;
        private readonly StrategyService _strategyService;
        private readonly TechnicalAnalysisService _technicalAnalysisService;

        public BacktestingService(
            IKiteConnectService kiteConnectService,
            StrategyService strategyService,
            TechnicalAnalysisService technicalAnalysisService)
        {
            _kiteConnectService = kiteConnectService;
            _strategyService = strategyService;
            _technicalAnalysisService = technicalAnalysisService;
        }

        public async Task<BacktestResultDto> RunBacktest(string symbol, DateTime from, DateTime to, string interval)
        {
            var historicalData = await _kiteConnectService.GetHistoricalDataAsync(symbol, from, to, interval);

            var result = new BacktestResultDto
            {
                Symbol = symbol,
                FromDate = from,
                ToDate = to,
                Interval = interval
            };

            SimulatedTrade? currentTrade = null;
            decimal totalProfitLoss = 0;
            int winningTrades = 0;
            int losingTrades = 0;
            decimal maxDrawdown = 0;
            decimal peakEquity = 0;
            decimal currentEquity = 0;

            foreach (var dataPoint in historicalData.OrderBy(d => d.TimeStamp))
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
                var lastDataPoint = historicalData.OrderByDescending(d => d.TimeStamp).FirstOrDefault();
                if (lastDataPoint.TimeStamp != default(DateTime))
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
