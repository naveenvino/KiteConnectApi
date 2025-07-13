using MediatR;

namespace KiteConnectApi.Features.Commands
{
    public record SquareOffAllPositionsCommand(string StrategyId) : IRequest<bool>;
}