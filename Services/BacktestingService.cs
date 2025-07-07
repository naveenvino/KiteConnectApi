// NOTE: This file now ONLY contains the BacktestingService class.
// All other service classes have been moved to their own files.
using KiteConnectApi.Models.Trading;
using System;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class BacktestingService
    {
        private readonly SimulatedKiteConnectService _simulatedKiteConnectService;
        private readonly StrategyService _strategyService;
        private readonly TechnicalAnalysisService _technicalAnalysisService;

        public BacktestingService(
            SimulatedKiteConnectService simulatedKiteConnectService,
            StrategyService strategyService,
            TechnicalAnalysisService technicalAnalysisService)
        {
            _simulatedKiteConnectService = simulatedKiteConnectService;
            _strategyService = strategyService;
            _technicalAnalysisService = technicalAnalysisService;
        }

        public async Task RunBacktest(string symbol, DateTime from, DateTime to, string interval)
        {
            var historicalData = await _technicalAnalysisService.GetHistoricalData(symbol, from, to, interval);
            _simulatedKiteConnectService.LoadHistoricalData(historicalData);

            var dataPoints = await _simulatedKiteConnectService.GetHistoricalDataAsync(symbol, from, to, interval);

            foreach (var dataPoint in dataPoints)
            {
                if (dataPoint.Close > dataPoint.Open)
                {
                    var alert = new TradingViewAlert { Symbol = symbol, Action = "BUY" };
                    await _strategyService.HandleTradingViewAlert(alert);
                }
                await _strategyService.MonitorAndExecuteExits();
            }
        }
    }
}
