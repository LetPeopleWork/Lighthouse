using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddOAuthCredentialUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OAuthCredentials_WorkTrackingSystemConnectionId",
                table: "OAuthCredentials");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthCredentials_WorkTrackingSystemConnectionId",
                table: "OAuthCredentials",
                column: "WorkTrackingSystemConnectionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OAuthCredentials_WorkTrackingSystemConnectionId",
                table: "OAuthCredentials");

            migrationBuilder.CreateIndex(
                name: "IX_OAuthCredentials_WorkTrackingSystemConnectionId",
                table: "OAuthCredentials",
                column: "WorkTrackingSystemConnectionId");
        }
    }
}
