using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class MultipleProjectsPerFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Features_Projects_ProjectId",
                table: "Features");

            migrationBuilder.DropIndex(
                name: "IX_Features_ProjectId",
                table: "Features");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Features");

            migrationBuilder.CreateTable(
                name: "FeatureProject",
                columns: table => new
                {
                    FeaturesId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureProject", x => new { x.FeaturesId, x.ProjectsId });
                    table.ForeignKey(
                        name: "FK_FeatureProject_Features_FeaturesId",
                        column: x => x.FeaturesId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeatureProject_Projects_ProjectsId",
                        column: x => x.ProjectsId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureProject_ProjectsId",
                table: "FeatureProject",
                column: "ProjectsId");

            migrationBuilder.Sql("DELETE FROM [Features]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureProject");

            migrationBuilder.AddColumn<int>(
                name: "ProjectId",
                table: "Features",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Features_ProjectId",
                table: "Features",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_Features_Projects_ProjectId",
                table: "Features",
                column: "ProjectId",
                principalTable: "Projects",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
