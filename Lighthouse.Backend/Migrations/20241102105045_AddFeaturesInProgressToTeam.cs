using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFeaturesInProgressToTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualFeatureWIP",
                table: "Teams");

            migrationBuilder.AddColumn<string>(
                name: "FeaturesInProgress",
                table: "Teams",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeaturesInProgress",
                table: "Teams");

            migrationBuilder.AddColumn<int>(
                name: "ActualFeatureWIP",
                table: "Teams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
