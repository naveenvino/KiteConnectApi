using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public class NotificationPreferenceRepository : INotificationPreferenceRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationPreferenceRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<NotificationPreference>> GetAllNotificationPreferencesAsync()
        {
            return await _context.NotificationPreferences.ToListAsync();
        }

        public async Task<NotificationPreference?> GetNotificationPreferenceByIdAsync(string id)
        {
            return await _context.NotificationPreferences.FindAsync(id);
        }

        public async Task AddNotificationPreferenceAsync(NotificationPreference preference)
        {
            await _context.NotificationPreferences.AddAsync(preference);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateNotificationPreferenceAsync(NotificationPreference preference)
        {
            _context.Entry(preference).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task DeleteNotificationPreferenceAsync(string id)
        {
            var preference = await _context.NotificationPreferences.FindAsync(id);
            if (preference != null)
            {
                _context.NotificationPreferences.Remove(preference);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<NotificationPreference>> GetActiveNotificationPreferencesByEventTypeAsync(string eventType)
        {
            return await _context.NotificationPreferences
                .Where(p => p.IsActive && p.EventTypes.Contains(eventType))
                .ToListAsync();
        }
    }
}
