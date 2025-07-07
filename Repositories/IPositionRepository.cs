using KiteConnectApi.Models.Trading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public interface IPositionRepository
    {
        Task<TradePosition?> GetPositionByIdAsync(string positionId);
        Task<IEnumerable<TradePosition>> GetAllPositionsAsync();
        Task AddPositionAsync(TradePosition position);
        Task UpdatePositionAsync(TradePosition position);
        Task DeletePositionAsync(string positionId);
        Task<IEnumerable<TradePosition>> GetOpenPositionsAsync();
        Task<IEnumerable<TradePosition>> GetPendingPositionsAsync();
        Task<TradePosition?> GetOpenPositionBySignalAsync(string signal);
    }
}
