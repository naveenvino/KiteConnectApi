using Orleans;
using System.Threading.Tasks;

namespace KiteConnectApi.Grains
{
    public interface IStrategyGrain : IGrainWithStringKey
    {
        Task SetStrategyConfig(string configId);
        Task ProcessTradingViewAlert(string alertJson);
        Task SquareOffAllPositions();
        Task MonitorAndAdjustPositions();
    }
}