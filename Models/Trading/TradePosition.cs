using System;
using KiteConnect;
using System.ComponentModel.DataAnnotations;

namespace KiteConnectApi.Models.Trading;

public class TradePosition
{
    [Key]
    public string PositionId { get; set; } = Guid.NewGuid().ToString();
    public string Status { get; set; } = "Open"; // Open, Closed, StopLossExecuted, Error
    public DateTime EntryTime { get; set; } = DateTime.UtcNow;
    public DateTime? ExitTime { get; set; }

    // Entry Leg (Sold CE)
    public int EntryInstrumentToken { get; set; }
    public string EntryTradingSymbol { get; set; }
    public string EntryOrderId { get; set; }
    public double EntryPrice { get; set; }

    // Hedge Leg (Bought CE)
    public int HedgeInstrumentToken { get; set; }
    public string HedgeTradingSymbol { get; set; }
    public string HedgeOrderId { get; set; }
    public double HedgePrice { get; set; }

    public int Quantity { get; set; }
    public int Strike { get; set; }
        public required string OptionType { get; set; }
    public DateTime Expiry { get; set; }

    // You might want to add P&L tracking here as well
}
