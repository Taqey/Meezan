using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSectorDailyAndSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SectorDailies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SectorId = table.Column<int>(type: "int", nullable: false),
                    TradingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ChangePct = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    TotalMarketCap = table.Column<decimal>(type: "decimal(24,4)", nullable: false),
                    Advancers = table.Column<int>(type: "int", nullable: false),
                    Decliners = table.Column<int>(type: "int", nullable: false),
                    Unchanged = table.Column<int>(type: "int", nullable: false),
                    StocksUsed = table.Column<int>(type: "int", nullable: false),
                    StocksTotal = table.Column<int>(type: "int", nullable: false),
                    CoveragePct = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Approximate = table.Column<bool>(type: "bit", nullable: false),
                    ShareOfWeightRose = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TopContributorsJson = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    MethodUsed = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SectorDailies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SectorDailies_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockDailySnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    Ticker = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TradingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    PrevClose = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    ChangePct = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SharesUsed = table.Column<decimal>(type: "decimal(24,4)", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    SectorId = table.Column<int>(type: "int", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockDailySnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockDailySnapshots_Sectors_SectorId",
                        column: x => x.SectorId,
                        principalTable: "Sectors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_StockDailySnapshots_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SectorDailies_SectorId_TradingDate",
                table: "SectorDailies",
                columns: new[] { "SectorId", "TradingDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockDailySnapshots_SectorId",
                table: "StockDailySnapshots",
                column: "SectorId");

            migrationBuilder.CreateIndex(
                name: "IX_StockDailySnapshots_StockId_TradingDate",
                table: "StockDailySnapshots",
                columns: new[] { "StockId", "TradingDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockDailySnapshots_TradingDate",
                table: "StockDailySnapshots",
                column: "TradingDate");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Key",
                table: "SystemSettings",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SectorDailies");

            migrationBuilder.DropTable(
                name: "StockDailySnapshots");

            migrationBuilder.DropTable(
                name: "SystemSettings");
        }
    }
}
