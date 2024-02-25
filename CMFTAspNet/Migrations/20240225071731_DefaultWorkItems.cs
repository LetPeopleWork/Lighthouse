using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMFTAspNet.Migrations
{
    /// <inheritdoc />
    public partial class DefaultWorkItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultAmountOfWorkItemsPerFeature",
                table: "Projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultAmountOfWorkItemsPerFeature",
                table: "Projects");
        }
    }
}
