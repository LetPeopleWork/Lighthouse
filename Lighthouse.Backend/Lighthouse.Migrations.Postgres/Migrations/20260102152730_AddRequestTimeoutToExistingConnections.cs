using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestTimeoutToExistingConnections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert RequestTimeoutInSeconds option for existing Azure DevOps connections
            // Use the value from AppSettings if it exists, otherwise default to 100
            migrationBuilder.Sql(@"
                INSERT INTO ""WorkTrackingSystemConnectionOption"" (""Key"", ""Value"", ""IsSecret"", ""IsOptional"", ""WorkTrackingSystemConnectionId"")
                SELECT 
                    'Request Timeout In Seconds',
                    COALESCE(
                        (SELECT ""Value"" FROM ""AppSettings"" WHERE ""Key"" = 'WorkTrackingSystemSettings:RequestTimeoutInSeconds'),
                        '100'
                    ),
                    false,
                    true,
                    ""Id""
                FROM ""WorkTrackingSystemConnections""
                WHERE ""WorkTrackingSystem"" = 0
                AND NOT EXISTS (
                    SELECT 1 FROM ""WorkTrackingSystemConnectionOption"" o
                    WHERE o.""WorkTrackingSystemConnectionId"" = ""WorkTrackingSystemConnections"".""Id""
                    AND o.""Key"" = 'Request Timeout In Seconds'
                )");

            // Insert RequestTimeoutInSeconds option for existing Jira connections
            migrationBuilder.Sql(@"
                INSERT INTO ""WorkTrackingSystemConnectionOption"" (""Key"", ""Value"", ""IsSecret"", ""IsOptional"", ""WorkTrackingSystemConnectionId"")
                SELECT 
                    'Request Timeout In Seconds',
                    COALESCE(
                        (SELECT ""Value"" FROM ""AppSettings"" WHERE ""Key"" = 'WorkTrackingSystemSettings:RequestTimeoutInSeconds'),
                        '100'
                    ),
                    false,
                    true,
                    ""Id""
                FROM ""WorkTrackingSystemConnections""
                WHERE ""WorkTrackingSystem"" = 1
                AND NOT EXISTS (
                    SELECT 1 FROM ""WorkTrackingSystemConnectionOption"" o
                    WHERE o.""WorkTrackingSystemConnectionId"" = ""WorkTrackingSystemConnections"".""Id""
                    AND o.""Key"" = 'Request Timeout In Seconds'
                )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove RequestTimeoutInSeconds options that were added by this migration
            migrationBuilder.Sql(@"
                DELETE FROM ""WorkTrackingSystemConnectionOption""
                WHERE ""Key"" = 'Request Timeout In Seconds'
                AND ""WorkTrackingSystemConnectionId"" IN (
                    SELECT ""Id"" FROM ""WorkTrackingSystemConnections""
                    WHERE ""WorkTrackingSystem"" IN (0, 1)
                )");
        }
    }
}
