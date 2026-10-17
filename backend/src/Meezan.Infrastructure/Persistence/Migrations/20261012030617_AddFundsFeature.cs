using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFundsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsAutoDerived = table.Column<bool>(type: "bit", nullable: false),
                    AutoRuleJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    IsVisible = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_FundCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Funds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SndukId = table.Column<long>(type: "bigint", nullable: false),
                    PublicId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    TypeNameAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TypeNameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RiskLevel = table.Column<int>(type: "int", nullable: false),
                    IsIslamic = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CurrentPrice = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    DailyChange = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Return1M = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Return3M = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Return6M = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Return1Y = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Return3Y = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    TotalScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Aum = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ManagementFee = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    MinimumInvestment = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    AssetManagerId = table.Column<long>(type: "bigint", nullable: true),
                    AssetManagerName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AssetManagerNameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AssetManagerCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LocalLogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DescriptionAr = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DescriptionEn = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    InvestmentStrategy = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    InceptionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProspectusUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SndukCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SndukUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastSyncedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Funds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FundSyncLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RunAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalFunds = table.Column<int>(type: "int", nullable: false),
                    UpdatedFunds = table.Column<int>(type: "int", nullable: false),
                    FailedFunds = table.Column<int>(type: "int", nullable: false),
                    SkippedFunds = table.Column<int>(type: "int", nullable: false),
                    ErrorSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TriggeredBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DurationSeconds = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundSyncLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FundSyncSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RunTimeCairo = table.Column<TimeOnly>(type: "time", nullable: false),
                    PreventOverlap = table.Column<bool>(type: "bit", nullable: false),
                    RequestDelaySeconds = table.Column<int>(type: "int", nullable: false),
                    MaxRetries = table.Column<int>(type: "int", nullable: false),
                    RetryBackoffSeconds = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundSyncSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FundAnalyses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    PerformanceScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    RiskScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    CostScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    StabilityScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    DiversificationScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    TotalScore = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Cagr = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Alpha = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Volatility = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Consistency = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    MaxDrawdown = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    SharpeRatio = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    WeightsJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PerformanceExplanationAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    PerformanceExplanationEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RiskExplanationAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    RiskExplanationEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CostExplanationAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CostExplanationEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StabilityExplanationAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    StabilityExplanationEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DiversificationExplanationAr = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    DiversificationExplanationEn = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundAnalyses_Funds_FundId",
                        column: x => x.FundId,
                        principalTable: "Funds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundCategoryChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundCategoryChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundCategoryChangeLogs_FundCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FundCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundCategoryChangeLogs_Funds_FundId",
                        column: x => x.FundId,
                        principalTable: "Funds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundCategoryOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundCategoryOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundCategoryOverrides_FundCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "FundCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundCategoryOverrides_Funds_FundId",
                        column: x => x.FundId,
                        principalTable: "Funds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundPriceHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Change = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    Performance = table.Column<decimal>(type: "decimal(10,4)", precision: 10, scale: 4, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundPriceHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundPriceHistory_Funds_FundId",
                        column: x => x.FundId,
                        principalTable: "Funds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FundSimilars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    SimilarFundId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundSimilars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundSimilars_Funds_FundId",
                        column: x => x.FundId,
                        principalTable: "Funds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundSimilars_Funds_SimilarFundId",
                        column: x => x.SimilarFundId,
                        principalTable: "Funds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FundSyncLogDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundSyncLogId = table.Column<int>(type: "int", nullable: false),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    FundCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundSyncLogDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundSyncLogDetails_FundSyncLogs_FundSyncLogId",
                        column: x => x.FundSyncLogId,
                        principalTable: "FundSyncLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundAnalyses_FundId",
                table: "FundAnalyses",
                column: "FundId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundCategories_Slug",
                table: "FundCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundCategoryChangeLogs_CategoryId",
                table: "FundCategoryChangeLogs",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FundCategoryChangeLogs_FundId",
                table: "FundCategoryChangeLogs",
                column: "FundId");

            migrationBuilder.CreateIndex(
                name: "IX_FundCategoryOverrides_CategoryId",
                table: "FundCategoryOverrides",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FundCategoryOverrides_FundId_CategoryId",
                table: "FundCategoryOverrides",
                columns: new[] { "FundId", "CategoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundPriceHistory_FundId_Date",
                table: "FundPriceHistory",
                columns: new[] { "FundId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Funds_Code",
                table: "Funds",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Funds_SndukId",
                table: "Funds",
                column: "SndukId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundSimilars_FundId_SimilarFundId",
                table: "FundSimilars",
                columns: new[] { "FundId", "SimilarFundId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundSimilars_SimilarFundId",
                table: "FundSimilars",
                column: "SimilarFundId");

            migrationBuilder.CreateIndex(
                name: "IX_FundSyncLogDetails_FundSyncLogId",
                table: "FundSyncLogDetails",
                column: "FundSyncLogId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundAnalyses");

            migrationBuilder.DropTable(
                name: "FundCategoryChangeLogs");

            migrationBuilder.DropTable(
                name: "FundCategoryOverrides");

            migrationBuilder.DropTable(
                name: "FundPriceHistory");

            migrationBuilder.DropTable(
                name: "FundSimilars");

            migrationBuilder.DropTable(
                name: "FundSyncLogDetails");

            migrationBuilder.DropTable(
                name: "FundSyncSettings");

            migrationBuilder.DropTable(
                name: "FundCategories");

            migrationBuilder.DropTable(
                name: "Funds");

            migrationBuilder.DropTable(
                name: "FundSyncLogs");
        }
    }
}
