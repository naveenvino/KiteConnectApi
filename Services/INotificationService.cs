using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string subject, string message);
        Task SendNotificationAsync(string eventType, string subject, string message);
    }
}
