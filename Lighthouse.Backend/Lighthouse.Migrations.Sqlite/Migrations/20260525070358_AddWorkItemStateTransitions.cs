using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemStateTransitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentStateEnteredAt",
                table: "WorkItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CurrentStateEnteredAt",
                table: "Features",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkItemStateTransitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    WorkItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    FromState = table.Column<string>(type: "TEXT", nullable: false),
                    ToState = table.Column<string>(type: "TEXT", nullable: false),
                    TransitionedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkItemStateTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkItemStateTransitions_WorkItems_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "WorkItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkItemStateTransitions_WorkItemId_TransitionedAt",
                table: "WorkItemStateTransitions",
                columns: new[] { "WorkItemId", "TransitionedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkItemStateTransitions");

            migrationBuilder.DropColumn(
                name: "CurrentStateEnteredAt",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "CurrentStateEnteredAt",
                table: "Features");
        }
    }
}
