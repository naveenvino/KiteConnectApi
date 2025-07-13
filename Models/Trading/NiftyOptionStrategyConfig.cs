using System.ComponentModel.DataAnnotations;
using System;

namespace KiteConnectApi.Models.Trading
{
    public class NiftyOptionStrategyConfig
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string? StrategyName { get; set; }
        public string? UnderlyingInstrument { get; set; } // Nifty or BankNifty
        public string? Exchange { get; set; }
        public string? ProductType { get; set; } // Intraday or Positional
        public int Quantity { get; set; } // Fixed quantity
        public decimal AllocatedMargin { get; set; } // For dynamic quantity calculation
        public DateTime FromDate { get; set; } // Strategy start date
        public DateTime ToDate { get; set; } // Strategy end date
        public int EntryTime { get; set; }
        public int ExitTime { get; set; }
        public decimal StopLossPercentage { get; set; }
        public decimal TargetPercentage { get; set; }
        public decimal TakeProfitPercentage { get; set; }
        public int MaxTradesPerDay { get; set; }
        public bool IsEnabled { get; set; }
        public int HedgeDistancePoints { get; set; } // For fixed points away hedge
        public decimal HedgePremiumPercentage { get; set; } // For dynamic hedge based on premium
        public string? OrderType { get; set; }
        public string? InstrumentPrefix { get; set; }
        public decimal LockProfitPercentage { get; set; } // For locking profit based on percentage
        public decimal LockProfitAmount { get; set; } // For locking profit based on amount
        public decimal TrailStopLossPercentage { get; set; } // For trailing stop loss based on percentage
        public decimal TrailStopLossAmount { get; set; } // For trailing stop loss based on amount
        public decimal OverallPositionStopLoss { get; set; } // For overall position stop loss
        public decimal MoveStopLossToEntryPricePercentage { get; set; } // For moving SL to entry price based on profit percentage
        public decimal MoveStopLossToEntryPriceAmount { get; set; } // For moving SL to entry price based on profit amount
        public decimal ExitAndReenterProfitPercentage { get; set; } // For exiting and re-entering based on profit percentage
        public decimal ExitAndReenterProfitAmount { get; set; } // For exiting and re-entering based on profit amount
        public DateTime? LastExitReentryTime { get; set; } // To prevent immediate re-entry after exit
        public string? ExecutionMode { get; set; } // "Auto" or "Manual"
        public string? EntryOrderType { get; set; } // MARKET, LIMIT, SL, SL-M
        public decimal? EntryLimitPrice { get; set; } // For LIMIT orders
    }
}
