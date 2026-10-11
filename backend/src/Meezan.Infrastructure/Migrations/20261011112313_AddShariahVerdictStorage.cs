using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShariahVerdictStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HalalStatus",
                table: "ShariahCompliances",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerdictDetailsJson",
                table: "ShariahCompliances",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VerdictSource",
                table: "ShariahCompliances",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ViaScholarsOnly",
                table: "ShariahCompliances",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HalalStatus",
                table: "ShariahCompliances");

            migrationBuilder.DropColumn(
                name: "VerdictDetailsJson",
                table: "ShariahCompliances");

            migrationBuilder.DropColumn(
                name: "VerdictSource",
                table: "ShariahCompliances");

            migrationBuilder.DropColumn(
                name: "ViaScholarsOnly",
                table: "ShariahCompliances");
        }
    }
}
