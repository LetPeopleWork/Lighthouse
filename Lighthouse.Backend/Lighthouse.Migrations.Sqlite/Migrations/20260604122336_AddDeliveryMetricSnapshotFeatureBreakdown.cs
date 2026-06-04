using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryMetricSnapshotFeatureBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FeatureBreakdownJson",
                table: "DeliveryMetricSnapshots",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeatureBreakdownJson",
                table: "DeliveryMetricSnapshots");
        }
    }
}
