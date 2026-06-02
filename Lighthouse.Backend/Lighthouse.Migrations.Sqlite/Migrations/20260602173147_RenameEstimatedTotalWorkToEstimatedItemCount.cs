using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class RenameEstimatedTotalWorkToEstimatedItemCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EstimatedTotalWork",
                table: "DeliveryMetricSnapshots",
                newName: "EstimatedItemCount");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "EstimatedItemCount",
                table: "DeliveryMetricSnapshots",
                newName: "EstimatedTotalWork");
        }
    }
}
