using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Lighthouse.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettingToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.InsertData(
                table: "AppSettings",
                columns: new[] { "Key", "Id", "Value" },
                values: new object[,]
                {
                    { "PeriodicRefresh:Features:Interval", 0, "60" },
                    { "PeriodicRefresh:Features:RefreshAfter", 0, "180" },
                    { "PeriodicRefresh:Features:StartDelay", 0, "2" },
                    { "PeriodicRefresh:Forecasts:Interval", 0, "60" },
                    { "PeriodicRefresh:Forecasts:RefreshAfter", 0, "180" },
                    { "PeriodicRefresh:Forecasts:StartDelay", 0, "5" },
                    { "PeriodicRefresh:Throughput:Interval", 0, "60" },
                    { "PeriodicRefresh:Throughput:RefreshAfter", 0, "180" },
                    { "PeriodicRefresh:Throughput:StartDelay", 0, "1" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");
        }
    }
}
