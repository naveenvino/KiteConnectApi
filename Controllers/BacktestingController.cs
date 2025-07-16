using KiteConnectApi.Models.Dto;
using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.Logging;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BacktestingController : ControllerBase
    {
        private readonly BacktestingService _backtestingService;
        private readonly ILogger<BacktestingController> _logger;
        private readonly IStrategyRepository _strategyRepository;

        public BacktestingController(BacktestingService backtestingService, ILogger<BacktestingController> logger, IStrategyRepository strategyRepository)
        {
            _backtestingService = backtestingService;
            _logger = logger;
            _strategyRepository = strategyRepository;
        }

        [HttpGet("strategies")]
        public async Task<ActionResult<IEnumerable<Strategy>>> GetBacktestStrategies()
        {
            _logger.LogInformation("Fetching strategies for backtesting.");
            try
            {
                var strategies = await _strategyRepository.GetAllStrategiesAsync();
                return Ok(strategies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching strategies for backtesting.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<BacktestResultDto>> RunBacktest([FromQuery] string strategyId)
        {
            _logger.LogInformation("Backtest request received for StrategyId={StrategyId}", strategyId);

            if (string.IsNullOrEmpty(strategyId))
            {
                _logger.LogWarning("Bad request for backtest: Missing strategyId.");
                return BadRequest("StrategyId is required.");
            }

            try
            {
                var result = await _backtestingService.RunBacktest(strategyId);
                _logger.LogInformation("Backtest completed successfully for StrategyId={StrategyId}", strategyId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during backtesting for StrategyId={StrategyId}", strategyId);
                return StatusCode(500, $"An error occurred during backtesting: {ex.Message}");
            }
        }
    }
}
