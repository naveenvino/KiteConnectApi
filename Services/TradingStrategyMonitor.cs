using KiteConnectApi.Models.Trading;
using KiteConnectApi.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KiteConnectApi.Services
{
    public class TradingStrategyMonitor : BackgroundService
    {
        private readonly ILogger<TradingStrategyMonitor> _logger;
        private readonly IServiceScopeFactory _scopeFactory;

        public TradingStrategyMonitor(ILogger<TradingStrategyMonitor> logger, IServiceScopeFactory scopeFactory)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken); // Check every 60 seconds
                _logger.LogInformation("Trading Strategy Monitor running at: {time}", DateTimeOffset.Now);

                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var positionRepository = scope.ServiceProvider.GetRequiredService<IPositionRepository>();
                        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
                        var kiteConnectService = scope.ServiceProvider.GetRequiredService<IKiteConnectService>();
                        var strategyService = scope.ServiceProvider.GetRequiredService<StrategyService>();
                        var strategyConfig = scope.ServiceProvider.GetRequiredService<IOptions<NiftyOptionStrategyConfig>>().Value;

                        var openPositions = await positionRepository.GetOpenPositionsAsync();

                        foreach (var position in openPositions)
                        {
                            if (position.PositionId == null) continue;

                            var orders = (await orderRepository.GetOrdersByPositionIdAsync(position.PositionId)).ToList();
                            var mainOrder = orders.FirstOrDefault(o => o.TransactionType == "SELL");
                            var hedgeOrder = orders.FirstOrDefault(o => o.TransactionType == "BUY");

                            if (mainOrder == null || hedgeOrder == null) continue;

                            // Get current prices
                            var quotes = await kiteConnectService.GetQuotesAsync(new[] { mainOrder.TradingSymbol, hedgeOrder.TradingSymbol });
                            var mainQuote = quotes[mainOrder.TradingSymbol];
                            var hedgeQuote = quotes[hedgeOrder.TradingSymbol];

                            // Calculate P&L
                            decimal initialCredit = mainOrder.Price - hedgeOrder.Price;
                            decimal currentCredit = mainQuote.LastPrice - hedgeQuote.LastPrice;
                            decimal pnl = initialCredit - currentCredit;
                            decimal pnlPercentage = (pnl / initialCredit) * 100;

                            _logger.LogInformation("Position {PositionId} P&L Check: Initial Credit={Initial}, Current Credit={Current}, P&L={PnL}, P&L%={PnlPercent}", position.PositionId, initialCredit, currentCredit, pnl, pnlPercentage);

                            // Check stop-loss
                            if (pnlPercentage <= -strategyConfig.StopLossPercentage)
                            {
                                _logger.LogWarning("AUTOMATED STOP-LOSS TRIGGERED for Position {PositionId}. P&L: {PnlPercent}%", position.PositionId, pnlPercentage);
                                await strategyService.ExitPositionByIdAsync(position.PositionId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred in the Trading Strategy Monitor.");
                }
            }
        }
    }
}
