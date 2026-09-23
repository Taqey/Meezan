using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Indices",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Code", "NameAr" },
                values: new object[] { "Shariah", "مؤشر الشريعة EGX30 Shariah" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Indices",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Code", "NameAr" },
                values: new object[] { "EGX 33", "مؤشر الشريعة EGX33 Shariah" });
        }
    }
}
