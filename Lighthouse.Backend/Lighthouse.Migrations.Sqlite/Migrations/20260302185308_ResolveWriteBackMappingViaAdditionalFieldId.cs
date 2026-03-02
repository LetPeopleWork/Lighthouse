using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class ResolveWriteBackMappingViaAdditionalFieldId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdditionalFieldDefinitionId",
                table: "WriteBackMappingDefinition",
                type: "INTEGER",
                nullable: true);

            // Migrate existing TargetFieldReference values to AdditionalFieldDefinitionId
            migrationBuilder.Sql("""
                UPDATE WriteBackMappingDefinition
                SET AdditionalFieldDefinitionId = (
                    SELECT afd.Id
                    FROM AdditionalFieldDefinition afd
                    WHERE afd.Reference = WriteBackMappingDefinition.TargetFieldReference
                      AND afd.WorkTrackingSystemConnectionId = WriteBackMappingDefinition.WorkTrackingSystemConnectionId
                    LIMIT 1
                )
                WHERE TargetFieldReference IS NOT NULL AND TargetFieldReference != ''
                """);

            migrationBuilder.DropColumn(
                name: "TargetFieldReference",
                table: "WriteBackMappingDefinition");

            migrationBuilder.CreateIndex(
                name: "IX_WriteBackMappingDefinition_AdditionalFieldDefinitionId",
                table: "WriteBackMappingDefinition",
                column: "AdditionalFieldDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WriteBackMappingDefinition_AdditionalFieldDefinition_AdditionalFieldDefinitionId",
                table: "WriteBackMappingDefinition",
                column: "AdditionalFieldDefinitionId",
                principalTable: "AdditionalFieldDefinition",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WriteBackMappingDefinition_AdditionalFieldDefinition_AdditionalFieldDefinitionId",
                table: "WriteBackMappingDefinition");

            migrationBuilder.DropIndex(
                name: "IX_WriteBackMappingDefinition_AdditionalFieldDefinitionId",
                table: "WriteBackMappingDefinition");

            migrationBuilder.DropColumn(
                name: "AdditionalFieldDefinitionId",
                table: "WriteBackMappingDefinition");

            migrationBuilder.AddColumn<string>(
                name: "TargetFieldReference",
                table: "WriteBackMappingDefinition",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
