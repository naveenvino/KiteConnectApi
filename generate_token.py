#!/usr/bin/env python3
"""
Generate fresh Kite Connect access token
"""

import webbrowser
import urllib.parse
from datetime import datetime

# Your credentials from appsettings.json
API_KEY = "a3vacbrbn3fs98ie"
API_SECRET = "zy2zaws481kifjmsv3v6pchu13ng2cbz"

def generate_access_token():
    """Generate new access token"""
    
    # Step 1: Generate login URL
    login_url = f"https://kite.zerodha.com/connect/login?api_key={API_KEY}&v=3"
    
    print("KITE CONNECT ACCESS TOKEN GENERATOR")
    print("=" * 50)
    print(f"API Key: {API_KEY}")
    print(f"API Secret: {API_SECRET[:10]}...")
    print()
    
    print("STEP 1: Get Request Token")
    print(f"1. Open this URL in your browser:")
    print(f"   {login_url}")
    print()
    print("2. Login with your Zerodha credentials")
    print("3. After login, you'll be redirected to a URL like:")
    print("   http://localhost:7000?request_token=XXXXXX&action=login&status=success")
    print()
    
    # Try to open browser automatically
    try:
        webbrowser.open(login_url)
        print("Browser opened automatically")
    except:
        print("Please copy and paste the URL manually")
    
    print()
    request_token = input("4. Enter the request_token from the URL: ").strip()
    
    if not request_token:
        print("No request token provided!")
        return
    
    # Step 2: Generate access token using requests
    try:
        import requests
        
        # Create session data
        url = "https://api.kite.trade/session/token"
        data = {
            "api_key": API_KEY,
            "request_token": request_token,
            "checksum": generate_checksum(API_KEY, request_token, API_SECRET)
        }
        
        response = requests.post(url, data=data)
        
        if response.status_code == 200:
            session_data = response.json()['data']
            access_token = session_data['access_token']
            user_id = session_data['user_id']
            
            print()
            print("ACCESS TOKEN GENERATED SUCCESSFULLY!")
            print("=" * 50)
            print(f"Access Token: {access_token}")
            print(f"User ID: {user_id}")
            print(f"Generated: {datetime.now()}")
            print()
            
            print("UPDATE YOUR appsettings.json:")
            print('    "AccessToken": "' + access_token + '",')
            print('    "UserId": "' + user_id + '",')
            print()
            
            # Test the token
            test_token(access_token)
            
        else:
            print(f"Error generating token: {response.status_code}")
            print(f"Response: {response.text}")
            
    except ImportError:
        print("requests library not found. Using manual method...")
        manual_token_generation(request_token)
    except Exception as e:
        print(f"Error: {e}")

def generate_checksum(api_key, request_token, api_secret):
    """Generate checksum for token generation"""
    import hashlib
    
    # Create checksum: sha256(api_key + request_token + api_secret)
    checksum_string = api_key + request_token + api_secret
    checksum = hashlib.sha256(checksum_string.encode()).hexdigest()
    return checksum

def manual_token_generation(request_token):
    """Manual token generation instructions"""
    checksum = generate_checksum(API_KEY, request_token, API_SECRET)
    
    print()
    print("MANUAL TOKEN GENERATION:")
    print("Use this curl command:")
    print()
    print(f'curl -X POST "https://api.kite.trade/session/token" \\')
    print(f'  -d "api_key={API_KEY}" \\')
    print(f'  -d "request_token={request_token}" \\')
    print(f'  -d "checksum={checksum}"')
    print()

def test_token(access_token):
    """Test the generated token"""
    try:
        import requests
        
        headers = {
            "X-Kite-Version": "3",
            "Authorization": f"token {API_KEY}:{access_token}"
        }
        
        # Test profile
        response = requests.get("https://api.kite.trade/user/profile", headers=headers)
        
        if response.status_code == 200:
            profile = response.json()['data']
            print("TOKEN TEST RESULT:")
            print(f"Token is valid!")
            print(f"   User: {profile['user_name']}")
            print(f"   Email: {profile['email']}")
            print()
            
            # Test NIFTY quote
            quote_response = requests.get("https://api.kite.trade/quote?i=NSE:NIFTY+50", headers=headers)
            if quote_response.status_code == 200:
                quote_data = quote_response.json()['data']['256265']
                print(f"NIFTY Quote Test Passed!")
                print(f"   Current Price: Rs.{quote_data['last_price']}")
                print()
            
        else:
            print(f"Token test failed: {response.status_code}")
            print(f"   {response.text}")
            
    except Exception as e:
        print(f"Token test error: {e}")

if __name__ == "__main__":
    generate_access_token()