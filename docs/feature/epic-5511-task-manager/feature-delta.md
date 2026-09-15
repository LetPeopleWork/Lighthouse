<!-- markdownlint-disable MD024 -->
# Feature Delta — epic-5511-task-manager

ADO Epic **#5511 "Task Manager"** (Planned, Size 6, Priority 2, tags `Community; Documentation;
nwave-discuss; nwave-design; Release Notes`, board column Planned, no target date). Board read
2026-09-14; at DISCUSS time on 2026-08-23 it was New, Size 2, column Options.

Children absorbed into this Epic — the six slices, all New / Backlog as of 2026-09-14:

- **#5788** — "A scheduled refresh that failed is reported to the browser as completed" (Bug) — slice 01
- **#5840** — "Slice 02: See what Lighthouse is doing right now" (User Story) — slice 02
- **#5841** — "Slice 03: See how long an update has been running" (User Story) — slice 03
- **#5842** — "Slice 04: Cancel a queued or running update" (User Story) — slice 04
- **#5019** — "I get warned when any connection's authentication breaks, not just OAuth" (User Story) — slice 05
- **#5843** — "Slice 06: Recent warnings and errors without opening the log" (User Story) — slice 06

Predecessor **#5733 Opt-In Usage Data** (Resolved) — not a build dependency; see Pre-requisites.
Related **#5502 Event-driven write-back collection** (Closed) — its write-back rounds are a *deferred*
surface here, see Out of Scope.
Related **#5877 "Update queue is a single lane"** (User Story, New) — raised 2026-08-31, after DESIGN.
A field report whose root cause is the subsystem this Epic instruments; see Post-DESIGN Reconciliation
at the foot of this file.
Successor **#5510 Sizing Poker** — unrelated, board ordering only.
Predecessor **#5512 GitLab Integration** — **Removed** on the board; a dead link, not a dependency.

Wave DISCUSS run 2026-08-23. Cold DISCUSS — no DISCOVER or DIVERGE artifacts existed for this Epic.
Grounded in an ADO read of the Epic plus its five linked items, and a code reality check of the update
pipeline, the status stores, the log configuration and the header (see Current-State Surface Inventory).

Density: `lean` + `ask-intelligent` — Tier-1 `[REF]` only. Triggers that fired are listed in the
expansion menu at the foot of this file.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role in this Epic |
|---|---|
| `platform-operator` | **Primary.** Runs the instance. Wants to know what the refresh pipeline is doing right now, why something is not moving, and how to stop it when it is doing harm. Both flavours — the standalone self-hoster and the LPW SaaS operator — ask the same question. |
| `config-admin` | **Primary for connection health.** Owns the work-tracking Connections. Today learns that a credential died by noticing a flat throughput chart. |
| `lighthouse-maintainer` | Secondary. Dogfoods on Tenant Zero; this is the surface that tells them a tenant's sync is wedged before the tenant reports it. |

With authentication disabled every caller is `lighthouse|auth-disabled` and therefore a System
Administrator, so the standalone single-container product gets the whole surface with nothing to
configure. That is deliberate: a standalone operator is exactly who has nowhere else to look.

---

## Wave: DISCUSS / [REF] JTBD One-Liners

| Job ID | One-liner |
|---|---|
| `job-operator-trust-that-a-finished-refresh-tells-the-truth` | When a scheduled refresh ends, I want the answer I am shown to be the answer that happened, so I do not build an operational habit on a status that is decorative. |
| `job-operator-see-what-lighthouse-is-doing-right-now` | When I am wondering whether Lighthouse is working or wedged, I want to see what is running and what is waiting without leaving the page I am on, so the question costs me a glance instead of a log download. |
| `job-operator-stop-a-refresh-that-is-doing-harm` | When a refresh is hammering a tracker I need quiet, or is chewing through a rate limit I share with other tools, I want to stop it from inside Lighthouse, so my only options are not "wait it out" or "restart the container". |
| `job-config-admin-know-any-credential-is-failing-not-just-oauth` | When a connection's credential stops working — a revoked token, an expired PAT, a deprovisioned owner — I want Lighthouse to say so regardless of which authentication method that connection uses, so I fix it before the team notices the data froze. |
| `job-operator-read-the-warnings-without-reading-the-log` | When something has gone wrong repeatedly, I want the warnings and errors surfaced where I already look, so noticing does not depend on me choosing to read a log file. |

Full JTBD narrative — dimensions, four forces, opportunity scores — lives in `docs/product/jobs.yaml`.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Established by reading the code before writing requirements. Every decision below rests on these.

| # | Fact | Evidence |
|---|---|---|
| S1 | **`IUpdateStatusStore` cannot be enumerated.** It offers `TryAdmit`, `Advance`, `Requeue`, `TryGet`, `Remove`, `HasActiveWork()` and `HasQueuedWork(keys)`. There is no "list everything admitted". A task list is therefore a *new capability on the port*, not a new read of an existing one. | `Services/Interfaces/Update/IUpdateStatusStore.cs` |
| S2 | **`UpdateStatus` carries three fields** — `UpdateType`, `Id`, `Status`. No entity name, no queued-at, no started-at, no duration, no failure reason. Everything a task list wants to show beyond "Team 12 is running" does not exist. | `Services/Implementation/BackgroundServices/Update/UpdateStatus.cs` |
| S3 | **The Redis store persists exactly one integer per key** — the `UpdateProgress` ordinal, in the hash `lighthouse:update-status`, and reconstructs `UpdateStatus` from key + ordinal in `StatusFor`. Two Lua scripts (`MonotonicAdvanceScript`, `RequeueIfAdmittedScript`) do `tonumber()` on that value. Any field added to `UpdateStatus` must survive that representation, or multi-replica loses it. | `RedisUpdateStatusStore.cs` |
| S4 | **`UpdateController` bypasses the port.** It injects the raw `ConcurrentDictionary<UpdateKey, UpdateStatus>` and counts it directly. Under Redis with more than one replica this answers only about the calling pod, so `/api/latest/update/status` is already wrong in the multi-replica product. | `API/UpdateController.cs:12,19` |
| S5 | **`UpdateProgress.Failed` is unreachable from any periodic refresh.** `UpdateServiceBase.TriggerUpdate` wraps `Update()` in its own `try/catch(Exception)`, logs, and swallows. The enqueued lambda therefore returns normally, so `RunUpdateAsync`'s catch never fires and it advances to `Completed`. `NotifyListeners` pushes `Status=Completed` over SignalR. This is Bug #5788. | `UpdateServiceBase.cs:29-41`, `UpdateQueueService.cs` (`RunUpdateAsync`) |
| S6 | **The RefreshLog row disagrees with the browser.** The `finally` in the updater persists `Success = false` and logs `Update completed \| … \| success=False`, both correct. Only the SignalR path lies. The two records already exist and already contradict each other. | `UpdateServiceBase.cs` (`WriteSummary`), `Models/RefreshLog.cs` |
| S7 | **`RefreshLog` stores `Success` as a bare `bool`.** There is no failure reason column. "It failed" is recordable today; "why" is not. | `Models/RefreshLog.cs` |
| S8 | **Nothing anywhere can be cancelled.** `IUpdateQueueService` has no cancel; `UpdateQueueService` holds no `CancellationTokenSource`; the queue is an unbounded `Channel<Func<Task>>` with a single reader loop. `EnqueueAndAwaitAsync`'s `cancellationToken` cancels *the caller's wait*, not the work. | `UpdateQueueService.cs` |
| S9 | **`IWorkTrackingConnector` takes no `CancellationToken` on any of its 16 methods.** Cooperative cancellation cannot reach inside a connector call without changing that port. | `Services/Interfaces/WorkTrackingConnectors/IWorkTrackingConnector.cs` |
| S10 | **The wall-clock is inside the connector.** Epic #5687's Data-Center dogfood went 468 856 ms → 2 087 ms by changing how the connector pages, with an identical scanned set. So the time a refresh spends is overwhelmingly connector paging — which is precisely where S9 says a token cannot currently reach. | `docs/feature/epic-5687-faster-updates/` |
| S11 | **`UpdateType` has five members** — `Team, Features, Forecasts, PortfolioDelete, TeamDelete` — while the frontend's union type has three (`"Team" \| "Features" \| "Forecasts"`). A list of everything admitted will surface the two delete types the UI has never had to name. | `UpdateType.cs`, `UpdateSubscriptionService.ts:5` |
| S12 | **A queued row can be queued for three different reasons and the store cannot tell them apart.** Genuinely waiting behind the single-reader loop; parked in `heldUpdates` by `HoldUntilQueuedWorkClears`; or `Requeue`d as a coalesced follow-up. All three read as `Queued`. | `UpdateQueueService.cs` (`heldUpdates`, `pendingReruns`, `TryScheduleRerun`) |
| S13 | **`DatabaseMaintenanceGate` drops triggers silently.** `IsBlockedByDatabaseMaintenance` logs at Information and returns — the update is never queued and leaves no trace a user can see. | `UpdateQueueService.cs` (`IsBlockedByDatabaseMaintenance`) |
| S14 | **Health is OAuth-only by construction.** `OAuthHealthAggregator` reads `IRepository<OAuthCredential>`, groups by connection, and counts anything not `Valid`. A PAT or API-token connection has no `OAuthCredential` row at all, so it is invisible to it — and `WorkTrackingSystemConnectionDto.RequiresReconnect` is likewise computed from `OAuthCredential.Status` alone. This is exactly the #5019 gap. | `OAuthHealthAggregator.cs`, `WorkTrackingSystemConnectionDto.cs:56` |
| S15 | **Auth failure is not distinguishable at the updater boundary.** `TriggerUpdate` catches bare `Exception`. The only typed failure that survives is `UnreadableSecretException` (encryption, not authentication) and `OAuthCredentialNotValidException`. A Jira 401 arrives as an untyped exception. | `UpdateServiceBase.cs:29-41`, `BuildUnreadableSecretReason` |
| S16 | **The header already carries the pattern.** `OAuthHealthIcon` is an `IconButton` + MUI `Badge`, gated on `isSystemAdmin`, returning `null` when there is nothing to say, and it *navigates away* on click (`/connections/{id}/edit`). It renders in both the mobile and desktop branches of `Header.tsx`. | `OAuthHealthIcon.tsx`, `Header.tsx:124,161` |
| S17 | **There is no structured log store.** Serilog writes to a rolling *text file*; `SerilogLogConfiguration.GetLogs()` finds the newest `*.txt` and `ReadToEnd()`s it into a string. `LogsController` returns that string. There is nothing to query by level, and no in-memory sink. | `SerilogLogConfiguration.cs`, `LoggingConfigurator.cs`, `API/LogsController.cs` |
| S18 | **`LogsController` is already `SystemAdmin`-guarded**, with a comment recording that it was unguarded until 2026-08-06 and that after ADR-137 "authenticated" includes every viewer who reaches the Jira frame. The log is instance-wide and carries team names, work-tracking URLs and connector errors. | `API/LogsController.cs:10-18` |
| S19 | **`SystemInfoController.GetRefreshLog` is `SystemAdmin`-guarded too**, with the same reasoning written out. Refresh history is already treated as administrator-only operational detail. | `SystemInfoController.cs` (`refreshlog`) |
| S20 | **The live channel already exists.** `UpdateNotificationHub` has a `GlobalUpdates` group that every connected client joins, and `UpdateQueueService.NotifyListeners` fires `GlobalUpdateNotification` into it on every status change. A popover needs no new transport. | `UpdateNotificationHub.cs:12,22`, `UpdateQueueService.NotifyListeners` |
| S21 | **`team`, `teams`, `portfolio`, `portfolios`, `feature`, `features`, `workTrackingSystem`, `workTrackingSystems` are configurable Terminology keys.** Every row label and section heading below renders the tenant's word. | `Seeding/TerminologySeeder.cs` |
| S22 | **Settings already has seven tabs**, including `System Info` (which hosts `RefreshHistorySection`, the RefreshLog table) and a log viewer under `LogSettings`. The material a task manager wants is scattered across two of them and neither is live. | `pages/Settings/Settings.tsx`, `pages/Settings/SystemInfo/`, `pages/Settings/LogSettings/` |

S23–S25 were added on 2026-09-14 when #5877 was reconciled in. They are verified the same way as the
rest — read from the code as it stands today, not taken from the bug report's own analysis.

| # | Fact | Evidence |
|---|---|---|
| S23 | **The update queue is one lane, and every update type shares it.** `UpdateQueueService` holds a single `Channel.CreateUnbounded<Func<Task>>()` drained by one `await foreach (… ReadAllAsync())` loop that awaits each task to completion before taking the next, and it is registered as a singleton. Teams, Portfolios and Forecasts therefore queue behind each other: one slow Portfolio refresh starves every Team refresh. | `UpdateQueueService.cs:11,442`, `Program.cs:1505` |
| S24 | **`GetParentFeaturesDetails` is the one by-key Jira fetch that does not chunk.** It passes the whole id list into `PrepareIssueKeyQuery` in a single query. Its own phase-1 twin `SweepParentFeatures` chunks at `ReferenceIdsPerQuery` (200), and the by-reference Feature fetch chunks too — carrying a comment that says why: every key is another OR clause in a URL that has to survive whatever proxy sits in front of Jira. | `JiraWorkTrackingConnector.cs:327-333` vs `:312,342`, `ReferenceIdsPerQuery` at `:50` |
| S25 | **Nothing bounds an update's total wall-time and nothing names what holds the lane.** The consumer loop awaits each task inside a bare `try/catch` that logs only "Error processing update task". A refresh that never returns occupies the lane indefinitely, and no log line identifies the `UpdateKey` responsible. | `UpdateQueueService.cs:442-452` |

∴ **S5 + S6 are the reason nothing can be built first.** A list that reports the same lie in five more
places is worse than no list. **S1 + S2 + S3 are the shape of the build** — the port must learn to
enumerate, and anything richer than an ordinal has to survive Redis. **S9 + S10 are the honest risk on
cancellation**, and they are why slice 04 opens with a probe rather than an estimate. **S14 + S15 are
the #5019 gap and its cost**: generalising health is not a rename, it needs a signal that does not
exist yet. **S17 is why the warning feed is a new sink, not a query.**

∴ **S23 + S25 are why #5877 is this Epic's problem and not a neighbour's.** A single lane with no
watchdog is precisely the condition under which an operator cannot tell "wedged" from "slow" — the
question slices 02 and 03 exist to answer — and S23 makes the starvation invisible in exactly the way
S12 already makes a held row invisible. **S24 is separable from all of it**: a one-line fix to a Jira
paging asymmetry, sharing only the bug report that found it.

---

## Wave: DISCUSS / [REF] Locked Decisions

### D1 — A header popover, not a page and not a dialog

**Decision** (user, 2026-08-23). The Task Manager opens from an icon in the `Header` as a **popover** —
anchored, dismiss-on-outside-click, non-modal. Not a route, not a Settings tab, not a `Dialog`.

**Why**: the user's framing is "something you just quickly check, independent of where you are, so you
don't need to navigate". A modal dialog would block the page underneath, which is wrong for a surface
whose whole purpose is a glance mid-task. A route would make checking cost a navigation and a return.

**Consequence**: the popover must be usable at the widths `Header.tsx` already handles — it renders in
both the mobile and desktop branches (S16). Content has to be scannable in a constrained box, so the
list is rows, not a table with eight columns.

### D2 — One icon: activity, with a worst-of health badge

**Decision** (user, 2026-08-23). `OAuthHealthIcon` is replaced, not joined. A single header icon
answers both questions at a glance: it animates while anything is running, carries a numeric badge for
the active count, and turns amber/red when any connection is unhealthy or a run failed.

**Why**: two adjacent status icons make the user decide which one to look at, which is the opposite of
a glance.

**Consequence**: the health signal must be *at least as good* as today's before the OAuth icon is
removed, so `OAuthHealthIcon` **stays in place until slice 05** and is deleted in the same slice that
lands generalised connection health. Removing it earlier would regress a shipped warning.

### D3 — System Administrator only

**Decision** (user, 2026-08-23). The icon does not render, and every endpoint refuses, for anyone who
is not a System Administrator.

**Why**: this matches what the codebase already decided twice, for the same material, in writing — the
refresh log (S19) and the log file (S18) are both `SystemAdmin`-guarded, both with a comment noting
that after ADR-137 "authenticated" includes any viewer who reaches the Jira frame. The task list
carries the same content: team and portfolio names, connection names, connector failure text.

**Consequence**: with authentication off everyone is an administrator, so the standalone product is
unaffected. A per-row RBAC scoping model (viewers seeing their own teams' runs) is explicitly not built.

### D4 — Free for everyone, no premium gate

**Decision** (user, 2026-08-23).

**Why**: it is operational truth about the instance you are running. Withholding it makes the free
product feel broken rather than limited — the specific thing being fixed in slice 01 is a *lie*, and a
lie is not a premium upsell. System Info and the log viewer are free today; this is the same class.

**Consequence**: no `OptionalFeature` entry, no licence check. Relevant because the premium gate on
optional features is itself known to silently drop writes and return 200, so not depending on it is
also the safer path.

### D5 — Cancellation is cooperative and best-effort, and says so

**Decision** (user, 2026-08-23). Cancel threads a `CancellationToken` from the queue into the running
update. Queued work is dropped before it starts. Running work stops at the next checkpoint it reaches;
an HTTP call already in flight is allowed to finish. `Cancelled` becomes a fifth, terminal
`UpdateProgress` value that the user can see.

**Why**: hard abort risks half-written work items, an orphaned `WriteBackRound` and a `RefreshLog` row
that describes a run that did not happen. Dequeue-only leaves the epic's actual pain — a runaway
refresh — unfixed.

**Consequence, stated plainly because it is uncomfortable**: S9 + S10 together say the time is spent
inside connector paging and the connector port takes no token. So "best effort" in the first
implementation may mean *the checkpoint is between entities and between phases, not between pages* —
which for a single wedged Team could be a long wait. Slice 04 opens with a probe to find out where the
reachable checkpoints actually are, and the slice's acceptance criteria state the granularity that was
achieved rather than assuming one.

### D6 — Truth before display: #5788 ships first, on its own

**Decision.** Bug #5788 (S5) is slice 01, ahead of any Task Manager UI.

**Why**: `UpdateProgress.Failed` is currently unreachable from a periodic refresh, so a task list built
today would render every failed run as "Completed" — and would do it in a surface whose only job is to
be believed. Fixing it is also user-visible on its own, in the refresh indicators that already ship on
Team and Portfolio detail: those stop lying before any new pixel exists.

**Consequence**: slice 01 is not infrastructure. It is the first value-bearing slice and satisfies the
slice-composition gate by itself.

### D7 — The task list reads through `IUpdateStatusStore`, never the raw dictionary

**Decision.** The new "what is admitted" read is a method on `IUpdateStatusStore`, implemented by both
`InProcessUpdateStatusStore` and `RedisUpdateStatusStore`. `UpdateController` is moved off its injected
`ConcurrentDictionary` onto the port in the same slice.

**Why**: S4 — the existing endpoint is already wrong under Redis with more than one replica, reporting
only the calling pod's dictionary. Building the Task Manager on the same shortcut would ship a fleet
surface that shows a third of the fleet. `RedisUpdateStatusStore.HasActiveWork` already reads the whole
hash, so the enumeration is a shape it can serve.

### D8 — Entity names are resolved on the read path, not stored in the status store

**Decision.** The API enriches each admitted key with the entity's display name by looking it up in the
repository when the list is requested. The name is not written into `UpdateStatus` and not into Redis.

**Why**: S3 — the Redis hash holds one integer per key, and both Lua scripts `tonumber()` it. Pushing a
name through the write path would force a representation change on the hot path of every admit and
advance, to serve a read that happens when a human opens a popover. The read path already has a
database and can afford one lookup per row.

**Consequence**: a row whose entity was deleted mid-run (the `TeamDelete` / `PortfolioDelete` update
types, S11) has no name to resolve. It renders by type and id rather than disappearing.

### D9 — Non-OAuth health comes from classified failures plus an on-demand test, not a probe loop

**Decision.** A connection's health is derived from (a) the outcome of its most recent refresh,
classified as an authentication failure where the connector can say so, and (b) an explicit
"Test connection" the administrator triggers. Not from a background loop that periodically calls every
tracker to see whether the credential still works.

**Why**: a probe loop adds a recurring outbound call per connection to systems whose rate limits we
already share and already trip — the Linear API key is shared with CI and 429s are a known local
hazard. It would also be the second scheduler in a product whose first one this Epic exists to explain.

**Consequence, and this is the cost**: S15 says a Jira 401 currently arrives at `TriggerUpdate` as an
untyped `Exception`. Classifying it means connectors have to surface an authentication failure as
something typed. Slice 05 carries that, and its learning hypothesis is aimed straight at it. Until a
connection has failed once, its health is `Unknown` rather than `Healthy` — and the UI says `Unknown`,
because claiming "healthy" from an absence of evidence is how the current icon would mislead.

### D10 — The warning feed is a bounded in-memory sink, not a parse of the log file

**Decision.** A Serilog sink holds the most recent N (order of 200) events at `Warning` and above as
structured records — timestamp, level, source context, rendered message, exception type. The popover
reads that. The full log file stays where it is, reachable by a link.

**Why**: S17 — there is no structured log store, and `GetLogs()` reads an entire rolling text file into
a string. Regex-parsing that per popover open is both expensive and brittle against the two different
output templates (`ConsoleTextTemplate` and `ConsoleJsonTemplate`) the configurator already switches
between.

**Consequence**: the buffer is per-process and does not survive a restart, and under multiple replicas
each pod holds its own. That is acceptable for "what has gone wrong lately"; it is not an audit log,
and the copy must not imply it is.

### D11 — The eleven adjacent ideas are recorded and not built

**Decision** (user, 2026-08-23): *"Don't do any — complicated enough already, but note it somewhere so
we could potentially later get back to it."* See Out of Scope for the list, kept in full so the next
pass starts from the analysis rather than redoing it.

---

## Wave: DISCUSS / [REF] Scope Assessment

**Verdict: right-sized after the user's cut. PASS.**

The Epic as written in ADO was oversized on three of the five heuristics — it bundled five independent
user outcomes that could each ship separately (task list, cancel, connection health, log-derived
warnings, write-back events), touched four bounded contexts, and its walking skeleton would have needed
more than five integration points. The user cut it in the DISCUSS session:

- **In**: live task list, cooperative cancel, connection health for all authentication types (#5019),
  warnings/errors elevated from logs. Plus #5788 as the precondition for any of it being believable.
- **Out**: write-back round outcomes (#5502's territory) and ten further adjacent surfaces — recorded
  under Out of Scope, not built.

What remains is six slices, each end-to-end and each under a day, across two bounded contexts (update
orchestration; connection health) plus one read-only sink. Two slices open with a timeboxed probe
because their uncertainty is real rather than estimable.

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — A refresh that failed says it failed

`job_id: job-operator-trust-that-a-finished-refresh-tells-the-truth` · ADO Bug **#5788** · slice 01

As a platform operator, when a scheduled Team or Portfolio refresh throws, I want every surface that
reports it to agree that it failed, so that a green indicator means something.

#### Elevator Pitch

Before: a scheduled refresh that throws pushes `Status=Completed` to the browser, while the RefreshLog
row it wrote in the same `finally` says `Success=false` — the two records disagree and the user is
looking at the wrong one.
After: on Team detail, when the refresh fails → the refresh indicator shows **Refresh failed** and the
Refresh History row under Settings → System Info agrees with it.
Decision enabled: whether to go and look at why, right now, instead of trusting a green tick and
discovering days later that the throughput chart has been flat.

#### Acceptance Criteria

- **AC-01.1** A periodic Team refresh whose `Update()` throws results in the SignalR listener receiving
  `Status = Failed` for that `UpdateKey`. (S5 — today it receives `Completed`.)
- **AC-01.2** The same run still writes its `RefreshLog` row with `Success = false` and still emits the
  one-line `Update completed | … | success=False` summary. The `finally` behaviour is unchanged.
- **AC-01.3** Write-back is still flushed and the round summary still written on the failure path —
  a failing refresh must not strand a `WriteBackRound`, because a round that never finishes silently
  drops everything it had staged.
- **AC-01.4** Work held behind the failed key by `HoldUntilQueuedWorkClears` is still released.
- **AC-01.5** `EnqueueAndAwaitAsync` callers see the same outcome they see today for a failing update —
  no caller that awaits an update begins throwing where it previously returned. Enumerated and asserted,
  because this is the change's actual blast radius.
- **AC-01.6** With authentication disabled, and with RBAC on as a System Administrator, the Team detail
  page shows the failed state in both configurations.

---

### US-02 — See what Lighthouse is doing right now

`job_id: job-operator-see-what-lighthouse-is-doing-right-now` · slice 02

As a platform operator, wherever I am in the app, I want one glance to tell me what is refreshing and
what is waiting, so that "is it working or is it wedged" is not a question I have to leave the page to
answer.

#### Elevator Pitch

Before: nothing in the product lists what is running. The only global signal is a boolean count behind
`/api/latest/update/status`, which under Redis reports only the pod that answered.
After: click the activity icon in the header → a popover lists **Team 'Lagunitas' — running** and
**Portfolio 'Q4 Platform' — queued**, updating live as they change.
Decision enabled: whether to wait, or to go and investigate, without downloading a log.

#### Acceptance Criteria

- **AC-02.1** `IUpdateStatusStore` gains an enumeration of everything currently admitted, implemented by
  both the in-process and the Redis store, and the Redis implementation answers about work admitted by
  *any* replica.
- **AC-02.2** `UpdateController` reads through `IUpdateStatusStore` and no longer injects
  `ConcurrentDictionary<UpdateKey, UpdateStatus>`. (S4 — this corrects a multi-replica defect that
  exists today.)
- **AC-02.3** Each row carries the update type, the entity id, the entity's display name resolved on the
  read path, and the status. A row whose entity no longer exists renders by type and id and does not
  break the list. (D8, S11.)
- **AC-02.4** Row labels use the tenant's configured Terminology for team / portfolio / feature — never
  the literal seeded default when the tenant has renamed it. (S21.)
- **AC-02.5** The popover updates without a manual refresh, driven by the existing
  `GlobalUpdateNotification` on the `GlobalUpdates` SignalR group. No new transport. (S20.)
- **AC-02.6** The icon and the endpoint are refused to a non-System-Administrator: the icon does not
  render, and the endpoint returns the same refusal `SystemInfoController`'s refresh log returns. (D3.)
- **AC-02.7** With nothing running the popover says so in words rather than showing an empty box.
- **AC-02.8** `OAuthHealthIcon` still renders, unchanged, beside the new icon. (D2 — it is removed in
  slice 05, not before.)
- **AC-02.9** Verified against a real instance with a real connector refresh in flight — not a seeded
  status dictionary.

---

### US-03 — See how long it has been going

`job_id: job-operator-see-what-lighthouse-is-doing-right-now` · slice 03

As a platform operator, I want each row to say how long it has been running or waiting, so that I can
tell a slow refresh from a stuck one.

#### Elevator Pitch

Before: a row says "running". A refresh that started four seconds ago and one that has been going for
forty minutes look identical, which is the difference the operator actually cares about.
After: the popover row reads **Team 'Lagunitas' — running for 12s** and **Portfolio 'Q4 Platform' —
queued for 3m**.
Decision enabled: whether this is normal or whether something is wedged — the judgement that decides
between waiting and cancelling.

#### Acceptance Criteria

- **AC-03.1** `UpdateStatus` carries the moment it was admitted and the moment it started running.
- **AC-03.2** Both survive the Redis store, and the monotonic-advance and requeue-if-admitted guarantees
  are preserved exactly — a key still cannot go backwards through `Advance`, and `Requeue` still refuses
  a key another replica has removed. (S3 — the Lua scripts currently `tonumber()` a bare ordinal.)
- **AC-03.3** A coalesced follow-up (`Requeue`) resets the queued-at moment, because it is new work
  waiting, not the old work still waiting.
- **AC-03.4** Elapsed time is computed against the same clock the rest of the backend anchors on — no
  browser-local `new Date()` deciding what "now" is on the server's behalf.
- **AC-03.5** Times are rendered as a duration, not a timestamp, and degrade to the row without a
  duration if the moment is absent (an entry admitted by an older replica mid-rollout).

---

### US-04 — Stop a refresh that is doing harm

`job_id: job-operator-stop-a-refresh-that-is-doing-harm` · slice 04

As a platform operator, when a refresh is hammering a tracker I need quiet or burning a shared rate
limit, I want to stop it from inside Lighthouse, so that my options are not "wait" or "restart the
container".

#### Elevator Pitch

Before: there is no cancel anywhere. The queue holds `Func<Task>` and no token; the only way to stop a
running refresh is to restart the process.
After: click **Cancel** on the row in the popover → the row moves to **Cancelled**, and the refresh
stops at its next checkpoint instead of running to completion.
Decision enabled: whether to intervene now — which is only a decision if intervening is possible.

#### Acceptance Criteria

- **AC-04.1** A **queued** update that is cancelled never runs: it leaves the store without contacting
  the work tracking system at all.
- **AC-04.2** A **running** update that is cancelled stops at the next checkpoint the probe established,
  and the achieved granularity is written into this slice's brief as a fact rather than an intention.
- **AC-04.3** `Cancelled` is appended to `UpdateProgress` **after** `Failed`, so existing persisted and
  transmitted ordinals keep their meaning and monotonic `Advance` can still reach it.
- **AC-04.4** A cancelled run still flushes or explicitly abandons its `WriteBackRound` and still
  releases anything held behind its key — a cancel must not strand staged write-backs or park a held
  update for good. This is the same failure mode AC-01.3 and AC-01.4 guard, reached by a different door.
- **AC-04.5** A cancelled run leaves the work-tracking system and the database in a state a subsequent
  refresh corrects — no half-written entity that a later run will not overwrite.
- **AC-04.6** The cancel endpoint is `SystemAdmin`-guarded and is idempotent: cancelling an update that
  has already finished is accepted and changes nothing.
- **AC-04.7** Cancelling one entity's refresh does not cancel another's — each `UpdateKey` is
  independently cancellable.
- **AC-04.8** Verified against a real connector refresh long enough to be cancelled mid-flight, not a
  synthetic sleep.

---

### US-05 — Any broken credential says so, not just OAuth

`job_id: job-config-admin-know-any-credential-is-failing-not-just-oauth` · ADO Story **#5019** · slice 05

As a configuration administrator, when a connection's credential stops working, I want Lighthouse to
tell me — whether that connection uses OAuth, a PAT, a scoped API token or anything else — so that I fix
it before the team notices their data froze.

#### Elevator Pitch

Before: the header icon only knows about `OAuthCredential.Status`. An Azure DevOps PAT that expired, a
Jira API token that was revoked, a Linear key whose owner was deprovisioned — all fail silently;
Lighthouse keeps making 401-returning calls and the chart goes flat.
After: open the Task Manager popover → a **Connections** section lists each connection with its state —
`Healthy`, `Authentication failed`, `Unreachable` or `Unknown` — and **Test connection** re-checks one
on demand.
Decision enabled: which credential to go and reissue, and whether the flat chart is a credential problem
at all.

#### Acceptance Criteria

- **AC-05.1** Health is reported for **every** connection regardless of authentication method, not only
  those with an `OAuthCredential` row. (S14.)
- **AC-05.2** An authentication failure from a connector is distinguishable from any other failure at
  the point health is derived. Where a connector cannot yet say, the connection reads `Unreachable`, not
  `Authentication failed` — guessing wrong sends an administrator to reissue a credential that was never
  the problem, which is the exact harm `BuildUnreadableSecretReason` was written to avoid.
- **AC-05.3** A connection that has never failed and has never been tested reads `Unknown`, not
  `Healthy`. (D9.)
- **AC-05.4** **Test connection** performs one outbound check for that connection only, on demand, and
  updates its state. No background probe loop is introduced. (D9.)
- **AC-05.5** OAuth connections keep exactly their current behaviour and wording — a `RefreshFailed` or
  `Disconnected` credential still surfaces as needing a reconnect, and still offers the route to the
  connection's edit page that `OAuthHealthIcon` offers today. (S16.)
- **AC-05.6** `OAuthHealthIcon` is deleted in this slice and its badge is folded into the activity icon
  as a worst-of. Not before. (D2.)
- **AC-05.7** The header icon's colour reflects the worst connection state plus any failed run, and its
  tooltip names what is wrong.
- **AC-05.8** Connection names are shown to System Administrators only, consistent with D3.

---

### US-06 — The warnings, without reading the log

`job_id: job-operator-read-the-warnings-without-reading-the-log` · slice 06

As a platform operator, I want recent warnings and errors surfaced where I already look, so that
noticing does not depend on me deciding to download a log file and read it.

#### Elevator Pitch

Before: warnings exist only inside a rolling text file that `GetLogs()` reads whole into a string. The
only way to see them is Settings → the log viewer, and only if you thought to go there.
After: open the popover → a **Recent problems** section lists the last warnings and errors with their
time, level and message, and a link opens the full log.
Decision enabled: whether the thing you just noticed in the task list has an explanation already sitting
in the logs, without leaving the popover to find out.

#### Acceptance Criteria

- **AC-06.1** A bounded in-memory sink retains the most recent events at `Warning` and above as
  structured records — time, level, source context, rendered message, exception type where present.
- **AC-06.2** The buffer is bounded and cannot grow without limit; the oldest entry is evicted first.
- **AC-06.3** Changing the log level through the existing `LogsController` endpoint takes effect on what
  the sink captures, without a restart — the level switch is already a `LoggingLevelSwitch`.
- **AC-06.4** The endpoint is on the already-`SystemAdmin`-guarded `LogsController`. (S18.)
- **AC-06.5** The section states plainly that it holds recent events since this instance started and is
  not a complete history — it is per-process and does not survive a restart. (D10.)
- **AC-06.6** With nothing captured, the section says so rather than rendering empty.
- **AC-06.7** Verified with real warnings produced by a real failing connector, not with injected log
  lines.

---

## Wave: DISCUSS / [REF] Story Map and Slices

**Backbone** (operator's activities, left to right):
notice something → see what is happening → judge whether it is stuck → intervene → find out why.

| Slice | Story | ADO | Ships | Learning hypothesis |
|---|---|---|---|---|
| `slice-01-a-failed-refresh-says-failed` | US-01 | **#5788** (Bug) | The status the browser receives matches what happened | Disproves that the existing status pipeline can carry a truthful terminal state |
| `slice-02-see-what-is-running` | US-02 | **#5840** | Activity icon + popover listing running and queued work | Disproves that the status store can answer "what is running" at all |
| `slice-03-how-long-has-it-been-going` | US-03 | **#5841** | Elapsed time per row | Disproves that the Redis hash can carry more than an ordinal safely |
| `slice-04-stop-a-refresh` | US-04 | **#5842** | Cancel, cooperative | Disproves that cancellation can be honoured without changing `IWorkTrackingConnector` |
| `slice-05-any-broken-credential-says-so` | US-05 | **#5019** | Connection health for every auth method; OAuth icon absorbed | Disproves that an auth failure is distinguishable from any other failure |
| `slice-06-the-warnings-without-the-log` | US-06 | **#5843** | Recent warnings and errors in the popover | Disproves that a level filter alone yields a signal rather than noise |

**Walking skeleton**: slice 01 + slice 02 together. After those two, an operator can open one thing and
see a truthful list of what the instance is doing. Everything after deepens that.

### Carpaccio taste tests

| Test | Verdict |
|---|---|
| Any slice shipping 4+ new components? | No. The largest, slice 02, adds one icon, one popover and one endpoint on an existing controller. |
| Every slice depending on a new abstraction? | No — and the one shared abstraction (`IUpdateStatusStore` enumeration) ships *inside* slice 02, the first slice that needs it, rather than as its own slice. |
| Does any slice disprove a pre-commitment? | Yes, four of six carry a hypothesis that can kill the approach. Slices 04 and 05 are the sharp ones: both could return "the port has to change", which would resize the Epic. |
| Synthetic data only? | No. AC-02.9, AC-04.8 and AC-06.7 each require a real connector refresh. |
| Two slices identical but for scale? | No. |

### Prioritisation

Order: **01 → 02 → 03 → 04 → 05 → 06**, with one deliberate exception.

- **01 first** on correctness, not preference: a display built on S5 would ship the lie into five new
  places (D6).
- **02 next** because it is the Epic's actual subject, and because it corrects the multi-replica defect
  in S4 as a side effect.
- **03 next** because it is small and it is what turns a list into a judgement.
- **Exception — run slice 04's probe during 02/03, not at the start of 04.** Slice 04 has the highest
  uncertainty in the Epic (S9 + S10) and its answer can resize the whole thing. Learning leverage says
  find out early, while the cost of being wrong is two slices rather than five.
- **05 before 06** because it is a named ADO child with a user waiting on it, and because "which
  connection is broken" is a more common question than "what warned recently".

---

## Wave: DISCUSS / [REF] Out of Scope

### Explicitly not built in this Epic (user decision, D11)

Eleven adjacent surfaces were analysed during DISCUSS and cut. Kept in full so a later pass starts from
the analysis rather than repeating it.

| # | Idea | Why it was attractive | Why it is out |
|---|---|---|---|
| A | Show **held** updates distinctly (`heldUpdates`) | A held row renders as `Queued` forever and looks stuck (S12) | Deferred with the rest; the list is honest without it, just less explanatory |
| B | Show a **coalesced follow-up** is pending (`pendingReruns`) | Otherwise a refresh appears to restart itself (S12) | Deferred |
| C | Show **paused by database maintenance** | `DatabaseMaintenanceGate` drops the trigger with no user-visible trace (S13) | Deferred |
| D | **Write-back round outcome** — what was pushed, what was refused | Jira 403s and *drops* the write; nobody learns. Highest-value of the deferred set | Pulls in #5502's event model; a slice of its own, later |
| E | **Failure reason** on the last run | `RefreshLog.Success` is a bare bool (S7) — "failed" is recordable, "why" is not | Deferred; note it needs a schema change, so it wants planning with D |
| F | **Next scheduled run** per entity | Answers "do I need to refresh?" before they refresh | Deferred |
| G | **Queue position / wait estimate** | Single-reader loop means position is a real wait | **Split, 2026-09-14.** The naming half — a queued row says what is holding the lane — is IN slice 02. Ordinal position and wait estimate stay deferred. See Decisions taken at the foot of this file |
| H | **Which replica is running it** | `RedisUpdateStatusStore` already knows; SaaS operators ask | Deferred |
| I | **Trigger a refresh from the popover** | Per-entity buttons exist; no central one | Deferred |
| J | **Stale-data badge** ("last synced 3d ago") | Ties staleness into the same glance | Deferred |
| K | **Toast when a background refresh fails** while you are elsewhere | Failures are only visible if you look | Deferred |

Recommended re-entry order if this is picked up again: **A + B + C + E** first — they are what make a
queued or failed row *explain itself* — then **D**, which is the largest and needs #5502's events.

### Also out

- **Write-back events as a monitor surface** (#5502's territory) — the Epic description raises it; it is D above.
- **Per-viewer RBAC scoping** of the task list (D3 settles this).
- **A premium gate** (D4).
- **A background credential probe loop** (D9).
- **A persistent, queryable log store** — the sink is bounded and in-process (D10).
- **A new Settings tab or route** — the popover is the only surface (D1).
- **Hard abort of in-flight work** (D5).

---

## Wave: DISCUSS / [REF] Walking Skeleton Strategy

**Strategy B — extend an existing end-to-end path.** No greenfield skeleton is needed or wanted.

The end-to-end path already exists and already runs in production: background service → `TriggerUpdate`
→ `UpdateQueueService` → `IUpdateStatusStore` → `UpdateNotificationHub` → `UpdateSubscriptionService` →
React. Every slice below hangs off a seam on that path. Slice 01 corrects it; slice 02 adds a read of it;
slices 03–04 deepen what it carries; slices 05–06 hang two adjacent read-only surfaces off the same
popover.

The two seams that are genuinely new are `IUpdateStatusStore`'s enumeration (slice 02, D7) and a
cancellation context alongside the existing `WriteBackRoundContext` (slice 04). Both are introduced
inside the first slice that needs them, not ahead of it.

---

## Wave: DISCUSS / [REF] Driving Ports

| Surface | Port | Guard |
|---|---|---|
| Task list read | `GET /api/latest/update/…` on the existing `UpdateController` | SystemAdmin (D3) |
| Existing global count | `GET /api/latest/update/status` — moved onto `IUpdateStatusStore` (D7) | unchanged `[Authorize]` |
| Cancel | `POST` on `UpdateController`, per `UpdateKey` | SystemAdmin |
| Connection health | replaces `GET /api/oauth/health` | SystemAdmin (already) |
| Recent warnings | new route on the existing `LogsController` | SystemAdmin (already, S18) |
| Live updates | existing `UpdateNotificationHub`, `GlobalUpdates` group (S20) | connection-level |
| UI | header icon + popover, rendered in both branches of `Header.tsx` (S16) | `isSystemAdmin` |

---

## Wave: DISCUSS / [REF] Outcome KPIs

Lighthouse is self-hosted and there is no vendor telemetry pipeline, so every KPI here is
`per_instance` or `vendor_demo_only`. Predecessor #5733 would change that; it is not a dependency.

| KPI | Target | Measurement | Scope |
|---|---|---|---|
| `OUT-5511-status-truthfulness` | **100%** of failing periodic refreshes report `Failed` to the browser; today it is 0% | Assert the SignalR terminal status against the `RefreshLog.Success` written by the same run — the two records that disagree today (S6) must agree | per_instance |
| `OUT-5511-time-to-notice` | Median time from a refresh failing to an administrator seeing it drops from *"whenever someone opens the log"* to **one page load** | The signal is visible without navigation on any page that renders the header | per_instance |
| `OUT-5511-cancel-effectiveness` | **≥ 90%** of cancels on a running update stop it within the checkpoint granularity the slice-04 probe established | Measured on a real connector refresh; the granularity is recorded in the slice brief as a number, not an intention | vendor_demo_only |
| `OUT-5511-credential-coverage` | **100%** of connections report a health state, against roughly the OAuth-only share today | Count connections with a non-`Unknown` state after one refresh cycle, over total connections | per_instance |
| `OUT-5511-multi-replica-correctness` | The active-update count is identical from every replica | Two replicas behind Redis; query both; compare. Corrects the defect in S4 | vendor_demo_only |

---

## Wave: DISCUSS / [REF] Pre-requisites

| # | Pre-requisite | State |
|---|---|---|
| P1 | Bug **#5788** fixed | **Blocking.** It is slice 01 of this Epic (D6). |
| P2 | `IUpdateStatusStore` enumerable | Built in slice 02 (D7). |
| P3 | Connectors able to name an authentication failure | **Unproven.** S15. Slice 05's hypothesis is aimed at it; it may resize that slice. |
| P4 | Redis representation able to carry more than an ordinal | **Unproven.** S3. Slice 03 opens with a probe. |
| P5 | `IWorkTrackingConnector` cancellation reach | **Unproven and consequential.** S9 + S10. Slice 04's probe runs early, during slices 02/03. |
| P6 | Opt-in usage data (#5733) | **Not a dependency.** It is a board predecessor. Without it the KPIs stay `per_instance`, which is what they are declared as. Now **Resolved** on the board (2026-09-14). |
| P7 | Premium licence | **Not required.** D4. |
| P8 | Queue lane structure (#5877) | **Not a blocker, but a sequencing constraint.** #5877's lane work and slice 04's cancellation edit the same class (S23, `UpdateQueueService`). Slice 04's probe answers part of #5877 for free. See Post-DESIGN Reconciliation. |

---

## Wave: DISCUSS / [REF] Definition of Ready

| # | Item | Evidence |
|---|---|---|
| 1 | Business value stated | Five job stories, each with a named persona; opportunity scores in `docs/product/jobs.yaml` |
| 2 | Acceptance criteria testable | 40 ACs across six stories, each naming an observable outcome; three name a real-connector verification |
| 3 | Dependencies identified | Pre-requisites table; three marked unproven with the slice that proves each |
| 4 | Sized | Six slices, each ≤ 1 day; two carry a timeboxed probe inside the slice |
| 5 | No blocking unknowns | P3, P4 and P5 are unknowns but not blockers — each is confined to one slice and each has a probe. P5's probe is deliberately pulled forward |
| 6 | UX defined | D1 (popover), D2 (one icon), D3 (System Admin only); anchored to `Header.tsx`'s two existing branches |
| 7 | Job traceability | Every story carries a real `job_id`; no story uses the infrastructure-only escape valve |
| 8 | Non-functional constraints stated | Multi-replica correctness (D7, S3/S4); Terminology (S21, AC-02.4); RBAC parity with `LogsController` and the refresh log (D3, S18/S19); write-back round integrity (AC-01.3, AC-04.4) |
| 9 | Out-of-scope explicit | Eleven deferred surfaces recorded with reasons and a re-entry order, plus seven further exclusions |

**Verdict: READY.** Requirements completeness 0.96 — the shortfall is P3/P5, which are honest unknowns
scoped to one slice each rather than gaps in the requirements.

---

## Wave: DISCUSS / [REF] Definition of Done

1. All six slices shipped, each with its own focused commit and its hypothesis answered in its brief.
2. Backend `dotnet build` zero warnings; `dotnet test` green with the live-connector categories excluded.
3. Frontend `pnpm test` green; `pnpm build` zero errors and zero warnings; Biome clean.
4. SonarQube Cloud gate green — no new issues of any severity.
5. Mutation testing run per feature on both stacks, kill rate ≥ 80%, recorded under
   `docs/feature/epic-5511-task-manager/mutation/`.
6. One walking-skeleton E2E through a Page Object for the popover, driven by demo data.
7. Per-theme `@screenshot` coverage for the popover, regenerated (delete the old PNG first — the
   comparator keeps the old file when the diff is under threshold).
8. Public docs updated at feature finalization, using the tenant's configurable Terminology.
9. ADO #5511, #5019 and #5788 transitioned; #5511 stops at Resolved, never Closed.

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key decisions

- **D1** Header popover, not a page or dialog — the user's "quickly check from wherever you are".
- **D2** One icon: activity with a worst-of health badge; `OAuthHealthIcon` absorbed in slice 05, not earlier.
- **D3** System Administrator only — matches what `LogsController` and the refresh log already decided in writing.
- **D4** Free for everyone — it is operational truth, and slice 01 fixes a lie, which is not an upsell.
- **D5** Cooperative best-effort cancel; `Cancelled` is a visible terminal state; granularity to be established by probe, not assumed.
- **D6** #5788 ships first, alone — truth before display.
- **D7** Read through `IUpdateStatusStore`, never the raw dictionary; corrects a live multi-replica defect.
- **D8** Entity names resolved on the read path; the Redis hot path keeps its bare ordinal.
- **D9** Non-OAuth health from classified failures plus an on-demand test; `Unknown` never renders as `Healthy`.
- **D10** Bounded in-memory Serilog sink, not a parse of the log file; the copy must not imply an audit log.
- **D11** Eleven adjacent surfaces recorded and not built.

### Requirements summary

- **Primary jobs**: see what the instance is doing; believe what it says; stop what is doing harm; know
  which credential broke; read the warnings without reading the log.
- **Walking skeleton**: slices 01 + 02 — a truthful list behind one header icon.
- **Feature type**: cross-cutting — backend orchestration, a new port capability, a logging sink, and a
  user-facing UI surface.

### Constraints established

- Multi-replica correctness is a requirement, not a nice-to-have: the endpoint being replaced is already
  wrong under Redis.
- Anything richer than an ordinal must survive the Redis hash and its two Lua scripts.
- Cancellation and failure paths must both leave `WriteBackRound` and `heldUpdates` in a state that does
  not silently drop staged writes or park held work for good.
- Every label renders the tenant's configured Terminology.

### Upstream changes

None. No DISCOVER or DIVERGE artifacts existed for this Epic, so no prior assumption was contradicted.

---

## Wave: DISCUSS / Tier-2 Expansion Menu

Density resolved to `lean` + `ask-intelligent`. Triggers evaluated against the artifacts above:

| Trigger | Fired | Suggested expansion |
|---|---|---|
| AC ambiguity across ≥2 stories | **Yes** — AC-04.2 and AC-05.2 both defer a definition to a probe result | `gherkin-scenarios` |
| Cross-context complexity (≥3 contexts or technologies) | **Yes** — update orchestration, connection health, Serilog sink, Redis, SignalR, React | `alternatives-considered` |
| Multi-stakeholder (≥3 personas) | No — two primary, one secondary | |
| Compliance / regulatory terms in ACs | No | |
| WS strategy = D (configurable) | No — strategy B | |

Suggested expansions for this feature (triggered by: AC ambiguity, cross-context complexity):

- `gherkin-scenarios` — Given-When-Then covering the happy path and the failure paths for each slice
- `alternatives-considered` — the alternatives weighed and rejected behind D5, D8, D9 and D10

Apply? `[Y/n/all/none/custom]` — or ask for any catalog item ad hoc.

---
---

# Wave: DESIGN

Run 2026-08-23. Scope: **Application / components** (`nw-solution-architect`). Mode: **propose**.
Paradigm unchanged — OOP on the C# backend, functional-leaning React on the frontend.
Architectural pattern unchanged — ports-and-adapters, extended, no new style introduced.

Density `lean` + `ask-intelligent`. Prior-wave consultation: `brief.md` (base section plus 40-odd
per-feature deltas), 180 existing ADRs, `journeys/epic-5511-task-manager.yaml`, `jobs.yaml`, and this
file's DISCUSS half. No SPIKE artifacts exist — the two probes are scheduled inside slices 03 and 04.
No contradiction found between DESIGN and DISCUSS.

**Outcome collision check**: `nwave-ai outcomes check-delta` exits `0` — no collisions.

## Wave: DESIGN / [REF] Correction to a DISCUSS assumption

DISCUSS S15 stated that an authentication failure is not distinguishable, and D9 costed slice 05 on
that basis. Reading the connectors during DESIGN shows the picture is better than DISCUSS assumed:

`ConnectionValidationResult` already carries a **`Code`** string discriminator, and the value
**`authentication_failed`** already exists — emitted by `AzureDevOpsWorkTrackingConnector`,
`JiraWorkTrackingConnector` and `ServiceNowValidationVerdict`. ServiceNow additionally emits
`insufficient_permissions`. `ValidateConnection` is already reachable from
`WorkTrackingSystemConnectionsController`. Linear and CSV emit no auth code.

S15 remains true about the **passive** path — a refresh that 401s never calls `ValidateConnection`, and
`TriggerUpdate` catches bare `Exception`. But the classifier itself is not greenfield. This is what
makes DDD-4 possible and shrinks slice 05.

---

## Wave: DESIGN / [REF] DDD List

| # | Decision | Verdict | One-line rationale |
|---|---|---|---|
| DDD-1 | Update activity is a **read through the status store**, not a new projection written on the update path | Locked | The store already holds exactly the set being asked about; a projection would be a second copy of a truth that already exists in one place. ADR-181 |
| DDD-2 | `UpdateController` stops injecting `ConcurrentDictionary<UpdateKey, UpdateStatus>` and depends on `IUpdateStatusStore` | Locked | The direct injection is a live multi-replica defect, not a shortcut to preserve. ADR-181 |
| DDD-3 | Admission and start moments live in a **sibling Redis hash**, written outside the Lua scripts | Locked | Keeps the monotonic-advance and requeue-if-admitted guarantees provably untouched on the hot path. ADR-182 |
| DDD-4 | Cancellation is an **ambient scoped token** plus a **narrow widening of the connector paging methods** | Locked | The context is needed either way; the paging widening is where the wall-clock actually is. ADR-183 |
| DDD-5 | `Cancelled` is appended to `UpdateProgress` **after** `Failed` | Locked | Monotonic `Advance` compares ordinals, and existing values must keep their meaning. ADR-183 |
| DDD-6 | Connection health is a **recorded verdict per connection**, classified by `ValidateConnection`'s existing `Code` | Locked | The classifier already exists on three of five connectors; the verdict must outlive the process that observed it. ADR-184 |
| DDD-7 | `OAuthHealthAggregator` and `OAuthHealthController` are **absorbed**, not kept alongside | Locked | Two sources for one question is how the two disagree. ADR-184 |
| DDD-8 | Recent problems is a **bounded in-process Serilog sink**, constructed before the logger and registered as a singleton | Locked | No structured store exists, and the sink must be reachable by DI while being handed to a logger built at builder time. ADR-185 |
| DDD-9 | The header badge is fed by a **small always-live summary**; each popover section fetches on open | Locked | The header is on every page for every administrator; the sections are read by one human occasionally. ADR-186 |
| DDD-10 | Name enrichment happens in an **application service**, not in the controller and not in the store | Locked | Keeps the repository fan-out out of the controller and the database out of the store. ADR-181 |
| DDD-11 | Entity names are resolved **per read**, not cached | Deferred to DELIVER | A popover open is not a hot path; revisit only if a real instance shows it is. |

---

## Wave: DESIGN / [REF] Component Decomposition

### Backend

| Component | File | Change | Summary |
|---|---|---|---|
| `IUpdateStatusStore` | `Services/Interfaces/Update/IUpdateStatusStore.cs` | EXTEND | Add an enumeration of everything admitted. Add the admission/start moments to what a status carries. |
| `InProcessUpdateStatusStore` | `.../Update/InProcessUpdateStatusStore.cs` | EXTEND | Enumerate the dictionary; stamp moments on admit and on advance-to-`InProgress`. |
| `RedisUpdateStatusStore` | `.../Update/RedisUpdateStatusStore.cs` | EXTEND | `HashGetAll` over `lighthouse:update-status` for the enumeration; a sibling hash `lighthouse:update-moments` for the moments, written and deleted alongside the ordinal but never inside the Lua scripts. |
| `UpdateStatus` | `.../Update/UpdateStatus.cs` | EXTEND | `QueuedAt` and `StartedAt`, both nullable — absent is a legitimate state during a rolling upgrade. |
| `UpdateProgress` | `.../Update/UpdateProgress.cs` | EXTEND | Append `Cancelled` after `Failed`. |
| `IUpdateQueueService` | `Services/Interfaces/Update/IUpdateQueueService.cs` | EXTEND | Add a cancel for one `UpdateKey`, idempotent. |
| `UpdateQueueService` | `.../Update/UpdateQueueService.cs` | EXTEND | A `CancellationTokenSource` per admitted key; set the token into the cancellation context in `ExecuteUpdateTask` beside the write-back round; record `Cancelled` as a terminal status; dispose the source alongside `statusStore.Remove`. |
| `UpdateCancellationContext` | `Services/Implementation/UpdateCancellationContext.cs` | **NEW** | `AsyncLocal<CancellationToken?>`, a direct sibling of `WriteBackRoundContext`, written only by the queue. |
| `UpdateServiceBase` | `.../Update/UpdateServiceBase.cs` | EXTEND | Stop swallowing (slice 01). Distinguish `OperationCanceledException` from a genuine failure so a cancel is not logged as an error. |
| `TeamUpdater` / `PortfolioUpdater` / `ForecastUpdater` | `.../Update/*.cs` | EXTEND | Observe the ambient token at phase boundaries and pass it into the connector paging calls. |
| `IUpdateActivityService` + `UpdateActivityService` | `Services/Interfaces/Update/`, `Services/Implementation/Update/` | **NEW** | The read model: takes what the store enumerates, resolves entity display names from the repositories, returns rows. Also computes the header summary. |
| `UpdateController` | `API/UpdateController.cs` | EXTEND | Drop the `ConcurrentDictionary` injection. Existing `status` route re-implemented over the port. Add the activity list, the summary and the cancel routes. |
| `IWorkTrackingConnector` | `Services/Interfaces/WorkTrackingConnectors/IWorkTrackingConnector.cs` | EXTEND | `CancellationToken` on the six paging methods only — the two `GetWorkItemsForTeam` overloads, the two `GetFeaturesForProject` overloads, and the three sweeps. Not on `ValidateConnection`, `GetPredefinedAdditionalFields`, or `WriteFieldsToWorkItems`. |
| Five connectors | `.../WorkTrackingConnectors/{AzureDevOps,Jira,Linear,ServiceNow,Csv}/` | EXTEND | Thread the token into the paging loop; that is the whole change. |
| `ConnectionHealthVerdict` | `Models/ConnectionHealthVerdict.cs` | **NEW** | One row per connection: state, code, message, observed-at. Additive, expand-only migration. |
| `IConnectionHealthService` + `ConnectionHealthService` | `Services/Interfaces/`, `Services/Implementation/` | **NEW** | Records a verdict when a refresh fails, by asking `ValidateConnection` why; runs the on-demand test; folds `OAuthCredential.Status` in; answers the read. |
| `ConnectionHealthController` | `API/ConnectionHealthController.cs` | **NEW** | Replaces `OAuthHealthController` at a connection-neutral route. |
| `OAuthHealthAggregator` / `OAuthHealthController` / `IOAuthHealthAggregator` | `.../OAuth/`, `API/` | **DELETE** (slice 05) | Absorbed into connection health. |
| `RecentProblemsSink` + `IRecentProblems` | `Services/Implementation/Logging/`, `Services/Interfaces/` | **NEW** | Bounded ring buffer of warning-and-above events as structured records. |
| `LoggingConfigurator` | `Startup/LoggingConfigurator.cs` | EXTEND | Accept the sink instance and wire it, the same way it already accepts the `LoggingLevelSwitch`. |
| `LogsController` | `API/LogsController.cs` | EXTEND | Add the recent-problems read. Already `SystemAdmin`-guarded. |

### Frontend

| Component | File | Change | Summary |
|---|---|---|---|
| `TaskManagerIcon` | `components/App/Header/TaskManagerIcon.tsx` | **NEW** | Activity icon, animating while work runs, badge for the active count, colour from the worst of run failures and connection health. Renders `null` for a non-System-Administrator. |
| `TaskManagerPopover` | `components/App/Header/TaskManagerPopover.tsx` | **NEW** | MUI `Popover`, non-modal, anchored to the icon. Three sections, each fetching on open. |
| `ActivitySection` / `ConnectionsSection` / `RecentProblemsSection` | `components/App/Header/TaskManager/` | **NEW** | One per slice — 02, 05, 06 — so each slice adds a section without reshaping the others. |
| `Header.tsx` | `components/App/Header/Header.tsx` | EXTEND | Mount the icon in **both** the mobile and desktop branches. |
| `OAuthHealthIcon.tsx` | `components/App/Header/OAuthHealthIcon.tsx` | **DELETE** (slice 05) | Not before — it is the only health signal until then. |
| `UpdateSubscriptionService.ts` | `services/UpdateSubscriptionService.ts` | EXTEND | `UpdateProgress` gains `"Cancelled"`; `UpdateType` gains `"PortfolioDelete"` and `"TeamDelete"`; add the summary subscription. |
| `SystemActivityService.ts` | `services/Api/SystemActivityService.ts` | **NEW** | HTTP adapter for the activity list, cancel, connection health and recent problems. Registered on `ApiServiceContext` beside the 28 existing services. |
| `OAuthService.ts` | `services/Api/OAuthService.ts` | EXTEND | `getHealth` removed in slice 05; the rest of the OAuth surface is untouched. |

---

## Wave: DESIGN / [REF] Driving Ports

| Method | Route | Guard | Purpose | Slice |
|---|---|---|---|---|
| GET | `/api/latest/update/status` | `[Authorize]` (unchanged) | Existing boolean + count, now answered through the port | 02 |
| GET | `/api/latest/update/summary` | SystemAdmin | Header badge: active count + worst severity. Small and frequently read | 02, widened 05/06 |
| GET | `/api/latest/update/activity` | SystemAdmin | The rows: type, entity id, resolved name, status, moments | 02 |
| POST | `/api/latest/update/{updateType}/{id}/cancel` | SystemAdmin | Cancel one `UpdateKey`; idempotent | 04 |
| GET | `/api/latest/connectionhealth` | SystemAdmin | Per-connection verdicts. Replaces `GET /api/oauth/health` | 05 |
| POST | `/api/latest/connectionhealth/{connectionId}/test` | SystemAdmin | On-demand re-check via `ValidateConnection` | 05 |
| GET | `/api/latest/logs/recent` | SystemAdmin (already) | Recent warning-and-above events | 06 |
| SignalR | `updateNotificationHub`, group `GlobalUpdates` | connection-level `[Authorize]` | Existing live signal; no new transport | 02 |

`GET /api/oauth/health` is **removed** in slice 05, not deprecated in place — it has exactly one caller
and that caller is deleted in the same slice.

---

## Wave: DESIGN / [REF] Driven Ports and Adapters

| Port | Adapter | Technology | Purpose | Change |
|---|---|---|---|---|
| Update status store | `InProcessUpdateStatusStore` / `RedisUpdateStatusStore` | `ConcurrentDictionary` / StackExchange.Redis | Enumerate admitted work; carry the moments | EXTEND |
| Update notification | `IHubContext<UpdateNotificationHub>` | SignalR (in-memory or Redis backplane) | Push status changes | UNCHANGED |
| Entity read | `IRepository<Team>` / `IRepository<Portfolio>` | EF Core | Resolve display names on the read path | REUSED |
| Connection health persistence | `LighthouseAppContext` | EF Core, SQLite/PostgreSQL | One verdict row per connection | EXTEND (additive migration) |
| Work tracking system | `IWorkTrackingConnector` | 5 connectors | Paging now observes a token; `ValidateConnection` classifies a failure | EXTEND |
| Log capture | `RecentProblemsSink` | Serilog `ILogEventSink` | Bounded ring buffer, in process | **NEW** |

---

## Wave: DESIGN / [REF] Technology Choices

No new technology. Every version is what the repository already pins.

| Concern | Technology | Note |
|---|---|---|
| Backend | ASP.NET Core, .NET 10 | unchanged |
| Persistence | EF Core, SQLite / PostgreSQL | one additive table |
| Distributed status | StackExchange.Redis | one additional hash; the existing hash and its two Lua scripts are untouched |
| Live push | SignalR | existing hub and group |
| Logging | Serilog | one additional sink on a logger that already takes a level switch the same way |
| Frontend | React 18, TypeScript, MUI | `Popover`, `Badge`, `IconButton` — all already in use in the header |
| Backend tests | NUnit 4.6, Moq, EF InMemory, WebApplicationFactory | unchanged |
| Frontend tests | Vitest, React Testing Library | unchanged |
| E2E | Playwright, Page Object Model | unchanged |

---

## Wave: DESIGN / [REF] Reuse Analysis

Every component with overlapping responsibility, classified. `CREATE NEW` requires evidence that
extending is impossible or creates unacceptable coupling.

| Existing component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| `IUpdateStatusStore` | `Services/Interfaces/Update/IUpdateStatusStore.cs` | Holds exactly the set being asked about | **EXTEND** | It already owns admitted work, in both deployment shapes. A parallel activity store would be a second copy of a truth that exists in one place, and the two would drift. |
| `UpdateController` | `API/UpdateController.cs` | Already serves the active-update count | **EXTEND** | Same resource, same route prefix. Adding three routes beats a second controller answering about the same thing — and the extension is what corrects its multi-replica defect. |
| `UpdateStatus` | `.../Update/UpdateStatus.cs` | The per-key record | **EXTEND** | Two nullable moments. A parallel record keyed the same way would need the same lifecycle for no gain. |
| `UpdateProgress` | `.../Update/UpdateProgress.cs` | Terminal states | **EXTEND** | Appending `Cancelled` after `Failed` preserves every existing ordinal, which is what monotonic `Advance` compares. |
| `WriteBackRoundContext` | `Services/Implementation/WriteBackRoundContext.cs` | Ambient per-execution value set only by the queue | **CREATE NEW** (sibling) | Deliberately **not** extended. Its round has `Join`/`Leave`/`HasFinished` semantics a token has no use for, and putting a token on it would mean an execution with no write-back still carries a round. `UpdateCancellationContext` copies the shape — the same `AsyncLocal`, the same single writer — and shares no state. |
| `UpdateNotificationHub` | `.../Update/UpdateNotificationHub.cs` | Live push | **EXTEND** (usage only) | No code change. Both new payloads ride the existing `GlobalUpdates` group. |
| `IWorkTrackingConnector` | `Services/Interfaces/WorkTrackingConnectors/` | The outbound port | **EXTEND** | Six paging methods gain a token. Widening the whole port would put a token on `ValidateConnection` and `GetPredefinedAdditionalFields`, which do not page and cannot use one. |
| `ConnectionValidationResult` | `Models/Validation/ConnectionValidationResult.cs` | Already carries a `Code` including `authentication_failed` | **EXTEND** (usage only) | No shape change. This is the whole reason DDD-6 is cheap: the classifier exists, it just was never asked outside a manual validate. |
| `OAuthHealthAggregator` | `.../OAuth/OAuthHealthAggregator.cs` | Health, OAuth-only | **REPLACE** | Not extended: its whole shape is "count OAuth credential rows", and a PAT connection has none. Generalising it in place would leave a class named for OAuth answering about connections that have no OAuth. `ConnectionHealthService` subsumes it and it is deleted. |
| `WorkTrackingSystemConnectionDto.RequiresReconnect` | `API/DTO/` | Per-connection OAuth-only health flag | **EXTEND** | Kept and left computed as it is, so the connection edit page is unaffected. The popover reads the verdict; the edit page keeps its existing flag. Merging them is a later tidy, not this Epic's job. |
| `LogsController` | `API/LogsController.cs` | The log read surface, already SystemAdmin-guarded | **EXTEND** | One route. A second logs controller would need the same guard and the same comment explaining it. |
| `SerilogLogConfiguration` | `Services/Implementation/SerilogLogConfiguration.cs` | Level switch and file read | **EXTEND** (usage only) | Untouched. The sink is a peer of the file sink, not a change to how the file one is read. |
| `LoggingConfigurator` | `Startup/LoggingConfigurator.cs` | Builds the logger before DI exists | **EXTEND** | It already takes a `LoggingLevelSwitch` constructed outside it and registered separately. The sink follows exactly that precedent. |
| `Header.tsx` | `components/App/Header/Header.tsx` | Hosts the status icons | **EXTEND** | Mount one icon in the two existing branches. |
| `OAuthHealthIcon.tsx` | `components/App/Header/OAuthHealthIcon.tsx` | The icon being replaced | **REPLACE** | Its click navigates away to a connection edit page; the new one opens a popover. Different interaction, different data source, one line of shared markup. Rewriting it in place would leave a file named for OAuth rendering activity. |
| `UpdateSubscriptionService.ts` | `services/UpdateSubscriptionService.ts` | The SignalR client | **EXTEND** | Two union types widen and one subscription is added. A second SignalR client would open a second connection to the same hub. |
| `ApiServiceContext` | `services/Api/ApiServiceContext.ts` | 28 registered services | **EXTEND** | One more service registered the established way. |

**Zero unjustified `CREATE NEW`.** The three genuinely new backend units are
`UpdateCancellationContext` (justified above against extending `WriteBackRoundContext`),
`ConnectionHealthService` + its verdict row (replacing a class whose shape cannot generalise), and
`RecentProblemsSink` (no in-memory log sink exists in any form).

---

## Wave: DESIGN / [REF] C4 — Container

```mermaid
graph TB
    subgraph Browser["Browser — System Administrator"]
        HDR["Header<br/>TaskManagerIcon + Popover"]
        SUB["UpdateSubscriptionService<br/>SignalR client"]
        SVC["SystemActivityService<br/>HTTP adapter"]
    end

    subgraph Backend["Lighthouse Backend — ASP.NET Core"]
        UC["UpdateController"]
        CHC["ConnectionHealthController"]
        LC["LogsController"]
        HUB["UpdateNotificationHub"]
        UAS["UpdateActivityService"]
        CHS["ConnectionHealthService"]
        SINK["RecentProblemsSink<br/>bounded ring buffer"]
        UQS["UpdateQueueService"]
        USS["IUpdateStatusStore"]
    end

    subgraph Stores["State"]
        DB[("SQLite / PostgreSQL")]
        REDIS[("Redis — optional<br/>update-status + update-moments")]
    end

    EXT["Work tracking systems<br/>Jira · ADO · Linear · ServiceNow · CSV"]

    HDR --> SVC
    HDR --> SUB
    SVC -->|"GET summary / activity<br/>POST cancel"| UC
    SVC -->|"GET health / POST test"| CHC
    SVC -->|"GET logs/recent"| LC
    SUB <-->|"GlobalUpdates group"| HUB
    UC --> UAS
    UAS --> USS
    UAS --> DB
    UC -->|cancel| UQS
    UQS --> USS
    UQS --> HUB
    UQS -->|paging with token| EXT
    CHC --> CHS
    CHS --> DB
    CHS -->|"ValidateConnection<br/>on failure and on demand"| EXT
    LC --> SINK
    USS -.->|"multi-replica only"| REDIS
    USS -.->|"single instance"| DB
```

Dashed edges are the deployment fork: with no Redis the store is the in-process dictionary and the
whole surface still works, which is the standing constraint that every change preserves the
single-container product unchanged.

## Wave: DESIGN / [REF] C4 — Component: the cancellation path

```mermaid
sequenceDiagram
    participant A as Administrator
    participant UC as UpdateController
    participant Q as UpdateQueueService
    participant CTX as UpdateCancellationContext
    participant U as TeamUpdater
    participant C as Connector paging loop
    participant H as UpdateNotificationHub

    Note over Q: on admit, a CancellationTokenSource per UpdateKey
    Q->>CTX: set token (beside the write-back round)
    Q->>U: run update task in a scope
    U->>C: fetch page N (token)
    A->>UC: POST update/Team/12/cancel
    UC->>Q: cancel(UpdateKey)
    Q-->>C: token cancelled
    C-->>U: OperationCanceledException between pages
    U->>U: finally — RefreshLog, round summary
    U-->>Q: cancelled, not failed
    Q->>Q: flush or abandon the WriteBackRound
    Q->>Q: release work held behind the key
    Q->>H: status = Cancelled
    H-->>A: row moves to Cancelled
```

The two steps after the exception are the ones that are easy to omit and expensive to omit: a round
that never finishes silently drops every write it staged, and held work behind a key that never
released stays parked until something unrelated happens to poke the same key.

---

## Wave: DESIGN / [REF] Decisions Table

| ADR | Title | Slice |
|---|---|---|
| ADR-181 | Update activity is a read through the status store | 02 |
| ADR-182 | Update moments live in a sibling hash, outside the advance script | 03 |
| ADR-183 | Cancellation is an ambient token with the paging methods widened | 04 |
| ADR-184 | Connection health is a recorded verdict classified by ValidateConnection | 05 |
| ADR-185 | Recent problems is a bounded in-process sink handed to the logger at builder time | 06 |
| ADR-186 | The header summary is live, the section payloads are fetched on open | 02 |

---

## Wave: DESIGN / [REF] Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Nothing outside the update package injects `ConcurrentDictionary<UpdateKey, UpdateStatus>` | ArchUnit — `UpdateActivitySeamArchUnitTest`, following the established `*SeamArchUnitTest` convention |
| `UpdateCancellationContext` is written only by `UpdateQueueService` | ArchUnit — no other type may reference its setter |
| Only `ConnectionHealthService` records a health verdict | ArchUnit — single writer of `ConnectionHealthVerdict` |
| No component fetches connection health directly; the popover reads it through `SystemActivityService` | Biome / import rule, mirroring the existing `useRbac` constraint |
| The moments hash is never read or written from inside a Lua script | Unit test asserting both existing scripts are byte-identical to their current text |
| Every new admin route carries `RbacGuard(SystemAdmin)` | Integration test enumerating the new routes, the way the existing guarded-route tests do |

---

## Wave: DESIGN / [REF] Open Questions — deferred to DISTILL/DELIVER

| # | Question | Deferred because |
|---|---|---|
| OQ-1 | Exact checkpoint granularity for cancel | Slice 04's probe measures it; AC-04.2 asserts the measured number |
| OQ-2 | Whether the moments hash needs its own expiry | Depends on whether an abandoned key can leave a moment behind — answerable once the store code exists |
| OQ-3 | Whether Linear and CSV should gain `authentication_failed` | Slice 05 ships with the three connectors that have it; the other two read `Unreachable` honestly until someone needs better |
| OQ-4 | Ring buffer size for recent problems | The slice-06 pre-check counts a real day's warnings first; guessing a number before that is how the section becomes noise |
| OQ-5 | Whether `RequiresReconnect` on the connection DTO should later fold into the verdict | Two sources for one question is a real smell, but merging them touches the connection edit page, which this Epic does not otherwise touch |
| OQ-6 | Whether name resolution needs caching | DDD-11 — measure on a real instance rather than pre-optimise a popover open |

---

## Wave: DESIGN / [REF] Wave Decisions Summary

**Pattern**: ports-and-adapters, extended. No new architectural style.
**Paradigm**: OOP (C#) / functional-leaning React. Unchanged, not re-litigated.
**Key components**: `IUpdateStatusStore` (extended to enumerate), `UpdateActivityService` (new read
model), `UpdateCancellationContext` (new, sibling of the write-back round context),
`ConnectionHealthService` + verdict row (new, replacing the OAuth-only aggregator),
`RecentProblemsSink` (new), `TaskManagerIcon` + `TaskManagerPopover` (new).

**Constraints established**
- The Redis ordinal hash and both its Lua scripts are frozen. Anything richer goes beside them.
- The single-container product must be unaffected: no Redis means the in-process store and the whole
  surface still works.
- Cancellation and failure must both leave the write-back round and the held-update set in a state that
  drops nothing silently. Same invariant, two doors.
- Every new route is System-Administrator-guarded, matching what the refresh log and the log file
  already decided.
- Every user-visible label renders the tenant's configured Terminology.
- The connector port widens only where it pages.

**Upstream changes**: one. DISCUSS S15/D9 understated what the connectors can already say —
`authentication_failed` exists as a `ConnectionValidationResult.Code` on three of five connectors.
Slice 05 shrinks accordingly, and its probe now confirms coverage rather than discovering a mechanism.
Recorded in full under *Correction to a DISCUSS assumption* above.

---

## Wave: DESIGN / Tier-2 Expansion Menu

| Trigger | Fired | Suggested expansion |
|---|---|---|
| Contested decision with a live alternative | **Yes** — DDD-4 was chosen over two alternatives, and the probe can still overturn it | `rejected-alternatives` |
| Quality attributes in tension | **Yes** — multi-replica correctness against hot-path cost drives DDD-3 and DDD-9 both | `trade-off-analysis` |
| Novel pattern | No — every seam mirrors one already in the codebase | |
| Performance budget unverified by spike | Partly — OQ-1 and OQ-4, both already scheduled as probes | |

Suggested expansions (triggered by: contested decision, quality attributes in tension):

- `rejected-alternatives` — why the serialised-record Redis encoding, the phase-boundary-only cancel and
  the typed-exception health path were weighed and set aside
- `trade-off-analysis` — the multi-replica-correctness vs hot-path-cost matrix behind DDD-3 and DDD-9

Apply? `[Y/n/all/none/custom]`

---

# Post-DESIGN Reconciliation — #5877, 2026-09-14

DESIGN closed on 2026-08-23. **#5877** was raised on **2026-08-31** and linked Related to this Epic,
so it is the one input the six slices were not planned against. Reconciled here rather than folded
in silently, because it changes sequencing and possibly scope.

## What #5877 reports

A field report via Jan McConnell on behalf of a user running 26.8.14.1 standalone (Tauri, macOS,
SQLite) against Jira Data Center. The symptom was "keeps getting hung updating team metrics",
recovering only on restart.

What the logs showed: a Portfolio refresh ran **77.7 minutes**, failed on a DNS error when the machine
slept, and its coalesced follow-up immediately retook the lane. From 06:11 until the 09:49 restart,
**every Team refresh logged "already queued or being processed" — 3h38m with zero team updates**.
After the restart the same three teams completed in 23s, 31s and 26s.

Nothing was deadlocked. Team updates were queued behind a Portfolio refresh that would not finish.

## What was verified against the code

The bug report carries its own root-cause analysis. It was re-derived from the code on 2026-09-14
rather than accepted, and it holds on all three counts — recorded as **S23, S24 and S25** in the
Current-State Surface Inventory above, with file and line citations.

Worth noting for whoever picks this up: S24 is not a missing optimisation, it is an **inconsistency**.
Two sibling methods in the same class fetch by key; one chunks and carries a comment explaining that
the URL has to survive a proxy, the other does not.

## How it relates to the six slices

| #5877's proposed work | Relation to this Epic | Verdict |
|---|---|---|
| **A** — chunk `GetParentFeaturesDetails` at `ReferenceIdsPerQuery` | None. A Jira paging asymmetry (S24) that shares only the report that found it. One line, low risk, no dependency on any slice. | **Ship independently, now.** Do not wait for this Epic. |
| **B** — per-type lanes, or N consumers, so a Portfolio cannot starve Teams | Collides with slice 04. Both rewrite `UpdateQueueService`'s execution path; slice 04 adds a `CancellationTokenSource` per admitted key and an ambient scoped token (D5, DDD-4). Doing them apart means touching that class twice, and B has to reason about the write-back round boundary and the execution lock that slice 04 is already reasoning about. | **Sequence with slice 04.** Slice 04's probe (P5, S9+S10) answers part of B for free: whether cancellation can reach inside a connector call determines whether an eviction path is even possible. |
| **C** — total-duration watchdog plus a log line naming what holds the lane | Subsumed. C is a worse version of slices 02 and 03: it puts the diagnosis in a log file the operator has to choose to read, which is exactly the habit slice 06 exists to break (S17). The task list *is* C's answer, delivered to the surface the operator already looks at. | **Covered by 02 + 03.** Only the wall-time *bound* is genuinely new; the *visibility* half is the Epic's subject. |

## What this changes

1. **The Epic's priority case is stronger, not its scope.** #5877 is a user who watched an instance
   appear wedged for 3h38m and had no way to see why, no way to stop the refresh holding the lane, and
   no signal that anything had failed. That is slices 01, 02, 03 and 04 described from the outside by
   somebody who did not know they were asking for them.
2. **Slice 04 gains a second justification.** It was the riskiest slice (P5) and the one most likely to
   be cut on cost. It is now also the fix for a reported production problem.
3. **S23 belongs in slice 02's read model.** A list that shows a Portfolio refresh running and three
   Team refreshes queued is only honest if it makes clear the Teams are queued *behind* that Portfolio,
   not merely waiting. Deferred item **G** (queue position, Out of Scope) was cut as a nicety; under
   S23 it is closer to the point. Resolved below — the naming half is now in slice 02.

## Decisions taken — 2026-09-14

**B stays #5877's own Story, delivered adjacent to slice 04.** Not a seventh slice. Every slice in this
Epic shows the operator something they could not see before; B is a throughput change with no surface,
and folding it in would have made it the one slice that breaks that property. The argument for merging
was never the subject matter, it was avoiding two passes over `UpdateQueueService` — and adjacency buys
that without a re-parent. B is built in the same working session as slice 04, on the same branch, after
slice 04's P5 probe has reported: whether cancellation can reach inside a connector call decides whether
an eviction path exists at all, and B's shape depends on that answer.

**Deferred item G is split; the naming half lands in slice 02.** A queued row says what is holding the
lane — "behind <Portfolio name>" — and nothing more. No ordinal position and no wait estimate: an
estimate needs historical durations, which drags `RefreshLog` into slice 02's read path to produce a
number that is wrong in exactly the case #5877 describes, where the lane-holder is the one that will not
finish. The naming is what makes the list honest; the arithmetic is what makes it expensive and wrong.

No ADO work items were created, removed or re-parented as part of this reconciliation.

---

# Wave: DISTILL — slice 01

Run 2026-09-14, against slice 01 only. The other five slices are not distilled yet.

Wave-decision reconciliation: passed, 0 contradictions. DESIGN's one correction to DISCUSS (S15/D9, the
connectors' existing `authentication_failed` code) lands in slice 05 and does not touch slice 01. The
Post-DESIGN reconciliation above changes slice 01's priority, not its content.

## Wave: DISTILL / [REF] Scenario list

All in `Lighthouse.Backend.Tests/API/Integration/TaskManager/`, categories `acceptance` +
`epic-5511-task-manager` + `slice-01`.

| Scenario | Tags | AC |
|---|---|---|
| `A_team_refresh_that_fails_tells_the_browser_it_failed` | `@walking_skeleton @driving_port @real-io @error` | AC-01.1 |
| `A_portfolio_refresh_that_fails_tells_the_browser_it_failed` | `@driving_port @real-io @error` | AC-01.1 |
| `A_team_refresh_that_works_still_tells_the_browser_it_completed` | `@driving_port @real-io` | AC-01.1 |
| `A_failing_refresh_is_announced_as_queued_and_then_as_failed` | `@driving_port @real-io @error` | AC-01.1 |
| `A_failed_refresh_still_records_that_it_did_not_succeed` | `@driving_port @real-io @error` | AC-01.2 |
| `A_failed_refresh_still_finishes_its_write_back_round` | `@driving_port @real-io @error` | AC-01.3 |
| `Asking_for_a_refresh_that_goes_on_to_fail_is_still_accepted` | `@driving_port @real-io @error` | AC-01.5 |
| `A_caller_awaiting_an_update_still_sees_the_failure_it_always_saw` | `@driving_port @real-io @error` | AC-01.5 |

Error-path share: 6 of 8. The positive control is deliberate — without it, "always report Failed"
satisfies every other scenario in the file.

**Not acceptance scenarios, and why:**

- **AC-01.4** (held work is let go behind a failed refresh) →
  `UpdateQueueServiceTests.EnqueueUpdate_TheUpdateFails_StillLetsGoOfTheWorkHeldBehindIt`. A hold only
  parks while the key it waits on is `Queued` rather than running, and the window between a refresh being
  admitted and the queue picking it up cannot be stood in deterministically from outside. That class owns
  the status dictionary, so there the precondition is exact rather than raced for.
- **AC-01.6** (both authentication configurations) → `TeamDetail.test.tsx`, parametrised over
  authentication-off and RBAC-on-as-System-Administrator. The backend half needs no scenario: the queue
  never consults the caller, so the push cannot differ by configuration, and who may subscribe to the hub
  at all is already pinned by `S3_UpdateNotificationHubAuthorizeTests`.

## Wave: DISTILL / [REF] Test placement

`API/Integration/TaskManager/` — a new folder, mirroring `FasterUpdates/` and `QuietWriteBack/`, with
`TaskManagerAcceptanceTest` as the Epic-wide harness so slices 02-06 inherit it rather than each standing
up its own host. `Scenarios.cs` + `Specifications.cs` partial-class split per the project's existing
convention.

## Wave: DISTILL / [REF] Ports and doubles

| Port | Class | Treatment |
|---|---|---|
| The scheduled refresh (`ITeamUpdater` / `IPortfolioUpdater`) | Driving | Real, through the production queue in its own DI scope |
| `POST /api/latest/teams/{id}` | Driving | Real, over `Factory.CreateClient()` |
| `IUpdateStatusStore`, `IUpdateExecutionLock`, `WriteBackRound`, `IRefreshLogService` | Driven internal | Real, EF over SQLite |
| `IHubContext<UpdateNotificationHub>` | Driven internal | **Recorded.** The terminal status is never readable after the fact — the store drops the key as the run ends — so the push is the only place the answer exists |
| `IWorkTrackingConnector` | Driven external | Faked; made to throw for the failure scenarios |
| `IForecastService`, `ILicenseService` | Driven external / non-deterministic | Faked |

One harness trap worth recording: the queue pushes the same `UpdateStatus` **object** it goes on to
advance, so a recorder that keeps the reference shows every earlier push wearing the last one's status.
`CapturedUpdateNotifications` copies each push, which is what a real client is sent anyway.

## Wave: DISTILL / [REF] Upstream findings

1. **`InProgress` is never pushed to the browser.** `EnqueueUpdate` pushes `Queued` and the terminal
   status; `RunUpdateAsync` advances the store to `InProgress` without notifying. A browser therefore
   cannot today tell a refresh that is running from one that is merely waiting. Not a slice 01 defect —
   slice 01 changes the last word and nothing else — but it is **slice 02's problem**, which is exactly
   the "what is running right now" question. Slice 02 either reads the store through its new route (the
   design's plan, and enough on its own) or adds the push. Recorded, not fixed.
2. **`UpdateType` has five members and a portfolio refresh is `Features`, not `Portfolio`.** Already
   noted as S11 for the frontend union; it bites backend test authors too.

---

# Wave: DELIVER — slice 01

Delivered 2026-09-14. ADO Bug **#5788**. Commits held locally at the user's request — CI is red upstream
on unrelated ServiceNow tests.

## What changed

One production statement: `UpdateServiceBase.TriggerUpdate` no longer catches the exception from
`Update()`. The `finally` is untouched, so the write-back flush and the round summary still run on the
way out. The queue's existing `catch` then records `Failed` and pushes it — that half already worked and
was already tested; nothing there needed changing.

**The swallow was the whole bug.** All three updaters (`TeamUpdater`, `PortfolioUpdater`,
`ForecastUpdater`) already let exceptions out of `Update()` and already write their `RefreshLog` row with
`Success = false` from their own `finally`. The single `catch` in the base class was the one place the
truth was lost.

## Why the log line was removed rather than rethrown

The obvious change is `catch { log; throw; }`. That would put three accounts of one failure in front of
an operator — the updater's line, the queue's line, and the summary line — in a codebase that has a whole
Epic (#5687 slice 01) about not doing that. What an operator actually needs from the removed line is the
*reason*, and the reason is not in it: `BuildUnreadableSecretReason` attaches to `outcome.Reason`, which
prints on the summary line's `reason=` field. So the line was dropped and the queue's report is the
single one.

Two existing tests pinned that line and were rewritten to assert what now happens instead — the failure
**propagates**, observable on the queue double:

- `TeamUpdaterTest.TriggerUpdate_WorkTrackingSystemUnreachable_*`
- `PortfolioUpdaterTest.TriggerUpdate_CredentialCannotBeRead_TheFailureStillPropagatesAndIsReportedOnce`
  — whose name had been describing an intention the code did not keep.

## AC-01.5, answered — the blast radius

`EnqueueAndAwaitAsync` has exactly **two** callers: `TeamController.DeleteTeam` and
`PortfolioController.DeletePortfolio`. Both pass their own inline lambda and never route through
`UpdateServiceBase.TriggerUpdate`, so the removed catch cannot reach them. `RunAwaitableUpdateAsync`
already calls `tcs.TrySetException(ex)`; that path was correct before this slice and is pinned by a
scenario now.

The fire-and-forget callers (`TriggerUpdate` on the two detail controllers, `DemoController`,
`PortfoliosController`, `TeamsController`, the two forecast trigger handlers and `PortfolioUpdater`
itself) return `void` and run on the queue's own loop, so nothing propagates to any of them.

One real consequence, and it was in a test double rather than in production: `UpdateServiceTestBase` ran
the enqueued task with `.Wait()` and would have surfaced the exception at the trigger — a place it never
reaches in the running application. The double now catches like the real queue and exposes
`WhatTheRefreshThrew`, which is what the two rewritten tests read.

## Frontend

`"Failed"` was already in the `UpdateProgress` union and already stopped the spinner — and then showed
nothing, because the status never arrived. A new `RefreshStatusIcon` renders a `CloudOff` in error colour
with a `titleAccess` label; Team detail and Portfolio detail both use it, each naming the entity with its
configured Terminology (`Last ${teamTerm} refresh failed` / `Last ${featuresTerm} refresh failed`). The
flag clears on the next `Queued`, `InProgress` or `Completed`.

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| `dotnet test` (connector categories excluded) | 6682 passed, 1 environmental failure |
| `pnpm test` | 370 files, 5132 tests, all green |
| `pnpm build` | clean, Biome included |

The one backend failure is `ServiceProviderValidationTest.ServiceContainer_BuildsWithoutScopeViolations_*`
— an `IOException` deleting its own SQLite file in teardown because the handle is still held. It
reproduces on unmodified `main` with the change stashed, and it is intermittent (it passed earlier in the
same session). Environmental, not a regression.

## Mutation testing

Full write-up in `mutation/results.md`. Frontend **94.74 %**, gate met; the first run scored 65 % and its
survivors were worth more than the number — they found that Portfolio detail had no failed-state test at
all, and that nothing proved the failure clears when a page is opened just after a refresh finished.
Five scenarios were added to close them.

Backend **70.00 %** on `UpdateServiceBase.cs`, with **zero survivors in `TriggerUpdate`** — the method
this slice changed. All 18 survivors are in code the slice does not touch: 11 log-message mutations,
4 in the hosted-service loop that no test starts, and 3 genuine pre-existing gaps recorded rather than
fixed. Excluding the accepted ones the rest kills 42 of 45. Whether a 70 % file-level number clears the
gate when the changed method is clean is a judgement call for the maintainer, not something this slice
should settle by writing coverage for behaviour it never touched.

---

# Wave: DISTILL — slice 02

Run 2026-09-14. Reconciliation: passed, 0 contradictions. Slice 02 inherits slice 01's truthful terminal
status (D6), which is why it could only be distilled after slice 01 shipped.

## Wave: DISTILL / [REF] The contract this slice fixes

`GET /api/latest/update/tasks`, System-Administrator-guarded, answering a JSON array with one object per
admitted piece of work:

| field | meaning |
|---|---|
| `updateType` | `Team`, `Features`, `Forecasts`, `TeamDelete` or `PortfolioDelete` — by name, because the browser's own union is strings |
| `id` | the entity's id |
| `name` | resolved on the read path (D8), falling back to something that still identifies the work when the entity has gone |
| `status` | `Queued` / `InProgress` / `Completed` / `Failed`, by name |
| `waitingBehind` | the entity holding the lane; absent for work that is running |

`waitingBehind` is the naming half of deferred item G, decided 2026-09-14. Everything else follows
US-02 as written.

## Wave: DISTILL / [REF] Scenario list

**Backend** — `API/Integration/TaskManager/Slice02SeeWhatIsRunning{Scenarios,Specifications}.cs`,
categories `acceptance` + `epic-5511-task-manager` + `slice-02`. All nine observe the endpoint over
HTTP, so the shape of the port the controller reads underneath stays DELIVER's decision.

| Scenario | Tags | AC |
|---|---|---|
| `A_refresh_that_is_running_is_listed_by_name` | `@walking_skeleton @driving_port @real-io` | 02.1, 02.3, 02.9 |
| `A_portfolio_refresh_that_is_running_is_listed_by_name` | `@driving_port @real-io` | 02.1, 02.3 |
| `A_refresh_waiting_its_turn_is_listed_as_queued` | `@driving_port @real-io` | 02.1, 02.3 |
| `A_queued_refresh_names_what_is_holding_the_lane` | `@driving_port @real-io` | item G |
| `A_refresh_whose_entity_has_gone_is_still_listed_by_what_it_is` | `@driving_port @real-io @error` | 02.3 |
| `With_nothing_running_the_task_list_is_empty_and_says_so_without_failing` | `@driving_port @real-io` | 02.7 |
| `The_task_list_is_refused_to_somebody_who_is_not_a_system_administrator` | `@driving_port @real-io @error` | 02.6 |
| `The_task_list_reports_work_admitted_through_the_shared_store` | `@driving_port @real-io` | 02.2 |
| `A_delete_that_is_waiting_is_listed_as_something_a_reader_can_understand` | `@driving_port @real-io` | 02.3 |

**Frontend** — `components/App/Header/TaskManagerIcon.test.tsx` (8) and one in `Header.test.tsx`.

| Scenario | AC |
|---|---|
| lists what is running and what is waiting, by name | 02.3 |
| says which of them is running and which is waiting | 02.3 |
| says what a waiting refresh is waiting behind | item G |
| names the kind of thing being refreshed in the reader's own words | 02.4 |
| refreshes itself when the instance says something changed | 02.5 |
| says in words that nothing is running, rather than showing an empty box | 02.7 |
| does not appear at all for somebody who is not a System Administrator | 02.6 |
| does not ask for the list when the reader may not see it | 02.6 |
| shows the activity icon beside the OAuth health icon, not instead of it | 02.8 |

Two of these are worth calling out as more than restatements of an AC. *The task list is refused …
the same way the refresh log does* asks both endpoints and compares their answers rather than writing a
status code down twice and letting the two drift apart. *Does not ask for the list when the reader may
not see it* is the half of AC-02.6 that a hidden icon does not cover: not rendering a control is not the
same as not fetching instance-wide data for someone who may not read it.

## Wave: DISTILL / [REF] Scaffolds

- `services/UpdateSubscriptionService.ts` — `UpdateTaskType`, `IUpdateTask` and `getRunningTasks()` on
  the port; the implementation throws with a `__SCAFFOLD__` marker. It throws rather than returning
  `[]`, so a specification that reaches it fails loudly instead of quietly agreeing the instance is idle.
- `components/App/Header/TaskManagerIcon.tsx` — throwing scaffold. DESIGN splits the popover out as
  `TaskManagerPopover`; that is a structure decision the scaffold deliberately does not make, and the
  specifications drive the icon rather than the split.
- `tests/MockApiServiceProvider.ts` — `getRunningTasks` added to the update-subscription mock.

No backend scaffold. Every backend scenario is driven over HTTP, so a missing route is a 404 and the
assertion fails on the answer rather than on a compile error.

## Wave: DISTILL / [REF] Red gate

All 18 fail, each for the right reason:

- the nine backend scenarios on `404` from `/api/latest/update/tasks`
- the eight icon specifications on the scaffold's own `Not yet implemented` message
- the header scenario on there being no activity control, with its OAuth half passing — which is what
  makes it a guard against replacing the old icon rather than adding beside it

Everything else stays green: backend 6683, frontend 5138.

## Wave: DISTILL / [REF] Answered elsewhere, deliberately

- **AC-02.1, multi-replica half** — that the Redis store answers about work admitted by *any* replica is
  a promise about an adapter method that does not exist yet, so there is nothing to drive from outside.
  It is authored at the start of DELIVER beside the port change, in `Integration/Containers/`, where
  `RedisContainerFixture` and the cross-pod fixtures already live.
- **AC-02.9** — "verified against a real instance with a real connector refresh in flight" is satisfied
  by the backend scenarios, which run the production queue against a gated connector rather than seeding
  a status dictionary. What they do not do is exercise a real tracker; that stays a manual check at
  slice close, as it was for slice 01.

## Wave: DISTILL / [REF] Carried into DELIVER

1. `UpdateType` has five members and the frontend union knew three. `UpdateTaskType` widens it for the
   task list only, leaving `IUpdateStatus` alone — the detail pages subscribe to three types and have no
   business knowing about deletes.
2. `InProgress` is never pushed to the browser (recorded at slice 01). The scenarios here read the list
   rather than the push, so slice 02 can be built without adding one — but the popover will only be as
   live as `GlobalUpdateNotification`, which *is* raised on every transition.
3. The endpoint being replaced, `/update/status`, stays for now: `useUpdateAll` reads it. Removing it is
   not in this slice.

---

# Wave: DELIVER — slice 02

Delivered 2026-09-14. ADO **#5840**. Commits held locally; CI is still red upstream on ServiceNow.

## What changed

**The port.** `IUpdateStatusStore.GetAdmittedWork()`. The in-process store hands back its dictionary's
values. The Redis store reads the whole hash — the shape `HasActiveWork` already scans — and
reconstructs each row's entity from the field name, because the hash keeps one ordinal per key and the
value alone says only how far the work got. A field that does not parse is skipped rather than thrown
on: one unreadable entry written by a later version must not cost an operator the whole list.

**The controller.** `UpdateController` stops injecting `ConcurrentDictionary<UpdateKey, UpdateStatus>`
and reads the port. That is the correctness fix hiding inside AC-02.2, not a tidy-up — the dictionary is
per-process, so the status endpoint has been answering about whichever replica took the request.

**The route.** `GET /api/latest/update/tasks`, guarded the way the refresh history is and for the same
reason: it names every entity on the instance. Each row carries type, id, name and status, with the name
resolved as the list is read (D8) so a rename shows immediately and the update path stays ignorant of
anything a screen needs.

**`waitingBehind`.** The queue runs one thing at a time, so whatever is running is what everything
queued is waiting on. That is the entire claim — not a position, not an estimate. Item G's naming half.

**The header.** An activity icon with a count, beside the OAuth one rather than instead of it, opening a
popover that lists the work. Kinds render in the tenant's Terminology. Deletes say they are removals.
The popover follows the instance over the existing `GlobalUpdates` group — no new transport — and
neither renders nor fetches for a non-administrator, because a hidden control is not the same as not
asking for instance-wide data on somebody's behalf.

## Decisions taken while building

- **A failed read leaves the list as it was** rather than emptying it. Deliberately unlike
  `getGlobalUpdateStatus` beside it, which swallows and reports an idle instance. Here a confident
  "nothing is running" is the one answer worse than none.
- **`UpdateTaskType` widens the browser's three update types to five for the task list only**, leaving
  `IUpdateStatus` alone. A detail page subscribes to three types and has no business knowing about
  deletes.
- **`/update/status` stays.** `useUpdateAll` reads it. It now answers through the port, so it is correct
  on multiple replicas for the first time; removing it is not this slice's business.

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| `dotnet test` (connector categories excluded) | 6695 passed, 0 failed |
| `pnpm test` | 371 files, 5155 tests, all green |
| `pnpm build` | clean, Biome included |
| Mutation — backend | **93.75 %**, gate met |
| Mutation — frontend | **73.33 %**, under the gate — see `mutation/results.md` |

The frontend number is under the gate because two thirds of the surviving mutants are MUI popover
geometry and React effect bookkeeping. Every behavioural promise is pinned; excluding the presentational
mutants the rest kills 55 of 62. Recorded rather than engineered away, the same call slice 01's backend
70 % raised.

## Not done here

- **Slice 04's cancellation probe.** DESIGN asks for it during this slice because its answer can resize
  the Epic. Not run — slice 02 was already the largest slice, and the probe is a question about
  `IWorkTrackingConnector`'s call boundaries rather than about anything this slice touches. It is the
  first thing slice 03 or 04 should do, and it is still the Epic's highest-uncertainty question.
- **A real-tracker check (AC-02.9's last mile).** The scenarios drive the production queue against a
  gated connector rather than a seeded dictionary, which is what the criterion asks for; watching it
  against a live Jira stays a manual step at slice close.

---

# Wave: DISTILL — slice 03

Run 2026-09-14. Reconciliation: passed, 0 contradictions. Slice 03 inherits slice 02's task list — there
is no row to put a duration on before it — and nothing in DESIGN or the ADRs accepted since contradicts
US-03 as DISCUSS wrote it.

## Wave: DISTILL / [REF] The contract this slice fixes

Each row of `GET /api/latest/update/tasks` gains **one** field:

| field | meaning |
|---|---|
| `elapsedMs` | how long the work has been in the state this row's `status` names, measured by the instance. Running counts from the moment it started, waiting from the moment it was admitted. A whole number of milliseconds, never negative, `null` when the moment behind it was never recorded |

One field rather than two moments, decided here. The alternative — shipping `queuedAt` and `startedAt`
and letting the browser subtract — fails AC-03.4 by construction: the subtrahend would be the reader's
own `new Date()`, so two people looking at one instance read different answers off the same row, each
wrong by however far their laptop has drifted. Computing it server-side also makes the degraded case a
single `null` rather than a pair of absences the browser has to reason about.

Which moment it counts from follows `status`, which is why one number can read as both "running for 12s"
and "queued for 3m" without the browser knowing which moment it is looking at.

`UpdateStatus` carries `QueuedAt` and `StartedAt` as `DateTimeOffset?` (AC-03.1). Both stores stamp them,
because `Advance` carries an ordinal and no moment — a caller cannot supply one without widening the port,
and the ADR puts the write *alongside the ordinal*, which is the adapter. Both store constructors
therefore take `ILighthouseClock`. The controller reads the same seam to compute `elapsedMs`, deliberately
one seam rather than two: a store on `TimeProvider` and a controller on `ILighthouseClock` would let a
test move one and not the other, and the scenario that proves AC-03.4 depends on moving exactly one thing.

## Wave: DISTILL / [REF] Scenario list

**Backend acceptance** — `API/Integration/TaskManager/Slice03HowLongHasItBeenGoing{Scenarios,Specifications}.cs`,
categories `acceptance` + `epic-5511-task-manager` + `slice-03`. All seven observe the endpoint over HTTP.
The instance clock is pinned for the fixture, through a new `ConfigureAdditionalServices` hook on
`TaskManagerAcceptanceTest`; elapsed time asserted against the real wall clock could only ever be
"greater than zero", which a stopwatch started at the wrong moment satisfies.

| Scenario | Tags | AC |
|---|---|---|
| `A_running_refresh_says_how_long_it_has_been_running` | `@walking_skeleton @driving_port @real-io` | 03.1, 03.4 |
| `A_refresh_waiting_its_turn_says_how_long_it_has_been_waiting` | `@driving_port @real-io` | 03.1, 03.4 |
| `Elapsed_time_follows_the_instances_own_clock_and_nothing_else` | `@driving_port @real-io` | 03.4 |
| `A_coalesced_follow_up_starts_its_wait_again` | `@driving_port @real-io` | 03.3 |
| `Work_admitted_before_the_upgrade_is_listed_without_a_duration_beside_work_that_has_one` | `@driving_port @real-io @error` | 03.5 |
| `A_running_refresh_whose_start_went_unrecorded_says_nothing_rather_than_reporting_its_wait` | `@driving_port @real-io @error` | 03.5 |
| `A_replica_whose_clock_runs_behind_never_makes_a_row_say_it_started_in_the_future` | `@driving_port @real-io @error` | 03.4, 03.5 |

Error-path share: 3 of 7.

Three are worth calling out as more than restatements of an AC:

*Elapsed time follows the instance's own clock* reads the row twice. Between the readings the instance
clock moves an hour and the test's own moves milliseconds, and the answer has to grow by the hour. A
duration computed anywhere else passes every other scenario in the file and fails this one, which is the
only reason it exists.

*A running refresh whose start went unrecorded* is the half-recorded entry, and it is the case a fallback
gets wrong quietly. The row has an admission moment and no start moment; falling back to the admission
reports the wait as the run — forty minutes against something that may have started seconds ago, which is
worse than saying nothing, because it is believable.

*A replica whose clock runs behind* is not a hypothetical. The moments are written by whichever replica
handled the transition and the elapsed time computed by whichever replica answers the read, and their
clocks do not agree to the millisecond. "Started in the future" is an ordinary state; a row reporting it
as a negative duration reads as broken rather than as new.

Both degradation scenarios carry a **control row** that does have its moments, asserted in the same list.
Without one they pass on a build where durations do not exist at all — they assert an absence, and the
absence is currently free. The control is also the honest shape of a rolling upgrade: a mixed list, some
rows from the old replica and some from the new.

**Backend store guarantees** — `Integration/Containers/TaskManagerMultiReplicaTests.cs`, new fixture
`TaskManagerMomentsMultiReplicaTests`, categories `epic-5511-task-manager` + `slice-03` +
`requires-docker`. AC-03.2 and AC-03.3 are enforced inside Lua, across connections, over state neither
replica owns; an in-process dictionary has nothing to say about any of it.

| Test | AC | Red at hand-off |
|---|---|---|
| `MomentsRecordedByOneReplica_AreReadBackByAnother` | 03.1, 03.2 | yes |
| `Advance_StillRefusesToMoveAKeyBackwards_NowThatAMomentTravelsBesideTheOrdinal` | 03.2 | no — guard |
| `Requeue_StillRefusesAKeyAnotherReplicaRemoved_AndLeavesNoMomentBehindEither` | 03.2 | no — guard |
| `Requeue_StartsTheWaitAgain_AndForgetsTheRunThatJustEnded` | 03.3 | yes |
| `Remove_TakesTheMomentWithTheOrdinal_RatherThanLeavingItToAccumulate` | ADR-182 open question | yes |

The last one closes an open question rather than an AC. ADR-182 left "whether the moments hash needs its
own expiry, in case an abandoned key leaves a moment behind" to be answered once the code existed. An
orphan is invisible from the port — the ordinal is gone, so the key is gone from every list — so the test
reads the sibling hash directly, the only white-box assertion in the slice. It asserts the moment is there
while the work is admitted **and** gone after `Remove`: the presence half is what stops it passing on a
build where the hash does not exist at all.

**The script freeze** — `Services/Implementation/BackgroundServices/Update/RedisUpdateStatusScriptFreezeTest.cs`.
The brief asks for this as a test rather than a probe, and it is the whole safety argument for AC-03.2:
slice 03 was allowed to add fields to this store only by not touching either script. The scripts are held
as literals copied from what shipped and compared character for character, in both their authored form and
the KEYS/ARGV form the client rewrites them into — freezing only the readable half would miss a change in
how parameters are bound, which is a change to the script even though the source looks untouched. No Redis
needed, so it runs everywhere the suite does.

It passes on arrival, which is correct for a guard, so it was verified by mutation instead: changing `>=`
to `>` in the advance script — one character, and precisely the change that would let a key go backwards
under load — fails 2 of its 3 tests. Reverted.

**Frontend** — `utils/date/formatElapsed.test.ts` (7) and `components/App/Header/TaskManagerIcon.test.tsx`
(5, under a `how long it has been going` describe).

| Scenario | AC |
|---|---|
| says how long a running refresh has been running | 03.5 |
| says how long a waiting refresh has been waiting, as well as what it waits behind | 03.5 |
| still lists a refresh whose duration the instance never recorded | 03.5 |
| gives the rows that have a duration theirs, without inventing one for the row that has none | 03.5 |
| does not count time on its own, however long the reader leaves the popover open | 03.4 |

The last is AC-03.4's browser half, and it is a real risk rather than a formality: a component that starts
its own stopwatch is wrong after a reload, wrong for a refresh that began before the tab was opened, and
wrong by this machine's drift — which is most of the occasions somebody opens this popover. It renders a
row, advances fake timers an hour, and requires the row to still say what the instance said.

## Wave: DISTILL / [REF] Scaffolds

- **`UpdateStatus`** — `QueuedAt` and `StartedAt`, both `DateTimeOffset?`, with the ADR's reason for
  absent being legitimate recorded on the property.
- **Both stores** — constructors widened to take `ILighthouseClock`. `TryAdmit` stamps `QueuedAt` and
  nothing else does anything; the Redis store does not persist it. This is the *minimum* that compiles:
  the project's Sonar gate runs in-build as errors, and `S4487` rejects a constructor seam nothing reads,
  so a pure throwing scaffold is not available here. Everything the slice actually promises — persistence,
  the start moment, the reset, the deletion, the elapsed computation — is still absent.
- **`Lighthouse.Backend.Tests/TestHelpers/Clocks.cs`** — `Clocks.SystemUtc`, for the twenty-odd existing
  construction sites across ten test files that now have to hand the stores a clock they never read.
- **`TaskManagerAcceptanceTest.ConfigureAdditionalServices`** — a no-op virtual hook called last in the
  factory's `ConfigureServices`, so a slice can replace something the Epic-wide harness set up. Slice 03
  pins the instance clock through it. Slices 01 and 02 are unaffected.
- **`services/UpdateSubscriptionService.ts`** — `elapsedMs?: number | null` on `IUpdateTask`.
- **`utils/date/formatElapsed.ts`** — throwing scaffold with the `__SCAFFOLD__` marker.

No scaffold for the endpoint or the popover. Both exist; the new field is simply missing from what they
produce, so the assertions fail on the answer rather than on a compile error.

**Reuse checked and rejected once:** `utils/date/formatDuration.ts` takes a number of *days* plus a chart's
chosen axis unit, and exists so every point on one chart is expressed the same way. `formatElapsed` takes
milliseconds and picks its own unit per value, because these rows are independent — a refresh going four
seconds and one going two days sit side by side and each wants its own unit. Same shape, different
knowledge.

**One piece of knowledge moved:** reading the task list over HTTP (`TheTaskList`, `TheRowFor`, `Text`,
`Number`, `Describe`) was private to slice 02's specifications and is now `protected` on
`TaskManagerAcceptanceTest`, with slice 02's copies deleted. Slice 03 is its second consumer and slices 05
and 06 will be the third and fourth. Slice 02's seventeen scenarios still pass unchanged.

## Wave: DISTILL / [REF] Red gate

23 fail, each for the right reason — the value is missing, not the test:

- the **7 backend acceptance** scenarios on `elapsedMs` being absent from every row
- **3 of the 5 multi-replica** store tests on `QueuedAt` / `StartedAt` coming back `null`, and on the
  moments hash not existing. The other two are guards over behaviour this slice must *not* change; they
  pass on arrival by design
- the **7 `formatElapsed`** specs on the scaffold's own `Not yet implemented` message
- **4 of the 5 popover** specifications on the duration not being rendered. The fifth asserts a row
  without a duration still reads correctly, which is currently free — its evidence is the mixed-list
  scenario beside it, which fails

The 3 script-freeze tests pass, as a freeze test should on the day it is written; the mutation check above
is what makes that meaningful.

Everything else stays green: frontend 5156 of 5167, backend unchanged from slice 02's close.

## Wave: DISTILL / [REF] Answered elsewhere, deliberately

- **AC-03.2** has no acceptance scenario. Both guarantees are enforced inside Lua over a bare number, so
  the only honest place to assert them is against a real Redis with two stores on one connection — the
  multi-replica fixture above — and the claim that the scripts did not change is the freeze test. Driving
  either through the endpoint would prove nothing about the mechanism that carries them.
- **AC-03.5's rendering half** — a duration rather than a timestamp — is a frontend promise and lives in
  `formatElapsed.test.ts`. The backend half, degrading to a row without one, is in the acceptance
  scenarios, because whether the field is absent is the server's decision.

## Wave: DISTILL / [REF] Carried into DELIVER

1. **The popover shows a snapshot, not a running counter.** Nothing in US-03 asks for one, and the list
   already re-reads on every `GlobalUpdateNotification`, so a row is as fresh as the last transition. A row
   that sits at "running for 12s" for a minute while the popover is open is the known cost. Making it tick
   means interpolating from the server's number rather than reading a local clock — permitted by AC-03.4,
   but it is a new promise, so it is the maintainer's call and not DELIVER's.
2. **`ADR-182` was still `Proposed`, and is now `Accepted`.** The decision itself was never open — the
   brief records the maintainer ruling that closed it, and slice 04's `ADR-183`, decided later, was
   already accepted. The status field was the only thing lagging, and it is the one slice 03 builds on.
3. **The moments encoding is left open.** ADR-182 fixes the hash name and that it is written outside the
   scripts; nothing here pins how the pair is encoded in a field. The tests assert the moments survive and
   that the two hashes carry the same key set, which is what the design actually promises.
4. **Twenty-odd construction sites now pass a clock they never read.** The cost of putting the seam in the
   adapter rather than in `UpdateQueueService`. It buys `IUpdateStatusStore` staying as it is — the
   alternative was widening `Advance` to carry a moment on every transition, for the one transition that
   needs it.

---

# Wave: DELIVER — slice 03

Delivered 2026-09-14. ADO **#5841**. Commits held locally; the push is still blocked on ServiceNow
upstream, as it was at slice 02's close.

## What changed

**The moments.** `UpdateStatus` carries `QueuedAt` and `StartedAt`, both nullable. `UpdateMoments` owns
how the pair is written down — unix milliseconds either side of a bar — because that is the part ADR-182
deliberately left open, and because it is the only thing in the store with no Redis in it.

**The stores.** Both stamp the moments, which is why both now take `ILighthouseClock`. `TryAdmit` records
the admission, the advance into `InProgress` records the start and only that transition does, and
`Requeue` starts the wait again and forgets the run that ended. The Redis store keeps the pair in
`lighthouse:update-moments`, written and deleted alongside the ordinal by ordinary commands, never from
inside either script.

**The read.** `UpdateController` computes `elapsedMs` per row against the same clock: from the start
moment while running, from the admission while waiting, from neither once it has finished.

**The row.** `formatElapsed` picks its own unit per value, and the popover appends `for <duration>` when
there is one. Nothing ticks — see *Carried into DELIVER* below.

## The seam decision, and what it cost

The moments are stamped in the store adapters rather than in `UpdateQueueService`. `Advance` carries an
ordinal and no moment, so a caller cannot supply one without widening the port for the single transition
that needs it, and ADR-182 puts the write alongside the ordinal — which is the adapter.

The price is about twenty existing construction sites across ten test files that now hand the stores a
clock they never read. Paid deliberately: `IUpdateStatusStore` is unchanged, and every caller of it is.

Both the store and the controller read `ILighthouseClock` rather than the store reading `TimeProvider`.
One seam is what lets a test move time once; two would let a test move one and not the other, and the
scenario that proves AC-03.4 is precisely a test that moves exactly one thing.

## What the adversarial review found

Two reviews ran. The code-quality pass approved with no findings. The adversarial pass — briefed to break
the code rather than assess it — found three ways the slice could damage an instance rather than merely
misinform it. All three are fixed, each with the test that would have caught it.

1. **`Parse` threw on numbers that are valid `long`s and not valid instants.** Today in microseconds is
   1.79e15 and in ticks 6.4e17; both parse and both throw on the way to a `DateTimeOffset`. "A later build
   writing a finer unit" is the exact case the format claims to survive, and the claim was false. On the
   read path it cost the whole task list; on the advance path it abandoned a key that nothing would then
   run or remove. The original tests probed forgiveness only with non-numeric text, so the one input class
   that reached the throw was untested.

2. **The moments were declared best-effort and implemented as mandatory.** Every moments command runs
   after its ordinal has already been committed, so a Redis timeout escaping one of them left a key
   admitted with no runner — and the team it named stopped refreshing until somebody cleared the hash by
   hand. Before this slice `TryAdmit` was a single command and the failure mode did not exist; the slice
   created it. They are now best-effort in the code as well as in the prose.

3. **A finished row reported its time since admission under a `Completed` label**, which reads as time
   since it completed. Usually a two-statement window, permanent if the replica running it died inside
   one.

A fourth change came out of the same pass: the in-process store hands out snapshots rather than its live
entries. The queue advances an entry while a reader is part-way through it, and the Redis store already
rebuilt each row from what it read — so this is what makes the two implementations of the port agree.

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| `dotnet test` (connector categories excluded) | 6739 passed, 0 failed |
| `pnpm test` | 372 files, 5170 tests, all green |
| Biome | clean |
| `tsc -b` | clean |
| Mutation — backend | **95.45 %**, gate met |
| Mutation — frontend | **100 %**, gate met |

One failure seen mid-session and chased to ground rather than assumed:
`ServiceContainer_BuildsWithoutScopeViolations` failing on an `IOException` deleting its SQLite file in
teardown. It reproduces on unmodified `main`, and the assertion itself passes — a Windows file-handle
race in `Dispose`, not a scope violation and not this slice. It passed on the final run.

## Carried into DELIVER's successors

1. **The popover shows a snapshot, not a running counter.** Nothing in US-03 asks for one and the list
   re-reads on every `GlobalUpdateNotification`, so a row is as fresh as the last transition. A row
   sitting at "running for 12s" for a minute while the popover is open is the known cost. Making it tick
   means interpolating from the server's number rather than reading a local clock — permitted by AC-03.4,
   but a new promise, so it is the maintainer's call.
2. **ADR-182's expiry question is still open, and now it is answerable.** `Remove` deletes both hashes
   and a test pins that, but a crash between the two `HDEL`s orphans a moment, and nothing reaps it —
   `GetAdmittedWork` transfers the whole moments hash on every popover open. Bounded by crashes rather
   than by traffic, but unbounded in time for keys that never return, such as a deleted team. Deciding
   between a TTL, a reap on read, and accepting the leak needs a maintainer.
3. **`waitingBehind` assumes one lane per instance.** True per process, false under Redis with several
   replicas, where each runs its own item against one shared ordinal hash — so a queued row can be
   labelled as waiting behind something running on a different replica that it is not waiting for.
   Pre-existing, from slice 02, not introduced here. Worth a slice of its own or a correction in 04.

---

# Retroactive review — slices 01 and 02

Run 2026-09-14, after slice 03. Slices 01 and 02 shipped with mutation testing but without a refactor
pass or an adversarial review; this is that missing pass, and it is recorded here rather than in either
slice's DELIVER section because it happened after both were written.

Sixteen findings. **Five are fixed** — the two that broke the acceptance criteria of the slice that
introduced them, and then, on the maintainer's call, the three that had first been recorded as deferred.
The remaining eleven findings are recorded below, as seven entries — the last of them groups three
narrow queue paths that share one shape.

## Fixed

**The coalescing path never reported how a run ended.** `RunUpdateAsync` returned early whenever a
follow-up had been parked, skipping the terminal advance, the completion publish and the notification.
The regression test's own output is the clearest statement of what that cost: with the early return in
place, a refresh that threw left the browser told *"Completed, Queued"*. That is Bug #5788 — a failed
refresh reported as a success — surviving on the one path slice 01 did not walk. AC-01.1 did not hold.

**A forecast run cleared the portfolio's failed-refresh mark.** `updatePortfolioRefreshButton` accepted
both `Features` and `Forecasts` and wrote the one shared flag, so a forecast completing after a failed
refresh put the icon back to healthy over data that had never been updated. Forecasts are triggered by a
refresh that has just finished, so the window is not hypothetical. The team page never had this.

**A failed refresh was invisible to any page opened afterwards.** The queue removes the key as the run
ends and `UpdateNotificationHub.GetUpdateStatus` read only that store, so a page opened at nine in the
morning after an 03:00 failure saw `null` and rendered the healthy icon. US-01's promise held only for a
browser that had the page open at the instant of failure. The hub now falls back to the refresh log —
the durable record AC-01.2 already writes, and the only place that answer still exists once the key is
gone.

**One failed connect disabled every live update for the life of the page.** `connect()` left nothing to
retry, and with no `withAutomaticReconnect` and no `onclose` a dropped connection left `isConnected`
true, so every later invoke went to a dead socket. Because `getUpdateStatus` catches to `null` and the
detail pages no-op on `null`, that rendered identically to a healthy idle instance. There is now
automatic reconnect, an `onclose` that lets the next caller reconnect, and a record of this page's own
subscriptions so a reconnect can ask for them again — the server keeps them per connection, and every
caller here subscribes once on mount and never asks twice.

**The task list never filtered terminal states** while its sibling `GetUpdateStatus` always has. A key
orphaned by a pod that died before `Remove`, or by the shutdown drain's timeout, was listed and counted
for the life of the deployment while `/update/status` called the same instance idle. `GetTasks` now
filters to what is running and what is waiting. Slice 03's scenario that pinned the old behaviour pins
the new one instead: its premise — a finished row listed without a duration — was superseded by the
stronger promise that it is not listed at all.

## Recorded, not fixed

Ordered by what they cost an operator.

1. **The popover's first failed read asserts idleness.** `tasks` initialises to `[]` and an empty list
   renders "Nothing is being refreshed right now.", so a 502 or a 403 on the first read produces exactly
   the answer slice 02's own record called worse than none. The "leave the list as it was" comment is
   true only from the second read on: it protects a list that has already been filled once, and says
   nothing about the first attempt. Needs an error state the popover does not currently have.

2. **`waitingBehind` is wrong by construction on more than one replica.** Already carried from slice 03;
   repeated here because this review reached it independently. It also has a same-name variant: a
   `Features` and a `Forecasts` row for one portfolio render identically, so a row can appear to be
   queued behind itself.

3. **Two subscribers share one SignalR handler namespace.** `useUpdateAll` and `TaskManagerIcon` both
   register on `GlobalUpdateNotification`, and the handler-less `connection.off(name)` removes both.
   This works today only because React fires every cleanup before every create-effect and the awaits
   resolve in order. The reconnect bookkeeping added above inherits the same flaw rather than repairing
   it: `joinedGroups` is keyed by group name, so one of the two unsubscribing drops the group for both,
   and a reconnect after that would not ask for it again. Fixing it properly means counting subscribers
   per group, or giving each its own handler token — a change to how subscription is modelled, which is
   why it is here rather than in the commit above.

4. **Concurrent reads of the task list have no sequence guard.** "Update All" enqueues N teams, each
   raising a notification, each starting a `GET /update/tasks` with no `AbortController` — last response
   wins, whichever that is.

5. **The Jira keyed download pages by offset over unordered JQL.** `OrderedForOffsetPaging` is applied to
   the sweep and to release membership but not to `PrepareIssueKeyQuery`, so on Data Center an issue
   edited mid-walk can slide onto a page already read and go silently un-updated for that cycle. The
   comment claiming the full download shares this exposure is wrong.

6. **Three narrower queue paths lose a notification or a key.** An `AcquireAsync` throw sits outside both
   `try` blocks and strands the key permanently; `PublishCompletionAsync` and `NotifyListeners` are not in
   a `finally`, so a throw in the first skips the second after the key has already been removed; and
   `AbandonUnqueuedWork` completes its awaiter with a value its caller cannot read, so a delete that never
   ran returns 200.

7. **`UpdateNotificationHub` carries `[Authorize]` and no `RbacGuard`.** AC-02.6 guards the endpoint;
    the hub lets any authenticated user ask about arbitrary ids and learn which entities exist and when
    they refresh. No names leak, which is why it is last.

## What the review confirmed was right

The chunking boundaries in the Jira connector, including the empty and exact-multiple cases; duplicate
parent keys; partial chunk failure, which throws and falls back to a full download rather than reading a
gap as a deletion; `TryScheduleRerun`'s hardcoded `Advance(Completed)`, which cannot mask a failure
because `Failed` outranks `Completed` under the monotonic advance; and the badge and popover, which read
one array and so cannot disagree with each other.

---

# Wave: DELIVER — slice 04

Delivered 2026-09-14. ADO **#5842**. Cancel is honest for Jira and structural for the other four
connectors — see *Not done here*.

## What changed

**The ask.** `POST /api/latest/update/tasks/{updateType}/{id}/cancel`, System-Administrator-guarded,
answering 204, and idempotent in the strongest sense: accepted for work running, waiting, already
finished, or never admitted. A deletion is refused with 400 — see below.

**The carry.** `IUpdateCancellationNotifier`, a sibling of the completion notifier, publishing to every
replica. The token source lives in the process that admitted the work and the task list shows work from
any replica, so the pod taking the click is usually not the pod that can act on it.

**The hold.** `AdmittedCancellations` owns one `CancellationTokenSource` per admitted key, created as the
key is admitted and disposed as it leaves the store.

**The reach.** `UpdateCancellationContext`, an `AsyncLocal` set by the queue, read by `WorkItemService`
and handed to the connector as an explicit parameter. Eight paging methods on `IWorkTrackingConnector`
widened. Jira checks once per page in all three of its walks and passes the token to the request itself.

**The row.** `Cancelled` appended to `UpdateProgress` after `Failed`, with the ordinals frozen in a test.
A Cancel control per popover row, which re-reads the list rather than editing it in place.

## What the adversarial review found

Eleven findings, four blocking. All four fixed, the two worst with regression tests. Two of them were
defects this slice introduced rather than inherited:

**A cancelled delete reported success.** `EnqueueAndAwaitAsync` returns `Task`, not `Task<bool>`, so
completing the awaiter on cancellation was unobservable — the delete controller awaited it and answered
204 while the row stayed in the database. The awaiter is now cancelled, and the route refuses deletes
outright: stopping one half-way is not a staleness problem, and the control offering it says *stop
refreshing*.

**Cancelling one entity destroyed another entity's staged writes.** A write-back round is left by the
flush at the end of an update task, so work cancelled *before* that task started joined a round nothing
would ever leave — and every write staged by the other work sharing it was dropped silently. AC-04.4 and
AC-04.7 together, and the invariant ADR-183 flags as the easy one to miss.

Also fixed: the coalesced follow-up inheriting a cancelled token (ADR-183 says it must not), an
`ObjectDisposedException` race turning the explicitly idempotent cancel into a 500, a window before the
token source existed where a cancel silently did nothing, cancels being logged as connector failures and
escalated into full downloads, and two missing checkpoints.

## The thing this slice got wrong, and how it was caught

The first DELIVER commit called the slice done while **every connector took the token and read none of
it** — eight mentions per file, all of them the parameter declaration. The acceptance scenarios passed
because they cancel a *mock written to honour the token*: they prove the ask arrives, never that a
connector acts on it.

What caught it was reading the production code rather than trusting a green suite. What proves the fix is
`JiraCancellationGranularityTest`, which counts real HTTP round trips against a stub tracker claiming a
hundred thousand records, and which was itself verified by hand-mutating the two calls to
`CancellationToken.None` — killing both Data Center tests.

Worth recording for the four connectors still to come: **CA2016 and S8949 are errors in this project**, so
once a method takes a token the build refuses to let it go unused. The decorative-parameter failure is
caught by the gates. A deliberate `CancellationToken.None` is not, which is why the mutation check is the
one that matters.

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| `dotnet test` (connector categories excluded) | 6779 passed, 1 environmental |
| `pnpm test` | 372 files, 5181 tests, all green |
| `pnpm build` | clean, Biome included |
| Mutation — backend | **89.19 %**, gate met |
| Mutation — frontend | **66.67 %** — four of six, both survivors equivalent; see `mutation/results.md` |

## Cost, recorded because it was under-quoted twice

ADR-183 priced the port widening at "six signatures across five implementations". Actual: **8 signatures,
5 implementations, 41 call sites, 9 callbacks, 59 files, +750/−519** — and that is before four of the five
connectors honour the token at all. The sweep broke the suite twice on the way (83 failures, then 62), both
times caught only by tests that already existed.

## Not done here

1. **Azure DevOps, CSV, Linear and ServiceNow still take the token without reading it.** Scheduled next,
   in that order, one connector per commit with its own round-trip-counting test verified by mutation.
   Not a sweep.
2. **AC-04.2's measured granularity is a floor for the sweep, not for the Jira Cloud download path**,
   where per-issue changelog fetches inside a page are uncancellable — up to fifty sequential round trips
   after a cancel. Decided 2026-09-14: thread the token into the changelog fetch so the recorded number
   becomes true.
3. **A cancel still writes `Success = false` to `RefreshLog`**, so refresh history shows a red row for
   something an operator chose. Decided 2026-09-14: add a cancelled state, which is a schema change and
   an EF migration through `Create-Migration.ps1`.

---

# Wave: DISTILL — slice 04, Azure DevOps

The first of the four connectors slice 04 left taking the token without reading it. Order agreed
2026-09-14: **Azure DevOps → CSV → Linear → ServiceNow**, one per commit, never a sweep.

## Wave: DISTILL / [REF] The contract this connector must satisfy

AC-04.2, measured against Azure DevOps rather than assumed from Jira. The acceptance scenarios cancel a
mock written to honour the token, so they prove the ask arrives and nothing more. What is missing is
evidence that a real `AzureDevOpsWorkTrackingConnector` acts on it.

Azure DevOps reaches its records in a different shape from Jira, and the shape decides where the
checkpoint has to be:

| Phase | Round trips for a 2 000-record team | Cancellable before this slice |
|---|---|---|
| WIQL — answers with ids alone | 1 | no |
| Field-definition lookup | 1 | no |
| Payload read, batched at 200 | 10 | no |
| **Per item: up to 4 `GetRevisionsAsync`** | **up to 8 000** | no |

The last row is the finding that shaped the slice. On Jira the per-issue changelog fetch is a Cloud
download-path special case; on Azure DevOps a revision read happens for **every item on every cycle**, so
the conversion phase, not the batch loop, is where a refresh actually spends its wall clock. Threading the
token into the batch loop alone would have produced a test that passes and a Cancel button that stops
roughly one part in eight hundred of the work.

## Wave: DISTILL / [REF] Scenario list

`AzureDevOpsCancellationGranularityTest`, four scenarios over the two walk shapes the connector has:

| Scenario | Asserts |
|---|---|
| `GetWorkItemsForTeam` cancelled before it starts | no request of any kind was attempted |
| `GetWorkItemsForTeam` cancelled while it reads batches | stops after the batch it is in — 3 of 10 |
| `SweepWorkItemsForTeam` cancelled before it starts | no request of any kind was attempted |
| `SweepWorkItemsForTeam` cancelled while it reads batches | stops after the batch it is in — 3 of 10 |

The organisation holds 2 000 records against a batch size of 200, so "it stopped" can never be satisfied
by a tracker that ran out of records to give — seven batches remain unread when the assertion is taken.

## Wave: DISTILL / [REF] Ports and doubles

No new double. `AzureDevOpsOrganisation` already runs the real connector over a recording
`WorkItemTrackingHttpClient` and was extended twice:

- **It refuses a round trip whose token is already set**, exactly as a socket would. Without this the fake
  answers a cancelled request happily and the walk never learns it was told to stop.
- **It records the attempt before refusing it, not after.** This one is the difference between a test that
  discriminates and a test that cannot. A count taken *after* the refusal is identical whether the walk
  stopped or issued a doomed request that the socket rejected — and those are not the same thing to an
  operator whose rate limit pays for the attempt either way.

## Wave: DISTILL / [REF] Red gate

All four red before any production edit, and for the right reason rather than a compile error: the sweep
read all **10** batches after being told to stop after 3.

---

# Wave: DELIVER — slice 04, Azure DevOps

## What changed

**The thread.** The token now runs from the four public entry points through every private method on the
fetch path — 18 signatures in `AzureDevOpsWorkTrackingConnector`, ending at the SDK call on each of the
five round-trip kinds (WIQL, field lookup, batched payload read, revision read, relations read).

**The checkpoint.** `ExecuteWithThrottle` is the one place every Azure DevOps round trip passes through,
and it is now where cancellation is honoured: the wait for a throttle slot refuses once the token is set,
so a cancelled refresh stops before it spends the quota. Its retry loop also stops sleeping — a
rate-limited organisation can back off for up to 90 seconds across six attempts, and a cancel arriving
into that used to sleep out the full backoff before noticing.

**The explicit opt-outs.** Five call sites pass `CancellationToken.None` in the open: connection and
settings validation, write-back, and board discovery. These are the three methods slice 04 put out of
scope, and none of them takes a token to forward. Written out rather than defaulted, so the next reader
sees a decision instead of an omission.

## What mutation testing found

Seven hand-applied probes, run before Stryker, and they changed the shape of the code:

**Every explicit `ThrowIfCancellationRequested()` checkpoint was dead.** Six of them, at the top of both
batch loops and in front of the WIQL, the sweep and the payload phase. All six survived mutation — because
`ExecuteWithThrottle` already refuses a cancelled token before taking a slot, so the guards in front of it
guarded nothing. They were removed. The cancellation promise now rests in one legible place rather than
six that read like protection and were not.

**The total opt-out is compile-blocked, not test-blocked.** Handing a read `CancellationToken.None` while
the method still takes a token fails the build with **S1172** (unused parameter, error-severity here), and
dropping the forward to `WaitAsync` fails with **CA2016**. This extends what slice 04 recorded: the ledger
said CA2016 catches a decorative parameter but not a deliberate `CancellationToken.None`. On this connector
S1172 catches that too — *as long as the token has no other use in the method*. A **partial** opt-out,
where the token is forwarded to one call and dropped at another, compiles cleanly, and that is exactly
what the round-trip test exists to catch.

**The dangerous mutant is killed.** A sweep that swallows the cancellation and answers with the batches it
managed to read is caught. It matters more than anything else here: removal is "stored minus swept", so a
partial answer reported as whole deletes every record on the batches that were never read.

**One survivor, recorded rather than fixed.** A swallowed cancellation in the *fetch* batch loop survives,
masked by the conversion phase throwing on the same token a moment later. The observable behaviour is
still correct — the caller gets an `OperationCanceledException` either way — so there is no defect to fix,
but the loop itself is not independently pinned.

## What the adversarial review found

`nw-software-crafter-reviewer`, given the diff and the mutation results. One blocker and two advisories,
all three fixed:

**The throttle bounding the conversion was never disposed** (blocker). `new SemaphoreSlim(8)`, one per
team or portfolio refresh, never released. Pre-existing — it sits on `HEAD` — but inside a method this
slice touched, so it is fixed here rather than left for someone else to find. `Task.WhenAll` waits for
every task including the faulted ones, so nothing still holds a slot when the `using` closes.

**The attempt after the retry loop gives up went out without re-reading the token.** Six rate-limited
attempts, and if the cancel landed during the last backoff the seventh request was still issued. Narrow —
the token is baked into the delegate, so the call itself would refuse — but a refresh told to stop should
not reach that point at all.

**The history assertion had only an upper bound.** `LessThan(50)` also passes for a walk that died in the
batch reads and never reached the history phase — a different defect wearing the same number. Now bounded
at both ends.

Two claims the review made that did not survive checking: that `GetWorkItemFieldsAsync` runs with
`CancellationToken.None` (it does on the validation path, which is deliberate; on the fetch path it gets
the real token), and its mutation table recording L774/L823 as killed when both survived.

Two findings the review confirmed rather than raised, both mine:

**A cancelled sweep reaches the caller as a cancel, not as a tracker failure.** Verified rather than
assumed: `WorkItemService.ScanRemoteIdentities` carries a dedicated `catch (OperationCanceledException)
{ throw; }` above its catch-all, added by slice 04 for precisely this. Without it a cancel would have been
logged as a broken cheap path and escalated into the full download the operator cancelled to avoid.

**`relationsTask` is abandoned when the conversion phase throws.** `TheTeamsWorkItemsFrom` starts the
relations read, awaits the conversion, then awaits the relations. A cancel makes the middle step throw and
the first task is dropped un-awaited. Pre-existing shape — true of any exception, not just cancellation —
and the dropped task now observes the same token, so it stops on its own. Left alone deliberately:
restructuring it would cost the parallelism that makes the fetch fast.

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| `dotnet test` (connector categories excluded) | 6 793 passed, 1 environmental |
| Azure DevOps unit tests | 63 passed — 59 pre-existing, 4 new |
| Hand-mutation of the token | the two that matter killed, the rest compile-blocked |
| Mutation — backend (Stryker) | 24.65 % raw; **6 survivors on touched lines**, see below |

## What the Stryker number is and is not

24.65 % — 190 mutants tested in the connector, 84 killed, 103 survived — and it should not be read as a
verdict on this change. The test filter excludes `AzureDevOpsWorkTrackingConnectorTest`, which is
`AdoIntegration`-categorised and carries most of this 1 300-line file's coverage, so the score is largely
measuring parsing, validation and board code that no unit test was ever meant to reach.

What is worth reading is that of 106 survivors, **6 fall on lines this change touched**:

| Line | Survivor | Verdict |
|---|---|---|
| 70, 140, 153 | string mutation on the sweep description | text used only inside exception messages; pre-existing, touched only because a parameter was appended |
| 774 | `limiter.WaitAsync(cancellationToken)` removed | equivalent — the SDK call carries the token too |
| 797 | the guard in the retry backoff | **a real gap**: nothing here exercises a rate-limited retry |
| 823 | `throttler.WaitAsync(cancellationToken)` | equivalent — `ExecuteWithThrottle` stops the reads underneath it |

The retry-backoff gap is left open deliberately. Closing it means a test that waits on a real backoff, and
the ledger already records a wall-clock budget taken on a developer machine failing on the CI agent.

## The test that could not be made to fail, and what was done about it

`GetWorkItemsForTeam_CancelledWhileItRebuildsHistory_StopsReadingRevisions` was written to pin the
conversion phase, and then survived every attempt to break it — handing the conversion gate
`CancellationToken.None`, swallowing the cancellation per item, both. `ExecuteWithThrottle` refuses
underneath all of them, so nothing above it can change the count.

It is kept, because it does pin something no other test does: that the revision reads go through a
cancellable path at all. A future read that bypasses the throttle would fail this and nothing else. But it
does not pin the gate it was named for, and saying otherwise would be the same mistake slice 04 made
twice.

Both of its bounds were then verified falsifiable by moving the trigger: with the cancel never firing the
upper bound fails, and with the cancel firing before the history phase the lower bound fails. The first
draft of that lower bound was pinned to the constant driving the trigger and therefore moved with it —
the ledger's self-satisfying-constant trap, caught only because the probe was run.

## Not done here

1. **An in-flight page is not proven to abort.** The tests cancel between round trips, and the fake answers
   synchronously, so what they pin is "issues no further request". Passing the token to the SDK call
   additionally aborts a request already on the wire, which matters when a batch read takes minutes — and
   nothing here measures it. The Jira baseline has the same gap.
2. **CSV, Linear and ServiceNow are unchanged.** Next, in that order.

---

# Wave: DELIVER — slice 04, Azure DevOps history reads

A separate commit from the cancellation work, because it is a refactor and changes no behaviour.

## What it costs to know when an item moved

Azure DevOps has no endpoint that answers "when did this cross into Doing". The only way to know is to
download every revision an item has and read the state changes out of them — and the connector wanted that
same list for four different questions:

| Question | Where it was asked |
|---|---|
| When did work start? | `GetStateTransitionDateThrottled`, Doing states |
| When did it close? | `GetStateTransitionDateThrottled`, Done states |
| Did it re-enter To Do after starting? | `GetStateTransitionDateThrottled`, To Do states |
| Which transitions should be synced? | `GetAllStateTransitionsThrottled` |

Each asked the tracker separately. Four identical answers at four times the price, per item, per cycle — a
done item cost four revision downloads, and a two thousand record team up to eight thousand. Against the
ten batch reads that fetched the same team, this is where the refresh actually spends its wall clock.

## What changed

The history is read **once** per item, and the four answers are computed from that one list. The read and
the arithmetic are now separate things: `RevisionsOfWorkItem` does the I/O, and `StartedAndClosedDateFrom`,
`StateTransitionDateFrom` and `SyncedTransitionsFrom` are pure static functions over the list it returns.

`GetAllStateTransitionsThrottled` keeps its signature for its one existing caller and now delegates its
mapping half to an extracted `StateTransitionsIn`.

## The thing that made this risky

The two old paths built transition lists with **different semantics**, and a refactor that blurred them
would have changed started and closed dates — the inputs to cycle time, work item age and every forecast —
without failing anything:

| | Dating path (`StateChangesIn`) | Sync path (`StateTransitionsIn`) |
|---|---|---|
| A revision that re-saved the same state | kept | skipped |
| The state an item was created in | kept, paired with an empty origin | skipped |
| `DateTimeKind` | left alone; normalised later by `LastEntryInto` | normalised to UTC here |

Both are preserved. The adversarial review was pointed at this specifically and confirmed it line by line.

## What the adversarial review found

**The new test asserted `SyncedTransitions` was `Is.Not.Empty` and nothing more** (blocker). That passes for
a list with the wrong count, the wrong states, or the wrong `DateTimeKind` — which is to say it passes for
every semantic change the table above is about. Now bounded on count, both states, and kind.

Its other finding — that nothing proves the tracker returns revisions in the same order on two separate
calls — is worth recording the other way round: reading once **removes** that window rather than opening
one. Four reads of a changing item could disagree with each other; one read cannot.

## What mutation testing found

Five hand-applied probes against the behaviour the refactor had to preserve. The first run killed two and
**survived two**, and the reason matters more than the count: the shared fixture could not express either
defect. Its revisions were already stamped `DateTimeKind.Utc`, making every normalisation downstream a
no-op no assertion could see fail, and it contained no re-save of the same state at all.

So the fixture was fixed rather than the assertions: `TheRevisionsOf` now hands back unzoned instants, the
way a tracker that promises nothing about zones would, and the parser test gained a four-revision case
where an item is re-saved in the state it is already in. On the second run all four killable probes died:

| Probe | Verdict |
|---|---|
| the created-in state leaks through as a move | killed |
| a re-save of the same state counts as a move | killed |
| the transitions stop being normalised to UTC | killed |
| the history is read per question again, not once | killed |
| the dating reader is handed the sync semantics | compile-blocked (S1144) |

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| Azure DevOps unit tests | 69 passed |
| `dotnet test` (connector categories excluded) | green |
| Hand-mutation of the preserved behaviour | 4 killed, 1 compile-blocked |

---

# Wave: DISTILL — slice 04, CSV

Second of the four connectors. CSV is unlike the rest and the difference decides the whole shape of the
slice: it makes **no remote calls at all**. The upload is carried on the entity as text and parsed in
memory, so there are no round trips to count, no rate limit being spent, and no socket to refuse.

## Wave: DISTILL / [REF] What is left to promise when there are no round trips

Three things, and they are the reason this connector is worth doing rather than waving through:

- **A large upload is real time.** Parsing holds the update slot while it runs.
- **The promise has to hold uniformly.** A Cancel button that stops four connectors and quietly finishes
  the fifth is a button nobody can trust.
- **The failure mode is the worst one in the Epic.** Removal is computed as stored minus fetched, so a
  parse that handed back the rows it managed before stopping would delete every record on the rows it
  never reached. On CSV this is not a theoretical ordering concern — it is one `break` away.

Only two methods do any work: `GetWorkItemsForTeam(team, ct)` and `GetFeaturesForProject(project, ct)`.
Four of the six paging methods throw `NotSupportedException` outright — both sweeps and both
by-reference-id overloads — and legitimately cannot honour a token they never get to use.

## Wave: DISTILL / [REF] Ports and doubles

A seam was needed, because nothing outside the connector could otherwise see how far a parse had got, and
how far it got is the entire claim. `ContentOf(owner)` is `internal virtual` and returns the `TextReader`
the parse reads from — the same precedent as
`AzureDevOpsWorkTrackingConnector.GetWorkItemTrackingHttpClientAsync`.

The double hands out a **fresh reader per call**, exactly as production does. The first version handed out
one shared instance, which the review caught: any second read of the same entity would have been given a
spent reader, and the only reason no test failed was that none happened to validate before fetching.
Keeping the list of readers handed out turned out to be worth more than the reader counts — it lets
"never opened the upload" be asserted as itself, rather than as "opened it and read nothing".

## Wave: DISTILL / [REF] Scenario list

| Scenario | Asserts |
|---|---|
| team fetch cancelled before it starts | the upload is never opened |
| portfolio fetch cancelled before it starts | the upload is never opened |
| team fetch cancelled while parsing | throws, and stops pulling the upload |
| portfolio fetch cancelled while parsing | throws, and stops pulling the upload |
| parent-feature details cancelled | stops rather than answering with none |

The upload holds 5 000 rows, about 229 KB, which CsvHelper pulls in roughly fifty buffer-sized bites. The
cancel lands on the **third** bite, not the first: cancelling on the first would land during the header
read, and a test that stops a parse before it has parsed anything says nothing the cancelled-before-start
test does not already say. That distinction is asserted directly — `Bites > 1`.

---

# Wave: DELIVER — slice 04, CSV

## What changed

`cancellationToken.ThrowIfCancellationRequested()` at the entry of both fetches and once per row inside
both `while (csv.Read())` loops, plus the entry of `GetParentFeaturesDetails`.

That is the whole change, and unlike Azure DevOps every one of those guards is load-bearing. There is no
throttle, no socket, no SDK underneath to refuse on the connector's behalf: the row is the only place a
CSV parse can be stopped.

## What the adversarial review found

**`GetParentFeaturesDetails` accepted the token and ignored it** (blocker). It answers with no features —
a CSV upload has no parent features to fetch — so nothing about it is slow. But answering normally lets a
cancelled refresh walk on to its next step, which is what the operator pressed the button to prevent.
`WorkItemService` calls it at two sites with the cancellation token, so this was a real silent gap.

The review's proposed fix was to throw `NotSupportedException` instead, matching the sweeps. **That was
checked and rejected**: both call sites consume the empty list, so throwing would break every CSV
portfolio refresh. The token check is the correct half of the finding.

**The test double handed out one shared reader.** Any second call to `ContentOf` in the same scenario
would have been given a spent one. Production already mints a fresh reader per call; the double now does
too.

**The mid-parse test cancelled on the first hand-over**, which happens while the header is being read —
so it was not testing a parse in progress at all. It now cancels on the third bite and asserts it landed
mid-stream.

`CA1859` was caught by the local build on a new `IReadOnlyList` property, the ledger rule with seven prior
recurrences.

## What mutation testing found

Six hand-applied probes, **all six killed**:

| Probe | Verdict |
|---|---|
| team fetch: entry guard dropped | killed |
| team fetch: per-row guard dropped | killed |
| team fetch: stops but answers with the rows it had | killed |
| portfolio fetch: entry guard dropped | killed |
| portfolio fetch: per-row guard dropped | killed |
| parent features: guard dropped | killed |

The third is the one that matters. It is the data-loss shape — a `break` instead of a throw — and on this
connector it compiles, reads as reasonable, and would silently delete every record on the unread rows.

Stryker: **66.22 %** over the whole connector (132 tested, 98 killed, 34 survived), and **zero survivors
on the 25 lines this change touched**. As with Azure DevOps the headline number is about the rest of the
file — parsing, validation, dependency columns — not about this change.

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| CSV tests | 64 passed — 59 pre-existing, 5 new |
| `dotnet test` (connector categories excluded) | 6 794 passed, 1 environmental |
| Hand-mutation of the guards | 6 of 6 killed |
| Mutation — backend (Stryker) | 66.22 % raw, 0 survivors on touched lines |

## Not done here

1. **The four `NotSupportedException` methods still ignore their token.** They throw before doing anything,
   and `NotSupportedException` says more about why a CSV upload cannot be swept than a cancellation would.
2. **Linear and ServiceNow are unchanged.** Next, in that order.

---

# Wave: DISTILL — slice 04, Linear

Third of the four connectors. Linear pages in exactly one place — `GetWithPagination` — and both halves of
the fetch walk through it, so unlike Azure DevOps there is no second walk shape to reason about. It also
supports **no identity sweep at all**, which means every Linear cycle takes the whole-query path and there
is no cheaper walk to fall back on when an operator wants one stopped.

## Wave: DISTILL / [REF] Scenario list

`LinearCancellationGranularityTest`, over a workspace that always has another page:

| Scenario | Asserts |
|---|---|
| portfolio fetch cancelled before it starts | the workspace is never asked anything |
| team fetch cancelled before it starts | the workspace is never asked anything |
| portfolio fetch cancelled while paging | stops after the query it is in — 3 |
| team fetch cancelled while paging | stops after the query it is in — 3 |
| initiative walk cancelled part way | stops instead of counting failures — 3 of 10 |

The team-half paging scenario was added after review. Both halves reach the same loop, which was the
argument for testing one of them — but the team half gets there through **two** walks rather than one, the
first resolving the team by name, and two walks into one loop is not the same claim as one.

## Wave: DISTILL / [REF] Ports and doubles

Linear already had the seam: the connector takes an `HttpMessageHandler` for testing, and
`LinearFetchRefusalTest` already drove it. The workspace double answers every query shape from **one JSON
envelope** carrying `projects`, `teams` and `team` at once — every GraphQL request goes to the same
endpoint, so the deserialiser takes the branch its response type wants and ignores the rest.

It offers **forty** pages rather than endless ones. An endless supply would make a connector that ignores
the token hang rather than fail, and a test that hangs teaches nothing. The RED run bore that out: forty
queries where three were required.

---

# Wave: DELIVER — slice 04, Linear

## What changed

The token threaded from the three public fetch entry points to the GraphQL request through eleven private
methods, plus a guard in the per-initiative walk and a re-throw ahead of its catch-all. Validation and
board discovery pass `CancellationToken.None` in the open.

## The bug the analyzers caught, which the tests would have caught second

`client.SendQueryAsync<T>(query, cancellationToken)` **compiles**. The GraphQL client's second positional
parameter is `object? variables`, so that boxes a `CancellationToken` into the variables argument and
cancels precisely nothing — the decorative-token failure this Epic has already made once, in a new
disguise. `CA2016` and `S8949` rejected it at build time; it is now `cancellationToken: cancellationToken`.

Worth carrying to ServiceNow: a token in the right position is not the same as a token in the right
parameter, and only the named form is obviously either.

## The catch-all that would have eaten the cancel

`GetParentFeaturesDetails` fetches one initiative per round trip inside `catch (Exception ex)`, counting
what it catches as an initiative that could not be fetched and moving on to the next one. A cancel caught
there is a refresh that was told to stop, kept going, and said nothing about it. It now re-throws
`OperationCanceledException` ahead of the catch-all — the same shape `WorkItemService.ScanRemoteIdentities`
already uses.

## What mutation testing found

**The guard inside the paging loop guarded nothing, and was removed.** `HttpClient` refuses an
already-cancelled token before it ever reaches the handler, so a walk cannot issue the doomed request the
guard existed to prevent — no test could distinguish its presence, because the behaviour is identical. The
checkpoint is the token on the request, and `SendQueryWithErrors` now says so where a reader will find it.
This is the same conclusion Azure DevOps reached about its six guards, by a different mechanism.

Sharpening the double to count a query when it is **asked for** rather than when it succeeds — the fix
that made the Azure DevOps harness discriminate — did not help here, and the reason is worth recording:
there the throttle was inside the connector, so an attempt was observable; here the refusal happens inside
`HttpClient`, above the handler, so the attempt never reaches anything a test can see.

**The two initiative guards are individually redundant and jointly essential.** Removing either leaves the
other to stop the walk. Removing **both** is killed — and what it produces is the dangerous shape: every
cancel swallowed, all ten initiatives walked, a partial list returned, and no exception raised at all.

| Probe | Verdict |
|---|---|
| the request is handed `CancellationToken.None` | compile-blocked (S1172) |
| the token goes back to the variables parameter | compile-blocked (CA2016) |
| the paging loop stops checking the token | survived — guard removed as dead |
| the initiative walk loses either guard | survived — the other still stops it |
| the initiative walk loses **both** guards | **killed** |

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| Linear tests | 38 passed — 33 pre-existing, 5 new |
| `dotnet test` (connector categories excluded) | 6 799 passed, 1 environmental |
| Hand-mutation of the guards | the dangerous shape killed, the rest compile-blocked or dead |
| Mutation — backend (Stryker) | 35.07 % raw; 3 survivors on touched lines, all accounted for |

## The three Stryker survivors on touched lines

35.07 % over the whole connector (172 tested, 100 killed), with the same caveat as the others — the filter
excludes `LinearWorkTrackingConnectorTest`, which is `LinearIntegration`-categorised. Of 72 survivors,
three sit on lines this change touched:

| Line | Survivor | Verdict |
|---|---|---|
| 175 | the per-initiative guard | the pair above: its twin still stops the walk |
| 501, 542 | `?? []` on the downgrade-and-retry path | pre-existing; the line is touched only by an appended parameter, and nothing here exercises a history downgrade at all |

## Not done here

1. **Nothing exercises the history-downgrade retry.** `FetchAllIssuesForTeam` and `FetchAllProjects` answer
   null when Linear rejects the history field, and the caller then walks the whole query a second time.
   That second walk is untested on both halves, which is why two of the three survivors above are there.
2. **`GetAllIssuesForTeam` and `GetAllProjects` duplicate a downgrade-and-retry wrapper** almost line for
   line. That is repeated knowledge rather than repeated shape, so it is a real finding — but it is
   pre-existing, and merging it needs generics over both the response type and the query. Out of scope for
   a cancellation slice; recorded so the next person to touch either one sees it.
3. **ServiceNow is unchanged.** Last, and the deepest: `ReadEveryPage` sits five private methods below the
   public surface and already hardcodes a `CancellationToken.None`.

---

# Wave: DISTILL — slice 04, ServiceNow

Last of the four, and the one that hid its cancellation deepest. Every ServiceNow round trip goes through
a single private `Read`, five methods below the public surface:

`GetWorkItemsForTeam` → `ReadHistory` → `ReadSpans` → `ReadEveryPage` → `Read`

`Read` did not merely fail to pass a token on. It wrote **`CancellationToken.None` out by hand** when
applying the credential, and gave `SendAsync` no token at all. Nothing in any signature above it said so.

## Wave: DISTILL / [REF] Scenario list

| Scenario | Asserts |
|---|---|
| team fetch cancelled before it starts | the instance is never asked anything |
| team fetch cancelled while paging | stops after the page it is reading — 3 |

## Wave: DISTILL / [REF] Ports and doubles

The connector already takes an `HttpMessageHandler` for testing. The instance double has to satisfy three
things the connector checks before it will page at all, and each one failed the first attempt in a way
that looked nothing like a cancellation defect:

- **Every record needs its own `sys_id`.** The connector aborts a read that sees one twice, because
  `number` is not unique on a real instance and a collision would cost a team every work item.
- **`X-Total-Count` has to agree with the `Link` headers.** The pager falls back to counting when the
  instance stops linking, so a count that outran the pages on offer walked to the connector's own
  thousand-page ceiling and died there — the first RED read 1 000 pages and threw `PagingDidNotTerminate`.
- **The fields arrive as `{ display_value, value }` pairs**, because the read asks for
  `sysparm_display_value=all`.

Once the count and the links agreed, RED was 120 reads where 3 were wanted, with no exception at all.

---

# Wave: DELIVER — slice 04, ServiceNow

## What changed

The token threaded from `GetWorkItemsForTeam` through `ReadHistory`, `ReadSpans`,
`ReadStateSpanDefinitions` and `ReadEveryPage` into `Read`, which now hands it to `ApplyAsync`,
`SendAsync` and `ReadAsStringAsync`. One guard at the top of the pager's `while (pageUri is not null)`
loop. Board discovery and validation pass `CancellationToken.None` in the open.

## The guard Linear did not need and this connector does

On Linear the equivalent guard was **removed** after mutation testing showed it guards nothing: the
GraphQL client's `HttpClient` refuses an already-cancelled token before the handler is ever reached, so
the doomed request could not be issued.

Here the same test setup gave **1 and 4** where 0 and 3 were wanted — the request reached the handler and
was counted before anything refused it. The guard is load-bearing, and mutation confirms it: removing it
is killed. Two connectors, opposite conclusions, and only counting attempts told them apart.

## What mutation testing found

| Probe | Verdict |
|---|---|
| the pager stops checking before each page | **killed** |
| the fetch hands the pager `CancellationToken.None` | **killed** |
| the request goes out with no token again | compile-blocked (CA2016) |
| the pager guard and the request token both go | compile-blocked (CA2016) |
| the history spans are read without the token | compile-blocked (S1172) |
| the credential is applied with `None` again | **survived** — see below |

**The credential opt-out survives, and is worth naming.** `authStrategy.ApplyAsync(request, connection,
CancellationToken.None)` still compiles, and nothing here can see it, because the strategy this test
supplies does no work. On a basic-auth connection that is genuinely harmless — applying a header is local.
It is not harmless on an OAuth strategy that refreshes a token over the network, and that call is now
cancellable only because the parameter is threaded. Testing it belongs with the auth strategies, not here.

## What the adversarial review found, and why both findings were rejected

The review verified the four claims that mattered — the `TaskCanceledException` catches are unreachable
from the fetch path, a cancelled history read throws rather than downgrading, no path returns partial
records without throwing, and the parameter binding is correct at every call site. Its two findings were
both wrong, and the second one dangerously so:

**"Use `ThePageTheCancelArrivesOn` in the assertion instead of the literal `3`."** That is the
self-satisfying-constant trap the ledger names, and this Epic hit it earlier in the same session: an
assertion pinned to the constant that drives the trigger moves with it, so changing the trigger silently
changes what is asserted. The literal stays.

**"The three `TaskCanceledException` handlers are unreachable dead code — remove them."** They are
reachable, just not by cancellation. `CreateHttpClient` builds a plain `HttpClient`, whose **default
hundred-second timeout raises `TaskCanceledException`**, and those handlers are what turn a timed-out
validation into "the instance could not be reached". Removing them would replace a config admin's
diagnosis with an unhandled exception.

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| ServiceNow tests | 339 passed — 337 pre-existing, 2 new |
| `dotnet test` (connector categories excluded) | 6 801 passed, 1 environmental |
| Hand-mutation of the guards | 2 killed, 3 compile-blocked, 1 recorded survivor |
| Mutation — backend (Stryker) | **90.07 %**, gate met; 0 survivors on touched lines |

## The one connector whose Stryker number means something

272 mutants, 249 killed, **90.07 %** — the only one of the four to clear the 80 % gate on the raw number,
and the reason is not that this change is better tested than the others. It is that ServiceNow keeps its
coverage in unit tests: 337 of them run without an instance. Azure DevOps and Linear keep theirs in
classes categorised `AdoIntegration` and `LinearIntegration`, which the mutation filter excludes because
they talk to real trackers — so their headline numbers were measuring parsing and validation code that no
unit test was ever meant to reach.

Zero of the 23 survivors fall on a line this change touched.

## Not done here

1. **The credential application is not covered.** See the surviving probe above: an auth strategy that
   goes to the network on `ApplyAsync` can now be cancelled, and nothing here proves it.
2. **The keyed and sweep methods still throw `NotSupportedException`.** ServiceNow supports neither yet.

---

# Wave: DELIVER — slice 04, the Jira changelog re-read

The first of the two fixes decided on 2026-09-14, and the one that makes AC-04.2's recorded number true
rather than approximately true.

## What was wrong

AC-04.2 says a cancelled refresh stops within one page round trip, and slice 04 measured that on the sweep
and on the page walk. It was never true of the Cloud download. Any issue carrying more than thirty
changelog entries is re-read on its own `/changelog` endpoint — and that re-read **pages on its own**, so a
page of fifty long-lived issues is fifty nested walks, none of which looked at the token.

The reason it survived slice 04 is the instructive part: **it still threw.** The outer page walk noticed
the cancel at its next checkpoint, so from the outside cancellation appeared to work. It simply did so
after paying for every nested changelog walk first. The RED reads **20 round trips where 3 were wanted**,
with an `OperationCanceledException` raised at the end of them.

## What changed

The token now reaches `CreateIssueWithCompleteChangelog` and `GetAllChangelogEntriesForIssue`, which
passes it to the request and to the body read, and checks once per changelog page.

**Data Center is not affected, and was checked rather than assumed.** It asks for `expand=changelog` on the
search itself and builds its issues straight from the answer; it never calls
`CreateIssueWithCompleteChangelog`. The nested re-read is a Cloud-only shape, so the fix is Cloud-only.

## What mutation testing found

| Probe | Verdict |
|---|---|
| the changelog request goes out with no token — *the original bug* | compile-blocked (CA2016) |
| the changelog walk is handed `CancellationToken.None` | compile-blocked (S1172) |
| the page guard and the request token both go | compile-blocked (CA2016) |
| **the per-issue walk is handed `CancellationToken.None`** | **killed** |
| the changelog walk stops checking per page | survived — see below |

The defect this fix exists for **can no longer be written**: reintroducing it fails the build. What the
test pins is the half the compiler cannot see — that the token handed to the per-issue walk is the live
one.

**The per-page guard is redundant and was kept anyway.** `HttpClient` refuses an already-cancelled token
before the handler is reached, so no test can distinguish it — the same finding Linear produced, where the
equivalent guard was deleted. It stays here because all four sibling walks in this file carry the
identical guard and shipped that way in slice 04. Deleting only the new one would read as an oversight,
and deleting theirs is not this fix's business. Consistency inside the file wins over the cross-connector
rule, deliberately.

Stryker: 54.96 % over the connector (565 tested, 368 killed) — the usual caveat, `JiraIntegration` carries
most of this 2 683-line file's coverage and the filter excludes it. **Zero survivors on the 16 lines this
change touched.**

## What the adversarial review found

Approved, having verified the four things that mattered: Data Center genuinely has no nested re-read,
nothing on the path converts the cancellation into a `JiraSearchRejection` (which would tell an operator
Jira refused their query when in fact they stopped it themselves), no partial issue list escapes to
`WorkItemService`, and the token is checked before the request rather than after.

Its one finding was mine to fix and real: `AnIssueWithAChangelogOf` built its JSON by slicing the last `}`
off `AnEpic(key)` and appending — which breaks the moment anyone passes issue links, because those contain
braces of their own. It now composes the issue explicitly, and takes links like its neighbours do.

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| Jira tests | 243 passed |
| `dotnet test` (connector categories excluded) | 6 803 passed, 0 failed |
| Hand-mutation of the changelog walk | the original bug compile-blocked; the live token killed |
| Mutation — backend (Stryker) | 54.96 % raw; 0 survivors on touched lines |

## Not done here

1. **A cancelled refresh still writes `Success = false` to `RefreshLog`**, so refresh history shows a red
   row for something an operator chose. That is the second decided fix: a schema change through
   `Create-Migration.ps1`, and it touches the history UI.

---

# Wave: DELIVER — slice 04, a cancelled refresh says cancelled

The second of the two fixes decided on 2026-09-14, and the first work in this Epic to cross into the
frontend.

## What was wrong

A cancelled refresh was written to `RefreshLog` with `Success = false`, because that is what the `finally`
records when anything at all escapes the `try`. An operator who stopped a refresh on purpose was then
shown it as a failure — and `UpdateNotificationHub`, which answers the header and the detail page from the
same last run, reported `UpdateProgress.Failed` for it. The task list said cancelled; every other screen
said broken.

## The design that could not ship, and why

The first attempt replaced `bool Success` with a `RefreshOutcome { Failed, Succeeded, Cancelled }` enum,
which makes the meaningless state unrepresentable and reads better at every call site.

It cannot ship here. **`ExpandOnlyMigrationGuard` forbids `DropColumn` and `RenameColumn` in a migration's
`Up`**, and replacing a column generates exactly those. The attempt was reverted whole.

It was found the expensive way: the model changed, and the backend suite went from one failure to **172**,
every one of them `PendingModelChangesWarning` — "you changed the model and owe a migration". Chasing the
migration led to the script, and the script's neighbourhood led to the guard. Worth recording as the order
to do it in: **read the migration guard before choosing a schema shape**, not after.

## What shipped

`Cancelled`, additive beside `Success`. No rename, no drop, no backfill — existing rows default to false,
which is true of every refresh recorded before cancellation existed. Both migrations are a single
`AddColumn<bool>(nullable: false, defaultValue: false)`.

All three updaters — Team, Portfolio and Forecast — rethrow `OperationCanceledException` above their
catch-alls and record it. Uniformly, on the CSV principle: a Cancel button that reports two of three
honestly is one nobody can trust.

`UpdateNotificationHub` reads `Cancelled` first, so the header and the detail page now agree with the task
list.

On the frontend, **Success Rate no longer counts a cancelled run against the rate** — it divides by the
runs that were left to finish, and shows a `Cancelled` count of its own so every run is still accounted
for. A refresh somebody stopped is not evidence about whether refreshing works.

The representable-but-meaningless state, `Success && Cancelled`, is unreachable: `success = true` is the
last statement of each `try`, so nothing that throws afterwards can have set it. The review traced all
three.

## What mutation testing found, after the review had approved

The adversarial review approved this with no defects, and was right about the design. Mutation testing
then found **three gaps in the tests holding that conclusion up**:

| Probe | What it exposed |
|---|---|
| `var cancelled = false` → `true` | **Nothing asserted `Cancelled` is false on a successful run.** Flip the initialiser and every healthy refresh is logged as cancelled, excluded from the rate, and the panel reports nothing at all. |
| `throw;` removed, in all three updaters | The cancel is **swallowed** — the updater returns as though it finished and the queue tells the browser the work completed. The same catch-all defect this Epic already found in Linear, in three more places. |
| the frontend `> 0` guards | Distinguishable **only at zero**: every run cancelled (divide by zero), and no runs cancelled (a `Cancelled: 0` row on every healthy entity). Neither was tested. |

All are closed. Final: backend **0 survivors on touched lines** (from 6), frontend **95.45 %**.

The one frontend survivor left is the `: []` else-branch of a conditional spread, mutated to a junk array.
Killing it means asserting the exact contents of the stats list, which breaks the moment somebody adds a
legitimate stat — brittleness bought with no defect caught. Left, deliberately.

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| `dotnet test` (connector categories excluded) | 6 806 passed, 1 environmental |
| `pnpm test` | 372 files, 5 184 tests, all green |
| `pnpm build` (Biome included) | clean |
| Mutation — backend | 71.77 % raw; **0 survivors on touched lines** |
| Mutation — frontend | **95.45 %**, one equivalent survivor |
| Migrations | SQLite + Postgres, both `AddColumn` only; expand-only guard passes |

## Not done here

1. **`Success` stays a bool.** The outcome enum is the better model and remains available as a two-release
   expand-then-contract, which is what the guard is asking for. Nobody should attempt it as one commit.
2. **Both decided fixes are now done**, which closes everything slice 04 recorded against itself. What
   remains on the Epic is slices 05 and 06.

---

# Wave: DISTILL — slice 05

Run 2026-09-15. Reconciliation: passed, 0 contradictions. DESIGN's one correction to DISCUSS — the
`authentication_failed` code three connectors already emit — is what this slice is built on, and
nothing accepted since contradicts US-05 as DISCUSS wrote it.

## Wave: DISTILL / [REF] The contract this slice fixes

Two routes, both System-Administrator-guarded, and one new row.

`GET /api/latest/connectionhealth` answers a JSON array with one object per configured connection:

| field | meaning |
|---|---|
| `connectionId`, `connectionName` | which connection this is about |
| `workTrackingSystem` | the tracker's name, rendered as its enum name |
| `state` | `Unknown`, `Healthy`, `Unreachable` or `AuthenticationFailed`, rendered as the name — the browser's own union is strings, and a renumbering would otherwise silently relabel the colour an operator reads |
| `message` | the sentence an administrator reads, written by the connector. Absent while the state is `Unknown` |
| `observedAt` | when the state was observed. Absent while the state is `Unknown` |

`POST /api/latest/connectionhealth/{connectionId}/test` answers the single row for that connection, or
404 when no connection has that id.

`ConnectionHealthVerdict` is one row per connection — state, code, message, observed-at — with a unique
index on the connection id and a cascade delete. At most one row, because the question the popover asks
is "how is this connection now", not "how has it been".

### Three decisions taken here, because the ACs do not settle them

**A successful refresh clears the verdict; it does not record health.** D9 is explicit that until a
connection has failed once its health is `Unknown` rather than `Healthy`, and AC-05.3 says the same.
But a connection that failed at 02:00 and refreshed cleanly at 08:00 must not still read broken at
09:00 — D9 derives health from *the most recent refresh outcome*, and a success is an outcome. So a
successful refresh removes the recorded failure and the connection returns to `Unknown`. **Test
connection is the only thing that can say `Healthy`**, which is what gives the button a reason to exist.

**A secret this instance can no longer decrypt reads `AuthenticationFailed`, and the tracker is never
asked.** This is the one place the slice deliberately does not degrade to `Unreachable`. Handing an
undecryptable secret to Jira gets it refused exactly as an expired token is refused, and an
administrator reading that goes off to reissue a credential that was intact — the precise harm
`BuildUnreadableSecretReason` exists to prevent. The pre-flight asks the same total reader the
connection screen asks, before anything leaves the machine, and the message names the field and the
key rather than the tracker.

**Everything else the connectors can say reads `Unreachable`.** Only `authentication_failed` means the
credential was refused. `connection_failed`, `invalid_url`, `validation_failed` and
`insufficient_permissions` all carry their own message and none of them is evidence about a credential.
Linear and CSV emit no auth code at all, so they read `Unreachable` on failure — honest, not degraded,
and OQ-3 stays open.

## Wave: DISTILL / [REF] Scenario list

**Backend acceptance** — `API/Integration/TaskManager/Slice05AnyBrokenCredentialSaysSo{Scenarios,Specifications}.cs`,
categories `acceptance` + `epic-5511-task-manager` + `slice-05`. All seventeen observe the two routes
over HTTP; the refresh half is driven through the production queue exactly as slices 01-04 drive it.
The last three were written by mutation testing and are marked — see the DELIVER section below for what
each of them caught.

| Scenario | Tags | AC |
|---|---|---|
| `A_refresh_that_fails_on_a_rejected_credential_says_the_credential_was_rejected` | `@walking_skeleton @driving_port @real-io @error` | 05.1, 05.2 |
| `A_refresh_that_fails_where_the_tracker_cannot_say_why_reads_unreachable_not_rejected` | `@driving_port @real-io @error` | 05.2 |
| `A_connection_nothing_has_been_observed_about_is_listed_without_claiming_it_is_healthy` | `@driving_port @real-io` | 05.1, 05.3 |
| `A_refresh_that_works_clears_the_failure_before_it_without_claiming_health` | `@driving_port @real-io` | 05.3 |
| `A_refresh_an_operator_stopped_says_nothing_about_the_credential` | `@driving_port @real-io @error` | 05.2 |
| `A_credential_this_instance_can_no_longer_read_is_never_offered_to_the_tracker` | `@driving_port @real-io @error` | 05.2 |
| `Testing_a_connection_asks_that_tracker_once_and_records_what_it_answered` | `@driving_port @real-io` | 05.4 |
| `Testing_one_connection_leaves_every_other_connection_as_it_found_it` | `@driving_port @real-io @error` | 05.4 |
| `Testing_a_connection_that_is_no_longer_there_is_refused_rather_than_answered` | `@driving_port @real-io @error` | 05.4 |
| `An_oauth_connection_that_lost_its_grant_still_says_it_needs_reconnecting` | `@driving_port @real-io @error` | 05.5 |
| `An_oauth_connection_whose_grant_is_intact_is_not_reported_as_broken` | `@driving_port @real-io` | 05.5 |
| `A_rejected_refresh_outranks_an_oauth_grant_that_still_believes_it_is_valid` | `@driving_port @real-io @error` | 05.5 |
| `Every_connection_is_listed_whatever_it_authenticates_with` | `@driving_port @real-io` | 05.1 |
| `Connection_names_are_not_handed_to_somebody_who_is_not_an_administrator` | `@driving_port @real-io @error` | 05.8 |
| `A_credential_this_instance_can_no_longer_read_says_which_field_to_enter_again` † | `@driving_port @real-io @error` | 05.2 |
| `A_connection_that_fails_twice_says_why_it_failed_the_second_time` † | `@driving_port @real-io @error` | 05.2 |
| `Testing_an_oauth_connection_whose_grant_is_broken_still_says_it_needs_reconnecting` † | `@driving_port @real-io @error` | 05.4, 05.5 |

† written to close a mutation survivor. Error-path share: 11 of 17.

Four are worth calling out as more than restatements of an AC:

*A refresh an operator stopped* is the trap this slice inherits from slice 04. A cancel arrives at the
updater as a failed `try` like any other, and recording it would put "authentication failed" against a
connection somebody had just protected from a rate limit. Nothing was learned about the credential, so
nothing is recorded.

*A credential this instance can no longer read* asserts something no other scenario can: that the
tracker was **never contacted**. Every other scenario would pass on a build that asks the tracker and
believes the answer.

*A rejected refresh outranks an OAuth grant that still believes it is valid* is the precedence between
the two sources. Without it, "fold OAuth in" and "let OAuth win" are indistinguishable.

*An OAuth connection whose grant is intact is not reported as broken* is the positive control for the
two OAuth scenarios. Without it, "every OAuth connection needs reconnecting" satisfies both.

**Architecture** — `Architecture/ConnectionHealthSingleWriterArchUnitTest.cs`. One verdict row, one
writer. `Program` is exempt: naming a type is how a composition root registers it.

**Frontend** — `components/App/Header/TaskManagerIcon.test.tsx`, under a `connections` describe (18),
plus the header's own composition spec in `Header.test.tsx` (1).

| Scenario | AC |
|---|---|
| lists every connection with what is known about it | 05.1 |
| says so when there are no connections at all † | 05.1 |
| says a connection has not been checked rather than calling it healthy | 05.3 |
| says a tracker it could not reach was not reached, rather than blaming the credential † | 05.2 |
| shows what to do about a connection that is broken † | 05.2 |
| says nothing further about a connection that is not broken † | 05.2 |
| names what is wrong on the icon itself | 05.7 |
| says nothing is wrong when nothing is wrong | 05.7 |
| leaves the icon its ordinary colour when no connection is in trouble † | 05.7 |
| colours the icon for the worst of the connection states, not the first | 05.7 |
| colours the icon for a tracker it could not reach differently from a credential it could not use | 05.7 |
| warns when only one of several connections cannot be reached † | 05.7 |
| names every connection that is in trouble, not just one † | 05.7 |
| tests only the connection whose button was pressed | 05.4 |
| shows what the test answered rather than assuming it worked | 05.4 |
| leaves the row as the instance last described it when the test fails | 05.4 |
| offers the way to the connection that needs fixing | 05.5 |
| takes the reader to the connection that needs fixing † | 05.5 |
| shows nothing at all to somebody who is not a System Administrator | 05.8 |
| *(Header.test.tsx)* shows one status icon, and it is the activity icon | 05.6 |

† written to close a mutation survivor.

The colour scenarios are asserted on the badge's MUI colour class, which is the only place MUI
expresses it — nothing in the accessibility tree carries a colour. The unreachable connection is listed
**first** in the worst-of scenario precisely so "take the first one" cannot pass it, and the
warning-rung scenarios beside it are what stop "always red" and "always warn" passing.

Two of them wait for a connection row to render before asserting, and that is not ceremony: the icon
reads "Activity" before the health read returns, so a spec that asserts the absence of a warning
immediately passes on a component that has not yet looked at anything.

## Wave: DISTILL / [REF] Ports and doubles

| Port | Class | Treatment |
|---|---|---|
| `GET /api/latest/connectionhealth`, `POST .../test` | Driving | Real, over `Factory.CreateClient()` |
| The scheduled refresh (`ITeamUpdater`) | Driving | Real, through the production queue in its own DI scope |
| `IRepository<ConnectionHealthVerdict>`, `IRepository<OAuthCredential>`, `IUpdateStatusStore` | Driven internal | Real, EF over SQLite |
| `IWorkTrackingConnector.ValidateConnection` | Driven external | Faked; made to answer each of the three codes the slice distinguishes |
| `ICryptoService` | Driven internal | A fake that has lost **one** key. Modelling a total loss would make every connection in the fixture unreadable and the scenario would pass on a build that never looked |

## Wave: DISTILL / [REF] Red gate

13 of the first 14 backend scenarios failed against the scaffold, each for the right reason — the endpoint
answering 500 from a `NotImplementedException` carrying the scaffold's own message, or the scaffold
throwing directly. The fourteenth is `Connection_names_are_not_handed_to_somebody_who_is_not_an_administrator`,
which passes on arrival: it is a guard over the `RbacGuard` attribute, and a guard that fails on the day
it is written would mean the attribute was missing.

Two migration tests also failed at the red gate, on `PendingModelChangesWarning`, and the migration
closed them. That is the model-vs-migration gap the ledger warns about, arriving on schedule.

## Wave: DISTILL / [REF] Deviations from DESIGN, and why

1. **No `/api/latest/update/summary`.** ADR-186 planned a small always-live summary to feed the header
   badge. Slice 02 never built it — `TaskManagerIcon` reads the task list on mount and on every
   `GlobalUpdateNotification`, and badges off its length. Slice 05 follows the shipped shape and reads
   connection health the same way rather than inventing a second header feed for one slice.
2. **`ConnectionHealthService.ts`, not `SystemActivityService.ts`.** DESIGN named one frontend HTTP
   adapter for all four new surfaces. Slices 02 and 04 put the activity and cancel calls on
   `UpdateSubscriptionService` instead, so `SystemActivityService` does not exist. A dedicated service
   registered on `ApiServiceContext` beside the other 29 matches what is actually there.
3. **`WorkTrackingSystemConnectionDto.RequiresReconnect` is untouched**, as ADR-184 says. Two sources
   for one question remains OQ-5.

---

# Wave: DELIVER — slice 05

## What changed

**Backend.** `ConnectionHealthVerdict` — one additive table, one row per connection, unique index on
the connection id, cascade delete. `ConnectionHealthService` is its only writer, enforced by
`ConnectionHealthSingleWriterArchUnitTest`. `ConnectionHealthController` serves the two new routes,
`SystemAdmin`-guarded like the refresh log and the log file already are.

`UpdateServiceBase` gains `RecordConnectionHealth`, called from the `finally` of both `TeamUpdater` and
`PortfolioUpdater` beside the `RefreshLog` write — the one place a refresh's outcome is already
classified. It returns early on a cancel and swallows its own failures: a health verdict must not
decide whether the refresh that produced it succeeded.

**Deleted, per D2 and ADR-184**: `OAuthHealthAggregator`, `IOAuthHealthAggregator`,
`OAuthHealthController`, `OAuthHealthDto`, `GET /api/oauth/health`, `OAuthService.getHealth`,
`OAuthHealthIcon.tsx`, and both of their test files. One question, one answer.

**Frontend.** `ConnectionHealthService.ts` is the adapter; `TaskManagerIcon` reads health on mount and
on every `GlobalUpdateNotification`, badges the worst of what it finds, and names the broken connection
in its tooltip. The popover gains a section headed with the tenant's own term for a work tracking
system, one row per connection, with **Test connection** and a route to the connection's edit page.

`TaskManagerIcon` was also split: `ActivitySection`, `ConnectionsSection` and `connectionHealthWording`
now live under `Header/TaskManager/`, which is where DESIGN put them. Adding a second section inline
would have pushed the render body past the cognitive-complexity limit the ledger has already lost two
CI cycles to.

## The cost this adds to a failed refresh, recorded because it is on #5877's path

A failed refresh now makes one extra outbound call — `ValidateConnection` — to the tracker that just
turned it away. ADR-184 accepted that call. What the ADR did not weigh is **where** it happens: inside
the updater's `finally`, while the single update lane is still held.

The bound is the connector's own per-request timeout, configurable per connection and **100 seconds by
default**. So a refresh that fails against an unreachable tracker can hold the lane for roughly twice
as long as it did before. That is bounded, not open-ended, and it is one small request rather than a
paging loop — but it lands on exactly the pathology #5877 reported, so it is written down here rather
than discovered later. If it proves to matter, the passive typed-exception path ADR-184 set aside is
the fix, and the verdict record is the same shape either way.

## What the adversarial review found

One blocker, and it was right: the ArchUnit rule's `Because` string opened with `ADR-184:`, which is
the pointer-instead-of-reason pattern this project bans — the message a developer reads when the rule
trips has to explain itself. Rewritten to say why a second writer is the defect. The review's other two
flags, an unverified CA1861 grep and the unique-index assumption, were checked and clean.

The review also traced the concurrency shape and judged it acceptable; independent checking agrees. Two
refreshes failing on one shared connection can both find no verdict and both insert, and the second
`Save` loses to the unique index. It throws into `RecordConnectionHealth`'s catch, is logged non-fatally,
and the refresh is unaffected — which is the right outcome, because the verdict that won says the same
thing the loser would have.

## What mutation testing found, after the review had approved

Both stacks started **below the gate** — backend 75.00 %, frontend 75.00 % — and the survivors were
not noise. Full triage in `mutation/results.md`; the three that changed shipped behaviour or design:

| Probe | What it exposed |
|---|---|
| the unreadable-secret message blanked to `""` | **Nothing asserted the message.** The scenario proved the state and proved the tracker was never contacted, but the sentence naming which field to re-enter — the whole reason this case does not read as a rejected token — was unpinned. |
| `verdict ??= new(...)` → `verdict = new(...)` | **No connection had ever failed twice.** Under the mutant the second failure loses to the unique index and is swallowed, so the row keeps showing the *first* cause forever. Closing it then proved `verdictRepository.Update` was **dead code** — the entity is already tracked by the same context — so the branch that justified it is gone. |
| the row's `Tooltip` title | **The connector's message was only reachable by hovering a row inside a popover.** Killing the mutant and fixing the design were one change: a broken connection now renders its explanation under the row. |

And one about a test rather than the code: `leaves the icon its ordinary colour when no connection is
in trouble` was **passing against an empty list**, because the icon reads "Activity" before the health
read returns. A hand-mutation to an unconditional `true` did not reveal it; only Stryker's narrower
mutation of the `some` callback did, since with an empty array the two agree. Worth generalising: a
component spec asserting an *absence* has to wait on something the data itself produces.

Final: backend **91.89 %** (3 survivors, all equivalent), frontend **86.96 %** (9 survivors, all MUI
`sx` layout props and one separator string).

## The defect the pre-push run caught, and why it was nearly missed

The rebase-and-push run failed `DeletePortfolio_WhileQueueTaskInFlight_AwaitsQueueDrain`, which had
passed every earlier run and passed again on the next one. Re-running is what the ledger warns against
treating as an answer, so the mechanism was traced instead — and there was one.

`RecordConnectionHealth` resolved `IConnectionHealthService` from **the refresh's own scope**, so
`verdictRepository.Save()` flushed that scope's database context: every pending tracked change the
refresh was still holding, not just the verdict. A verdict could therefore commit half-finished refresh
work, or fail because of it. And because the catch logs the exception — which it must, so the type
reaches the log — a `DbUpdateConcurrencyException` raised by somebody else's pending change would be
logged under connection health, which is exactly the string that test asserts never appears.

The comment above the method already claimed a health verdict must not decide whether the refresh
succeeded. The shared context quietly contradicted it. `RecordConnectionHealth` now takes its own
scope, so `Save` can only ever write the verdict.

Recorded rather than quietly fixed because the failure looked exactly like the environmental flake
sitting next to it in the same run.

## Gates

| Gate | Result |
|---|---|
| `dotnet build` | 0 warnings, 0 errors |
| `dotnet test` (connector categories excluded) | 6 817 passed, 0 failed |
| `dotnet format analyzers --severity info` | **0 findings in any file this slice touches**; the 39 reported are all pre-existing CA1861/CA1825 in generated migrations plus one S6561 in an ADO connector test |
| `pnpm test` | 371 files, 5 194 tests, all green |
| `pnpm build` (Biome included) | clean |
| Mutation — backend | **91.89 %** |
| Mutation — frontend | **86.96 %** |
| Migrations | SQLite + Postgres, `CreateTable` + `CreateIndex` only; expand-only guard passes |

## Not done here

1. **The popover is still undocumented.** Nothing in the public docs describes the Task Manager at all —
   slices 02, 03 and 04 added none either. Writing it now means rewriting it when slice 06 adds the last
   section, so it is deliberately carried to the end of the Epic. This departs from the per-feature docs
   rule and is recorded as a departure rather than skipped.
2. **OQ-3 stays open.** Linear and CSV still emit no `authentication_failed` code, so they read
   `Unreachable` on failure. Honest, and a separate small piece of work.
3. **OQ-5 stays open.** `WorkTrackingSystemConnectionDto.RequiresReconnect` is still computed the
   OAuth-only way for the connection edit page. Two sources for one question, untouched by design.
4. **PAT expiry dates remain invisible.** #5019's description notes them; Lighthouse gains no awareness
   of them here, exactly as the slice brief scoped it.
5. **What remains on the Epic is slice 06.**

---

# Wave: DISTILL — slice 06

Run 2026-09-15. Reconciliation: passed, 0 contradictions. DISCUSS (D10, US-06), DESIGN (DDD-8,
ADR-185) and the surface inventory (S17, S18) all say the same thing about this slice — a new bounded
in-process sink, read through the controller that already guards the log file — and nothing accepted
since contradicts any of it.

## Wave: DISTILL / [REF] The contract this slice fixes

One route, on the controller that already exists, and one new setting.

`GET /api/latest/logs/problems` answers a JSON array, most recent first, with one object per retained
event:

| field | meaning |
|---|---|
| `recordedAt` | when it happened |
| `level` | `Warning`, `Error` or `Fatal`, rendered as the name — the browser reads it as a word, and renumbering would silently relabel what an operator sees |
| `source` | which part of Lighthouse is complaining |
| `message` | the sentence as it was rendered |
| `exceptionType` | the type of whatever threw, absent when nothing did |

It sits on `LogsController`, which has carried `RbacGuard(SystemAdmin)` since 2026-08-06 (S18), so the
guard is inherited rather than re-argued. `RecentProblems:Capacity` bounds the buffer and defaults to
200; eviction is oldest-first.

The section is per-process, starts empty, and does not survive a restart. That is a property of the
design (D10) and the UI states it in words rather than leaving it to be inferred — see the copy pinned
in AC-06.5 below.

## Wave: DISTILL / [REF] OQ-4 answered — the buffer holds 200

**Measured, not guessed.** Nine real day-logs from the dev instance
(`Lighthouse.Backend/Lighthouse.Backend/logs/`) were counted before the slice was written:
warning-and-above lands at **1–39 entries per day, median 7**. 200 therefore covers roughly five of the
busiest day observed, which is the right order for "what has gone wrong lately" without holding a month
of failures in memory. **OQ-4 is closed.**

**The secondary finding matters more than the number.** 65 % of those entries — **69 of 106** — are
startup chatter rather than connector signal: `Migrations: PRAGMA foreign_keys = 0`,
`Kestrel: Overriding address(es)` and their kin. That is dilution, not failure: the interesting
warnings are all still there and still few enough to read. Curation stays out of scope per D10, and
this is the measurement the follow-up would start from if the hypothesis does fire.

No test pins 200. A test that compares a value to the constant it came from is self-satisfying, and
200 is a tuning default rather than a promise — what is promised, and tested, is that the bound is
honoured and that the oldest goes first.

## Wave: DISTILL / [REF] Scenario list

**Backend acceptance** — `API/Integration/TaskManager/Slice06TheWarningsWithoutTheLog{Scenarios,Specifications}.cs`,
categories `acceptance` + `epic-5511-task-manager` + `slice-06`. All eight observe the route over HTTP.
Every one of them that has a problem to read produces it by running a **real refresh against a
connector that will not answer**, through the production queue, exactly as slices 01-05 drive it —
which is AC-06.7's whole point.

| Scenario | Tags | AC |
|---|---|---|
| `A_refresh_that_broke_is_waiting_in_the_popover_instead_of_in_the_log_file` | `@walking_skeleton @driving_port @real-io @error` | 06.1, 06.7 |
| `A_recent_problem_says_when_it_happened_how_serious_it_is_where_it_came_from_and_what_broke` | `@driving_port @real-io @error` | 06.1, 06.7 |
| `A_refresh_that_worked_leaves_nothing_behind_for_an_operator_to_worry_about` | `@driving_port @real-io` | 06.1 |
| `The_problem_that_just_happened_is_the_one_an_operator_reads_first` | `@driving_port @real-io @error` | 06.1 |
| `The_oldest_problem_makes_way_for_the_newest_once_there_is_no_more_room` | `@driving_port @real-io @error` | 06.2 |
| `Telling_the_instance_to_report_only_the_very_worst_takes_effect_on_the_spot` | `@driving_port @real-io @error` | 06.3 |
| `Telling_it_to_report_problems_again_takes_effect_on_the_spot_too` | `@driving_port @real-io @error` | 06.3 |
| `Recent_problems_are_not_handed_to_somebody_who_may_not_read_the_log_either` | `@driving_port @real-io @error` | 06.4 |

Error-path share: 6 of 8.

Three are worth calling out as more than restatements of an AC:

*A refresh that worked leaves nothing behind* is the negative control and the one that keeps the
section honest. Every refresh writes its own summary line; a section that carried those would be a wall
of text within the hour, which is exactly the outcome the hypothesis says would disprove the slice. Its
positive control is that the refresh really ran — without it, a read that silently answers nothing
passes.

*Telling it to report problems again* is the control on the scenario before it. A section that has
quietly stopped recording anything at all satisfies "report only the very worst" perfectly, and only
the pair distinguishes a level that took effect from a buffer that broke.

*Recent problems are not handed to somebody who may not read the log either* asserts against the log
file's own answer rather than against a status code, so the two cannot drift apart. It is the one
scenario not held back: it passes on arrival, and a guard that failed on the day it was written would
mean the attribute had gone missing.

**Frontend** — `components/App/Header/TaskManagerIcon.test.tsx`, under a `recent problems` describe (6),
following the shape slice 05 used for its own half.

| Scenario | AC |
|---|---|
| says what went wrong and how serious it was | 06.1 |
| reads newest first, in the order the instance gave them | 06.1 |
| says plainly that this is only since the instance started, and is not a complete history | 06.5 |
| says nothing has gone wrong rather than rendering an empty section | 06.6 |
| offers the way through to the full log | 06.5 |
| does not claim nothing has gone wrong when it could not ask | — |

The AC-06.5 copy is pinned against the **literal sentence**, not against the constant it will be read
from: *"Only what has gone wrong since this instance started. Not a complete history, and not kept
after a restart."* Blanking that constant has to turn the test red, and a test that reads the sentence
from the source cannot do that — this is the single most common survivor in copy-bearing code.

The last row is not an AC. Rendering "nothing has gone wrong" after a read that failed is the same
false reassurance the icon this popover replaced was built out of, so it is specified alongside the
empty state rather than left to be discovered.

## Wave: DISTILL / [REF] Ports and doubles

| Port | Class | Treatment |
|---|---|---|
| `GET /api/latest/logs/problems` | Driving | Real, over `Factory.CreateClient()` |
| `POST /api/latest/logs/level` | Driving | Real, over `Factory.CreateClient()` — the level control the log viewer already offers |
| The scheduled refresh (`ITeamUpdater`) | Driving | Real, through the production queue in its own DI scope |
| The Serilog pipeline and the sink behind `IRecentProblems` | Driven internal | Real — built by `Program.ConfigureLogging`, not by the test. See the wiring decision below |
| `IRepository<Team>`, `IRepository<WorkTrackingSystemConnection>`, `IUpdateStatusStore` | Driven internal | Real, EF over SQLite |
| `IWorkTrackingConnector` | Driven external | Faked; made to turn every refresh away, because a scenario about a tracker that will not answer cannot ask a real one to stop answering |

## Wave: DISTILL / [REF] Test placement

`Lighthouse.Backend.Tests/API/Integration/TaskManager/` — beside slices 01-05, on the harness they all
share. Precedent: the driving port for this Epic is a refresh run by the production queue, and
`TaskManagerAcceptanceTest` is where that is already stood up.

Frontend promises go in `TaskManagerIcon.test.tsx` under their own describe, which is where slice 05
put its own half. The **component** under test belongs in
`components/App/Header/TaskManager/RecentProblemsSection.tsx`, a sibling of `ActivitySection` and
`ConnectionsSection` — not inline in `TaskManagerIcon`, whose render body is already near the
cognitive-complexity limit this repository has lost two CI cycles to, and whose split exists precisely
to prevent a third section being added to it.

## Wave: DISTILL / [REF] Driving adapter coverage

| Entry point in DESIGN | Exercised by |
|---|---|
| Recent-problems read on `LogsController` | All eight backend scenarios, over HTTP |
| Level control on `LogsController` | The two AC-06.3 scenarios, over HTTP |
| Scheduled refresh (`ITeamUpdater` through the production queue) | Six backend scenarios |
| `RecentProblemsSection` in the popover | Six frontend promises, through `TaskManagerIcon` |

No entry point in this slice is left to a service-level test.

## Wave: DISTILL / [REF] Scaffolds

| File | State |
|---|---|
| `Models/Logging/RecentProblem.cs` | NEW — the record, real |
| `Services/Interfaces/IRecentProblems.cs` | NEW — the read-only port, real |
| `Services/Implementation/Logging/RecentProblemsSink.cs` | NEW — **scaffold**. `MostRecentFirst()` throws with the scaffold's own message; the ring buffer is DELIVER's |
| `Startup/LoggingConfigurator.cs` | EXTEND — takes the sink and wires it at `Warning` and above, the same way it already takes the level switch |
| `Program.ConfigureLogging` | EXTEND — constructs the sink from `RecentProblems:Capacity`, registers it as a singleton, hands it to `CreateLogger` |
| `API/LogsController.cs` | EXTEND — the read route; `#pragma warning disable S6960` with the reason written out, because a second injected service reads to Sonar as a second controller and D10/S18 say the opposite |
| `services/Api/LogService.ts` | EXTEND — `getRecentProblems`, real, on the service that already owns this controller |
| `tests/MockApiServiceProvider.ts` | EXTEND — the mock defaults to an instance that has had nothing go wrong |

`Emit` deliberately does **not** throw. The sink sits inside the log pipeline, so a scaffold that threw
from it would take logging down across the whole application instead of failing the tests waiting for
the buffer. It touches instance state instead — which is also what keeps S2325 and S4487 off a stub
that would otherwise not compile here.

The wiring is written now rather than left to DELIVER because it is the part the acceptance tests have
to prove; what is scaffolded is the behaviour. The cost is recorded rather than discovered: **adding a
line to `Program.cs` force-fulls every live-connector integration suite in CI** (`path-classifier.sh`
sets `connector_shared=true`), so this slice's first push runs Jira, ADO, Linear, ServiceNow and the
GitHub pair whether or not it touched them.

## Wave: DISTILL / [REF] The wiring decision — logging through the production logger, not a rebuilt one

`TaskManagerAcceptanceTest.Init()` removes `ILoggerFactory` and installs one writing to `CapturedLogs`.
In that harness an application warning would **never** reach a sink attached by
`Program.ConfigureLogging`, so a naively written scenario here would pass against a buffer that is not
wired into the product at all — the Tested-But-Unwired defect the port-to-port principle exists to
prevent.

**The decision**: the slice-06 `ConfigureAdditionalServices` rebuilds the factory around **the running
host's own production Serilog logger**, resolved from the container `UseSerilog` registered it in, with
`CapturedLogs` teed alongside. Nothing is re-configured and no second logger is constructed, so if
`Program` stops handing the sink to `CreateLogger` — or hands it one instance and registers another —
every scenario goes red.

Two shape differences had to be reconciled deliberately:

1. **Levels.** The harness pins `MinimumLevel.Verbose()` with `Microsoft.EntityFrameworkCore` and
   `Microsoft.AspNetCore` overridden to `Warning`; the production configurator reads from
   `IConfiguration`. The wrapper here keeps `Verbose` and applies **no overrides at all**, because
   anything it filtered would be filtered before the production logger ever saw it — and what the
   production logger sees is the subject. All the filtering that matters is the production logger's own,
   which is what AC-06.1 and AC-06.3 are about. The cost is a noisier `CapturedLogs`; nothing in this
   fixture asserts on it.
2. **Capacity.** The buffer's size has to be overridable for the eviction scenario, and the channel is
   `UseSetting`, not `ConfigureAppConfiguration`. Measured, not assumed: the sink is built on the way in
   to `builder.Build()`, and configuration sources added the other way are not applied until the build
   itself, so they arrive after the sink already exists. With `ConfigureAppConfiguration` the sink
   reported room for 200 while the fixture asked for 5, and the eviction scenario would have passed for
   the wrong reason.

`TaskManagerAcceptanceTest` was **not** modified. `ConfigureAdditionalServices` is the hook it already
provides for exactly this, and the capacity override chains a factory off `Factory` in the derived
`[SetUp]`, which is the same move slice 05 makes for its non-administrator scenario.

## Wave: DISTILL / [REF] Red gate

Seven of the eight backend scenarios failed against the scaffold, each for the right reason — the route
answering 500 from the `NotImplementedException` carrying the scaffold's own message. The eighth is
`Recent_problems_are_not_handed_to_somebody_who_may_not_read_the_log_either`, which passes on arrival
for the same reason its slice-05 twin does: it guards an attribute that is already there.

The wiring was verified separately rather than assumed. A throwaway probe resolved `IRecentProblems`
from the container after a real failing refresh and read the scaffold's own exception message back:

> Retaining recent problems is not implemented - DISTILL scaffold (room for 5, worst level seen so far Error)

Both halves are in that one line — *room for 5* proves the capacity override reached the sink the host
actually built, and *worst level seen so far Error* proves the failing refresh's error travelled the
production pipeline into the DI-registered instance. The probe was deleted; its output is the evidence.

Scenarios are held with `[Ignore]` so the hand-off commit is green. Unskipping one is the first act of
the DELIVER step that implements it.

## Wave: DISTILL / [REF] Upstream findings, and what is carried into DELIVER

**The row names the failing refresh by id, not by name — and that is the hypothesis, arriving early.**
AC-06.7 asks for verification against a real failing connector, and doing it surfaced an asymmetry
nobody had written down. The only line at warning-and-above that identifies a failed refresh is
`UpdateQueueService`'s own — *"Error processing update task for Team with ID 7"* — which carries the
update type and the id. The line that carries the team's **name** is the refresh summary
(*"Update completed | Team 'Lagunitas' | … | success=False"*), written at `Information` by slice 01's
one-line-per-refresh work, and therefore below this section's threshold by design.

So a severity filter surfaces the line with the id and hides the line with the name. The scenarios
assert what is actually there — the row identifies which refresh broke and what broke it — rather than
demanding the name, because naming the team in the queue's error line would mean a repository lookup
inside the queue, which DDD-10 argues against, and it is not in this slice's scope. **This is a finding
about the hypothesis, not a defect**: it is a concrete instance of "a level filter alone yields signal
rather than noise" being only half true, and it is the first thing to look at if the section reads
badly on a real instance. The curated-operational-events follow-up D10 names is where it would be
fixed.

**Carried into DELIVER:**

1. The ring buffer itself — bounded, oldest-first, thread-safe, and the `LogEvent` to `RecentProblem`
   projection including trimming `SourceContext` to its last segment the way the file template does.
2. `RecentProblemsSection.tsx` and its wiring into `TaskManagerIcon`, including the link to
   `/settings?tab=system-info`.
3. A unit-level check that a rolled-over buffer holds exactly its capacity. The acceptance scenario
   proves the oldest goes first; the exact count at the boundary is a data-structure invariant and is
   cheaper to pin there.
4. The public docs for the Task Manager, which slices 02-05 deliberately deferred to the end of the
   Epic. Slice 06 is the end of the Epic.

---

## Wave: DELIVER / [REF] What slice 06 shipped

`RecentProblemsSink` is a bounded ring buffer — a `Queue<RecentProblem>` behind one lock taken by both
`Emit` and `MostRecentFirst`, evicting oldest-first, answering most-recent-first. It projects each event
to time, level, short source name, rendered message and exception type. The scaffold DISTILL left behind
(whose `MostRecentFirst` threw) is gone; the Scaffolds table above records the state at DISTILL, not now.

`RecentProblemsSection.tsx` sits beside `ActivitySection` and `ConnectionsSection`. It takes
`IRecentProblem[] | null`: `null` means the instance was asked and did not answer, and the section renders
nothing rather than the reassuring empty state — an unanswered question is not a clean bill of health, which
is the failure mode the icon slice 05 deleted was built out of.

### Two things decided during DELIVER that the acceptance criteria did not settle

**A mistyped buffer size does not stop the instance starting.** The sink's constructor refuses a
non-positive capacity, because a ring buffer with no room is meaningless. The *configuration boundary*
is lenient instead: `RecentProblems:Capacity` set to zero or a negative number falls back to 200. The
invariant belongs to the type; the leniency belongs where the untrusted number arrives, and only there can
a programmer's mistake be told from an operator's typo. A diagnostic buffer must never be the reason the
whole application will not boot.

**`RecentProblems:Capacity` exists at all** because AC-06.2 is otherwise untestable at the port — pinning
eviction against the shipped default would mean provoking two hundred real failures.

### The hypothesis, answered early and not entirely in its favour

The slice set out to disprove that a level filter alone yields signal rather than noise. It survives, but
with two findings that belong to whoever picks up the follow-up D10 names:

**Volume is not the problem.** Nine days of a real instance's logs hold 1–39 warning-or-worse events per
day, median 7. A level filter leaves a readable list, not a wall.

**Composition partly is.** Two thirds of those entries (69 of 106) are startup chatter — `Migrations:
PRAGMA foreign_keys = 0`, `Kestrel: Overriding address(es)` — rather than anything about a connector, so a
freshly started instance opens on framework noise.

**And the filter hides the better line.** The only warning-or-above line identifying a failed refresh is
the queue's own, which names it by id: *Error processing update task for Team with ID 7*. The line that
carries the team's **name** is the refresh summary, written at `Information` and therefore below this
section's threshold by design. The severity filter keeps the row with the id and drops the row with the
name. The acceptance scenarios match on the id rather than smoothing this over, because it is evidence
about the instrument, not an implementation detail: it is the sharpest argument yet that the follow-up
wants a named set of operational events rather than a severity threshold.

### Gates

`dotnet build` zero warnings · backend suite 6837 passed / 10 skipped, the single failure the known
`ServiceContainer_BuildsWithoutScopeViolations` teardown `IOException` · `dotnet format analyzers
--severity info` zero findings in any touched file · `pnpm test` 5200 passed · `pnpm build` clean ·
mutation **95.65 % backend**, **66.67 % frontend with 12 of 12 behavioural mutants killed** and six
cosmetic survivors recorded in `mutation/results.md`.

### Carried out of this slice

The public Task Manager docs, which slices 02–05 deferred to the end of the Epic. Slice 06 is the last
slice, so they are now the Epic's only outstanding work.

---

## Wave: DELIVER / [REF] The rows say whose refresh broke (OQ-6, follow-up to slice 06)

Slice 06 shipped rows reading `Error processing update task for Team with ID 7`. The maintainer's call on
review: an id is not something an operator can act on, and the row must say the entity's name.

This turned out to cost nothing in new concepts, because the log call was **already structured** —
`"...for {UpdateType} with ID {Id}"` — so the values arrive at the sink as properties rather than as text
to be parsed back out.

- `RefreshSubject(Kind, Id, AsWritten)` names a concept that had none: which refresh a problem was about,
  plus the exact stretch of the sentence standing for it. Captured while the unrendered template is still
  in hand, so naming it later is a search for a piece of text and nothing more.
- `RecentProblemsSink` captures the reference and **never touches the database** — it runs on every thread
  in the application, inside the log pipeline.
- `RecentProblemsReport` resolves the name on read through the existing `IUpdateTaskNaming`, which the
  task list already uses. No second resolver, no port widening, and the answer is never older than the
  moment it is read, so a rename shows immediately.
- `RecentProblem.Refresh` is `[JsonIgnore]`. **The wire contract is unchanged**: the reference exists to
  have a name looked up for it, and by the time the browser is answered that has happened. The frontend
  needed no production change at all.

### What the change revealed

**A portfolio refresh was worse off than a team one.** `UpdateType` has no `Portfolio` member — portfolios
queue as `Features` — so those rows read `Error processing update task for Features with ID 1`: an
unhelpful id *and* a type name no operator would recognise. Both now route through the naming port.

### Two things a future reader needs

**A deleted entity still reads `Team 7`**, because the naming port falls back to kind-and-id rather than
returning nothing. That fallback is permanent, not transitional — it is what stops the change turning a
row blank for an entity that vanished mid-refresh.

**Detection requires both properties, with their types.** `Id` is about as generic a property name as
exists, so a line using it for a work-item reference would otherwise be rewritten into the name of team
nought. The pairing with a real `UpdateType` is what makes it narrow, and a template carrying `{Id}`
before `{UpdateType}` is deliberately not enriched rather than enriched wrongly.

Mutation **95.00 %** backend after the assertion-strength gaps found at 80.00 % were closed — see
`mutation/results.md`, which records the two survivors kept alive and why.
