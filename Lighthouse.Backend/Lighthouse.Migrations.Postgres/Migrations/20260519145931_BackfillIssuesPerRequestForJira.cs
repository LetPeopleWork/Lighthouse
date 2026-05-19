using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lighthouse.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class BackfillIssuesPerRequestForJira : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO ""WorkTrackingSystemConnectionOption"" (""WorkTrackingSystemConnectionId"", ""Key"", ""Value"", ""IsSecret"", ""IsOptional"")
                SELECT c.""Id"", 'Issues Per Request', '1000', FALSE, TRUE
                FROM ""WorkTrackingSystemConnections"" c
                WHERE c.""WorkTrackingSystem"" = 1
                  AND NOT EXISTS (
                    SELECT 1 FROM ""WorkTrackingSystemConnectionOption"" o
                    WHERE o.""WorkTrackingSystemConnectionId"" = c.""Id""
                      AND o.""Key"" = 'Issues Per Request'
                  )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""WorkTrackingSystemConnectionOption""
                WHERE ""Key"" = 'Issues Per Request'
                  AND ""WorkTrackingSystemConnectionId"" IN (
                    SELECT ""Id"" FROM ""WorkTrackingSystemConnections"" WHERE ""WorkTrackingSystem"" = 1
                  )");
        }
    }
}
