using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiteConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedTradePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "HedgeOrderId",
                table: "TradePositions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "EntryOrderId",
                table: "TradePositions",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<double>(
                name: "NetPremium",
                table: "TradePositions",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "StopLossOrderId",
                table: "TradePositions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "StopLossPrice",
                table: "TradePositions",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TakeProfitOrderId",
                table: "TradePositions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TakeProfitPrice",
                table: "TradePositions",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NetPremium",
                table: "TradePositions");

            migrationBuilder.DropColumn(
                name: "StopLossOrderId",
                table: "TradePositions");

            migrationBuilder.DropColumn(
                name: "StopLossPrice",
                table: "TradePositions");

            migrationBuilder.DropColumn(
                name: "TakeProfitOrderId",
                table: "TradePositions");

            migrationBuilder.DropColumn(
                name: "TakeProfitPrice",
                table: "TradePositions");

            migrationBuilder.AlterColumn<string>(
                name: "HedgeOrderId",
                table: "TradePositions",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EntryOrderId",
                table: "TradePositions",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
