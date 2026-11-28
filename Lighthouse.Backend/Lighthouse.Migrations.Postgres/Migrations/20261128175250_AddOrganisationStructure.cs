using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations;

/// <inheritdoc />
public partial class _20261128175250_AddOrganisationStructure : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ConnectionMappings",
            columns: table => new
            {
                ConnectionId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConnectionMappings", x => x.ConnectionId);
                table.ForeignKey(
                    name: "FK_ConnectionMappings_WorkTrackingSystemConnections_Connection~",
                    column: x => x.ConnectionId,
                    principalTable: "WorkTrackingSystemConnections",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "OrganisationStructures",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false),
                ConcurrencyToken = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrganisationStructures", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "ConnectionLevelMappings",
            columns: table => new
            {
                ConnectionId = table.Column<int>(type: "integer", nullable: false),
                LevelKey = table.Column<string>(type: "text", nullable: false),
                ItemTypes = table.Column<List<string>>(type: "text[]", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ConnectionLevelMappings", x => new { x.ConnectionId, x.LevelKey });
                table.ForeignKey(
                    name: "FK_ConnectionLevelMappings_ConnectionMappings_ConnectionId",
                    column: x => x.ConnectionId,
                    principalTable: "ConnectionMappings",
                    principalColumn: "ConnectionId",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Levels",
            columns: table => new
            {
                Key = table.Column<string>(type: "text", nullable: false),
                OrganisationStructureId = table.Column<int>(type: "integer", nullable: false),
                Ordinal = table.Column<int>(type: "integer", nullable: false),
                UnitSingular = table.Column<string>(type: "text", nullable: false),
                UnitPlural = table.Column<string>(type: "text", nullable: false),
                ItemSingular = table.Column<string>(type: "text", nullable: false),
                ItemPlural = table.Column<string>(type: "text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Levels", x => x.Key);
                table.ForeignKey(
                    name: "FK_Levels_OrganisationStructures_OrganisationStructureId",
                    column: x => x.OrganisationStructureId,
                    principalTable: "OrganisationStructures",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Levels_OrganisationStructureId",
            table: "Levels",
            column: "OrganisationStructureId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ConnectionLevelMappings");

        migrationBuilder.DropTable(
            name: "Levels");

        migrationBuilder.DropTable(
            name: "ConnectionMappings");

        migrationBuilder.DropTable(
            name: "OrganisationStructures");
    }
}
