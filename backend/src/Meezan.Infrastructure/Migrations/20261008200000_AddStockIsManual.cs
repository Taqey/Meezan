using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockIsManual : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add IsManual as nullable first so we can backfill existing rows safely.
            // Using defaultValue: false (which EF generates) would mark all existing rows
            // as non-manual — that is actually the correct intent here.
            migrationBuilder.AddColumn<bool>(
                name: "IsManual",
                table: "Stocks",
                type: "bit",
                nullable: true);

            // All pre-existing stocks have no manual assignment — set IsManual = 0.
            migrationBuilder.Sql("UPDATE Stocks SET IsManual = 0 WHERE IsManual IS NULL;");

            // Now make the column NOT NULL.
            migrationBuilder.AlterColumn<bool>(
                name: "IsManual",
                table: "Stocks",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool?),
                oldType: "bit",
                oldNullable: true);

            // Add DB-level DEFAULT (0) so raw SQL inserts (e.g. seed scripts) also get the right value.
            migrationBuilder.Sql("ALTER TABLE Stocks ADD DEFAULT (0) FOR IsManual;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsManual",
                table: "Stocks");
        }
    }
}
