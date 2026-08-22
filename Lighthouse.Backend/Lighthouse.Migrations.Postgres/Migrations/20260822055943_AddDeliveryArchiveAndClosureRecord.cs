using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryArchiveAndClosureRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivedOn",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryClosureRecords",
                columns: table => new
                {
                    DeliveryId = table.Column<int>(type: "integer", nullable: false),
                    ArchivedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TargetDateAtClosure = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TotalWork = table.Column<int>(type: "integer", nullable: false),
                    DoneWork = table.Column<int>(type: "integer", nullable: false),
                    RemainingWork = table.Column<int>(type: "integer", nullable: false),
                    EstimatedItemCount = table.Column<int>(type: "integer", nullable: true),
                    LikelihoodPercentage = table.Column<double>(type: "double precision", nullable: true),
                    WhenDistributionJson = table.Column<string>(type: "text", nullable: true),
                    FeatureBreakdownJson = table.Column<string>(type: "text", nullable: true),
                    HasSufficientData = table.Column<bool>(type: "boolean", nullable: false),
                    TeamsWithoutForecastJson = table.Column<string>(type: "text", nullable: true),
                    SelectionMode = table.Column<int>(type: "integer", nullable: false),
                    RuleDefinitionJson = table.Column<string>(type: "text", nullable: true),
                    RuleSchemaVersion = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliveryClosureRecords", x => x.DeliveryId);
                    table.ForeignKey(
                        name: "FK_DeliveryClosureRecords_Deliveries_DeliveryId",
                        column: x => x.DeliveryId,
                        principalTable: "Deliveries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeliveryClosureRecords");

            migrationBuilder.DropColumn(
                name: "ArchivedOn",
                table: "Deliveries");
        }
    }
}
