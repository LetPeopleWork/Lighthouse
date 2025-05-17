using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class HarmonizeLastUpdatedNameForTeamsProject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TeamUpdateTime",
                table: "Teams",
                newName: "UpdateTime");

            migrationBuilder.RenameColumn(
                name: "ProjectUpdateTime",
                table: "Projects",
                newName: "UpdateTime");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdateTime",
                table: "Teams",
                newName: "TeamUpdateTime");

            migrationBuilder.RenameColumn(
                name: "UpdateTime",
                table: "Projects",
                newName: "ProjectUpdateTime");
        }
    }
}
