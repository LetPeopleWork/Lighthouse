using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomFieldsToWorkTrackingSystems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentOverrideField",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "FeatureOwnerField",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "ParentOverrideField",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "SizeEstimateField",
                table: "Portfolios");

            migrationBuilder.AddColumn<string>(
                name: "AdditionalFieldValues",
                table: "WorkItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ParentOverrideAdditionalFieldDefinitionId",
                table: "Teams",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeatureOwnerAdditionalFieldDefinitionId",
                table: "Portfolios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentOverrideAdditionalFieldDefinitionId",
                table: "Portfolios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SizeEstimateAdditionalFieldDefinitionId",
                table: "Portfolios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalFieldValues",
                table: "Features",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AdditionalFieldDefinition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Reference = table.Column<string>(type: "text", nullable: false),
                    WorkTrackingSystemConnectionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalFieldDefinition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdditionalFieldDefinition_WorkTrackingSystemConnections_Wor~",
                        column: x => x.WorkTrackingSystemConnectionId,
                        principalTable: "WorkTrackingSystemConnections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdditionalFieldDefinition_WorkTrackingSystemConnectionId",
                table: "AdditionalFieldDefinition",
                column: "WorkTrackingSystemConnectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdditionalFieldDefinition");

            migrationBuilder.DropColumn(
                name: "AdditionalFieldValues",
                table: "WorkItems");

            migrationBuilder.DropColumn(
                name: "ParentOverrideAdditionalFieldDefinitionId",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "FeatureOwnerAdditionalFieldDefinitionId",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "ParentOverrideAdditionalFieldDefinitionId",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "SizeEstimateAdditionalFieldDefinitionId",
                table: "Portfolios");

            migrationBuilder.DropColumn(
                name: "AdditionalFieldValues",
                table: "Features");

            migrationBuilder.AddColumn<string>(
                name: "ParentOverrideField",
                table: "Teams",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatureOwnerField",
                table: "Portfolios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentOverrideField",
                table: "Portfolios",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SizeEstimateField",
                table: "Portfolios",
                type: "text",
                nullable: true);
        }
    }
}
