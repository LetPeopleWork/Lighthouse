# Slice 01: Recent problems names the database fault

Story US-6500-01 · job `job-operator-read-the-warnings-without-reading-the-log` · estimate about 2 days (DISCUSS said 1.5; +0.5 after the DESIGN review for dedicated-role Postgres tests, the class-42 rule and the ratchet; the
former slice 02 is merged in, see AFK decision A2 in `feature-delta.md`)

## Goal

When the refresh loop, a forecast, the update queue or a write-back flush hits a database fault, the Task Manager
shows one plain sentence that names fix and cause, and the complete exception stays in the log only.

## IN scope

- The classifier: Postgres SqlState and `SqliteErrorCode` to a fault kind plus sentence, all eight classes (table
  in the delta), looking through wrapped exceptions; a safe default sentence for anything unrecognised.
- `RecentProblemsSink` replaces the message of any Recent problems entry whose attached exception is a database
  fault. Unconditional (N2): it is what makes the fixed-text sites below, and every future one, name the cause.
- Call sites changed, each logging the exception through `LogError(exception, "fixed text")` and never rendering it
  into the message: `UpdateServiceBase.cs:295`, `ForecastUpdater.cs:368` and `UpdateQueueService.cs:563` only.
  Unchanged, the sink covers them (DESIGN F3): `UpdateServiceBase.cs:71`, `ForecastUpdater.cs:276`,
  `UpdateQueueService.cs:437` and `WriteBackTriggerService.cs:57,100,190` (tracker text stays byte-identical).
- Log hygiene only, no user-visible effect: `WriteBackService.cs:65` stops copying `ex.Message` into the item
  results, which are read only by a Debug log line.
- A test per fault class and provider; a guard test per call site that a thrown `PostgresException` never puts
  `Exception data:`, `SqlState` or `pgsql_tmp` into a Recent problems message.

## OUT scope

Settings alerts and the health text (slice 03), page requests (slice 04), retry pacing (separate Bug), old entries
(Q6), any frontend change beyond a clamp check.

## Learning hypothesis

Disproved if a call site needs provider-specific code to get a usable sentence, or if write-back cannot keep tracker
text while rewording only database faults. Confirmed if one classifier call is enough for both Npgsql and SQLite
and the sink alone already names the cause at a fixed-text site.

## Acceptance criteria

- [ ] All eight classes produce their own sentence on Postgres, and on SQLite where they apply: out of space,
      out of memory, connection limit, read-only, unreachable, sign-in refused, locked, damaged.
- [ ] Sign-in refused (`28P01`, `28000`) says PostgreSQL refused the sign-in and to check the user and password;
      `42501` says a permission is missing and to grant it; it is not treated as a query error although it sits in
      Postgres class 42 (the rule is "class 42 except `42501`", pinned as a counter-example).
- [ ] Locked (`55P03`; SQLite 5 and 6) says a lock was held too long; on SQLite, to run only one Lighthouse on the
      data folder.
- [ ] Damaged (`XX001`, `XX002`; SQLite 11 and 26) says to restore from a backup.
- [ ] SqlState 53100 shows "Free disk space on the PostgreSQL volume: the database has run out of space." in Recent
      problems, with no driver text.
- [ ] SQLite error 13 shows the SQLite variant; SQLite error 10 (`IOERR`) is not read as disk-full and shows the
      default sentence.
- [ ] A fixed-text log event ("Failed to refresh Portfolio Northwind Platform") with a `53100` exception attached
      shows the classified sentence; the same text with a non-database exception is unchanged.
- [ ] An unrecognised database exception shows "Open the full log: Lighthouse could not use its database."
- [ ] The core of every sentence is at most 80 characters, with the fix first (string-length assertion over the
      table); "Lighthouse tries again by itself" follows it, on background sites only.
- [ ] A Jira "401 Unauthorized" at each write-back site is byte-identical to today.
- [ ] The log file still holds the full exception at every site above.

## Dependencies

None. Real-instance Postgres tests on `SharedPostgresContainer` each create their own role and database and never
`ALTER` the shared role or terminate another session; codes that cannot be provoked that way use constructed
exceptions. Production-data check: a real `PostgresException` from a size-limited Postgres volume and a real
`SqliteException` 13, not only constructed ones.

## Reference class

Sibling slices of Epic 5511 (Task Manager): one backend seam plus mechanical call-site changes behind it, about a
day and a half, raised to about two with the review items.
