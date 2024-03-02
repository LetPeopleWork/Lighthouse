using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMFTAspNet.Migrations
{
    /// <inheritdoc />
    public partial class TIPAddID : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TeamInProject",
                table: "TeamInProject");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "TeamInProject",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeamInProject",
                table: "TeamInProject",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInProject_TeamId",
                table: "TeamInProject",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TeamInProject",
                table: "TeamInProject");

            migrationBuilder.DropIndex(
                name: "IX_TeamInProject_TeamId",
                table: "TeamInProject");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "TeamInProject");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TeamInProject",
                table: "TeamInProject",
                columns: new[] { "TeamId", "ProjectId" });
        }
    }
}
