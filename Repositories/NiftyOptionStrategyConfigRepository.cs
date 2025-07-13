using Microsoft.EntityFrameworkCore;
using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;

namespace KiteConnectApi.Repositories
{
    public class NiftyOptionStrategyConfigRepository : INiftyOptionStrategyConfigRepository
    {
        private readonly ApplicationDbContext _context;

        public NiftyOptionStrategyConfigRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<NiftyOptionStrategyConfig?> GetByIdAsync(string id)
        {
            return await _context.NiftyOptionStrategyConfigs.FindAsync(id);
        }

        public async Task<IEnumerable<NiftyOptionStrategyConfig>> GetAllAsync()
        {
            return await _context.NiftyOptionStrategyConfigs.ToListAsync();
        }

        public async Task AddAsync(NiftyOptionStrategyConfig config)
        {
            await _context.NiftyOptionStrategyConfigs.AddAsync(config);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(NiftyOptionStrategyConfig config)
        {
            _context.Entry(config).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var config = await _context.NiftyOptionStrategyConfigs.FindAsync(id);
            if (config != null)
            {
                _context.NiftyOptionStrategyConfigs.Remove(config);
                await _context.SaveChangesAsync();
            }
        }
    }
}
