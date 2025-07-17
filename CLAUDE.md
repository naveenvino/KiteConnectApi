# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a KiteConnect API-based trading platform built with .NET 9.0. The application provides automated trading capabilities, real-time market data, portfolio management, and options trading strategies. It integrates with the Zerodha Kite Connect API for live trading and includes simulation modes for testing.

## Development Commands

### Basic Commands
```bash
# Run the application
dotnet run

# Build the application
dotnet build

# Run tests
dotnet test

# Clean build artifacts
dotnet clean

# Restore NuGet packages
dotnet restore
```

### Database Commands
```bash
# Apply database migrations
dotnet ef database update

# Create new migration
dotnet ef migrations add MigrationName

# Remove last migration
dotnet ef migrations remove
```

### Development Setup
- Uses SQL Server LocalDB for development: `(localdb)\mssqllocaldb`
- Database name: `KiteConnectApi`
- Redis connection: `localhost:6379`
- Default port: HTTPS 7000, HTTP 5000

## Architecture

### Core Components

**Controllers**: Handle API endpoints for trading, portfolio, quotes, strategies, and notifications
- `TradingController`: Processes TradingView alerts and manual trading actions
- `PortfolioController`: Portfolio positions and P&L calculations
- `OrdersController`: Order management and execution
- `OptionsStrategyController`: Options trading strategies management

**Services**: Business logic layer with dependency injection
- `KiteConnectService`: Live trading via Zerodha Kite Connect API
- `SimulatedKiteConnectService`: Simulated trading for testing
- `TradingStrategyService`: Strategy execution and monitoring
- `RiskManagementService`: Position monitoring and risk controls
- `OptionsTradeService`: Options-specific trading logic

**Data Layer**: Entity Framework Core with repository pattern
- `ApplicationDbContext`: Main database context
- Repository interfaces in `Repositories/` folder
- Models in `Models/Trading/` folder

**Background Services**: Long-running services for monitoring
- `TradingStrategyMonitor`: Monitors active strategies
- `OrderMonitoringService`: Tracks order status
- `ExpiryDayMonitor`: Handles options expiry
- `ExpirySquareOffService`: Automated position squaring

### Configuration Modes

**Simulated Mode** (`UseSimulatedServices: true`):
- Uses `SimulatedKiteConnectService` for safe testing
- Uses `SimulatedPositionRepository` and `SimulatedOrderRepository`
- No real money at risk

**Live Mode** (`UseSimulatedServices: false`):
- Uses real `KiteConnectService` with Zerodha API
- Requires valid API credentials in environment variables
- Uses production repositories

## Key Technologies

- **.NET 9.0**: Core framework
- **Entity Framework Core**: Database ORM with SQL Server
- **SignalR**: Real-time communication for market data
- **MediatR**: Command/Query pattern implementation
- **Orleans**: Actor-based concurrency via `StrategyGrain`
- **Hangfire**: Background job processing
- **Redis**: Caching and session management
- **Serilog**: Structured logging
- **FluentValidation**: Input validation
- **Polly**: Resilience and retry policies
- **OpenTelemetry**: Distributed tracing
- **Mapster**: Object mapping

## Database Schema

Key entities:
- `Order`: Trading orders with status tracking
- `TradePosition`: Open positions and P&L
- `OptionsTradePosition`: Options-specific positions
- `Strategy`: Trading strategy configurations
- `NiftyOptionStrategyConfig`: Nifty options strategy settings
- `PendingAlert`: Queued trading alerts
- `HedgeConfiguration`: Risk hedging rules
- `RiskManagementRule`: Risk management parameters

## TradingView Integration

The application processes TradingView alerts via webhook at `/api/Trading/alert`:
- Supports 8 signal types (S1-S8) for different trading actions
- Validates alerts using `TradingViewAlertValidator`
- Processes alerts through `TradingController`
- Stores manual alerts in `ManualTradingViewAlert` table

## Testing and Validation

- Default test credentials: Username: `admin`, Password: `password`
- Use `UseSimulatedServices: true` for safe testing
- Comprehensive validation using FluentValidation
- Repository pattern enables easy mocking for unit tests

## Security Features

- JWT authentication (configurable via `Jwt:Enabled`)
- Environment-based configuration for sensitive data
- Risk management with configurable loss limits
- Input validation and sanitization
- Audit logging for all trading activities

## Monitoring and Observability

- **Serilog**: Structured logging to console, file, and SQL Server
- **OpenTelemetry**: Distributed tracing with Jaeger export
- **Health Checks**: Built-in application health monitoring
- **Hangfire Dashboard**: Background job monitoring
- **SignalR**: Real-time updates for market data and positions

## Configuration

Key configuration sections in `appsettings.json`:
- `UseSimulatedServices`: Toggle between live and simulated trading
- `ConnectionStrings`: Database and Redis connections
- `KiteConnect`: API credentials (use environment variables)
- `NiftyOptionStrategy`: Trading strategy parameters
- `RiskParameters`: Risk management settings
- `Jwt`: Authentication configuration
- `Notification`: Email/SMS notification settings

## Important Notes

- Always use simulated mode for development and testing
- Set Kite Connect API credentials via environment variables, not configuration files
- The application uses CQRS pattern with MediatR for complex operations
- Database migrations are automatically applied on startup
- Redis is required for caching and session management
- The application includes comprehensive error handling and logging