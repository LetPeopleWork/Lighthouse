using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddTagsToTeamsAndProjects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "Teams",
                type: "text[]",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "Projects",
                type: "text[]",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Projects");
        }
    }
}
