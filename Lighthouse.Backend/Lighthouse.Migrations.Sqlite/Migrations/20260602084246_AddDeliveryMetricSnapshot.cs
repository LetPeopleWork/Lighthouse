using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryMetricSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DeliveryMetricSnapshots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DeliveryId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalWork = table.Column<int>(type: "INTEGER", nullable: false),
                    DoneWork = table.Column<int>(type: "INTEGER", nullable: false),
                    RemainingWork = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedTotalWork = table.Column<int>(type: "INTEGER", nullable: true),
                    ForecastHowMany = table.Column<int>(type: "INTEGER", nullable: true),
                    LikelihoodPercentage = table.Column<double>(type: "REAL", nullable: true),
                    WhenDistributionJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryMetricSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliveryMetricSnapshots_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DeliveryMetricSnapshots_DeliveryId_RecordedAt",
                table: "DeliveryMetricSnapshots",
                columns: new[] { "DeliveryId", "RecordedAt" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryMetricSnapshots");
        }
    }
}
