using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RecalcFairValuePremiumConvention : Migration
    {
        /// <summary>
        /// One-time recalculation of every stock's persisted fair-value gap to the
        /// premium convention — the same formula as
        /// FairValueCalculator.ComputePremium:
        ///   premiumPct = (price − fairValue) / fairValue × 100
        /// (positive = overvalued/Expensive, negative = undervalued/Cheap,
        /// within ±2% of fair value = Fair; missing/invalid inputs = nulls and
        /// 'Unavailable'). The detail endpoint itself always computes these at
        /// response time; this backfill only aligns the persisted columns used by
        /// list sorting/filtering.
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE fv
                SET
                    FairValueDiff = md.ClosingPrice - fv.FairValue,
                    FairValueDiffPct = CASE
                        WHEN md.ClosingPrice IS NULL OR md.ClosingPrice <= 0 OR fv.FairValue IS NULL OR fv.FairValue <= 0 THEN NULL
                        ELSE (md.ClosingPrice - fv.FairValue) / fv.FairValue * 100
                    END,
                    PriceComparison = CASE
                        WHEN md.ClosingPrice IS NULL OR md.ClosingPrice <= 0 OR fv.FairValue IS NULL OR fv.FairValue <= 0 THEN 'Unavailable'
                        WHEN md.ClosingPrice > fv.FairValue * 1.02 THEN 'Expensive'
                        WHEN md.ClosingPrice < fv.FairValue * 0.98 THEN 'Cheap'
                        ELSE 'Fair'
                    END
                FROM StockFairValues fv
                LEFT JOIN StockMarketData md ON md.StockId = fv.StockId;
                """);
        }

        /// <summary>
        /// Restores the previous upside convention ((price − fair) / price with a
        /// ±5% tolerance) for rollback purposes only.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE fv
                SET
                    FairValueDiff = md.ClosingPrice - fv.FairValue,
                    FairValueDiffPct = CASE
                        WHEN md.ClosingPrice IS NULL OR md.ClosingPrice <= 0 OR fv.FairValue IS NULL OR fv.FairValue <= 0 THEN NULL
                        ELSE (md.ClosingPrice - fv.FairValue) / md.ClosingPrice * 100
                    END,
                    PriceComparison = CASE
                        WHEN md.ClosingPrice IS NULL OR md.ClosingPrice <= 0 OR fv.FairValue IS NULL OR fv.FairValue <= 0 THEN 'Unavailable'
                        WHEN fv.FairValue > md.ClosingPrice * 1.05 THEN 'Cheap'
                        WHEN fv.FairValue < md.ClosingPrice * 0.95 THEN 'Expensive'
                        ELSE 'Fair'
                    END
                FROM StockFairValues fv
                LEFT JOIN StockMarketData md ON md.StockId = fv.StockId;
                """);
        }
    }
}
