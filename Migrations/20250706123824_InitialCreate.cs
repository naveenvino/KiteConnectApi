using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiteConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TradePositions",
                columns: table => new
                {
                    PositionId = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    EntryTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExitTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EntryInstrumentToken = table.Column<int>(type: "INTEGER", nullable: false),
                    EntryTradingSymbol = table.Column<string>(type: "TEXT", nullable: false),
                    EntryOrderId = table.Column<string>(type: "TEXT", nullable: false),
                    EntryPrice = table.Column<double>(type: "REAL", nullable: false),
                    HedgeInstrumentToken = table.Column<int>(type: "INTEGER", nullable: false),
                    HedgeTradingSymbol = table.Column<string>(type: "TEXT", nullable: false),
                    HedgeOrderId = table.Column<string>(type: "TEXT", nullable: false),
                    HedgePrice = table.Column<double>(type: "REAL", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Strike = table.Column<int>(type: "INTEGER", nullable: false),
                    OptionType = table.Column<string>(type: "TEXT", nullable: false),
                    Expiry = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradePositions", x => x.PositionId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TradePositions");
        }
    }
}
