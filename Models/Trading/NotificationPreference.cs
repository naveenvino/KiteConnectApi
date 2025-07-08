using KiteConnectApi.Models.Enums;
using System.Collections.Generic;

namespace KiteConnectApi.Models.Trading
{
    public class NotificationPreference
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public required string UserId { get; set; } // To link to a user if authentication is implemented
        public NotificationChannel Channel { get; set; }
        public required string Destination { get; set; } // e.g., email address, phone number, Telegram chat ID
        public bool IsActive { get; set; }
        public List<string> EventTypes { get; set; } = new List<string>(); // e.g., "OrderFilled", "RiskBreach", "SignalGenerated"
    }
}
