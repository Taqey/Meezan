using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFundClassificationTree : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FundClassificationNodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
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
                    RequiresChildSelection = table.Column<bool>(type: "bit", nullable: false),
                    ChildrenLabelAr = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ChildrenLabelEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AutoRuleJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AutoClassificationEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundClassificationNodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundClassificationNodes_FundClassificationNodes_ParentId",
                        column: x => x.ParentId,
                        principalTable: "FundClassificationNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FundClassificationChangeLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: true),
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FundNavigationId = table.Column<int>(type: "int", nullable: true),
                    NodeNavigationId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundClassificationChangeLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundClassificationChangeLogs_FundClassificationNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "FundClassificationNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundClassificationChangeLogs_FundClassificationNodes_NodeNavigationId",
                        column: x => x.NodeNavigationId,
                        principalTable: "FundClassificationNodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FundClassificationChangeLogs_Funds_FundId",
                        column: x => x.FundId,
                        principalTable: "Funds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FundClassificationChangeLogs_Funds_FundNavigationId",
                        column: x => x.FundNavigationId,
                        principalTable: "Funds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FundClassificationOverrides",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    NodeId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FundClassificationNodeId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundClassificationOverrides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FundClassificationOverrides_FundClassificationNodes_FundClassificationNodeId",
                        column: x => x.FundClassificationNodeId,
                        principalTable: "FundClassificationNodes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_FundClassificationOverrides_FundClassificationNodes_NodeId",
                        column: x => x.NodeId,
                        principalTable: "FundClassificationNodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FundClassificationOverrides_Funds_FundId",
                        column: x => x.FundId,
                        principalTable: "Funds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationChangeLogs_CreatedAt",
                table: "FundClassificationChangeLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationChangeLogs_FundId",
                table: "FundClassificationChangeLogs",
                column: "FundId");

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationChangeLogs_FundNavigationId",
                table: "FundClassificationChangeLogs",
                column: "FundNavigationId");

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationChangeLogs_NodeId",
                table: "FundClassificationChangeLogs",
                column: "NodeId");

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationChangeLogs_NodeNavigationId",
                table: "FundClassificationChangeLogs",
                column: "NodeNavigationId");

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationNodes_Level",
                table: "FundClassificationNodes",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationNodes_ParentId_DisplayOrder",
                table: "FundClassificationNodes",
                columns: new[] { "ParentId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationNodes_Slug",
                table: "FundClassificationNodes",
                column: "Slug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationOverrides_FundClassificationNodeId",
                table: "FundClassificationOverrides",
                column: "FundClassificationNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationOverrides_FundId_NodeId",
                table: "FundClassificationOverrides",
                columns: new[] { "FundId", "NodeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationOverrides_NodeId",
                table: "FundClassificationOverrides",
                column: "NodeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundClassificationChangeLogs");

            migrationBuilder.DropTable(
                name: "FundClassificationOverrides");

            migrationBuilder.DropTable(
                name: "FundClassificationNodes");
        }
    }
}
