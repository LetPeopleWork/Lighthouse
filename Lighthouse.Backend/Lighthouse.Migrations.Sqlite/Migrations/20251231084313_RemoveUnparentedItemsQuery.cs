using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnparentedItemsQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete features that were marked as unparented (not needed anymore)
            migrationBuilder.Sql("DELETE FROM \"Features\" WHERE \"IsUnparentedFeature\" = 1;");

            migrationBuilder.DropColumn(
                name: "UnparentedItemsQuery",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "IsUnparentedFeature",
                table: "Features");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UnparentedItemsQuery",
                table: "Portfolios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnparentedFeature",
                table: "Features",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
