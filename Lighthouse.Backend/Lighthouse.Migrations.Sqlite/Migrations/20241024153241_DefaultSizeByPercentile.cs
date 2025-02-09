using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class DefaultSizeByPercentile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultWokrItemPercentile",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "HistoricalFeaturesWorkItemQuery",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "UsePercentileToCalculateDefaultAmountOfWorkItems",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultWokrItemPercentile",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "HistoricalFeaturesWorkItemQuery",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "UsePercentileToCalculateDefaultAmountOfWorkItems",
                table: "Projects");
        }
    }
}
