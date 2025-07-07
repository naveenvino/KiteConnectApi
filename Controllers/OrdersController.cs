using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IKiteConnectService _kiteConnectService;

        public OrdersController(IKiteConnectService kiteConnectService)
        {
            _kiteConnectService = kiteConnectService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _kiteConnectService.GetOrdersAsync();
            return Ok(orders);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderParams orderParams)
        {
            var order = await _kiteConnectService.PlaceOrderAsync(
                orderParams.Exchange,
                orderParams.TradingSymbol,
                orderParams.TransactionType,
                orderParams.Quantity,
                orderParams.Product,
                orderParams.OrderType,
                orderParams.Price);

            return Ok(order);
        }

        [HttpPut("{orderId}")]
        public async Task<IActionResult> ModifyOrder(string orderId, [FromBody] ModifyOrderParams orderParams)
        {
            var result = await _kiteConnectService.ModifyOrderAsync(
                orderId,
                quantity: orderParams.Quantity,
                price: orderParams.Price,
                order_type: orderParams.OrderType
            );
            return Ok(result);
        }

        [HttpDelete("{orderId}")]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            var result = await _kiteConnectService.CancelOrderAsync(orderId);
            return Ok(result);
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