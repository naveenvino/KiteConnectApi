using KiteConnectApi.Repositories;
using System.Threading.Tasks;
using System.Linq;

namespace KiteConnectApi.Services
{
    public class PortfolioAllocationService
    {
        private readonly IStrategyConfigRepository _strategyConfigRepository;

        public PortfolioAllocationService(IStrategyConfigRepository strategyConfigRepository)
        {
            _strategyConfigRepository = strategyConfigRepository;
        }

        public async Task<decimal> GetTotalAllocatedCapitalAsync()
        {
            var activeStrategies = await _strategyConfigRepository.GetAllStrategyConfigsAsync();
            return activeStrategies.Where(s => s.IsActive).Sum(s => s.AllocatedCapital);
        }

        // Future: Add methods for more complex allocation logic, e.g., rebalancing
    }
}
