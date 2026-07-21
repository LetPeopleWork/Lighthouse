using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddFeatureBlockedTransition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FeatureBlockedTransitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FeatureId = table.Column<int>(type: "INTEGER", nullable: false),
                    PortfolioId = table.Column<int>(type: "INTEGER", nullable: false),
                    EnteredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LeftAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureBlockedTransitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FeatureBlockedTransitions_Features_FeatureId",
                        column: x => x.FeatureId,
                        principalTable: "Features",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FeatureBlockedTransitions_Portfolios_PortfolioId",
                        column: x => x.PortfolioId,
                        principalTable: "Portfolios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureBlockedTransitions_FeatureId_PortfolioId",
                table: "FeatureBlockedTransitions",
                columns: new[] { "FeatureId", "PortfolioId" });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureBlockedTransitions_PortfolioId_EnteredAt",
                table: "FeatureBlockedTransitions",
                columns: new[] { "PortfolioId", "EnteredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureBlockedTransitions");
        }
    }
}
