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
        private readonly IKiteConnectService _kiteConnectService;

        public QuotesController(IKiteConnectService kiteConnectService)
        {
            _kiteConnectService = kiteConnectService;
        }

        [HttpGet]
        public async Task<IActionResult> GetQuotes([FromQuery] string[] instruments)
        {
            if (instruments == null || instruments.Length == 0)
            {
                return BadRequest("Instrument list cannot be empty.");
            }
            var quotes = await _kiteConnectService.GetQuotesAsync(instruments);
            return Ok(quotes);
        }

        [HttpGet("historical")]
        public async Task<IActionResult> GetHistoricalData(
            [FromQuery] string instrumentToken,
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] string interval)
        {
            if (string.IsNullOrEmpty(instrumentToken))
            {
                return BadRequest("Instrument token is required.");
            }

            var historicalData = await _kiteConnectService.GetHistoricalDataAsync(instrumentToken, from, to, interval, false); // Added missing 'continuous' parameter
            return Ok(historicalData);
        }
    }
}
