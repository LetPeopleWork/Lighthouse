using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "FeatureHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FeatureId = table.Column<int>(type: "integer", nullable: false),
                    FeatureReferenceId = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    Snapshot = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Features",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReferenceId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<string>(type: "text", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true),
                    IsUnparentedFeature = table.Column<bool>(type: "boolean", nullable: false),
                    IsUsingDefaultFeatureSize = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Features", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PreviewFeatures",
                columns: table => new
                {
                    Key = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreviewFeatures", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "WorkTrackingSystemConnections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    WorkTrackingSystem = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTrackingSystemConnections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureWorkHistoryEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    RemainingWorkItems = table.Column<int>(type: "integer", nullable: false),
                    TotalWorkItems = table.Column<int>(type: "integer", nullable: false),
                    FeatureHistoryEntryId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureWorkHistoryEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureWorkHistoryEntry_FeatureHistory_FeatureHistoryEntryId",
                        column: x => x.FeatureHistoryEntryId,
                        principalTable: "FeatureHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    WorkItemTypes = table.Column<List<string>>(type: "text[]", nullable: false),
                    FeatureWIP = table.Column<int>(type: "integer", nullable: false),
                    AutomaticallyAdjustFeatureWIP = table.Column<bool>(type: "boolean", nullable: false),
                    FeaturesInProgress = table.Column<List<string>>(type: "text[]", nullable: false),
                    AdditionalRelatedField = table.Column<string>(type: "text", nullable: true),
                    TeamUpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RawThroughput = table.Column<int[]>(type: "integer[]", nullable: false),
                    UseFixedDatesForThroughput = table.Column<bool>(type: "boolean", nullable: false),
                    ThroughputHistoryStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ThroughputHistoryEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ThroughputHistory = table.Column<int>(type: "integer", nullable: false),
                    WorkItemQuery = table.Column<string>(type: "text", nullable: false),
                    ToDoStates = table.Column<List<string>>(type: "text[]", nullable: false),
                    DoingStates = table.Column<List<string>>(type: "text[]", nullable: false),
                    DoneStates = table.Column<List<string>>(type: "text[]", nullable: false),
                    WorkTrackingSystemConnectionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_WorkTrackingSystemConnections_WorkTrackingSystemConne~",
                        column: x => x.WorkTrackingSystemConnectionId,
                        principalTable: "WorkTrackingSystemConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkTrackingSystemConnectionOption",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    IsSecret = table.Column<bool>(type: "boolean", nullable: false),
                    IsOptional = table.Column<bool>(type: "boolean", nullable: false),
                    WorkTrackingSystemConnectionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkTrackingSystemConnectionOption", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkTrackingSystemConnectionOption_WorkTrackingSystemConnec~",
                        column: x => x.WorkTrackingSystemConnectionId,
                        principalTable: "WorkTrackingSystemConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FeatureWork",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    RemainingWorkItems = table.Column<int>(type: "integer", nullable: false),
                    TotalWorkItems = table.Column<int>(type: "integer", nullable: false),
                    FeatureId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureWork", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureWork_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeatureWork_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ForecastBase",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TotalTrials = table.Column<int>(type: "integer", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Discriminator = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: false),
                    FeatureId = table.Column<int>(type: "integer", nullable: true),
                    TeamId = table.Column<int>(type: "integer", nullable: true),
                    NumberOfItems = table.Column<int>(type: "integer", nullable: true),
                    FeatureHistoryEntryId = table.Column<int>(type: "integer", nullable: true),
                    WhenForecastHistoryEntry_TeamId = table.Column<int>(type: "integer", nullable: true),
                    WhenForecastHistoryEntry_NumberOfItems = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastBase", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastBase_FeatureHistory_FeatureHistoryEntryId",
                        column: x => x.FeatureHistoryEntryId,
                        principalTable: "FeatureHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForecastBase_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForecastBase_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    WorkItemTypes = table.Column<List<string>>(type: "text[]", nullable: false),
                    DefaultAmountOfWorkItemsPerFeature = table.Column<int>(type: "integer", nullable: false),
                    OwningTeamId = table.Column<int>(type: "integer", nullable: true),
                    FeatureOwnerField = table.Column<string>(type: "text", nullable: true),
                    ProjectUpdateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UnparentedItemsQuery = table.Column<string>(type: "text", nullable: true),
                    SizeEstimateField = table.Column<string>(type: "text", nullable: true),
                    UsePercentileToCalculateDefaultAmountOfWorkItems = table.Column<bool>(type: "boolean", nullable: false),
                    HistoricalFeaturesWorkItemQuery = table.Column<string>(type: "text", nullable: false),
                    DefaultWorkItemPercentile = table.Column<int>(type: "integer", nullable: false),
                    OverrideRealChildCountStates = table.Column<List<string>>(type: "text[]", nullable: false),
                    WorkItemQuery = table.Column<string>(type: "text", nullable: false),
                    ToDoStates = table.Column<List<string>>(type: "text[]", nullable: false),
                    DoingStates = table.Column<List<string>>(type: "text[]", nullable: false),
                    DoneStates = table.Column<List<string>>(type: "text[]", nullable: false),
                    WorkTrackingSystemConnectionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Projects_Teams_OwningTeamId",
                        column: x => x.OwningTeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Projects_WorkTrackingSystemConnections_WorkTrackingSystemCo~",
                        column: x => x.WorkTrackingSystemConnectionId,
                        principalTable: "WorkTrackingSystemConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IndividualSimulationResult",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Key = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    ForecastId = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "FeatureProject",
                columns: table => new
                {
                    FeaturesId = table.Column<int>(type: "integer", nullable: false),
                    ProjectsId = table.Column<int>(type: "integer", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "Milestone",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectId = table.Column<int>(type: "integer", nullable: false)
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
                name: "ProjectTeam",
                columns: table => new
                {
                    ProjectsId = table.Column<int>(type: "integer", nullable: false),
                    TeamsId = table.Column<int>(type: "integer", nullable: false)
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
                name: "IX_FeatureHistory_FeatureReferenceId_Snapshot",
                table: "FeatureHistory",
                columns: new[] { "FeatureReferenceId", "Snapshot" });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureProject_ProjectsId",
                table: "FeatureProject",
                column: "ProjectsId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureWork_FeatureId",
                table: "FeatureWork",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureWork_TeamId",
                table: "FeatureWork",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureWorkHistoryEntry_FeatureHistoryEntryId",
                table: "FeatureWorkHistoryEntry",
                column: "FeatureHistoryEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastBase_FeatureHistoryEntryId",
                table: "ForecastBase",
                column: "FeatureHistoryEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastBase_FeatureId",
                table: "ForecastBase",
                column: "FeatureId");

            migrationBuilder.CreateIndex(
                name: "IX_ForecastBase_TeamId",
                table: "ForecastBase",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_IndividualSimulationResult_ForecastId",
                table: "IndividualSimulationResult",
                column: "ForecastId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestone_ProjectId",
                table: "Milestone",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_OwningTeamId",
                table: "Projects",
                column: "OwningTeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_WorkTrackingSystemConnectionId",
                table: "Projects",
                column: "WorkTrackingSystemConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectTeam_TeamsId",
                table: "ProjectTeam",
                column: "TeamsId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_WorkTrackingSystemConnectionId",
                table: "Teams",
                column: "WorkTrackingSystemConnectionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkTrackingSystemConnectionOption_WorkTrackingSystemConnec~",
                table: "WorkTrackingSystemConnectionOption",
                column: "WorkTrackingSystemConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.DropTable(
                name: "FeatureProject");

            migrationBuilder.DropTable(
                name: "FeatureWork");

            migrationBuilder.DropTable(
                name: "FeatureWorkHistoryEntry");

            migrationBuilder.DropTable(
                name: "IndividualSimulationResult");

            migrationBuilder.DropTable(
                name: "Milestone");

            migrationBuilder.DropTable(
                name: "PreviewFeatures");

            migrationBuilder.DropTable(
                name: "ProjectTeam");

            migrationBuilder.DropTable(
                name: "WorkTrackingSystemConnectionOption");

            migrationBuilder.DropTable(
                name: "ForecastBase");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "FeatureHistory");

            migrationBuilder.DropTable(
                name: "Features");

            migrationBuilder.DropTable(
                name: "Teams");

            migrationBuilder.DropTable(
                name: "WorkTrackingSystemConnections");
        }
    }
}
