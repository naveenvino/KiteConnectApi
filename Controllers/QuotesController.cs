using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuotesController : ControllerBase
    {
        private readonly IKiteConnectService _kiteConnectService;
        private readonly ILogger<QuotesController> _logger;

        public QuotesController(IKiteConnectService kiteConnectService, ILogger<QuotesController> logger)
        {
            _kiteConnectService = kiteConnectService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetQuotes([FromQuery] string[] instruments)
        {
            _logger.LogInformation("Fetching quotes for instruments: {Instruments}", string.Join(", ", instruments));

            if (instruments == null || instruments.Length == 0)
            {
                _logger.LogWarning("Bad request for GetQuotes: Instrument list cannot be empty.");
                return BadRequest("Instrument list cannot be empty.");
            }

            try
            {
                var quotes = await _kiteConnectService.GetQuotesAsync(instruments);
                _logger.LogInformation("Successfully fetched quotes for {Count} instruments.", quotes.Count);
                return Ok(quotes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching quotes for instruments: {Instruments}", string.Join(", ", instruments));
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("historical")]
        public async Task<IActionResult> GetHistoricalData(
            [FromQuery] string instrumentToken,
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            [FromQuery] string interval)
        {
            _logger.LogInformation("Fetching historical data for InstrumentToken={InstrumentToken}, From={From}, To={To}, Interval={Interval}", instrumentToken, from, to, interval);

            if (string.IsNullOrEmpty(instrumentToken))
            {
                _logger.LogWarning("Bad request for GetHistoricalData: Instrument token is required.");
                return BadRequest("Instrument token is required.");
            }

            try
            {
                var historicalData = await _kiteConnectService.GetHistoricalDataAsync(instrumentToken, from, to, interval, false); // Added missing 'continuous' parameter
                _logger.LogInformation("Successfully fetched {Count} historical data points for InstrumentToken={InstrumentToken}", historicalData.Count, instrumentToken);
                return Ok(historicalData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching historical data for InstrumentToken={InstrumentToken}", instrumentToken);
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}