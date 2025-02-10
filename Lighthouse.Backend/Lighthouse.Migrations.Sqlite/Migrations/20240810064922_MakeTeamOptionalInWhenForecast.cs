using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class MakeTeamOptionalInWhenForecast : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForecastBase_Teams_TeamId",
                table: "ForecastBase");

            migrationBuilder.AddForeignKey(
                name: "FK_ForecastBase_Teams_TeamId",
                table: "ForecastBase",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForecastBase_Teams_TeamId",
                table: "ForecastBase");

            migrationBuilder.AddForeignKey(
                name: "FK_ForecastBase_Teams_TeamId",
                table: "ForecastBase",
                column: "TeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
