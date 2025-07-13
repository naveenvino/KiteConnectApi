using Microsoft.AspNetCore.Mvc;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class StrategiesController : ControllerBase
    {
        private readonly IStrategyRepository _strategyRepository;
        private readonly ILogger<StrategiesController> _logger;

        public StrategiesController(IStrategyRepository strategyRepository, ILogger<StrategiesController> logger)
        {
            _strategyRepository = strategyRepository;
            _logger = logger;
        }

        // GET: api/Strategies
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Strategy>>> GetStrategies()
        {
            _logger.LogInformation("Fetching all strategies.");
            try
            {
                var strategies = await _strategyRepository.GetAllStrategiesAsync();
                _logger.LogInformation("Successfully fetched {Count} strategies.", strategies.Count());
                return Ok(strategies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all strategies.");
                return StatusCode(500, "Internal server error.");
            }
        }

        // GET: api/Strategies/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Strategy>> GetStrategy(int id)
        {
            _logger.LogInformation("Fetching strategy with ID: {Id}", id);
            try
            {
                var strategy = await _strategyRepository.GetStrategyByIdAsync(id);

                if (strategy == null)
                {
                    _logger.LogWarning("Strategy with ID: {Id} not found.", id);
                    return NotFound();
                }

                _logger.LogInformation("Successfully fetched strategy with ID: {Id}", id);
                return Ok(strategy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching strategy with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        // POST: api/Strategies
        [HttpPost]
        public async Task<ActionResult<Strategy>> PostStrategy(Strategy strategy)
        {
            _logger.LogInformation("Adding new strategy with ID: {Id}", strategy.Id);
            try
            {
                await _strategyRepository.AddStrategyAsync(strategy);
                _logger.LogInformation("Successfully added strategy with ID: {Id}", strategy.Id);
                return CreatedAtAction(nameof(GetStrategy), new { id = strategy.Id }, strategy);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding strategy with ID: {Id}", strategy.Id);
                return StatusCode(500, "Internal server error.");
            }
        }

        // PUT: api/Strategies/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutStrategy(int id, Strategy strategy)
        {
            _logger.LogInformation("Updating strategy with ID: {Id}", id);
            if (id != strategy.Id)
            {
                _logger.LogWarning("Strategy ID mismatch for update. Route ID: {RouteId}, Body ID: {BodyId}", id, strategy.Id);
                return BadRequest();
            }

            try
            {
                await _strategyRepository.UpdateStrategyAsync(strategy);
                _logger.LogInformation("Successfully updated strategy with ID: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating strategy with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        // DELETE: api/Strategies/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStrategy(int id)
        {
            _logger.LogInformation("Deleting strategy with ID: {Id}", id);
            try
            {
                await _strategyRepository.DeleteStrategyAsync(id);
                _logger.LogInformation("Successfully deleted strategy with ID: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting strategy with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}