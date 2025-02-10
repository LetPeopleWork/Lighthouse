using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexToFeatureHistoryEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FeatureHistory_FeatureReferenceId_Snapshot",
                table: "FeatureHistory",
                columns: new[] { "FeatureReferenceId", "Snapshot" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeatureHistory_FeatureReferenceId_Snapshot",
                table: "FeatureHistory");
        }
    }
}
