using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace KiteConnectApi.Hubs
{
    public class MarketDataHub : Hub
    {
        public async Task Subscribe(int[] instrumentTokens)
        {
            // This method will be called by clients to subscribe to instruments
            // The actual subscription logic will be in MarketDataService
            await Clients.Caller.SendAsync("ReceiveMessage", "Subscribed to instruments: " + string.Join(", ", instrumentTokens));
        }

        public async Task Unsubscribe(int[] instrumentTokens)
        {
            // This method will be called by clients to unsubscribe from instruments
            // The actual unsubscription logic will be in MarketDataService
            await Clients.Caller.SendAsync("ReceiveMessage", "Unsubscribed from instruments: " + string.Join(", ", instrumentTokens));
        }
    }
}
