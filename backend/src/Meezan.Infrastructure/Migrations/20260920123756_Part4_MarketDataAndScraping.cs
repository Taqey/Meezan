using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Part4_MarketDataAndScraping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScrapeRunLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TriggeredBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TotalStocks = table.Column<int>(type: "int", nullable: false),
                    SucceededStocks = table.Column<int>(type: "int", nullable: false),
                    FailedStocks = table.Column<int>(type: "int", nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScrapeRunLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockFairValues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    FairValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    PriceComparison = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FairValueDiff = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    FairValueDiffPct = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MethodsUsedCount = table.Column<int>(type: "int", nullable: false),
                    MethodsExcludedCount = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockFairValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockFairValues_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockMarketData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    NominalValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    MarketValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    BookValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    PbRatio = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Eps = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    PeRatio = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    High = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Low = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Open = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ClosingPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SourceLastUpdateText = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMarketData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMarketData_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockSupportResistance",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    LastPrice = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ChangePct = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Pivot = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    R1 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    R2 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    S1 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    S2 = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    FetchedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockSupportResistance", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockSupportResistance_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockFairValueMethods",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockFairValueId = table.Column<int>(type: "int", nullable: false),
                    MethodName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EstimatedValue = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    IsOutlier = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockFairValueMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockFairValueMethods_StockFairValues_StockFairValueId",
                        column: x => x.StockFairValueId,
                        principalTable: "StockFairValues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockFairValueMethods_StockFairValueId",
                table: "StockFairValueMethods",
                column: "StockFairValueId");

            migrationBuilder.CreateIndex(
                name: "IX_StockFairValues_StockId",
                table: "StockFairValues",
                column: "StockId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockMarketData_StockId",
                table: "StockMarketData",
                column: "StockId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockSupportResistance_StockId",
                table: "StockSupportResistance",
                column: "StockId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScrapeRunLogs");

            migrationBuilder.DropTable(
                name: "StockFairValueMethods");

            migrationBuilder.DropTable(
                name: "StockMarketData");

            migrationBuilder.DropTable(
                name: "StockSupportResistance");

            migrationBuilder.DropTable(
                name: "StockFairValues");
        }
    }
}
