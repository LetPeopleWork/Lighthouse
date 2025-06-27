using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddParentOverrideFieldToProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AdditionalRelatedField",
                table: "Teams",
                newName: "ParentOverrideField");

            migrationBuilder.AddColumn<string>(
                name: "ParentOverrideField",
                table: "Projects",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentOverrideField",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "ParentOverrideField",
                table: "Teams",
                newName: "AdditionalRelatedField");
        }
    }
}
