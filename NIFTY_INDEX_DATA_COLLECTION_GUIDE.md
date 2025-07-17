# 📊 NIFTY 50 INDEX DATA COLLECTION GUIDE

## 🎯 **Complete Solution for Getting NIFTY Index Data**

Based on Zerodha Kite Connect API documentation, here's everything you need to collect proper NIFTY 50 index data for signal detection.

---

## 📋 **API Requirements & Setup**

### **1. Kite Connect API Access**
```
✅ Purchase Kite Connect API subscription from Zerodha
✅ Generate API Key and API Secret
✅ Get Access Token (manual process or auto-refresh)
```

### **2. NIFTY 50 Index Details**
```
Instrument Token: 256265
Exchange Symbol: NSE:NIFTY 50
API Identifier: NSE:NIFTY+50 (for quotes)
```

### **3. Historical Data Limits**
```
Interval    | Max Days | Perfect For
------------|----------|-------------
60minute    | 400 days | ✅ Our 1H strategy
30minute    | 200 days | Alternative
15minute    | 200 days | Higher resolution
day         | 2000 days| Long-term analysis
```

---

## 🔧 **Implementation Options**

### **Option A: Use Our NiftyIndexDataCollectionService**

#### **Step 1: Configure API Credentials**
```json
// appsettings.json
{
  "KiteConnect": {
    "ApiKey": "your_api_key_here",
    "AccessToken": "your_access_token_here"
  }
}
```

#### **Step 2: Register Service**
```csharp
// Program.cs
builder.Services.AddScoped<NiftyIndexDataCollectionService>();
builder.Services.AddHttpClient<NiftyIndexDataCollectionService>();
```

#### **Step 3: Collect Data**
```bash
# Call the collection API (will create endpoint)
curl -k -X POST "https://localhost:7000/api/NiftyDataCollection/collect" \
  -H "Content-Type: application/json" \
  -d '{
    "fromDate": "2024-06-24",
    "toDate": "2024-07-15",
    "interval": "60minute",
    "saveToDatabase": true
  }'
```

### **Option B: Python Script (Direct Collection)**

```python
# nifty_data_collector.py
from kiteconnect import KiteConnect
import pandas as pd
import sqlite3
from datetime import datetime, timedelta

# Setup
api_key = "your_api_key"
access_token = "your_access_token"
kite = KiteConnect(api_key=api_key)
kite.set_access_token(access_token)

# NIFTY 50 instrument token
NIFTY_TOKEN = 256265

def collect_nifty_data(from_date, to_date):
    """Collect NIFTY 50 index historical data"""
    try:
        # Fetch historical data
        data = kite.historical_data(
            instrument_token=NIFTY_TOKEN,
            from_date=from_date,
            to_date=to_date,
            interval="60minute"
        )
        
        # Convert to DataFrame
        df = pd.DataFrame(data)
        df['symbol'] = 'NIFTY_INDEX'
        df['underlying'] = 'NIFTY_INDEX'
        df['trading_symbol'] = 'NIFTY_INDEX'
        df['interval'] = '60minute'
        df['last_price'] = df['close']  # For index, last_price = close
        
        print(f"✅ Collected {len(df)} NIFTY index candles")
        return df
        
    except Exception as e:
        print(f"❌ Error collecting data: {e}")
        return None

def save_to_database(df, db_path):
    """Save to SQLite database (or SQL Server)"""
    try:
        conn = sqlite3.connect(db_path)
        
        # Insert data
        df.to_sql('OptionsHistoricalData', conn, if_exists='append', index=False)
        
        print(f"✅ Saved {len(df)} records to database")
        conn.close()
        
    except Exception as e:
        print(f"❌ Error saving to database: {e}")

# Collect data for your backtesting period
if __name__ == "__main__":
    from_date = "2024-06-24"
    to_date = "2024-07-15"
    
    # Collect NIFTY index data
    nifty_data = collect_nifty_data(from_date, to_date)
    
    if nifty_data is not None:
        # Save to database
        save_to_database(nifty_data, "kite_data.db")
        
        # Display sample
        print("\n📊 Sample NIFTY Index Data:")
        print(nifty_data.head())
```

