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
