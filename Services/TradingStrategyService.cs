using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using KiteConnectApi.Data;
using Microsoft.EntityFrameworkCore;
using KiteConnectApi.Models.Enums;
using KiteConnectApi.Models.Dto;
using MediatR;
using KiteConnectApi.Features.Commands;

namespace KiteConnectApi.Services
{
    public class TradingStrategyService : ITradingStrategyService
    {
        private readonly INiftyOptionStrategyConfigRepository _strategyConfigRepository;
        private readonly ILogger<TradingStrategyService> _logger;
        private readonly IMediator _mediator;

        public TradingStrategyService(
            INiftyOptionStrategyConfigRepository strategyConfigRepository,
            ILogger<TradingStrategyService> logger,
            IMediator mediator)
        {
            _strategyConfigRepository = strategyConfigRepository;
            _logger = logger;
            _mediator = mediator;
        }

        public async Task ProcessTradingViewAlert(TradingViewAlert alert)
        {
            _logger.LogInformation($"Processing TradingView alert for strategy: {alert.StrategyName}, Action: {alert.Action}");

            var strategyConfig = (await _strategyConfigRepository.GetAllAsync())
                                 .FirstOrDefault(s => s.StrategyName == alert.StrategyName && s.IsEnabled);

            if (strategyConfig == null)
            {
                _logger.LogWarning($"No active strategy configuration found for name: {alert.StrategyName}");
                return;
            }

            if (alert.Action == "Entry")
            {
                await _mediator.Send(new PlaceEntryOrderCommand(alert, strategyConfig));
            }
            else if (alert.Action == "Stoploss")
            {
                await _mediator.Send(new SquareOffPositionCommand(alert, strategyConfig));
            }
            else
            {
                _logger.LogWarning($"Unknown action received in TradingView alert: {alert.Action}");
            }
        }

        public async Task SquareOffAllPositions(string strategyId)
        {
            await _mediator.Send(new SquareOffAllPositionsCommand(strategyId));
        }

        public async Task MonitorAndAdjustPositions()
        {
            // This method will remain in TradingStrategyService as it orchestrates monitoring
            // and uses the TradeExecutionService for actual adjustments.
            _logger.LogInformation("Monitoring and adjusting positions...");

            var activeStrategies = (await _strategyConfigRepository.GetAllAsync())
                                   .Where(s => s.IsEnabled && s.ToDate >= DateTime.Today)
                                   .ToList();

            foreach (var strategy in activeStrategies)
            {
                var config = await _strategyConfigRepository.GetByIdAsync(strategy.Id);
                if (config == null)
                {
                    _logger.LogWarning($"Strategy configuration not found for ID: {strategy.Id}");
                    return;
                }

                // Delegate to TradeExecutionService for monitoring and adjustments
                await _mediator.Send(new MonitorAndAdjustPositionsCommand(config));
            }
            _logger.LogInformation("Position monitoring and adjustment complete.");
        }
    }
}