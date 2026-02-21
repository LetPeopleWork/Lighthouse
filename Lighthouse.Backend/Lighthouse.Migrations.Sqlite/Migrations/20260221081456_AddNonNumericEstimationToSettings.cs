using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddNonNumericEstimationToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstimationCategoryValues",
                table: "Teams",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<bool>(
                name: "UseNonNumericEstimation",
                table: "Teams",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EstimationCategoryValues",
                table: "Portfolios",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<bool>(
                name: "UseNonNumericEstimation",
                table: "Portfolios",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimationCategoryValues",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "UseNonNumericEstimation",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "EstimationCategoryValues",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "UseNonNumericEstimation",
                table: "Portfolios");
        }
    }
}
