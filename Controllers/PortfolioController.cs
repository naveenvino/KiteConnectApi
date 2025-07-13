using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using KiteConnectApi.Models.Dto;
using System.Linq;
using KiteConnectApi.Repositories;
using Microsoft.Extensions.Logging;
using System;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PortfolioController : ControllerBase
    {
        private readonly IKiteConnectService _kiteConnectService;
        private readonly IPositionRepository _positionRepository;
        private readonly ILogger<PortfolioController> _logger;

        public PortfolioController(IKiteConnectService kiteConnectService, IPositionRepository positionRepository, ILogger<PortfolioController> logger)
        {
            _kiteConnectService = kiteConnectService;
            _positionRepository = positionRepository;
            _logger = logger;
        }

        [HttpGet("holdings")]
        public async Task<IActionResult> GetHoldings()
        {
            _logger.LogInformation("Fetching holdings.");
            try
            {
                var holdings = await _kiteConnectService.GetHoldingsAsync();
                _logger.LogInformation("Successfully fetched {Count} holdings.", holdings.Count);
                return Ok(holdings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching holdings.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("positions")]
        public async Task<IActionResult> GetPositions()
        {
            _logger.LogInformation("Fetching positions.");
            try
            {
                var positions = await _kiteConnectService.GetPositionsAsync();
                _logger.LogInformation("Successfully fetched {Count} positions.", positions.Count);
                return Ok(positions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching positions.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("pnl")]
        public async Task<ActionResult<PositionPnlDto>> GetPortfolioPnl()
        {
            _logger.LogInformation("Calculating portfolio P&L.");
            try
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

                _logger.LogInformation("Portfolio P&L calculated: Realized={Realized}, Unrealized={Unrealized}, Overall={Overall}", pnlDto.TotalRealizedPnl, pnlDto.TotalUnrealizedPnl, pnlDto.OverallPnl);
                return Ok(pnlDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating portfolio P&L.");
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}