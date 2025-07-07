using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string subject, string message);
    }
}
