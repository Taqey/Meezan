using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Meezan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFundTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundTypeDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundTypeDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FundTypeChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: true),
                    FundTypeDefinitionId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<int>(type: "int", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundTypeChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundTypeChangeLogs_FundTypeDefinitions_FundTypeDefinitionId",
                        column: x => x.FundTypeDefinitionId,
                        principalTable: "FundTypeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FundTypeChangeLogs_Funds_FundId",
                        column: x => x.FundId,
                        principalTable: "Funds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FundTypeOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    FundTypeDefinitionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundTypeOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundTypeOverrides_FundTypeDefinitions_FundTypeDefinitionId",
                        column: x => x.FundTypeDefinitionId,
                        principalTable: "FundTypeDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundTypeOverrides_Funds_FundId",
                        column: x => x.FundId,
                        principalTable: "Funds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "FundTypeDefinitions",
                columns: new[] { "Id", "Color", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedReason", "DisplayOrder", "Icon", "IsDeleted", "IsVisible", "NameAr", "NameEn", "Slug", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", null, "", 1, "", false, true, "صندوق أسهم", "Equity Fund", "equity", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed" },
                    { 2, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", null, "", 2, "", false, true, "صندوق دخل ثابت", "Fixed Income Fund", "fixed_income", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed" },
                    { 3, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", null, "", 3, "", false, true, "صندوق دخل ثابت بالدولار", "USD Fixed Income Fund", "fixed_income_usd", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed" },
                    { 4, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", null, "", 4, "", false, true, "صندوق متوازن", "Balanced Fund", "balanced", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed" },
                    { 5, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", null, "", 5, "", false, true, "صندوق سوق نقدي", "Money Market Fund", "money_market", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed" },
                    { 6, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", null, "", 6, "", false, true, "صندوق مؤشر", "Index Fund", "index", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed" },
                    { 7, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", null, "", 7, "", false, true, "صندوق ذهب", "Gold Fund", "gold", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed" },
                    { 8, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", null, "", 8, "", false, true, "صندوق سلع", "Commodities Fund", "commodities", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed" },
                    { 9, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", null, "", 9, "", false, true, "صندوق الحفاظ على رأس المال", "Capital Preservation Fund", "capital_preservation", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed" },
                    { 10, "", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed", null, "", 99, "", false, true, "غير محدد", "Unclassified", "unknown", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Seed" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundTypeChangeLogs_CreatedAt",
                table: "FundTypeChangeLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FundTypeChangeLogs_FundId",
                table: "FundTypeChangeLogs",
                column: "FundId");

            migrationBuilder.CreateIndex(
                name: "IX_FundTypeChangeLogs_FundTypeDefinitionId",
                table: "FundTypeChangeLogs",
                column: "FundTypeDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_FundTypeDefinitions_DisplayOrder",
                table: "FundTypeDefinitions",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_FundTypeDefinitions_Slug",
                table: "FundTypeDefinitions",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundTypeOverrides_FundId",
                table: "FundTypeOverrides",
                column: "FundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundTypeOverrides_FundTypeDefinitionId",
                table: "FundTypeOverrides",
                column: "FundTypeDefinitionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundTypeChangeLogs");

            migrationBuilder.DropTable(
                name: "FundTypeOverrides");

            migrationBuilder.DropTable(
                name: "FundTypeDefinitions");
        }
    }
}
