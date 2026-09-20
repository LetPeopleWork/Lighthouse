using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddStartForecasts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StartFeatureId",
                table: "ForecastBase",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartTeamId",
                table: "ForecastBase",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForecastBase_StartFeatureId",
                table: "ForecastBase",
                column: "StartFeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastBase_StartTeamId",
                table: "ForecastBase",
                column: "StartTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_ForecastBase_Features_StartFeatureId",
                table: "ForecastBase",
                column: "StartFeatureId",
                principalTable: "Features",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ForecastBase_Teams_StartTeamId",
                table: "ForecastBase",
                column: "StartTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForecastBase_Features_StartFeatureId",
                table: "ForecastBase");

            migrationBuilder.DropForeignKey(
                name: "FK_ForecastBase_Teams_StartTeamId",
                table: "ForecastBase");

            migrationBuilder.DropIndex(
                name: "IX_ForecastBase_StartFeatureId",
                table: "ForecastBase");

            migrationBuilder.DropIndex(
                name: "IX_ForecastBase_StartTeamId",
                table: "ForecastBase");

            migrationBuilder.DropColumn(
                name: "StartFeatureId",
                table: "ForecastBase");

            migrationBuilder.DropColumn(
                name: "StartTeamId",
                table: "ForecastBase");
        }
    }
}
