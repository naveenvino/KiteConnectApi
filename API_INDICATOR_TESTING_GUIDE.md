# API Indicator Backtesting - Complete Testing Guide

## Prerequisites

1. **Application Status**: KiteConnectApi.exe should be running (Process ID: 33740)
2. **Port**: Application runs on HTTPS port 7000
3. **Database**: SQL Server LocalDB should be accessible
4. **Historical Data**: 3 weeks of options data should be stored

## Step-by-Step Testing Process

### Step 1: Verify Application is Running

```bash
# Check if application process is running
tasklist | findstr KiteConnect

# Should show: KiteConnectApi.exe with process ID
```

### Step 2: Test Basic API Connectivity

```bash
# Test if API is responding (should return some content)
curl -k https://localhost:7000/swagger

# Test a simple endpoint
curl -k -H "Content-Type: application/json" https://localhost:7000/api/HistoricalData/sample-params
```

### Step 3: Test Historical Data Availability

```bash
# Check historical data coverage
curl -k -X GET https://localhost:7000/api/HistoricalData/coverage

# Test price data retrieval
curl -k -X GET "https://localhost:7000/api/HistoricalData/test-price?strike=24000&optionType=CE"
```

### Step 4: Test Current API Signals

```bash
# Get current signals from API indicator
curl -k -X GET https://localhost:7000/api/IndicatorBacktest/current-signals

# Alternative endpoint
curl -k -X GET https://localhost:7000/api/ApiTradingDashboard/current-signals
```

### Step 5: Test API Indicator Backtest (Main Feature)

```bash
# Run quick 3-week backtest
curl -k -X POST https://localhost:7000/api/IndicatorBacktest/quick-backtest \
  -H "Content-Type: application/json"

# Run custom backtest with specific parameters
curl -k -X POST https://localhost:7000/api/IndicatorBacktest/run-api-backtest \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2024-06-24",
    "toDate": "2024-07-15",
    "quantity": 50,
    "symbol": "NIFTY"
  }'
```

### Step 6: Test API vs TradingView Comparison

```bash
# Compare API signals with TradingView signals
curl -k -X POST https://localhost:7000/api/IndicatorBacktest/compare-signals \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2024-06-24",
    "toDate": "2024-07-15",
    "quantity": 50,
    "symbol": "NIFTY"
  }'
```

### Step 7: Test Live Dashboard

```bash
# Get live dashboard with current signals and performance
curl -k -X GET https://localhost:7000/api/ApiTradingDashboard/live-dashboard

# Get recent alerts
curl -k -X GET https://localhost:7000/api/ApiTradingDashboard/recent-alerts

# Get performance summary
curl -k -X GET https://localhost:7000/api/ApiTradingDashboard/performance-summary
```

### Step 8: Test Signal Generation

```bash
# Test signal generation
curl -k -X POST https://localhost:7000/api/ApiTradingDashboard/test-signals \
  -H "Content-Type: application/json" \
  -d '{
    "symbol": "NIFTY",
    "includeHistory": true
  }'
```

## Expected Results

### 1. Current Signals Response
```json
{
  "timestamp": "2025-01-17T15:30:00Z",
  "symbol": "NIFTY",
  "signalCount": 2,
  "signals": [
    {
      "signalId": "S1",
      "signalName": "Bear Trap",
      "direction": 1,
      "strikePrice": 24000,
      "optionType": "PE",
      "confidence": 0.8,
      "description": "Triggers after fake breakdown..."
    }
  ]
}
```

### 2. Quick Backtest Response
```json
{
  "period": "2024-06-24 to 2024-07-15",
  "totalDays": 21,
  "results": {
    "source": "API",
    "totalSignals": 24,
    "totalTrades": 18,
    "winRate": 66.67,
    "totalPnL": 8750.0,
    "averagePnL": 486.11,
    "sharpeRatio": 1.42
  },
  "signalBreakdown": [
    {
      "signalId": "S1",
      "totalTrades": 3,
      "winRate": 66.67,
      "totalPnL": 1250.0
    }
  ]
}
```

## Troubleshooting

### Common Issues

1. **Connection Refused (Port 7000)**
   ```bash
   # Restart application
   taskkill /PID 33740 /F
   cd C:\Users\E1791\KiteApp\Kite
   dotnet run
   ```

2. **Service Registration Error**
   - Check Program.cs for service registration
   - Ensure all services are properly registered

3. **Database Connection Error**
   - Verify SQL Server LocalDB is running
   - Check connection string in appsettings.json

4. **No Historical Data**
   ```bash
   # Populate sample data first
   curl -k -X POST https://localhost:7000/api/HistoricalData/populate-sample \
     -H "Content-Type: application/json" \
     -d '{
       "fromDate": "2024-06-24",
       "toDate": "2024-07-15",
       "strikes": [23500, 24000, 24500],
       "optionTypes": ["CE", "PE"]
     }'
   ```

### Debug Steps

1. **Check Application Logs**
   ```bash
   # View recent logs
   tail -f logs/log-$(date +%Y%m%d).txt
   ```

2. **Test Individual Components**
   ```bash
   # Test indicator service directly
   curl -k -X GET https://localhost:7000/api/ApiTradingDashboard/current-signals
   
   # Test historical data service
   curl -k -X GET https://localhost:7000/api/HistoricalData/coverage
   ```

3. **Verify Database Tables**
   - Check if OptionsHistoricalData table has data
   - Verify ApiTradeLog table exists
   - Check ManualTradingViewAlerts table

## Success Criteria

✅ **Application responds to API calls**
✅ **Current signals are generated (S1-S8)**
✅ **Historical data is available**
✅ **Backtest runs successfully**
✅ **Performance metrics are calculated**
✅ **All 8 signal types are working**
✅ **Win rate > 60%**
✅ **Positive total P&L**

## Next Steps After Testing

1. **Live Trading**: Switch to live mode by setting `UseSimulatedServices: false`
2. **Real Data**: Use real Kite Connect API for live market data
3. **Automation**: Set up automated signal generation
4. **Monitoring**: Implement real-time performance monitoring
5. **Alerts**: Configure notifications for signal generation

## API Endpoint Summary

| Endpoint | Method | Purpose |
|----------|---------|---------|
| `/api/IndicatorBacktest/current-signals` | GET | Get live signals |
| `/api/IndicatorBacktest/quick-backtest` | POST | 3-week backtest |
| `/api/IndicatorBacktest/run-api-backtest` | POST | Custom backtest |
| `/api/IndicatorBacktest/compare-signals` | POST | API vs TradingView |
| `/api/ApiTradingDashboard/live-dashboard` | GET | Real-time dashboard |
| `/api/ApiTradingDashboard/test-signals` | POST | Test signal generation |

This completes the comprehensive testing guide for the API Indicator Backtesting system!