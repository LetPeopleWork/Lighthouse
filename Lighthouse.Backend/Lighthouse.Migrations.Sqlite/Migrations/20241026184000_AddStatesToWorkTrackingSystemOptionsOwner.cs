using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddStatesToWorkTrackingSystemOptionsOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
               name: "DoingStates",
               table: "Teams",
               type: "TEXT",
               nullable: false,
               defaultValue: "[\"Active\", \"Resolved\", \"In Progress\", \"Committed\"]");

            migrationBuilder.AddColumn<string>(
                name: "DoneStates",
                table: "Teams",
                type: "TEXT",
                nullable: false,
                defaultValue: "[\"Done\", \"Closed\"]");

            migrationBuilder.AddColumn<string>(
                name: "ToDoStates",
                table: "Teams",
                type: "TEXT",
                nullable: false,
                defaultValue: "[\"New\", \"Proposed\", \"To Do\"]");

            migrationBuilder.AddColumn<string>(
                name: "DoingStates",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "[\"Active\", \"Resolved\", \"In Progress\", \"Committed\"]");

            migrationBuilder.AddColumn<string>(
                name: "DoneStates",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "[\"Done\", \"Closed\"]");

            migrationBuilder.AddColumn<string>(
                name: "ToDoStates",
                table: "Projects",
                type: "TEXT",
                nullable: false,
                defaultValue: "[\"New\", \"Proposed\", \"To Do\"]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoingStates",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "DoneStates",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "ToDoStates",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "DoingStates",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "DoneStates",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ToDoStates",
                table: "Projects");
        }
    }
}
