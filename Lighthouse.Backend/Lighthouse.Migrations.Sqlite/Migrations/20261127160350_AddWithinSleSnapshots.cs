using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations;

/// <inheritdoc />
public partial class _20261127160350_AddWithinSleSnapshots : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WithinSleSnapshots",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                OwnerType = table.Column<int>(type: "INTEGER", nullable: false),
                RecordedAt = table.Column<DateOnly>(type: "TEXT", nullable: false),
                Horizon = table.Column<int>(type: "INTEGER", nullable: false),
                ClosedCount = table.Column<int>(type: "INTEGER", nullable: false),
                WithinCount = table.Column<int>(type: "INTEGER", nullable: false),
                SlePercentile = table.Column<int>(type: "INTEGER", nullable: false),
                SleDays = table.Column<int>(type: "INTEGER", nullable: false),
                Origin = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_WithinSleSnapshots", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_WithinSleSnapshots_OwnerId_OwnerType_Horizon_RecordedAt",
            table: "WithinSleSnapshots",
            columns: new[] { "OwnerId", "OwnerType", "Horizon", "RecordedAt" },
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "WithinSleSnapshots");
    }
}
