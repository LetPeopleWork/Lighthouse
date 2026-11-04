# Bug 6500: a database failure reaches the user as raw exception text

Root-cause analysis: `rca.md` in this folder.

## Maintainer decisions (2026-11-02, after the RCA)

| # | Question | Decision |
|---|---|---|
| Q2 | How wide is the fix | **Every database fault class, at every site that shows exception text to a person.** One classifier turns a database failure (disk full, out of memory, connections, read-only, unreachable) into a fixed sentence. Tracker (connector) error text stays as it is. |
| Q3 | Wording | **One plain sentence; the full exception goes to the log file only.** No raw "Details" block in the UI. |
| Q4 | The unpaced retry loop in `TryUpdating` | **Separate Bug**, not this one (to be created after confirmation). |
| Q5 | Early warning before the disk is full | **Skip.** No capacity check, no Story. |
| Q1 | Which surface the user saw | **Task Manager → Recent problems** (confirmed by the maintainer, who was in the session). From there the user went to the logs, which worked, but they had to browse a lot to find out what was wrong. So the plain sentence must name the cause and the fix directly (e.g. "the database ran out of disk space; free space on its volume"), so nobody has to dig through the log to learn it. |
| Q6 | Recent problems keeps old entries until restart | **Leave as is.** New entries carry the plain sentence after the fix. |
| Q7 | Information disclosure (SQL text, stack traces) | **Part of this Bug**, no advisory; mention it in the release notes at `/release`. |
| Q8 | Probe the HTTP 500 path before DESIGN | **Yes.** One local request against a Production-mode build with a forced database failure, to see whether the body is empty, the SPA shell, or carries text. Result recorded in `probe-http-500.md` in this folder. |

## Wave: DISCUSS / [REF] Scope, persona, job

Mode: PROPOSE, except that the decisions tables (Q1 to Q8, N1, N2) and the AFK decisions at the end are binding.
Feature type: user-facing bug fix (backend classifier plus fixed sentences at the sites that show text; one small
backend handler and one frontend mapping for ordinary page requests).
Walking skeleton: N/A, because the path (log event -> `RecentProblemsSink` -> `LogsController` -> Task Manager)
already exists end to end; slice 01 is the thin vertical cut through it. Journey artifacts (visual/YAML):
N/A, because it is one glance at an existing surface with a two-step arc (alarmed by gibberish -> told what to
free and where); the sketches carry it. Gherkin: embedded in the story below.

### Scope Assessment: PASS

1 story in 3 slices (slice 02 was merged into 01, A2), 3 bounded contexts touched only at call sites (Work Items
refresh pipeline, Licensing, Platform lane), no new integration points, estimated 3.75 to 4.25 days in total (slice 01
about 1.5 days, slice 03 about 1.25, slice 04 0.5 to 1). One oversize signal at most (several call sites), so not
oversized. The retry loop (Q4) and early warning (Q5) are out.

### Persona and job

- **Persona**: Jonas Brandt, a System Administrator who self-hosts Lighthouse and PostgreSQL in Docker on a
  40 GB VPS for about twelve Teams. He glances at the Task Manager (header) when someone says the data looks
  old. He is not a database specialist; he knows how to free disk space once told which volume.
- **job_id**: `job-operator-read-the-warnings-without-reading-the-log` (existing, from Epic 5511 Task Manager;
  one-liner: "When something has gone wrong repeatedly, I want the warnings and errors surfaced where I already
  look, so noticing does not depend on me choosing to read a log file."). Fit: this Bug is the same promise
  one level down. Epic 5511 put the problem on the Task Manager; it did not make the *text* readable, and the
  Q1 evidence is exactly the broken promise: the user had to browse the logs a lot to learn the cause.
  **No new job proposed**: the job file has no database-specific job and none is needed, because the operator's
  goal (know what is wrong without digging) is unchanged. The id was verified in `docs/product/jobs.yaml` at line
  6491 (`- id: job-operator-read-the-warnings-without-reading-the-log`). Proposed `jobs.yaml` edit (DELIVER, not
  DISCUSS): a dated `REVISED` line in that job's note: "the surfaced warning must also say the cause and the fix; a
  database fault is worded, never rendered from the exception".
- **JTBD one-liner**: When the database fails (disk full, out of memory, too many connections, read-only,
  unreachable), I want the place I already look to say what broke and what I must do, in one plain sentence,
  so I fix the volume or the server in minutes instead of reading logs.

## Wave: DISCUSS / [REF] Locked decisions

Q1 to Q8 above are binding. Derived decisions for DESIGN to honour:

- **D1** (Q2): one classifier turns any caught exception into a `DatabaseFault` (a kind plus a fixed sentence),
  or "not a database fault". It looks through wrapped exceptions (`DbUpdateException`, EF's retry wrapper,
  `AggregateException`), because the Postgres exception is rarely the outermost one.
- **D2** (Q3, N2): the exception goes to the log through `LogError(exception, "fixed text")`, which keeps it in the
  file only, and the text a person reads is never rendered from it. Two layers choose that text: a call site that
  knows more than the classifier (a Settings alert) picks its own sentence, and `RecentProblemsSink` rewrites the
  message of any Recent problems entry whose attached exception is a database fault, unconditionally. The sink is
  the safety net that also names the cause at fixed-text sites (for example "Failed to refresh Portfolio X") and at
  every future site. No "Details" block in the UI.
- **D3** (Q2, Q3): unknown exceptions get a safe fixed sentence, never their text. Tracker (connector) text is
  untouched: a site that mixes both (write-back) classifies first, and only database faults are reworded.
- **D4** (Q1, M6): every sentence puts the fix first and the cause second, and its core is at most 80 characters,
  because the Task Manager row is clamped to two lines (`RecentProblemsSection.tsx`, `WebkitLineClamp: 2`); the
  derivation is under the fault-class table. Anything optional ("Lighthouse tries again by itself", only where true)
  goes last, where the clamp may cut it.
- **D5** (Q6): old Recent problems entries stay until restart. New ones carry the sentence. No migration.
- **D6** (Q4): until the separate Bug lands, an outage still produces one entry per pass, so the five visible
  rows will repeat the same sentence. Accepted and said in the release note draft, not fixed here.
- **D7** (Q7): disclosure is part of this Bug: after it, no SQL fragment, table name, file path or stack reaches
  a browser from the sites below. Release notes mention it at `/release`; no advisory.
- **D8** (Q8 and probe): the "ordinary `/api` request hits a failure" surface is slice 04 and it is a fix, not a
  confirmation: the probe found that `UseExceptionHandler("/Error")` (`Program.cs:173`) points at a route that does
  not exist, so a GET answers 500 with the SPA shell and a POST/PUT answers 500 with an empty body, and the
  frontend copies either into the error message. The handler is replaced and the frontend maps any 5xx with an
  HTML or empty body to a plain sentence (see the slice brief).

## Wave: DISCUSS / [REF] Fault classes and sentences (binding: N1)

Detection is by Postgres SqlState and by `SqliteException.SqliteErrorCode` (the driver's code is already the
primary one). All eight classes are binding: the first five are Q2's list, the last three were added by N1. Each
sentence names fix and cause, fix first (M6), in the "core" column; the optional tail is in the next section.

| Class | Postgres detected by | SQLite detected by | Core sentence, Postgres | Core sentence, SQLite |
|---|---|---|---|---|
| Out of space | `53100` | 13 (`SQLITE_FULL`) only | "Free disk space on the PostgreSQL volume: the database has run out of space." (76) | "Free disk space where Lighthouse keeps its data: the database is full." (70) |
| Out of memory | `53200` | 7 (NOMEM) | "Give the PostgreSQL server more memory: the database ran out of memory." (71) | "Give the Lighthouse host more memory: it ran out reading its database." (70) |
| Connection limit | `53300` | n/a | "Raise max_connections in PostgreSQL: the database refuses new connections." (74) | n/a |
| Read-only | `25006` | 8 (READONLY) | "Connect to the primary PostgreSQL server: this one is read-only." (64) | "Make Lighthouse's data folder writable: its database file is read-only." (71) |
| Unreachable | `08xxx`, `57P01`, `57P03`, `3D000`, connection-refused/timeouts from `NpgsqlException` | 14 (CANTOPEN) | "Check the server and connection string: Lighthouse cannot reach PostgreSQL." (75) | "Check the data folder exists and is readable: the database cannot open." (71) |
| Sign-in refused | `28P01`, `28000` | n/a | "Check the user and password: PostgreSQL refused Lighthouse's sign-in." (69) | n/a |
| (same row) permission refused | `42501` (insufficient_privilege: a missing grant, not a sign-in failure) | n/a | "Grant Lighthouse's PostgreSQL user the missing permission: access was refused." (78) | n/a |
| Locked | `55P03` (lock timeout) | 5 (BUSY), 6 (LOCKED) | "Stop whatever else is locking the database: a lock was held too long." (69) | "Run only one Lighthouse on this data folder: the database file is locked." (73) |
| Damaged | `XX001`, `XX002` | 11 (CORRUPT), 26 (NOTADB) | "Restore from a backup: PostgreSQL reports a damaged database." (61) | "Restore from a backup: the database file looks damaged." (55) |
| Anything else (default) | any other `PostgresException` or `DbException` | any other, including 10 (IOERR) | "Open the full log: Lighthouse could not use its database." (57) | same |

Note (added at DEVOPS review; the table is binding and untouched): "any other `PostgresException` or `DbException`" in the default row is narrowed by DESIGN DD-1, "class 42 except 42501": query and data errors (Postgres classes 22, 23, 42 except `42501`; SQLite 1, 19, 20, 21, 25) are not database faults and keep their authored text (AFK-D2).

The number in brackets is the sentence's length in characters. SQLite error 10 (`SQLITE_IOERR`) is deliberately
not read as disk-full: it covers many I/O failures and no extended code is reliable enough to say "disk" (M5); it
falls to the default sentence.

Optional tail, shown after the core, where the clamp may cut it (D4). Background sites (Recent problems) only:
" Lighthouse tries again by itself." on Postgres out of space, out of memory, unreachable, locked, and on SQLite
out of space. Settings sites (backup, restore, clear, licence import) never carry it: those are single attempts and
the clause would be false (M4b). Explanatory tails, any caller: read-only on Postgres adds " (a standby server,
default_transaction_read_only on, or a read-only transaction)"; locked on SQLite adds " Make sure no backup tool
holds the file."; damaged adds " The log has the details."; read-only on SQLite adds " Check the folder permissions
and that its volume is not mounted read-only."

**The 80-character rule (M6; for DISTILL).** The core sentence of every class, on both providers, is at most 80
characters, counted from its first letter; DISTILL pins this as a string-length assertion over the table (jsdom
cannot measure pixels). Derivation: the popover paper is `minWidth: 320, maxWidth: 520` with `p: 2`
(`TaskManagerIcon.tsx:99`), so a row has at most 520 - 32 = 488 px. The row is `body2` (14 px) in Quicksand
(`appThemes.ts:21`), whose average glyph is about 0.55 em = 7.7 px (an estimate, to be eyeballed once in a browser at
520 px), so about 63 characters a line, 126 over the two lines of the clamp (`RecentProblemsSection.tsx:70-75`).
Subtract the prefix `<date, time> — <level> — ` (up to 31 characters with "Warning") and about 16 for word-wrap
loss (one short word a line): 126 - 31 - 16 = 79, rounded to 80. The longest core above is 78.

Three nuances for DESIGN. (1) The kind is not the whole answer: the same "out of space" has a different fix
depending on *where* it happened, and the sentence is chosen by the place, never by tool text. (a) The "Lighthouse
host" disk sentence is chosen only from Lighthouse's own `IOException`: the manifest write
(`DatabaseManagementService.cs:63`), the zip creation (`:65`), the restore's upload copy and unzip (`:143,:150`)
and the SQLite provider's file copy into the temp directory (`SqliteDatabaseManagementProvider.cs:40-48`). Its
wording: "The backup ran out of disk space on the Lighthouse host: free space on the volume that holds its temporary
folder, then try again." (b) Tool stderr (`pg_dump`, `pg_restore`) never selects a location: a failed tool gets "The
backup tool failed: the log has the details." and names no disk, even when the real cause is the host's temp disk.
(c) A database fault inside backup, restore or clear uses the class sentence above, without the retry tail.
(2) Transient concurrency faults (`40001`, `40P01`) are not a class: the existing `ConcurrencyConflictExceptionFilter`
and EF's retry own them; they fall to "anything else" only if they escape.

