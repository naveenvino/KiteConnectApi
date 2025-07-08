using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KiteConnectApi.Models.Dto;
using System.Linq;
using KiteConnectApi.Repositories;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly IKiteConnectService _kiteConnectService;
        private readonly IPositionRepository _positionRepository;

        public PortfolioController(IKiteConnectService kiteConnectService, IPositionRepository positionRepository)
        {
            _kiteConnectService = kiteConnectService;
            _positionRepository = positionRepository;
        }

        [HttpGet("holdings")]
        public async Task<IActionResult> GetHoldings()
        {
            var holdings = await _kiteConnectService.GetHoldingsAsync();
            return Ok(holdings);
        }

        [HttpGet("positions")]
        public async Task<IActionResult> GetPositions()
        {
            var positions = await _kiteConnectService.GetPositionsAsync();
            return Ok(positions);
        }

        [HttpGet("pnl")]
        public async Task<ActionResult<PositionPnlDto>> GetPortfolioPnl()
        {
            var positions = await _kiteConnectService.GetPositionsAsync();
            var holdings = await _kiteConnectService.GetHoldingsAsync();

            decimal totalRealizedPnl = 0;
            decimal totalUnrealizedPnl = 0;

            // Calculate PnL from positions
            foreach (var position in positions)
            {
                totalRealizedPnl += position.Realised ?? 0;
                totalUnrealizedPnl += position.Unrealised ?? 0;
            }

            // Calculate PnL from holdings
            foreach (var holding in holdings)
            {
                // Assuming PnL for holdings is calculated based on current price vs average price
                // KiteConnect.Holding has `LastPrice` and `AveragePrice`
                totalUnrealizedPnl += (holding.LastPrice - holding.AveragePrice) * holding.Quantity;
            }

            var pnlDto = new PositionPnlDto
            {
                TotalRealizedPnl = totalRealizedPnl,
                TotalUnrealizedPnl = totalUnrealizedPnl,
                OverallPnl = totalRealizedPnl + totalUnrealizedPnl
            };

            return Ok(pnlDto);
        }
    }
}