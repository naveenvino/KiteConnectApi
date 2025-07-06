using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly KiteConnectService _kiteConnectService;

        public OrdersController(KiteConnectService kiteConnectService)
        {
            _kiteConnectService = kiteConnectService;
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            var orders = await _kiteConnectService.GetOrdersAsync();
            return Ok(orders);
        }

        [HttpGet("{orderId}/trades")]
        public async Task<IActionResult> GetOrderTrades(string orderId)
        {
            var trades = await _kiteConnectService.GetOrderTradesAsync(orderId);
            return Ok(trades);
        }

        [HttpPost]
        public async Task<IActionResult> PlaceOrder([FromForm] string exchange, [FromForm] string tradingsymbol, [FromForm] string transaction_type, [FromForm] int quantity, [FromForm] string product, [FromForm] string order_type, [FromForm] decimal? price)
        {
            var response = await _kiteConnectService.PlaceOrderAsync(exchange, tradingsymbol, transaction_type, quantity, product, order_type, price);
            return Ok(response);
        }

        [HttpPut("{orderId}")]
        public async Task<IActionResult> ModifyOrder(string orderId, [FromForm] string price, [FromForm] string quantity)
        {
            // Error was here: CS1503 and CS1503
            // The 'price' and 'quantity' from the form are strings and must be parsed.

            // Attempt to parse the price string to a decimal. If it's null or empty, it will be null.
            decimal? parsedPrice = !string.IsNullOrEmpty(price) ? decimal.Parse(price) : (decimal?)null;

            // Attempt to parse the quantity string to an integer. If it's null or empty, it will be null.
            int? parsedQuantity = !string.IsNullOrEmpty(quantity) ? int.Parse(quantity) : (int?)null;

            var response = await _kiteConnectService.ModifyOrderAsync(
                order_id: orderId,
                price: parsedPrice,
                quantity: parsedQuantity
            );
            return Ok(response);
        }

        [HttpDelete("{orderId}")]
        public async Task<IActionResult> CancelOrder(string orderId)
        {
            var response = await _kiteConnectService.CancelOrderAsync(orderId);
            return Ok(response);
        }
    }
}
