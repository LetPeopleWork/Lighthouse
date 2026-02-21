using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddEstimationFieldToTeamAndPortfolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstimationAdditionalFieldDefinitionId",
                table: "Teams",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstimationAdditionalFieldDefinitionId",
                table: "Portfolios",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimationAdditionalFieldDefinitionId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "EstimationAdditionalFieldDefinitionId",
                table: "Portfolios");
        }
    }
}
