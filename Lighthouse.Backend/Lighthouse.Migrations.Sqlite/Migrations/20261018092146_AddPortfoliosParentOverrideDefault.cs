using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfoliosParentOverrideDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentOverrideForPortfolios",
                table: "WorkTrackingSystemConnections",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentOverrideSource",
                table: "Portfolios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // A Portfolio naming a field its own connection does not have reads nothing from it, so the value
            // is cleared and the Portfolio follows. Every Portfolio still naming a field owns it.
            migrationBuilder.Sql(
                """
                UPDATE "Portfolios" SET "ParentOverrideAdditionalFieldDefinitionId" = NULL
                WHERE "ParentOverrideAdditionalFieldDefinitionId" IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM "AdditionalFieldDefinition" AS field
                    WHERE field."Id" = "Portfolios"."ParentOverrideAdditionalFieldDefinitionId"
                      AND field."WorkTrackingSystemConnectionId" = "Portfolios"."WorkTrackingSystemConnectionId");
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Portfolios" SET "ParentOverrideSource" = 1
                WHERE "ParentOverrideAdditionalFieldDefinitionId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentOverrideForPortfolios",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "ParentOverrideSource",
                table: "Portfolios");
        }
    }
}
