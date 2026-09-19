# Feature Delta — story-5877-update-queue-lanes

**ADO**: User Story #5877 — *Update queue is a single lane: one slow portfolio refresh starves all team
refreshes* · New · tagged `Release Notes` · Related to Epic #5511 · reported by Janée McConnell on
behalf of a user running 26.8.14.1 standalone (Tauri, macOS, SQLite) against Jira Data Center.

**Waves**: DISCUSS (2026-09-18).

**One line**: every refresh in Lighthouse queues in one lane, so a Portfolio refresh that will not
finish stops every Team refresh behind it — for 3h38m in the reported case — and nothing bounds how
long one refresh may hold that lane.

**Density**: `lean` + `ask-intelligent` (`~/.nwave/global-config.json`). Tier-1 `[REF]` only.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `platform-operator` | **Primary and only.** Runs the instance, watches the dashboards stop moving, and today has one remedy: restart it. Both flavours — the self-hoster on SQLite standalone (the reporter's user) and the SaaS operator on Tenant Zero. |

No new persona. The `lighthouse-maintainer` appears only as the person who dogfoods the change, not as
a stakeholder with a distinct job.

---

## Wave: DISCUSS / [REF] JTBD One-Liners

| Job ID | One-liner |
|---|---|
| `job-operator-keep-teams-moving-while-a-portfolio-refreshes` | When one entity's refresh is slow, I want every other entity to keep refreshing, so one slow portfolio does not read as a dead instance. |
| `job-operator-bound-a-refresh-that-will-not-end` | When a refresh stops making progress, I want Lighthouse to end it on its own and say so, so an instance cannot be lost to a single run nobody is watching. |

Both new, both `platform-operator`. Opportunity scores in `docs/product/jobs.yaml`.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Read from the code on 2026-09-18, not taken from the bug report's own analysis. The report carries a
root-cause section; it was re-derived, and where it is now out of date that is recorded.

| # | Fact | Evidence |
|---|---|---|
| S1 | **One lane, one reader, every update type.** A single `Channel.CreateUnbounded<Func<Task>>()` drained by one `await foreach (… ReadAllAsync())` loop that awaits each task to completion before taking the next. Registered as a singleton. Teams, Portfolios, Forecasts and both delete operations share it. | `UpdateQueueService.cs:11`, `:602-621`; `Program.cs:1528` |
| S2 | **`waitingBehind` names an arbitrary running row.** The read model picks `admitted.FirstOrDefault(work => work.Status == InProgress)` and calls that the lane holder. Correct only while exactly one thing can ever be running. | `UpdateController.cs:61-62` |
| S3 | **`WriteBackRound` is half thread-safe.** `Join`/`Leave` are `Interlocked` and `HasFinished` is `Volatile` — the parts written for concurrency. `Stage`, `TakeStaged`, `ReportRefresh`, `ReportForecast` and `TakeSummary` mutate plain `Dictionary` fields with no synchronisation. A Portfolio refresh and the Forecast it triggers **already share one round**; they are safe today only because they run one after the other. | `WriteBackRound.cs:15-17,24,33,42,79-107`; `PortfolioUpdater.cs:123`; `UpdateQueueService.cs` `RoundForNewWork` |
| S4 | **Nothing bounds an update's total wall-time, and the failure log names nothing.** The consumer loop awaits each task inside a bare `try/catch` that logs `"Error processing update task"` with no `UpdateKey`. A refresh that never returns holds the lane until the process restarts. | `UpdateQueueService.cs:610-620` |
| S5 | **A cancellation token *does* reach inside a connector call**, and the reachable granularity is one page round-trip. Measured 2026-09-14 by #5511's slice-04 probe, which runs in the ordinary suite. This was not known when #5877 was written; it means **evicting** a running refresh is mechanically possible, not only dropping queued work. | `slice-04-stop-a-refresh.md:97-154`; `API/Integration/TaskManager/Slice04CancellationReachProbe.cs` |
| S6 | **The cross-replica execution lock is already per-key**, not global — `AcquireAsync(updateKey, …)`. Parallel lanes do not weaken the guarantee that one key runs on one replica. | `UpdateQueueService.cs` `RunUpdateAsync`; `Program.cs:1511,1518` |
| S7 | **Each update already runs in its own DI scope**, and `IWriteBackCollector` is `Scoped`. Two concurrent executions get two collectors. The only shared mutable object on the path is the round itself (S3). | `UpdateQueueService.cs` `ExecuteUpdateTask`; `Program.cs:1386` |
| S8 | **On SQLite, each `DbContext` gets its own connection**, because `AddDbContext` uses the `(provider, options)` overload and so rebuilds the options per context. WAL is on and `busy_timeout` is 10 000 ms, so concurrent writers serialise at the file rather than failing — but 10 s is a ceiling, not a guarantee. | `DatabaseConfigurator.cs:23,60-83`, PRAGMAs at `:71-73` |
| S9 | **Item A of the report is already shipped.** `GetParentFeaturesDetails` chunks at `ReferenceIdsPerQuery`, with a test. | `690a208f9` (2026-09-14); `JiraWorkTrackingConnector.cs:349-356` |
| S10 | **Item C's visibility half is already shipped.** The task list shows what is running, for how long, in queue order, with a stop control — #5511 slices 02, 03, 04 and 07. Only C's wall-time *bound* is outstanding, exactly as that Epic's reconciliation predicted. | `feature-delta.md:1131` of `epic-5511-task-manager` |

∴ **S1 is the defect and S3 is its price.** Removing the single lane removes the only thing currently
making `WriteBackRound` safe, so round safety is not an optional hardening — it is a precondition, and
it has to land before the second consumer exists rather than after.

∴ **S2 makes the fix invisible if left alone.** With several lanes running, a queued Team would be
told it is waiting behind a Portfolio it is not waiting behind. The list would be wrong in exactly the
situation the list was built to explain.

∴ **S5 changes the shape of the bound.** #5877 proposed a watchdog that logs. The probe means it can
be a watchdog that *acts*, through the cancel path an operator already drives.

---

## Wave: DISCUSS / [REF] Locked Decisions

### D1 — Scope is item B plus item C's wall-time bound. Item A is done.

The report proposed A, B and C. A shipped on 2026-09-14 (S9). C's visibility half shipped as four
slices of #5511 (S10). What is left is the lane structure and the bound — confirmed with the user,
2026-09-18.

### D2 — Lanes are per update type, not N generic consumers

`UpdateType` has five members: `Team`, `Features` (portfolios), `Forecasts`, `PortfolioDelete`,
`TeamDelete`. N generic consumers would let two Portfolio refreshes run at once, which multiplies
tracker load in exactly the deployment that reported this — an on-premise Data Center instance already
close to its limits. Per-type lanes fix the reported starvation and change nothing about how hard any
one tracker is hit at a time.

### D3 — Within a type, work stays serial

One Portfolio refresh at a time; one Team refresh at a time. This is a deliberate retention, not an
oversight: same-type work is the work most likely to share a connection and a rate limit. Revisitable
in DESIGN if a lane turns out to be the wrong granularity, and listed in the handoff as such.

### D4 — `waitingBehind` means "what is holding *your* lane"

Not "something that is running". Corrects `UpdateController.cs:61`. A queued Team names the running
Team; a queued Portfolio names the running Portfolio; work whose lane is free never says it is
waiting behind anything.

### D5 — Round safety is a precursor commit, not a slice

Making `WriteBackRound` safe for two concurrent executions has no user-visible output of its own, so it
cannot ship as a slice — a slice of only `@infrastructure` stories is a structural failure. It lands as
the first commit of slice 01, before the second consumer exists.

### D6 — Deletes share their entity type's lane

`PortfolioDelete` runs in the Portfolio lane, `TeamDelete` in the Team lane. A delete and a refresh of
the same entity type serialising is the conservative reading, and deletes are awaited by an HTTP caller
that is already holding a response open. They are also the two update types that cannot be cancelled
(`UpdateController.cs` `CancelTask` refuses them), so they must not become evictable by D9 either.

### D7 — The bound is per run, not per key

A coalesced follow-up starts a fresh clock. Inheriting the elapsed time of the run it follows would
kill every follow-up of a long refresh the instant it started — a bound that makes the reported
symptom worse.

### D8 — The bound has a configurable value with a generous default, and the default is chosen from real data

Lighthouse already stores `DurationMs` for every refresh in `RefreshLog`. The default is set in DESIGN
from that distribution rather than guessed here; the reported instance's 77.7-minute run and #5687's
468-second pre-optimisation measurement are both on record as points it must clear. This is the one
deliberate gap in this wave's requirements completeness.

### D9 — Eviction uses the operator-cancel path, not a new mechanism

S5 proved a token reaches inside a connector call. The bound therefore drives the same
`AdmittedCancellations` path an operator's Cancel drives, and the run reaches the same visible terminal
state. The only new thing is who pressed it — which is why the refresh history has to distinguish the
two (AC-02.2). Not applicable to the two delete types (D6).

### D10 — RBAC: no new gate

The task list is already System Administrator only. The bound is instance configuration, which is
already administrator-only on every surface it could land on. No permission is added, removed or moved.

### D11 — Terminology: the tenant's words, everywhere the change is visible

Task-list rows already render the tenant's configured word for team and portfolio. Any new copy — the
reason on a bound-ended run, the bound's own setting label — uses the same source. No literal "Epic",
"Initiative" or "Project" in user-facing text.

### D12 — Lighthouse-Clients (CLI / MCP): not applicable, explicitly

No CLI or MCP tool exposes the update task list or instance refresh configuration. The
`UpdateTaskResponse` record keeps its shape; only `WaitingBehind`'s meaning is corrected (D4), and
nothing outside the frontend reads it.

### D13 — Website marketing surface: not applicable, explicitly

This removes a failure mode from an existing capability. It is release-notes material — the story
carries the `Release Notes` tag — not a new thing the site advertises.

### D14 — Free, not premium

Same as #5511. An instance that appears wedged is a correctness problem, and correctness is not a tier.

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — right-sized.** Two user stories, two slices, one bounded context (the update pipeline), one
precursor commit. Backend-dominant with one small frontend consequence. No new endpoint, no schema
change, no external integration, no walking skeleton.

The one oversized signal that came close is "walking skeleton requires >5 integration points" — the
change touches the queue, the round, the read model, the cancel path and the refresh log. It is not
five *integrations*; it is five call sites in one context, all of them already in production and all of
them covered by the #5511 suite.

---

## Wave: DISCUSS / [REF] WS Strategy

**C — no walking skeleton.** Brownfield. Every mechanism on the path runs in production today: the
channel, the per-key execution lock, the ambient cancellation context, the task-list read model, the
refresh log. Nothing here is a mechanism nobody has run.

---

## Wave: DISCUSS / [REF] Driving Ports

| Surface | Change |
|---|---|
| Header → Task Manager popover | Several rows can read *Running* at once. A queued row names the holder of its **own** lane (D4). |
| Settings → System Info → refresh history | A run ended by the bound appears with its duration and a reason that distinguishes it from an operator's Cancel (D9). |
| `GET /api/latest/update/tasks` | Response shape unchanged. `WaitingBehind` semantics corrected (D4). |
| Instance configuration | Gains the wall-time bound. Which surface holds it is open for DESIGN (D8). |
| CLI / MCP | **None** (D12). |
| HTTP API additions | **None.** |

---

## Wave: DISCUSS / [REF] Pre-requisites

- **#5511 slices 01–08 are pushed.** Every surface this story changes was built by them. Nothing here
  is blocked on that Epic; the dependency is already satisfied.
- **#5511's sequencing decision is spent.** Its reconciliation said B should be "built in the same
  working session as slice 04, on the same branch, after slice 04's P5 probe has reported". The probe
  reported (S5) and slice 04 shipped, so the adjacency is no longer available. B is now a standalone
  pass over a class slice 04 already rewrote — which is the cost that decision was trying to avoid, and
  it is now sunk.
- No other in-flight item touches `UpdateQueueService`, `WriteBackRound` or `UpdateController`.

---

## Wave: DISCUSS / [REF] Out of Scope

- **Parallelising two refreshes of the same type** (D3).
- **The orphaned-feature cleanup path.** `PortfolioUpdater.cs:176` runs `CleanUpOrphanedFeatures()` in a
  `finally`, so it runs after a *failed* refresh too, and `OrphanedFeatureCleanupService` hard-deletes
  any Feature matching `!IsParentFeature && !Portfolios.Any()`. The reporter's log shows "Cleaned up 36
  orphaned features" immediately after the DNS failure. **Split to its own Bug** by user decision,
  2026-09-18 — it is a potential data-loss path with a different root cause, a different fix and a
  different urgency, and folding it in would make this story two stories wearing one number. Carried
  into the handoff with the verification it needs so it is not lost.
- **Queue ordinal position and wait estimate** — deferred item G's arithmetic half, already refused in
  #5511 (D15) because an estimate needs historical durations and is wrong in precisely the case #5877
  describes.
- **Jira parent-Feature chunking** — shipped (S9).
- **Priority or preemption between lanes.** Lanes are independent, not ranked.
- **Cross-replica lane coordination.** Each replica runs its own lanes; the per-key execution lock (S6)
  already prevents one key running twice.
- **Persisting queue wait in `RefreshLog`.** Would make KPI-1 cheaper to measure. Not required, and
  named in the handoff as an optional DESIGN addition rather than assumed.

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — A slow Portfolio refresh stops holding up every Team

`job_id`: `job-operator-keep-teams-moving-while-a-portfolio-refreshes`

As a Platform Operator, when one Portfolio refresh is slow or stuck, I want every Team to go on
refreshing, so that I can tell "one portfolio is slow" from "Lighthouse is dead" without restarting it.

#### Elevator Pitch

```
Before: while a Portfolio refresh is running, no Team refresh starts; the instance looks wedged and
        only a restart clears it.
After:  open the Task Manager popover in the header while a long Portfolio refresh is running → sees
        the Team row reading `Running` beside the Portfolio's, and that Team's metrics dashboard
        showing a fresh last-updated moment.
Decision enabled: the operator decides whether one portfolio's query needs narrowing, instead of
        deciding whether to restart the instance.
```

#### Acceptance Criteria

- **AC-01.1** — With a Portfolio refresh **held open** (a connector double that blocks until released,
  not merely a slow one — the failure mode is starvation, and a test that waits for a slow refresh to
  finish passes against the bug), triggering a Team refresh takes it to `InProgress` while the
  Portfolio's is still `InProgress`.
- **AC-01.2** — With a Portfolio refresh held open, a second Portfolio refresh stays `Queued` (D3).
- **AC-01.3** — With a Portfolio and a Team both running and a second Team queued, the queued Team's
  `waitingBehind` is the **running Team's** name, not the Portfolio's (D4). Asserted over repeated
  reads, because `FirstOrDefault` over a concurrent store is non-deterministic and one correct read
  proves nothing.
- **AC-01.4** — Work whose lane is free reports `waitingBehind` as absent, never as an arbitrary
  running row.
- **AC-01.5** — A Portfolio refresh and the Forecast it triggers still write back **once, together**.
  With both executions staging into the same round concurrently, the set the round hands to the writer
  equals the union of what both staged — nothing lost, nothing sent twice (S3).
- **AC-01.6** — Cancelling a running update in one lane leaves a running update in another lane
  running.
- **AC-01.7** — Shutdown still drains: with work in flight in two lanes, `DrainAsync` returns only
  after both have finished or the shutdown timeout has elapsed, and neither is left admitted.
- **AC-01.8** — **Production data.** On Tenant Zero, against the real work-tracking connections, two
  `RefreshLog` rows exist whose run intervals overlap — one Team, one Portfolio. Synthetic doubles
  prove the lanes exist; only this proves they survive a real connector, a real database and a real
  write-back round.

---

### US-02 — A refresh that will not end is ended, and says so

`job_id`: `job-operator-bound-a-refresh-that-will-not-end`

As a Platform Operator, I want a refresh that has run past a sane bound to be stopped and recorded, so
that no single run can quietly hold a lane until I next restart the instance.

#### Elevator Pitch

```
Before: a refresh that never returns holds its lane until the process restarts; the log line says
        "Error processing update task" and names nothing.
After:  it stops on its own → the Task Manager row reaches `Cancelled`, and Settings → System Info →
        refresh history shows the run with its duration and a reason naming the bound it passed.
Decision enabled: the operator learns their refresh is systematically too slow for their tracker — and
        widens the interval or narrows the query — instead of discovering it as an apparently dead app.
```

#### Acceptance Criteria

- **AC-02.1** — An update whose total wall-time passes the bound is cancelled through the same path an
  operator's Cancel drives (D9), and its row reaches the `Cancelled` terminal state.
- **AC-02.2** — Its `RefreshLog` row records the run as cancelled with a reason naming the bound, and
  that reason is distinguishable from an operator-initiated cancel. An operator who did not press
  anything must not read a history that says they did.
- **AC-02.3** — The lane is free afterwards: the next queued work of that type starts.
- **AC-02.4** — It stops **within one page round-trip** of the bound firing, the granularity S5
  measured. If it does not, the bound is a log line rather than a lever, and the slice has failed its
  own hypothesis.
- **AC-02.5** — A coalesced follow-up starts a fresh clock (D7): after a run ends at the bound, its
  follow-up runs to completion rather than being killed immediately.
- **AC-02.6** — The bound is configurable, and a value that is absent or unreadable falls back to the
  default rather than to "no bound" or to zero.
- **AC-02.7** — The two delete types are never ended by the bound (D6) — a half-done delete leaves the
  caller told an entity is gone while its row is in the database.
- **AC-02.8** — **Production data.** On Tenant Zero, with the bound temporarily lowered, a real refresh
  against a real connector is ended by it, the history records it, the lane frees, and the next
  scheduled refresh of that type runs normally. Bound restored afterwards.

---

## Wave: DISCUSS / [REF] Story Map

**Backbone**: *Notice it is slow* → *Keep working anyway* → *Stop what will never finish* → *Read what
happened*.

The first and last activities are shipped (#5511 slices 02, 03, 07 and the refresh history). This story
fills the middle two.

| Slice | Story | Activity | Ships |
|---|---|---|---|
| 01 | US-01 | Keep working anyway | Per-type lanes, round safety as a precursor commit, corrected `waitingBehind` |
| 02 | US-02 | Stop what will never finish | Wall-time bound driving the cancel path, recorded in the refresh history |

Slice briefs: `slices/slice-01-teams-keep-moving.md`, `slices/slice-02-nothing-holds-a-lane-forever.md`.

---

## Wave: DISCUSS / [REF] Slice Taste Tests

| Test | Verdict |
|---|---|
| "Ships 4+ new components" → not thin | **Pass.** Slice 01 changes two classes and one read-model line; slice 02 adds one bound and one reason. |
| Every slice depends on a new abstraction → ship the abstraction first | **Pass, and acted on.** The lane abstraction is slice 01's own subject. Round safety is the shared precondition and is pulled out as a precursor commit (D5). |
| No slice disproves any pre-commitment → decoration | **Pass.** Slice 01 disproves that head-of-line blocking is the whole of "hung"; slice 02 disproves that the measured cancellation granularity is good enough to act on. Both are genuine, and both would change what gets built next. |
| Synthetic data only → proves plumbing | **Pass.** AC-01.8 and AC-02.8 are production-data criteria on Tenant Zero, and both are stated as overlapping or bound-ended `RefreshLog` rows rather than as an observation someone made. |
| 2+ slices identical except for scale | **Pass.** Different mechanisms, different risks. |
| Slice composition — every slice carries a user-visible value story | **Pass.** Both slices have a non-`@infrastructure` story with a complete Elevator Pitch. The one `@infrastructure` piece is a precursor commit, not a slice (D5). |

---

## Wave: DISCUSS / [REF] Prioritization

**Slice 01 first**, on learning leverage rather than dependency. Slice 02's bound is only worth having
if the lanes did not already answer the reported complaint — if teams keep moving, a stuck portfolio
becomes a slow portfolio rather than a dead instance, and the bound changes from a rescue to a tidy-up.
Building the rescue first would be building for a world slice 01 may have already removed.

Slice 01 also carries the concurrency risk. Failing there is cheaper before the bound exists to
complicate the picture.

**Slice 02 second**, and genuinely optional on slice 01's dogfood outcome — the one place in this story
where a slice may be dropped rather than deferred.

Dogfood cadence: each slice is dogfooded on Tenant Zero the day it ships (AC-01.8, AC-02.8).

---

## Wave: DISCUSS / [REF] Outcome KPIs

| # | KPI | Target | Measurement |
|---|---|---|---|
| KPI-1 | A Team refresh runs while a Portfolio refresh is in flight | ≥1 overlapping Team/Portfolio pair per day on Tenant Zero for 7 days after slice 01 | Two `RefreshLog` rows whose `[ExecutedAt − DurationMs, ExecutedAt]` intervals overlap. Persisted, so provable after the fact rather than observed live. |
| KPI-2 | Longest single update wall-time | No `RefreshLog.DurationMs` exceeds the configured bound plus one page round-trip, over 30 days after slice 02 | `RefreshLog.DurationMs` on Tenant Zero. |
| KPI-3 | Write-back integrity under concurrency | Zero staged updates lost | The concurrent-staging assertion in AC-01.5, plus comparing staged against written counts in the per-round write-back summary over the 7-day dogfood window. |
| KPI-4 | "I had to restart it" reports | 0 in the 60 days after release | Community and support reports naming a stuck or hung refresh, counted against the same 60-day window before release as a baseline. |

KPI-1 is deliberately not "queue wait under N seconds": `UpdateStatus.QueuedAt`/`StartedAt` live in the
status store and are not persisted, so a wait-time target would need a schema change this story does not
need. The overlap test answers the same question from data already written.

---

## Wave: DISCUSS / [REF] Definition of Done

1. Both slices' acceptance criteria pass as automated tests (NUnit; Vitest for the read-model change).
2. `dotnet build` zero warnings; `dotnet test` green on the non-connector filter.
3. `pnpm test` green; `pnpm build` zero errors and zero warnings; Biome clean on `./src`.
4. SonarQube Cloud introduces no new issue of any severity.
5. Backend mutation testing (Stryker.NET) run per-feature, ≥80% kill rate, recorded under
   `docs/feature/story-5877-update-queue-lanes/mutation/`. Frontend Stryker scoped to the changed
   task-list component.
6. `docs/` prose: the configuration page gains the wall-time bound and what it does. The concurrency
   change needs no user-facing prose — it removes a failure mode rather than adding a control.
7. Per-feature screenshots: **N/A, because** nothing changes what a screenshot would show. The task
   list's rows already render the same way; only how many can read *Running* at once differs, and that
   is not a stable thing to capture.
8. `ARCHITECTURE.md` / `docs/product/architecture/brief.md`: updated — the update pipeline's
   single-lane property is documented there and stops being true.
9. An ADR for the lane structure if DESIGN's answer differs from D2/D3, alongside ADR-183's precedent.
10. Lighthouse-Clients CLI/MCP version bump: **N/A, because** no client-facing contract changes (D12).
11. Website marketing surface: **N/A, because** this removes a failure mode from a shipped capability
    (D13).
12. RBAC impact: **N/A, because** no gate is added, removed or moved (D10).
13. Release notes drafted from the customer pain — "team metrics stopped updating and only a restart
    fixed it" — not from the mechanism. The story carries the `Release Notes` tag, and the reporter and
    the affected user are credited.
14. ADO #5877 transitioned New → Active → Resolved, not Closed. The orphaned-feature finding raised as
    its own Bug before #5877 resolves, so it does not leave with the story.

---

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Evidence |
|---|---|---|
| 1 | Business value stated | Both elevator pitches. A field report with logs: 3h38m of zero team updates, recovered only by restart, on a supported standalone configuration. |
| 2 | Job traceability | US-01 → `job-operator-keep-teams-moving-while-a-portfolio-refreshes`; US-02 → `job-operator-bound-a-refresh-that-will-not-end`. No `infrastructure-only` escape used. |
| 3 | Acceptance criteria testable | 16 ACs, each asserting an observed status, a persisted `RefreshLog` row, a rendered value, or a measured interval. Two are production-data criteria. |
| 4 | Dependencies known | #5511 slices 01–08 shipped; its sequencing decision recorded as spent. No other in-flight item touches the three files. |
| 5 | Sized | Two slices, ~6h and ~4h of crafter dispatch, plus a precursor commit inside slice 01. |
| 6 | Technical feasibility | S5 proves the eviction path exists and measures its granularity. S6, S7 and S8 establish that the execution lock, the DI scoping and the SQLite connection model already tolerate concurrent executions. S3 is the one genuine trap and it is named with file and line before a slice starts. |
| 7 | Non-functional constraints | Per-type lanes keep per-tracker concurrency at one (D3), so tracker load does not rise. SQLite writers serialise under WAL with a 10 s `busy_timeout` (S8) — a ceiling DESIGN must check against a real two-lane save, not assume. |
| 8 | UX defined | D4 fixes what a queued row says. AC-02.2 fixes what the history says and that it must not read as an operator action. No new control, no new surface. |
| 9 | Testable in isolation | `UpdateQueueService` and `WriteBackRound` are unit-testable with a blocking connector double; the read model through the existing `TaskManagerAcceptanceTest`; the frontend through the existing `TaskManagerIcon` tests. |

**Requirements completeness: 0.96.** The one deliberate gap is D8 — the bound's numeric default and the
configuration surface that holds it. Both are one decision, both are DESIGN's to make from `RefreshLog`
data this product already stores, and slice 01 does not depend on either.

**Per-wave peer review: skipped.** DoR surfaced no ambiguity, the JTBD rests on a field report with
logs rather than on assumption, and no vendor-neutrality risk appears in the ACs. The consolidated
review fires at end of DISTILL.

**Expansion catalog — one trigger fired.** AC ambiguity: no, every AC names an observable. Multi-
stakeholder: no, one persona. Compliance: no regulatory language. WS strategy is C, not D.
**Cross-context complexity: fires** — the change spans four distinct technologies on one path (ASP.NET
Core, React/TypeScript, Redis as the distributed status store and SignalR backplane, and
SQLite/Postgres under the execution lock). Suggested expansion: `alternatives-considered`. Offered at
wave end; not rendered.

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key Decisions

- **[D1]** Scope is B plus C's wall-time bound; A is shipped and C's visibility half is shipped
  (user, 2026-09-18).
- **[D2]** Per-type lanes, not N generic consumers — protects the on-premise tracker that reported this.
- **[D3]** Same-type work stays serial, deliberately; revisitable in DESIGN.
- **[D4]** `waitingBehind` means the holder of your own lane, correcting `UpdateController.cs:61`.
- **[D5]** `WriteBackRound` safety is a precursor commit inside slice 01, not a slice.
- **[D7]** The bound is per run; a coalesced follow-up starts a fresh clock.
- **[D8]** The bound's default comes from `RefreshLog` data in DESIGN, not from a guess here.
- **[D9]** Eviction drives the operator-cancel path, which S5 proved reaches inside a connector call.

### Requirements Summary

- Primary need: one slow entity must not stop every other entity refreshing, and no single run may hold
  a lane indefinitely.
- Walking skeleton scope: N/A — strategy C, brownfield.
- Feature type: **backend**, with one read-model correction visible in the frontend.

### Constraints Established

- `WriteBackRound`'s staging and reporting are unsynchronised (S3). Nothing may run two executions of
  one round concurrently until that is fixed.
- Per-tracker concurrency must stay at one (D3) — the reporting deployment is an on-premise Data Center
  instance already close to its URL and rate limits.
- On SQLite, concurrent writers serialise at the file with a 10 s `busy_timeout` ceiling (S8). DESIGN
  verifies a two-lane save against it rather than assuming WAL makes it free.
- The two delete types are never cancellable and never evictable (D6).
- Deleting the single lane deletes the only thing that currently makes several invariants hold by
  construction. Each one named above gets a test, not an argument.

### Upstream Changes

No DISCOVER or DIVERGE wave ran for this story. The nearest upstream artifact is the **Post-DESIGN
Reconciliation** in `docs/feature/epic-5511-task-manager/feature-delta.md:1096`, and this wave changes
two of its conclusions:

> **Original (2026-09-14, `feature-delta.md:1130`)**: *"**Sequence with slice 04.** Slice 04's probe
> (P5, S9+S10) answers part of B for free: whether cancellation can reach inside a connector call
> determines whether an eviction path is even possible."*

**New assumption**: the probe reported and slice 04 shipped, so the adjacency is spent and B is a
standalone pass. The probe's *answer* survives and is stronger than expected — a token reaches inside a
connector call, at one page round-trip of granularity — which is why D9 makes the bound act rather than
log. Recorded here rather than back in #5511, whose DISCUSS documents are not modified.

> **Original (2026-09-14, `feature-delta.md:1131`)**: *"**C** … **Covered by 02 + 03.** Only the
> wall-time bound is genuinely new."*

**New assumption**: unchanged in substance, and now acted on — the bound is US-02, and slices 02, 03
and 07 having shipped is what makes it the only outstanding half.

---

## Wave: DISCUSS / [REF] SSOT Updates

| File | Change |
|---|---|
| `docs/product/jobs.yaml` | Two new jobs with dimensions, four forces and opportunity scores; `story-5877-update-queue-lanes` added to `feature_context`. |
| `docs/product/journeys/story-5877-update-queue-lanes.yaml` | New journey `keep-working-while-one-thing-is-slow`, with emotional arc, shared artifacts and error paths. |
| `docs/product/personas/platform-operator.yaml` | Both new job IDs appended to `primary_jobs`. |

---

## Wave: DISCUSS / [REF] Handoff

**To**: `nw-solution-architect` (DESIGN) — full artifact set.
**To**: `nw-platform-architect` (DEVOPS) — KPIs only. Expected to be close to a no-op: no deployment,
infrastructure or new observability surface, though KPI-2 leans on `RefreshLog` retention.

Open for DESIGN:

1. **D8 — the bound's default and where it is configured.** The number comes from `RefreshLog`'s
   duration distribution, not from a guess. It must clear both the reported 77.7-minute run and
   #5687's 468-second pre-optimisation measurement.
2. **How lanes are realised.** N readers keyed by `UpdateType` over one channel, or one channel per
   type. The second makes D3 true by construction; the first makes it a filter that can be got wrong.
3. **How `WriteBackRound` becomes safe.** A lock inside the round, concurrent collections, or a
   per-execution staging area merged on `Leave`. The third is the only one that also removes
   last-stage-wins ambiguity between two executions of one round — and the only one that changes
   observable behaviour, so it needs a decision rather than a preference.
4. **Whether the lane is the right granularity**, or whether per-connection would serve D3's intent
   better — two Portfolios on two different trackers have no reason to wait for each other.
5. **Whether `RefreshLog` should carry queue wait.** Out of scope for this story; it would make KPI-1
   direct rather than inferred, and it is the kind of thing that is cheap now and expensive later.

Carried, not scoped — **raise as its own Bug before #5877 resolves**:

> `PortfolioUpdater.cs:176` runs `CleanUpOrphanedFeatures()` in a `finally`, so it runs after a failed
> refresh too, and `OrphanedFeatureCleanupService` hard-deletes any Feature matching
> `!IsParentFeature && !Portfolios.Any()`. The reporter's log shows "Cleaned up 36 orphaned features"
> immediately after the DNS failure. **Verification it needs**: whether `UpdateFeaturesForPortfolio` can
> leave a Feature detached from its Portfolio at any point a mid-refresh failure could land on. If it
> can, a transient network failure deletes real data, and the fix is ordering, not cleanup.
