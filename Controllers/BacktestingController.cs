using KiteConnectApi.Models.Dto;
using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BacktestingController : ControllerBase
    {
        private readonly BacktestingService _backtestingService;

        public BacktestingController(BacktestingService backtestingService)
        {
            _backtestingService = backtestingService;
        }

        [HttpPost]
        public async Task<ActionResult<BacktestResultDto>> RunBacktest(
            [FromQuery] string symbol,
            [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate,
            [FromQuery] string interval)
        {
            if (string.IsNullOrEmpty(symbol) || string.IsNullOrEmpty(interval) || fromDate == default || toDate == default)
            {
                return BadRequest("Symbol, fromDate, toDate, and interval are required.");
            }

            try
            {
                var result = await _backtestingService.RunBacktest(symbol, fromDate, toDate, interval);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred during backtesting: {ex.Message}");
            }
        }
    }
}
