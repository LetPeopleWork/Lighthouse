using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveThroughputFeatureWIPFromTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeaturesInProgress",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "RawThroughput",
                table: "Teams");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TeamUpdateTime",
                table: "Teams",
                nullable: false,
                defaultValue: DateTime.MinValue,
                oldClrType: typeof(DateTime));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeaturesInProgress",
                table: "Teams",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RawThroughput",
                table: "Teams",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
