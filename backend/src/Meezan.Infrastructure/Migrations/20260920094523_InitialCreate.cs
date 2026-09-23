using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Indices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Indices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sectors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sectors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UploadHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndexId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowsAffected = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    UploadedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UploadHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UploadHistories_Indices_IndexId",
                        column: x => x.IndexId,
                        principalTable: "Indices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SymbolCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    ReutersCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SectorId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stocks_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "IndexConstituents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IndexId = table.Column<int>(type: "int", nullable: false),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,8)", precision: 18, scale: 8, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndexConstituents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndexConstituents_Indices_IndexId",
                        column: x => x.IndexId,
                        principalTable: "Indices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IndexConstituents_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Indices",
                columns: new[] { "Id", "Code", "Description", "LastUpdated", "NameAr", "NameEn" },
                values: new object[,]
                {
                    { 1, "EGX30", "Top 30 liquid and active companies", null, "المؤشر الرئيسي للبورصة المصرية EGX30", "EGX 30 Index" },
                    { 2, "EGX30TR", "EGX 30 Total Return Index including dividend reinvestment", null, "مؤشر العائد الكلي EGX30TR", "EGX 30 Total Return Index" },
                    { 3, "EGX70", "EGX 70 Equal Weights Index", null, "مؤشر الشركات الصغيرة والمتوسطة EGX70 EWI", "EGX 70 EWI Index" },
                    { 4, "EGX100", "EGX 100 Equal Weights Index", null, "المؤشر الأوسع نطاقاً EGX100 EWI", "EGX 100 EWI Index" },
                    { 5, "EGX35-LV", "EGX 35 Low Volatility Index", null, "مؤشر التذبذب المنخفض EGX35-LV", "EGX 35 Low Volatility Index" },
                    { 6, "Shariah", "Shariah Compliant Index", null, "مؤشر الشريعة EGX30 Shariah", "EGX 33 Shariah Index" },
                    { 7, "Sectoral-Indices", "EGX Sectoral Indices Constituents", null, "المؤشرات القطاعية", "Sectoral Indices" },
                    { 8, "TAMAYUZ", "Tamayuz Small & Medium Enterprises Index", null, "مؤشر سوق الشركات الصغيرة والمتوسطة تميز", "Tamayuz Index" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_IndexConstituents_IndexId_StockId",
                table: "IndexConstituents",
                columns: new[] { "IndexId", "StockId" });

            migrationBuilder.CreateIndex(
                name: "IX_IndexConstituents_StockId",
                table: "IndexConstituents",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_Indices_Code",
                table: "Indices",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_NameAr",
                table: "Sectors",
                column: "NameAr");

            migrationBuilder.CreateIndex(
                name: "IX_Sectors_NameEn",
                table: "Sectors",
                column: "NameEn");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ReutersCode",
                table: "Stocks",
                column: "ReutersCode");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_SectorId",
                table: "Stocks",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_SymbolCode",
                table: "Stocks",
                column: "SymbolCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UploadHistories_IndexId",
                table: "UploadHistories",
                column: "IndexId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndexConstituents");

            migrationBuilder.DropTable(
                name: "UploadHistories");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "Indices");

            migrationBuilder.DropTable(
                name: "Sectors");
        }
    }
}
