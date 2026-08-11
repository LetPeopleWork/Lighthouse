using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddFetchFingerprintToQueryOwners : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FetchFingerprint",
                table: "Teams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FetchFingerprint",
                table: "Portfolios",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FetchFingerprint",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "FetchFingerprint",
                table: "Portfolios");
        }
    }
}
