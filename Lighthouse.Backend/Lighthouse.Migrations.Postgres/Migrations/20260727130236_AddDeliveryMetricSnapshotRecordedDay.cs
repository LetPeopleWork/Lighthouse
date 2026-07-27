using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryMetricSnapshotRecordedDay : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "RecordedDay",
                table: "DeliveryMetricSnapshots",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            // Backfill every existing row onto the calendar day of its own RecordedAt. RecordedAt is
            // a timestamptz, and a bare CAST(... AS date) would reduce it in the SESSION time zone -
            // the very class of bug this step exists to remove - so it is pinned to UTC explicitly.
            //
            // A colliding population - one delivery with two RecordedAt values on the same calendar
            // day - would make the unique index below fail and the application refuse to start.
            // DeliveryMetricSnapshotDayCollisionGuard pre-checks for exactly that and aborts first,
            // naming every offending DeliveryId and date. DEDUPLICATION WAS CONSIDERED AND
            // DELIBERATELY REJECTED: quietly rewriting recorded history is worse than failing to
            // start with a message the operator can act on. Do not add a row-removing statement here.
            migrationBuilder.Sql("""
                UPDATE "DeliveryMetricSnapshots"
                SET "RecordedDay" = CAST(("RecordedAt" AT TIME ZONE 'UTC') AS date)
                """);

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMetricSnapshots_DeliveryId_RecordedDay",
                table: "DeliveryMetricSnapshots",
                columns: new[] { "DeliveryId", "RecordedDay" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DeliveryMetricSnapshots_DeliveryId_RecordedDay",
                table: "DeliveryMetricSnapshots");

            migrationBuilder.DropColumn(
                name: "RecordedDay",
                table: "DeliveryMetricSnapshots");
        }
    }
}
