using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfoliosSizeEstimateFieldDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SizeEstimateFieldForPortfolios",
                table: "WorkTrackingSystemConnections",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SizeEstimateFieldSource",
                table: "Portfolios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // A Portfolio naming a field its own connection does not have reads no size estimate from it, so the
            // value is cleared and the Portfolio follows. Every Portfolio still naming one owns it.
            migrationBuilder.Sql(
                """
                UPDATE "Portfolios" SET "SizeEstimateAdditionalFieldDefinitionId" = NULL
                WHERE "SizeEstimateAdditionalFieldDefinitionId" IS NOT NULL
                  AND NOT EXISTS (
                    SELECT 1 FROM "AdditionalFieldDefinition" AS field
                    WHERE field."Id" = "Portfolios"."SizeEstimateAdditionalFieldDefinitionId"
                      AND field."WorkTrackingSystemConnectionId" = "Portfolios"."WorkTrackingSystemConnectionId");
                """);

            migrationBuilder.Sql(
                """
                UPDATE "Portfolios" SET "SizeEstimateFieldSource" = 1
                WHERE "SizeEstimateAdditionalFieldDefinitionId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SizeEstimateFieldForPortfolios",
                table: "WorkTrackingSystemConnections");

            migrationBuilder.DropColumn(
                name: "SizeEstimateFieldSource",
                table: "Portfolios");
        }
    }
}
