using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations;

/// <inheritdoc />
public partial class _20261126172255_AddReportNotes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ReportNotes",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ReportId = table.Column<int>(type: "integer", nullable: false),
                Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastEditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                AuthorUserProfileId = table.Column<int>(type: "integer", nullable: true),
                AuthorDisplayName = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReportNotes", x => x.Id);
                table.ForeignKey(
                    name: "FK_ReportNotes_Reports_ReportId",
                    column: x => x.ReportId,
                    principalTable: "Reports",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ReportNotes_UserProfiles_AuthorUserProfileId",
                    column: x => x.AuthorUserProfileId,
                    principalTable: "UserProfiles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ReportNotes_AuthorUserProfileId",
            table: "ReportNotes",
            column: "AuthorUserProfileId");

        migrationBuilder.CreateIndex(
            name: "IX_ReportNotes_ReportId",
            table: "ReportNotes",
            column: "ReportId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ReportNotes");
    }
}
