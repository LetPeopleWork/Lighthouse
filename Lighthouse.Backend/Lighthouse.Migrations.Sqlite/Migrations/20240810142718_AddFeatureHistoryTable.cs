using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FeatureHistoryEntryId",
                table: "ForecastBase",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WhenForecastHistoryEntry_NumberOfItems",
                table: "ForecastBase",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WhenForecastHistoryEntry_TeamId",
                table: "ForecastBase",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FeatureHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FeatureId = table.Column<int>(type: "INTEGER", nullable: false),
                    FeatureReferenceId = table.Column<string>(type: "TEXT", nullable: false),
                    Snapshot = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureWorkHistoryEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TeamId = table.Column<int>(type: "INTEGER", nullable: false),
                    RemainingWorkItems = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalWorkItems = table.Column<int>(type: "INTEGER", nullable: false),
                    FeatureHistoryEntryId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureWorkHistoryEntry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureWorkHistoryEntry_FeatureHistory_FeatureHistoryEntryId",
                        column: x => x.FeatureHistoryEntryId,
                        principalTable: "FeatureHistory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastBase_FeatureHistoryEntryId",
                table: "ForecastBase",
                column: "FeatureHistoryEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureWorkHistoryEntry_FeatureHistoryEntryId",
                table: "FeatureWorkHistoryEntry",
                column: "FeatureHistoryEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_ForecastBase_FeatureHistory_FeatureHistoryEntryId",
                table: "ForecastBase",
                column: "FeatureHistoryEntryId",
                principalTable: "FeatureHistory",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForecastBase_FeatureHistory_FeatureHistoryEntryId",
                table: "ForecastBase");

            migrationBuilder.DropTable(
                name: "FeatureWorkHistoryEntry");

            migrationBuilder.DropTable(
                name: "FeatureHistory");

            migrationBuilder.DropIndex(
                name: "IX_ForecastBase_FeatureHistoryEntryId",
                table: "ForecastBase");

            migrationBuilder.DropColumn(
                name: "FeatureHistoryEntryId",
                table: "ForecastBase");

            migrationBuilder.DropColumn(
                name: "WhenForecastHistoryEntry_NumberOfItems",
                table: "ForecastBase");

            migrationBuilder.DropColumn(
                name: "WhenForecastHistoryEntry_TeamId",
                table: "ForecastBase");
        }
    }
}
