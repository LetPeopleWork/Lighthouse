using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
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
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

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
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TerminologyEntries",
                table: "TerminologyEntries",
                column: "Key");
        }
    }
}
