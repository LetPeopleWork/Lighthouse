using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RenameWorkItemQueryToDataRetrievalValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WorkItemQuery",
                table: "Teams",
                newName: "DataRetrievalValue");

            migrationBuilder.RenameColumn(
                name: "WorkItemQuery",
                table: "Portfolios",
                newName: "DataRetrievalValue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DataRetrievalValue",
                table: "Teams",
                newName: "WorkItemQuery");

            migrationBuilder.RenameColumn(
                name: "DataRetrievalValue",
                table: "Portfolios",
                newName: "WorkItemQuery");
        }
    }
}
