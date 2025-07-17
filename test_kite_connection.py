#!/usr/bin/env python3
"""
Simple test script to verify Kite Connect API connection
This helps validate credentials before running the full .NET application
"""

import requests
import json
from datetime import datetime

# Configuration from appsettings.json
API_KEY = "a3vacbrbn3fs98ie"
ACCESS_TOKEN = "HqVWPQMHNi591jaAIznrZZaq3Wvc3VBb"  # Current token from appsettings.json
BASE_URL = "https://api.kite.trade"

def test_api_connection():
    """Test basic API connection"""
    if not ACCESS_TOKEN:
        print("❌ ACCESS_TOKEN is empty!")
        print("📝 Please generate access token first using the steps above")
        return False
    
    headers = {
        "X-Kite-Version": "3",
        "Authorization": f"token {API_KEY}:{ACCESS_TOKEN}"
    }
    
    try:
        # Test 1: Get user profile
        print("🧪 Testing API connection...")
        profile_url = f"{BASE_URL}/user/profile"
        response = requests.get(profile_url, headers=headers)
        
        if response.status_code == 200:
            profile_data = response.json()
            print(f"✅ API Connection successful!")
            print(f"   User: {profile_data['data']['user_name']}")
            print(f"   Email: {profile_data['data']['email']}")
            return True
        else:
            print(f"❌ API Connection failed: {response.status_code}")
            print(f"   Error: {response.text}")
            return False
            
    except Exception as e:
        print(f"❌ Connection error: {e}")
        return False

def test_nifty_quote():
    """Test NIFTY 50 quote retrieval"""
    if not ACCESS_TOKEN:
        return False
    
    headers = {
        "X-Kite-Version": "3",
        "Authorization": f"token {API_KEY}:{ACCESS_TOKEN}"
    }
    
    try:
        print("🧪 Testing NIFTY 50 quote retrieval...")
        quote_url = f"{BASE_URL}/quote?i=NSE:NIFTY+50"
        response = requests.get(quote_url, headers=headers)
        
        if response.status_code == 200:
            quote_data = response.json()
            nifty_data = quote_data['data']['256265']  # NIFTY 50 instrument token
            
            print(f"✅ NIFTY 50 Quote retrieved successfully!")
            print(f"   Last Price: ₹{nifty_data['last_price']}")
            print(f"   Open: ₹{nifty_data['ohlc']['open']}")
            print(f"   High: ₹{nifty_data['ohlc']['high']}")
            print(f"   Low: ₹{nifty_data['ohlc']['low']}")
            print(f"   Close: ₹{nifty_data['ohlc']['close']}")
            return True
        else:
            print(f"❌ NIFTY Quote failed: {response.status_code}")
            print(f"   Error: {response.text}")
            return False
            
    except Exception as e:
        print(f"❌ Quote error: {e}")
        return False

def test_nifty_historical_data():
    """Test NIFTY 50 historical data retrieval"""
    if not ACCESS_TOKEN:
        return False
    
    headers = {
        "X-Kite-Version": "3",
        "Authorization": f"token {API_KEY}:{ACCESS_TOKEN}"
    }
    
    try:
        print("🧪 Testing NIFTY 50 historical data retrieval...")
        
        # Get last 5 days of 1-hour data
        from_date = "2024-07-10"  # Adjust as needed
        to_date = "2024-07-16"    # Adjust as needed
        
        hist_url = f"{BASE_URL}/instruments/historical/256265/60minute"
        params = {
            "from": from_date,
            "to": to_date,
            "continuous": 0,
            "oi": 0
        }
        
        response = requests.get(hist_url, headers=headers, params=params)
        
        if response.status_code == 200:
            hist_data = response.json()
            candles = hist_data['data']['candles']
            
            print(f"✅ NIFTY 50 Historical data retrieved successfully!")
            print(f"   Total Candles: {len(candles)}")
            print(f"   Date Range: {from_date} to {to_date}")
            
            if candles:
                first_candle = candles[0]
                last_candle = candles[-1]
                print(f"   First: {first_candle[0]} | OHLC: {first_candle[1]:.2f}/{first_candle[2]:.2f}/{first_candle[3]:.2f}/{first_candle[4]:.2f}")
                print(f"   Last:  {last_candle[0]} | OHLC: {last_candle[1]:.2f}/{last_candle[2]:.2f}/{last_candle[3]:.2f}/{last_candle[4]:.2f}")
            
            return True
        else:
            print(f"❌ Historical data failed: {response.status_code}")
            print(f"   Error: {response.text}")
            return False
            
    except Exception as e:
        print(f"❌ Historical data error: {e}")
        return False

def main():
    """Main test function"""
    print("🎯 KITE CONNECT API TESTING")
    print("=" * 50)
    
    # Test 1: API Connection
    if not test_api_connection():
        print("\n❌ API connection failed. Please check your credentials.")
        return
    
    # Test 2: NIFTY Quote
    if not test_nifty_quote():
        print("\n❌ NIFTY quote test failed.")
        return
    
    # Test 3: Historical Data
    if not test_nifty_historical_data():
        print("\n❌ Historical data test failed.")
        return
    
    print("\n🎉 All tests passed! Your Kite Connect API is working correctly.")
    print("✅ Ready to test the .NET application!")

if __name__ == "__main__":
    main()