using KiteConnectApi.Data;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ApplicationDbContext _context;

        public OrderRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteOrderAsync(string orderId)
        {
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
            if (order != null)
            {
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders.ToListAsync();
        }

        public async Task<Order?> GetOrderByIdAsync(string orderId)
        {
            return await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public async Task<IEnumerable<Order>> GetOpenOrdersAsync()
        {
            return await _context.Orders.Where(o => o.Status == "OPEN" || o.Status == "TRIGGER PENDING").ToListAsync();
        }

        public async Task<IEnumerable<Order>> GetOrdersBySymbolAsync(string symbol)
        {
            return await _context.Orders.Where(o => o.TradingSymbol == symbol).OrderByDescending(o => o.OrderTimestamp).ToListAsync();
        }

        public async Task UpdateOrderAsync(Order order)
        {
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
        }

        // --- FIX: Implemented the missing method ---
        public async Task<IEnumerable<Order>> GetOrdersByPositionIdAsync(string positionId)
        {
            return await _context.Orders.Where(o => o.PositionId == positionId).ToListAsync();
        }
        // --- END OF FIX ---

        public async Task<IEnumerable<Order>> GetTodaysClosedOrdersAsync()
        {
            var today = System.DateTime.UtcNow.Date;
            return await _context.Orders.Where(o => o.Status == "COMPLETE" && o.OrderTimestamp.Date == today).ToListAsync();
        }
    }
}
