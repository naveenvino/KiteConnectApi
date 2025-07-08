using KiteConnect;
using KiteConnectApi.Models.Trading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class MarketScreenerService
    {
        private readonly IKiteConnectService _kiteConnectService;

        public MarketScreenerService(IKiteConnectService kiteConnectService)
        {
            _kiteConnectService = kiteConnectService;
        }

        public async Task<List<Instrument>> ScreenMarketAsync(ScreenerCriteria criteria)
        {
            var allInstruments = await _kiteConnectService.GetInstrumentsAsync(criteria.Exchange);

            // Filter by instrument type if specified
            if (!string.IsNullOrEmpty(criteria.InstrumentType))
            {
                allInstruments = allInstruments.Where(i => i.InstrumentType == criteria.InstrumentType).ToList();
            }

            // If specific watchlist symbols are provided, filter by them first
            if (criteria.WatchlistSymbols != null && criteria.WatchlistSymbols.Any())
            {
                allInstruments = allInstruments.Where(i => criteria.WatchlistSymbols.Contains(i.TradingSymbol)).ToList();
            }

            // Get quotes for the filtered instruments
            var instrumentTokens = allInstruments.Select(i => i.InstrumentToken.ToString()).ToArray();
            if (!instrumentTokens.Any())
            {
                return new List<Instrument>();
            }

            var quotes = await _kiteConnectService.GetQuotesAsync(instrumentTokens);

            var screenedInstruments = new List<Instrument>();

            foreach (var instrument in allInstruments)
            {
                if (quotes.TryGetValue(instrument.InstrumentToken.ToString(), out Quote quote))
                {
                    // Check if quote is valid (e.g., LastPrice is not default for struct)
                    if (quote.LastPrice == 0 && quote.Volume == 0) continue; // Skip invalid quotes

                    // Apply price criteria
                    if (criteria.MinPrice.HasValue && quote.LastPrice < criteria.MinPrice.Value)
                        continue;
                    if (criteria.MaxPrice.HasValue && quote.LastPrice > criteria.MaxPrice.Value)
                        continue;

                    // Apply volume criteria
                    if (criteria.MinVolume.HasValue && quote.Volume < criteria.MinVolume.Value)
                        continue;
                    if (criteria.MaxVolume.HasValue && quote.Volume > criteria.MaxVolume.Value)
                        continue;

                    // TODO: Implement technical indicator screening here
                    // This will require fetching historical data and calculating indicators

                    screenedInstruments.Add(instrument);
                }
            }

            return screenedInstruments;
        }
    }
}
