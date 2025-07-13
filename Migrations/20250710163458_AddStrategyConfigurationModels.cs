using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiteConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class AddStrategyConfigurationModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Strategies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BrokerLevelSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StrategyId = table.Column<int>(type: "int", nullable: false),
                    OverallStopLoss = table.Column<double>(type: "float", nullable: true),
                    OverallTarget = table.Column<double>(type: "float", nullable: true),
                    IncrementProfitBy = table.Column<double>(type: "float", nullable: true),
                    TrailSLBy = table.Column<double>(type: "float", nullable: true),
                    LockProfitAt = table.Column<double>(type: "float", nullable: true),
                    MinimumProfitToLock = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrokerLevelSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BrokerLevelSettings_Strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "Strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StrategyId = table.Column<int>(type: "int", nullable: false),
                    EntryTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ExitTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    ProductType = table.Column<int>(type: "int", nullable: false),
                    EntryOrderType = table.Column<int>(type: "int", nullable: false),
                    ExitOrderType = table.Column<int>(type: "int", nullable: false),
                    LimitBuffer = table.Column<double>(type: "float", nullable: true),
                    TargetSLRefPrice = table.Column<int>(type: "int", nullable: false),
                    QuantityMultiplier = table.Column<int>(type: "int", nullable: false),
                    DelayEntryBySeconds = table.Column<int>(type: "int", nullable: false),
                    TradingDays = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AutoSquareoff = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionSettings_Strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "Strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Legs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StrategyId = table.Column<int>(type: "int", nullable: false),
                    UnderlyingAsset = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StrikePrice = table.Column<double>(type: "float", nullable: false),
                    OptionType = table.Column<int>(type: "int", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    QuantityLots = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Legs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Legs_Strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "Strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrokerLevelSettings_StrategyId",
                table: "BrokerLevelSettings",
                column: "StrategyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionSettings_StrategyId",
                table: "ExecutionSettings",
                column: "StrategyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Legs_StrategyId",
                table: "Legs",
                column: "StrategyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BrokerLevelSettings");

            migrationBuilder.DropTable(
                name: "ExecutionSettings");

            migrationBuilder.DropTable(
                name: "Legs");

            migrationBuilder.DropTable(
                name: "Strategies");
        }
    }
}
