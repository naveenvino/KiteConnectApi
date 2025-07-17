@echo off
echo ========================================
echo API Indicator Backtesting - Step by Step Test
echo ========================================
echo.

echo Step 1: Checking if application is running...
tasklist | findstr KiteConnect
if %errorlevel% neq 0 (
    echo ERROR: Application is not running!
    echo Please start the application first with: dotnet run
    pause
    exit /b 1
)
echo ✓ Application is running
echo.

echo Step 2: Testing basic API connectivity...
curl -k -s -o NUL -w "HTTP Status: %%{http_code}" https://localhost:7000/swagger
if %errorlevel% neq 0 (
    echo ERROR: Cannot connect to API
    echo Check if application is running on port 7000
    pause
    exit /b 1
)
echo.
echo ✓ API is responding
echo.

echo Step 3: Testing historical data availability...
echo Checking data coverage...
curl -k -X GET https://localhost:7000/api/HistoricalData/coverage
echo.
echo.

echo Step 4: Testing current API signals...
echo Getting current signals from API indicator...
curl -k -X GET https://localhost:7000/api/IndicatorBacktest/current-signals
echo.
echo.

echo Step 5: Running quick backtest (3 weeks)...
echo This will test the main functionality...
curl -k -X POST https://localhost:7000/api/IndicatorBacktest/quick-backtest -H "Content-Type: application/json"
echo.
echo.

echo Step 6: Testing API dashboard...
echo Getting live dashboard data...
curl -k -X GET https://localhost:7000/api/ApiTradingDashboard/live-dashboard
echo.
echo.

echo Step 7: Testing signal generation...
echo Testing signal generation with history...
curl -k -X POST https://localhost:7000/api/ApiTradingDashboard/test-signals -H "Content-Type: application/json" -d "{\"symbol\":\"NIFTY\",\"includeHistory\":true}"
echo.
echo.

echo ========================================
echo Testing Complete!
echo ========================================
echo.
echo If you see JSON responses above, the API is working correctly.
echo If you see errors, check the troubleshooting section in the guide.
echo.
pause