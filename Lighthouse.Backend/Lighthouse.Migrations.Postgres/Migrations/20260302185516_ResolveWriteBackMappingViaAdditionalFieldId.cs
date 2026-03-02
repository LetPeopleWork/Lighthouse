using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
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
                type: "integer",
                nullable: true);

            // Migrate existing TargetFieldReference values to AdditionalFieldDefinitionId
            migrationBuilder.Sql("""
                UPDATE "WriteBackMappingDefinition" wbm
                SET "AdditionalFieldDefinitionId" = afd."Id"
                FROM "AdditionalFieldDefinition" afd
                WHERE afd."Reference" = wbm."TargetFieldReference"
                  AND afd."WorkTrackingSystemConnectionId" = wbm."WorkTrackingSystemConnectionId"
                  AND wbm."TargetFieldReference" IS NOT NULL
                  AND wbm."TargetFieldReference" != ''
                """);

            migrationBuilder.DropColumn(
                name: "TargetFieldReference",
                table: "WriteBackMappingDefinition");

            migrationBuilder.CreateIndex(
                name: "IX_WriteBackMappingDefinition_AdditionalFieldDefinitionId",
                table: "WriteBackMappingDefinition",
                column: "AdditionalFieldDefinitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_WriteBackMappingDefinition_AdditionalFieldDefinition_Additi~",
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
                name: "FK_WriteBackMappingDefinition_AdditionalFieldDefinition_Additi~",
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
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
