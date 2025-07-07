using KiteConnectApi.Models.Trading;
using System;
using System.Threading.Tasks;

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

        public async Task RunBacktest(string symbol, DateTime from, DateTime to, string interval)
        {
            var historicalData = await _technicalAnalysisService.GetHistoricalData(symbol, from, to, interval);
            // The simulated service no longer needs to load historical data this way.
            // _simulatedKiteConnectService.LoadHistoricalData(historicalData);

            var dataPoints = await _kiteConnectService.GetHistoricalDataAsync(symbol, from, to, interval);

            foreach (var dataPoint in dataPoints)
            {
                if (dataPoint.Close > dataPoint.Open)
                {
                    var alert = new TradingViewAlert { Strike = 22500, Type = "PE", Action = "Entry", Signal = "Backtest-Buy" };
                    await _strategyService.HandleTradingViewAlert(alert);
                }
            }
        }
    }
}
