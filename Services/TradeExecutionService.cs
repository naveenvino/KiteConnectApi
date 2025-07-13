using KiteConnectApi.Data;
using KiteConnectApi.Models.Dto;
using KiteConnectApi.Models.Trading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using KiteConnect;
using MediatR;
using KiteConnectApi.Features.Commands;

namespace KiteConnectApi.Services
{
    public class TradeExecutionService : ITradeExecutionService
    {
        private readonly IMediator _mediator;

        public TradeExecutionService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task HandleEntryAlert(TradingViewAlert alert, NiftyOptionStrategyConfig config)
        {
            await _mediator.Send(new PlaceEntryOrderCommand(alert, config));
        }

        public async Task HandleStopLossAlert(TradingViewAlert alert, NiftyOptionStrategyConfig config)
        {
            await _mediator.Send(new SquareOffPositionCommand(alert, config));
        }

        public async Task SquareOffAllPositions(string strategyId)
        {
            await _mediator.Send(new SquareOffAllPositionsCommand(strategyId));
        }

        public async Task MonitorAndAdjustPositionsForStrategy(NiftyOptionStrategyConfig config)
        {
            await _mediator.Send(new MonitorAndAdjustPositionsCommand(config));
        }
    }
}