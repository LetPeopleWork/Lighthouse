using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsProjectConnection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProjectTeam",
                columns: table => new
                {
                    ProjectsId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectTeam", x => new { x.ProjectsId, x.TeamsId });
                    table.ForeignKey(
                        name: "FK_ProjectTeam_Projects_ProjectsId",
                        column: x => x.ProjectsId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProjectTeam_Teams_TeamsId",
                        column: x => x.TeamsId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeam_TeamsId",
                table: "ProjectTeam",
                column: "TeamsId");

            migrationBuilder.Sql(@"
                INSERT INTO ProjectTeam (ProjectsId, TeamsId)
                SELECT p.Id, t.Id
                FROM Projects p
                JOIN FeatureProject fp ON fp.ProjectsId = p.Id
                JOIN Features f ON f.Id = fp.FeaturesId
                JOIN FeatureWork fw ON fw.FeatureId = f.Id
                LEFT JOIN Teams t ON fw.TeamId = t.Id
                WHERE t.Id IS NOT NULL
                GROUP BY p.Id, t.Id
            ");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProjectTeam");
        }
    }
}
