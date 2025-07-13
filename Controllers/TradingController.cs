using KiteConnectApi.Models.Dto;
using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class TradingController : ControllerBase
    {
        private readonly StrategyService _strategyService;

        public TradingController(StrategyService strategyService)
        {
            _strategyService = strategyService;
        }

        [HttpPost("alert")]
        public async Task<IActionResult> HandleAlert([FromBody] TradingViewAlert alert)
        {
            await _strategyService.HandleTradingViewAlert(alert);
            return Ok(new { Status = "AlertProcessed" });
        }

        // --- NEW ENDPOINT FOR MANUAL EXIT ---
        [HttpPost("exit-all")]
        public async Task<IActionResult> ExitAllPositions()
        {
            await _strategyService.ExitAllPositionsAsync();
            return Ok(new { Status = "Exit all command processed." });
        }
        // --- END OF NEW ENDPOINT ---
    }
}
