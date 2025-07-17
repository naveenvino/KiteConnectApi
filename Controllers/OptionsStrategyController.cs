using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace KiteConnectApi.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [Authorize]
    public class OptionsStrategyController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OptionsStrategyController> _logger;

        public OptionsStrategyController(
            ApplicationDbContext context,
            ILogger<OptionsStrategyController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllStrategies()
        {
            try
            {
                var strategies = await _context.NiftyOptionStrategyConfigs
                    .Where(s => !s.IsDeleted)
                    .OrderBy(s => s.StrategyName)
                    .ToListAsync();

                return Ok(new { Status = "Success", Data = strategies });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving strategies");
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetStrategy(string id)
        {
            try
            {
                var strategy = await _context.NiftyOptionStrategyConfigs
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (strategy == null)
                {
                    return NotFound(new { Status = "Error", Message = "Strategy not found" });
                }

                return Ok(new { Status = "Success", Data = strategy });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving strategy {StrategyId}", id);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateStrategy([FromBody] NiftyOptionStrategyConfig strategy)
        {
            try
            {
                if (string.IsNullOrEmpty(strategy.StrategyName))
                {
                    return BadRequest(new { Status = "Error", Message = "Strategy name is required" });
                }

                // Check if strategy name already exists
                var existingStrategy = await _context.NiftyOptionStrategyConfigs
                    .FirstOrDefaultAsync(s => s.StrategyName == strategy.StrategyName && !s.IsDeleted);

                if (existingStrategy != null)
                {
                    return BadRequest(new { Status = "Error", Message = "Strategy with this name already exists" });
                }

                strategy.Id = Guid.NewGuid().ToString();
                strategy.CreatedTime = DateTime.Now;
                strategy.LastUpdated = DateTime.Now;

                _context.NiftyOptionStrategyConfigs.Add(strategy);
                await _context.SaveChangesAsync();

                return Ok(new { Status = "Success", Message = "Strategy created successfully", Data = strategy });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating strategy");
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStrategy(string id, [FromBody] NiftyOptionStrategyConfig strategy)
        {
            try
            {
                var existingStrategy = await _context.NiftyOptionStrategyConfigs
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (existingStrategy == null)
                {
                    return NotFound(new { Status = "Error", Message = "Strategy not found" });
                }

                // Update fields
                existingStrategy.StrategyName = strategy.StrategyName;
                existingStrategy.UnderlyingInstrument = strategy.UnderlyingInstrument;
                existingStrategy.Exchange = strategy.Exchange;
                existingStrategy.ProductType = strategy.ProductType;
                existingStrategy.Quantity = strategy.Quantity;
                existingStrategy.AllocatedMargin = strategy.AllocatedMargin;
                existingStrategy.UseDynamicQuantity = strategy.UseDynamicQuantity;
                existingStrategy.FromDate = strategy.FromDate;
                existingStrategy.ToDate = strategy.ToDate;
                existingStrategy.EntryTime = strategy.EntryTime;
                existingStrategy.ExitTime = strategy.ExitTime;
                existingStrategy.StopLossPercentage = strategy.StopLossPercentage;
                existingStrategy.TargetPercentage = strategy.TargetPercentage;
                existingStrategy.TakeProfitPercentage = strategy.TakeProfitPercentage;
                existingStrategy.MaxTradesPerDay = strategy.MaxTradesPerDay;
                existingStrategy.IsEnabled = strategy.IsEnabled;
                
                // Hedge Configuration
                existingStrategy.HedgeEnabled = strategy.HedgeEnabled;
                existingStrategy.HedgeType = strategy.HedgeType;
                existingStrategy.HedgeDistancePoints = strategy.HedgeDistancePoints;
                existingStrategy.HedgePremiumPercentage = strategy.HedgePremiumPercentage;
                existingStrategy.HedgeRatio = strategy.HedgeRatio;
                
                // Risk Management
                existingStrategy.OverallPositionStopLoss = strategy.OverallPositionStopLoss;
                existingStrategy.MaxDailyLoss = strategy.MaxDailyLoss;
                existingStrategy.MaxPositionSize = strategy.MaxPositionSize;
                
                // Profit Management
                existingStrategy.LockProfitPercentage = strategy.LockProfitPercentage;
                existingStrategy.LockProfitAmount = strategy.LockProfitAmount;
                existingStrategy.TrailStopLossPercentage = strategy.TrailStopLossPercentage;
                existingStrategy.TrailStopLossAmount = strategy.TrailStopLossAmount;
                existingStrategy.MoveStopLossToEntryPricePercentage = strategy.MoveStopLossToEntryPricePercentage;
                existingStrategy.MoveStopLossToEntryPriceAmount = strategy.MoveStopLossToEntryPriceAmount;
                
                // Exit and Re-enter Logic
                existingStrategy.ExitAndReenterProfitPercentage = strategy.ExitAndReenterProfitPercentage;
                existingStrategy.ExitAndReenterProfitAmount = strategy.ExitAndReenterProfitAmount;
                existingStrategy.MinReentryDelayMinutes = strategy.MinReentryDelayMinutes;
                
                // Execution Mode
                existingStrategy.ExecutionMode = strategy.ExecutionMode;
                existingStrategy.ManualExecutionTimeoutMinutes = strategy.ManualExecutionTimeoutMinutes;
                
                // Expiry Management
                existingStrategy.AutoSquareOffOnExpiry = strategy.AutoSquareOffOnExpiry;
                existingStrategy.ExpirySquareOffTimeMinutes = strategy.ExpirySquareOffTimeMinutes;
                existingStrategy.UseNearestWeeklyExpiry = strategy.UseNearestWeeklyExpiry;
                
                // Signal Configuration
                existingStrategy.AllowedSignals = strategy.AllowedSignals;
                existingStrategy.AllowMultipleSignals = strategy.AllowMultipleSignals;
                
                // Notifications
                existingStrategy.NotifyOnEntry = strategy.NotifyOnEntry;
                existingStrategy.NotifyOnExit = strategy.NotifyOnExit;
                existingStrategy.NotifyOnStopLoss = strategy.NotifyOnStopLoss;
                existingStrategy.NotifyOnProfit = strategy.NotifyOnProfit;
                
                existingStrategy.LastUpdated = DateTime.Now;
                existingStrategy.Notes = strategy.Notes;

                await _context.SaveChangesAsync();

                return Ok(new { Status = "Success", Message = "Strategy updated successfully", Data = existingStrategy });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating strategy {StrategyId}", id);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStrategy(string id)
        {
            try
            {
                var strategy = await _context.NiftyOptionStrategyConfigs
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (strategy == null)
                {
                    return NotFound(new { Status = "Error", Message = "Strategy not found" });
                }

                // Soft delete
                strategy.IsDeleted = true;
                strategy.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { Status = "Success", Message = "Strategy deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting strategy {StrategyId}", id);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpPost("{id}/enable")]
        public async Task<IActionResult> EnableStrategy(string id)
        {
            try
            {
                var strategy = await _context.NiftyOptionStrategyConfigs
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (strategy == null)
                {
                    return NotFound(new { Status = "Error", Message = "Strategy not found" });
                }

                strategy.IsEnabled = true;
                strategy.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { Status = "Success", Message = "Strategy enabled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enabling strategy {StrategyId}", id);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpPost("{id}/disable")]
        public async Task<IActionResult> DisableStrategy(string id)
        {
            try
            {
                var strategy = await _context.NiftyOptionStrategyConfigs
                    .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

                if (strategy == null)
                {
                    return NotFound(new { Status = "Error", Message = "Strategy not found" });
                }

                strategy.IsEnabled = false;
                strategy.LastUpdated = DateTime.Now;

                await _context.SaveChangesAsync();

                return Ok(new { Status = "Success", Message = "Strategy disabled successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disabling strategy {StrategyId}", id);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpGet("{id}/positions")]
        public async Task<IActionResult> GetStrategyPositions(string id)
        {
            try
            {
                var positions = await _context.OptionsTradePositions
                    .Where(p => p.StrategyId == id)
                    .OrderByDescending(p => p.EntryTime)
                    .ToListAsync();

                return Ok(new { Status = "Success", Data = positions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving strategy positions for {StrategyId}", id);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }

        [HttpGet("{id}/performance")]
        public async Task<IActionResult> GetStrategyPerformance(string id)
        {
            try
            {
                var positions = await _context.OptionsTradePositions
                    .Where(p => p.StrategyId == id)
                    .ToListAsync();

                var performance = new
                {
                    TotalTrades = positions.Count,
                    OpenPositions = positions.Count(p => p.Status == "OPEN"),
                    ClosedPositions = positions.Count(p => p.Status == "CLOSED"),
                    TotalPnL = positions.Sum(p => p.PnL),
                    WinningTrades = positions.Count(p => p.PnL > 0),
                    LosingTrades = positions.Count(p => p.PnL < 0),
                    MaxProfit = positions.Any() ? positions.Max(p => p.PnL) : 0,
                    MaxLoss = positions.Any() ? positions.Min(p => p.PnL) : 0,
                    AveragePnL = positions.Any() ? positions.Average(p => p.PnL) : 0,
                    WinRate = positions.Any() ? (double)positions.Count(p => p.PnL > 0) / positions.Count * 100 : 0
                };

                return Ok(new { Status = "Success", Data = performance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving strategy performance for {StrategyId}", id);
                return BadRequest(new { Status = "Error", Message = ex.Message });
            }
        }
    }
}