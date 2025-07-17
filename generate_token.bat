@echo off
echo ===================================
echo KITE CONNECT TOKEN GENERATOR
echo ===================================
echo.
echo API Key: a3vacbrbn3fs98ie
echo API Secret: zy2zaws481kifjmsv3v6pchu13ng2cbz
echo.
echo Step 1: Open this URL in your browser:
echo https://kite.zerodha.com/connect/login?api_key=a3vacbrbn3fs98ie^&v=3
echo.
echo Step 2: Login with Zerodha credentials
echo Step 3: Copy request_token from redirect URL
echo.
set /p request_token=Enter request_token: 
echo.
echo Your request_token: %request_token%
echo.
echo Step 4: Generate checksum and access token
echo.
echo Use this curl command:
echo curl -X POST "https://api.kite.trade/session/token" \
echo   -d "api_key=a3vacbrbn3fs98ie" \
echo   -d "request_token=%request_token%" \
echo   -d "checksum=NEED_TO_CALCULATE_SHA256"
echo.
echo To calculate checksum, use online SHA256 calculator with:
echo a3vacbrbn3fs98ie%request_token%zy2zaws481kifjmsv3v6pchu13ng2cbz
echo.
pause