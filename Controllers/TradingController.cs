using KiteConnectApi.Models.Trading;
using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
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
            // --- FIX ---
            // The controller was calling the wrong method.
            // It now correctly calls HandleTradingViewAlert to process the incoming alert.
            await _strategyService.HandleTradingViewAlert(alert);
            // --- END OF FIX ---

            return Ok(new { Status = "AlertProcessed" });
        }
    }
}
