using KiteConnectApi.Models.Trading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public class SimulatedPositionRepository : IPositionRepository
    {
        private readonly List<TradePosition> _positions;

        public SimulatedPositionRepository()
        {
            _positions = new List<TradePosition>();
        }

        public Task<TradePosition?> GetPositionByIdAsync(string positionId)
        {
            return Task.FromResult(_positions.FirstOrDefault(p => p.PositionId == positionId));
        }

        public Task<IEnumerable<TradePosition>> GetAllPositionsAsync()
        {
            return Task.FromResult<IEnumerable<TradePosition>>(_positions);
        }

        public Task AddPositionAsync(TradePosition position)
        {
            _positions.Add(position);
            return Task.CompletedTask;
        }

        public Task UpdatePositionAsync(TradePosition position)
        {
            var existingPosition = _positions.FirstOrDefault(p => p.PositionId == position.PositionId);
            if (existingPosition != null)
            {
                _positions.Remove(existingPosition);
                _positions.Add(position);
            }
            return Task.CompletedTask;
        }

        public Task DeletePositionAsync(string positionId)
        {
            _positions.RemoveAll(p => p.PositionId == positionId);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<TradePosition>> GetOpenPositionsAsync()
        {
            return Task.FromResult<IEnumerable<TradePosition>>(_positions.Where(p => p.Status == "Open"));
        }

        public Task<IEnumerable<TradePosition>> GetPendingPositionsAsync()
        {
            return Task.FromResult<IEnumerable<TradePosition>>(_positions.Where(p => p.Status == "Pending" || p.Status == "Pending Closure"));
        }

        public void Clear()
        {
            _positions.Clear();
        }
    }
}
