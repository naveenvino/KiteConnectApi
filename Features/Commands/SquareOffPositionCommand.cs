using MediatR;
using KiteConnectApi.Models.Dto;
using KiteConnectApi.Models.Trading;

namespace KiteConnectApi.Features.Commands
{
    public record SquareOffPositionCommand(TradingViewAlert Alert, NiftyOptionStrategyConfig Config) : IRequest<bool>;
}