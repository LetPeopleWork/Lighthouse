using Lighthouse.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Lighthouse.Backend.Tests.TestHelpers
{
    /// <summary>
    /// Migration fixtures roll a database back to a HISTORICAL migration and then seed it through the
    /// CURRENT EF model. That combination has a standing trap: every column a later migration adds to a
    /// table one of those seeds touches is a column the historical schema does not have, and the insert
    /// dies with <c>table X has no column named Y</c> - in a fixture that has nothing to do with the
    /// change that added it.
    ///
    /// Adding the column for the duration of the seed and dropping it again before migrating forward
    /// keeps the historical schema honest for the migration actually under test, while letting the seed
    /// through. <c>BlockedRuleSetMigrationTests.EnsureLegacyColumnsHaveWorkingDefaultAsync</c> already
    /// does the same thing for the opposite case (a column the model has stopped declaring).
    ///
    /// Epic #5687 slice 05 is what surfaced it: <c>FetchFingerprint</c> is the first column added to
    /// Teams or Portfolios since either fixture's pinned migration. Any future column on a table these
    /// fixtures seed has to be listed below - that is the price of seeding through the model rather than
    /// through raw SQL, and it is cheaper than hand-writing an insert for thirty NOT NULL columns across
    /// two providers.
    /// </summary>
    public static class HistoricalSchemaPatch
    {
        /// <param name="AddedByMigration">
        /// Suffix of the migration that adds the column. The patch is applied only while that migration
        /// is still pending, so a fixture pinned AFTER it is left alone.
        /// </param>
        private sealed record LaterColumn(string Table, string Column, string Type, string AddedByMigration);

        private static readonly LaterColumn[] ColumnsAddedAfterTheseFixturesPinnedTheirSchema =
        [
            new("Teams", "FetchFingerprint", "TEXT", "AddFetchFingerprintToQueryOwners"),
            new("Portfolios", "FetchFingerprint", "TEXT", "AddFetchFingerprintToQueryOwners"),
            new("Portfolios", "DependencyOverrideAdditionalFieldDefinitionId", "INTEGER", "AddPortfolioDependencySettings"),
            new("Portfolios", "IgnoreDependencies", "boolean", "AddPortfolioDependencySettings"),
            new("Deliveries", "ArchivedOn", "timestamp with time zone", "AddDeliveryArchiveAndClosureRecord"),
        ];

        /// <summary>Call right after rolling back, before seeding through the EF model.</summary>
        public static Task AddColumnsTheCurrentModelExpectsAsync(LighthouseAppContext context)
            => ApplyAsync(context, column => "ALTER TABLE \"" + column.Table + "\" ADD COLUMN \"" + column.Column + "\" " + column.Type + " NULL");

        /// <summary>Call after seeding, before migrating forward - or the real migration re-adds the column and fails.</summary>
        public static Task RemoveColumnsTheCurrentModelExpectsAsync(LighthouseAppContext context)
            => ApplyAsync(context, column => "ALTER TABLE \"" + column.Table + "\" DROP COLUMN \"" + column.Column + "\"");

        // EF1002/EF1003 flag non-constant SQL text as an injection risk. Every fragment here comes from
        // the fixed table above, never from external input, and the statements are DDL with no values.
#pragma warning disable EF1002, EF1003
        private static async Task ApplyAsync(LighthouseAppContext context, Func<LaterColumn, string> statement)
        {
            var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();

            foreach (var column in ColumnsAddedAfterTheseFixturesPinnedTheirSchema
                .Where(column => pending.Exists(migration => migration.EndsWith(column.AddedByMigration, StringComparison.Ordinal))))
            {
                await context.Database.ExecuteSqlRawAsync(statement(column));
            }
        }
#pragma warning restore EF1002, EF1003
    }
}
