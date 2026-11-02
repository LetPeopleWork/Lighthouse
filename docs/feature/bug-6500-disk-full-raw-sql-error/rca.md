# RCA: Bug 6500, a full database disk shows the bare database exception to the user

All paths are relative to the repository root. Line numbers are against `c995492a0` (branch
`worktree-vivid-greeting-honey`). Nothing was run live: this is a read-only code analysis. Where a claim
rests on library behaviour or on a run nobody did, it is marked **Hypothesis** and marked in the text.

## 1. Problem statement and scope

A community user ran Lighthouse on Postgres in Docker and the volume filled up. The UI showed the bare
driver text:

```
Exception data: Severity: ERROR  SqlState: 53100  MessageText: could not write to file
"base/pgsql_tmp/pgsql_tmp194.0": No space left on device  File: buffile.c  Line: 543  Routine: BufFileDumpBuffer
```

Maintainer's note: "bare sql issue popped up to user, not nice. should be better".

Two questions, kept apart:

- **(a)** Why did the raw exception text reach a user-visible surface at all?
- **(b)** Why is a full disk, a condition an operator can fix in minutes, not named as such anywhere, and
  why is nothing warning before it happens?

**In scope:** every path from a database exception to the browser (HTTP pipeline, controllers, background
updaters, the in-memory problems list, the log view, database-management operations); the SQLite
equivalent; recovery after the disk is freed; what operators get (logs, health, system info).

**Out of scope:** making Postgres itself safe against a full disk (`temp_file_limit`, volume sizing). That
is the operator's. Lighthouse can only name the condition and warn.

### What the text proves

`Exception data:` followed by `Severity:`, `SqlState:`, `MessageText:`, `File:`, `Line:`, `Routine:` is the
layout of `Npgsql.PostgresException.ToString()` (**Hypothesis**: from knowledge of the driver, the Npgsql
source was not opened). `ex.Message` alone is one line (`53100: could not write to file ...`) and carries
no `Exception data:` block. So whatever the user saw was a **whole exception rendered to text**, not
`ex.Message`. That narrows the candidates considerably (section 3).

`base/pgsql_tmp` is Postgres spilling a sort or hash to a temp file. The failing statement was a large
read, not a write of Lighthouse data.

## 2. Symptoms

- **S1.** Raw driver text (SqlState, routine, file, line) is shown to a user.
- **S2.** Nothing tells the user or the operator that the cause is a full disk, or what to do about it.
- **S3.** The same failure repeats without pause while the disk is full (found during the investigation,
  section 3, WHY 3A).
- **S4.** `/health/ready` stays green while the disk is full (section 3, branch C).

## 3. Toyota 5 Whys

### Branch A: how the raw text reaches the user (S1, S3)

There are four candidate channels. Three are ruled out or secondary; one matches the evidence.

| Channel | Carries raw DB text? | Verdict |
|---|---|---|
| HTTP request that throws inside a controller | No body at all, see below | Not the channel |
| Refresh status pushed over SignalR | No, enum only | Not the channel |
| **Recent problems list (Task Manager) and the log view** | **Yes, a whole exception is rendered into the message** | **Most likely** |
| Database backup / restore / clear operation status | Yes, `ex.Message` | Real, second leak, section 5 |

