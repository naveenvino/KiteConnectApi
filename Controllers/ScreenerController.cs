using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScreenerController : ControllerBase
    {
        private readonly IScreenerCriteriaRepository _screenerCriteriaRepository;
        private readonly MarketScreenerService _marketScreenerService;

        public ScreenerController(
            IScreenerCriteriaRepository screenerCriteriaRepository,
            MarketScreenerService marketScreenerService)
        {
            _screenerCriteriaRepository = screenerCriteriaRepository;
            _marketScreenerService = marketScreenerService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScreenerCriteria>>> GetAllScreenerCriterias()
        {
            var criterias = await _screenerCriteriaRepository.GetAllScreenerCriteriasAsync();
            return Ok(criterias);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScreenerCriteria>> GetScreenerCriteria(string id)
        {
            var criteria = await _screenerCriteriaRepository.GetScreenerCriteriaByIdAsync(id);
            if (criteria == null)
            {
                return NotFound();
            }
            return Ok(criteria);
        }

        [HttpPost]
        public async Task<ActionResult<ScreenerCriteria>> AddScreenerCriteria([FromBody] ScreenerCriteria criteria)
        {
            await _screenerCriteriaRepository.AddScreenerCriteriaAsync(criteria);
            return CreatedAtAction(nameof(GetScreenerCriteria), new { id = criteria.Id }, criteria);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateScreenerCriteria(string id, [FromBody] ScreenerCriteria criteria)
        {
            if (id != criteria.Id)
            {
                return BadRequest("Screener criteria ID mismatch.");
            }

            var existingCriteria = await _screenerCriteriaRepository.GetScreenerCriteriaByIdAsync(id);
            if (existingCriteria == null)
            {
                return NotFound();
            }

            await _screenerCriteriaRepository.UpdateScreenerCriteriaAsync(criteria);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteScreenerCriteria(string id)
        {
            var existingCriteria = await _screenerCriteriaRepository.GetScreenerCriteriaByIdAsync(id);
            if (existingCriteria == null)
            {
                return NotFound();
            }

            await _screenerCriteriaRepository.DeleteScreenerCriteriaAsync(id);
            return NoContent();
        }

        [HttpPost("run/{id}")]
        public async Task<ActionResult<IEnumerable<KiteConnect.Instrument>>> RunScreener(string id)
        {
            var criteria = await _screenerCriteriaRepository.GetScreenerCriteriaByIdAsync(id);
            if (criteria == null)
            {
                return NotFound("Screener criteria not found.");
            }

            var result = await _marketScreenerService.ScreenMarketAsync(criteria);
            return Ok(result);
        }
    }
}
