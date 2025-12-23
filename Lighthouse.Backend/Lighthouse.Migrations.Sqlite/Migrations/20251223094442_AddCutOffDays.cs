using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCutOffDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DoneItemsCutoffDays",
                table: "Teams",
                type: "INTEGER",
                nullable: false,
                defaultValue: 180);

            migrationBuilder.AddColumn<int>(
                name: "DoneItemsCutoffDays",
                table: "Portfolios",
                type: "INTEGER",
                nullable: false,
                defaultValue: 365);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DoneItemsCutoffDays",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "DoneItemsCutoffDays",
                table: "Portfolios");
        }
    }
}
