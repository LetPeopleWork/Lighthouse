using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyBlockedConfiguration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockedStates",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "BlockedTags",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "BlockedStates",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "BlockedTags",
                table: "Portfolios");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockedStates",
                table: "Teams",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "BlockedTags",
                table: "Teams",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "BlockedStates",
                table: "Portfolios",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "BlockedTags",
                table: "Portfolios",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");
        }
    }
}
