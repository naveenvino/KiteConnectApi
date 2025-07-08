using KiteConnectApi.Models.Trading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public interface IStrategyConfigRepository
    {
        Task<IEnumerable<StrategyConfig>> GetAllStrategyConfigsAsync();
        Task<StrategyConfig?> GetStrategyConfigByIdAsync(string id);
        Task AddStrategyConfigAsync(StrategyConfig strategyConfig);
        Task UpdateStrategyConfigAsync(StrategyConfig strategyConfig);
        Task DeleteStrategyConfigAsync(string id);
    }
}
