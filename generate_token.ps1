# Kite Connect Access Token Generator
# No Python required, just PowerShell

Write-Host "KITE CONNECT ACCESS TOKEN GENERATOR" -ForegroundColor Green
Write-Host "====================================" -ForegroundColor Green
Write-Host ""
Write-Host "API Key: a3vacbrbn3fs98ie"
Write-Host "API Secret: zy2zaws481kifjmsv3v6pchu13ng2cbz"
Write-Host ""

# Step 1: Open login URL
$loginUrl = "https://kite.zerodha.com/connect/login?api_key=a3vacbrbn3fs98ie&v=3"
Write-Host "Step 1: Opening login URL in browser..." -ForegroundColor Yellow
Start-Process $loginUrl

Write-Host ""
Write-Host "Step 2: Login with your Zerodha credentials"
Write-Host "Step 3: After login, you'll be redirected to a URL like:"
Write-Host "http://localhost:7000?request_token=XXXXXX&action=login&status=success"
Write-Host ""

# Step 2: Get request token
$requestToken = Read-Host "Enter the request_token from the URL"

if (-not $requestToken) {
    Write-Host "No request token provided!" -ForegroundColor Red
    exit
}

Write-Host ""
Write-Host "Request Token: $requestToken" -ForegroundColor Green

# Step 3: Generate checksum
$apiKey = "a3vacbrbn3fs98ie"
$apiSecret = "zy2zaws481kifjmsv3v6pchu13ng2cbz"
$checksumString = $apiKey + $requestToken + $apiSecret

# Calculate SHA256
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$hash = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($checksumString))
$checksum = [System.BitConverter]::ToString($hash).Replace("-", "").ToLower()

Write-Host ""
Write-Host "Generated Checksum: $checksum" -ForegroundColor Green
Write-Host ""

# Step 4: Generate access token
Write-Host "Step 4: Generating access token..." -ForegroundColor Yellow

$body = @{
    api_key = $apiKey
    request_token = $requestToken
    checksum = $checksum
}

try {
    $response = Invoke-RestMethod -Uri "https://api.kite.trade/session/token" -Method POST -Body $body
    
    $accessToken = $response.data.access_token
    $userId = $response.data.user_id
    
    Write-Host ""
    Write-Host "ACCESS TOKEN GENERATED SUCCESSFULLY!" -ForegroundColor Green
    Write-Host "====================================" -ForegroundColor Green
    Write-Host "Access Token: $accessToken"
    Write-Host "User ID: $userId"
    Write-Host ""
    
    Write-Host "UPDATE YOUR appsettings.json:" -ForegroundColor Yellow
    Write-Host '    "AccessToken": "' + $accessToken + '",'
    Write-Host '    "UserId": "' + $userId + '",'
    Write-Host ""
    
    # Test the token
    Write-Host "Testing the token..." -ForegroundColor Yellow
    
    $headers = @{
        "X-Kite-Version" = "3"
        "Authorization" = "token $apiKey`:$accessToken"
    }
    
    $profileResponse = Invoke-RestMethod -Uri "https://api.kite.trade/user/profile" -Headers $headers
    
    if ($profileResponse.status -eq "success") {
        Write-Host "Token test PASSED!" -ForegroundColor Green
        Write-Host "User: $($profileResponse.data.user_name)"
        Write-Host "Email: $($profileResponse.data.email)"
        
        # Test NIFTY quote
        $quoteResponse = Invoke-RestMethod -Uri "https://api.kite.trade/quote?i=NSE:NIFTY+50" -Headers $headers
        $niftyPrice = $quoteResponse.data.'256265'.last_price
        Write-Host "NIFTY Current Price: Rs.$niftyPrice"
        
    } else {
        Write-Host "Token test FAILED!" -ForegroundColor Red
    }
    
} catch {
    Write-Host "Error generating token: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Response: $($_.Exception.Response)" -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")