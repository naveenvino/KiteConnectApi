using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.Dto;

namespace KiteConnectApi.Services
{
    public interface ITradingStrategyService
    {
        Task ProcessTradingViewAlert(TradingViewAlert alert);
        Task SquareOffAllPositions(string strategyId);
        Task MonitorAndAdjustPositions();
    }
}
