using KiteConnectApi.Models.Trading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public interface IStrategyRepository
    {
        Task<IEnumerable<Strategy>> GetAllStrategiesAsync();
        Task<Strategy?> GetStrategyByIdAsync(int id);
        Task AddStrategyAsync(Strategy strategy);
        Task UpdateStrategyAsync(Strategy strategy);
        Task DeleteStrategyAsync(int id);
    }
}