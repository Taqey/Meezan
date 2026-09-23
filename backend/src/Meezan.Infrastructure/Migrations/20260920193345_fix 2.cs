using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    public partial class fix2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Indices",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Code", "NameAr" },
                values: new object[] { "EGX 33", "مؤشر EGX33" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Indices",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Code", "NameAr" },
                values: new object[] { "Shariah", "مؤشر الشريعة EGX30 Shariah" });
        }
    }
}