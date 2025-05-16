using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddSLEToProjectTeams : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServiceLevelExpectationProbability",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ServiceLevelExpectationRange",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ServiceLevelExpectationProbability",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ServiceLevelExpectationRange",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceLevelExpectationProbability",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ServiceLevelExpectationRange",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ServiceLevelExpectationProbability",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ServiceLevelExpectationRange",
                table: "Projects");
        }
    }
}
