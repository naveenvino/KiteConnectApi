using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScreenerController : ControllerBase
    {
        private readonly IScreenerCriteriaRepository _screenerCriteriaRepository;
        private readonly MarketScreenerService _marketScreenerService;
        private readonly ILogger<ScreenerController> _logger;

        public ScreenerController(
            IScreenerCriteriaRepository screenerCriteriaRepository,
            MarketScreenerService marketScreenerService,
            ILogger<ScreenerController> logger)
        {
            _screenerCriteriaRepository = screenerCriteriaRepository;
            _marketScreenerService = marketScreenerService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScreenerCriteria>>> GetAllScreenerCriterias()
        {
            _logger.LogInformation("Fetching all screener criterias.");
            try
            {
                var criterias = await _screenerCriteriaRepository.GetAllScreenerCriteriasAsync();
                _logger.LogInformation("Successfully fetched {Count} screener criterias.", criterias.Count());
                return Ok(criterias);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all screener criterias.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScreenerCriteria>> GetScreenerCriteria(string id)
        {
            _logger.LogInformation("Fetching screener criteria with ID: {Id}", id);
            try
            {
                var criteria = await _screenerCriteriaRepository.GetScreenerCriteriaByIdAsync(id);
                if (criteria == null)
                {
                    _logger.LogWarning("Screener criteria with ID: {Id} not found.", id);
                    return NotFound();
                }
                _logger.LogInformation("Successfully fetched screener criteria with ID: {Id}", id);
                return Ok(criteria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching screener criteria with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost]
        public async Task<ActionResult<ScreenerCriteria>> AddScreenerCriteria([FromBody] ScreenerCriteria criteria)
        {
            _logger.LogInformation("Adding new screener criteria with ID: {Id}", criteria.Id);
            try
            {
                await _screenerCriteriaRepository.AddScreenerCriteriaAsync(criteria);
                _logger.LogInformation("Successfully added screener criteria with ID: {Id}", criteria.Id);
                return CreatedAtAction(nameof(GetScreenerCriteria), new { id = criteria.Id }, criteria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding screener criteria with ID: {Id}", criteria.Id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateScreenerCriteria(string id, [FromBody] ScreenerCriteria criteria)
        {
            _logger.LogInformation("Updating screener criteria with ID: {Id}", id);
            if (id != criteria.Id)
            {
                _logger.LogWarning("Screener criteria ID mismatch for update. Route ID: {RouteId}, Body ID: {BodyId}", id, criteria.Id);
                return BadRequest("Screener criteria ID mismatch.");
            }

            try
            {
                var existingCriteria = await _screenerCriteriaRepository.GetScreenerCriteriaByIdAsync(id);
                if (existingCriteria == null)
                {
                    _logger.LogWarning("Screener criteria with ID: {Id} not found for update.", id);
                    return NotFound();
                }

                await _screenerCriteriaRepository.UpdateScreenerCriteriaAsync(criteria);
                _logger.LogInformation("Successfully updated screener criteria with ID: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating screener criteria with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteScreenerCriteria(string id)
        {
            _logger.LogInformation("Deleting screener criteria with ID: {Id}", id);
            try
            {
                var existingCriteria = await _screenerCriteriaRepository.GetScreenerCriteriaByIdAsync(id);
                if (existingCriteria == null)
                {
                    _logger.LogWarning("Screener criteria with ID: {Id} not found for deletion.", id);
                    return NotFound();
                }

                await _screenerCriteriaRepository.DeleteScreenerCriteriaAsync(id);
                _logger.LogInformation("Successfully deleted screener criteria with ID: {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting screener criteria with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost("run/{id}")]
        public async Task<ActionResult<IEnumerable<KiteConnect.Instrument>>> RunScreener(string id)
        {
            _logger.LogInformation("Running screener with ID: {Id}", id);
            try
            {
                var criteria = await _screenerCriteriaRepository.GetScreenerCriteriaByIdAsync(id);
                if (criteria == null)
                {
                    _logger.LogWarning("Screener criteria with ID: {Id} not found for running screener.", id);
                    return NotFound("Screener criteria not found.");
                }

                var result = await _marketScreenerService.ScreenMarketAsync(criteria);
                _logger.LogInformation("Screener with ID: {Id} completed successfully. Found {Count} instruments.", id, result.Count());
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running screener with ID: {Id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}