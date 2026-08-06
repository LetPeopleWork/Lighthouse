using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class ViewerEmbedSessionColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TokenId",
                table: "EmbedSessionTokens",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "SecretHash",
                table: "EmbedSessionTokens",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<int>(
                name: "ApiKeyId",
                table: "EmbedSessionTokens",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<DateTime>(
                name: "HandshakeConsumedAt",
                table: "EmbedSessionTokens",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandshakeNonceHash",
                table: "EmbedSessionTokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefusalCode",
                table: "EmbedSessionTokens",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "EmbedSessionTokens",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmbedSessionTokens_HandshakeNonceHash",
                table: "EmbedSessionTokens",
                column: "HandshakeNonceHash");

            migrationBuilder.CreateIndex(
                name: "IX_EmbedSessionTokens_Subject",
                table: "EmbedSessionTokens",
                column: "Subject");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EmbedSessionTokens_GrantOrRefusal",
                table: "EmbedSessionTokens",
                sql: "(\"TokenId\" IS NOT NULL AND \"SecretHash\" IS NOT NULL AND \"RefusalCode\" IS NULL) OR (\"TokenId\" IS NULL AND \"SecretHash\" IS NULL AND \"RefusalCode\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmbedSessionTokens_HandshakeNonceHash",
                table: "EmbedSessionTokens");

            migrationBuilder.DropIndex(
                name: "IX_EmbedSessionTokens_Subject",
                table: "EmbedSessionTokens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EmbedSessionTokens_GrantOrRefusal",
                table: "EmbedSessionTokens");

            migrationBuilder.DropColumn(
                name: "HandshakeConsumedAt",
                table: "EmbedSessionTokens");

            migrationBuilder.DropColumn(
                name: "HandshakeNonceHash",
                table: "EmbedSessionTokens");

            migrationBuilder.DropColumn(
                name: "RefusalCode",
                table: "EmbedSessionTokens");

            migrationBuilder.DropColumn(
                name: "Subject",
                table: "EmbedSessionTokens");

            migrationBuilder.AlterColumn<string>(
                name: "TokenId",
                table: "EmbedSessionTokens",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SecretHash",
                table: "EmbedSessionTokens",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ApiKeyId",
                table: "EmbedSessionTokens",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
