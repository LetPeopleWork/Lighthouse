using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToFeatureWork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove duplicate FeatureWork rows before adding unique constraint.
            // Keeps the row with the lowest Id for each (FeatureId, TeamId) pair.
            migrationBuilder.Sql(
                """
                DELETE FROM "FeatureWork"
                WHERE "Id" NOT IN (
                    SELECT MIN("Id")
                    FROM "FeatureWork"
                    GROUP BY "FeatureId", "TeamId"
                );
                """);

            migrationBuilder.DropIndex(
                name: "IX_FeatureWork_FeatureId",
                table: "FeatureWork");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureWork_FeatureId_TeamId",
                table: "FeatureWork",
                columns: new[] { "FeatureId", "TeamId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeatureWork_FeatureId_TeamId",
                table: "FeatureWork");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureWork_FeatureId",
                table: "FeatureWork",
                column: "FeatureId");
        }
    }
}
