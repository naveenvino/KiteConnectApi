using KiteConnectApi.Models.Trading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public interface IScreenerCriteriaRepository
    {
        Task<IEnumerable<ScreenerCriteria>> GetAllScreenerCriteriasAsync();
        Task<ScreenerCriteria?> GetScreenerCriteriaByIdAsync(string id);
        Task AddScreenerCriteriaAsync(ScreenerCriteria criteria);
        Task UpdateScreenerCriteriaAsync(ScreenerCriteria criteria);
        Task DeleteScreenerCriteriaAsync(string id);
    }
}
