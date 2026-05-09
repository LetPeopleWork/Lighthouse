using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddApiKeyOwnershipLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerSubject",
                table: "ApiKeys",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwnerUserProfileId",
                table: "ApiKeys",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_OwnerSubject",
                table: "ApiKeys",
                column: "OwnerSubject");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_OwnerUserProfileId",
                table: "ApiKeys",
                column: "OwnerUserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_UserProfiles_OwnerUserProfileId",
                table: "ApiKeys",
                column: "OwnerUserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiKeys_UserProfiles_OwnerUserProfileId",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_OwnerSubject",
                table: "ApiKeys");

            migrationBuilder.DropIndex(
                name: "IX_ApiKeys_OwnerUserProfileId",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "OwnerSubject",
                table: "ApiKeys");

            migrationBuilder.DropColumn(
                name: "OwnerUserProfileId",
                table: "ApiKeys");
        }
    }
}
