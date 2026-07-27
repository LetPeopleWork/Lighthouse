using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Data
{
    /// <summary>
    /// Pre-check for the AddDeliveryMetricSnapshotRecordedDay migration (Bug #5567). That migration
    /// puts a UNIQUE index over (DeliveryId, RecordedDay); colliding legacy rows would fail it at
    /// startup with a provider error naming nothing. Reachable only from a restored backup or a
    /// database written by an older version - which is exactly this migration's population.
    ///
    /// DEDUPLICATION WAS CONSIDERED AND DELIBERATELY REJECTED. Do not "helpfully" add it: an
    /// operator whose app refuses to start with the colliding rows named can repair them; an
    /// operator whose recorded history was quietly rewritten cannot.
    /// </summary>
    public static class DeliveryMetricSnapshotDayCollisionGuard
    {
        public const string GuardedMigrationSuffix = "_AddDeliveryMetricSnapshotRecordedDay";

        private const string TableCreatingMigrationSuffix = "_AddDeliveryMetricSnapshot";

        private const string SqliteCollisionSql = """
            SELECT "DeliveryId", date("RecordedAt") AS "Day", COUNT(*) AS "Rows"
            FROM "DeliveryMetricSnapshots"
            GROUP BY "DeliveryId", date("RecordedAt")
            HAVING COUNT(*) > 1
            ORDER BY "DeliveryId", "Day"
            """;

        // RecordedAt is a timestamptz: reducing it with a bare CAST would use the SESSION time zone,
        // so the pre-check pins UTC exactly like the migration's backfill does.
        private const string PostgresCollisionSql = """
            SELECT "DeliveryId", CAST(("RecordedAt" AT TIME ZONE 'UTC') AS date) AS "Day", COUNT(*) AS "Rows"
            FROM "DeliveryMetricSnapshots"
            GROUP BY "DeliveryId", CAST(("RecordedAt" AT TIME ZONE 'UTC') AS date)
            HAVING COUNT(*) > 1
            ORDER BY 1, 2
            """;

        public static void EnsureNoCollisions(DbContext context)
        {
            if (!IsGuardedMigrationPending(context))
            {
                return;
            }

            var collisions = FindCollisions(context);
            if (collisions.Count == 0)
            {
                return;
            }

            throw new InvalidOperationException(
                "Cannot apply the AddDeliveryMetricSnapshotRecordedDay migration: "
                + $"{collisions.Count} delivery/day combination(s) in DeliveryMetricSnapshots have more than one row "
                + "recorded on the same calendar day, so the unique (DeliveryId, RecordedDay) index cannot be created. "
                + "Colliding rows: " + string.Join("; ", collisions) + ". "
                + "Lighthouse deliberately does NOT de-duplicate them - rewriting recorded history silently is worse "
                + "than refusing to start. Remove or merge the extra rows by hand, then restart Lighthouse.");
        }

        private static bool IsGuardedMigrationPending(DbContext context)
        {
            var applied = context.Database.GetAppliedMigrations().ToList();

            // Nothing to collide with until the table itself exists.
            if (!applied.Exists(migration => migration.EndsWith(TableCreatingMigrationSuffix, StringComparison.Ordinal)))
            {
                return false;
            }

            return context.Database
                .GetPendingMigrations()
                .Any(migration => migration.EndsWith(GuardedMigrationSuffix, StringComparison.Ordinal));
        }

        private static List<string> FindCollisions(DbContext context)
        {
            var collisions = new List<string>();
            var connection = context.Database.GetDbConnection();
            var openedHere = connection.State != System.Data.ConnectionState.Open;

            if (openedHere)
            {
                connection.Open();
            }

            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = context.Database.IsNpgsql() ? PostgresCollisionSql : SqliteCollisionSql;

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    collisions.Add(
                        $"DeliveryId {reader.GetInt32(0)} on {DayOf(reader.GetValue(1))} ({reader.GetInt64(2)} rows)");
                }
            }
            finally
            {
                if (openedHere)
                {
                    connection.Close();
                }
            }

            return collisions;
        }

        /// <summary>
        /// Postgres groups to a date, SQLite to ISO-8601 text; both render as yyyy-MM-dd so the
        /// operator sees one shape whichever provider they run.
        /// </summary>
        private static string DayOf(object value)
        {
            return value switch
            {
                DateOnly day => day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                DateTime instant => instant.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty,
            };
        }
    }
}
