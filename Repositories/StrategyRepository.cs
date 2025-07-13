using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public class StrategyRepository : IStrategyRepository
    {
        private readonly ApplicationDbContext _context;

        public StrategyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Strategy>> GetAllStrategiesAsync()
        {
            return await _context.Strategies
                                 .Include(s => s.Legs)
                                 .Include(s => s.ExecutionSettings)
                                 .Include(s => s.BrokerLevelSettings)
                                 .ToListAsync();
        }

        public async Task<Strategy?> GetStrategyByIdAsync(int id)
        {
            return await _context.Strategies
                                 .Include(s => s.Legs)
                                 .Include(s => s.ExecutionSettings)
                                 .Include(s => s.BrokerLevelSettings)
                                 .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddStrategyAsync(Strategy strategy)
        {
            await _context.Strategies.AddAsync(strategy);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStrategyAsync(Strategy strategy)
        {
            _context.Strategies.Update(strategy);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteStrategyAsync(int id)
        {
            var strategy = await _context.Strategies.FindAsync(id);
            if (strategy != null)
            {
                _context.Strategies.Remove(strategy);
                await _context.SaveChangesAsync();
            }
        }
    }
}