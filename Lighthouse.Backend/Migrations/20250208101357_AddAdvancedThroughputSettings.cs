using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAdvancedThroughputSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ThroughputHistoryEndDate",
                table: "Teams",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ThroughputHistoryStartDate",
                table: "Teams",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseFixedDatesForThroughput",
                table: "Teams",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ThroughputHistoryEndDate",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ThroughputHistoryStartDate",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "UseFixedDatesForThroughput",
                table: "Teams");
        }
    }
}