**HTTP pipeline (ruled out as the source of this text).**
`Lighthouse.Backend/Lighthouse.Backend/Program.cs:167-175` uses `UseDeveloperExceptionPage()` only in
Development, otherwise `UseExceptionHandler("/Error")`. The shipped image and the Postgres example
compose file set no `ASPNETCORE_ENVIRONMENT` (`examples/postgres/docker-compose.yml:16-38`, and a repo-wide
grep finds `Development` only in `launchSettings.json` and `.vscode/launch.json`), so a Docker user runs
Production. No controller or endpoint is mapped at `/Error` (grep of `*.cs` finds the string only at
`Program.cs:173`), no `AddProblemDetails` / `IExceptionHandler` is registered, and the only exception
filters are `ConcurrencyConflictExceptionFilter` and `DeliveryArchivedExceptionFilter`
(`Program.cs:303-304`) for two specific types. So an unhandled `PostgresException` in a request becomes a
500 whose body is either empty or the SPA shell (the re-executed `/Error` falls through to `UseSpa`,
`Program.cs:247-263`; **Hypothesis**: which of the two needs one live request to confirm). Either way the
frontend shows a generic message, not SQL text: `BaseApiService.createApiErrorFromAxios` uses the
payload's `message`/`detail`, else axios's `Request failed with status code 500`
(`Lighthouse.Frontend/src/services/Api/BaseApiService.ts:46-65, 118-149`), and `assertNotHtmlResponse`
turns an HTML body into "Lighthouse received a web page where data was expected"
(`.../htmlResponseGuard.ts:9-26`). Not pretty, but not the reported text.

**SignalR / update status (ruled out).** `UpdateStatus` holds only `UpdateType`, `Id`, an
`UpdateProgress` enum and two timestamps (`Services/Implementation/BackgroundServices/Update/UpdateStatus.cs:5-24`).
A failed refresh is `Failed`, with no text. The refresh log's `Reason` is filled only for
`UnreadableSecretException`, `WorkTrackingRefusedException`, `WorkTrackingReadException`
(`.../Update/TeamUpdater.cs:71-93`, `.../Update/PortfolioUpdater.cs:136-156`); a DB exception gets none.

**The matching channel: the in-memory Recent problems list.**

- **WHY 1A.** The user saw a whole `PostgresException` rendered as text, in a surface meant for operators.
  [Evidence: `LoggingConfigurator.CreateLogger` sends every Warning-or-worse event to `RecentProblemsSink`
  (`Startup/LoggingConfigurator.cs:44`); the sink stores `logEvent.RenderMessage(...)` as the problem's
  `Message` (`Services/Implementation/Logging/RecentProblemsSink.cs:83-90`); `LogsController.GetRecentProblems`
  returns it (`API/LogsController.cs:44-48`); the Task Manager prints `problem.message` verbatim
  (`Lighthouse.Frontend/src/components/App/Header/TaskManager/RecentProblemsSection.tsx:59-76`), clamped to
  two lines with the full text in the hover title. A rendered *message* contains an exception only if the
  exception was passed as a template argument.]
- **WHY 2A.** One logging call does exactly that. [Evidence:
  `Services/Implementation/BackgroundServices/Update/UpdateServiceBase.cs:295`,
  `Logger.LogError(exception, "An exception occured: {Exception}.", exception);`. The `{Exception}` argument
  is the exception object itself, so Serilog renders its `ToString()` into the message: type, message, stack
  and, for `PostgresException`, the `Exception data:` block.] The same file and its siblings pass
  `exception.Message` into the template instead (`UpdateServiceBase.cs:71`, `ForecastUpdater.cs:275,368`,
  `UpdateQueueService.cs:437,563`, `WriteBackService.cs:76`, `WriteBackTriggerService.cs:56,99,189`), which
  also puts driver text into the Recent problems list, but without the `Exception data:` block.
