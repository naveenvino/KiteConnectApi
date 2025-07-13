using KiteConnectApi.Models.Trading;

namespace KiteConnectApi.Repositories
{
    public interface INiftyOptionStrategyConfigRepository
    {
        Task<NiftyOptionStrategyConfig?> GetByIdAsync(string id);
        Task<IEnumerable<NiftyOptionStrategyConfig>> GetAllAsync();
        Task AddAsync(NiftyOptionStrategyConfig config);
        Task UpdateAsync(NiftyOptionStrategyConfig config);
        Task DeleteAsync(string id);
    }
}
