using KiteConnectApi.Models.Dto;
using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BacktestingController : ControllerBase
    {
        private readonly BacktestingService _backtestingService;
        private readonly ILogger<BacktestingController> _logger;

        public BacktestingController(BacktestingService backtestingService, ILogger<BacktestingController> logger)
        {
            _backtestingService = backtestingService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<BacktestResultDto>> RunBacktest(
            [FromQuery] string symbol,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] string interval)
        {
            _logger.LogInformation("Backtest request received: Symbol={Symbol}, FromDate={FromDate}, ToDate={ToDate}, Interval={Interval}", symbol, fromDate, toDate, interval);

            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(interval) || fromDate == default || toDate == default)
            {
                _logger.LogWarning("Bad request for backtest: Missing required parameters.");
                return BadRequest("Symbol, fromDate, toDate, and interval are required.");
            }

            try
            {
                var result = await _backtestingService.RunBacktest(symbol, fromDate, toDate, interval);
                _logger.LogInformation("Backtest completed successfully for Symbol={Symbol}", symbol);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during backtesting for Symbol={Symbol}", symbol);
                return StatusCode(500, $"An error occurred during backtesting: {ex.Message}");
            }
        }
    }
}