- **WHY 3A.** That catch is reached by the periodic refresh loop, and a database that cannot finish a big
  read reaches it on every pass. [Evidence: `UpdateServiceBase.TryUpdating` (`:281-296`) calls
  `UpdateAll()`, which runs `repository.GetAll().ToList()` (`:300-311`). For Portfolios that is
  `GetAllProjectsWithIncludes()` (`Services/Implementation/Repositories/PortfolioRepository.cs:90-94,159-168`):
  Portfolio, Features, FeatureWork, Team connection options, Forecasts and **SimulationResults**, the
  largest table, each as a split query that sorts by key. This is the best candidate for a sort that spills
  to `pgsql_tmp` (**Hypothesis**: the failing statement is not named in the text; any large metrics or
  forecast read behaves the same). It runs on every updater loop, whether or not anyone has a page open,
  which is why a user who did nothing special saw it.]
  The catch also has **no delay**: `Task.Delay(...)` sits inside the `try` after `UpdateAll()`
  (`:291`), so when `UpdateAll()` throws, control returns to `while (!stoppingToken.IsCancellationRequested)`
  (`:273-276`) and the loop runs again at once. Postgres is protected a little by
  `EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: 5 s)` (`Data/DatabaseConfigurator.cs:35-38`); SQLite
  has no retry (`:75`, `:83-87`) and would spin. Each pass writes a full stack trace to the log file and a
  new entry to the 200-entry Recent problems queue (`RecentProblemsSink.cs:21,57-70`), so one outage pushes
  every other problem out of the list within minutes and grows the log on the disk that is already full
  (daily files, `appsettings.json` `Serilog.WriteTo.File`: no size cap or `rollOnFileSizeLimit` set; the
  Serilog default of 31 retained files applies, **Hypothesis**).
- **WHY 4A.** Nothing between "an exception was caught" and "text a human reads" decides what the reader
  should see. [Evidence: there is one error-to-text path and it is the log renderer. No type or
  classification exists for infrastructure faults: a repo search for `DiskFull`, `53100`, `SQLITE_FULL`,
  `SqliteErrorCode`, `NpgsqlException`, `DbException` finds one use of `DbException`
  (`API/SystemInfoController.cs:80`, an RBAC fallback) and none that translates a driver error. Work
  tracking failures do have this layer (`WorkTrackingRefusedException`, `WorkTrackingReadException`,
  `BuildRefusalReason`, `UpdateServiceBase.cs:187-190`); the Lighthouse database does not.]
- **WHY 5A. ROOT CAUSE A.** The product treats its own database as infallible plumbing. Failures of the
  *tracker* are modelled, worded and shown with care; failures of the *store* are not modelled at all, so the
  only thing a catch block can pass on is the exception itself, and the operator-facing surfaces (Recent
  problems, log view) render whatever they are given. There is no rule saying "the message shown to a human
  is chosen, never rendered from an exception", and nothing enforces one.

### Branch B: a second, deliberate leak of exception text (S1, general)

- **WHY 1B.** Backup, restore and clear operations show `ex.Message` in the Settings alert.
  [Evidence: `Services/Implementation/DatabaseManagement/DatabaseManagementService.cs:81,131,184`,
  `tracker.TransitionToFailed(operationId, ex.Message)`; the page renders `result.failureReason`
  (`Lighthouse.Frontend/src/pages/Settings/DatabaseManagement/DatabaseManagementSettings.tsx:85,134,164,258`).
  The text includes `pg_dump`/`pg_restore` standard error verbatim
  (`PostgresDatabaseManagementProvider.cs:106,136,170`) and any `IOException`.]
- **WHY 2B.** A backup writes a temp directory and a zip to local disk first
  (`DatabaseManagementService.cs:60-67`), so **a full disk is the most likely way to hit this path**.
- **WHY 3B.** Several controllers also return `ex.Message` as the response body:
  `DatabaseManagementController.cs:37,52,78,82,96`, `LicenseController.cs:65,80`
  (`$"Error processing license file: {ex.Message}"`), `BlackoutPeriodsController.cs:36-72`,
  `RecurringBlackoutRulesController.cs:36-72`. The frontend shows a string body as the message
  (`BaseApiService.ts:80-82`). Most of these catch specific domain exceptions with controlled text and are
  fine; the `LicenseController` and database-management ones catch broad types.
- **WHY 4B.** Each author reasoned locally ("show the user why it failed") and no shared rule or analyzer
  separates "message I wrote" from "message that came from a library". [Evidence: the `catch (Exception)`
  sites carry `#pragma warning disable CA1031` with justifications (e.g. `UpdateServiceBase.cs:64-69`)
  that explain why not to rethrow, never what to show.]
