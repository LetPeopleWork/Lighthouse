using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations;

/// <inheritdoc />
public partial class _20261127160359_AddWithinSleSnapshots : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "WithinSleSnapshots",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                OwnerId = table.Column<int>(type: "integer", nullable: false),
                OwnerType = table.Column<int>(type: "integer", nullable: false),
                RecordedAt = table.Column<DateOnly>(type: "date", nullable: false),
                Horizon = table.Column<int>(type: "integer", nullable: false),
                ClosedCount = table.Column<int>(type: "integer", nullable: false),
                WithinCount = table.Column<int>(type: "integer", nullable: false),
                SlePercentile = table.Column<int>(type: "integer", nullable: false),
                SleDays = table.Column<int>(type: "integer", nullable: false),
                Origin = table.Column<int>(type: "integer", nullable: false)
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
