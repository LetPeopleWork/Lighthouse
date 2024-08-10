using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class StoreMultipleForecastsPerFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ForecastBase_FeatureId",
                table: "ForecastBase");

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "ForecastBase",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForecastBase_FeatureId",
                table: "ForecastBase",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastBase_TeamId",
                table: "ForecastBase",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_ForecastBase_Teams_TeamId",
                table: "ForecastBase",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForecastBase_Teams_TeamId",
                table: "ForecastBase");

            migrationBuilder.DropIndex(
                name: "IX_ForecastBase_FeatureId",
                table: "ForecastBase");

            migrationBuilder.DropIndex(
                name: "IX_ForecastBase_TeamId",
                table: "ForecastBase");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "ForecastBase");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastBase_FeatureId",
                table: "ForecastBase",
                column: "FeatureId",
                unique: true);
        }
    }
}
