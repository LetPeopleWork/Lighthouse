using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddActualFeatureWIP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ThroughputUpdateTime",
                table: "Teams",
                newName: "TeamUpdateTime");

            migrationBuilder.AddColumn<int>(
                name: "ActualFeatureWIP",
                table: "Teams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActualFeatureWIP",
                table: "Teams");

            migrationBuilder.RenameColumn(
                name: "TeamUpdateTime",
                table: "Teams",
                newName: "ThroughputUpdateTime");
        }
    }
}
