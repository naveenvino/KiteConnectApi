using KiteConnectApi.Models.Enums;
using KiteConnectApi.Repositories;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class NotificationService : INotificationService
    {
        private readonly INotificationPreferenceRepository _notificationPreferenceRepository;
        private readonly EmailNotificationService _emailNotificationService;
        // private readonly SmsNotificationService _smsNotificationService; // Future
        // private readonly TelegramNotificationService _telegramNotificationService; // Future
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationPreferenceRepository notificationPreferenceRepository,
            EmailNotificationService emailNotificationService,
            ILogger<NotificationService> logger)
        {
            _notificationPreferenceRepository = notificationPreferenceRepository;
            _emailNotificationService = emailNotificationService;
            _logger = logger;
        }

        public async Task SendNotificationAsync(string eventType, string subject, string message)
        {
            var activePreferences = await _notificationPreferenceRepository.GetActiveNotificationPreferencesByEventTypeAsync(eventType);

            foreach (var preference in activePreferences)
            {
                switch (preference.Channel)
                {
                    case NotificationChannel.Email:
                        await _emailNotificationService.SendNotificationAsync(subject, message);
                        break;
                    case NotificationChannel.Sms:
                        // await _smsNotificationService.SendSmsAsync(preference.Destination, message); // Future
                        _logger.LogWarning($"SMS notification not implemented yet for {preference.Destination}");
                        break;
                    case NotificationChannel.Telegram:
                        // await _telegramNotificationService.SendTelegramMessageAsync(preference.Destination, message); // Future
                        _logger.LogWarning($"Telegram notification not implemented yet for {preference.Destination}");
                        break;
                    default:
                        _logger.LogWarning($"Unknown notification channel: {preference.Channel}");
                        break;
                }
            }
        }

        // Implement the INotificationService interface method, which will be used by other services
        public async Task SendNotificationAsync(string subject, string message)
        {
            // This method will be used for general notifications without specific event types
            // For now, it will just send to all active email preferences.
            var emailPreferences = await _notificationPreferenceRepository.GetAllNotificationPreferencesAsync();
            foreach (var preference in emailPreferences)
            {
                if (preference.IsActive && preference.Channel == NotificationChannel.Email)
                {
                    await _emailNotificationService.SendNotificationAsync(subject, message);
                }
            }
        }
    }
}
