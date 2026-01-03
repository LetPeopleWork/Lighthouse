using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
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
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ParentOverrideAdditionalFieldDefinitionId",
                table: "Teams",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FeatureOwnerAdditionalFieldDefinitionId",
                table: "Portfolios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParentOverrideAdditionalFieldDefinitionId",
                table: "Portfolios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SizeEstimateAdditionalFieldDefinitionId",
                table: "Portfolios",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AdditionalFieldValues",
                table: "Features",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AdditionalFieldDefinition",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: false),
                    WorkTrackingSystemConnectionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdditionalFieldDefinition", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdditionalFieldDefinition_WorkTrackingSystemConnections_WorkTrackingSystemConnectionId",
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
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeatureOwnerField",
                table: "Portfolios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentOverrideField",
                table: "Portfolios",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SizeEstimateField",
                table: "Portfolios",
                type: "TEXT",
                nullable: true);
        }
    }
}
