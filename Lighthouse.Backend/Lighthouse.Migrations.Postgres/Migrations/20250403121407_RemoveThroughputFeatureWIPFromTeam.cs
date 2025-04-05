using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RemoveThroughputFeatureWIPFromTeam : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FeaturesInProgress",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "RawThroughput",
                table: "Teams");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TeamUpdateTime",
                table: "Teams",
                nullable: false,
                defaultValue: DateTime.MinValue,
                oldClrType: typeof(DateTime));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "FeaturesInProgress",
                table: "Teams",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<int[]>(
                name: "RawThroughput",
                table: "Teams",
                type: "integer[]",
                nullable: false,
                defaultValue: new int[0]);
        }
    }
}
