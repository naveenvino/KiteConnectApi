using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiteConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTradingSymbolToSimulatedHistoricalData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EntryLimitPrice",
                table: "NiftyOptionStrategyConfigs",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntryOrderType",
                table: "NiftyOptionStrategyConfigs",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntryLimitPrice",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "EntryOrderType",
                table: "NiftyOptionStrategyConfigs");
        }
    }
}
