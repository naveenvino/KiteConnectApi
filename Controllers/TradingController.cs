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
            // Error CS1061: Renamed 'ProcessTradingViewAlert' to 'HandleTradingViewAlert'.
            await _strategyService.HandleTradingViewAlert(alert);
            return Ok(new { Status = "AlertProcessed" });
        }
    }
}
