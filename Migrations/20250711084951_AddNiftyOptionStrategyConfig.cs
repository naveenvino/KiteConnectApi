using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiteConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class AddNiftyOptionStrategyConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EntryTime",
                table: "TradePositions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExitTime",
                table: "TradePositions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StrategyConfigId",
                table: "TradePositions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NiftyOptionStrategyConfigs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StrategyName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UnderlyingInstrument = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Exchange = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProductType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    AllocatedMargin = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntryTime = table.Column<int>(type: "int", nullable: false),
                    ExitTime = table.Column<int>(type: "int", nullable: false),
                    StopLossPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TargetPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TakeProfitPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxTradesPerDay = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    HedgeDistancePoints = table.Column<int>(type: "int", nullable: false),
                    HedgePremiumPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstrumentPrefix = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LockProfitPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LockProfitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrailStopLossPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TrailStopLossAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OverallPositionStopLoss = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MoveStopLossToEntryPricePercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MoveStopLossToEntryPriceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NiftyOptionStrategyConfigs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "EntryTime",
                table: "TradePositions");

            migrationBuilder.DropColumn(
                name: "ExitTime",
                table: "TradePositions");

            migrationBuilder.DropColumn(
                name: "StrategyConfigId",
                table: "TradePositions");
        }
    }
}
