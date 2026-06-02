using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
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
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DeliveryId = table.Column<int>(type: "integer", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalWork = table.Column<int>(type: "integer", nullable: false),
                    DoneWork = table.Column<int>(type: "integer", nullable: false),
                    RemainingWork = table.Column<int>(type: "integer", nullable: false),
                    EstimatedTotalWork = table.Column<int>(type: "integer", nullable: true),
                    ForecastHowMany = table.Column<int>(type: "integer", nullable: true),
                    LikelihoodPercentage = table.Column<double>(type: "double precision", nullable: true),
                    WhenDistributionJson = table.Column<string>(type: "text", nullable: true)
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
