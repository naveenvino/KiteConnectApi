# Manual Testing Guide for API Indicator Backtesting

## How to Test the API Indicator Backtest - Step by Step

### Step 1: Start the Application

1. **Open Command Prompt** as Administrator
2. **Navigate to the project directory**:
   ```cmd
   cd C:\Users\E1791\KiteApp\Kite
   ```

3. **Start the application**:
   ```cmd
   dotnet run
   ```

4. **Wait for the application to start**. You should see output like:
   ```
   info: Microsoft.Hosting.Lifetime[14]
         Now listening on: https://localhost:7000
         Now listening on: http://localhost:5000
   ```

### Step 2: Verify Application is Running

1. **Check process**:
   ```cmd
   tasklist | findstr KiteConnect
   ```
   Should show: `KiteConnectApi.exe` with a process ID

2. **Test basic connectivity**:
   ```cmd
   curl -k https://localhost:7000/swagger
   ```
   Should return HTML content (Swagger UI)

### Step 3: Test Historical Data (Foundation)

1. **Check data coverage**:
   ```cmd
   curl -k -X GET https://localhost:7000/api/HistoricalData/coverage
   ```

2. **Test price data retrieval**:
   ```cmd
   curl -k -X GET "https://localhost:7000/api/HistoricalData/test-price?strike=24000&optionType=CE"
   ```

### Step 4: Test API Indicator Signals

1. **Get current signals**:
   ```cmd
   curl -k -X GET https://localhost:7000/api/IndicatorBacktest/current-signals
   ```

2. **Expected response**:
   ```json
   {
     "timestamp": "2025-01-17T...",
     "symbol": "NIFTY",
     "signalCount": 2,
     "signals": [
       {
         "signalId": "S1",
         "signalName": "Bear Trap",
         "direction": 1,
         "strikePrice": 24000,
         "optionType": "PE",
         "confidence": 0.8
       }
     ]
   }
   ```

### Step 5: Run Quick Backtest (Main Feature)

1. **Execute 3-week backtest**:
   ```cmd
   curl -k -X POST https://localhost:7000/api/IndicatorBacktest/quick-backtest -H "Content-Type: application/json"
   ```

2. **Expected response**:
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
     "signalBreakdown": [...]
   }
   ```

### Step 6: Test Custom Backtest

1. **Run custom period backtest**:
   ```cmd
   curl -k -X POST https://localhost:7000/api/IndicatorBacktest/run-api-backtest -H "Content-Type: application/json" -d "{\"fromDate\":\"2024-06-24\",\"toDate\":\"2024-07-15\",\"quantity\":50,\"symbol\":\"NIFTY\"}"
   ```

### Step 7: Test Dashboard Features

1. **Get live dashboard**:
   ```cmd
   curl -k -X GET https://localhost:7000/api/ApiTradingDashboard/live-dashboard
   ```

2. **Test signal generation**:
   ```cmd
   curl -k -X POST https://localhost:7000/api/ApiTradingDashboard/test-signals -H "Content-Type: application/json" -d "{\"symbol\":\"NIFTY\",\"includeHistory\":true}"
   ```

### Step 8: Test Signal Comparison

1. **Compare API vs TradingView**:
   ```cmd
   curl -k -X POST https://localhost:7000/api/IndicatorBacktest/compare-signals -H "Content-Type: application/json" -d "{\"fromDate\":\"2024-06-24\",\"toDate\":\"2024-07-15\",\"quantity\":50,\"symbol\":\"NIFTY\"}"
   ```

## What to Look For

### ✅ Success Indicators:
- Application starts without errors
- API responds with JSON data
- Signals are generated (S1-S8)
- Backtest returns performance metrics
- Win rate > 60%
- Positive P&L values

### ❌ Failure Indicators:
- Connection refused errors
- Service registration errors
- Empty signal arrays
- Database connection failures
- Zero trades in backtest

## Troubleshooting Steps

### Problem: Application won't start
```cmd
# Check if port is in use
netstat -an | findstr :7000

# Kill existing processes
taskkill /F /IM KiteConnectApi.exe

# Rebuild and restart
dotnet build
dotnet run
```

### Problem: API not responding
```cmd
# Check if process is running
tasklist | findstr KiteConnect

# Test different ports
curl -k https://localhost:7000/swagger
curl http://localhost:5000/swagger
```

### Problem: No historical data
```cmd
# Populate sample data
curl -k -X POST https://localhost:7000/api/HistoricalData/populate-sample -H "Content-Type: application/json" -d "{\"fromDate\":\"2024-06-24\",\"toDate\":\"2024-07-15\",\"strikes\":[23500,24000,24500],\"optionTypes\":[\"CE\",\"PE\"]}"
```

### Problem: Service registration errors
- Check `Program.cs` for service registration
- Ensure all dependencies are properly registered:
  ```csharp
  builder.Services.AddScoped<TradingViewIndicatorService>();
  builder.Services.AddScoped<ApiTradingDashboardService>();
  builder.Services.AddScoped<IndicatorBacktestingService>();
  ```

## Alternative Testing Methods

### Using PowerShell:
```powershell
# Test API connectivity
Invoke-RestMethod -Uri "https://localhost:7000/api/IndicatorBacktest/current-signals" -Method GET -SkipCertificateCheck

# Run quick backtest
Invoke-RestMethod -Uri "https://localhost:7000/api/IndicatorBacktest/quick-backtest" -Method POST -ContentType "application/json" -SkipCertificateCheck
```

### Using Browser:
1. Open browser to: `https://localhost:7000/swagger`
2. Navigate to API endpoints
3. Test each endpoint manually through Swagger UI

## Expected Performance Metrics

Based on the 3-week historical data:
- **Total Signals**: 20-30 signals
- **Total Trades**: 15-25 trades
- **Win Rate**: 60-75%
- **Total P&L**: Positive (₹5,000-₹15,000)
- **Sharpe Ratio**: 1.0-2.0
- **Max Drawdown**: < 20% of total P&L

## Files Created for Testing

1. **API_INDICATOR_TESTING_GUIDE.md** - Comprehensive guide
2. **test_api_step_by_step.bat** - Automated test script
3. **test_api_backtest.py** - Demo simulation
4. **MANUAL_TESTING_STEPS.md** - This file

## Next Steps After Successful Testing

1. **Live Trading**: Switch to live mode
2. **Real Data**: Connect to live Kite Connect API
3. **Automation**: Set up scheduled signal generation
4. **Monitoring**: Implement real-time alerts
5. **Optimization**: Fine-tune signal parameters

This completes the manual testing guide. Follow these steps in order to verify the API indicator backtesting functionality!