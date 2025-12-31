using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnparentedItemsQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Delete features that were marked as unparented (not needed anymore)
            migrationBuilder.Sql("DELETE FROM \"Features\" WHERE \"IsUnparentedFeature\" = true;");

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
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsUnparentedFeature",
                table: "Features",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
