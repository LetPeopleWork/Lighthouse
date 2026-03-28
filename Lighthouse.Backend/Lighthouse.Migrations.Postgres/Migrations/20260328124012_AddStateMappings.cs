using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddStateMappings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StateMappings",
                table: "Teams",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StateMappings",
                table: "Portfolios",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.DropForeignKey(
                name: "FK_FeatureProject_Projects_ProjectsId",
                table: "FeaturePortfolio");

            migrationBuilder.AddForeignKey(
                name: "FK_FeaturePortfolio_Portfolios_PortfoliosId",
                table: "FeaturePortfolio",
                column: "PortfoliosId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FeaturePortfolio_Portfolios_PortfoliosId",
                table: "FeaturePortfolio");

            migrationBuilder.AddForeignKey(
                name: "FK_FeatureProject_Projects_ProjectsId",
                table: "FeaturePortfolio",
                column: "PortfoliosId",
                principalTable: "Portfolios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.DropColumn(
                name: "StateMappings",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "StateMappings",
                table: "Portfolios");
        }
    }
}
