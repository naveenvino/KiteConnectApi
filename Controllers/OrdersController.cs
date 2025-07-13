using KiteConnectApi.Repositories;
using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IKiteConnectService _kiteConnectService;
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(IKiteConnectService kiteConnectService, IOrderRepository orderRepository, ILogger<OrdersController> logger)
        {
            _kiteConnectService = kiteConnectService;
            _orderRepository = orderRepository;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            _logger.LogInformation("Fetching all orders.");
            try
            {
                var orders = await _kiteConnectService.GetOrdersAsync();
                _logger.LogInformation("Successfully fetched {Count} orders.", orders.Count);
                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all orders.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetOrderHistory()
        {
            _logger.LogInformation("Fetching order history.");
            try
            {
                var allOrders = await _orderRepository.GetAllOrdersAsync();
                _logger.LogInformation("Successfully fetched {Count} historical orders.", allOrders.Count());
                return Ok(allOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order history.");
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderParams orderParams)
        {
            _logger.LogInformation($"Placing new order: Symbol={orderParams.TradingSymbol}, Type={orderParams.TransactionType}, Qty={orderParams.Quantity}");
            try
            {
                var order = await _kiteConnectService.PlaceOrderAsync(
                    orderParams.Exchange,
                    orderParams.TradingSymbol,
                    orderParams.TransactionType,
                    orderParams.Quantity,
                    orderParams.Product,
                    orderParams.OrderType,
                    orderParams.Price);

                _logger.LogInformation($"Order placed successfully. OrderId: {order["order_id"]}");
                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing order: Symbol={Symbol}, Type={Type}, Qty={Qty}", orderParams.TradingSymbol, orderParams.TransactionType, orderParams.Quantity);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpPut("{orderId}")]
        public async Task<IActionResult> ModifyOrder(string orderId, [FromBody] ModifyOrderParams orderParams)
        {
            _logger.LogInformation("Modifying order: OrderId={OrderId}, Qty={Qty}, Price={Price}", orderId, orderParams.Quantity, orderParams.Price);
            try
            {
                var result = await _kiteConnectService.ModifyOrderAsync(
                    orderId,
                    quantity: orderParams.Quantity,
                    price: orderParams.Price,
                    order_type: orderParams.OrderType
                );
                _logger.LogInformation("Order modified successfully. OrderId: {OrderId}", orderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error modifying order: OrderId={OrderId}", orderId);
                return StatusCode(500, "Internal server error.");
            }
        }

        [HttpDelete("{orderId}")]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            _logger.LogInformation("Cancelling order: OrderId={OrderId}", orderId);
            try
            {
                var result = await _kiteConnectService.CancelOrderAsync(orderId);
                _logger.LogInformation("Order cancelled successfully. OrderId: {OrderId}", orderId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order: OrderId={OrderId}", orderId);
                return StatusCode(500, "Internal server error.");
            }
        }
    }

    public class PlaceOrderParams
    {
        public string? Exchange { get; set; }
        public string? TradingSymbol { get; set; }
        public string? TransactionType { get; set; }
        public int Quantity { get; set; }
        public string? Product { get; set; }
        public string? OrderType { get; set; }
        public decimal? Price { get; set; }
    }

    public class ModifyOrderParams
    {
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? TriggerPrice { get; set; }
        public string OrderType { get; set; } = "";
    }
}