### **Option C: Manual Collection via Kite Connect**

```python
# Quick collection script
import requests
import json
from datetime import datetime

# API Setup
api_key = "your_api_key"
access_token = "your_access_token"
base_url = "https://api.kite.trade"

headers = {
    "X-Kite-Version": "3",
    "Authorization": f"token {api_key}:{access_token}"
}

# Collect NIFTY 50 historical data
def get_nifty_historical(from_date, to_date):
    url = f"{base_url}/instruments/historical/256265/60minute"
    params = {
        "from": from_date,
        "to": to_date,
        "continuous": 0,
        "oi": 0
    }
    
    response = requests.get(url, headers=headers, params=params)
    
    if response.status_code == 200:
        data = response.json()
        candles = data["data"]["candles"]
        
        print(f"✅ Collected {len(candles)} NIFTY candles")
        
        # Sample candle format:
        # ["2024-06-24T09:15:00+0530", 23450.5, 23485.2, 23420.1, 23465.8, 0]
        # [timestamp, open, high, low, close, volume]
        
        return candles
    else:
        print(f"❌ API Error: {response.status_code}")
        return None

# Usage
candles = get_nifty_historical("2024-06-24", "2024-07-15")
```

---

## 🗄️ **Database Storage**

### **Correct Database Schema**
```sql
-- Store NIFTY index data with proper identification
INSERT INTO OptionsHistoricalData (
    Underlying,        -- 'NIFTY_INDEX'
    TradingSymbol,     -- 'NIFTY_INDEX' (same as underlying for index)
    Timestamp,         -- 2024-06-24 09:15:00
    Open,              -- 23450.50
    High,              -- 23485.20  
    Low,               -- 23420.10
    Close,             -- 23465.80
    LastPrice,         -- 23465.80 (same as close for index)
    Volume,            -- 0 (index doesn't have volume)
    Interval           -- '60minute'
)
```

### **Verification Query**
```sql
-- Check collected NIFTY index data
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

---

## 🔐 **API Authentication Guide**

### **Step 1: Get Kite Connect Subscription**
1. Login to Zerodha Kite
2. Go to Apps section
3. Purchase Kite Connect API (₹2000/month)
4. Generate API Key and Secret

### **Step 2: Generate Access Token**

#### **Manual Method (One-time setup):**
```python
from kiteconnect import KiteConnect

# Initial setup
api_key = "your_api_key"
api_secret = "your_api_secret"
kite = KiteConnect(api_key=api_key)

# Generate login URL
login_url = kite.login_url()
print(f"Login URL: {login_url}")

# After login, get request_token from callback URL
request_token = "token_from_callback_url"

# Generate access token
data = kite.generate_session(request_token, api_secret=api_secret)
access_token = data["access_token"]

print(f"Access Token: {access_token}")
# Save this token for API calls
```

#### **Programmatic Method (Advanced):**
```python
# Auto-refresh access token (requires request_token handling)
def refresh_access_token():
    # Implementation depends on your authentication flow
    pass
```

---

## 🧪 **Testing & Validation**

### **Step 1: Test API Connection**
```bash
# Test current NIFTY quote
curl -H "X-Kite-Version: 3" \
     -H "Authorization: token api_key:access_token" \
     "https://api.kite.trade/quote?i=NSE:NIFTY+50"
```

### **Step 2: Validate Historical Data**
```python
# Check data quality
def validate_nifty_data(df):
    print(f"📊 Data Quality Report:")
    print(f"Total Records: {len(df)}")
    print(f"Date Range: {df['date'].min()} to {df['date'].max()}")
    print(f"Missing Values: {df.isnull().sum().sum()}")
    print(f"Price Range: {df['close'].min():.2f} - {df['close'].max():.2f}")
    
    # Check for trading hours (9:15 AM to 3:30 PM)
    df['hour'] = pd.to_datetime(df['date']).dt.hour
    trading_hours = df['hour'].between(9, 15).sum()
    print(f"Trading Hours Data: {trading_hours}/{len(df)} records")
