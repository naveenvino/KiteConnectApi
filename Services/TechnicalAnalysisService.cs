using KiteConnectApi.Models.Trading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class TechnicalAnalysisService
    {
        private readonly IKiteConnectService _kiteConnectService;

        public TechnicalAnalysisService(IKiteConnectService kiteConnectService)
        {
            _kiteConnectService = kiteConnectService;
        }

        public async Task<decimal> CalculateRSI(string symbol, string exchange, int period)
        {
            Console.WriteLine($"Calculating RSI for {symbol}");
            var data = await GetHistoricalData(symbol, exchange, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, "day");
            if (data.Count > period)
            {
                return 45m; // Dummy value
            }
            return 50m; // Default dummy value
        }

        public async Task<List<SimulatedHistoricalData>> GetHistoricalData(string symbol, string exchange, DateTime from, DateTime to, string interval)
        {
            // In a real backtesting scenario, this would fetch actual historical data.
            // For now, return the data set via SetHistoricalData.
            var instruments = await _kiteConnectService.GetInstrumentsAsync(exchange);
            var instrument = instruments.FirstOrDefault(i => i.TradingSymbol == symbol);

            if (instrument == null)
            {
                // Log error or throw exception
                return new List<SimulatedHistoricalData>();
            }

            return await _kiteConnectService.GetHistoricalDataAsync(instrument.InstrumentToken.ToString(), from, to, interval);
        }
    }
}
