using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CollapseStockCodesToTicker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Re-point child rows for the 179 duplicate stocks created by Shariah seed/refresh
            migrationBuilder.Sql(@"
;WITH ComputedStocks AS (
    SELECT 
        Id, 
        SymbolCode, 
        ReutersCode,
        UPPER(LTRIM(RTRIM(
            CASE 
                WHEN ReutersCode IS NOT NULL AND LTRIM(RTRIM(ReutersCode)) <> '' THEN
                    CASE 
                        WHEN CHARINDEX('.', LTRIM(RTRIM(ReutersCode))) > 0 
                        THEN SUBSTRING(LTRIM(RTRIM(ReutersCode)), 1, CHARINDEX('.', LTRIM(RTRIM(ReutersCode))) - 1)
                        ELSE LTRIM(RTRIM(ReutersCode))
                    END
                ELSE LTRIM(RTRIM(SymbolCode))
            END
        ))) AS ComputedTicker
    FROM Stocks
),
RankedStocks AS (
    SELECT 
        Id,
        ComputedTicker,
        ROW_NUMBER() OVER (
            PARTITION BY ComputedTicker 
            ORDER BY 
                CASE WHEN ReutersCode IS NOT NULL AND ReutersCode <> '' THEN 0 ELSE 1 END,
                Id ASC
        ) as rn,
        FIRST_VALUE(Id) OVER (
            PARTITION BY ComputedTicker 
            ORDER BY 
                CASE WHEN ReutersCode IS NOT NULL AND ReutersCode <> '' THEN 0 ELSE 1 END,
                Id ASC
        ) as TargetId
    FROM ComputedStocks
)
SELECT Id as DuplicateId, TargetId, ComputedTicker
INTO #StockMergeMap
FROM RankedStocks
WHERE Id <> TargetId;

-- Reparent ShariahCompliances
UPDATE sc
SET sc.StockId = m.TargetId
FROM ShariahCompliances sc
JOIN #StockMergeMap m ON sc.StockId = m.DuplicateId;

-- Reparent StockShariahMetrics
UPDATE sm
SET sm.StockId = m.TargetId
FROM StockShariahMetrics sm
JOIN #StockMergeMap m ON sm.StockId = m.DuplicateId;

-- Reparent ShariahSourceOpinions
UPDATE so
SET so.StockId = m.TargetId
FROM ShariahSourceOpinions so
JOIN #StockMergeMap m ON so.StockId = m.DuplicateId;

-- Reparent IndexConstituents
UPDATE ic
SET ic.StockId = m.TargetId
FROM IndexConstituents ic
JOIN #StockMergeMap m ON ic.StockId = m.DuplicateId;

-- Delete duplicate stocks
DELETE s
FROM Stocks s
JOIN #StockMergeMap m ON s.Id = m.DuplicateId;

DROP TABLE #StockMergeMap;
");

            // 2. Backfill SymbolCode with Computed Ticker (ReutersCode minus .CA, else SymbolCode) for all remaining stocks
            migrationBuilder.Sql(@"
UPDATE Stocks
SET SymbolCode = UPPER(LTRIM(RTRIM(
    CASE 
        WHEN ReutersCode IS NOT NULL AND LTRIM(RTRIM(ReutersCode)) <> '' THEN
            CASE 
                WHEN CHARINDEX('.', LTRIM(RTRIM(ReutersCode))) > 0 
                THEN SUBSTRING(LTRIM(RTRIM(ReutersCode)), 1, CHARINDEX('.', LTRIM(RTRIM(ReutersCode))) - 1)
                ELSE LTRIM(RTRIM(ReutersCode))
            END
        ELSE LTRIM(RTRIM(SymbolCode))
    END
)));
");

            migrationBuilder.DropIndex(
                name: "IX_Stocks_ReutersCode",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "ReutersCode",
                table: "Stocks");

            migrationBuilder.RenameColumn(
                name: "SymbolCode",
                table: "Stocks",
                newName: "Ticker");

            migrationBuilder.RenameIndex(
                name: "IX_Stocks_SymbolCode",
                table: "Stocks",
                newName: "IX_Stocks_Ticker");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Ticker",
                table: "Stocks",
                newName: "SymbolCode");

            migrationBuilder.RenameIndex(
                name: "IX_Stocks_Ticker",
                table: "Stocks",
                newName: "IX_Stocks_SymbolCode");

            migrationBuilder.AddColumn<string>(
                name: "ReutersCode",
                table: "Stocks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ReutersCode",
                table: "Stocks",
                column: "ReutersCode");
        }
    }
}
