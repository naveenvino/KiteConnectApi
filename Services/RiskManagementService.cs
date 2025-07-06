using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<RiskManagementService> _logger;

        public RiskManagementService(
            IPositionRepository positionRepository,
            IOrderRepository orderRepository,
            IOptions<RiskParameters> riskParameters,
            ILogger<RiskManagementService> logger)
        {
            _positionRepository = positionRepository;
            _orderRepository = orderRepository;
            _riskParameters = riskParameters.Value;
            _logger = logger;
        }

        public async Task<bool> CheckTradeAllowed(string tradeType, decimal potentialLoss = 0)
        {
            // Check max open positions
            var openPositions = await _positionRepository.GetOpenPositionsAsync();
            if (openPositions.Count() >= _riskParameters.MaxOpenPositions)
            {
                _logger.LogWarning($"Trade not allowed: Max open positions ({_riskParameters.MaxOpenPositions}) reached.");
                return false;
            }

            // Implement more sophisticated risk checks here, e.g., daily loss, per-trade loss
            // For now, a simple check on open positions.

            _logger.LogInformation($"Trade allowed: Current open positions: {openPositions.Count()}");
            return true;
        }
    }
}
