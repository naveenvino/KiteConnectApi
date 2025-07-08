using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class TelegramNotificationService : INotificationService
    {
        private readonly ILogger<TelegramNotificationService> _logger;

        public TelegramNotificationService(ILogger<TelegramNotificationService> logger)
        {
            _logger = logger;
        }

        public Task SendNotificationAsync(string subject, string message)
        {
            return SendNotificationAsync("General", subject, message);
        }

        public Task SendNotificationAsync(string eventType, string subject, string message)
        {
            _logger.LogInformation($"Simulating Telegram notification for event '{eventType}': Subject='{subject}', Message='{message}'");
            // In a real application, integrate with Telegram Bot API
            return Task.CompletedTask;
        }
    }
}
