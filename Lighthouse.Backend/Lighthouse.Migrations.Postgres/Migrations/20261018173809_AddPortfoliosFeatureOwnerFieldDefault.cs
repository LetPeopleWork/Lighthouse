using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfoliosFeatureOwnerFieldDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FeatureOwnerFieldForPortfolios",
                table: "WorkTrackingSystemConnections",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeatureOwnerFieldSource",
                table: "Portfolios",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // A Portfolio naming a field its own connection does not have reads no owner from it, so the value is
            // cleared and the Portfolio follows. Every Portfolio still naming one owns it.
            migrationBuilder.Sql(
                """
                UPDATE "Portfolios" SET "FeatureOwnerAdditionalFieldDefinitionId" = NULL
                WHERE "FeatureOwnerAdditionalFieldDefinitionId" IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM "AdditionalFieldDefinition" AS field
                    WHERE field."Id" = "Portfolios"."FeatureOwnerAdditionalFieldDefinitionId"
                      AND field."WorkTrackingSystemConnectionId" = "Portfolios"."WorkTrackingSystemConnectionId");
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Portfolios" SET "FeatureOwnerFieldSource" = 1
                WHERE "FeatureOwnerAdditionalFieldDefinitionId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeatureOwnerFieldForPortfolios",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "FeatureOwnerFieldSource",
                table: "Portfolios");
        }
    }
}
