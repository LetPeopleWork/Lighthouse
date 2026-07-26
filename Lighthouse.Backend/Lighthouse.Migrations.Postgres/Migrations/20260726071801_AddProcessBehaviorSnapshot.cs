using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    OwnerType = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    MetricType = table.Column<int>(type: "integer", nullable: false),
                    Unpl = table.Column<int>(type: "integer", nullable: false),
                    Average = table.Column<int>(type: "integer", nullable: false),
                    Lnpl = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessBehaviorSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessBehaviorSnapshots_OwnerId_OwnerType_MetricType_Recor~",
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
