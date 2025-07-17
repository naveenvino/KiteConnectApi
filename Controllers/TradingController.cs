using KiteConnectApi.Models.Dto;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using KiteConnectApi.Data;
using Microsoft.EntityFrameworkCore;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous]
    public class TradingController : ControllerBase
    {
        private readonly StrategyService _strategyService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TradingController> _logger;

        public TradingController(StrategyService strategyService, ApplicationDbContext context, ILogger<TradingController> logger)
        {
            _strategyService = strategyService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("alert")]
        public async Task<IActionResult> HandleAlert([FromBody] TradingViewAlert alert)
        {
            try
            {
                _logger.LogInformation("Received TradingView alert: {Alert}", System.Text.Json.JsonSerializer.Serialize(alert));
                await _strategyService.HandleTradingViewAlert(alert);
                return Ok(new { Status = "AlertProcessed", Message = "Alert processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing TradingView alert");
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpPost("exit-all")]
        public async Task<IActionResult> ExitAllPositions()
        {
            try
            {
                await _strategyService.ExitAllPositionsAsync();
                return Ok(new { Status = "Success", Message = "Exit all command processed." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exiting all positions");
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpPost("exit-strategy/{strategyId}")]
        public async Task<IActionResult> ExitStrategyPositions(string strategyId)
        {
            try
            {
                await _strategyService.ExitStrategyPositionsAsync(strategyId);
                return Ok(new { Status = "Success", Message = $"Strategy {strategyId} positions exited." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exiting strategy positions for {StrategyId}", strategyId);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpGet("pending-alerts")]
        public async Task<IActionResult> GetPendingAlerts()
        {
            try
            {
                var pendingAlerts = await _context.PendingAlerts
                    .Where(a => a.Status == "PENDING" && a.ExpiryTime > DateTime.Now)
                    .OrderBy(a => a.Priority)
                    .ThenBy(a => a.ReceivedTime)
                    .ToListAsync();

                return Ok(new { Status = "Success", Data = pendingAlerts });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending alerts");
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpPost("execute-pending-alert/{alertId}")]
        public async Task<IActionResult> ExecutePendingAlert(string alertId)
        {
            try
            {
                var alert = await _context.PendingAlerts
                    .FirstOrDefaultAsync(a => a.Id == alertId && a.Status == "PENDING");

                if (alert == null)
                {
                    return NotFound(new { Status = "Error", Message = "Alert not found or already processed" });
                }

                var tradingAlert = System.Text.Json.JsonSerializer.Deserialize<TradingViewAlert>(alert.AlertJson);
                if (tradingAlert != null)
                {
                    await _strategyService.HandleTradingViewAlert(tradingAlert);
                }

                alert.Status = "EXECUTED";
                alert.ExecutedTime = DateTime.Now;
                alert.ExecutedBy = "Manual";
                await _context.SaveChangesAsync();

                return Ok(new { Status = "Success", Message = "Alert executed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing pending alert {AlertId}", alertId);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpPost("cancel-pending-alert/{alertId}")]
        public async Task<IActionResult> CancelPendingAlert(string alertId)
        {
            try
            {
                var alert = await _context.PendingAlerts
                    .FirstOrDefaultAsync(a => a.Id == alertId && a.Status == "PENDING");

                if (alert == null)
                {
                    return NotFound(new { Status = "Error", Message = "Alert not found or already processed" });
                }

                alert.Status = "CANCELLED";
                await _context.SaveChangesAsync();

                return Ok(new { Status = "Success", Message = "Alert cancelled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling pending alert {AlertId}", alertId);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpPost("execute-all-pending")]
        public async Task<IActionResult> ExecuteAllPendingAlerts()
        {
            try
            {
                var pendingAlerts = await _context.PendingAlerts
                    .Where(a => a.Status == "PENDING" && a.ExpiryTime > DateTime.Now)
                    .OrderBy(a => a.Priority)
                    .ThenBy(a => a.ReceivedTime)
                    .ToListAsync();

                var executedCount = 0;
                var failedCount = 0;

                foreach (var alert in pendingAlerts)
                {
                    try
                    {
                        var tradingAlert = System.Text.Json.JsonSerializer.Deserialize<TradingViewAlert>(alert.AlertJson);
                        if (tradingAlert != null)
                        {
                            await _strategyService.HandleTradingViewAlert(tradingAlert);

                            alert.Status = "EXECUTED";
                            alert.ExecutedTime = DateTime.Now;
                            alert.ExecutedBy = "Auto";
                            executedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        alert.Status = "FAILED";
                        alert.ErrorMessage = ex.Message;
                        failedCount++;
                        _logger.LogError(ex, "Error executing alert {AlertId}", alert.Id);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { 
                    Status = "Success", 
                    Message = $"Executed {executedCount} alerts, {failedCount} failed",
                    ExecutedCount = executedCount,
                    FailedCount = failedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing all pending alerts");
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpGet("positions")]
        public async Task<IActionResult> GetOptionsPositions()
        {
            try
            {
                var positions = await _context.OptionsTradePositions
                    .Where(p => p.Status == "OPEN")
                    .OrderBy(p => p.EntryTime)
                    .ToListAsync();

                return Ok(new { Status = "Success", Data = positions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving options positions");
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpGet("positions/{strategyId}")]
        public async Task<IActionResult> GetStrategyPositions(string strategyId)
        {
            try
            {
                var positions = await _context.OptionsTradePositions
                    .Where(p => p.StrategyId == strategyId && p.Status == "OPEN")
                    .OrderBy(p => p.EntryTime)
                    .ToListAsync();

                return Ok(new { Status = "Success", Data = positions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving strategy positions for {StrategyId}", strategyId);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpPost("modify-position/{positionId}")]
        public async Task<IActionResult> ModifyPosition(string positionId, [FromBody] ModifyPositionRequest request)
        {
            try
            {
                var position = await _context.OptionsTradePositions
                    .FirstOrDefaultAsync(p => p.Id == positionId && p.Status == "OPEN");

                if (position == null)
                {
                    return NotFound(new { Status = "Error", Message = "Position not found or already closed" });
                }

                if (request.StopLossPrice.HasValue)
                {
                    position.StopLossPrice = request.StopLossPrice.Value;
                }

                if (request.TargetPrice.HasValue)
                {
                    position.TargetPrice = request.TargetPrice.Value;
                }

                position.LastUpdated = DateTime.Now;
                await _context.SaveChangesAsync();

                return Ok(new { Status = "Success", Message = "Position modified successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error modifying position {PositionId}", positionId);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpPost("close-position/{positionId}")]
        public async Task<IActionResult> ClosePosition(string positionId)
        {
            try
            {
                await _strategyService.ClosePositionAsync(positionId, "MANUAL");
                return Ok(new { Status = "Success", Message = "Position closed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing position {PositionId}", positionId);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }
    }

    public class ModifyPositionRequest
    {
        public decimal? StopLossPrice { get; set; }
        public decimal? TargetPrice { get; set; }
    }
}
