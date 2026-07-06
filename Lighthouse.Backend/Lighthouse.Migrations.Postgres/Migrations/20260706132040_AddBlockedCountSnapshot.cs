using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockedCountSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockedCountSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    OwnerType = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateOnly>(type: "date", nullable: false),
                    BlockedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedCountSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockedCountSnapshots_OwnerId_OwnerType_RecordedAt",
                table: "BlockedCountSnapshots",
                columns: new[] { "OwnerId", "OwnerType", "RecordedAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockedCountSnapshots");
        }
    }
}
