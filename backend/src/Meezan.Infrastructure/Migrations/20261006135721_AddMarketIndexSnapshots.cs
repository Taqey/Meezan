using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketIndexSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketIndexSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    MubasherCode = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Change = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ChangePct = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SourceDateText = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Source = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketIndexSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketIndexSnapshots_Kind_Code",
                table: "MarketIndexSnapshots",
                columns: new[] { "Kind", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketIndexSnapshots");
        }
    }
}
