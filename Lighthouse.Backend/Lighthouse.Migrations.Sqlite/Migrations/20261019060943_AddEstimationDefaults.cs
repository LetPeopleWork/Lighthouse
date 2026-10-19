using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimationDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EstimationCategoryValuesForPortfolios",
                table: "WorkTrackingSystemConnections",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "EstimationCategoryValuesForTeams",
                table: "WorkTrackingSystemConnections",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "EstimationFieldForPortfolios",
                table: "WorkTrackingSystemConnections",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimationFieldForTeams",
                table: "WorkTrackingSystemConnections",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimationUnitForPortfolios",
                table: "WorkTrackingSystemConnections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EstimationUnitForTeams",
                table: "WorkTrackingSystemConnections",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UseNonNumericEstimationForPortfolios",
                table: "WorkTrackingSystemConnections",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "UseNonNumericEstimationForTeams",
                table: "WorkTrackingSystemConnections",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EstimationSource",
                table: "Teams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EstimationSource",
                table: "Portfolios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // An Estimation Field naming nothing the owner's own connection has (a deleted field, or a field of another
            // connection) estimates nothing, so the whole bundle is emptied and the owner follows. Every owner still
            // naming a field owns its bundle.
            migrationBuilder.Sql(
                """
                UPDATE "Teams" SET "EstimationAdditionalFieldDefinitionId" = NULL, "EstimationUnit" = NULL,
                    "UseNonNumericEstimation" = 0, "EstimationCategoryValues" = '[]'
                WHERE "EstimationAdditionalFieldDefinitionId" IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM "AdditionalFieldDefinition" AS field
                    WHERE field."Id" = "Teams"."EstimationAdditionalFieldDefinitionId"
                      AND field."WorkTrackingSystemConnectionId" = "Teams"."WorkTrackingSystemConnectionId");
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Portfolios" SET "EstimationAdditionalFieldDefinitionId" = NULL, "EstimationUnit" = NULL,
                    "UseNonNumericEstimation" = 0, "EstimationCategoryValues" = '[]'
                WHERE "EstimationAdditionalFieldDefinitionId" IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM "AdditionalFieldDefinition" AS field
                    WHERE field."Id" = "Portfolios"."EstimationAdditionalFieldDefinitionId"
                      AND field."WorkTrackingSystemConnectionId" = "Portfolios"."WorkTrackingSystemConnectionId");
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Teams" SET "EstimationSource" = 1
                WHERE "EstimationAdditionalFieldDefinitionId" IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Portfolios" SET "EstimationSource" = 1
                WHERE "EstimationAdditionalFieldDefinitionId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimationCategoryValuesForPortfolios",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "EstimationCategoryValuesForTeams",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "EstimationFieldForPortfolios",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "EstimationFieldForTeams",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "EstimationUnitForPortfolios",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "EstimationUnitForTeams",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "UseNonNumericEstimationForPortfolios",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "UseNonNumericEstimationForTeams",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "EstimationSource",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "EstimationSource",
                table: "Portfolios");
        }
    }
}
