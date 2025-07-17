using KiteConnectApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace KiteConnectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OptionSellingBacktestController : ControllerBase
    {
        private readonly OptionSellingBacktestService _backtestService;
        private readonly ILogger<OptionSellingBacktestController> _logger;

        public OptionSellingBacktestController(
            OptionSellingBacktestService backtestService,
            ILogger<OptionSellingBacktestController> logger)
        {
            _backtestService = backtestService;
            _logger = logger;
        }

        /// <summary>
        /// Run option selling backtest with hedge positions
        /// </summary>
        [HttpPost("run")]
        public async Task<ActionResult<OptionSellingBacktestResult>> RunOptionSellingBacktestAsync([FromBody] OptionSellingBacktestRequest request)
        {
            try
            {
                _logger.LogInformation("Starting option selling backtest from {FromDate} to {ToDate}", 
                    request.FromDate, request.ToDate);

                var result = await _backtestService.RunOptionSellingBacktestAsync(request);
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running option selling backtest");
                return StatusCode(500, new { error = "Failed to run option selling backtest", message = ex.Message });
            }
        }

        /// <summary>
        /// Run quick option selling backtest with last 3 weeks of data
        /// </summary>
        [HttpPost("quick-run")]
        public async Task<ActionResult<object>> RunQuickOptionSellingBacktestAsync()
        {
            try
            {
                var toDate = DateTime.Today.AddDays(-1);
                var fromDate = toDate.AddDays(-21); // 3 weeks

                var request = new OptionSellingBacktestRequest
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    InitialCapital = 100000,
                    LotSize = 50,
                    HedgePoints = 300,
                    StopLossPercentage = 50
                };

                _logger.LogInformation("Running quick option selling backtest for last 3 weeks");

                var result = await _backtestService.RunOptionSellingBacktestAsync(request);
                
                return Ok(new
                {
                    Period = $"{fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}",
                    Strategy = "Option Selling with Hedge",
                    Results = result,
                    Summary = new
                    {
                        InitialCapital = result.InitialCapital,
                        FinalCapital = result.FinalCapital,
                        TotalPnL = result.TotalPnL,
                        TotalTrades = result.TotalTrades,
                        WinRate = $"{result.WinRate:F2}%",
                        AveragePnL = result.AveragePnL,
                        MaxProfit = result.MaxProfit,
                        MaxLoss = result.MaxLoss,
                        MaxDrawdown = result.MaxDrawdown,
                        ProfitFactor = result.ProfitFactor
                    },
                    TradeDetails = result.Trades.Select(t => new
                    {
                        t.SignalId,
                        t.SignalName,
                        t.WeekStart,
                        t.ExitReason,
                        MainStrike = t.MainPosition.Strike,
                        HedgeStrike = t.HedgePosition.Strike,
                        MainEntry = t.MainPosition.EntryPrice,
                        HedgeEntry = t.HedgePosition.EntryPrice,
                        MainExit = t.MainExitPrice,
                        HedgeExit = t.HedgeExitPrice,
                        t.NetPnL,
                        t.DaysHeld,
                        t.Success
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running quick option selling backtest");
                return StatusCode(500, new { error = "Failed to run quick option selling backtest", message = ex.Message });
            }
        }

        /// <summary>
        /// Get sample parameters for option selling backtest
        /// </summary>
        [HttpGet("sample-params")]
        public ActionResult<OptionSellingBacktestRequest> GetSampleParams()
        {
            var toDate = DateTime.Today.AddDays(-1);
            var fromDate = toDate.AddDays(-21);

            var sampleRequest = new OptionSellingBacktestRequest
            {
                FromDate = fromDate,
                ToDate = toDate,
                InitialCapital = 100000,
                LotSize = 50,
                HedgePoints = 300,
                StopLossPercentage = 50
            };

            return Ok(sampleRequest);
        }

        /// <summary>
        /// Get strategy explanation
        /// </summary>
        [HttpGet("strategy-info")]
        public ActionResult<object> GetStrategyInfo()
        {
            return Ok(new
            {
                Strategy = "NIFTY Option Selling with Hedge",
                Description = "Weekly option selling strategy with protective hedge positions",
                Signals = new[]
                {
                    new { Id = "S1", Name = "Bear Trap", Type = "Bullish Entry" },
                    new { Id = "S2", Name = "Support Hold", Type = "Bullish Entry" },
                    new { Id = "S3", Name = "Resistance Hold", Type = "Bearish Entry" },
                    new { Id = "S4", Name = "Bias Failure (Bullish)", Type = "Bullish Entry" },
                    new { Id = "S5", Name = "Bias Failure (Bearish)", Type = "Bearish Entry" },
                    new { Id = "S6", Name = "Weakness Confirmed", Type = "Bearish Entry" },
                    new { Id = "S7", Name = "1H Breakout Confirmed", Type = "Bullish Entry" },
                    new { Id = "S8", Name = "1H Breakdown Confirmed", Type = "Bearish Entry" }
                },
                Logic = new
                {
                    WeeklyProcessing = "Each week, conditions are checked for all 8 signals",
                    SignalTrigger = "Maximum 1 signal can trigger per week",
                    TradingViewAlert = "JSON format: {\"strike\": 22500, \"type\": \"CE\", \"signal\": \"S3\", \"action\": \"Entry\"}",
                    OptionSelling = "Main position is SOLD (receive premium)",
                    HedgeProtection = "Hedge position is BOUGHT (pay premium) at strike ± 300 points",
                    StopLoss = "Triggered when main option price increases by 50% of entry premium",
                    ThursdayExpiry = "If no stop loss hit, position expires worthless = PROFIT",
                    PnLCalculation = "(Entry Premium - Exit Premium) + (Hedge Exit - Hedge Entry)"
                },
                RiskManagement = new
                {
                    MaxOneTradePerWeek = "Only 1 signal processes per week",
                    HedgeProtection = "300 points away from main strike",
                    StopLossLevel = "50% of entry premium",
                    WeeklyExpiry = "Thursday expiry limits time risk"
                }
            });
        }
    }
}