```

### **Step 3: Verify with Your Signals**
```csharp
// Test with collected data
var niftyData = await GetNiftyIndexDataAsync("2024-06-24", "2024-07-15");
var signals = await ProcessNiftySignalsAsync(niftyData);

Console.WriteLine($"✅ Processed {niftyData.Count} NIFTY candles");
Console.WriteLine($"🎯 Generated {signals.Count} trading signals");
```

---

## ⚡ **Quick Start Commands**

### **For Python Users:**
```bash
# Install required packages
pip install kiteconnect pandas sqlalchemy

# Run collection script
python nifty_data_collector.py
```

### **For .NET Users:**
```bash
# Add to your appsettings.json
# Register NiftyIndexDataCollectionService
# Call collection API endpoint
```

### **For Manual Collection:**
```bash
# Direct API call (replace with your credentials)
curl -H "X-Kite-Version: 3" \
     -H "Authorization: token YOUR_API_KEY:YOUR_ACCESS_TOKEN" \
     "https://api.kite.trade/instruments/historical/256265/60minute?from=2024-06-24&to=2024-07-15"
```

---

## 🎯 **Expected Results**

### **For 3 Weeks (June 24 - July 15, 2024):**
```
Expected Records: ~315 candles (21 days × 6.5 hours × 1 hour)
Trading Days: ~15 days (excluding weekends)
Data Size: ~15KB JSON / ~5KB database storage
Sample Price Range: 22,000 - 25,000 (approximate)
```

### **Sample NIFTY Index Candle:**
```json
{
  "timestamp": "2024-06-24T09:15:00+0530",
  "open": 23450.50,
  "high": 23485.20,
  "low": 23420.10,
  "close": 23465.80,
  "volume": 0
}
```

---

## 🔧 **Troubleshooting**

### **Common Issues:**

1. **"Invalid API credentials"**
   - ✅ Check API key and access token
   - ✅ Ensure Kite Connect subscription is active

2. **"Date range too large"**
   - ✅ Use smaller date ranges (max 400 days for 60minute)
   - ✅ Split large requests into chunks

3. **"No data returned"**
   - ✅ Check date format (YYYY-MM-DD)
   - ✅ Ensure dates are trading days
   - ✅ Verify instrument token (256265)

4. **"Rate limit exceeded"**
   - ✅ Add delays between API calls
   - ✅ Check API usage limits

### **Data Quality Checks:**
```python
# Verify data completeness
def check_data_gaps(df):
    df['date'] = pd.to_datetime(df['date'])
    df = df.sort_values('date')
    
    # Check for hour gaps during trading hours
    time_diff = df['date'].diff()
    gaps = time_diff[time_diff > pd.Timedelta(hours=1)]
    
    if len(gaps) > 0:
        print(f"⚠️ Found {len(gaps)} data gaps:")
        for gap in gaps:
            print(f"   Gap: {gap}")
    else:
        print("✅ No data gaps found")
```

---

## 🎉 **Once You Have NIFTY Index Data**

### **You Can:**
1. ✅ Run proper signal detection on real NIFTY index candles
2. ✅ Get accurate strike price calculation based on current NIFTY price
3. ✅ Use existing options data for entry/exit prices
4. ✅ Generate reliable backtest results

### **Updated Backtest Flow:**
```
NIFTY Index 1H → Signal Detection → Strike Calculation → Options Trading → P&L
     ↓                 ↓                 ↓                 ↓              ↓
Real OHLC Data    S1-S8 Triggers    Current Price    Entry/Exit Prices   Profit/Loss
```

**Get your Kite Connect API setup and start collecting real NIFTY 50 index data for accurate backtesting!**