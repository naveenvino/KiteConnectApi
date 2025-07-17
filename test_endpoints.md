# 🧪 .NET Application API Testing Guide

## Prerequisites
✅ Access token generated and added to appsettings.json
✅ Python test script passed successfully

## Step 1: Start the Application

```bash
cd C:\Users\E1791\KiteApp\Kite
dotnet run
```

The application will start on:
- HTTPS: `https://localhost:7000`
- HTTP: `http://localhost:5000`
- Swagger UI: `https://localhost:7000/swagger` (if available)

## Step 2: Test API Endpoints

### 2.1 Test API Setup Guide
```bash
curl -k https://localhost:7000/api/NiftyDataCollection/setup-guide
```

### 2.2 Test Kite Connect Connection
```bash
curl -k https://localhost:7000/api/NiftyDataCollection/test-connection
```

**Expected Response:**
```json
{
  "success": true,
  "message": "✅ Kite Connect API connection successful",
  "connection": {
    "apiEndpoint": "https://api.kite.trade",
    "instrumentToken": 256265,
    "symbol": "NSE:NIFTY 50",
    "status": "Connected"
  },
  "testResult": {
    "currentPrice": 24500.25,
    "timestamp": "2024-07-17T18:30:00",
    "error": null,
    "note": "API credentials are working correctly"
  }
}
```

### 2.3 Get Current NIFTY Quote
```bash
curl -k https://localhost:7000/api/NiftyDataCollection/current-quote
```

### 2.4 Check Data Inventory
```bash
curl -k https://localhost:7000/api/NiftyDataCollection/inventory
```

### 2.5 Collect Backtesting Data
```bash
curl -k -X POST https://localhost:7000/api/NiftyDataCollection/collect-backtest-period
```

**Expected Response:**
```json
{
  "success": true,
  "message": "Backtesting period NIFTY data collection completed",
  "data": {
    "period": "June 24 - July 15, 2024",
    "interval": "60minute (1-hour candles)",
    "collectedCandles": 315,
    "expectedCandles": "~315 (21 days × 6.5 hours)",
    "issues": ["✅ Received 315 candles from Kite Connect API"]
  },
  "summary": {
    "dataQuality": "✅ Good",
    "readyForBacktest": true,
    "nextStep": "Run corrected signal backtest with real NIFTY index data"
  }
}
```

### 2.6 Custom Data Collection
```bash
curl -k -X POST https://localhost:7000/api/NiftyDataCollection/collect \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2024-07-01",
    "toDate": "2024-07-16",
    "interval": "60minute",
    "saveToDatabase": true
  }'
```

## Step 3: Verify Data Quality

### 3.1 Check Database
```sql
-- Connect to your LocalDB
-- Server: (localdb)\mssqllocaldb
-- Database: KiteConnectApi

SELECT 
    COUNT(*) as TotalRecords,
    MIN(Timestamp) as FirstRecord,
    MAX(Timestamp) as LastRecord,
    AVG(Close) as AvgNiftyPrice
FROM OptionsHistoricalData 
WHERE Underlying = 'NIFTY_INDEX' 
  AND TradingSymbol = 'NIFTY_INDEX'
  AND Interval = '60minute';
```

### 3.2 Expected Results
- **Total Records**: ~315 for 3-week period
- **Date Range**: June 24 - July 15, 2024
- **Avg Price**: ~23,000-24,000 (depends on market)
- **No Missing Data**: All trading hours covered

## Step 4: Test Signal Detection

### 4.1 Test Pure Signal Service
```bash
curl -k -X POST https://localhost:7000/api/backtesting/pure-1h-signals \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2024-06-24",
    "toDate": "2024-07-15",
    "initialCapital": 100000,
    "maxPositions": 3
  }'
```

### 4.2 Test Corrected Signal Service
```bash
curl -k -X POST https://localhost:7000/api/backtesting/corrected-nifty-signals \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2024-06-24",
    "toDate": "2024-07-15",
    "useRealNiftyData": true,
    "initialCapital": 100000
  }'
```

## Step 5: Troubleshooting

### Common Issues:

1. **"API credentials not configured"**
   - ✅ Check appsettings.json has correct AccessToken
   - ✅ Verify Python test passes first

2. **"No data received from Kite Connect API"**
   - ✅ Check date range (trading days only)
   - ✅ Verify API subscription is active
   - ✅ Try smaller date range

3. **"Port already in use"**
   - ✅ Kill existing processes: `wmic process where "name='KiteConnectApi.exe'" delete`
   - ✅ Try different port: `dotnet run --urls="https://localhost:7001"`

4. **"Database connection failed"**
   - ✅ Ensure SQL Server LocalDB is installed
   - ✅ Check connection string in appsettings.json

## Step 6: Success Indicators

✅ **API Connection Test**: Returns current NIFTY price
✅ **Data Collection**: Collects 300+ candles for backtesting period
✅ **Data Quality**: No gaps in trading hours data
✅ **Signal Detection**: Generates S1-S8 signals using real NIFTY data
✅ **Strike Calculation**: Each signal calculates its own strike price
✅ **Options Lookup**: Finds option prices using signal-determined strikes

## Next Steps After Success

1. **Run Full Backtest**: Use corrected signal service with real NIFTY data
2. **Analyze Results**: Check P&L, signal accuracy, and performance
3. **Optimize Parameters**: Adjust hedge distance, stop loss, take profit
4. **Test Different Periods**: Try various market conditions
5. **Deploy for Live Trading**: Switch to live mode when ready

Remember: Always test in simulated mode first before live trading!