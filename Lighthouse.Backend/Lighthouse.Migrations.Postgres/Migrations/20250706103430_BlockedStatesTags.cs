using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class BlockedStatesTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "BlockedStates",
                table: "Teams",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "BlockedTags",
                table: "Teams",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "BlockedStates",
                table: "Projects",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "BlockedTags",
                table: "Projects",
                type: "text[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockedStates",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "BlockedTags",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "BlockedStates",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "BlockedTags",
                table: "Projects");
        }
    }
}
