using KiteConnectApi.Models.Trading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public interface INotificationPreferenceRepository
    {
        Task<IEnumerable<NotificationPreference>> GetAllNotificationPreferencesAsync();
        Task<NotificationPreference?> GetNotificationPreferenceByIdAsync(string id);
        Task AddNotificationPreferenceAsync(NotificationPreference preference);
        Task UpdateNotificationPreferenceAsync(NotificationPreference preference);
        Task DeleteNotificationPreferenceAsync(string id);
        Task<IEnumerable<NotificationPreference>> GetActiveNotificationPreferencesByEventTypeAsync(string eventType);
    }
}
