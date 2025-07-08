using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class StrategyManagerService
    {
        private readonly IStrategyConfigRepository _strategyConfigRepository;
        private readonly StrategyService _strategyService;
        private readonly ILogger<StrategyManagerService> _logger;

        public StrategyManagerService(
            IStrategyConfigRepository strategyConfigRepository,
            StrategyService strategyService,
            ILogger<StrategyManagerService> logger)
        {
            _strategyConfigRepository = strategyConfigRepository;
            _strategyService = strategyService;
            _logger = logger;
        }

        public async Task<IEnumerable<StrategyConfig>> GetActiveStrategiesAsync()
        {
            var allConfigs = await _strategyConfigRepository.GetAllStrategyConfigsAsync();
            return allConfigs.Where(s => s.IsActive);
        }

        public async Task LoadAndExecuteStrategiesAsync()
        {
            var activeStrategies = await GetActiveStrategiesAsync();
            foreach (var strategyConfig in activeStrategies)
            {
                _logger.LogInformation($"Executing strategy: {strategyConfig.Name}");
                // Here you would typically pass the strategyConfig to the StrategyService
                // or a specific strategy implementation based on StrategyType.
                // For now, we'll just log and assume StrategyService can handle it.
                // In a real scenario, you might have a factory pattern here.
                // await _strategyService.ExecuteStrategy(strategyConfig);
            }
        }

        public async Task AddStrategyConfigAsync(StrategyConfig config)
        {
            await _strategyConfigRepository.AddStrategyConfigAsync(config);
        }

        public async Task UpdateStrategyConfigAsync(StrategyConfig config)
        {
            await _strategyConfigRepository.UpdateStrategyConfigAsync(config);
        }

        public async Task DeleteStrategyConfigAsync(string id)
        {
            await _strategyConfigRepository.DeleteStrategyConfigAsync(id);
        }

        public async Task<StrategyConfig?> GetStrategyConfigByIdAsync(string id)
        {
            return await _strategyConfigRepository.GetStrategyConfigByIdAsync(id);
        }
    }
}
