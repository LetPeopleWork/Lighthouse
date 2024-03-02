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
                    IncludeUnparentedItems = table.Column<bool>(type: "INTEGER", nullable: false),
                    DefaultAmountOfWorkItemsPerFeature = table.Column<int>(type: "INTEGER", nullable: false),
                    ProjectUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    WorkTrackingSystem = table.Column<int>(type: "INTEGER", nullable: false)
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
                    WorkItemTypes = table.Column<string>(type: "TEXT", nullable: false),
                    IgnoredTags = table.Column<string>(type: "TEXT", nullable: false),
                    AdditionalRelatedFields = table.Column<string>(type: "TEXT", nullable: false),
                    FeatureWIP = table.Column<int>(type: "INTEGER", nullable: false),
                    ThroughputUpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RawThroughput = table.Column<string>(type: "TEXT", nullable: false),
                    ThroughputHistory = table.Column<int>(type: "INTEGER", nullable: false),
                    WorkTrackingSystem = table.Column<int>(type: "INTEGER", nullable: false)
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
                name: "Milestone",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ProjectId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestone", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Milestone_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkTrackingSystemOption<Project>",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Secret = table.Column<bool>(type: "INTEGER", nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false)
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
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false),
                    Secret = table.Column<bool>(type: "INTEGER", nullable: false),
                    EntityId = table.Column<int>(type: "INTEGER", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ForecastBase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TotalTrials = table.Column<int>(type: "INTEGER", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Discriminator = table.Column<string>(type: "TEXT", maxLength: 13, nullable: false),
                    FeatureId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastBase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastBase_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
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
                name: "IndividualSimulationResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Key = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<int>(type: "INTEGER", nullable: false),
                    ForecastId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndividualSimulationResult", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndividualSimulationResult_ForecastBase_ForecastId",
                        column: x => x.ForecastId,
                        principalTable: "ForecastBase",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Features_ProjectId",
                table: "Features",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastBase_FeatureId",
                table: "ForecastBase",
                column: "FeatureId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IndividualSimulationResult_ForecastId",
                table: "IndividualSimulationResult",
                column: "ForecastId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestone_ProjectId",
                table: "Milestone",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RemainingWork_FeatureId",
                table: "RemainingWork",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_RemainingWork_TeamId",
                table: "RemainingWork",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTrackingSystemOption<Project>_EntityId",
                table: "WorkTrackingSystemOption<Project>",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTrackingSystemOption<Team>_EntityId",
                table: "WorkTrackingSystemOption<Team>",
                column: "EntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IndividualSimulationResult");

            migrationBuilder.DropTable(
                name: "Milestone");

            migrationBuilder.DropTable(
                name: "RemainingWork");

            migrationBuilder.DropTable(
                name: "WorkTrackingSystemOption<Project>");

            migrationBuilder.DropTable(
                name: "WorkTrackingSystemOption<Team>");

            migrationBuilder.DropTable(
                name: "ForecastBase");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
