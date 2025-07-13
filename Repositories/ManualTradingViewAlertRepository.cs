using KiteConnectApi.Models.Trading;
using KiteConnectApi.Data;
using Microsoft.EntityFrameworkCore;

namespace KiteConnectApi.Repositories
{
    public interface IManualTradingViewAlertRepository
    {
        Task<ManualTradingViewAlert?> GetByIdAsync(string id);
        Task<IEnumerable<ManualTradingViewAlert>> GetAllPendingAsync();
        Task AddAsync(ManualTradingViewAlert alert);
        Task UpdateAsync(ManualTradingViewAlert alert);
        Task DeleteAsync(string id);
    }

    public class ManualTradingViewAlertRepository : IManualTradingViewAlertRepository
    {
        private readonly ApplicationDbContext _context;

        public ManualTradingViewAlertRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ManualTradingViewAlert?> GetByIdAsync(string id)
        {
            return await _context.ManualTradingViewAlerts.FindAsync(id);
        }

        public async Task<IEnumerable<ManualTradingViewAlert>> GetAllPendingAsync()
        {
            return await _context.ManualTradingViewAlerts
                                 .Where(a => !a.IsExecuted)
                                 .ToListAsync();
        }

        public async Task AddAsync(ManualTradingViewAlert alert)
        {
            await _context.ManualTradingViewAlerts.AddAsync(alert);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ManualTradingViewAlert alert)
        {
            _context.ManualTradingViewAlerts.Update(alert);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var alert = await _context.ManualTradingViewAlerts.FindAsync(id);
            if (alert != null)
            {
                _context.ManualTradingViewAlerts.Remove(alert);
                await _context.SaveChangesAsync();
            }
        }
    }
}
