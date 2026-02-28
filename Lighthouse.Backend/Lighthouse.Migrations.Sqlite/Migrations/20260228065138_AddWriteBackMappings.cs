using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWriteBackMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WriteBackMappingDefinition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ValueSource = table.Column<int>(type: "INTEGER", nullable: false),
                    AppliesTo = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetFieldReference = table.Column<string>(type: "TEXT", nullable: false),
                    TargetValueType = table.Column<int>(type: "INTEGER", nullable: false),
                    DateFormat = table.Column<string>(type: "TEXT", nullable: true),
                    WorkTrackingSystemConnectionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WriteBackMappingDefinition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WriteBackMappingDefinition_WorkTrackingSystemConnections_WorkTrackingSystemConnectionId",
                        column: x => x.WorkTrackingSystemConnectionId,
                        principalTable: "WorkTrackingSystemConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WriteBackMappingDefinition_WorkTrackingSystemConnectionId",
                table: "WriteBackMappingDefinition",
                column: "WorkTrackingSystemConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WriteBackMappingDefinition");
        }
    }
}
