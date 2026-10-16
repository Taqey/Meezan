using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFundClassificationBackupAndSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FundClassificationChangeLogs_FundClassificationNodes_NodeNavigationId",
                table: "FundClassificationChangeLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_FundClassificationChangeLogs_Funds_FundNavigationId",
                table: "FundClassificationChangeLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_FundClassificationOverrides_FundClassificationNodes_FundClassificationNodeId",
                table: "FundClassificationOverrides");

            migrationBuilder.DropIndex(
                name: "IX_FundClassificationOverrides_FundClassificationNodeId",
                table: "FundClassificationOverrides");

            migrationBuilder.DropIndex(
                name: "IX_FundClassificationChangeLogs_FundNavigationId",
                table: "FundClassificationChangeLogs");

            migrationBuilder.DropIndex(
                name: "IX_FundClassificationChangeLogs_NodeNavigationId",
                table: "FundClassificationChangeLogs");

            migrationBuilder.DropColumn(
                name: "FundClassificationNodeId",
                table: "FundClassificationOverrides");

            migrationBuilder.DropColumn(
                name: "FundNavigationId",
                table: "FundClassificationChangeLogs");

            migrationBuilder.DropColumn(
                name: "NodeNavigationId",
                table: "FundClassificationChangeLogs");

            migrationBuilder.AddColumn<bool>(
                name: "AutoClassificationEnabled",
                table: "FundSyncSettings",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AlterColumn<int>(
                name: "NodeId",
                table: "FundClassificationChangeLogs",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "FundClassificationBackups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FundId = table.Column<int>(type: "int", nullable: false),
                    FundCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FundNameAr = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    FundNameEn = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    OldType = table.Column<int>(type: "int", nullable: false),
                    OldTypeSlug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    OldTypeOverrideSlug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OldCategorySlugs = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OldManualIncludeSlugs = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OldManualExcludeSlugs = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    NewPath = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MigrationReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BackedUpAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BackedUpBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FundClassificationBackups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationBackups_FundCode",
                table: "FundClassificationBackups",
                column: "FundCode");

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationBackups_FundId",
                table: "FundClassificationBackups",
                column: "FundId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FundClassificationBackups");

            migrationBuilder.DropColumn(
                name: "AutoClassificationEnabled",
                table: "FundSyncSettings");

            migrationBuilder.AddColumn<int>(
                name: "FundClassificationNodeId",
                table: "FundClassificationOverrides",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "NodeId",
                table: "FundClassificationChangeLogs",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FundNavigationId",
                table: "FundClassificationChangeLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NodeNavigationId",
                table: "FundClassificationChangeLogs",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationOverrides_FundClassificationNodeId",
                table: "FundClassificationOverrides",
                column: "FundClassificationNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationChangeLogs_FundNavigationId",
                table: "FundClassificationChangeLogs",
                column: "FundNavigationId");

            migrationBuilder.CreateIndex(
                name: "IX_FundClassificationChangeLogs_NodeNavigationId",
                table: "FundClassificationChangeLogs",
                column: "NodeNavigationId");

            migrationBuilder.AddForeignKey(
                name: "FK_FundClassificationChangeLogs_FundClassificationNodes_NodeNavigationId",
                table: "FundClassificationChangeLogs",
                column: "NodeNavigationId",
                principalTable: "FundClassificationNodes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FundClassificationChangeLogs_Funds_FundNavigationId",
                table: "FundClassificationChangeLogs",
                column: "FundNavigationId",
                principalTable: "Funds",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_FundClassificationOverrides_FundClassificationNodes_FundClassificationNodeId",
                table: "FundClassificationOverrides",
                column: "FundClassificationNodeId",
                principalTable: "FundClassificationNodes",
                principalColumn: "Id");
        }
    }
}
