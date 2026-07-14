using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
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
            // The original AddColumn (20250706103430_BlockedStatesTags) specified no default, which only
            // worked because it ran against an empty install. This Down() can run against a table with
            // real rows (a genuine rollback), so it needs defaultValueSql — otherwise Postgres rejects a
            // NOT NULL ADD COLUMN on a non-empty table outright (columns come back empty either way; data
            // loss on rollback is expected/acceptable per the expand-then-contract migration strategy).
            migrationBuilder.AddColumn<List<string>>(
                name: "BlockedStates",
                table: "Teams",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<List<string>>(
                name: "BlockedTags",
                table: "Teams",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<List<string>>(
                name: "BlockedStates",
                table: "Portfolios",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");

            migrationBuilder.AddColumn<List<string>>(
                name: "BlockedTags",
                table: "Portfolios",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'");
        }
    }
}
