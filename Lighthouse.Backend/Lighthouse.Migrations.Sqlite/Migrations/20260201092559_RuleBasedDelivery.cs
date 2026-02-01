using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RuleBasedDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RuleDefinitionJson",
                table: "Deliveries",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RuleSchemaVersion",
                table: "Deliveries",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SelectionMode",
                table: "Deliveries",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RuleDefinitionJson",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "RuleSchemaVersion",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "SelectionMode",
                table: "Deliveries");
        }
    }
}
