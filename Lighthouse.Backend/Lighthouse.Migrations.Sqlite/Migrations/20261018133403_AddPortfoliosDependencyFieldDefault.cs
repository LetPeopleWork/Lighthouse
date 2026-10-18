using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfoliosDependencyFieldDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DependencyFieldForPortfolios",
                table: "WorkTrackingSystemConnections",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DependencyFieldSource",
                table: "Portfolios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // A Portfolio naming a field or link type its own connection does not have reads nothing from it, so
            // the value is cleared and the Portfolio follows. Every Portfolio still naming one owns it.
            migrationBuilder.Sql(
                """
                UPDATE "Portfolios" SET "DependencyOverrideAdditionalFieldDefinitionId" = NULL
                WHERE "DependencyOverrideAdditionalFieldDefinitionId" IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM "AdditionalFieldDefinition" AS field
                    WHERE field."Id" = "Portfolios"."DependencyOverrideAdditionalFieldDefinitionId"
                      AND field."WorkTrackingSystemConnectionId" = "Portfolios"."WorkTrackingSystemConnectionId");
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Portfolios" SET "DependencyFieldSource" = 1
                WHERE "DependencyOverrideAdditionalFieldDefinitionId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DependencyFieldForPortfolios",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "DependencyFieldSource",
                table: "Portfolios");
        }
    }
}