- **WHY 5B. ROOT CAUSE B** (same family as A, different surface). No single place classifies a caught
  exception into a user-facing message; each catch decides alone, and exception text is the path of least
  effort.

### Branch C: why a full disk is neither named nor warned about (S2, S4)

- **WHY 1C.** Neither the user nor the operator is told "the database disk is full". [Evidence: no code
  names the condition (WHY 4A); the only artefact is the exception text.]
- **WHY 2C.** The health checks cannot see it. [Evidence: `/health/ready` runs `DatabaseConnectivityHealthCheck`
  (`Health/DatabaseConnectivityHealthCheck.cs:16-28`), which calls `CanConnectAsync`, and
  `MigrationsAppliedHealthCheck` and `DrainReadinessHealthCheck` (`Program.cs:1044-1047`). A full disk lets a
  connection open, and small reads and writes can still succeed (this failure was a *temp-file spill*, not
  a write to the data directory), so the probe is green. The compose healthcheck uses `/health/ready`
  (`examples/postgres/docker-compose.yml:48`), and the Postgres one uses `pg_isready`
  (`:15`), also green.]
- **WHY 3C.** No capacity signal exists anywhere in the product. [Evidence: a repo search for
  `DriveInfo`, `AvailableFreeSpace`, `disk` finds nothing in the backend; `SystemInfo` reports version,
  auth and process facts (`API/SystemInfoController.cs:46-56`, `Services/.../SystemInfoService`). The log
  rolls daily with no size guard.]
- **WHY 4C.** Lighthouse can only see its own volumes. The Postgres data volume is another container's
  filesystem, so a free-space probe on the app's disk would not have helped this user. The one signal the
  app does have is the database itself: Postgres can report `pg_database_size` and the failing SqlState.
- **WHY 5C. ROOT CAUSE C.** "Resource exhaustion of the store" was never a designed-for failure mode, so
  there is neither a name for it, a place it shows, nor a leading indicator. It is a gap, not a regression:
  the same holds for out of memory (`53200`), too many connections (`53300`) and a read-only database.

### Completeness check (other places looked at)

- `ForecastUpdater`, `PortfolioUpdater`, `TeamUpdater`, `OverTimeHistoryFiller`, `ConnectionHealthProber`:
  each logs the exception as `LogError(exception, "fixed text")`. That form puts the exception in the log
  file only, not into the Recent problems message, and is the safe pattern. Only the call sites listed in
  WHY 2A put exception text into a message.
- Frontend: `SnackbarErrorHandler` shows any `Error.message` or unhandled rejection
  (`SnackbarErrorHandler.tsx:81-83,131,147`). It is a last-resort sink, and shows whatever an `ApiError`
  carries; it adds no text of its own.
- `ConnectionsSection.tsx:109-116` prints `connection.message`; its source is the connection verdict, not DB
  text. Not affected.
- The log view (System info tab, `LogsController.GetLogs`) shows the file with `{@x}` full exceptions
  (`appsettings.json` File template). That is by design an operator surface and System Administrator only
  (`LogsController.cs:19`). Raw text there is expected; the defect is in what the *Task Manager*, a surface
  for every administrator at a glance, is fed.

### Alternative hypotheses considered

| Hypothesis | Verdict |
|---|---|
| Dev exception page was on | Eliminated for the Docker image; would also show a stack and request data, not a clamped line. Not provable for this user's own compose file. |
| A controller returned `ex.Message` for the query | Eliminated for the data reads: they do not catch. `PostgresException.Message` also has no `Exception data:` block. |
| The user was looking at the log view | Possible and consistent (the file uses `{@x}`), but that is a System Administrator surface and not "popped up". Branch A covers both because the cause is the same call. |

## 4. SQLite

