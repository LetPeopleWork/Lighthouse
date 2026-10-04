using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddSizingLogEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SizingLogEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    WorkItemReferenceId = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Answer = table.Column<int>(type: "integer", nullable: true),
                    Comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VoterKey = table.Column<string>(type: "text", nullable: false),
                    VoterProfileId = table.Column<int>(type: "integer", nullable: true),
                    VoterDisplayName = table.Column<string>(type: "text", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Channel = table.Column<int>(type: "integer", nullable: false),
                    YardstickDays = table.Column<int>(type: "integer", nullable: true),
                    YardstickSource = table.Column<int>(type: "integer", nullable: false),
                    YardstickProbability = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SizingLogEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SizingLogEntries_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SizingLogEntries_UserProfiles_VoterProfileId",
                        column: x => x.VoterProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SizingLogEntries_TeamId_WorkItemReferenceId_Id",
                table: "SizingLogEntries",
                columns: new[] { "TeamId", "WorkItemReferenceId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_SizingLogEntries_VoterProfileId",
                table: "SizingLogEntries",
                column: "VoterProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SizingLogEntries");
        }
    }
}
