using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShariahBoardFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "HasShariahBoard",
                table: "ShariahCompliances",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // Populate the flag from the source data: any stock whose Shariah source-opinion
            // notes indicate board governance — a plain "لجنة شرعية" (committee) or an
            // accredited "هيئة رقابة شرعية داخلية معتمدة" (internal board) alike.
            migrationBuilder.Sql(@"
UPDATE c
SET c.HasShariahBoard = 1
FROM ShariahCompliances AS c
WHERE EXISTS (
    SELECT 1
    FROM ShariahSourceOpinions AS o
    WHERE o.StockId = c.StockId
      AND ((o.Note LIKE N'%لجنة%' AND o.Note LIKE N'%شرعية%')
            OR o.Note LIKE N'%هيئة رقابة شرعية%'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasShariahBoard",
                table: "ShariahCompliances");
        }
    }
}
