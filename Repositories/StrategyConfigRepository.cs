using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public class StrategyConfigRepository : IStrategyConfigRepository
    {
        private readonly ApplicationDbContext _context;

        public StrategyConfigRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<StrategyConfig>> GetAllStrategyConfigsAsync()
        {
            return await _context.StrategyConfigs.ToListAsync();
        }

        public async Task<StrategyConfig?> GetStrategyConfigByIdAsync(string id)
        {
            return await _context.StrategyConfigs.FindAsync(id);
        }

        public async Task AddStrategyConfigAsync(StrategyConfig strategyConfig)
        {
            await _context.StrategyConfigs.AddAsync(strategyConfig);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStrategyConfigAsync(StrategyConfig strategyConfig)
        {
            _context.Entry(strategyConfig).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteStrategyConfigAsync(string id)
        {
            var strategyConfig = await _context.StrategyConfigs.FindAsync(id);
            if (strategyConfig != null)
            {
                _context.StrategyConfigs.Remove(strategyConfig);
                await _context.SaveChangesAsync();
            }
        }
    }
}
