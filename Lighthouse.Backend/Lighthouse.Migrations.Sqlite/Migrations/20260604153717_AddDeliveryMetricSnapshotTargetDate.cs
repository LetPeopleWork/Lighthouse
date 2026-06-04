using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryMetricSnapshotTargetDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TargetDateAtSnapshot",
                table: "DeliveryMetricSnapshots",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TargetDateAtSnapshot",
                table: "DeliveryMetricSnapshots");
        }
    }
}
