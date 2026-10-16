using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MakeChangeLogFundIdNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FundCategoryChangeLogs_Funds_FundId",
                table: "FundCategoryChangeLogs");

            migrationBuilder.AlterColumn<int>(
                name: "FundId",
                table: "FundCategoryChangeLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_FundCategoryChangeLogs_Funds_FundId",
                table: "FundCategoryChangeLogs",
                column: "FundId",
                principalTable: "Funds",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FundCategoryChangeLogs_Funds_FundId",
                table: "FundCategoryChangeLogs");

            migrationBuilder.AlterColumn<int>(
                name: "FundId",
                table: "FundCategoryChangeLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FundCategoryChangeLogs_Funds_FundId",
                table: "FundCategoryChangeLogs",
                column: "FundId",
                principalTable: "Funds",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
