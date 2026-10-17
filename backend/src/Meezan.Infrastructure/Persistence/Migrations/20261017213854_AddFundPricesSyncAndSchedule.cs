using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Meezan.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFundPricesSyncAndSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "FullSyncRunTimeCairo",
                table: "FundSyncSettings",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(2, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "FullSyncScheduleEnabled",
                table: "FundSyncSettings",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "FundSyncLogs",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Full");

            migrationBuilder.AddColumn<DateTime>(
                name: "PricesUpdatedAt",
                table: "Funds",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FullSyncRunTimeCairo",
                table: "FundSyncSettings");

            migrationBuilder.DropColumn(
                name: "FullSyncScheduleEnabled",
                table: "FundSyncSettings");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "FundSyncLogs");

            migrationBuilder.DropColumn(
                name: "PricesUpdatedAt",
                table: "Funds");
        }
    }
}
