using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddParentSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentSourcesForPortfolios",
                table: "WorkTrackingSystemConnections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentSourcesForTeams",
                table: "WorkTrackingSystemConnections",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentSources",
                table: "Teams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentSources",
                table: "Portfolios",
                type: "text",
                nullable: true);

            // The text is what the serialiser writes for the list, byte for byte, so that a row from the upgrade
            // and a row saved later cannot differ. Followers and connections without a default stay NULL.
            migrationBuilder.Sql(
                """
                UPDATE "Teams" SET "ParentSources" =
                  '[{"Kind":1,"AdditionalFieldDefinitionId":' || CAST("ParentOverrideAdditionalFieldDefinitionId" AS TEXT) || '},{"Kind":0,"AdditionalFieldDefinitionId":null}]'
                WHERE "ParentOverrideSource" = 1 AND "ParentOverrideAdditionalFieldDefinitionId" IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Teams" SET "ParentSources" = '[{"Kind":0,"AdditionalFieldDefinitionId":null}]'
                WHERE "ParentOverrideSource" = 1 AND "ParentOverrideAdditionalFieldDefinitionId" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Portfolios" SET "ParentSources" =
                  '[{"Kind":1,"AdditionalFieldDefinitionId":' || CAST("ParentOverrideAdditionalFieldDefinitionId" AS TEXT) || '},{"Kind":0,"AdditionalFieldDefinitionId":null}]'
                WHERE "ParentOverrideSource" = 1 AND "ParentOverrideAdditionalFieldDefinitionId" IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Portfolios" SET "ParentSources" = '[{"Kind":0,"AdditionalFieldDefinitionId":null}]'
                WHERE "ParentOverrideSource" = 1 AND "ParentOverrideAdditionalFieldDefinitionId" IS NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "WorkTrackingSystemConnections" SET "ParentSourcesForTeams" =
                  '[{"Kind":1,"AdditionalFieldDefinitionId":' || CAST("ParentOverrideForTeams" AS TEXT) || '},{"Kind":0,"AdditionalFieldDefinitionId":null}]'
                WHERE "ParentOverrideForTeams" IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "WorkTrackingSystemConnections" SET "ParentSourcesForPortfolios" =
                  '[{"Kind":1,"AdditionalFieldDefinitionId":' || CAST("ParentOverrideForPortfolios" AS TEXT) || '},{"Kind":0,"AdditionalFieldDefinitionId":null}]'
                WHERE "ParentOverrideForPortfolios" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentSourcesForPortfolios",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "ParentSourcesForTeams",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "ParentSources",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ParentSources",
                table: "Portfolios");
        }
    }
}
