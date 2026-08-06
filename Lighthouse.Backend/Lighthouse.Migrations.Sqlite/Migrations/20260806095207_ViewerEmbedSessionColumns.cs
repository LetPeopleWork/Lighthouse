using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Backend.Migrations
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
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "SecretHash",
                table: "EmbedSessionTokens",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<int>(
                name: "ApiKeyId",
                table: "EmbedSessionTokens",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<DateTime>(
                name: "HandshakeConsumedAt",
                table: "EmbedSessionTokens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HandshakeNonceHash",
                table: "EmbedSessionTokens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefusalCode",
                table: "EmbedSessionTokens",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Subject",
                table: "EmbedSessionTokens",
                type: "TEXT",
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
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "SecretHash",
                table: "EmbedSessionTokens",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ApiKeyId",
                table: "EmbedSessionTokens",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
