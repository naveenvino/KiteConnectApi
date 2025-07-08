using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public class ScreenerCriteriaRepository : IScreenerCriteriaRepository
    {
        private readonly ApplicationDbContext _context;

        public ScreenerCriteriaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ScreenerCriteria>> GetAllScreenerCriteriasAsync()
        {
            return await _context.ScreenerCriterias.ToListAsync();
        }

        public async Task<ScreenerCriteria?> GetScreenerCriteriaByIdAsync(string id)
        {
            return await _context.ScreenerCriterias.FindAsync(id);
        }

        public async Task AddScreenerCriteriaAsync(ScreenerCriteria criteria)
        {
            await _context.ScreenerCriterias.AddAsync(criteria);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateScreenerCriteriaAsync(ScreenerCriteria criteria)
        {
            _context.Entry(criteria).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteScreenerCriteriaAsync(string id)
        {
            var criteria = await _context.ScreenerCriterias.FindAsync(id);
            if (criteria != null)
            {
                _context.ScreenerCriterias.Remove(criteria);
                await _context.SaveChangesAsync();
            }
        }
    }
}
