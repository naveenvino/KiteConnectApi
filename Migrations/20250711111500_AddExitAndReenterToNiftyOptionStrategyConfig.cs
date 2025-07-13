using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiteConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class AddExitAndReenterToNiftyOptionStrategyConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TriggerPrice",
                table: "Orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExitAndReenterProfitAmount",
                table: "NiftyOptionStrategyConfigs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExitAndReenterProfitPercentage",
                table: "NiftyOptionStrategyConfigs",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TriggerPrice",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ExitAndReenterProfitAmount",
                table: "NiftyOptionStrategyConfigs");

            migrationBuilder.DropColumn(
                name: "ExitAndReenterProfitPercentage",
                table: "NiftyOptionStrategyConfigs");
        }
    }
}