Equivalent: `SQLITE_FULL` (result code 13, text `database or disk is full`) is the only disk-full code;
`SQLITE_IOERR` (10) covers many I/O failures and is not read as disk-full. `SQLITE_CANTOPEN` (14) is the
unreachable case. `Microsoft.Data.Sqlite` throws `SqliteException` with `SqliteErrorCode == 13`
and `Message` `SQLite Error 13: 'database or disk is full'.`. It surfaces by the very same channels: the
same updater loop (`UpdateServiceBase.cs:295`), the same Recent problems feed, the same
`DatabaseManagementService` `ex.Message`. Differences that matter:

- No `EnableRetryOnFailure` for SQLite (`DatabaseConfigurator.cs:75`, `:83-87`), so the updater loop in
  WHY 3A spins with no retry delay at all.
- WAL mode (`PRAGMA journal_mode=WAL`, `:68`) means the `-wal` file and checkpoints need space too, and
  large sorts use the temp store; a full disk fails reads that sort as well as writes.
- The message is a single short sentence (no `Exception data:` block), so the leak is less dramatic, but
  still unworded and still unexplained to the user.
- SQLite's database file lives on the app's own `/app/data` volume (compose `lighthouse-data`), so for
  SQLite a free-space probe on the app disk *would* see the problem. For Postgres it would not (WHY 4C).

## 5. Scope boundary

| Question | Answer |
|---|---|
| Resource-exhaustion only? | No. The leak is general. |
| General "raw exception text reaches the user"? | Yes, in a bounded set of places (table below). |
| Information-disclosure concern? | Yes, modest. The Recent problems feed and log are System Administrator only (`LogsController.cs:19`), but the feed is shown in a Task Manager popover, and `LicenseController` / database-management return `ex.Message` to the caller. Driver text can carry SQL fragments, table names and, with detailed errors on, parameter values. No stack reaches an anonymous caller in Production. |

| Site | Reader | Text |
|---|---|---|
| `UpdateServiceBase.cs:295` | Admin, Task Manager and log | Whole exception (`ToString`) |
| `UpdateServiceBase.cs:71`, `ForecastUpdater.cs:275,368`, `UpdateQueueService.cs:437,563` | Admin, Task Manager and log | `ex.Message` |
| `WriteBackService.cs:65,76`, `WriteBackTriggerService.cs:56,99,189` | Admin and the write-back result | `ex.Message` (partly tracker text, by intent) |
| `DatabaseManagementService.cs:81,131,184` | Caller of backup/restore/clear | `ex.Message`, tool stderr |
| `LicenseController.cs:65,80` | Caller | `ex.Message` |
| Controllers catching domain exceptions (`Blackout*`, `DatabaseManagementController`) | Caller | Controlled domain text, acceptable |

Connector failures (Jira, ADO, Linear, ServiceNow) put `ex.Message` into refresh reasons on purpose: that is
the tracker's own wording and the product shows it deliberately. They are **out of scope**.

## 6. Recovery after the disk is freed

- **No permanent damage from this failure.** A temp-file `53100` is an ERROR that aborts the statement; it is
  not a data-directory or WAL failure. Once space is free the next loop pass succeeds, the refresh queue
  carries on, and `UpdateStatus` returns to `Completed` for later runs (`UpdateQueueService.cs:610-650`
  sets `Failed` per run only; nothing is latched). (**Hypothesis**: a full disk during a WAL write can make
  Postgres PANIC and need crash recovery; that is Postgres's behaviour, not Lighthouse's.)
- **Stays wrong until restart.** The Recent problems queue is in memory only, has no clear or expiry
  (`RecentProblemsSink.cs:48-70`), so the old entries, up to 200 of them, remain visible after recovery.
  The Task Manager keeps saying something has gone wrong, with the raw text, until the process restarts.
- **Possibly stays wrong in data.** Write-back and a refresh that died mid-way leave the stored data one
  round behind; the next successful round heals it. Nothing is recorded as "last refresh failed because the
  store was full", so the operator cannot tell which rounds were lost.