## Wave: DISCUSS / [REF] Sites that show exception text today, and the slice that covers each

From `rca.md` section 5, re-read against the code on this branch. Each row is one call site, numbered so the KPI
baseline (15 call sites) can be checked against it.

| # | Site | Reader | Text today | Slice |
|---|---|---|---|---|
| 1 | `UpdateServiceBase.cs:295` (`TryUpdating` catch) | Admin: Task Manager, log view | whole exception, `Exception data:` block | **01** |
| 2 | `UpdateServiceBase.cs:71` (write-back flush) | Admin: Task Manager, log view | `exception.Message` | **01** |
| 3 | `ForecastUpdater.cs:276` | Admin: Task Manager | `exception.Message` | **01** |
| 4 | `ForecastUpdater.cs:368` | Admin: Task Manager | `exception.Message` | **01** |
| 5 | `UpdateQueueService.cs:437` | Admin: Task Manager | `exception.Message` | **01** |
| 6 | `UpdateQueueService.cs:563` | Admin: Task Manager | `exception.Message` | **01** |
| 7 | `WriteBackTriggerService.cs:57` | Admin: Task Manager | `ex.Message` (tracker text by intent) | **01** (database faults reworded, tracker text stays) |
| 8 | `WriteBackTriggerService.cs:100` | Admin: Task Manager | `ex.Message` (same) | **01** |
| 9 | `WriteBackTriggerService.cs:190` | Admin: Task Manager | `ex.Message` (same) | **01** |
| 10 | `DatabaseManagementService.cs:81` (backup) | Backup caller, Settings alert | `ex.Message`, `pg_dump` stderr, `IOException` | **03** |
| 11 | `DatabaseManagementService.cs:131` (restore) | Restore caller, Settings alert | same | **03** |
| 12 | `DatabaseManagementService.cs:184` (clear) | Clear caller, Settings alert | same | **03** |
| 13 | `LicenseController.cs:65` | Caller, Settings licence alert | `$"Error processing license file: {ex.Message}"` | **03** |
| 14 | `LicenseController.cs:80` | Caller, Settings licence alert | same shape | **03** |
| 15 | `Health/ClusterSubstrateHealthCheck.cs:54` (`/health/startup`, `AllowAnonymous`, `Program.cs:1112-1115`) | **Anyone, unauthenticated** | `Refused($"... {ex.Message}")` | **03**: a fixed sentence, the exception logged |

Not call-site rows, but covered:

| Site | Reader | Text today | Slice |
|---|---|---|---|
| `RecentProblemsSink` message for any event whose attached exception is a database fault (N2); this also covers the fixed-text sites `UpdateQueueService.cs:631,761,810` ("Error processing update task ...") | Admin: Task Manager | whatever the call site rendered | **01**, unconditional |
| `WriteBackService.cs:65` (`ErrorMessage = ex.Message` in the item results) | nobody: read only by a Debug log line (`:75-77`), below the Recent problems threshold, and not returned by any controller | `ex.Message` | **01** as log hygiene only (A2); no user-visible effect, so no row above |
| `DatabaseManagementController.cs:37,52,78,82,96` | Caller | said to be controlled domain text | **03** (audit: confirm each catch is domain-typed; reword any that is broad) |
| Ordinary `/api` request that throws (`UseExceptionHandler("/Error")`, `Program.cs:173`, points at a route that does not exist) | Any caller | GET: 500 with the SPA `index.html`; POST/PUT: 500 with an empty body; the frontend copies either into the error message (`probe-http-500.md`) | **04** |
| Log view (`LogsController.GetLogs`) | System Administrator | full exceptions by design | **not changed**; raw text there is the operator surface |
| Connector `ex.Message` in refresh reasons, `Blackout*` controllers with domain text | various | tracker or domain wording | **out of scope** (Q2, RCA section 5) |

## Wave: DISCUSS / [REF] Context map, glossary, events, UI surface

- **Contexts touched (ADR-270)**: **Platform lane** (not a context; composition-root infrastructure): the
  classifier, logging and backup/restore live here. **Work Items** (refresh pipeline) and **Licensing**: call
  sites only, they stop rendering exceptions. No other context. Organisation set-up, Access, Delivery,
  Tracker integration: not touched.
- **Edges**: no new edge on the context map. One DESIGN question: contexts must reach the classifier through a
  port that lives where they may depend on it (shared kernel or an interface they already see), not by
  referencing a Platform class. Same shape as ADR-126's refusal channel for connectors.
- **Glossary**: no new Lighthouse user-facing term. One proposed ADR-270 glossary row, applied in DELIVER:
  **Database fault** = "a caught failure of Lighthouse's own store (full disk, out of memory, connection
  limit, read-only, unreachable) as a typed answer with a fixed sentence; code name `DatabaseFault`; distinct
  from a tracker failure (`WorkTrackingReadException`)". Not a new concept on the map, so the "target only"
  marks are unchanged. Proposed `ARCHITECTURE.md` section 4: one line under the Platform lane entry.
- **Domain events**: none published, none consumed. A fault is reported by the caller that hit it; making it a
  published event would only matter for the early-warning idea, which Q5 dropped.
- **UI surface (ADR-271)**: the Task Manager is the header popover (ADR-186), a cross-cutting
  System Administrator glance that ADR-271 does not name; the backup alert is Settings, System, Database
  management; the licence alert is Settings, System. No new surface, nothing moved. Step towards the target:
  none cheap here, said explicitly.
- **Error-handling ADRs found**: none. A search of `docs/product/architecture` for exception, ProblemDetails and
  RecentProblems finds no ADR on how a caught failure becomes human text. The closest are
  **ADR-185** (Recent problems sink stores `RenderMessage` plus the exception type name; this Bug adds the rule
  the ADR never stated: the message is chosen, not rendered from an exception), **ADR-126** (typed refusal
  channel from connectors; the model for "typed fault with a sentence"), **ADR-184** (verdict ladder; its rule
  "where the connector cannot tell the cause, report the weaker true state, never a guess" (Unreachable, never Authentication failed) governs the default sentence) and **ADR-186** (header sections). Proposed:
  DESIGN writes one short ADR "A caught database failure becomes a typed fault with a fixed sentence" and adds a
  dated note to ADR-185. The tests-assert-it guard from RCA section 8 (no `Exception data:` in a Recent problems
  message) is DESIGN's to place.

## Wave: DISCUSS / [REF] UI sketches (walk-through happens at DISTILL)

Sketch 1, Task Manager, Recent problems (existing `RecentProblemsSection`, same row, same clamp). Drawn at the real
text width of the popover (about 63 characters a line, derivation under the fault-class table):

```
BEFORE
+-------------------------------------------------------------------+
| Recent problems                                                   |
| 02 Nov 2026, 09:14 — Error — An exception occured:                |
| Npgsql.PostgresException (0x80004005): 53100: could not write...  |
| (clamped at 2 lines; hover = whole text incl. "Exception data:")  |
| Open the full log                                                 |
+-------------------------------------------------------------------+

AFTER
+-------------------------------------------------------------------+
| Recent problems                                                   |
| 02 Nov 2026, 09:14 — Error — Free disk space on the PostgreSQL    |
| volume: the database has run out of space.                        |
| Open the full log                                                 |
+-------------------------------------------------------------------+
```

The "after" row is 29 characters of prefix plus a 76-character sentence, inside the 126 characters of two lines.
Reuses the existing section; no new control. The hover title carries the same sentence, never the exception.

Sketch 2, Settings, Database management, failed backup (existing error `Alert`, `onClose` kept; not clamped):

```
BEFORE (illustrative)                               AFTER
+------------------------------------------+        +------------------------------------------+
| (x) Could not write to path '/tmp/light- |        | (x) The backup ran out of disk space on  |
|     house-db-9c1e4f2a7b/manifest.json': |        |     the Lighthouse host: free space on   |
|     No space left on device     [close]  |        |     the volume that holds its temporary  |
+------------------------------------------+        |     folder, then try again.     [close]  |
                                                    +------------------------------------------+
```

Which volume that is: the backup writes a directory `lighthouse-db-<guid>` and a zip
`lighthouse-backup-<guid>.zip`, both under .NET's temp path (`Path.GetTempPath()`,
`DatabaseManagementService.cs:252,274`), which is `/tmp` on Linux. In the Docker image `/tmp` is inside the
container, so the volume is the Docker host's disk that holds container storage, unless the operator mounts `/tmp`
from a volume. The sentence cannot know which, so it says "the volume that holds its temporary folder"; the
database-management docs page (finalize) spells out the Docker case. The alert never says "Lighthouse tries again
by itself": a backup is a single attempt.

