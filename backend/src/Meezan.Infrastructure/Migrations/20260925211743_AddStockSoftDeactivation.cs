using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockSoftDeactivation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                table: "Stocks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                table: "Stocks",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // Add IsActive as nullable first so we can safely set it on existing rows.
            // Using defaultValue: false (which EF generates by default) would incorrectly
            // deactivate all existing stocks. Instead we backfill to 1 (active) manually,
            // then alter the column to NOT NULL — following the same pattern as DataStatus.
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Stocks",
                type: "bit",
                nullable: true);

            // All pre-existing stocks are active — set IsActive = 1 before making it NOT NULL.
            migrationBuilder.Sql("UPDATE Stocks SET IsActive = 1 WHERE IsActive IS NULL;");

            // Now make the column NOT NULL (SQL Server allows this once all rows have a value).
            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Stocks",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool?),
                oldType: "bit",
                oldNullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Stocks");
        }
    }
}
