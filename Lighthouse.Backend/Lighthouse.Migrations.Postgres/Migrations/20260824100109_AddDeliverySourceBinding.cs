using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliverySourceBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceKey",
                table: "Deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SourceLastSyncedOn",
                table: "Deliveries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                table: "Deliveries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceUnavailableReason",
                table: "Deliveries",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SourceKey",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "SourceLastSyncedOn",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                table: "Deliveries");

            migrationBuilder.DropColumn(
                name: "SourceUnavailableReason",
                table: "Deliveries");
        }
    }
}