Sketch 3, the licence import alert changes the same way, copy only, no retry clause ("The licence could not be
saved: free disk space on the PostgreSQL volume, the database has run out of space.").

Sketch 4 (slice 04), any page that fails to load or save during an outage: the existing top snackbar and the
existing validation banner, copy only:

```
BEFORE (GET)                      BEFORE (POST/PUT)             AFTER (both)
+----------------------------+    +-----------------------+     +--------------------------------------+
| (x) <!doctype html><html>  |    | (x)                   |     | (x) Free disk space on the           |
|     <head>... (whole page) |    |     (empty)           |     |     PostgreSQL volume: the database  |
+----------------------------+    +-----------------------+     |     has run out of space.            |
                                                                +--------------------------------------+
```

Slow-connection rule: no new screen fetches data. The Task Manager already renders nothing when its request
fails (never the reassuring empty state), the backup alert is driven by the operation poll as today, and slice 04
adds no screen: its failure look is the existing snackbar and validation banner, its loading look is unchanged.
DESIGN confirms nothing more is needed. Visualization skill: N/A, because nothing here charts data.

## Wave: DISCUSS / [REF] User stories

<!-- markdownlint-disable MD024 -->

### US-6500-01: A database failure is named in plain words where an administrator reads it

job_id: `job-operator-read-the-warnings-without-reading-the-log`

#### Problem

Jonas Brandt runs Lighthouse with PostgreSQL in Docker. The Postgres volume filled up on a Sunday. On Monday a
colleague said the forecasts were stale; Jonas opened Task Manager, Recent problems, and read a two-line wall
of `PostgresException`, `SqlState: 53100` and `pgsql_tmp194.0`. He could not tell what to do, so he opened the
log and browsed it for a long time before finding "No space left on device". The same raw text also reaches
the Settings alerts for backup, restore and clear, and the licence import.

#### Elevator Pitch

Before: When the database fails, Jonas gets driver text he cannot act on and has to search the logs to learn
the cause.
After: open the Task Manager (header) -> Recent problems -> sees "Free disk space on the PostgreSQL volume: the
database has run out of space.", and Settings -> Database management shows the matching sentence when a backup
fails, and a page that cannot load during an outage says so in a sentence instead of dumping HTML.
Decision enabled: free space on the named volume (or restart, resize, reconnect) now, instead of browsing logs.

#### Domain examples

1. **Disk full on Postgres (happy path)**: 09:14 on 2 Nov 2026 the refresh of Portfolio "Northwind Platform"
   dies with `53100`. Recent problems shows the out-of-space sentence. The log file still holds the whole
   exception.
2. **SQLite, data folder read-only**: Priya Nair self-hosts on SQLite; after a restore her volume mounted
   read-only (code 8). Recent problems says Lighthouse cannot write to its database file and names the folder
   permissions and the mount.
3. **Backup fills the Lighthouse host (boundary)**: Jonas presses Backup with 200 MB free; writing the backup's
   temporary files fails with an `IOException`. The alert says the *Lighthouse host* is out of space, not the
   database, and no path or `pg_dump` stderr appears. If instead `pg_dump` itself fails, the alert says only
   that the backup tool failed and points at the log; it names no disk.
4. **Unknown fault (error path)**: Postgres answers `XX000` internal error. Recent problems says Lighthouse
   could not use its database and points at the full log; the sentence has no SQL text.

#### UAT scenarios (BDD)

```gherkin
Scenario: A full database disk is named in Recent problems
  Given Jonas runs Lighthouse on PostgreSQL and the refresh of Portfolio "Northwind Platform" hits SqlState 53100
  When Jonas opens the Task Manager and reads Recent problems
  Then the newest row says the database ran out of disk space and which volume to free
  And the row contains no "SqlState", no "pgsql_tmp" and no stack text
  And the log file still holds the complete exception

Scenario Outline: Each database fault class reads as its own sentence
  Given the refresh of Portfolio "Northwind Platform" hits <fault>
  When Jonas reads Recent problems
  Then the row says <what is wrong> and <what to do>, and contains no driver text

  Examples:
    | fault | what is wrong | what to do |
    | out of memory (53200) | the database ran out of memory | give the PostgreSQL server more memory |
    | connection limit (53300) | new connections are refused | raise max_connections |
    | read-only (25006) | the database is read-only | connect to the primary server |
    | unreachable (08006) | Lighthouse cannot reach PostgreSQL | check the server and connection string |
    | sign-in refused (28P01) | PostgreSQL refused the sign-in | check the user and password |
    | permission refused (42501) | access was refused | grant the missing permission |
    | locked (55P03) | a lock was held too long | stop whatever else locks the database |
    | damaged (XX001) | PostgreSQL reports a damaged database | restore from a backup |

Scenario: SQLite faults are worded for SQLite
  Given Priya Nair runs Lighthouse on SQLite and the data volume is full (error 13)
  When she reads Recent problems
  Then the row names the Lighthouse data folder volume, not PostgreSQL

Scenario: An unrecognised database failure gets a safe sentence
  Given Postgres answers with an error code Lighthouse has no sentence for
  When Jonas reads Recent problems
  Then the row says Lighthouse could not use its database and to open the full log, with no driver text

Scenario: A failed backup names the right disk
  Given the Lighthouse host has no room to write the backup's temporary files
  When Jonas presses Backup in Settings, Database management
  Then the alert says the backup ran out of disk space on the Lighthouse host
  And it shows no file path and no tool output
  And it does not say Lighthouse tries again by itself

Scenario: A tracker error keeps the tracker's own words
  Given Jira refuses a refresh with "401 Unauthorized"
  When Jonas reads Recent problems
  Then the row shows the connector text exactly as before

Scenario: A fixed-text log line still names the database cause
  Given the refresh of Portfolio "Northwind Platform" logs "Failed to refresh Portfolio Northwind Platform" with a PostgresException 53100 attached
  When Jonas reads Recent problems
  Then the row shows the out-of-space sentence, not the fixed text
  And a "Failed to refresh" line with a non-database exception attached is shown unchanged
```

(Seven scenarios cover slices 01 and 03. Scenario 6 guards Q2's boundary. Slice 04's page-request scenarios live in
its own brief, `slices/slice-04-page-requests-conditional-on-the-probe.md`, whose file name predates the probe.)

#### Acceptance criteria

- [ ] All eight classes (out of space, out of memory, connection limit, read-only, unreachable, sign-in refused,
      locked, damaged), on Postgres and on SQLite where they apply, each produce their own sentence (Q2, N1).
- [ ] Sign-in refused: `28P01` and `28000` say PostgreSQL refused the sign-in and to check the user and password;
      `42501` says a permission is missing and to grant it.
- [ ] Locked: `55P03` (Postgres) and SQLite 5 and 6 say a lock was held too long, and on SQLite to run only one
      Lighthouse on the data folder.
- [ ] Damaged: `XX001`, `XX002` (Postgres) and SQLite 11 and 26 say to restore from a backup.
- [ ] SQLite out of space is detected by error 13 (`SQLITE_FULL`) only; error 10 (`IOERR`) is not read as disk-full
      and falls to the default sentence.
- [ ] A log event with a fixed text (for example "Failed to refresh Portfolio Northwind Platform") and a database
      exception attached shows the classified sentence in Recent problems (N2); an event without a database
      exception is unchanged.
- [ ] No site in the sites table puts a driver message, SqlState, SQL, table name, file path, tool stderr or
      stack into text a person reads (Q7), and a test asserts it per site.
- [ ] The cause-and-fix part of every Recent problems sentence is at most 80 characters, with the fix first
      (Q1; derivation under the fault-class table). Anything optional ("Lighthouse tries again by itself") follows it.
- [ ] "Lighthouse tries again by itself" appears only where that is true: background sites (Recent problems),
      never on a Settings alert for backup, restore, clear or licence import, which are single attempts.
- [ ] The complete exception is still in the log file at every site (Q3).
- [ ] Connector text is byte-identical to today (Q2).
- [ ] An unknown fault shows the safe sentence, never its text.

#### Outcome KPIs

- **Who**: self-hosting System Administrators who meet a database fault.
- **Does what**: names the cause from the Task Manager or the Settings alert without opening the log.
- **By how much**: 100% of the 15 call sites in the sites table, plus the sink and the `/api` handler, show a
  chosen sentence (0 of 15 call sites today); 0 raw `Exception data:` strings in any shown message.
- **Measured by**: automated tests per site (the guard test); the maintainer's next report of a full disk
  contains the cause in the user's own words. No telemetry (see checklist).
- **Baseline**: raw text at every listed site; the reporter needed log browsing to find the cause.

#### Technical notes

Dependencies: none on other Bugs; Q4's retry-pacing Bug is independent (D6). The classifier must tolerate
wrapped exceptions (D1). Existing tests asserting current wording (`RecentProblemsSinkTest`,
`LogsControllerTest`) will change. EF migrations: none. Slice order and briefs in `slices/`. The Postgres
`25006` read-only state has three possible causes (a standby server, `default_transaction_read_only` on, or a
read-only transaction), which is why its sentence says "primary server" first and lists the causes after the clamp.

## Wave: DISCUSS / [REF] Slices

Briefs in `slices/`. Order by learning leverage: the reported path first.

| Slice | Covers | Learning hypothesis (disproved if it fails) |
|---|---|---|
| 01 | classifier (eight classes), `RecentProblemsSink` rewording, and every background site: `UpdateServiceBase.cs:295`/`:71`, `ForecastUpdater.cs:276,368`, `UpdateQueueService.cs:437,563`, `WriteBackTriggerService.cs:57,100,190` (about 1.5 days) | One classifier over wrapped Npgsql and SQLite exceptions yields a usable sentence without the call site knowing the provider, and the sink alone already names the cause at fixed-text sites. Disproved if a call site needs provider-specific code, or if write-back cannot keep tracker text while rewording only database faults. |
| 02 | **dropped, merged into 01** (A2): after the sink rewrite its only effect was log hygiene | n/a |
| 03 | Settings alerts (backup/restore/clear, licence) and the anonymous `/health/startup` text (about 1.25 days) | The sentence depends on where the fault happened (Lighthouse host vs database server vs backup tool). Disproved if one sentence per kind is enough. |
| 04 | ordinary `/api` requests that throw: ProblemDetails handler plus one frontend mapping (0.5 to 1 day) | The probe showed the answer is HTML (GET) or empty (POST/PUT), not text. Disproved if the existing `{message}` mapping already shows a plain sentence for a 5xx with such a body (it does not: `BaseApiService.ts:75-77`). |

Taste tests: no slice adds 4+ components; only slice 01 adds an abstraction and ships first; every slice
disproves something; the production-data criterion is met by feeding slice 01 a real `PostgresException`
produced by a Postgres in a size-limited tmpfs volume and a real `SqliteException` 13 (not only constructed
exceptions); a dogfood moment: the maintainer's own dev instance.

## Wave: DISCUSS / [REF] Lighthouse checklist

| Item | Answer |
|---|---|
| RBAC impact | None added. The surfaces keep their guards: `LogsController` `SystemAdmin`, `DatabaseManagementController` `RbacGuard`, `LicenseController` `SystemAdminOrBootstrap`. No new permission, no API-key scope change; `useRbac()` untouched and no component fetches `my-summary`. The change narrows what those guarded callers are shown (Q7). Slice 04's handler gives every caller the same database-neutral sentence, with no role check (AFK-D5, superseding A7), and the anonymous `/health/startup` text becomes a fixed sentence (slice 03). |
| Lighthouse-Clients (CLI/MCP) versioning | **N/A, because** nothing a client reads changes. Searched `/storage/repos/lighthouse-clients` for `errorMessage`, `failureReason`, `RecentProblems`, `recent-problems`, `database` and `backup`: no client or MCP tool reads the Recent problems messages, the backup `failureReason` or the licence `BadRequest` text. The one shared seam is slice 04: for a non-409 error the client reports the fixed text `Request failed with status N.` and copies only an optional `code` and `title` from the body (`packages/client/src/index.ts:1839-1920`). A 500 that now carries a ProblemDetails `title` therefore shows up as the existing optional `problemTitle` field: additive, no new field, route, status or enum, so no changeset. |
| Website marketing surface | N/A, because this is a bug fix with no new capability or claim. Release note draft (at `/release`, not at finalize): customer pain first ("a full database disk now says so in plain words in the Task Manager, instead of showing database internals"), plus the disclosure line (Q7). ADO "Release Notes" tag: maintainer decides. |
| Docs prose (for DELIVER finalize) | **Yes, owed**: `docs/settings/taskmanager.md` ("Recent problems", line ~120) gets one sentence that a database fault reads as a plain sentence and the full detail is in the log; the database-management settings page is checked for a "backup failed" description. |
| Per-feature screenshots | **N/A, because** no screenshot can show the change: a database fault needs a failing database, which demo data cannot seed, and the Task Manager screenshot routes its rows through a fixture, so it keeps its fixture wording. No screenshot is regenerated; a copy check of the fixture is enough. |
| Demo data | **N/A, because** a failure path needs a failing database, not data. |
| Usage-data event (DEVOPS pre-answer) | Proposed "N/A, because the change reports a failure and adds nothing to be used; a name-only 'database fault shown' event would count outages, not use". DEVOPS confirms. |
| Terminology | Copy says "database", "volume", "Lighthouse"; it uses no configurable term (Team, Feature, Work Item). The refresh sentence names no heading word. |
| Context map and glossary | Answered above; proposed edits are the ADR-270 glossary row and one `ARCHITECTURE.md` section 4 line, applied in DELIVER. |
| Proposed shared-doc edits, applied in DELIVER | ADR-185 dated note (message is chosen, not rendered); new short ADR (database fault classifier); ADR-270 glossary row "Database fault"; `ARCHITECTURE.md` section 4 line; `jobs.yaml` REVISED note; `docs/settings/taskmanager.md`. |

## Wave: DISCUSS / [REF] Definition of Ready (8 items)

| # | Item | Evidence |
|---|---|---|
| 1 | Problem in domain language | Problem section: stale forecasts, raw `53100` text, browsing logs. |
| 2 | Persona | Jonas Brandt (Postgres, Docker) and Priya Nair (SQLite), System Administrators. |
| 3 | 3+ examples with real data | Four in the story, plus five in slice 04, with dates, portfolio name, codes. |
| 4 | UAT 3-7 | Seven scenarios in the story (slices 01 and 03); slice 04's five live in its own brief. |
| 5 | AC from UAT | One or more criteria per scenario, in the story and in each slice brief. |
| 6 | Right-sized | 3 slices (01 about 1.5 days, 03 about 1.25, 04 0.5 to 1); about 3.75 to 4.25 days in total. |
| 7 | Technical notes and dependencies | Technical notes above plus D1 to D8; probe result is in; Q4 Bug independent; N1 and N2 answered. |
| 8 | Outcome KPIs | Above; measured by tests and the next report, no telemetry. |

DoR passes for all three slices. Job traceability: real `job_id` (verified at `docs/product/jobs.yaml:6491`), not
`@infrastructure`, Elevator Pitch present. No per-wave reviewer dispatched beyond the DISCUSS peer review (revised
here); the consolidated review fires at the end of DISTILL.

## Wave: DISCUSS / [REF] Out of scope

The unpaced retry loop and the identical-error flood in Recent problems (Q4, follow-up below); any early warning
or capacity check, `/health/ready` including the database disk, Postgres `temp_file_limit` guidance (Q5);
clearing or expiring old Recent problems (Q6); connector text (Q2); the log view's raw text, a System
Administrator surface by design; the usage-data event.

## Follow-up Bug (Q4, to be created after confirmation)

Not created in ADO. Proposed, for the maintainer to approve:

- **Title**: A failing refresh loop retries with no pause and floods the log and Recent problems
- **Description 1**: When `UpdateAll()` throws in `UpdateServiceBase.TryUpdating`, the `Task.Delay` sits inside
  the `try` after it, so the loop runs again at once; SQLite has no `EnableRetryOnFailure` at all.
- **Description 2**: One database outage writes a full stack trace per pass to the daily log (no size cap) and
  fills the 200-entry Recent problems queue with identical entries, pushing out every other problem and growing
  the log on the disk that may already be full.
- **Description 3**: Expected: a hold-off between failed passes taken from the existing `RefreshSettings`
  (no new constant), and an identical error logged on change and then at intervals, not every pass.
- **Description 4**: Related: Bug 6500 (database faults worded in plain language), RCA `rca.md` WHY 3A; tests
  that depend on retry timing change.

## Wave: DISCUSS / Open questions

None open. N1 (the three classes beyond Q2's five) and N2 (the sink also rewords) were answered by the maintainer
in the table below and are folded into the fault-class table, D2 and slice 01.

## Maintainer decisions on the DISCUSS questions (2026-11-02)

| # | Decision |
|---|---|
| N1 | **All eight fault classes** are worded (the five from Q2 plus sign-in refused, locked, damaged); anything else gets the safe default sentence. |
| N2 | **Both**: each call site chooses its sentence, and `RecentProblemsSink` also rewords any event whose attached exception is a database fault. |

## AFK decisions (2026-11-02, orchestrator)

Taken by the orchestrator while the maintainer is away; binding for DESIGN until the maintainer overturns them.

| # | Decision | Why |
|---|---|---|
| A1 | Postgres `42501` (insufficient_privilege) keeps its place in the sign-in row but with its own "permission refused" sentence. | It is a missing grant, not a wrong password, and the fix differs; one row with two sentences keeps the table at the eight classes of N1. |
| A2 | Slice 02 is dropped and its work is merged into slice 01. | After the sink rewrite (N2) it has no user-visible effect left. `WriteBackResult.ErrorMessage` (`WriteBackService.cs:65`) is read only by a `LogDebug` line (`:75-77`), below the Warning threshold of Recent problems, and no controller or DTO returns it. What remains of slice 02 is log hygiene (templates stop rendering `exception.Message`), which is cheaper to do in the same pass as the two `UpdateServiceBase` sites. |
| A3 | `ClusterSubstrateHealthCheck.cs:54` is in scope, in slice 03. | `/health/startup` is `AllowAnonymous` (`Program.cs:1112-1115`) and echoes `ex.Message` (driver text) to anyone; it is the one unauthenticated leak and costs one fixed sentence. |
| A4 | Slice 04 stays in this Bug and is sized at 0.5 to 1 day. | The probe settled the input; it is two small seams (a backend handler, one frontend mapping) and leaving a 500 that dumps HTML into a snackbar would undo the point of the Bug on the read path. |
| A5 | The Definition of Ready gate is the 8-item form: technical notes and dependencies are one item, KPIs the eighth. | The reviewer asked for the 8-item gate; no content is dropped. |
| A6 | Cause-and-fix part of every Recent problems sentence is at most 80 characters (derivation under the fault-class table). | A measurable rule DISTILL can pin as a string-length assertion. |
| A7 (**superseded by AFK-D5**: every `/api` caller gets the same generic database sentence) | ~~On `/api`, only a signed-in caller gets the class sentence~~ for a database fault; an anonymous caller gets the safe generic sentence. | The class sentences name PostgreSQL and a fix; an anonymous caller must learn no database detail (D7, RBAC row). Flagged as my reading of slice 04's "a DB fault carries its class sentence". |
| A8 | Lighthouse-Clients and screenshots are answered "N/A, because ..." in the checklist, not left open. | Searched `/storage/repos/lighthouse-clients`; the screenshot suite uses fixtures (see checklist). |

Citation corrections applied in this file: `WriteBackTriggerService.cs:57/:100/:190` (the `ex.Message` arguments), `WriteBackService.cs:65` only (`:76` is a Debug line). Still stale in `rca.md`, left alone on purpose: the SQLite WAL pragma is `DatabaseConfigurator.cs:67`, and the Recent problems capacity constant is `RecentProblemsSink.cs:26`.

## Wave: DESIGN / [REF] Mode, inputs, reuse

Mode: PROPOSE (maintainer AFK). Binding inputs: Q1 to Q8, N1, N2, A1 to A8. Application scope; the brief
(`docs/product/architecture/brief.md`) is not edited here (outside this folder); the shared-doc edits are listed
under "DELIVER owes" below. ADR draft: `adr-draft-database-fault-is-a-typed-answer-with-a-fixed-sentence.md`.
C4 L1 and L2 are unchanged (no new container, no new external system, no new integration), so only the component
view below is drawn. Peer review: not dispatched from inside this run; the orchestrator dispatches
`nw-solution-architect-reviewer` on this section and the ADR draft.

Team and Conway: one maintainer plus agents; a single deployable; a modular monolith stays. No microservice, no
new project, no new package (Npgsql is referenced by `Lighthouse.Backend.csproj`; Microsoft.Data.Sqlite arrives transitively through EF Core's SQLite provider and is used directly by the detector, which compiles today). Slices 01 and 03 edit no `.csproj`: a csproj edit is on the shared-path list and would force the full live-connector run in CI (see DEVOPS), so the reference stays transitive; if a build rule ever demands an explicit one, it is added in slice 04's last step together with the `Program.cs` change.

### Reuse analysis (no new component without "no existing alternative")

| Need | Existing thing | Verdict | Contract shape / universe / assertion |
|---|---|---|---|
| Turn an exception into something a person reads | none (RCA WHY 4A) | **New**: `DatabaseFaults` namespace (classifier, detectors, sentence catalogue) | pure-function; universe: its arguments only; asserted by table tests, no mocks |
| Per-event rewording at the log pipeline | `RecentProblemsSink.AsProblem` (pure function of `LogEvent`) | **Extend** (one branch) | pure-function for `AsProblem`; the sink as a whole stays bounded-change (its queue under its lock); asserted by feeding a real `LogEvent` |
| Refresh-event detection | `RecentProblemsSink.TryReadProperty<UpdateType>` / `RefreshOf` | **Reuse** to decide the retry tail | pure |
| Exception to HTTP body | `ConcurrencyConflictExceptionFilter` pattern (ProblemDetails + `code` extension) | **Reuse the shape**; the handler is new because filters cannot see exceptions outside MVC (the API-key auth handler reads the database before any controller runs) | bounded-change: writes the response only; logging stays with `ExceptionHandlerMiddleware` (logged once) |
| Frontend error to message | `BaseApiService.parseApiErrorPayload` / `extractMessage` (already reads `message`, `errors`, `detail`) | **Extend**: one pre-check for an unreadable 5xx body | pure |
| Settings alert text | `tracker.TransitionToFailed(operationId, text)` | **Reuse** unchanged; only the text argument is chosen differently | n/a |
| Health text | `ClusterSubstrateHealthCheck.Refused(...)` | **Reuse**, add a logger | n/a |

## Wave: DESIGN / [REF] Decisions, options, recommendation

### DD-1 The classifier: place, API, unwrapping, provider detection (question 1)

Place. Options: (a) shared kernel `Models`; (b) Platform lane, beside `DatabaseManagement`, `Seeding`,
`DomainEvents`, as `Services.Implementation.DatabaseFaults`; (c) inside `Data/`. **Recommend (b).** (a) is closed by
ADR-270 decision 7 (the kernel may not reference a driver); (c) is where EF lives but contexts do not depend on `Data`
by convention. The Platform lane is below every context, so Work Items, Licensing and the API depend on it
downward, which the module rules already allow (the connectors depend downward on Platform/Persistence). Add
`DatabaseFaults` to the `PlatformPersistencePattern` in `ModuleBoundariesArchUnitTest`.

No port. DISCUSS asked for "a port where contexts may depend on it". The classifier is a pure function with no
effect to isolate, and the sink is constructed before dependency injection exists (ADR-185), so an injected port
would need a second construction path only to be mockable. The typed answer (`DatabaseFault`) is the contract the
contexts see; they never see a driver type. Rejected: `IDatabaseFaultClassifier` via DI.

API (contract only):

| Piece | Contract |
|---|---|
| `DatabaseFaultKind` | `OutOfSpace, OutOfMemory, ConnectionLimit, ReadOnly, Unreachable, SignInRefused, PermissionRefused, Locked, Damaged, Other` (the eight classes of N1, with A1's `PermissionRefused` beside `SignInRefused`, and `Other` as the default row) |
| `DatabaseStore` | `PostgreSql, Sqlite, Unknown` (the sentence names the store; `Unknown` is a bare `DbException` and gets the provider-neutral default) |
| `DatabaseFault` | immutable (Kind, Store); no exception, no text |
| `DatabaseFaultClassifier.Classify(Exception?)` | returns `DatabaseFault?`; `null` = not a database fault; never throws; no logging; no I/O |
| `FaultSentences.Describe(DatabaseFault, bool lighthouseRetries)` | core (at most 80 characters, fix first) + explanatory tail where the table has one + the "tries again" tail only if the table allows it for that kind and the caller says so |
| `FaultSentences` other members | the non-database sentences in the same file (see DD-2) |

Per-caller variant. Only two exist, and they differ by one flag, so no audience enum: `lighthouseRetries` is true
only for the sink, and only for an event that carries a refresh `UpdateType` (a refresh round runs again by itself
and says so). Settings and page requests pass false. Reason for deriving it from the event rather than from "the
sink is background": the sink also receives Settings-sourced events (the failed-backup `LogError(ex, ...)` carries
a database exception), and "Lighthouse tries again by itself" on a backup would be false. Rejected: a caller-supplied
audience enum (a third value would be invented the day a fourth caller appears); a scope property per call site
(a mechanism for a clause the separate retry-pacing Bug will make precise).

Unwrapping. A bounded walk (depth at most 16, a visited set against cycles) over `InnerException` and, for an
`AggregateException`, every `InnerExceptions` entry; no wrapper type is named, so `DbUpdateException`, EF's
`RetryLimitExceededException` and `TargetInvocationException` all work. Each node goes through the detectors; the
most specific hit wins: a `PostgresException`/`SqliteException` verdict beats the `Other` verdict of a bare
`NpgsqlException`/`DbException` seen earlier in the chain. `OperationCanceledException` classifies as `null`.

Provider detection without the contexts knowing the providers. Two small internal detectors
(`PostgresFaultDetector`, `SqliteFaultDetector`), each a code table (data, so a test can enumerate every row), tried
in turn by the classifier; the exception type is self-identifying, so the configured provider is not consulted (a
restore from the other provider's backup cannot confuse it). Postgres: `PostgresException.SqlState` exact codes
plus the prefixes `08`, `XX`; a bare `NpgsqlException` (not a `PostgresException`) whose chain holds a
`SocketException`, `TimeoutException` or `IOException` is `Unreachable`, any other is `Other`. SQLite:
`SqliteException.SqliteErrorCode` as it is (the driver already reports the primary code; the extended code is a separate property that is not read, so no masking), where 13 is the only `OutOfSpace` and 10 (IOERR) is deliberately `Other` (M5). A third
provider adds one detector and catalogue rows and changes nothing else (open/closed).

**Refinement of the DISCUSS table (flagged, one row, reversible).** The table says "any other `PostgresException` or
`DbException`" gets the default sentence, and N2 makes the sink reword unconditionally. Taken literally, a unique-key
violation (`23505`), a syntax error (`42601`) or SQLite 19 (CONSTRAINT) logged with an authored message such as
"Failed to save Team X" would be overwritten by "Open the full log: Lighthouse could not use its database", which is
both wrong (the store is healthy) and a loss of a good message. So query and data errors are not database faults:
Postgres classes `22`, `23` and `42` **except `42501`**; SQLite 1, 19, 20, 21, 25 classify as `null` and keep their authored text. `42501` (insufficient_privilege) is the counter-example and is named in the Postgres code table as `PermissionRefused`: it sits in class 42 but is a missing grant on the store, so a bare "class 42" rule would hide an outage the operator must fix behind an authored log line. The detector tests `42501` before the class-42 exclusion, and a test pins it (DD-7). `3D000` (missing database) is also named, in the `Unreachable` row, with no ninth class (N1 says eight): that row's sentence points at the connection string, which is where a wrong database name lives. Everything
else from a driver, including `40001`/`40P01` if they escape, `57014` and SQLite 10, is `Other` (Q2 unchanged for the
infrastructure classes). Tracker exceptions are never classified: no tracker library throws a `DbException`, and the
walk only matches driver types, so the "tracker text stays" rule holds by construction, not by a check.

### DD-2 The sentence catalogue (question 2)

One static class, one file, `FaultSentences`, holding every sentence a person can be shown for a failure of the store
or of an operation on it:

- the class table of the delta (core, optional explanatory tail, `RetryTail` flag per row), as data keyed by
  (kind, store); a total lookup with a provider-neutral `Other` fallback. A table beats a `switch` here: an exhaustive
  switch over an enum needs a CS8524 pragma in this repository and hides missing rows from a test; a table can be
  enumerated;
- the host-disk and tool-failed sentences of Settings (DD-4), the two request sentences (DD-5), the licence
  sentences, the startup health sentence.

Rules, each pinned by a unit test: core at most 80 characters for every row on both stores; fix before cause; no
path, SqlState, table name or tool output; the retry tail only on rows the delta allows; Settings and HTTP callers
get none. The test enumerates `DatabaseFaultKind x DatabaseStore` so that a row cannot be forgotten.
Rejected: sentences on the enum via attributes or resources (invites a localisation story nobody asked for and
splits the text across files); one sentence per call site (the leak this Bug is about).

### DD-3 `RecentProblemsSink` rewording and the `:295` template (question 3)

Mechanism. In `AsProblem` (a pure function of the `LogEvent`): call `DatabaseFaultClassifier.Classify(logEvent.Exception)`;
if a fault comes back, `Message` becomes `FaultSentences.Describe(fault, lighthouseRetries: <the event carries an
UpdateType property>)` and `Refresh` is `null` (the name swap in `RecentProblemsReport` searches for text that is no
longer there; a clean null is clearer than a no-op `Replace`); otherwise the line is exactly as today. The full exception
is untouched on the `LogEvent`, so the file sinks still hold it. The classifier is a static pure call, which is why the
sink needs no constructor change and no DI. `ExceptionType` keeps the type name.

Consequences to state: the whole authored line is replaced, so the Task Manager row no longer says which Portfolio
(the log has it); this is what the 80-character budget forces. Old entries stay (Q6). The sink also rewrites
Settings-sourced and HTTP-sourced events (the `ExceptionHandlerMiddleware` "unhandled exception" line carries the
database exception), which is the point of N2.

Call sites. Because the sink is the safety net, a site needs a change only if its text could show something the
sink does not cover:

| Site | Change |
|---|---|
| `UpdateServiceBase.cs:295` (`{Exception}` renders the whole exception, stack included, into the message for **any** exception) | Fixed text, exception attached: `LogError(exception, "The {UpdateType} update loop failed.", updateType)`; `updateType` is passed as the enum (not `.ToString()`) so the event carries a refresh `UpdateType` and the sink can add the retry tail. This is the reported leak. |
| `ForecastUpdater.cs:368`, `UpdateQueueService.cs:563` (a Redis or database cleanup; no tracker text possible) | Fixed text, no `exception.Message`. The crafter confirms by reading the try body that no connector call is inside; if one is, treat as the next row. |
| `UpdateServiceBase.cs:71`, `ForecastUpdater.cs:276`, `UpdateQueueService.cs:437`, `WriteBackTriggerService.cs:57,100,190` (write-back: tracker text can be the message) | **Unchanged.** A database fault there is reworded by the sink; a tracker error stays byte-identical (slice 01's Jira 401 criterion). Rejected: calling the classifier at each of these six sites (it would reword nothing the sink does not, and spreads the rule). |
| `WriteBackService.cs:65` | Log hygiene only, as A2 says: stop copying `ex.Message` into the item result. |

So the classifier is called from exactly: the sink, the Settings wording (DD-4), the HTTP handler (DD-5). That is the
design's answer to "does a call site need provider-specific code": it does not even need the classifier.

### DD-4 Settings sites and the anonymous health check (question 4)

`DatabaseManagementService` (backup, restore, clear) and `LicenseController` pick their sentence from the catalogue
by the **type** of what was thrown, never by text:

1. A `DatabaseOperationException` (new, derives from `InvalidOperationException` so every existing catch and test
   shape survives; carries `OperatorSentence`, which is also its `Message`, plus a separate `LogDetail` property holding the tool stderr or the zip entry name; the log detail is written to the log explicitly, so nothing that reads `ex.Message` can leak it) shows its
   `OperatorSentence`. Thrown by: the provider tool failures (`pg_dump`, `pg_restore`, `psql`; sentence "The backup
   tool failed: the log has the details." with the stderr kept in the log detail only) and the authored rejections in
   `ExtractEncryptedZip` / `DecryptFile` / `ExtractToDirectorySafely` (sentences as today, minus the entry name that
   `Zip Slip detected: {entry.FullName}` puts in the alert). Tool stderr can therefore never select a location (the
   tool failure is a different type from an `IOException`).
2. A `DatabaseFault` (classifier, `lighthouseRetries: false`) shows the class sentence.
3. An `IOException` that is out of space shows the host-disk sentence of the delta. Every `IOException` that reaches
   the catch is Lighthouse's own file work (manifest, zip, upload copy, unzip, the SQLite provider's copy), because
   tools fail as (1). Detection: `HResult == 28` (ENOSPC on Linux and macOS) or `(HResult & 0xFFFF)` of `0x70` /
   `0x27` (Windows). **Hypothesis, to verify in DELIVER (AFK-D4), time-boxed to one hour on a tiny tmpfs and, if a machine is at hand, on Windows; when the hour is up the fallback below ships:** the HResult values; if they
   do not hold, fall back to the same sentence for any `IOException` at these stages with "disk space or
   permissions" worded in, rather than guessing from the message.
4. Anything else: "The {backup|restore|clear} failed: the log has the details." The exception is in the log through the
   existing `LogError(ex, ...)`.

One function does this for all three operations (`DatabaseOperationWording.Describe(exception, operation)`, in the
`DatabaseManagement` namespace, pure), so the three catch blocks stay three lines; the licence controller reuses (2)
and the catalogue directly. `DatabaseManagementController.cs:37,52,78,82,96`: the audit result is "keep", for a reason that is stated here: the service's backup, restore and clear (`DatabaseManagementService.cs:78,128,181`) swallow every exception into the tracker (`TransitionToFailed`) and never rethrow, so a `DatabaseOperationException` cannot reach the controller's `catch (InvalidOperationException)`; those catches see only the gate and artefact-lookup exceptions. All five render `ex.Message`, so the ratchet entry for this file is exactly 5. A test pins the premise (DD-7, Settings row). Even if the service ever let one escape, its `Message` is the `OperatorSentence` (M1), never the log detail. Their
`InvalidOperationException`/`KeyNotFoundException`/`ArgumentException` are thrown by the gate and the artefact lookup
with authored text; none comes from a driver. The crafter re-reads each catch to confirm, and the ratchet (DD-6) lists
the file with its exact count so a sixth `ex.Message` fails the build of the test.

`LicenseController.cs:65,80`: keep the status (`BadRequest`, copy-only: the frontend renders the string). `JsonException`
(a malformed file) answers the existing "Invalid license file"; a `DatabaseFault` answers "The licence could not be
saved: " + class sentence (Settings variant, no retry tail); anything else answers "The licence could not be saved: the
log has the details." with the exception logged (there is no log call at all today).

`ClusterSubstrateHealthCheck.cs:54`: inject `ILogger<ClusterSubstrateHealthCheck>`, log the exception, return
`Refused("the cluster substrate probe could not run against Postgres and Redis; the log has the details.")`, passed without the `health.startup.refused:` prefix because `Refused` adds it itself (`ClusterSubstrateHealthCheck.cs:58-59`). No
classification here: the endpoint is anonymous (A3/A7), so not even the class sentence. The other `Refused` strings are
authored constants and stay.

### DD-5 Slice 04: ordinary `/api` failures (question 5)

Options. (A) `IExceptionHandler` registered with `AddExceptionHandler<ApiExceptionHandler>()` and
`UseExceptionHandler(options => options.ExceptionHandlingPath = "/index.html")`; (B) a third MVC `IExceptionFilter`
beside the two existing ones; (C) a hand-written try/catch middleware. **Recommend (A).** (B) only sees exceptions
raised inside MVC, and the API-key and JWT authentication handlers read the database before any controller exists, so
those would keep answering an empty 500; SignalR negotiate and minimal endpoints likewise. (C) reinvents what the
framework ships. (A) keeps the framework's single "unhandled exception" log line, so the exception is logged once
(and the sink rewords it). The two existing filters still run first for their own types (a handled exception never
reaches the middleware).

Behaviour of `ApiExceptionHandler` (a driving adapter, in `API/`):

| Case | Answer |
|---|---|
| Path not under `/api` | returns `false`; the middleware then falls through to the configured path `"/index.html"`, which replaces the dead `"/Error"` and keeps the old behaviour for page paths (the shell with status 500, GET only; nothing posts to a page path). A DELIVER check, not an assumption: confirm the fallback response keeps status 500 (the middleware sets it before re-executing, but the static-file or fallback endpoint might reset it to 200); if it does, accept 200 for page paths, since nothing keys on it, and say so in the delivery log. |
| `OperationCanceledException` and the request was aborted | returns `false` (the client is gone; nothing to say) |
| `BadHttpRequestException` | its own 4xx status, with the "other" sentence below (today it would be 500) |
| `Classify` returns a fault, for any caller (signed in, anonymous or authentication off; no role check) | 500, ProblemDetails: `detail` = the database sentence "Lighthouse cannot use its database right now. Please tell your administrator.", `title` = "Database fault", extension `code` = `database-fault`, `type` = the code (same convention as the 409 filter) |
| Not a database fault, for any caller | 500, `detail` = the generic sentence "Lighthouse could not finish that request: try again, and if it keeps failing ask your administrator to open the log.", `title` = "Request failed", `code` = `unexpected-error` |

Exactly two sentences exist on `/api` (AFK-D5, which replaces AFK-D1): the database sentence and the "other" sentence, the same for every caller. The handler has no RBAC service, makes no role check and reads no database, so it cannot fail for the very reason it reports. The specific cause is where administrators already look: Task Manager (the sink) and the log. Never exception text, never a stack, `Cache-Control: no-store`. The status is 500 for both cases; 503 for "database
unreachable" would be more precise but changes what `title`/retry logic in clients might key on, and the delta asked
for no new status. Not wired in Development (the developer exception page stays, as today).

What breaks? Nothing: the frontend's `extractMessage` already falls through `message`, `errors`, then `detail`, so a
ProblemDetails `detail` is shown as the message with no frontend change for this case. Lighthouse-Clients copies only an
optional `code` and `title` (`packages/client/src/index.ts:1893-1894`), so the two new codes arrive as the existing
optional `problemCode`/`problemTitle`; additive, no changeset (A8 stands). The crafter greps the frontend for
`problemCode ===` to confirm no branch is keyed on a 5xx code.

Frontend (`BaseApiService.createApiErrorFromAxios`). Before the existing parse: if the status is `>= 500` and the body is
unreadable, the message is the generic sentence (same words as the backend's) and `technicalDetails` is
`HTTP <status> from <url>`. "Unreadable" is: a string that is empty or whitespace, or a string that starts with `<`
after trimming, or a `text/html` response `content-type`. **Not** "any 5xx string": `DeliveriesController.cs:578,586`
answers 503 with an authored plain-text sentence (`SourceCouldNotBeAsked`), which must survive byte-identical, as must every
4xx. The 5xx check lives in `createApiErrorFromAxios` (`BaseApiService.ts:46-65`; the string-body copy that leaks the HTML is at `:80-81`), the one place the error path builds `ApiError.message`; it may reuse the HTML test of `htmlResponseGuard.ts` (one place that knows what an HTML
answer looks like) and `BaseApiService` keeps the sentence as one constant. Blob responses are skipped (the body is not a
string). The generic sentence is deliberately the same text on both sides; the two tests pin it (knowledge shared across
stacks without codegen, accepted).

Slow connection (CLAUDE.md rule). No screen is added or changed; the failure look of every surface is the existing one
and now carries a sentence: the top snackbar (`SnackbarErrorHandler`), the validation banner
(`setValidationError(error.message)` in `useModifySettings`, the wizards), the backup `Alert` (driven by the operation
poll), and the Task Manager row. Loading look: unchanged everywhere. Two checks the crafter makes by hand with the
browser throttled: (1) the Task Manager request itself reads memory only (`RecentProblemsSink`), so it keeps working
during a database outage and never shows the reassuring empty state; if it fails it renders nothing, as today;
(2) a 5xx never leaves a previous answer on screen as if current, which TanStack's keyed queries already guarantee.
Known limit, to confirm in DELIVER: `ExceptionHandlerMiddleware` clears response headers, so a cross-origin deployment
(`Authentication:AllowedOrigins`) loses the CORS headers on the problem body and the browser reports a network error. It
does so today for the empty 500, so this Bug does not worsen it; the same-origin Docker deployment is covered.

### DD-6 The guard against regressions (question 6)

Candidates and what each would have caught of the reported bug (`LogError(exception, "...{Exception}", exception)`):

| Guard | Catches the reported shape | Catches `ex.Message` in a template or body | Cost | Verdict |
|---|---|---|---|---|
| BannedApiAnalyzers `P:System.Exception.Message` in `BannedSymbols.txt` | **No**: the leak passes the exception as a template argument | Partly: it bans the base property, so a type that overrides `Message` (for example `ArgumentException`) slips through | about 50 sites, 33 outside connectors, each needing an inline `#pragma warning disable RS0030` or a folder `.editorconfig`; high noise, low signal (most are authored domain text) | **Rejected** as the main guard |
| Roslyn analyzer of our own | Yes | Yes | a new analyzer project | Rejected: disproportionate |
| IL scan (existing `IlDependencies`) for calls to `get_Message` | No | Yes, whatever the variable is called | medium | Held in reserve if the name list below is ever evaded |
| **Source ratchet test** in `Tests/Architecture/` (the style of `SecretReadPathSourceStructureTest`): production `*.cs`, regex for `\{Exception\}`-style placeholders fed an exception, and for `(ex|exception|e|inner)\.Message`, with an **exact per-file count** table (connectors, Blackout controllers, `DatabaseManagementController` (exactly 5), the six write-back sites, `Program.cs` FATAL) | **Yes** (the placeholder rule, count 0 everywhere) | Yes: a new use anywhere outside the table fails with "classify it, choose a sentence, or add the file with a reason" | one test, about an hour | **Recommended** |
| Sink behaviour test over a real `LogEvent` carrying a `PostgresException` | Yes, structurally | n/a (Recent problems only) | part of slice 01 | **Required**, it is the design's real guard: the leak is not representable in Recent problems whatever a future author writes |

Recommendation: the sink behaviour test plus the per-file ratchet. The ratchet is the "cheapest that would have caught
this": the placeholder rule alone fails on `UpdateServiceBase.cs:295`.

### DD-7 Test strategy (question 7)

| Layer | What | Notes |
|---|---|---|
| Classifier table tests, **real instances where cheap** | SQLite, no container: 13 via `PRAGMA max_page_count` then insert; 8 by opening read-only and writing; 26 from a garbage file; 5/6 with two connections; 14 by opening in a missing folder. Postgres via the shared container (`SharedPostgresContainer`, `Lighthouse.Backend.Tests/Integration/Containers/SharedPostgresContainer.cs`), **each test creating its own role and its own database** and dropping both at the end; never `ALTER` the shared role, never `pg_terminate_backend` outside the test's own session (the pid comes from the test's own connection, `pg_backend_pid()`). In that frame: `28P01` wrong password for the dedicated role, `3D000` connecting to a database name that does not exist, `42501` dedicated role without a grant, `55P03` with `lock_timeout` and two sessions of the dedicated role, `53300` with `CONNECTION LIMIT` on the dedicated role only, `25006` with `default_transaction_read_only` set on the dedicated database or session, `57P01` by terminating the test's own session, `08` by connecting to a closed port. Any code that cannot be provoked inside this frame uses a constructed exception | This is the Earned Trust probe: the mapping is a belief about a vendor, and the real instance is the test that the belief holds. |
| Classifier table tests, **constructed** | `53100`, `53200`, `XX001/XX002` and the rest, built with the public constructors (`PostgresException`, `NpgsqlException`, `SqliteException`; the crafter confirms the exact overloads in the pinned driver versions) | The two that cannot be provoked cheaply (`53100`, `53200`) are covered by the maintainer's dogfood run on a size-limited volume (slice 01's production-data criterion), recorded in the delivery log. |
| Wrapping | each fault inside `DbUpdateException`, EF `RetryLimitExceededException`, `AggregateException`, a three-deep chain, a cycle, a chain with a tracker exception beside it | The tracker case asserts `null`. |
| Not-a-fault rows | `23505`, `42601`, SQLite 19 and 1 assert `null` and an authored log line stays unchanged in the sink. Counter-example: `42501` sits in class 42 yet asserts `PermissionRefused` (not `null`), and a fixed-text sink event carrying it is reworded | Pins the refinement and its one exception. |
| Catalogue | every (kind, store) has a row; core length at most 80; no digits-and-colons that look like a SqlState; retry tail only where allowed | A string-length assertion over the table, as the delta asked. |
| Mapping completeness (self-application of the probe) | the detector tables are enumerated and every row must be hit by at least one test above | A row without a test fails. |
| Sink | a real `LogEvent` per class: sentence replaces message; exception still on the event; fixed-text event with `53100` attached is reworded; same text with a non-database exception is unchanged; an event with an `UpdateType` property gets the tail, one without does not; `Refresh` is null when reworded | Changes `RecentProblemsSinkTest`. |
| Call sites | one test per changed site: a thrown `PostgresException` never puts `Exception data:`, `SqlState` or `pgsql_tmp` into the Recent problems message (through the real sink); a Jira 401 at each write-back site is byte-identical | `Changes to ForecastUpdater`, `UpdateQueueService`, `UpdateServiceBase`, `WriteBackTriggerService` tests. |
| Settings | `DatabaseOperationWording` per type (tool failure, rejection, database fault, IOException out of space, other); `DatabaseOperationException.Message` equals its `OperatorSentence` and never holds the `LogDetail`; **controller premise**: a `DatabaseManagementService` whose provider throws a `DatabaseOperationException` with a distinctive log detail completes without throwing, and the tracker holds only the `OperatorSentence`, which pins that the controller's `catch (InvalidOperationException)` never receives one; no path, SqlState or stderr in any result; no "tries again" in any | The HResult hypothesis is checked on a tmpfs once. |
| Licence + health | `JsonException` keeps "Invalid license file"; fault and other get the fixed sentences; `/health/startup` with a failing probe returns the fixed sentence and logs the exception | |
| Handler | unit tests of `ApiExceptionHandler` (the table in DD-5: the same answer for anonymous, signed-in and auth-off callers, aborted request, `BadHttpRequestException`); **one** `WebApplicationFactory` test in Production environment, whose database fault is injected by a test-only faulting service registered in `ConfigureTestServices` (its method throws a constructed `PostgresException`/`SqliteException`, reached from a real controller action, so the real handler and pipeline run; a dead database connection string is the heavier alternative that yields a real `NpgsqlException`, slower and provider-specific): a forced database fault on `GET`, `POST`, `PUT` answers JSON ProblemDetails, never `text/html`, never empty; a non-`/api` path still gets the shell | Production environment because the handler is not wired in Development. Editing `Program.cs` forces the full backend Integration suite (known); keep that edit to the three registrations. |
| Frontend | `BaseApiService` tests: 500 with HTML, 500 with empty string, 502 with a proxy page, 500 with `text/html` header → the sentence; 503 with the authored delivery sentence, 409, 400 validation, 4xx string → byte-identical; ProblemDetails `detail` shown | Vitest. |
| Guards | the ratchet (DD-6); an ArchUnit rule that `DatabaseFaults` depends on no other Lighthouse namespace (leaf), and that Work Items, Forecasting, Portfolio/Delivery, RBAC/Identity do not reference `Npgsql` or `Microsoft.Data.Sqlite` (check it is green first; if not, ratchet the current offenders) | |

### DD-8 Effect isolation and Earned Trust (summary)

- Functional core: `DatabaseFaultClassifier`, detectors, `FaultSentences`, `DatabaseOperationWording`, `AsProblem`,
  the frontend predicate. All pure; none logs, reads configuration or touches I/O. The "preview wrote to disk" class of
  bug has no analogue because nothing here has an effect to misplace.
- Imperative shell: the sink's queue under its lock (as today), the handler's response write, the Settings tracker
  transition. Each already exists; none gains a capability.
- Driving ports that only read do not gain write methods (`IRecentProblems` is unchanged).
- Earned Trust. The one external belief is "what a driver's code means". The probe is the real-instance tests plus the
  mapping-completeness test above, and it re-runs whenever Npgsql or Microsoft.Data.Sqlite changes major version
  (Renovate PRs run it for free). The environment that lies: Docker `/tmp`, so the host-disk sentence says "the volume
  that holds its temporary folder" and the docs page spells out the Docker case. Where the cause cannot be told, use the weaker true sentence, as ADR-184 does with Unreachable rather than Authentication failed
  (ADR-184): unknown means the default sentence.

## Wave: DESIGN / [REF] Component view and slice changes

```
 DRIVING ADAPTERS            APPLICATION / PLATFORM LANE                 DRIVEN / OUTPUT
 ----------------            ---------------------------                 ---------------
 Task Manager (SPA) ------> LogsController ----> IRecentProblems <------ RecentProblemsSink  [MODIFIED]
                              (unchanged)          (unchanged)            |  AsProblem: Classify(ex) -> Describe
                                                                          |
 Settings page ----------> DatabaseManagementController (audited, kept)   |   +--------------------------------+
                           DatabaseManagementService          [MODIFIED]--+-->| DatabaseFaults  (Platform lane)|
                             + DatabaseOperationWording       [NEW]       |   |  DatabaseFaultClassifier  [NEW]|
                             + DatabaseOperationException     [NEW]       |   |  Postgres/Sqlite detectors[NEW]|
 Settings > licence -----> LicenseController                  [MODIFIED]--+-->|  FaultSentences (catalogue)[NEW|
 anonymous /health/startup> ClusterSubstrateHealthCheck       [MODIFIED]     |  pure, no state, no I/O        |
                              (logs; fixed sentence)                         +---------------^----------------+
 any /api request --------> ApiExceptionHandler               [NEW]----------------------------+
   (throws)                   (replaces UseExceptionHandler("/Error"); no RBAC service, no role check; two sentences)
 SPA error path ----------> BaseApiService + htmlResponseGuard [MODIFIED]  (5xx HTML/empty -> sentence)

 Refresh loop / queue / forecast / write-back  (UpdateServiceBase :295 MODIFIED; ForecastUpdater :368,
   UpdateQueueService :563 MODIFIED; six write-back sites UNCHANGED, WriteBackService :65 hygiene)
   --> Serilog --> RecentProblemsSink + rolling file (full exception, unchanged)

 Domain / shared kernel (Models): UNCHANGED.   Persistence, trackers, EF migrations: UNCHANGED.
 New external call: none.   New port: none (pure function).   New event: none.
```

Slice impact (no re-slicing; estimates hold):

| Slice | Design delta |
|---|---|
| 01 | classifier, detectors, catalogue, sink branch, `:295`/`:368`/`:563` templates, `WriteBackService:65`, ratchet test, leaf and driver-reference ArchUnit rules, real-instance tests. About 2 days: the DISCUSS 1.5 plus 0.5 after the DESIGN review (0.25 for real-instance Postgres tests with a dedicated role and database per test, 0.25 for the stricter class-42 rule, its counter-example tests and the ratchet entries). The six write-back sites need no edit, which removes the "classify first" risk the slice listed. |
| 03 | `DatabaseOperationException` + `DatabaseOperationWording`, provider throws changed to the typed exception, licence, health logger. About 1.25 days. |
| 04 | `ApiExceptionHandler`, three registrations in `Program.cs` (`AddExceptionHandler<ApiExceptionHandler>`, `AddProblemDetails`, and the `UseExceptionHandler` line that replaces `UseExceptionHandler("/Error")`), frontend predicate and sentence, one Production-environment integration test. 0.5 to 1 day. |

Deviations from the DISCUSS text, all reversible, all decided here (AFK):

| # | Deviation | Why |
|---|---|---|
| F1 | Query and data errors (Postgres 22, 23 and 42 except `42501`; SQLite 1, 19, 20, 21, 25) are not database faults and keep their authored text | The sink rewords unconditionally (N2); without this a constraint violation would read "could not use its database" and hide a good message |
| F2 | The retry tail is added only to events that carry a refresh `UpdateType` | Settings and HTTP events also reach the sink; "tries again" on a backup is false. Where it cannot be shown true it is left out (the delta's own "only where true") |
| F3 | The six write-back sites keep `exception.Message`; the sink covers database faults there | Keeps tracker text byte-identical and avoids six redundant classifier calls |
| F4 | Settings sentences are chosen by exception **type** (`DatabaseOperationException`), with `pg_dump` and rejections thrown as that type | The current `InvalidOperationException` is shared by tool failures and authored rejections, so text-sniffing would be the only alternative |
| F5 | Superseded by AFK-D5 (which replaces AFK-D1): on `/api` every caller gets the same database sentence and there is no role check in the handler | The administrator check reads the database, so it would fail during the outage it reports; exactly two `/api` sentences exist |

## Wave: DESIGN / [REF] DELIVER owes (shared-document edits, in the slice that makes them true)

ADR-185 dated note (the message is chosen, not rendered); the new ADR from the draft in this folder (number at
DELIVER); ADR-270 glossary row "Database fault" and `ARCHITECTURE.md` section 4 line under the Platform lane
(`DatabaseFaults` added to the Platform/Persistence pattern); `jobs.yaml` REVISED note; `docs/settings/taskmanager.md`
sentence; database-management docs page (the Docker `/tmp` case). Handoff to DEVOPS: no external integration is added
or changed, so no contract-test annotation; usage-data event proposed "N/A, because it reports a failure and adds
nothing to be used" (DEVOPS confirms). Quality gates: requirements traced (15 sites plus the sink and the handler to
DD-3 to DD-5); boundaries stated; ADR draft with 6 alternatives; OSS only (no new package); enforcement tooling named
(ArchUnitNET rules and the source ratchet); AC stay behavioural.

### Open points for the maintainer (none blocks DELIVER)

1. Closed by AFK-D5: `/api` gives every caller the same two sentences and makes no role check (ADR-185 notes the reach of
   "authenticated"), so the handler needs no RBAC service and nothing to gate.
2. The IOException out-of-space detection (DD-4 item 3) rests on HResult values; DELIVER verifies them within a one-hour time-box (AFK-D4), then the fallback sentence ships.
3. Cross-origin deployments lose the problem body to the exception middleware's header clearing (DD-5); same as today.

## AFK decisions after DESIGN (2026-11-02, orchestrator)

| # | Decision | Why |
|---|---|---|
| AFK-D1 (DESIGN F5) (**superseded by AFK-D5**) | ~~**On `/api`, the database-fault class sentence goes to System Administrators only.**~~ Every other signed-in caller (and every anonymous one) gets the generic sentence, e.g. "Lighthouse cannot use its database right now: please tell your administrator." | The class sentences name PostgreSQL volumes and `max_connections`; a non-admin (or a Jira-frame viewer) cannot act on them and should not learn the deployment's internals. One condition in the handler. |
| AFK-D2 (DESIGN F1) | **Accepted:** query and data errors (Postgres classes 22, 23, and 42 **except 42501**, which is `PermissionRefused`; SQLite 1, 19, 20, 21, 25) are not database faults and keep their authored text. | Without it, the unconditional sink rewrite would turn a constraint violation into "could not use its database". |
| AFK-D3 (DESIGN F2-F4, frontend 5xx rule) | **Accepted as designed:** the "tries again" tail only on refresh events; the six write-back sites unchanged; the Settings sentence chosen by exception type; only an empty / HTML / `text/html` 5xx body becomes the plain sentence (authored 503 text in `DeliveriesController` stays). | Each keeps the change narrow and avoids message-text sniffing. |
| AFK-D5 (replaces AFK-D1, after the DESIGN review M2/M3) | **On `/api`, every caller gets the same generic sentence for a database fault: "Lighthouse cannot use its database right now. Please tell your administrator."** No role check in the exception handler. The specific cause is where administrators already look: Task Manager → Recent problems (via the sink) and the log. Non-database failures get the generic "could not finish that request" sentence; exactly these two sentences exist on `/api`. | The System Administrator check reads the database, so during a database outage it throws, and the handler would need a fallback that makes the admin-only path almost never reachable; it also adds a database round trip per error and a lifetime trap in the handler. The Task Manager already carries the cause for the people who can act on it. |
| AFK-D4 | **The `IOException` HResult values (28; 0x70 / 0x27 on Windows) are verified in DELIVER** on a small tmpfs (and on Windows if available), with the "disk space or permissions" fallback sentence if they fail. | Unverified hypothesis; a cheap check. |

## Wave: DEVOPS / [REF] Platform and delivery readiness (bug-fix scale)

Mode: PROPOSE (maintainer AFK). Binding inputs: Q1 to Q8, N1, N2, A1 to A8, AFK-D2 to AFK-D5 (AFK-D5 replaces A7 and AFK-D1). No new
infrastructure, container, workflow, package or deployment target: the fix ships in the existing artifacts (server zips, Docker
image and Helm chart, Standalone) through the existing `ci.yml` pipeline. Existing infrastructure was searched first
(`ci_backend.yml`, `ci_changes.yml`, `Scripts/test-selection/path-classifier.sh`, the verify workflows, the Containers tests); nothing is added.
Rejected simpler alternative for the pipeline: none needed, because nothing is added.

### 1. Usage-data event

N/A, because the Bug reports a failure and adds nothing a user would use, so no event would show "the feature is used".
`UsageDataEventName` stays at its current last value, 24 on this checkout. Story 6501 has reserved 25 (name-only
`ParentSourcesSavedWithSeveralEntries`, re-read on `main` at its DELIVER step one); this Bug takes no integer and
does not touch `UsageDataEventName.cs` or `docs/settings/usagedata.md`.

### 2. CI

| Question | Answer |
|---|---|
| Test placement and categories | New backend tests go beside what they test (`Tests/Services/Implementation/DatabaseFaults/`, the existing `RecentProblemsSinkTest`, updater, queue, write-back, `DatabaseManagementService`, `LicenseController`, health and handler test classes) and carry no `Integration` category. The CI filter is `Category!=Integration` (`ci_backend.yml:114`), so an Integration-tagged class would silently never run in the ordinary job; DELIVER adds a one-line check in review: `git grep 'Category("Integration")'` over the new files returns nothing. The Production-environment `WebApplicationFactory` handler test is an ordinary test, not Integration (it injects a faulting service; no live connector). |
| Postgres real-instance tests | They use `SharedPostgresContainer` in `Tests/Integration/Containers/`, like the existing migration and parity tests there (tagged `requires-docker` or an epic category, never `Integration`, so they run in the default job). Every CI job on `ubuntu-latest` (`ci_backend.yml:41`) has Docker, which is how those existing container tests already run; no workflow change. `PostgresContainerSharingGuardTest` only scans for `PostgresContainerFixture.StartFreshAsync(`, so one shared container is kept by convention, not enforced against every route: the new tests take their own role and database from the shared one (DD-7) and never start a container. Runtime estimate (a guess to confirm in the delivery log): about ten real-instance tests, each creating and dropping a role and database (a few hundred ms) plus the slow ones (`lock_timeout`, closed-port connect, terminating the test's own session; a few seconds), so roughly 15 to 30 seconds on the already-running shared container; the backend budget check (`ci_backend.yml:200-204`) is warn-only, so a miss warns and does not fail. Local runs need Docker; without it the container tests fail locally the way the existing ones do. |
| Ratchet and ArchUnit | The source ratchet (`Tests/Architecture/`) and the ArchUnit rules (the `DatabaseFaults` leaf rule; the no-driver-in-contexts rule; `DatabaseFaults` added to `PlatformPersistencePattern`) are plain tests in the default suite, no category, so they run on every backend push. CI tests the Release build, where ArchUnitNET misses async edges, so the new rules use the IL-dependency form the existing module rules use (memory: ArchUnitNET misses async edges in Release). |
| Program.cs edit and the Integration suite | **Consequence, stated.** `Program.cs` is on `SHARED_REGEX` (`Scripts/test-selection/path-classifier.sh`, and `ci_changes.yml` sets `connector_shared`). The edit therefore sets `force_full=true` in `ci_backend.yml:128-134`, and the filter becomes `Category!=Integration|(Category=Integration&Category!=UsageDataCanary)`: every live connector suite (Jira, ADO, Linear, ServiceNow, GitHub) runs against the real services in that CI run. This is a DELIVER cost, not a design defect: (a) the Linear API key is shared with CI, so no local connector run and no repeated CI re-runs of this push; (b) push once, from a green local `dotnet build` and filtered `dotnet test`; (c) keep the `Program.cs` edit to the three registrations of slice 04 (`AddExceptionHandler<ApiExceptionHandler>`, `AddProblemDetails`, and the `UseExceptionHandler` line replacing `UseExceptionHandler("/Error")`; no other change) so nothing else in the file can turn the run red; (d) put the `Program.cs` change in the last backend slice step (slice 04), so slices 01 and 03 push without the full suite; (e) a red connector test in that run is triaged as environmental first (rate limit, tracker outage), not retried blindly. Local runs keep excluding the five live categories. |
| E2E (`ci_verifysqlite`, `ci_verifypostgres`) | They run the built artifact against the real database and drive the UI through Page Objects against seeded demo data. The ProblemDetails change is only visible on a 5xx, which a healthy E2E run never produces, and the frontend change only rewrites an empty or HTML 5xx body, so **no E2E spec should change**. Not run locally by this Bug. DELIVER confirms with one search that no spec or Page Object asserts on a 5xx body or message (the status-code matches found under `Lighthouse.EndToEndTests` are timeouts and row counts, to be confirmed by the crafter). The 503 `SourceCouldNotBeAsked` text stays byte-identical, so the delivery specs are untouched. No new E2E test: this is a thin-sanity-check suite and the outage path is covered by the Production-mode `WebApplicationFactory` test. |
| Frontend | `pnpm test` and `pnpm build` (Biome plus `tsc`) as in the quality gates; the `BaseApiService` tests are ordinary Vitest. |
| Docs-only pushes | A finalize push that touches only docs starts no CI run (`ci.yml` paths filter); the code commit's run is the one that counts. |

### 3. Observability: what operators see after the fix

| Surface | Before | After |
|---|---|---|
| Log files (all file sinks) | whole exception, stack included, and for `TryUpdating` also in the message | **Unchanged in content**: the full exception stays attached to the event and is written by the file sinks; the sink rewording happens only in `RecentProblemsSink.AsProblem`, which does not touch the other sinks. Message templates at the changed sites lose their `{Exception}` or `ex.Message` placeholders, so the log message is shorter and the exception still prints in the stack block below it. |
| Task Manager, Recent problems | two-line wall of driver text | one fixed sentence per database fault (fix first, cause second, core at most 80 characters); non-database entries unchanged; tracker text unchanged; old entries stay until restart (Q6). The row no longer names the Portfolio (the log does). |
| `/health/startup` (anonymous) | `ex.Message` of the driver | a fixed sentence; the exception is logged once with `ILogger` at Information (DO-11). |
| `/api` 5xx | GET: SPA shell; POST/PUT: empty body | JSON ProblemDetails with `code`, `title`, `detail`; `Cache-Control: no-store`; two sentences only (AFK-D5). |

Log volume. The file sinks gain no event for a handled `/api` exception, because `ExceptionHandlerMiddleware`
already logs it once and the new handler does not log (DD-5). `/health/startup` gains one Information line per refused probe (the exception is now logged where it was only echoed; Information keeps it in the log file but below the Warning threshold of Recent problems, so a polled probe cannot flood that list); a down database adds one line per poll, the same cadence as the
refusal itself. The sink rewording changes no event count. The unpaced retry loop (Q4, separate Bug) still produces
one entry per pass, so the log and Recent problems repeat during an outage, as today (D6). SLO: none defined for this
single-instance, self-hosted product; the operational signal that matters is "an operator can name the cause from the Task Manager without opening the log", measured by the Bug's KPI (the maintainer's dogfood run on a size-limited volume, per slice 01), not by a dashboard. No alert, dashboard or new metric is added.

### 4. Release and rollback

- **No migration**, no schema or setting change, no new configuration key. Expand-only rule: trivially met.
- **Rollback first**: the change is code only, so rollback is `git revert` of the delivery commits (or shipping the previous
  release): old Recent problems entries were never rewritten, the Settings alerts and the 500 bodies return to their old text, and no data
  depends on the new sentences. Nothing to unwind in persisted state. Docker and Helm users roll back by pinning the previous image tag; Standalone users reinstall the previous package.
- **Wire change**: 5xx bodies on `/api` become `application/problem+json` (they were an HTML shell on GET and empty on POST/PUT). Checked in
  `/storage/repos/lighthouse-clients/packages/client/src/index.ts`: the client reads a response body through `getProblemString(body, "code")` and
  `getProblemString(body, "title")` (lines 1893-1894) and attaches them only when present as the optional `problemCode` and `problemTitle`
  (lines 821-823, 1897-1898); an empty or HTML body yields neither today, a ProblemDetails body now yields both. This is additive and changes no
  type, so no client release and no changeset; Clients is **N/A for a version bump, because the only change is two optional fields that already exist**.
  DELIVER repeats the grep for `problemCode ===` in the frontend and in `lighthouse-clients` to confirm no branch is keyed on a 5xx code.
- Deployment strategy: none beyond the normal release; a canary would add nothing for a pure text and handler change with no persisted state.

### 5. Security note (for the release notes at `/release`; Q7: part of this Bug, no advisory)

Draft, to be refined at `/release` in the positioning tone (pain first, then the win): "Lighthouse no longer shows database error
details to the people who use it. When the database fails (disk full, out of memory, connection limit, read-only, unreachable), the
Task Manager, the Settings alerts for backup, restore, clear and licence import, the start-up health endpoint and ordinary page
requests now say in one plain sentence what is wrong and what to do. Previously these surfaces could show SQL fragments, table or
file names, driver error text or a stack trace; the anonymous `/health/startup` endpoint could show driver text to anyone who could reach it. The
full details remain in the log, where administrators read them. Connector error text from your Work Tracking System is unchanged." Also in the release notes, one line from D6: during an outage the Task Manager repeats the same sentence until the retry-pacing fix lands. No CVE, no advisory (Q7). Written at `/release`, not at finalize.

### 6. Mutation testing (project strategy: per-feature, break 80)

DELIVER output, not run now. Two configs, kept in the feature workspace under `docs/feature/bug-6500-disk-full-raw-sql-error/mutation/`
(the project convention, e.g. `docs/feature/fix-jira-dc-portfolio-refusal/mutation/`), run last on frozen code (memory: any edit invalidates the run).

Backend: `docs/feature/bug-6500-disk-full-raw-sql-error/mutation/stryker.6500.backend.json`, modelled on `stryker.6019.connector.json`, with `thresholds.break` set to 80
(high 80, low 70 kept from the model), `coverage-analysis: perTestInIsolation`, concurrency 4, additional-timeout 30000. Mutate globs (final file names settled by the crafter; these follow DD-1 to DD-5):

- `**/Services/Implementation/DatabaseFaults/DatabaseFaultClassifier.cs`
- `**/Services/Implementation/DatabaseFaults/PostgresFaultDetector.cs`
- `**/Services/Implementation/DatabaseFaults/SqliteFaultDetector.cs`
- `**/Services/Implementation/DatabaseFaults/FaultSentences.cs`
- `**/Services/Implementation/DatabaseManagement/DatabaseOperationWording.cs` (the Settings wording, DD-4; it lives in the `DatabaseManagement` namespace, not `DatabaseFaults`)
- `**/Services/Implementation/Logging/RecentProblemsSink.cs` (whole file; the .NET runner ignores line spans, so a large file is mutated whole and judged by its per-file score)
- `**/API/ApiExceptionHandler.cs` (the handler, DD-5)

`test-case-filter`: only the new and changed fast classes, for example `(FullyQualifiedName~DatabaseFault|FullyQualifiedName~FaultSentences|FullyQualifiedName~DatabaseOperationWording|FullyQualifiedName~RecentProblemsSinkTest|FullyQualifiedName~ApiExceptionHandler)&FullyQualifiedName!~IntegrationTest&FullyQualifiedName!~Containers&FullyQualifiedName!~Acceptance`.
It excludes the container tests and the acceptance suite on purpose (memory: Stryker must skip the acceptance suite, 80 minutes against 3; Docker per mutant is not viable); so the detector rows that only the real-instance tests pin are also pinned by the constructed-exception table tests, which is why DD-7 keeps both. Survivors are killed with one targeted test each, not with hours of re-runs.

Frontend: `docs/feature/bug-6500-disk-full-raw-sql-error/mutation/vitest.stryker.6500.config.ts`, modelled on
`docs/feature/story-6249-chart-loading-indicators/mutation/vitest.stryker.6249.config.ts`, plus a Stryker config beside it
(`stryker.config.6500.mjs`, vitest runner pointing at that vitest config, own temp dir `.stryker-tmp-6500`, own JSON file name,
`thresholds: { high: 90, low: 80, break: 80 }`, `disableTypeChecks: false`, `coverageAnalysis: "off"` as in the 6249 model, `concurrency: 2`).
Both live in the feature folder; the frontend Stryker configs in `Lighthouse.Frontend` are gitignored, so the run copies them there and nothing is committed outside this folder. The vitest config's `include` lists only the new spec for `BaseApiService` / `createApiErrorFromAxios` (DELIVER creates it, for example `src/services/Api/BaseApiService.test.ts`; no test for that class exists yet, only `htmlResponseGuard.test.ts`) and the predicate's spec if one is extracted. `mutate` takes one entry per line span, written `N-M` and `N-N` for a single line: `src/services/Api/BaseApiService.ts:46-65` (`createApiErrorFromAxios`) plus the predicate module if the crafter extracts one. The HTML test in `htmlResponseGuard.ts` is reused, not mutated here.

### 7. Docs at finalize (per feature, not batched; written after the code is confirmed, per the maintainer's rule)

| Page | Change |
|---|---|
| `docs/settings/taskmanager.md` | In the Recent problems part: a database failure shows as one plain sentence naming the fix and the cause; the full details are in the log; old entries stay until restart. Wording uses the configurable terms. |
| `docs/settings/databasemanagement.md` | The failed-backup sentences; the host-disk case ("the volume that holds its temporary folder") and the **Docker `/tmp` case**: in the Docker image `/tmp` is inside the container, so that volume is the Docker host's disk that holds container storage unless `/tmp` is mounted from a volume; tool failure says only that the backup tool failed. |
| `/health/startup` | The only mention found in `docs` outside feature folders is `docs/product/architecture/brief.md`; its text for the endpoint becomes "a fixed sentence, the cause is in the log". No user docs page documents the endpoint; if the crafter finds one, it gets the same line. |
| Not docs | ADR-185 dated note, the new ADR, ADR-270 glossary row "Database fault", `ARCHITECTURE.md` section 4 line, `jobs.yaml` REVISED note: already listed under "DELIVER owes". Screenshots: N/A, because no layout changed (only the text of an existing row, alert and snackbar). Demo data: N/A, because no data shape changed and no seeder is touched. Website marketing asset: N/A, because it is a fix of wording, not a capability. Release notes: written at `/release`, not at finalize. |

### AFK decisions (DEVOPS)

| # | Decision | Why |
|---|---|---|
| DO-1 | **Usage-data event: none.** `UsageDataEventName` untouched; 25 stays with Story 6501. | A failure report adds nothing to be used; avoids a collision on the integer with 6501 and 6502. |
| DO-2 | **New tests carry no `Integration` category;** container tests follow the existing `Tests/Integration/Containers/` pattern with `SharedPostgresContainer`, one role and database per test. | `ci_backend.yml` runs `Category!=Integration` by default; Docker is available on `ubuntu-latest`, as the existing container tests prove. |
| DO-3 | **Accept the full-Integration CI run caused by the `Program.cs` edit,** but schedule it: the `Program.cs` change goes in the last backend step of slice 04 and is pushed once. | `Program.cs` is on `SHARED_REGEX`; the Linear key is shared with CI, so repeated runs risk a 429 that reds the next build. Isolating it keeps slices 01 and 03 off the live connectors. |
| DO-4 | **No E2E spec change;** DELIVER confirms by search that none asserts a 5xx body. | The change shows only on a 5xx, which healthy E2E never produces; E2E stays a thin sanity check. |
| DO-5 | **No observability additions:** no metric, dashboard, alert or SLO. One Error log line per refused `/health/startup` probe is accepted. | Self-hosted single instance; the Task Manager is the existing signal; the log line keeps the cause that the fixed sentence hides. |
| DO-6 | **No migration; rollback is a revert.** | Code-only change; no persisted state depends on the new text. |
| DO-7 | **Lighthouse-Clients: no release, no changeset.** | Only the optional `problemCode` and `problemTitle` already in the client can newly appear. |
| DO-8 | **Security note goes into the release notes at `/release`, no advisory;** the draft above is a starting point. | Q7. |
| DO-9 | **Mutation: break 80 on both stacks;** backend config in this folder's `mutation/` directory, frontend config in this folder's `mutation/` directory too (see DO-13); both created and run in DELIVER, last, on frozen code; the container and acceptance suites are excluded from the backend run. | Project strategy per-feature; the story-6019 config is the model. |
| DO-10 | **Docs at finalize:** taskmanager.md, databasemanagement.md (Docker `/tmp` case), the `/health/startup` line in `brief.md`; screenshots, demo data and website N/A with reasons. | Per-feature rule; no silent N/A. |
| DO-11 (review M1) | **A refused `/health/startup` probe logs at Information, not Error.** | It stays in the log file but below the Warning threshold of Recent problems, so a polled probe cannot flood that list during an outage; the refresh-loop entries already put the same outage there. |
| DO-12 (review M2) | **Slices 01 and 03 edit no `.csproj`;** `Microsoft.Data.Sqlite` stays transitive. If a build rule ever needs an explicit reference, it goes in slice 04's last step with the `Program.cs` change. | A csproj edit matches `SHARED_REGEX` and would force the full live-connector run in CI earlier than needed (DO-3). |
| DO-13 (review M3, H2, L1 to L7) | **Mutation globs and plan corrected:** `DatabaseOperationWording` is mutated at `Services/Implementation/DatabaseManagement/`; the frontend config is `mutation/vitest.stryker.6500.config.ts` in this folder (gitignored in `Lighthouse.Frontend`, modelled on the 6249 one), `include` names the new `BaseApiService` spec, `disableTypeChecks: false`, `concurrency: 2`, one mutate entry per span, `BaseApiService.ts:46-65`. The three `Program.cs` registrations are named the same way everywhere. `PostgresContainerSharingGuardTest` is described as scanning only `StartFreshAsync(`, so the one-container rule is "kept by convention". A runtime estimate for the new Postgres container tests (about 15 to 30 s, warn-only budget) is in the CI table. | Doc-only fixes from the combined re-review. |

### Held for the maintainer

None. Nothing here has a large blast radius: no infrastructure, no migration, no contract break, no new workflow.

Handoff to DISTILL: the placement and category rules (DO-2), the 5xx-body search for E2E (DO-4), the mutation globs and config paths (DO-9), and the sketch walk-through at the start of DISTILL per CLAUDE.md. No nWave scenario tags in code.

## Maintainer decision on the follow-up (2026-11-02, evening)

The follow-up Bug for the unpaced retry loop (Q4, "Pause between failed update retries") is **not created**. The draft
stays here for reference only; it is not tracked in ADO.

## Maintainer decision: scope cut to the reported case (2026-11-04)

The design above was about four days of work for a Bug that should be small. The maintainer cut it to: translate the
one reported exception into plain language where the administrator reads it. Early detection stays out, as in Q5.

| Topic | Decision |
|---|---|
| What ships | `RecentProblemsSink` replaces the row text when the exception, or one inside it, is Postgres `53100` or SQLite error 13, using the sentences from the fault-class table. |
| Overrides | Q2 (every fault class, every site) and Q7 (information disclosure). |
| Not done | The other fault classes, slices 03 and 04, the classifier, `/health/startup`. They stay here for reference and are not tracked. |
| Waves | DISTILL and the roadmap skipped at the maintainer's word; a plain test-first fix in `RecentProblemsSinkTest`. |
| Activity section | Unchanged: a failed refresh shows `Failed` with no text, so no driver text reaches it. |

