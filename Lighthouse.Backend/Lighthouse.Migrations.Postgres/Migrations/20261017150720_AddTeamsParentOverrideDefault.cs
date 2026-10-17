using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsParentOverrideDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentOverrideForTeams",
                table: "WorkTrackingSystemConnections",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentOverrideSource",
                table: "Teams",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // A Team naming a field its own connection does not have reads nothing from it, so the value
            // is cleared and the Team follows. Every Team still naming a field owns it.
            migrationBuilder.Sql(
                """
                UPDATE "Teams" SET "ParentOverrideAdditionalFieldDefinitionId" = NULL
                WHERE "ParentOverrideAdditionalFieldDefinitionId" IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM "AdditionalFieldDefinition" AS field
                    WHERE field."Id" = "Teams"."ParentOverrideAdditionalFieldDefinitionId"
                      AND field."WorkTrackingSystemConnectionId" = "Teams"."WorkTrackingSystemConnectionId");
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Teams" SET "ParentOverrideSource" = 1
                WHERE "ParentOverrideAdditionalFieldDefinitionId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentOverrideForTeams",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "ParentOverrideSource",
                table: "Teams");
        }
    }
}
