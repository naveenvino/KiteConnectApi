using Mapster;
using KiteConnectApi.Models.Dto;
using KiteConnectApi.Models.Trading;

namespace KiteConnectApi
{
    public static class MapsterConfig
    {
        public static void RegisterMappings()
        {
            TypeAdapterConfig<TradePosition, TradePositionDto>.NewConfig();
        }
    }
}