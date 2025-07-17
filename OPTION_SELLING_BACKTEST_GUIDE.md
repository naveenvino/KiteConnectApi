# Option Selling Backtest with Hedge - Complete Guide

## 🎯 **Your Strategy - CONFIRMED & IMPLEMENTED**

### **Strategy Overview**
- **NIFTY Option Selling** with protective hedge
- **8 Signals** (S1-S8) with weekly condition checks
- **Maximum 1 signal** triggers per week
- **TradingView alerts** in JSON format
- **Thursday expiry** with proper P&L calculation

### **Trade Flow**
1. **Signal Generation**: Weekly check → 1 of 8 signals triggers
2. **TradingView Alert**: `{"strike": 22500, "type": "CE", "signal": "S3", "action": "Entry"}`
3. **Option Selling**: SELL main option (receive premium)
4. **Hedge Protection**: BUY hedge option ±300 points (pay premium)
5. **Monitoring**: Watch for stop loss or Thursday expiry
6. **Exit**: Stop loss OR expiry → Calculate P&L with hedge

### **P&L Calculation (CORRECT)**
```
Main P&L = (Entry Premium Received - Exit Premium Paid) × Quantity
Hedge P&L = (Hedge Exit Price - Hedge Entry Price) × Quantity
Net P&L = Main P&L + Hedge P&L - Brokerage
```

### **Stop Loss Logic (FIXED)**
- **Stop Loss Triggers**: When main option price increases by 50% of entry premium
- **Not Always Loss**: Even if stop loss hits, could be profit due to hedge protection
- **Expiry = Profit**: If no stop loss hit, option expires worthless = keep premium

## 🚀 **How to Test**

### **Step 1: Start Application**
```bash
cd C:\Users\E1791\KiteApp\Kite
dotnet run
```

### **Step 2: Run Option Selling Backtest**
```bash
# Quick 3-week backtest
curl -k -X POST https://localhost:7000/api/OptionSellingBacktest/quick-run -H "Content-Type: application/json"

# Custom period backtest
curl -k -X POST https://localhost:7000/api/OptionSellingBacktest/run -H "Content-Type: application/json" -d "{\"fromDate\":\"2024-06-24\",\"toDate\":\"2024-07-15\",\"initialCapital\":100000,\"lotSize\":50,\"hedgePoints\":300,\"stopLossPercentage\":50}"
```

### **Step 3: Get Strategy Information**
```bash
curl -k -X GET https://localhost:7000/api/OptionSellingBacktest/strategy-info
```

### **Step 4: Get Sample Parameters**
```bash
curl -k -X GET https://localhost:7000/api/OptionSellingBacktest/sample-params
```

## 📊 **Expected Results**

### **Backtest Response Format**
```json
{
  "period": "2024-06-24 to 2024-07-15",
  "strategy": "Option Selling with Hedge",
  "results": {
    "initialCapital": 100000,
    "finalCapital": 108750,
    "totalPnL": 8750,
    "totalTrades": 3,
    "winRate": "66.67%",
    "averagePnL": 2916.67,
    "maxProfit": 4500,
    "maxLoss": -1200,
    "maxDrawdown": 1200,
    "profitFactor": 1.85
  },
  "tradeDetails": [
    {
      "signalId": "S3",
      "signalName": "Resistance Hold (Bearish)",
      "weekStart": "2024-06-24",
      "exitReason": "EXPIRY",
      "mainStrike": 22500,
      "hedgeStrike": 22800,
      "mainEntry": 120.5,
      "hedgeEntry": 45.2,
      "mainExit": 0.1,
      "hedgeExit": 0.1,
      "netPnL": 3760,
      "daysHeld": 4,
      "success": true
    }
  ]
}
```

### **Key Metrics to Watch**
- **Win Rate**: Should be 60-80% (good for option selling)
- **Average P&L**: Should be positive
- **Max Drawdown**: Should be manageable
- **Profit Factor**: Should be > 1.5

