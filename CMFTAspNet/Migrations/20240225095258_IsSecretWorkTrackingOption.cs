using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMFTAspNet.Migrations
{
    /// <inheritdoc />
    public partial class IsSecretWorkTrackingOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Secret",
                table: "WorkTrackingSystemOption",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Secret",
                table: "WorkTrackingSystemOption");
        }
    }
}
