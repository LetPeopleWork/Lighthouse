using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RemoveArchivedFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "ForecastBase",
                type: "character varying(13)",
                maxLength: 13,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(34)",
                oldMaxLength: 34);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Discriminator",
                table: "ForecastBase",
                type: "character varying(34)",
                maxLength: 34,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(13)",
                oldMaxLength: 13);

            migrationBuilder.AddColumn<int>(
                name: "FeatureHistoryEntryId",
                table: "ForecastBase",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WhenForecastHistoryEntry_NumberOfItems",
                table: "ForecastBase",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WhenForecastHistoryEntry_TeamId",
                table: "ForecastBase",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FeatureHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FeatureId = table.Column<int>(type: "integer", nullable: false),
                    FeatureReferenceId = table.Column<string>(type: "text", nullable: false),
                    Snapshot = table.Column<DateOnly>(type: "date", nullable: false),
                    State = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureHistory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FeatureWorkHistoryEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FeatureHistoryEntryId = table.Column<int>(type: "integer", nullable: false),
                    RemainingWorkItems = table.Column<int>(type: "integer", nullable: false),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    TotalWorkItems = table.Column<int>(type: "integer", nullable: false)
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
                name: "IX_FeatureHistory_FeatureReferenceId_Snapshot",
                table: "FeatureHistory",
                columns: new[] { "FeatureReferenceId", "Snapshot" });

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
    }
}