## 🔧 **Implementation Details**

### **1. Signal Processing**
- **Weekly Basis**: Each Monday, check all 8 signal conditions
- **Single Signal**: Maximum 1 signal processes per week
- **Condition Logic**: Each signal has specific technical conditions

### **2. Option Selling Execution**
- **Main Position**: SELL option at signal strike
- **Hedge Position**: BUY option at strike ± 300 points
- **Quantity**: Default 50 lots per position
- **Entry Time**: Based on signal timestamp

### **3. Position Monitoring**
- **Stop Loss**: Main option price increases by 50% of entry premium
- **Expiry**: Thursday 3:30 PM
- **Exit Logic**: First trigger (stop loss or expiry) wins

### **4. P&L Calculation**
```csharp
// Main position (SELL to open, BUY to close)
var mainPnL = (entryPremium - exitPremium) * quantity;

// Hedge position (BUY to open, SELL to close)  
var hedgePnL = (hedgeExitPrice - hedgeEntryPrice) * quantity;

// Net P&L
var netPnL = mainPnL + hedgePnL - brokerage;
```

## 📋 **API Endpoints**

| Endpoint | Method | Description |
|----------|---------|-------------|
| `/api/OptionSellingBacktest/run` | POST | Run custom backtest |
| `/api/OptionSellingBacktest/quick-run` | POST | Run 3-week quick backtest |
| `/api/OptionSellingBacktest/sample-params` | GET | Get sample parameters |
| `/api/OptionSellingBacktest/strategy-info` | GET | Get strategy explanation |

## 🎯 **Signal Types & Logic**

### **Bullish Signals (Sell PE)**
- **S1**: Bear Trap - Fake breakdown recovery
- **S2**: Support Hold - Bullish bias at support
- **S4**: Bias Failure (Bullish) - Gap up against bearish bias
- **S7**: 1H Breakout Confirmed - Pure breakout signal

### **Bearish Signals (Sell CE)**
- **S3**: Resistance Hold - Bearish bias at resistance
- **S5**: Bias Failure (Bearish) - Gap down against bullish bias
- **S6**: Weakness Confirmed - Breakdown confirmation
- **S8**: 1H Breakdown Confirmed - Pure breakdown signal

## 🔍 **Validation Points**

### **✅ Confirm These Results**
1. **Signal Generation**: 8 signals implemented with proper logic
2. **Weekly Processing**: Only 1 signal per week
3. **Option Selling**: Main position is SOLD (receive premium)
4. **Hedge Protection**: Hedge position is BOUGHT (pay premium)
5. **Stop Loss**: Triggers on price increase, not always loss
6. **Thursday Expiry**: Automatic profit if no stop loss
7. **P&L Accuracy**: Includes both main and hedge positions
8. **TradingView Format**: JSON alerts match your specification

### **🎯 Success Criteria**
- **Positive Overall P&L**: Strategy should be profitable
- **Reasonable Win Rate**: 60-80% is excellent for option selling
- **Controlled Max Drawdown**: Risk management working
- **Hedge Protection**: Limits large losses

## 📝 **Files Created**

1. **OptionSellingBacktestService.cs** - Main backtesting logic
2. **OptionSellingBacktestController.cs** - API endpoints
3. **OPTION_SELLING_BACKTEST_GUIDE.md** - This guide
4. **Updated Program.cs** - Service registration
5. **Fixed IndicatorBacktestingService.cs** - Corrected option selling logic

## 🚀 **Next Steps**

1. **Test the backtest** with your 3 weeks of data
2. **Verify P&L calculations** match your expectations
3. **Check hedge protection** is working properly
4. **Validate signal logic** for each of the 8 signals
5. **Fine-tune parameters** if needed (hedge points, stop loss %)

Your option selling strategy with hedge is now **FULLY IMPLEMENTED** and ready for comprehensive backtesting! 🎉