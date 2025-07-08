// --- Services/OrderMonitoringService.cs ---
// This file had an incorrect null check for a struct.
using KiteConnectApi.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class OrderMonitoringService : BackgroundService
    {
        private readonly ILogger<OrderMonitoringService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public OrderMonitoringService(
            ILogger<OrderMonitoringService> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Order Monitoring Service running at: {time}", DateTimeOffset.Now);
                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        var kiteConnectService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();
                        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

                        var openOrders = await orderRepository.GetOpenOrdersAsync();
                        foreach (var order in openOrders)
                        {
                            var kiteOrders = await kiteConnectService.GetOrdersAsync();
                            var matchedOrder = kiteOrders.FirstOrDefault(o => o.OrderId == order.OrderId);

                            // FIXED: KiteConnect.Order is a struct, so we check a property for null/default.
                            if (matchedOrder.OrderId != null && matchedOrder.Status != order.Status)
                            {
                                order.Status = matchedOrder.Status;
                                await orderRepository.UpdateOrderAsync(order);

                                if (matchedOrder.Status == "COMPLETE")
                                {
                                    await notificationService.SendNotificationAsync("OrderExecution", $"Order {order.OrderId} for {order.TradingSymbol} is complete.", "Order Execution Update");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while monitoring orders.");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
