using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
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
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FetchFingerprint",
                table: "Portfolios",
                type: "TEXT",
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
