using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class WorkTrackingSystemConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkTrackingSystemConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    WorkTrackingSystem = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTrackingSystemConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkTrackingSystemConnectionOption",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    IsSecret = table.Column<bool>(type: "INTEGER", nullable: false),
                    WorkTrackingSystemConnectionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTrackingSystemConnectionOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkTrackingSystemConnectionOption_WorkTrackingSystemConnections_WorkTrackingSystemConnectionId",
                        column: x => x.WorkTrackingSystemConnectionId,
                        principalTable: "WorkTrackingSystemConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTrackingSystemConnectionOption_WorkTrackingSystemConnectionId",
                table: "WorkTrackingSystemConnectionOption",
                column: "WorkTrackingSystemConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkTrackingSystemConnectionOption");

            migrationBuilder.DropTable(
                name: "WorkTrackingSystemConnections");
        }
    }
}
