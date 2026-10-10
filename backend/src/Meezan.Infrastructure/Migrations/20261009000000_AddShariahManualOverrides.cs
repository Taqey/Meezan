using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShariahManualOverrides : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CoreActivityCompliantOverride",
                table: "StockShariahMetrics",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryArOverride",
                table: "StockShariahMetrics",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CategoryEnOverride",
                table: "StockShariahMetrics",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HaramRevenuePercentageOverride",
                table: "StockShariahMetrics",
                type: "decimal(18,4)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LoansPercentageOverride",
                table: "StockShariahMetrics",
                type: "decimal(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoreActivityCompliantOverride",
                table: "StockShariahMetrics");

            migrationBuilder.DropColumn(
                name: "CategoryArOverride",
                table: "StockShariahMetrics");

            migrationBuilder.DropColumn(
                name: "CategoryEnOverride",
                table: "StockShariahMetrics");

            migrationBuilder.DropColumn(
                name: "HaramRevenuePercentageOverride",
                table: "StockShariahMetrics");

            migrationBuilder.DropColumn(
                name: "LoansPercentageOverride",
                table: "StockShariahMetrics");
        }
    }
}