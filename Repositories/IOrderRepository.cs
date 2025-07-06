using KiteConnectApi.Models.Trading;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public interface IOrderRepository
    {
        Task<Order> GetOrderByIdAsync(string orderId);
        Task<IEnumerable<Order>> GetOrdersByPositionIdAsync(string positionId);
        Task AddOrderAsync(Order order);
        Task UpdateOrderAsync(Order order);
        Task DeleteOrderAsync(string orderId);
    }
}
