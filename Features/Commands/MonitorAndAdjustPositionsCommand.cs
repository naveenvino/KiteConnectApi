using MediatR;
using KiteConnectApi.Models.Trading;

namespace KiteConnectApi.Features.Commands
{
    public record MonitorAndAdjustPositionsCommand(NiftyOptionStrategyConfig Config) : IRequest<bool>;
}