using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class FixTypoInDefaultWorkItemPercentilePropertyName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DefaultWokrItemPercentile",
                table: "Projects",
                newName: "DefaultWorkItemPercentile");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DefaultWorkItemPercentile",
                table: "Projects",
                newName: "DefaultWokrItemPercentile");
        }
    }
}
