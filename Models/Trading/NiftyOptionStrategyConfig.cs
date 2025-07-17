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
        public bool UseDynamicQuantity { get; set; } = false; // Use allocated margin for quantity calculation
        public DateTime FromDate { get; set; } // Strategy start date
        public DateTime ToDate { get; set; } // Strategy end date
        public int EntryTime { get; set; }
        public int ExitTime { get; set; }
        public decimal StopLossPercentage { get; set; }
        public decimal TargetPercentage { get; set; }
        public decimal TakeProfitPercentage { get; set; }
        public int MaxTradesPerDay { get; set; }
        public bool IsEnabled { get; set; }
        
        // Hedge Configuration
        public bool HedgeEnabled { get; set; } = true;
        public string HedgeType { get; set; } = "POINTS"; // POINTS or PERCENTAGE
        public int HedgeDistancePoints { get; set; } = 300; // For fixed points away hedge
        public decimal HedgePremiumPercentage { get; set; } = 30; // For dynamic hedge based on premium
        public decimal HedgeRatio { get; set; } = 1.0m; // Ratio of hedge to main position
        
        // Order Configuration
        public string? OrderType { get; set; } = "MARKET";
        public string? InstrumentPrefix { get; set; }
        public string? EntryOrderType { get; set; } = "MARKET"; // MARKET, LIMIT, SL, SL-M
        public decimal? EntryLimitPrice { get; set; } // For LIMIT orders
        public bool UseOrderProtection { get; set; } = true; // Use protective measures for market orders
        
        // Risk Management
        public decimal OverallPositionStopLoss { get; set; } // For overall position stop loss
        public decimal MaxDailyLoss { get; set; } = 0; // Maximum daily loss limit
        public decimal MaxPositionSize { get; set; } = 0; // Maximum position size limit
        
        // Profit Management
        public decimal LockProfitPercentage { get; set; } // For locking profit based on percentage
        public decimal LockProfitAmount { get; set; } // For locking profit based on amount
        public decimal TrailStopLossPercentage { get; set; } // For trailing stop loss based on percentage
        public decimal TrailStopLossAmount { get; set; } // For trailing stop loss based on amount
        public decimal MoveStopLossToEntryPricePercentage { get; set; } // For moving SL to entry price based on profit percentage
        public decimal MoveStopLossToEntryPriceAmount { get; set; } // For moving SL to entry price based on profit amount
        
        // Exit and Re-enter Logic
        public decimal ExitAndReenterProfitPercentage { get; set; } // For exiting and re-entering based on profit percentage
        public decimal ExitAndReenterProfitAmount { get; set; } // For exiting and re-entering based on profit amount
        public DateTime? LastExitReentryTime { get; set; } // To prevent immediate re-entry after exit
        public int MinReentryDelayMinutes { get; set; } = 5; // Minimum delay between exit and re-entry
        
        // Execution Mode
        public string? ExecutionMode { get; set; } = "Auto"; // "Auto" or "Manual"
        public int ManualExecutionTimeoutMinutes { get; set; } = 30; // Timeout for manual execution
        
        // Expiry Management
        public bool AutoSquareOffOnExpiry { get; set; } = true; // Auto square-off on Thursday expiry
        public int ExpirySquareOffTimeMinutes { get; set; } = 330; // Minutes before expiry to square-off (3:30 PM)
        public bool UseNearestWeeklyExpiry { get; set; } = true; // Use nearest weekly expiry
        
        // Signal Configuration
        public string? AllowedSignals { get; set; } = "S1,S2,S3,S4,S5,S6,S7,S8"; // Comma-separated allowed signals
        public bool AllowMultipleSignals { get; set; } = false; // Allow multiple signals simultaneously
        
        // Notifications
        public bool NotifyOnEntry { get; set; } = true;
        public bool NotifyOnExit { get; set; } = true;
        public bool NotifyOnStopLoss { get; set; } = true;
        public bool NotifyOnProfit { get; set; } = true;
        
        // Audit Fields
        public DateTime CreatedTime { get; set; } = DateTime.Now;
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public string? CreatedBy { get; set; }
        public string? LastUpdatedBy { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? Notes { get; set; }
    }
}
