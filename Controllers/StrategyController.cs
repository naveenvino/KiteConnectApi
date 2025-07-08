using KiteConnectApi.Models.Trading;
using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StrategyController : ControllerBase
    {
        private readonly StrategyManagerService _strategyManagerService;
        private readonly PortfolioAllocationService _portfolioAllocationService;

        public StrategyController(StrategyManagerService strategyManagerService, PortfolioAllocationService portfolioAllocationService)
        {
            _strategyManagerService = strategyManagerService;
            _portfolioAllocationService = portfolioAllocationService;
        }

        [HttpGet("total-allocated-capital")]
        public async Task<ActionResult<decimal>> GetTotalAllocatedCapital()
        {
            var totalCapital = await _portfolioAllocationService.GetTotalAllocatedCapitalAsync();
            return Ok(totalCapital);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StrategyConfig>>> GetActiveStrategies()
        {
            var strategies = await _strategyManagerService.GetActiveStrategiesAsync();
            return Ok(strategies);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StrategyConfig>> GetStrategyConfig(string id)
        {
            var strategyConfig = await _strategyManagerService.GetStrategyConfigByIdAsync(id);
            if (strategyConfig == null)
            {
                return NotFound();
            }
            return Ok(strategyConfig);
        }

        [HttpPost]
        public async Task<ActionResult<StrategyConfig>> AddStrategyConfig([FromBody] StrategyConfig config)
        {
            await _strategyManagerService.AddStrategyConfigAsync(config);
            return CreatedAtAction(nameof(GetStrategyConfig), new { id = config.Id }, config);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStrategyConfig(string id, [FromBody] StrategyConfig config)
        {
            if (id != config.Id)
            {
                return BadRequest("Strategy ID mismatch.");
            }

            var existingConfig = await _strategyManagerService.GetStrategyConfigByIdAsync(id);
            if (existingConfig == null)
            {
                return NotFound();
            }

            await _strategyManagerService.UpdateStrategyConfigAsync(config);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStrategyConfig(string id)
        {
            var existingConfig = await _strategyManagerService.GetStrategyConfigByIdAsync(id);
            if (existingConfig == null)
            {
                return NotFound();
            }

            await _strategyManagerService.DeleteStrategyConfigAsync(id);
            return NoContent();
        }
    }
}
