using System;
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
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    SearchBy = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkItemTypes = table.Column<string>(type: "TEXT", nullable: false),
                    SearchTerm = table.Column<string>(type: "TEXT", nullable: false),
                    TargetDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IncludeUnparentedItems = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAmountOfWorkItemsPerFeature = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    WorkTrackingSystem = table.Column<int>(type: "INTEGER", nullable: false),
                    AreaPaths = table.Column<string>(type: "TEXT", nullable: false),
                    WorkItemTypes = table.Column<string>(type: "TEXT", nullable: false),
                    IgnoredTags = table.Column<string>(type: "TEXT", nullable: false),
                    AdditionalRelatedFields = table.Column<string>(type: "TEXT", nullable: false),
                    FeatureWIP = table.Column<int>(type: "INTEGER", nullable: false),
                    ThroughputUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RawThroughput = table.Column<string>(type: "TEXT", nullable: false),
                    ThroughputHistory = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReferenceId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    IsUnparentedFeature = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Features_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamInProject",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamInProject", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamInProject_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamInProject_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkTrackingSystemOption",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Secret = table.Column<bool>(type: "INTEGER", nullable: false),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTrackingSystemOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkTrackingSystemOption_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RemainingWork",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    RemainingWorkItems = table.Column<int>(type: "INTEGER", nullable: false),
                    FeatureId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemainingWork", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemainingWork_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RemainingWork_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WhenForecast",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FeatureId = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalTrials = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WhenForecast", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WhenForecast_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndividualSimulationResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false),
                    WhenForecastId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndividualSimulationResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndividualSimulationResult_WhenForecast_WhenForecastId",
                        column: x => x.WhenForecastId,
                        principalTable: "WhenForecast",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Features_ProjectId",
                table: "Features",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_IndividualSimulationResult_WhenForecastId",
                table: "IndividualSimulationResult",
                column: "WhenForecastId");

            migrationBuilder.CreateIndex(
                name: "IX_RemainingWork_FeatureId",
                table: "RemainingWork",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_RemainingWork_TeamId",
                table: "RemainingWork",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInProject_ProjectId",
                table: "TeamInProject",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamInProject_TeamId",
                table: "TeamInProject",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_WhenForecast_FeatureId",
                table: "WhenForecast",
                column: "FeatureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkTrackingSystemOption_TeamId",
                table: "WorkTrackingSystemOption",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndividualSimulationResult");

            migrationBuilder.DropTable(
                name: "RemainingWork");

            migrationBuilder.DropTable(
                name: "TeamInProject");

            migrationBuilder.DropTable(
                name: "WorkTrackingSystemOption");

            migrationBuilder.DropTable(
                name: "WhenForecast");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
