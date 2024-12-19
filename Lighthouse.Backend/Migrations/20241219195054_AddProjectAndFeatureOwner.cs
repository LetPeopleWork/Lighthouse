using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectAndFeatureOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeatureOwnerField",
                table: "Projects",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OwningTeamId",
                table: "Projects",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwningTeamId",
                table: "Projects",
                column: "OwningTeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_Teams_OwningTeamId",
                table: "Projects",
                column: "OwningTeamId",
                principalTable: "Teams",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_Teams_OwningTeamId",
                table: "Projects");

            migrationBuilder.DropIndex(
                name: "IX_Projects_OwningTeamId",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "FeatureOwnerField",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "OwningTeamId",
                table: "Projects");
        }
    }
}
