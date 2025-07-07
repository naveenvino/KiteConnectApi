using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using KiteConnectApi.Models.Dto;
using System.Collections.Generic;

namespace KiteConnectApi.Hubs
{
    public class MarketDataHub : Hub
    {
        public async Task SubscribeToPnl()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "PNL_Subscribers");
            await Clients.Caller.SendAsync("ReceiveMessage", "Subscribed to PnL updates.");
        }

        public async Task UnsubscribeFromPnl()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "PNL_Subscribers");
            await Clients.Caller.SendAsync("ReceiveMessage", "Unsubscribed from PnL updates.");
        }

        public async Task BroadcastPnlUpdate(List<PositionPnlDto> pnlUpdates)
        {
            await Clients.Group("PNL_Subscribers").SendAsync("ReceivePnlUpdate", pnlUpdates);
        }
    }
}
