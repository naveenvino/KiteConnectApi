@echo off
echo ============================================
echo API Indicator Backtesting - Testing Guide
echo ============================================
echo.

echo STEP 1: Start the Application
echo -----------------------------
echo Open a new command prompt and run:
echo   cd C:\Users\E1791\KiteApp\Kite
echo   dotnet run
echo.
echo Wait for the message: "Now listening on: https://localhost:7000"
echo.

echo STEP 2: Run These Tests (copy and paste each command)
echo ---------------------------------------------------
echo.

echo Test 1: Check if application is running
echo ----------------------------------------
echo tasklist ^| findstr KiteConnect
echo.

echo Test 2: Test current signals
echo ----------------------------
echo curl -k -X GET https://localhost:7000/api/IndicatorBacktest/current-signals
echo.

echo Test 3: Run quick backtest (MAIN TEST)
echo ---------------------------------------
echo curl -k -X POST https://localhost:7000/api/IndicatorBacktest/quick-backtest -H "Content-Type: application/json"
echo.

echo Test 4: Test API dashboard
echo ---------------------------
echo curl -k -X GET https://localhost:7000/api/ApiTradingDashboard/current-signals
echo.

echo Test 5: Test signal generation
echo -------------------------------
echo curl -k -X POST https://localhost:7000/api/ApiTradingDashboard/test-signals -H "Content-Type: application/json" -d "{\"symbol\":\"NIFTY\",\"includeHistory\":true}"
echo.

echo Test 6: Run custom backtest
echo ----------------------------
echo curl -k -X POST https://localhost:7000/api/IndicatorBacktest/run-api-backtest -H "Content-Type: application/json" -d "{\"fromDate\":\"2024-06-24\",\"toDate\":\"2024-07-15\",\"quantity\":50,\"symbol\":\"NIFTY\"}"
echo.

echo Test 7: Compare with TradingView
echo ---------------------------------
echo curl -k -X POST https://localhost:7000/api/IndicatorBacktest/compare-signals -H "Content-Type: application/json" -d "{\"fromDate\":\"2024-06-24\",\"toDate\":\"2024-07-15\",\"quantity\":50,\"symbol\":\"NIFTY\"}"
echo.

echo WHAT TO EXPECT:
echo ===============
echo - Test 1: Should show KiteConnectApi.exe process
echo - Test 2: Should return JSON with signal data
echo - Test 3: Should return backtest results with win rate, P&L, etc.
echo - Test 4: Should return current signals dashboard
echo - Test 5: Should return test signals with history
echo - Test 6: Should return custom backtest results
echo - Test 7: Should return comparison between API and TradingView signals
echo.

echo TROUBLESHOOTING:
echo ================
echo - If "connection refused": Application not started or wrong port
echo - If "service registration error": Services not properly registered
echo - If "empty results": No historical data available
echo - If "database error": SQL Server LocalDB not running
echo.

echo Files created for reference:
echo - API_INDICATOR_TESTING_GUIDE.md (comprehensive guide)
echo - MANUAL_TESTING_STEPS.md (detailed steps)
echo - test_api_backtest.py (demo simulation)
echo.

pause