using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddPercentilesOverTimeSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PercentilesOverTimeSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerType = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAt = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    MetricType = table.Column<int>(type: "INTEGER", nullable: false),
                    Horizon = table.Column<int>(type: "INTEGER", nullable: true),
                    P50 = table.Column<int>(type: "INTEGER", nullable: false),
                    P70 = table.Column<int>(type: "INTEGER", nullable: false),
                    P85 = table.Column<int>(type: "INTEGER", nullable: false),
                    P95 = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PercentilesOverTimeSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PercentilesOverTimeSnapshots_OwnerId_OwnerType_MetricType_Horizon_RecordedAt",
                table: "PercentilesOverTimeSnapshots",
                columns: new[] { "OwnerId", "OwnerType", "MetricType", "Horizon", "RecordedAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PercentilesOverTimeSnapshots");
        }
    }
}
