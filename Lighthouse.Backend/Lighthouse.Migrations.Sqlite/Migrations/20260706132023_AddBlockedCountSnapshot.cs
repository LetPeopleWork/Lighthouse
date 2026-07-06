using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
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
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OwnerId = table.Column<int>(type: "INTEGER", nullable: false),
                    OwnerType = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAt = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    BlockedCount = table.Column<int>(type: "INTEGER", nullable: false)
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
