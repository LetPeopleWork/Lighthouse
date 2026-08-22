using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
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
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DeliveryClosureRecords",
                columns: table => new
                {
                    DeliveryId = table.Column<int>(type: "INTEGER", nullable: false),
                    ArchivedOn = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TargetDateAtClosure = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalWork = table.Column<int>(type: "INTEGER", nullable: false),
                    DoneWork = table.Column<int>(type: "INTEGER", nullable: false),
                    RemainingWork = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimatedItemCount = table.Column<int>(type: "INTEGER", nullable: true),
                    ForecastHowMany = table.Column<int>(type: "INTEGER", nullable: true),
                    LikelihoodPercentage = table.Column<double>(type: "REAL", nullable: true),
                    WhenDistributionJson = table.Column<string>(type: "TEXT", nullable: true),
                    FeatureBreakdownJson = table.Column<string>(type: "TEXT", nullable: true),
                    HasSufficientData = table.Column<bool>(type: "INTEGER", nullable: false),
                    TeamsWithoutForecastJson = table.Column<string>(type: "TEXT", nullable: true),
                    SelectionMode = table.Column<int>(type: "INTEGER", nullable: false),
                    RuleDefinitionJson = table.Column<string>(type: "TEXT", nullable: true),
                    RuleSchemaVersion = table.Column<int>(type: "INTEGER", nullable: true)
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
