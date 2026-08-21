using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioDependencySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DependencyOverrideAdditionalFieldDefinitionId",
                table: "Portfolios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IgnoreDependencies",
                table: "Portfolios",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DependencyOverrideAdditionalFieldDefinitionId",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "IgnoreDependencies",
                table: "Portfolios");
        }
    }
}
