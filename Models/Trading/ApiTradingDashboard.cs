using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KiteConnectApi.Models.Trading
{
    [Table("ApiTradeLog")]
    public class ApiTradeLog
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        [Required]
        [MaxLength(20)]
        public string WeekStartDate { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(10)]
        public string SignalId { get; set; } = string.Empty;
        
        [Required]
        public int Direction { get; set; } // 1 for Bullish, -1 for Bearish
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal StopLoss { get; set; }
        
        [Required]
        public DateTime EntryTime { get; set; }
        
        [Required]
        [MaxLength(10)]
        public string Outcome { get; set; } = "OPEN"; // "OPEN", "WIN", "LOSS"
        
        public DateTime? ExitTime { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string TradingSymbol { get; set; } = string.Empty;
        
        [Required]
        public int Strike { get; set; }
        
        [Required]
        [MaxLength(2)]
        public string OptionType { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal EntryPrice { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? ExitPrice { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? PnL { get; set; }
        
        [Required]
        public int Quantity { get; set; } = 1;
        
        [Required]
        [MaxLength(10)]
        public string ExpiryDay { get; set; } = "Thursday";
        
        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? UpdatedAt { get; set; }
        
        [MaxLength(500)]
        public string? Notes { get; set; }
        
        [Column(TypeName = "decimal(8,4)")]
        public decimal Confidence { get; set; } = 1.0m;
        
        [MaxLength(20)]
        public string Source { get; set; } = "API";
    }

    public class ApiSignalStats
    {
        public string Id { get; set; } = string.Empty;
        public int TotalTrades { get; set; } = 0;
        public int Wins { get; set; } = 0;
        public int Losses { get; set; } = 0;
        public decimal TotalPnL { get; set; } = 0;
        public double WinRate { get; set; } = 0;
        public decimal AveragePnL { get; set; } = 0;
        public decimal MaxWin { get; set; } = 0;
        public decimal MaxLoss { get; set; } = 0;
        public int ConsecutiveWins { get; set; } = 0;
        public int ConsecutiveLosses { get; set; } = 0;
        public List<ApiTradeLog> Trades { get; set; } = new();
    }

    public class ApiDashboardData
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Year { get; set; }
        public string Month { get; set; } = "All";
        public List<ApiTradeLog> FilteredTrades { get; set; } = new();
        public Dictionary<string, ApiSignalStats> SignalPerformance { get; set; } = new();
        public ApiSignalStats OverallStats { get; set; } = new();
        public List<WeeklyPerformance> WeeklyBreakdown { get; set; } = new();
        public List<MonthlyPerformance> MonthlyBreakdown { get; set; } = new();
        public string NoTradesMessage { get; set; } = string.Empty;
    }

    public class WeeklyPerformance
    {
        public string WeekStartDate { get; set; } = string.Empty;
        public int TotalTrades { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal TotalPnL { get; set; }
        public double WinRate { get; set; }
        public List<string> SignalsTriggered { get; set; } = new();
    }

    public class MonthlyPerformance
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public int TotalTrades { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal TotalPnL { get; set; }
        public double WinRate { get; set; }
        public decimal AveragePnL { get; set; }
    }

    public class DashboardRequest
    {
        public int Year { get; set; } = DateTime.Now.Year;
        public string Month { get; set; } = "All"; // "All", "Jan-Apr", "May-Aug", "Sep-Dec"
        public string View { get; set; } = "Performance Summary"; // "Trade Log", "Performance Summary", "Weekly Breakdown"
        public string ExpiryDay { get; set; } = "Thursday";
        public bool ShowOpenTrades { get; set; } = true;
        public bool ShowClosedTrades { get; set; } = true;
        public string? FilterBySignal { get; set; }
        public string? FilterByOutcome { get; set; }
    }

    public class ApiAlertDetails
    {
        public string SignalId { get; set; } = string.Empty;
        public string SignalName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int Strike { get; set; }
        public string OptionType { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public int Direction { get; set; }
        public decimal StopLossPrice { get; set; }
        public decimal Confidence { get; set; }
        public string Description { get; set; } = string.Empty;
        public string TradingSymbol { get; set; } = string.Empty;
        public decimal CurrentPrice { get; set; }
        public string WeekStartDate { get; set; } = string.Empty;
        public string JsonAlert { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string ExpiryDay { get; set; } = "Thursday";
        public DateTime ExpiryDate { get; set; }
        public string BiasDirection { get; set; } = string.Empty;
        public decimal UpperZoneTop { get; set; }
        public decimal UpperZoneBottom { get; set; }
        public decimal LowerZoneTop { get; set; }
        public decimal LowerZoneBottom { get; set; }
        public string AlertMessage { get; set; } = string.Empty;
    }

    public class TradeManagementStatus
    {
        public string TradeId { get; set; } = string.Empty;
        public string SignalId { get; set; } = string.Empty;
        public string Status { get; set; } = "MONITORING"; // "MONITORING", "SL_HIT", "EXPIRED_WIN", "MANUAL_EXIT"
        public DateTime LastChecked { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal StopLossPrice { get; set; }
        public bool IsBeforeExpiry { get; set; }
        public string ExpiryDay { get; set; } = "Thursday";
        public DateTime ExpiryDate { get; set; }
        public string AlertMessage { get; set; } = string.Empty;
        public bool ShouldTriggerAlert { get; set; }
    }
}