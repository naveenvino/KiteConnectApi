using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotesController : ControllerBase
    {
        private readonly KiteConnectService _kiteConnectService;

        public QuotesController(KiteConnectService kiteConnectService)
        {
            _kiteConnectService = kiteConnectService;
        }

        [HttpGet]
        public async Task<IActionResult> GetQuotes([FromQuery] string[] instruments)
        {
            var quotes = await _kiteConnectService.GetQuoteAsync(instruments);
            return Ok(quotes);
        }

        [HttpGet("ohlc")]
        public async Task<IActionResult> GetOHLC([FromQuery] string[] instruments)
        {
            var ohlc = await _kiteConnectService.GetOHLCAsync(instruments);
            return Ok(ohlc);
        }

        [HttpGet("historical")]
        public async Task<IActionResult> GetHistoricalData([FromQuery] string instrument_token, [FromQuery] string from_date, [FromQuery] string to_date, [FromQuery] string interval)
        {
            // Error CS1061: Renamed 'GetHistoricalData' to 'GetHistoricalDataAsync'.
            var historicalData = await _kiteConnectService.GetHistoricalDataAsync(instrument_token, DateTime.Parse(from_date), DateTime.Parse(to_date), interval);
            return Ok(historicalData);
        }
    }
}
