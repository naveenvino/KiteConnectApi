using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.Dto;

namespace KiteConnectApi.Services
{
    public interface ITradeExecutionService
    {
        Task HandleEntryAlert(TradingViewAlert alert, NiftyOptionStrategyConfig config);
        Task HandleStopLossAlert(TradingViewAlert alert, NiftyOptionStrategyConfig config);
        Task SquareOffAllPositions(string strategyId);
        Task MonitorAndAdjustPositionsForStrategy(NiftyOptionStrategyConfig config);
    }
}
