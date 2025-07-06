using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using KiteConnect;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class MarketDataService
    {
        private readonly Ticker _ticker;
        private readonly IHubContext<Hubs.MarketDataHub> _hubContext;
        private readonly IConfiguration _configuration;

        public MarketDataService(IHubContext<Hubs.MarketDataHub> hubContext, IConfiguration configuration)
        {
            _hubContext = hubContext;
            _configuration = configuration;

            var apiKey = _configuration["KiteConnect:ApiKey"];
            var accessToken = _configuration["KiteConnect:AccessToken"]; // Added AccessToken retrieval  

            // Updated constructor call to include required parameters  
            _ticker = new KiteConnect.Ticker(apiKey, _configuration["KiteConnect:AccessToken"]);

            _ticker.OnConnect += () => OnConnect();
            _ticker.OnClose += () => OnClose();
            _ticker.OnError += (string message) => OnError(message);
            _ticker.OnTick += (Tick tick) => OnTick(tick);

            _ticker.Connect();
        }

        private void OnConnect()
        {
            _hubContext.Clients.All.SendAsync("ReceiveMessage", "Market data WebSocket connected.");
        }

        private void OnClose()
        {
            _hubContext.Clients.All.SendAsync("ReceiveMessage", "Market data WebSocket disconnected.");
        }

        private void OnError(string message)
        {
            _hubContext.Clients.All.SendAsync("ReceiveMessage", $"Market data WebSocket error: {message}");
        }

        private void OnTick(Tick tick)
        {
            _hubContext.Clients.All.SendAsync("ReceiveTick", tick);
        }

        public void Subscribe(int[] instrumentTokens)
        {
            _ticker.Subscribe(instrumentTokens.Select(i => (uint)i).ToArray());
        }

        public void Unsubscribe(uint[] instrumentTokens)
        {
            _ticker.UnSubscribe(instrumentTokens);
        }
    }
}
