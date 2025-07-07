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

        // --- FIX: Removed unused parameters from the method signature ---
        public async Task<bool> CanPlaceOrder()
        {
            var openPositions = await _positionRepository.GetOpenPositionsAsync();

            if (openPositions.Count() >= _riskParameters.MaxOpenPositions)
            {
                return false;
            }

            return true;
        }
        // --- END OF FIX ---

        public async Task<bool> ShouldSquareOff()
        {
            return await Task.FromResult(false);
        }
    }
}
