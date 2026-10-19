using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSectorStockAndConstituentIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stocks_SectorId",
                table: "Stocks");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_SectorId_IsActive_DataStatus",
                table: "Stocks",
                columns: new[] { "SectorId", "IsActive", "DataStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stocks_SectorId_IsActive_DataStatus",
                table: "Stocks");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_SectorId",
                table: "Stocks",
                column: "SectorId");
        }
    }
}
