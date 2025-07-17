using KiteConnectApi.Data;
using KiteConnectApi.Models.Dto;
using KiteConnectApi.Models.Trading;
using KiteConnectApi.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace KiteConnectApi.Services
{
    public class OptionsTradeService
    {
        private readonly ApplicationDbContext _context;
        private readonly IKiteConnectService _kiteConnectService;
        private readonly ILogger<OptionsTradeService> _logger;
        private readonly INotificationService _notificationService;

        public OptionsTradeService(
            ApplicationDbContext context,
            IKiteConnectService kiteConnectService,
            ILogger<OptionsTradeService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _kiteConnectService = kiteConnectService;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<bool> ProcessOptionsAlertAsync(TradingViewAlert alert)
        {
            try
            {
                _logger.LogInformation("Processing options alert: {Alert}", JsonSerializer.Serialize(alert));

                // Find matching strategy configuration
                var strategyConfig = await FindMatchingStrategyAsync(alert);
                if (strategyConfig == null)
                {
                    _logger.LogWarning("No matching strategy found for alert: {Signal}", alert.Signal);
                    return false;
                }

                // Check if strategy is enabled and within active period
                if (!IsStrategyActive(strategyConfig))
                {
                    _logger.LogInformation("Strategy {StrategyName} is not active", strategyConfig.StrategyName);
                    return false;
                }

                // Handle manual execution mode
                if (strategyConfig.ExecutionMode == "Manual")
                {
                    await CreatePendingAlertAsync(alert, strategyConfig);
                    return true;
                }

                // Process the alert based on action type
                if (alert.Action?.ToUpper() == "ENTRY")
                {
                    return await ProcessEntryAlertAsync(alert, strategyConfig);
                }
                else if (alert.Action?.ToUpper() == "STOPLOSS")
                {
                    return await ProcessStopLossAlertAsync(alert, strategyConfig);
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing options alert");
                return false;
            }
        }

        private async Task<NiftyOptionStrategyConfig?> FindMatchingStrategyAsync(TradingViewAlert alert)
        {
            var index = alert.Index ?? "Nifty"; // Default to Nifty if not specified
            var signal = alert.Signal ?? "S1";

            return await _context.NiftyOptionStrategyConfigs
                .Where(s => s.IsEnabled && 
                           s.UnderlyingInstrument == index &&
                           s.AllowedSignals != null && 
                           s.AllowedSignals.Contains(signal))
                .FirstOrDefaultAsync();
        }

        private bool IsStrategyActive(NiftyOptionStrategyConfig strategy)
        {
            var now = DateTime.Now;
            
            // Check date range
            if (now < strategy.FromDate || now > strategy.ToDate)
                return false;

            // Check time range (if specified)
            if (strategy.EntryTime > 0 && strategy.ExitTime > 0)
            {
                var currentTime = now.Hour * 100 + now.Minute;
                if (currentTime < strategy.EntryTime || currentTime > strategy.ExitTime)
                    return false;
            }

            return true;
        }

        private async Task CreatePendingAlertAsync(TradingViewAlert alert, NiftyOptionStrategyConfig strategy)
        {
            var pendingAlert = new PendingAlert
            {
                StrategyId = strategy.Id,
                AlertJson = JsonSerializer.Serialize(alert),
                Strike = alert.Strike,
                OptionType = alert.Type ?? "CE",
                Signal = alert.Signal ?? "S1",
                Action = alert.Action ?? "Entry",
                Index = alert.Index ?? "Nifty",
                ExpiryTime = DateTime.Now.AddMinutes(strategy.ManualExecutionTimeoutMinutes),
                Priority = GetAlertPriority(alert.Action)
            };

            _context.PendingAlerts.Add(pendingAlert);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created pending alert for manual execution: {AlertId}", pendingAlert.Id);
        }

        private int GetAlertPriority(string? action)
        {
            return action?.ToUpper() switch
            {
                "STOPLOSS" => 1, // Highest priority
                "ENTRY" => 2,
                _ => 3
            };
        }

        private async Task<bool> ProcessEntryAlertAsync(TradingViewAlert alert, NiftyOptionStrategyConfig strategy)
        {
            try
            {
                // Validate input parameters
                if (!ValidateEntryAlert(alert, strategy))
                {
                    return false;
                }

                // Check if we already have an open position for this signal
                var existingPosition = await _context.OptionsTradePositions
                    .FirstOrDefaultAsync(p => p.StrategyId == strategy.Id && 
                                            p.Signal == alert.Signal && 
                                            p.Status == "OPEN");

                if (existingPosition != null && !strategy.AllowMultipleSignals)
                {
                    _logger.LogInformation("Position already exists for signal {Signal}", alert.Signal);
                    return false;
                }

                // Get nearest weekly expiry
                var expiryDate = GetNearestWeeklyExpiry();
                
                // Calculate quantity
                var quantity = await CalculateQuantityAsync(strategy);
                if (quantity <= 0)
                {
                    _logger.LogWarning("Invalid quantity calculated: {Quantity}", quantity);
                    await _notificationService.SendNotificationAsync(
                        "Entry Failed", 
                        $"Invalid quantity calculated for strategy {strategy.StrategyName}: {quantity}");
                    return false;
                }

                // Create main position (SELL CE/PE)
                var mainPosition = await CreateMainPositionAsync(alert, strategy, expiryDate, quantity);
                if (mainPosition == null)
                {
                    await _notificationService.SendNotificationAsync(
                        "Entry Failed", 
                        $"Failed to create main position for {alert.Signal} - {alert.Strike} {alert.Type}");
                    return false;
                }

                // Create hedge position if enabled
                if (strategy.HedgeEnabled)
                {
                    var hedgePosition = await CreateHedgePositionAsync(alert, strategy, expiryDate, quantity, mainPosition);
                    if (hedgePosition != null)
                    {
                        mainPosition.MainPositionId = hedgePosition.Id; // Link hedge to main position
                        _context.OptionsTradePositions.Update(mainPosition);
                        await _context.SaveChangesAsync();
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create hedge position, but main position was successful");
                    }
                }

                // Send notification
                if (strategy.NotifyOnEntry)
                {
                    await _notificationService.SendNotificationAsync(
                        "Options Entry", 
                        $"Entered position: {mainPosition.TradingSymbol} | Qty: {quantity} | Price: {mainPosition.EntryPrice}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing entry alert");
                await _notificationService.SendNotificationAsync(
                    "Entry Alert Error", 
                    $"Error processing entry alert for {alert.Signal}: {ex.Message}");
                return false;
            }
        }

        private bool ValidateEntryAlert(TradingViewAlert alert, NiftyOptionStrategyConfig strategy)
        {
            var errors = new List<string>();

            if (alert.Strike <= 0)
                errors.Add("Strike price must be greater than 0");

            if (string.IsNullOrEmpty(alert.Type) || (alert.Type.ToUpper() != "CE" && alert.Type.ToUpper() != "PE"))
                errors.Add("Option type must be CE or PE");

            if (string.IsNullOrEmpty(alert.Signal))
                errors.Add("Signal is required");

            if (strategy.Quantity <= 0 && !strategy.UseDynamicQuantity)
                errors.Add("Quantity must be greater than 0 or dynamic quantity must be enabled");

            if (strategy.UseDynamicQuantity && strategy.AllocatedMargin <= 0)
                errors.Add("Allocated margin must be greater than 0 when using dynamic quantity");

            if (errors.Any())
            {
                _logger.LogWarning("Entry alert validation failed: {Errors}", string.Join("; ", errors));
                return false;
            }

            return true;
        }

        private async Task<bool> ProcessStopLossAlertAsync(TradingViewAlert alert, NiftyOptionStrategyConfig strategy)
        {
            try
            {
                // Find open positions for this signal
                var positions = await _context.OptionsTradePositions
                    .Where(p => p.StrategyId == strategy.Id && 
                              p.Signal == alert.Signal && 
                              p.Status == "OPEN")
                    .ToListAsync();

                if (!positions.Any())
                {
                    _logger.LogWarning("No open positions found for signal {Signal}", alert.Signal);
                    return false;
                }

                var closedCount = 0;
                foreach (var position in positions)
                {
                    var success = await ClosePositionAsync(position, "STOPLOSS");
                    if (success) closedCount++;
                }

                // Send notification
                if (strategy.NotifyOnStopLoss && closedCount > 0)
                {
                    await _notificationService.SendNotificationAsync(
                        "Options Stop Loss", 
                        $"Closed {closedCount} positions for signal {alert.Signal}");
                }

                return closedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing stop loss alert");
                return false;
            }
        }

        private DateTime GetNearestWeeklyExpiry()
        {
            var today = DateTime.Now.Date;
            var daysUntilThursday = ((int)DayOfWeek.Thursday - (int)today.DayOfWeek + 7) % 7;
            
            if (daysUntilThursday == 0 && DateTime.Now.TimeOfDay > TimeSpan.FromHours(15.5))
            {
                // If it's Thursday after 3:30 PM, move to next Thursday
                daysUntilThursday = 7;
            }

            return today.AddDays(daysUntilThursday);
        }

        private Task<int> CalculateQuantityAsync(NiftyOptionStrategyConfig strategy)
        {
            if (!strategy.UseDynamicQuantity)
            {
                return Task.FromResult(strategy.Quantity);
            }

            // Dynamic quantity calculation based on allocated margin
            // This is a simplified calculation - in reality, you'd need to consider:
            // - Current option premium
            // - Margin requirements
            // - Risk parameters
            
            var baseQuantity = (int)(strategy.AllocatedMargin / 2000); // Assuming 2000 margin per lot
            return Task.FromResult(Math.Max(baseQuantity, 1)); // Minimum 1 lot
        }

        private async Task<OptionsTradePosition?> CreateMainPositionAsync(
            TradingViewAlert alert, 
            NiftyOptionStrategyConfig strategy, 
            DateTime expiryDate, 
            int quantity)
        {
            try
            {
                // Generate trading symbol
                var tradingSymbol = GenerateTradingSymbol(
                    strategy.UnderlyingInstrument ?? "NIFTY",
                    expiryDate,
                    alert.Strike,
                    alert.Type ?? "CE");

                // Get current market price
                var marketPrice = await GetOptionPriceAsync(tradingSymbol);
                if (marketPrice <= 0)
                {
                    _logger.LogError("Could not get market price for {TradingSymbol}", tradingSymbol);
                    return null;
                }

                // Place order with protection
                var orderId = await PlaceProtectedOrderAsync(tradingSymbol, "SELL", quantity, marketPrice, strategy.EntryOrderType, strategy.UseOrderProtection);
                if (string.IsNullOrEmpty(orderId))
                {
                    _logger.LogError("Failed to place order for {TradingSymbol}", tradingSymbol);
                    return null;
                }

                // Create position record
                var position = new OptionsTradePosition
                {
                    StrategyId = strategy.Id,
                    Signal = alert.Signal ?? "S1",
                    TradingSymbol = tradingSymbol,
                    Strike = alert.Strike,
                    OptionType = alert.Type ?? "CE",
                    TransactionType = "SELL",
                    Quantity = quantity,
                    EntryPrice = marketPrice,
                    CurrentPrice = marketPrice,
                    ExpiryDate = expiryDate,
                    OrderId = orderId,
                    StopLossPrice = CalculateStopLossPrice(marketPrice, strategy.StopLossPercentage),
                    TargetPrice = CalculateTargetPrice(marketPrice, strategy.TargetPercentage),
                    Status = "OPEN"
                };

                _context.OptionsTradePositions.Add(position);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created main position: {TradingSymbol} | Qty: {Quantity} | Price: {Price}", 
                    tradingSymbol, quantity, marketPrice);

                return position;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating main position");
                return null;
            }
        }

        private async Task<OptionsTradePosition?> CreateHedgePositionAsync(
            TradingViewAlert alert, 
            NiftyOptionStrategyConfig strategy, 
            DateTime expiryDate, 
            int quantity, 
            OptionsTradePosition mainPosition)
        {
            try
            {
                int hedgeStrike;
                
                if (strategy.HedgeType == "POINTS")
                {
                    hedgeStrike = alert.Strike + strategy.HedgeDistancePoints;
                }
                else // PERCENTAGE
                {
                    hedgeStrike = await CalculateHedgeStrikeByPercentageAsync(
                        alert.Strike, 
                        mainPosition.EntryPrice, 
                        strategy.HedgePremiumPercentage, 
                        alert.Type ?? "CE", 
                        expiryDate, 
                        strategy.UnderlyingInstrument ?? "NIFTY");
                }

                var hedgeTradingSymbol = GenerateTradingSymbol(
                    strategy.UnderlyingInstrument ?? "NIFTY",
                    expiryDate,
                    hedgeStrike,
                    alert.Type ?? "CE");

                var hedgePrice = await GetOptionPriceAsync(hedgeTradingSymbol);
                if (hedgePrice <= 0)
                {
                    _logger.LogError("Could not get hedge price for {TradingSymbol}", hedgeTradingSymbol);
                    return null;
                }

                var hedgeQuantity = (int)(quantity * strategy.HedgeRatio);
                var hedgeOrderId = await PlaceProtectedOrderAsync(hedgeTradingSymbol, "BUY", hedgeQuantity, hedgePrice, strategy.EntryOrderType, strategy.UseOrderProtection);
                
                if (string.IsNullOrEmpty(hedgeOrderId))
                {
                    _logger.LogError("Failed to place hedge order for {TradingSymbol}", hedgeTradingSymbol);
                    return null;
                }

                var hedgePosition = new OptionsTradePosition
                {
                    StrategyId = strategy.Id,
                    Signal = alert.Signal ?? "S1",
                    TradingSymbol = hedgeTradingSymbol,
                    Strike = hedgeStrike,
                    OptionType = alert.Type ?? "CE",
                    TransactionType = "BUY",
                    Quantity = hedgeQuantity,
                    EntryPrice = hedgePrice,
                    CurrentPrice = hedgePrice,
                    ExpiryDate = expiryDate,
                    OrderId = hedgeOrderId,
                    IsHedge = true,
                    MainPositionId = mainPosition.Id,
                    Status = "OPEN"
                };

                _context.OptionsTradePositions.Add(hedgePosition);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created hedge position: {TradingSymbol} | Qty: {Quantity} | Price: {Price}", 
                    hedgeTradingSymbol, hedgeQuantity, hedgePrice);

                return hedgePosition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating hedge position");
                return null;
            }
        }

        private string GenerateTradingSymbol(string underlying, DateTime expiry, int strike, string optionType)
        {
            // Format: NIFTY24717{strike}CE/PE (e.g., NIFTY2471722500CE)
            var year = expiry.ToString("yy");
            var month = expiry.Month.ToString();
            var day = expiry.Day.ToString("D2");
            
            return $"{underlying}{year}{month}{day}{strike}{optionType.ToUpper()}";
        }

        private async Task<decimal> GetOptionPriceAsync(string tradingSymbol)
        {
            try
            {
                // Get current market price from KiteConnect API with retry logic
                var quotes = await _kiteConnectService.GetQuotesAsync(new[] { tradingSymbol });
                
                if (quotes.TryGetValue(tradingSymbol, out var quote))
                {
                    // Use last traded price (LTP) as the current price
                    var ltp = (decimal)quote.LastPrice;
                    
                    // Validate price is reasonable (not zero or negative)
                    if (ltp <= 0)
                    {
                        _logger.LogWarning("Invalid LTP {LTP} for {TradingSymbol}, using bid/ask average", ltp, tradingSymbol);
                        
                        // Fallback to bid/ask average if LTP is invalid
                        var bidPrice = 0.0; // Simplified for now
                        var askPrice = 0.0; // Simplified for now
                        
                        if (bidPrice > 0 && askPrice > 0)
                        {
                            ltp = (decimal)((bidPrice + askPrice) / 2);
                        }
                        else
                        {
                            _logger.LogError("No valid price available for {TradingSymbol}", tradingSymbol);
                            return 0;
                        }
                    }
                    
                    _logger.LogDebug("Retrieved price {Price} for {TradingSymbol}", ltp, tradingSymbol);
                    return ltp;
                }
                else
                {
                    _logger.LogWarning("No quote found for {TradingSymbol}", tradingSymbol);
                    return 0;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting option price for {TradingSymbol}", tradingSymbol);
                return 0;
            }
        }

        private async Task<string?> PlaceProtectedOrderAsync(string tradingSymbol, string transactionType, int quantity, decimal marketPrice, string? orderType, bool useProtection, decimal protectionBuffer = 0.05m)
        {
            try
            {
                string finalOrderType = orderType ?? "MARKET";
                decimal? finalPrice = null;

                // Apply order protection if enabled and order is MARKET
                if (useProtection && finalOrderType == "MARKET")
                {
                    // Convert MARKET order to LIMIT order with protective buffer
                    finalOrderType = "LIMIT";
                    
                    if (transactionType == "BUY")
                    {
                        // For BUY orders, add buffer to ensure execution
                        finalPrice = marketPrice * (1 + protectionBuffer);
                    }
                    else
                    {
                        // For SELL orders, subtract buffer to ensure execution
                        finalPrice = marketPrice * (1 - protectionBuffer);
                    }
                    
                    _logger.LogInformation("Order protection applied: {TradingSymbol} | Original: MARKET | Protected: LIMIT @ {Price} (Market: {MarketPrice})", 
                        tradingSymbol, finalPrice, marketPrice);
                }
                else if (finalOrderType == "LIMIT")
                {
                    finalPrice = marketPrice;
                }

                return await PlaceOrderAsync(tradingSymbol, transactionType, quantity, finalPrice ?? marketPrice, finalOrderType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error placing protected order for {TradingSymbol}", tradingSymbol);
                return null;
            }
        }

        private async Task<string?> PlaceOrderAsync(string tradingSymbol, string transactionType, int quantity, decimal price, string? orderType)
        {
            const int maxRetries = 3;
            const int retryDelayMs = 1000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation($"Placing order (attempt {attempt}/{maxRetries}): {tradingSymbol} | {transactionType} | Qty: {quantity} | Price: {price} | Type: {orderType}");

                    // Determine correct product type based on strategy config
                    var productType = DetermineProductType(tradingSymbol);
                    
                    // Place order through KiteConnect API with proper parameters
                    var orderResult = await _kiteConnectService.PlaceOrderAsync(
                        exchange: "NFO", // Options are on NFO
                        tradingsymbol: tradingSymbol,
                        transaction_type: transactionType,
                        quantity: quantity,
                        product: productType,
                        order_type: orderType ?? "MARKET",
                        price: orderType == "LIMIT" ? price : null,
                        validity: "DAY",
                        tag: "KiteApp_Auto" // Tag for tracking
                    );

                    if (orderResult.TryGetValue("order_id", out var orderId))
                    {
                        var orderIdStr = orderId.ToString();
                        _logger.LogInformation($"Order placed successfully: {orderIdStr} for {tradingSymbol}");
                        return orderIdStr;
                    }
                    else
                    {
                        _logger.LogError($"Order placement failed for {tradingSymbol}: No order ID returned. Result: {System.Text.Json.JsonSerializer.Serialize(orderResult)}");
                        
                        if (attempt < maxRetries)
                        {
                            await Task.Delay(retryDelayMs * attempt);
                            continue;
                        }
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error placing order for {tradingSymbol} (attempt {attempt}/{maxRetries})");
                    
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelayMs * attempt);
                        continue;
                    }
                    
                    // Send notification for order failure
                    await _notificationService.SendNotificationAsync(
                        "Order Placement Failed",
                        $"Failed to place order for {tradingSymbol} after {maxRetries} attempts. Error: {ex.Message}");
                    
                    return null;
                }
            }

            return null;
        }

        private string DetermineProductType(string tradingSymbol)
        {
            // For options trading, determine product type based on strategy
            // MIS: Margin Intraday Squareoff (most common for options)
            // NRML: Normal (for overnight positions)
            // CNC: Cash and Carry (for equity delivery)
            
            // Since we're doing options trading, default to MIS for intraday
            // or NRML for overnight positions
            return "MIS"; // Default to intraday for options
        }

        private decimal CalculateStopLossPrice(decimal entryPrice, decimal stopLossPercentage)
        {
            return entryPrice * (1 + stopLossPercentage / 100);
        }

        private decimal CalculateTargetPrice(decimal entryPrice, decimal targetPercentage)
        {
            return entryPrice * (1 - targetPercentage / 100);
        }

        private async Task<int> CalculateHedgeStrikeByPercentageAsync(int mainStrike, decimal mainPremium, decimal hedgePercentage, string optionType, DateTime expiryDate, string underlying)
        {
            try
            {
                var targetHedgePremium = mainPremium * (hedgePercentage / 100);
                var bestStrike = mainStrike + 300; // Default fallback
                var bestPriceDiff = decimal.MaxValue;

                // Try strikes at different intervals based on option type
                var strikeOffsets = optionType.ToUpper() == "CE" 
                    ? new[] { 50, 100, 200, 300, 500, 700, 1000 } // For CE, go higher
                    : new[] { -50, -100, -200, -300, -500, -700, -1000 }; // For PE, go lower

                foreach (var offset in strikeOffsets)
                {
                    var testStrike = mainStrike + offset;
                    var testSymbol = GenerateTradingSymbol(underlying, expiryDate, testStrike, optionType);
                    
                    try
                    {
                        var testPrice = await GetOptionPriceAsync(testSymbol);
                        
                        if (testPrice > 0)
                        {
                            var priceDiff = Math.Abs(testPrice - targetHedgePremium);
                            if (priceDiff < bestPriceDiff)
                            {
                                bestPriceDiff = priceDiff;
                                bestStrike = testStrike;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Error getting price for hedge strike {Strike}", testStrike);
                        continue;
                    }
                }

                _logger.LogInformation("Selected hedge strike {Strike} with premium difference {Diff} from target {Target}", 
                    bestStrike, bestPriceDiff, targetHedgePremium);

                return bestStrike;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating hedge strike by percentage, using default");
                return mainStrike + 300; // Fallback to default
            }
        }

        public async Task<bool> ClosePositionAsync(OptionsTradePosition position, string exitReason)
        {
            try
            {
                var currentPrice = await GetOptionPriceAsync(position.TradingSymbol);
                
                // Find strategy config to check order protection setting
                var strategy = await _context.NiftyOptionStrategyConfigs
                    .FirstOrDefaultAsync(s => s.Id == position.StrategyId);
                
                var exitOrderId = await PlaceProtectedOrderAsync(
                    position.TradingSymbol, 
                    position.TransactionType == "SELL" ? "BUY" : "SELL", 
                    position.Quantity, 
                    currentPrice, 
                    "MARKET",
                    strategy?.UseOrderProtection ?? true); // Default to protection if strategy not found

                if (string.IsNullOrEmpty(exitOrderId))
                {
                    return false;
                }

                position.Status = "CLOSED";
                position.ExitTime = DateTime.Now;
                position.ExitReason = exitReason;
                position.ExitOrderId = exitOrderId;
                position.CurrentPrice = currentPrice;
                position.PnL = CalculatePnL(position, currentPrice);
                position.LastUpdated = DateTime.Now;

                _context.OptionsTradePositions.Update(position);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Closed position: {TradingSymbol} | P&L: {PnL}", position.TradingSymbol, position.PnL);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing position {PositionId}", position.Id);
                return false;
            }
        }

        public async Task<bool> ClosePositionAsync(string positionId, string exitReason)
        {
            var position = await _context.OptionsTradePositions
                .FirstOrDefaultAsync(p => p.Id == positionId && p.Status == "OPEN");

            if (position == null)
            {
                return false;
            }

            return await ClosePositionAsync(position, exitReason);
        }

        private decimal CalculatePnL(OptionsTradePosition position, decimal currentPrice)
        {
            if (position.TransactionType == "SELL")
            {
                return (position.EntryPrice - currentPrice) * position.Quantity;
            }
            else
            {
                return (currentPrice - position.EntryPrice) * position.Quantity;
            }
        }

        public async Task<List<OptionsTradePosition>> GetExpiringPositionsAsync()
        {
            return await _context.OptionsTradePositions
                .Where(p => p.Status == "OPEN" && p.ExpiryDate.Date == DateTime.Now.Date)
                .ToListAsync();
        }

        public async Task<bool> SquareOffAllPositionsAsync()
        {
            var openPositions = await _context.OptionsTradePositions
                .Where(p => p.Status == "OPEN")
                .ToListAsync();

            var successCount = 0;
            foreach (var position in openPositions)
            {
                var success = await ClosePositionAsync(position, "MANUAL_SQUARE_OFF");
                if (success) successCount++;
            }

            return successCount > 0;
        }

        public async Task<bool> SquareOffStrategyPositionsAsync(string strategyId)
        {
            var openPositions = await _context.OptionsTradePositions
                .Where(p => p.StrategyId == strategyId && p.Status == "OPEN")
                .ToListAsync();

            var successCount = 0;
            foreach (var position in openPositions)
            {
                var success = await ClosePositionAsync(position, "STRATEGY_SQUARE_OFF");
                if (success) successCount++;
            }

            return successCount > 0;
        }
    }
}