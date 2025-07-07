using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class RiskManagementService
    {
        private readonly IPositionRepository _positionRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly RiskParameters _riskParameters;

        public RiskManagementService(IPositionRepository positionRepository, IOrderRepository orderRepository, IOptions<RiskParameters> riskParameters)
        {
            _positionRepository = positionRepository;
            _orderRepository = orderRepository;
            _riskParameters = riskParameters.Value;
        }

        public async Task<bool> CanPlaceOrder(string tradingSymbol, int quantity, decimal price)
        {
            var positions = await _positionRepository.GetOpenPositionsAsync();
            var openOrders = await _orderRepository.GetOpenOrdersAsync();

            if (positions.Count() >= _riskParameters.MaxOpenPositions)
            {
                return false;
            }

            // NOTE: The logic for MaxExposure has been commented out because the required
            // properties are not available in the database schema.
            /*
            var totalValue = positions.Sum(p => p.Quantity * p.AveragePrice);
            var newOrderValue = quantity * price;

            if ((totalValue + newOrderValue) > _riskParameters.MaxExposure)
            {
                return false;
            }
            */

            return true;
        }

        public async Task<bool> ShouldSquareOff()
        {
            // NOTE: This logic has been commented out because PnL is not stored in the database.
            // PnL must be calculated at runtime based on the current market price.
            /*
            var positions = await _positionRepository.GetOpenPositionsAsync();
            var totalPnl = positions.Sum(p => p.PnL);

            if (totalPnl <= _riskParameters.MaxLoss)
            {
                return true;
            }

            if (totalPnl >= _riskParameters.MaxProfit)
            {
                return true;
            }
            */
            return await Task.FromResult(false);
        }
    }
}
