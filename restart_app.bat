@echo off
echo Stopping existing application...
taskkill /F /IM KiteConnectApi.exe 2>nul
timeout /t 3 /nobreak >nul

echo Starting application with simulated services...
cd /d "C:\Users\E1791\KiteApp\Kite"
start "" dotnet run

echo Application restarting...
echo Wait for application to load, then try backtesting again.
pause