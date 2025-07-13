using Orleans;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.Dto;
using KiteConnectApi.Repositories;
using KiteConnectApi.Services;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MediatR;
using KiteConnectApi.Features.Commands;

namespace KiteConnectApi.Grains
{
    public class StrategyGrain : Grain, IStrategyGrain
    {
        private readonly INiftyOptionStrategyConfigRepository _strategyConfigRepository;
        private readonly ILogger<StrategyGrain> _logger;
        private readonly IMediator _mediator;

        private NiftyOptionStrategyConfig? _config;

        public StrategyGrain(
            INiftyOptionStrategyConfigRepository strategyConfigRepository,
            ILogger<StrategyGrain> logger,
            IMediator mediator)
        {
            _strategyConfigRepository = strategyConfigRepository;
            _logger = logger;
            _mediator = mediator;
        }

        public override async Task OnActivateAsync(CancellationToken cancellationToken)
        {
            // Load strategy config on activation
            var configId = this.GetPrimaryKeyString();
            _config = await _strategyConfigRepository.GetByIdAsync(configId);
            if (_config == null)
            {
                _logger.LogError($"Strategy config not found for grain {configId}");
            }
            await base.OnActivateAsync(cancellationToken);
        }

        public async Task SetStrategyConfig(string configId)
        {
            _config = await _strategyConfigRepository.GetByIdAsync(configId);
            if (_config == null)
            {
                _logger.LogError($"Strategy config not found for grain {configId}");
            }
        }

        public async Task ProcessTradingViewAlert(string alertJson)
        {
            if (_config == null)
            {
                _logger.LogWarning($"Cannot process alert: Strategy config not loaded for grain {this.GetPrimaryKeyString()}");
                return;
            }

            var alert = JsonSerializer.Deserialize<TradingViewAlert>(alertJson);
            if (alert == null)
            {
                _logger.LogError("Failed to deserialize TradingViewAlert.");
                return;
            }

            if (alert.Action == "Entry")
            {
                await _mediator.Send(new PlaceEntryOrderCommand(alert, _config));
            }
            else if (alert.Action == "Stoploss")
            {
                await _mediator.Send(new SquareOffPositionCommand(alert, _config));
            }
            else
            {
                _logger.LogWarning($"Unknown action received in TradingView alert: {alert.Action}");
            }
        }

        public async Task SquareOffAllPositions()
        {
            if (_config == null)
            {
                _logger.LogWarning($"Cannot square off positions: Strategy config not loaded for grain {this.GetPrimaryKeyString()}");
                return;
            }
            await _mediator.Send(new SquareOffAllPositionsCommand(_config.Id));
        }

        public async Task MonitorAndAdjustPositions()
        {
            if (_config == null)
            {
                _logger.LogWarning($"Cannot monitor positions: Strategy config not loaded for grain {this.GetPrimaryKeyString()}");
                return;
            }
            await _mediator.Send(new MonitorAndAdjustPositionsCommand(_config));
        }
    }
}