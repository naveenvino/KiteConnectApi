using KiteConnectApi.Models.Trading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public class SimulatedOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = new List<Order>();
        private int _nextId = 1;

        public Task AddOrderAsync(Order order)
        {
            order.Id = _nextId++;
            order.OrderTimestamp = DateTime.UtcNow;
            _orders.Add(order);
            return Task.CompletedTask;
        }

        public Task DeleteOrderAsync(string orderId)
        {
            var order = _orders.FirstOrDefault(o => o.OrderId == orderId);
            if (order != null)
            {
                _orders.Remove(order);
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return Task.FromResult<IEnumerable<Order>>(_orders);
        }

        public Task<Order?> GetOrderByIdAsync(string orderId)
        {
            return Task.FromResult(_orders.FirstOrDefault(o => o.OrderId == orderId));
        }

        public Task<IEnumerable<Order>> GetOpenOrdersAsync()
        {
            return Task.FromResult(_orders.Where(o => o.Status == "OPEN" || o.Status == "TRIGGER PENDING"));
        }

        public Task<IEnumerable<Order>> GetOrdersBySymbolAsync(string symbol)
        {
            return Task.FromResult(_orders.Where(o => o.TradingSymbol == symbol).OrderByDescending(o => o.OrderTimestamp).AsEnumerable());
        }

        public Task UpdateOrderAsync(Order order)
        {
            var existingOrder = _orders.FirstOrDefault(o => o.Id == order.Id);
            if (existingOrder != null)
            {
                existingOrder.Status = order.Status;
            }
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Order>> GetOrdersByPositionIdAsync(string positionId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Order>> GetTodaysClosedOrdersAsync()
        {
            var today = DateTime.UtcNow.Date;
            return Task.FromResult(_orders.Where(o => o.Status == "COMPLETE" && o.OrderTimestamp.Date == today));
        }
    }
}
