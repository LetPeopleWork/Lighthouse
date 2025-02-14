using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOldWorkTrackingConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM [Projects]");
            migrationBuilder.Sql("DELETE FROM [Teams]");

            migrationBuilder.DropTable(
                name: "WorkTrackingSystemOption<Project>");

            migrationBuilder.DropTable(
                name: "WorkTrackingSystemOption<Team>");

            migrationBuilder.RenameColumn(
                name: "WorkTrackingSystem",
                table: "Teams",
                newName: "WorkTrackingSystemConnectionId");

            migrationBuilder.RenameColumn(
                name: "WorkTrackingSystem",
                table: "Projects",
                newName: "WorkTrackingSystemConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_WorkTrackingSystemConnectionId",
                table: "Teams",
                column: "WorkTrackingSystemConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_WorkTrackingSystemConnectionId",
                table: "Projects",
                column: "WorkTrackingSystemConnectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Projects_WorkTrackingSystemConnections_WorkTrackingSystemConnectionId",
                table: "Projects",
                column: "WorkTrackingSystemConnectionId",
                principalTable: "WorkTrackingSystemConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Teams_WorkTrackingSystemConnections_WorkTrackingSystemConnectionId",
                table: "Teams",
                column: "WorkTrackingSystemConnectionId",
                principalTable: "WorkTrackingSystemConnections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Projects_WorkTrackingSystemConnections_WorkTrackingSystemConnectionId",
                table: "Projects");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_WorkTrackingSystemConnections_WorkTrackingSystemConnectionId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_WorkTrackingSystemConnectionId",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Projects_WorkTrackingSystemConnectionId",
                table: "Projects");

            migrationBuilder.RenameColumn(
                name: "WorkTrackingSystemConnectionId",
                table: "Teams",
                newName: "WorkTrackingSystem");

            migrationBuilder.RenameColumn(
                name: "WorkTrackingSystemConnectionId",
                table: "Projects",
                newName: "WorkTrackingSystem");

            migrationBuilder.CreateTable(
                name: "WorkTrackingSystemOption<Project>",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Secret = table.Column<bool>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTrackingSystemOption<Project>", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkTrackingSystemOption<Project>_Projects_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkTrackingSystemOption<Team>",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Secret = table.Column<bool>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTrackingSystemOption<Team>", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkTrackingSystemOption<Team>_Teams_EntityId",
                        column: x => x.EntityId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTrackingSystemOption<Project>_EntityId",
                table: "WorkTrackingSystemOption<Project>",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTrackingSystemOption<Team>_EntityId",
                table: "WorkTrackingSystemOption<Team>",
                column: "EntityId");
        }
    }
}