using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddIdAsPrimaryKeyForTerminologyEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TerminologyEntries",
                table: "TerminologyEntries");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "TerminologyEntries",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TerminologyEntries",
                table: "TerminologyEntries",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_TerminologyEntries_Key",
                table: "TerminologyEntries",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TerminologyEntries",
                table: "TerminologyEntries");

            migrationBuilder.DropIndex(
                name: "IX_TerminologyEntries_Key",
                table: "TerminologyEntries");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "TerminologyEntries",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TerminologyEntries",
                table: "TerminologyEntries",
                column: "Key");
        }
    }
}
