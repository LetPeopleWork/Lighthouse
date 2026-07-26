using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessBehaviorSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessBehaviorSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerType = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAt = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    MetricType = table.Column<int>(type: "INTEGER", nullable: false),
                    Unpl = table.Column<int>(type: "INTEGER", nullable: false),
                    Average = table.Column<int>(type: "INTEGER", nullable: false),
                    Lnpl = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessBehaviorSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessBehaviorSnapshots_OwnerId_OwnerType_MetricType_RecordedAt",
                table: "ProcessBehaviorSnapshots",
                columns: new[] { "OwnerId", "OwnerType", "MetricType", "RecordedAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProcessBehaviorSnapshots");
        }
    }
}
