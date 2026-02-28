using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ValueSource = table.Column<int>(type: "integer", nullable: false),
                    AppliesTo = table.Column<int>(type: "integer", nullable: false),
                    TargetFieldReference = table.Column<string>(type: "text", nullable: false),
                    TargetValueType = table.Column<int>(type: "integer", nullable: false),
                    DateFormat = table.Column<string>(type: "text", nullable: true),
                    WorkTrackingSystemConnectionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WriteBackMappingDefinition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WriteBackMappingDefinition_WorkTrackingSystemConnections_Wo~",
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
