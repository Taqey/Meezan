using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShariahTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShariahCompliances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Pct = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LastCheckedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShariahCompliances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShariahCompliances_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShariahRefreshLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PctUpdatedCount = table.Column<int>(type: "int", nullable: false),
                    SkippedNoValueCount = table.Column<int>(type: "int", nullable: false),
                    SkippedNotFoundCount = table.Column<int>(type: "int", nullable: false),
                    StocksFullyRefreshedCount = table.Column<int>(type: "int", nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShariahRefreshLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShariahSourceOpinions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    SourceKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Percentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PdfUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SourceLastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExtraData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShariahSourceOpinions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShariahSourceOpinions_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockShariahMetrics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    Zakat = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    SpHaramEarningPercentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    AaoifiHaramEarningPerShare = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    HaramEarningsPercentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    LoansPercentage = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    FairValueValuation = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    BookValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Profit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    Dividend = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DividendType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CoreActivityCompliant = table.Column<bool>(type: "bit", nullable: true),
                    CashLiquidityCompliant = table.Column<bool>(type: "bit", nullable: true),
                    HaramInvestmentsCompliant = table.Column<bool>(type: "bit", nullable: true),
                    CategoryEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CategoryAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    SourceUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockShariahMetrics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockShariahMetrics_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShariahCompliances_StockId",
                table: "ShariahCompliances",
                column: "StockId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShariahSourceOpinions_StockId_SourceKey",
                table: "ShariahSourceOpinions",
                columns: new[] { "StockId", "SourceKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockShariahMetrics_StockId",
                table: "StockShariahMetrics",
                column: "StockId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShariahCompliances");

            migrationBuilder.DropTable(
                name: "ShariahRefreshLogs");

            migrationBuilder.DropTable(
                name: "ShariahSourceOpinions");

            migrationBuilder.DropTable(
                name: "StockShariahMetrics");
        }
    }
}
