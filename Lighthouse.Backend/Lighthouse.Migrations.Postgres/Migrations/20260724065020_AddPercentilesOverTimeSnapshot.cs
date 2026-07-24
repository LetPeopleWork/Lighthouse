using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    OwnerType = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    MetricType = table.Column<int>(type: "integer", nullable: false),
                    Horizon = table.Column<int>(type: "integer", nullable: true),
                    P50 = table.Column<int>(type: "integer", nullable: false),
                    P70 = table.Column<int>(type: "integer", nullable: false),
                    P85 = table.Column<int>(type: "integer", nullable: false),
                    P95 = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PercentilesOverTimeSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PercentilesOverTimeSnapshots_OwnerId_OwnerType_MetricType_H~",
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
