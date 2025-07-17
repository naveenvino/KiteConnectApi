using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiteConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class AddApiTradingDashboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiTradeLog",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    WeekStartDate = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SignalId = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    StopLoss = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EntryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExitTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TradingSymbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Strike = table.Column<int>(type: "int", nullable: false),
                    OptionType = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    EntryPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    PnL = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ExpiryDay = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Confidence = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiTradeLog", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OptionsHistoricalData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TradingSymbol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Exchange = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Underlying = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Strike = table.Column<int>(type: "int", nullable: false),
                    OptionType = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Open = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    High = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Low = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Close = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Volume = table.Column<long>(type: "bigint", nullable: false),
                    OpenInterest = table.Column<long>(type: "bigint", nullable: false),
                    Delta = table.Column<decimal>(type: "decimal(8,4)", nullable: true),
                    Gamma = table.Column<decimal>(type: "decimal(8,4)", nullable: true),
                    Theta = table.Column<decimal>(type: "decimal(8,4)", nullable: true),
                    Vega = table.Column<decimal>(type: "decimal(8,4)", nullable: true),
                    ImpliedVolatility = table.Column<decimal>(type: "decimal(8,4)", nullable: true),
                    BidPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    AskPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    BidAskSpread = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DataSource = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Interval = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OptionsHistoricalData", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OptionsHistoricalData_Symbol_Timestamp",
                table: "OptionsHistoricalData",
                columns: new[] { "TradingSymbol", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_OptionsHistoricalData_Timestamp",
                table: "OptionsHistoricalData",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_OptionsHistoricalData_Underlying_Strike_Type_Expiry",
                table: "OptionsHistoricalData",
                columns: new[] { "Underlying", "Strike", "OptionType", "ExpiryDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiTradeLog");

            migrationBuilder.DropTable(
                name: "OptionsHistoricalData");
        }
    }
}
