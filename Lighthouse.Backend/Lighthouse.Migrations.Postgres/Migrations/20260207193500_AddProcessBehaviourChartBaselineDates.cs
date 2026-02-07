using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessBehaviourChartBaselineDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessBehaviourChartBaselineEndDate",
                table: "Teams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessBehaviourChartBaselineStartDate",
                table: "Teams",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessBehaviourChartBaselineEndDate",
                table: "Portfolios",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessBehaviourChartBaselineStartDate",
                table: "Portfolios",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessBehaviourChartBaselineEndDate",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ProcessBehaviourChartBaselineStartDate",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ProcessBehaviourChartBaselineEndDate",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "ProcessBehaviourChartBaselineStartDate",
                table: "Portfolios");
        }
    }
}
