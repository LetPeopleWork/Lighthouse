using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SQLitePCL;

namespace Lighthouse.Backend.Data
{
    /// <summary>
    /// Restores SQLite's legacy double-quoted-string tolerance (DQS) on every opened SQLite connection.
    /// SQLitePCLRaw.bundle_e_sqlite3 2.1.12 (pinned to resolve CVE-2025-6965) ships a SQLite engine with
    /// DQS disabled, which rejects double-quoted identifiers that older engines silently reinterpreted as
    /// string literals. Several historical EF migrations regenerate table-rebuild SQL that relied on that
    /// lenient behaviour, so a from-scratch <c>Migrate()</c> otherwise fails with
    /// <c>no such column: "X" - should this be a string literal in single-quotes?</c>. Enabling DQS_DML/DQS_DDL
    /// reproduces the pre-2.1.12 engine behaviour without reverting the security pin or rewriting applied
    /// migrations. No-op for non-SQLite providers.
    /// </summary>
    public sealed class SqliteLegacyDoubleQuotedStringInterceptor : DbConnectionInterceptor
    {
        public static readonly SqliteLegacyDoubleQuotedStringInterceptor Instance = new();

        public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
        {
            EnableLegacyDoubleQuotedStrings(connection);
            base.ConnectionOpened(connection, eventData);
        }

        public override Task ConnectionOpenedAsync(
            DbConnection connection,
            ConnectionEndEventData eventData,
            CancellationToken cancellationToken = default)
        {
            EnableLegacyDoubleQuotedStrings(connection);
            return base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
        }

        /// <summary>
        /// Enables legacy double-quoted-string handling on an already-open SQLite connection. Call this
        /// directly for connections that are opened outside EF's interception pipeline (e.g. a connection
        /// opened manually before being handed to <c>UseSqlite</c>).
        /// </summary>
        public static void EnableLegacyDoubleQuotedStrings(DbConnection connection)
        {
            if (connection is not SqliteConnection sqliteConnection)
            {
                return;
            }

            var handle = sqliteConnection.Handle;
            if (handle == null)
            {
                return;
            }

            raw.sqlite3_db_config(handle, raw.SQLITE_DBCONFIG_DQS_DML, 1, out _);
            raw.sqlite3_db_config(handle, raw.SQLITE_DBCONFIG_DQS_DDL, 1, out _);
        }
    }
}
