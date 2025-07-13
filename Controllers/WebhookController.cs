using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Services;
using KiteConnectApi.Models.Dto;
using KiteConnectApi.Repositories;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;
        private readonly ITradingStrategyService _tradingStrategyService;
        private readonly INiftyOptionStrategyConfigRepository _strategyConfigRepository;
        private readonly IManualTradingViewAlertRepository _manualAlertRepository;

        public WebhookController(ILogger<WebhookController> logger, ITradingStrategyService tradingStrategyService, INiftyOptionStrategyConfigRepository strategyConfigRepository, IManualTradingViewAlertRepository manualAlertRepository)
        {
            _logger = logger;
            _tradingStrategyService = tradingStrategyService;
            _strategyConfigRepository = strategyConfigRepository;
            _manualAlertRepository = manualAlertRepository;
        }

        [HttpPost("tradingview")]
        public async Task<IActionResult> TradingViewAlert([FromBody] TradingViewAlert alert)
        {
            try
            {
                _logger.LogInformation($"Received TradingView alert: {JsonSerializer.Serialize(alert)}");

                var strategyConfig = (await _strategyConfigRepository.GetAllAsync())
                                 .FirstOrDefault(s => s.StrategyName == alert.StrategyName && s.IsEnabled);

                if (strategyConfig == null)
                {
                    _logger.LogWarning($"No active strategy configuration found for name: {alert.StrategyName}");
                    return NotFound($"No active strategy configuration found for name: {alert.StrategyName}");
                }

                if (strategyConfig.ExecutionMode == "Manual")
                {
                    var manualAlert = new ManualTradingViewAlert
                    {
                        StrategyName = alert.StrategyName,
                        Strike = alert.Strike,
                        Type = alert.Type,
                        Signal = alert.Signal,
                        Action = alert.Action,
                        ReceivedTime = DateTime.UtcNow,
                        IsExecuted = false
                    };
                    await _manualAlertRepository.AddAsync(manualAlert);
                    _logger.LogInformation($"Manual execution mode: Alert for {alert.StrategyName} stored for manual confirmation. ID: {manualAlert.Id}");
                    return Ok("Alert received. Manual confirmation required.");
                }
                else // Auto execution mode
                {
                    await _tradingStrategyService.ProcessTradingViewAlert(alert);
                    return Ok("Alert processed automatically.");
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing TradingView alert payload.");
                return BadRequest("Invalid JSON payload.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing TradingView alert.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("manual-alerts")]
        public async Task<ActionResult<IEnumerable<ManualTradingViewAlert>>> GetPendingManualAlerts()
        {
            var pendingAlerts = await _manualAlertRepository.GetAllPendingAsync();
            return Ok(pendingAlerts);
        }

        [HttpPost("execute-manual-alert/{id}")]
        public async Task<IActionResult> ExecuteManualAlert(string id)
        {
            var manualAlert = await _manualAlertRepository.GetByIdAsync(id);
            if (manualAlert == null)
            {
                return NotFound($"Manual alert with ID {id} not found.");
            }

            if (manualAlert.IsExecuted)
            {
                return BadRequest($"Manual alert with ID {id} has already been executed.");
            }

            // Reconstruct TradingViewAlert from ManualTradingViewAlert
            var tradingViewAlert = new TradingViewAlert
            {
                StrategyName = manualAlert.StrategyName,
                Strike = manualAlert.Strike,
                Type = manualAlert.Type,
                Signal = manualAlert.Signal,
                Action = manualAlert.Action
            };

            var strategyConfig = (await _strategyConfigRepository.GetAllAsync())
                                 .FirstOrDefault(s => s.StrategyName == tradingViewAlert.StrategyName && s.IsEnabled);

            if (strategyConfig == null)
            {
                _logger.LogWarning($"No active strategy configuration found for name: {tradingViewAlert.StrategyName}");
                return NotFound($"No active strategy configuration found for name: {tradingViewAlert.StrategyName}");
            }

            try
            {
                await _tradingStrategyService.ProcessTradingViewAlert(tradingViewAlert);
                manualAlert.IsExecuted = true;
                await _manualAlertRepository.UpdateAsync(manualAlert);
                _logger.LogInformation($"Manual alert {id} executed successfully.");
                return Ok("Manual alert executed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing manual alert {id}.");
                return StatusCode(500, $"Error executing manual alert: {ex.Message}");
            }
        }
    }
}