using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class SmsNotificationService : INotificationService
    {
        private readonly ILogger<SmsNotificationService> _logger;

        public SmsNotificationService(ILogger<SmsNotificationService> logger)
        {
            _logger = logger;
        }

        public Task SendNotificationAsync(string subject, string message)
        {
            return SendNotificationAsync("General", subject, message);
        }

        public Task SendNotificationAsync(string eventType, string subject, string message)
        {
            _logger.LogInformation($"Simulating SMS notification for event '{eventType}': Subject='{subject}', Message='{message}'");
            // In a real application, integrate with an SMS gateway (e.g., Twilio, Nexmo)
            return Task.CompletedTask;
        }
    }
}
