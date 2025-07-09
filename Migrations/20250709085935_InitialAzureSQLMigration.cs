using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KiteConnectApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialAzureSQLMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Realised",
                table: "TradePositions",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Unrealised",
                table: "TradePositions",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Realised",
                table: "TradePositions");

            migrationBuilder.DropColumn(
                name: "Unrealised",
                table: "TradePositions");
        }
    }
}
