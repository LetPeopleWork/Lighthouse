using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMFTAspNet.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Team",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ProjectName = table.Column<string>(type: "TEXT", nullable: false),
                    WorkTrackingSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    AreaPaths = table.Column<string>(type: "TEXT", nullable: false),
                    WorkItemTypes = table.Column<string>(type: "TEXT", nullable: false),
                    IgnoredTags = table.Column<string>(type: "TEXT", nullable: false),
                    AdditionalRelatedFields = table.Column<string>(type: "TEXT", nullable: false),
                    FeatureWIP = table.Column<int>(type: "INTEGER", nullable: false),
                    RawThroughput = table.Column<string>(type: "TEXT", nullable: false),
                    ThroughputHistory = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Team", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkTrackingSystemOption",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTrackingSystemOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkTrackingSystemOption_Team_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Team",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkTrackingSystemOption_TeamId",
                table: "WorkTrackingSystemOption",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkTrackingSystemOption");

            migrationBuilder.DropTable(
                name: "Team");
        }
    }
}
