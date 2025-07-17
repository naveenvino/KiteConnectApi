@echo off
echo Testing NIFTY Index Data Collection
echo ====================================

echo.
echo Step 1: Testing API credentials...
curl -H "X-Kite-Version: 3" -H "Authorization: token a3vacbrbn3fs98ie:HqVWPQMHNi591jaAIznrZZaq3Wvc3VBb" "https://api.kite.trade/user/profile"

echo.
echo.
echo Step 2: Testing NIFTY 50 quote...
curl -H "X-Kite-Version: 3" -H "Authorization: token a3vacbrbn3fs98ie:HqVWPQMHNi591jaAIznrZZaq3Wvc3VBb" "https://api.kite.trade/quote?i=NSE:NIFTY+50"

echo.
echo.
echo Step 3: Testing NIFTY historical data...
curl -H "X-Kite-Version: 3" -H "Authorization: token a3vacbrbn3fs98ie:HqVWPQMHNi591jaAIznrZZaq3Wvc3VBb" "https://api.kite.trade/instruments/historical/256265/60minute?from=2024-07-15&to=2024-07-16"

echo.
echo.
echo If all above tests show TokenException, your credentials need to be fixed.
echo If they work, you can collect NIFTY data successfully!
pause