using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class EmailNotificationService : INotificationService
    {
        private readonly ILogger<EmailNotificationService> _logger;
        private readonly IConfiguration _configuration;

        public EmailNotificationService(ILogger<EmailNotificationService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SendNotificationAsync(string subject, string message)
        {
            await SendNotificationAsync("General", subject, message); // Call the new overload with a default event type
        }

        public async Task SendNotificationAsync(string eventType, string subject, string message)
        {
            try
            {
                var smtpHost = _configuration["Notification:Email:SmtpHost"];
                var smtpPort = int.Parse(_configuration["Notification:Email:SmtpPort"]!);
                var enableSsl = bool.Parse(_configuration["Notification:Email:EnableSsl"]!);
                var username = _configuration["Notification:Email:Username"]!;
                var password = _configuration["Notification:Email:Password"]!;
                var fromAddress = _configuration["Notification:Email:FromAddress"]!;
                var toAddress = _configuration["Notification:Email:ToAddress"]!;

                using (var client = new SmtpClient(smtpHost!, smtpPort))
                {
                    client.EnableSsl = enableSsl;
                    client.Credentials = new NetworkCredential(username, password);

                    var mailMessage = new MailMessage(fromAddress, toAddress, subject, message);
                    await client.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email notification sent for event '{eventType}': {subject}");
                }
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification for event '{EventType}'.", eventType);
            }
        }
    }
}
