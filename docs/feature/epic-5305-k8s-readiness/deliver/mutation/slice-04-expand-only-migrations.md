# Mutation testing — slice-04 expand-only migrations + concurrent-startup coordination

**Story**: US-04 (ADO #5308) · **Date**: 2026-06-20 · **Tool**: Stryker.NET 4.14.2
**Config**: `Lighthouse.Backend.Tests/stryker-config.epic-5305-slice-04-migrations.json`

## Scope

The story's production surface is the advisory-lock migration coordination added to
`Data/DatabaseConfigurator.cs` (`ApplyMigrations` provider branch, `MigrateUnderAdvisoryLock`,
`ExecuteNonQuery`; lines 87–143). The expand-only guard (Part A) is test-project code and is
therefore not a mutation target.

The whole-file Stryker score (37%) is dominated by pre-existing, separately-untested config
methods (`AddDbContext` SQLite block, `AddDatabaseConfiguration`, `RegisterDatabaseManagementProvider`)
which this slice did not touch. The relevant figure is the new surface.

## Result — new surface (lines 87–143)

**12 killed / 2 survived = 85.7%** (≥ 80% gate met).

Killing tests (`Integration/Containers/ConcurrentStartupMigrationTests.cs`, real Postgres via
Testcontainers + a SQLite path):

- `IsNpgsql()` branch, advisory-lock acquire/release, the migrate call, and the open/close
  connection lifecycle are killed by the three concurrent-startup tests plus
  `Startup_PostgresProvider_ClosesTheMigrationConnectionItOpened` (asserts the dedicated
  migration connection is `Closed` afterwards → kills the close-removal and `openedByUs`-negate
  mutants).
- The non-Postgres `else` branch (`context.Database.Migrate()`) is killed by
  `Startup_NonPostgresProvider_AppliesMigrationsViaPlainMigrate` (SQLite).

## Surviving mutants — justified equivalent

Both survivors are `logger.LogInformation("Migrating Database")` (statement removal + log-string
emptying). Logging is a side effect we deliberately do not assert (asserting log text is brittle
and discouraged by the project's testing conventions), so these are equivalent mutants.