- **Log file growth is not undone.** The spin in WHY 3A can leave a very large day log behind.

## 7. Backwards-chain validation

- A: a read that spills to temp, no space, `PostgresException`, caught at `UpdateServiceBase.cs:293`,
  rendered with `{Exception}`, sink stores the rendered message, Task Manager prints it. Produces S1 and
  explains the exact `Exception data:` layout. Produces S3 because no delay follows the catch.
- B: independently produces S1 for backup/restore/clear and explains why it is cheap to repeat.
- C: produces S2 and S4 (green health, no wording, no leading indicator).
- No contradictions between branches. The one gap is that the user's exact surface is inferred, not
  reproduced (section 11, question 1).

## 8. Candidate fix directions

### Direction 1: a database-fault classifier, used at every catch that shows text (recommended)

One small component maps a caught exception to a typed result, for example `StoreFault` with a kind
(`OutOfSpace`, `OutOfMemory`, `TooManyConnections`, `ReadOnly`, `Unreachable`, `Unknown`) and a fixed,
worded sentence per kind ("The database ran out of disk space. Free space on the database volume; Lighthouse
retries on its own."). Recognises Postgres by SqlState (`53100`, `53200`, `53300`, `25006`) and SQLite by
`SqliteErrorCode` 13/10. Call sites in section 5 use the sentence, never the exception, and log the
exception with `LogError(exception, "fixed text")`. Add a hold-off to `TryUpdating` so the loop waits before
retrying, and have it stop logging an identical error every pass (log on change, then at intervals).

- Trade-offs: a new seam and a small mapping table to maintain; unknown faults still need a safe default
  ("Lighthouse could not use its database. Details are in the log."). Solves A, B and the spin, and gives
  C something to hang a banner on. Medium effort, one backend slice plus a frontend wording check.

### Direction 2: sanitise only at the sink and at the two return sites

Make `RecentProblemsSink` store only a short, type-based message when an exception is attached
(`ExceptionType` already exists, `RecentProblem.cs:15`), fix `UpdateServiceBase.cs:295` to drop the
`{Exception}` argument, and replace `ex.Message` in the database-management and licence paths with fixed
text.

- Trade-offs: smallest diff and closes the disclosure, but the user still sees "An exception occured" with no
  cause, which fails the maintainer's "should be better"; adds no wording and no early warning; the spin
  remains unless the delay is also added. Cheap, treats the symptom only.

### Direction 3: Direction 1 plus an early-warning signal

Add a `StoreCapacity` health check (Postgres: `pg_database_size` against a configured ceiling, plus the
volume free-space where the app can see it, e.g. SQLite's `/app/data`), surfaced in `/health/ready` details
and as a warning in the Task Manager before the disk is full. A one-line operator note in the docs about
`temp_file_limit` and volume sizing.

- Trade-offs: the only fix for the "warn before" part, but for Postgres the app cannot see the other
  container's disk, so the check is partial there and needs a new setting; risk of a noisy or false
  readiness failure that restarts a healthy container. Larger scope, should be its own slice or its own
  story.

### Recommendation

Do Direction 1 as the bug fix, because it addresses the root causes of both leaks and removes the spin.
Split the early-warning part of Direction 3 into a separate Story, since it needs product decisions
(thresholds, readiness semantics) a bug fix should not carry. Keep Direction 2's rule as a guard inside
Direction 1: nothing shown to a human is rendered from an exception, and a test asserts it for the sites in
section 5.

## 9. Files affected (recommended shape)

- `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/BackgroundServices/Update/UpdateServiceBase.cs`
  (`TryUpdating` catch and delay, `:71`)
- `.../Update/ForecastUpdater.cs:275,368`, `.../Update/UpdateQueueService.cs:437,563`
- `.../Services/Implementation/WriteBackService.cs:65,76`, `.../WriteBackTriggerService.cs:56,99,189`
- `.../Services/Implementation/DatabaseManagement/DatabaseManagementService.cs:81,131,184`
- `.../API/LicenseController.cs:65,80`
- `.../Services/Implementation/Logging/RecentProblemsSink.cs` (what message is kept when an exception is
  attached)
- New: the fault classifier and its wording, near `Services/Implementation`
- Frontend: only if a new fixed sentence needs a place in `RecentProblemsSection.tsx`
- Tests: classifier by SqlState and `SqliteErrorCode`; no `Exception data:` in the Recent problems message for
  a thrown `PostgresException`; updater waits after a failed pass

## 10. Risk assessment

- Hiding the exception from the Task Manager must not hide it from the log. The log file stays the full
  record; only the at-a-glance surfaces change.
- The classifier must default safe: an unrecognised exception gets a generic sentence, never its text.
- A hold-off in `TryUpdating` changes timing of retry and of tests that depend on it; pick an interval from
  the existing `RefreshSettings` rather than a new constant.
- Existing tests may assert on the current message wording (`RecentProblemsSinkTest`, `LogsControllerTest`).

## 11. Open questions for the maintainer

1. **Which surface did the user see it on?**
   Options: (a) Task Manager "Recent problems", (b) the log view in System info, (c) the Settings database
   backup alert, (d) a snackbar. My analysis says (a) is the best match to the `Exception data:` layout
   (`UpdateServiceBase.cs:295`), (b) next. Recommendation: ask the community user for a screenshot or the
   page before DISCUSS; the fix is the same for (a) and (b), but (c) or (d) would change the scope.

2. **How wide is the fix?**
   Options: (A) only the disk-full case, (B) every database fault class (space, memory, connections,
   read-only, unreachable), (C) every site that renders exception text to a human (section 5).
   Recommendation: (B) for the classifier plus (C) for the call sites in section 5, since they are one
   change and one rule. Connector text stays as it is.

3. **What should the user-facing wording say, and who is it for?**
   Options: (a) one plain sentence for every reader ("The database ran out of disk space. Free space on the
   database volume; Lighthouse retries on its own."), (b) plain sentence plus an expandable "Details" holding
   the raw text for System Administrators, (c) plain sentence only, details in the log. Recommendation: (c).
   The log already is the details view, and a collapsible raw block keeps the disclosure concern.

4. **Is the unpaced retry loop in scope of this bug?**
   Options: (a) fix it here, since it produces the repeated text and the log growth that makes a full disk
   worse; (b) log it as its own Bug. Recommendation: (a), a delay and a log-on-change rule are part of
   showing one calm message instead of hundreds.

5. **Early warning before the disk is full: this bug or a Story?**
   Options: (a) skip, (b) separate Story with a capacity health check and a Task Manager warning,
   (c) include in this bug. Recommendation: (b). It needs decisions on thresholds, on whether it may turn
   `/health/ready` red (a restart will not free disk), and on Postgres being invisible from the app.

6. **Should the Recent problems list clear itself once the condition is gone?**
   Options: (a) leave as is (process restart clears it), (b) drop an entry when the same source reports
   success afterwards, (c) add a Clear button for System Administrators. Recommendation: (a) for this bug;
   (b) belongs with question 5.

7. **Is the information-disclosure angle handled as a Bug or does it need a security note?**
   Options: (a) treat as part of this bug, no advisory; (b) also check the other `ex.Message` returns in
   section 5 for what they can carry and note it in the release notes; (c) separate security review.
   Recommendation: (a), then (b) at `/release`. Everything affected is behind a login, and System
   Administrator for the log and problems feeds.

8. **Do you want a live check of the HTTP 500 path before DESIGN?**
   Options: (a) no, (b) one local request against a Production-mode build with a forced DB failure to see
   whether the body is empty or the SPA shell. Recommendation: (b), a small probe, because the user-facing
   wording for an ordinary page load that hits a full disk (not the background loop) is a separate surface
   from the one in this report and may need its own sentence.
