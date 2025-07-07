using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public class PositionRepository : IPositionRepository
    {
        private readonly ApplicationDbContext _context;

        public PositionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TradePosition?> GetPositionByIdAsync(string positionId)
        {
            return await _context.TradePositions.FindAsync(positionId);
        }

        public async Task<IEnumerable<TradePosition>> GetAllPositionsAsync()
        {
            return await _context.TradePositions.ToListAsync();
        }

        public async Task AddPositionAsync(TradePosition position)
        {
            _context.TradePositions.Add(position);
            await _context.SaveChangesAsync();
        }

        public async Task UpdatePositionAsync(TradePosition position)
        {
            _context.Entry(position).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeletePositionAsync(string positionId)
        {
            var position = await _context.TradePositions.FindAsync(positionId);
            if (position != null)
            {
                _context.TradePositions.Remove(position);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<TradePosition>> GetOpenPositionsAsync()
        {
            return await _context.TradePositions.Where(p => p.Status == "Open").ToListAsync();
        }

        public async Task<IEnumerable<TradePosition>> GetPendingPositionsAsync()
        {
            return await _context.TradePositions.Where(p => p.Status == "Pending" || p.Status == "Pending Closure").ToListAsync();
        }
    }
}
