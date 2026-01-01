using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthenticationMethodKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add column as nullable
            migrationBuilder.AddColumn<string>(
                name: "AuthenticationMethodKey",
                table: "WorkTrackingSystemConnections",
                type: "text",
                nullable: true);

            // Step 2: Backfill based on WorkTrackingSystem and options
            // Azure DevOps (0) -> ado.pat
            migrationBuilder.Sql(@"
                UPDATE ""WorkTrackingSystemConnections"" 
                SET ""AuthenticationMethodKey"" = 'ado.pat' 
                WHERE ""WorkTrackingSystem"" = 0");

            // Linear (2) -> linear.apikey
            migrationBuilder.Sql(@"
                UPDATE ""WorkTrackingSystemConnections"" 
                SET ""AuthenticationMethodKey"" = 'linear.apikey' 
                WHERE ""WorkTrackingSystem"" = 2");

            // CSV (3) -> none
            migrationBuilder.Sql(@"
                UPDATE ""WorkTrackingSystemConnections"" 
                SET ""AuthenticationMethodKey"" = 'none' 
                WHERE ""WorkTrackingSystem"" = 3");

            // Jira (1) -> jira.cloud if Username option has a value, else jira.datacenter
            migrationBuilder.Sql(@"
                UPDATE ""WorkTrackingSystemConnections"" 
                SET ""AuthenticationMethodKey"" = 
                    CASE 
                        WHEN EXISTS (
                            SELECT 1 FROM ""WorkTrackingSystemConnectionOption"" 
                            WHERE ""WorkTrackingSystemConnectionId"" = ""WorkTrackingSystemConnections"".""Id"" 
                            AND ""Key"" = 'Username' 
                            AND ""Value"" IS NOT NULL 
                            AND TRIM(""Value"") != ''
                        ) THEN 'jira.cloud'
                        ELSE 'jira.datacenter'
                    END
                WHERE ""WorkTrackingSystem"" = 1");

            // Step 3: Ensure no nulls remain (fallback for any edge case)
            migrationBuilder.Sql(@"
                UPDATE ""WorkTrackingSystemConnections"" 
                SET ""AuthenticationMethodKey"" = 'none' 
                WHERE ""AuthenticationMethodKey"" IS NULL");

            // Step 4: Alter column to NOT NULL
            migrationBuilder.AlterColumn<string>(
                name: "AuthenticationMethodKey",
                table: "WorkTrackingSystemConnections",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthenticationMethodKey",
                table: "WorkTrackingSystemConnections");
        }
    }
}
