using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace KiteConnectApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class ExpiryController : ControllerBase
    {
        private readonly ExpirySquareOffService _expirySquareOffService;
        private readonly ILogger<ExpiryController> _logger;

        public ExpiryController(
            ExpirySquareOffService expirySquareOffService,
            ILogger<ExpiryController> logger)
        {
            _expirySquareOffService = expirySquareOffService;
            _logger = logger;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetExpirySquareOffStatus()
        {
            try
            {
                var status = await _expirySquareOffService.GetExpirySquareOffStatusAsync();
                return Ok(new { Status = "Success", Data = status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting expiry square-off status");
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpPost("force-square-off")]
        public async Task<IActionResult> ForceSquareOffAllExpiring()
        {
            try
            {
                var success = await _expirySquareOffService.ForceSquareOffAllExpiringPositionsAsync();
                
                if (success)
                {
                    return Ok(new { Status = "Success", Message = "All expiring positions squared off successfully" });
                }
                else
                {
                    return BadRequest(new { Status = "Error", Message = "Some positions failed to square off" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error force squaring off expiring positions");
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }
    }
}