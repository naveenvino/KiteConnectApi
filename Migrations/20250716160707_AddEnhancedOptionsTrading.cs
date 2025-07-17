using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiteConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class AddEnhancedOptionsTrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HedgeTradingSymbol",
                table: "TradePositions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Strategies",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<bool>(
                name: "AllowMultipleSignals",
                table: "NiftyOptionStrategyConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AllowedSignals",
                table: "NiftyOptionStrategyConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoSquareOffOnExpiry",
                table: "NiftyOptionStrategyConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "NiftyOptionStrategyConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedTime",
                table: "NiftyOptionStrategyConfigs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "ExpirySquareOffTimeMinutes",
                table: "NiftyOptionStrategyConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HedgeEnabled",
                table: "NiftyOptionStrategyConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "HedgeRatio",
                table: "NiftyOptionStrategyConfigs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "HedgeType",
                table: "NiftyOptionStrategyConfigs",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "NiftyOptionStrategyConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastUpdated",
                table: "NiftyOptionStrategyConfigs",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "LastUpdatedBy",
                table: "NiftyOptionStrategyConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ManualExecutionTimeoutMinutes",
                table: "NiftyOptionStrategyConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxDailyLoss",
                table: "NiftyOptionStrategyConfigs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxPositionSize",
                table: "NiftyOptionStrategyConfigs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "MinReentryDelayMinutes",
                table: "NiftyOptionStrategyConfigs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "NiftyOptionStrategyConfigs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnEntry",
                table: "NiftyOptionStrategyConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnExit",
                table: "NiftyOptionStrategyConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnProfit",
                table: "NiftyOptionStrategyConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnStopLoss",
                table: "NiftyOptionStrategyConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseDynamicQuantity",
                table: "NiftyOptionStrategyConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseNearestWeeklyExpiry",
                table: "NiftyOptionStrategyConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseOrderProtection",
                table: "NiftyOptionStrategyConfigs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "TradingDays",
                table: "ExecutionSettings",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "HedgeConfigurations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StrategyId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    HedgeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HedgePoints = table.Column<int>(type: "int", nullable: false),
                    HedgePercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    HedgeTransactionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HedgeRatio = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxHedgePrice = table.Column<int>(type: "int", nullable: false),
                    MinHedgePrice = table.Column<int>(type: "int", nullable: false),
                    AutoAdjustHedge = table.Column<bool>(type: "bit", nullable: false),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HedgeConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OptionsTradePositions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StrategyId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Signal = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TradingSymbol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InstrumentToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Strike = table.Column<int>(type: "int", nullable: false),
                    OptionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransactionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    EntryPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StopLossPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TargetPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EntryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PnL = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PnLPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExitReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExitOrderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsHedge = table.Column<bool>(type: "bit", nullable: false),
                    MainPositionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrailingStopLoss = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxProfit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxLoss = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProfitLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedProfitLevel = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionsTradePositions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PendingAlerts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StrategyId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AlertJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReceivedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExecutedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutedTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutionResult = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Strike = table.Column<int>(type: "int", nullable: false),
                    OptionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Signal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Index = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingAlerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RiskManagementRules",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StrategyId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RuleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TriggerPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TriggerAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActionPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TriggerCondition = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsRecurring = table.Column<bool>(type: "bit", nullable: false),
                    LastTriggered = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaxTriggers = table.Column<int>(type: "int", nullable: false),
                    TriggerCount = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskManagementRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HedgeConfiguration_StrategyId",
                table: "HedgeConfigurations",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_OptionsTradePosition_ExpiryDate",
                table: "OptionsTradePositions",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_OptionsTradePosition_Strategy_Signal_Status",
                table: "OptionsTradePositions",
                columns: new[] { "StrategyId", "Signal", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PendingAlert_ExpiryTime",
                table: "PendingAlerts",
                column: "ExpiryTime");

            migrationBuilder.CreateIndex(
                name: "IX_PendingAlert_Strategy_Status",
                table: "PendingAlerts",
                columns: new[] { "StrategyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RiskManagementRule_Strategy_Enabled",
                table: "RiskManagementRules",
                columns: new[] { "StrategyId", "IsEnabled" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HedgeConfigurations");

            migrationBuilder.DropTable(
                name: "OptionsTradePositions");

            migrationBuilder.DropTable(
                name: "PendingAlerts");

            migrationBuilder.DropTable(
                name: "RiskManagementRules");

            migrationBuilder.DropColumn(
                name: "HedgeTradingSymbol",
                table: "TradePositions");

            migrationBuilder.DropColumn(
                name: "AllowMultipleSignals",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "AllowedSignals",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "AutoSquareOffOnExpiry",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "CreatedTime",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "ExpirySquareOffTimeMinutes",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "HedgeEnabled",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "HedgeRatio",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "HedgeType",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "LastUpdatedBy",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "ManualExecutionTimeoutMinutes",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "MaxDailyLoss",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "MaxPositionSize",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "MinReentryDelayMinutes",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "NotifyOnEntry",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "NotifyOnExit",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "NotifyOnProfit",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "NotifyOnStopLoss",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "UseDynamicQuantity",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "UseNearestWeeklyExpiry",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "UseOrderProtection",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Strategies",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "TradingDays",
                table: "ExecutionSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
