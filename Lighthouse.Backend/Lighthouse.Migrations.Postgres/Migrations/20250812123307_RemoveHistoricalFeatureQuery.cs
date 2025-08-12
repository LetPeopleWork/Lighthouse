using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHistoricalFeatureQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HistoricalFeaturesWorkItemQuery",
                table: "Projects");

            migrationBuilder.AddColumn<int>(
                name: "PercentileHistoryInDays",
                table: "Projects",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PercentileHistoryInDays",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "HistoricalFeaturesWorkItemQuery",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
