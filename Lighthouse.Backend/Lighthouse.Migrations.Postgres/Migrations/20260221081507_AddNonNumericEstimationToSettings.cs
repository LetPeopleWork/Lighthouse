using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddNonNumericEstimationToSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "EstimationCategoryValues",
                table: "Teams",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>());

            migrationBuilder.AddColumn<bool>(
                name: "UseNonNumericEstimation",
                table: "Teams",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "EstimationCategoryValues",
                table: "Portfolios",
                type: "text[]",
                nullable: false,
                defaultValue: new List<string>());

            migrationBuilder.AddColumn<bool>(
                name: "UseNonNumericEstimation",
                table: "Portfolios",
                type: "boolean",
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
