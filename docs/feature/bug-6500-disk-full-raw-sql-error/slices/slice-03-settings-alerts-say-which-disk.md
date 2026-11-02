# Slice 03: Settings alerts say which disk is full

Story US-6500-01 · job `job-operator-read-the-warnings-without-reading-the-log` · estimate about 1.25 days

## Goal

A failed backup, restore, clear or licence import shows a sentence that names the cause and the right place to fix
it, with no path, tool output or driver text; the anonymous startup health check stops echoing exception text.

## IN scope

- `DatabaseManagementService.cs:81,131,184`: the failure reason becomes a chosen sentence.
  - A database fault uses the classifier sentence.
  - The "Lighthouse host" disk sentence is chosen only from Lighthouse's own `IOException` for lack of space: the
    manifest write (`:63`), the zip creation (`:65`), the restore's upload copy and unzip (`:143,:150`) and the SQLite
    provider's file copy into the temp directory (`SqliteDatabaseManagementProvider.cs:40-48`).
  - `pg_dump`/`pg_restore` stderr never selects a location. It goes to the log only; a failed tool gets "The backup
    tool failed: the log has the details." and names no disk.
  - No sentence says "Lighthouse tries again by itself": backup, restore and clear are single attempts.
- `DatabaseManagementController.cs:37,52,78,82,96`: audit that each catch is domain-typed; reword any broad one. The audit result is "keep": the service swallows every
  exception into the tracker (`DatabaseManagementService.cs:78,128,181`), so a `DatabaseOperationException` never
  reaches the controller's `catch (InvalidOperationException)`; a test pins that. All five catches render `ex.Message`,
  so the ratchet entry for the file is exactly 5.
- `DatabaseOperationException.Message` is the `OperatorSentence`; the tool stderr or zip entry name goes in its own
  `LogDetail` property and the log only.
- `LicenseController.cs:65,80`: fixed sentences; database faults use the classifier (no retry clause); the "invalid
  file" answer is unchanged.
- `Health/ClusterSubstrateHealthCheck.cs:54`: `/health/startup` is `AllowAnonymous` (`Program.cs:1112-1115`); the
  unhealthy description becomes a fixed sentence ("the cluster substrate probe could not run
  against Postgres and Redis; the log has the details."), which `Refused` prefixes with `health.startup.refused:` itself (`ClusterSubstrateHealthCheck.cs:58-59`), and the
  exception goes to the log.
- Frontend: no code change expected (the page renders `failureReason`); copy check only.

## OUT scope

Page requests (slice 04), Recent problems (slice 01), docs prose (finalize).

## Learning hypothesis

Disproved if one sentence per kind is enough, i.e. the host-versus-server-versus-tool distinction turns out to be
needless.

## Acceptance criteria

- [ ] A backup whose temporary files do not fit on the Lighthouse host says so and names the host, not the database.
- [ ] A backup whose `pg_dump` fails says the backup tool failed and the log has the details; it names no disk and
      no path, whatever `pg_dump` printed.
- [ ] A restore or clear that meets a database fault shows the classified sentence, without "tries again".
- [ ] No Settings alert for backup, restore, clear or licence import contains "tries again by itself".
- [ ] No alert contains a file path, SqlState or tool stderr.
- [ ] A `DatabaseOperationException` never lets its log detail reach a message, and never escapes the service into
      the controller's catch.
- [ ] A malformed licence file still gets a useful "invalid licence file" answer.
- [ ] `GET /health/startup` with a failing probe returns the fixed sentence and none of the exception's text, and
      the exception is in the log.

## Dependencies

Slice 01 (the classifier).

## Reference class

Three adjacent copy-only call sites plus one health line, about a day with the audit.
