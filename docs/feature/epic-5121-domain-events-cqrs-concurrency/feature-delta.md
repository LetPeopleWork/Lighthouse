<!-- markdownlint-disable MD024 -->
# Feature: epic-5121-domain-events-cqrs-concurrency

ADO Epic #5121 — **Target Architecture: Domain Events, CQRS-lite & Concurrency (ADR-027)**. Implements the accepted ADR-027 incrementally across five independent thin slices (the ADO children). Backend / cross-cutting. SSOT for all scope and guardrails: `docs/product/architecture/adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md` (D1–D8, the concurrency section, and the 2026-05-29 event-family addendum, families A–E).

ADO Epic: <https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5121>
Children: #5098, #5099, #5100, #5101, #5122.

Density mode: **lean** + `ask-intelligent`. Tier-2 expansion deferred to the orchestrator's wave-end menu.

## Wave: DISCUSS / [REF] Precursor inheritance

This epic implements the analysis-only precursor `target-architecture-4618` (ADR-027). The inherited commitments and open questions live in `docs/feature/target-architecture-4618/feature-delta.md`. Two precursor open questions are RESOLVED here:

- *Confirm the tokened-aggregate set before any concurrency slice scopes `SaveWithRetry`* → resolved as the 5 config roots (Team, Portfolio, WorkTrackingSystemConnection, RBAC: UserProfile/RbacGroupMapping/ApiKey, Delivery) — slice 03 / #5100.
- *Decide whether the seven modules become ArchUnitNET-enforced now or named-only* → resolved as ENFORCED, building on the #5098 harness — slice 04 / #5101.

The third (migration sequencing beyond the first slice) is resolved by the prioritization rationale below.

## Wave: DISCUSS / [REF] Persona ID

- **`config-admin`** (NEW persona, `docs/product/personas/config-admin.yaml`) — actor for #5100. The configuration administrator who edits Team / Portfolio / Connection / RBAC / Delivery aggregates. No existing persona file fit a config-editor (the four existing files are `flow-coach`, `delivery-lead-rte`, `product-owner`, `delivery-forecaster`; the `system-admin` / `team-admin` / `connector-admin` ids referenced in `jobs.yaml` have no backing files). `config-admin` aliases all of those admin roles.
- **`flow-coach`** (existing) — eventual beneficiary for #5122 (proactive reaction to stuck/blocked/transitioned work). The events emitted here unlock the proactive push the flow-coach currently has to go looking for.
- The three refactors (#5098, #5099, #5101) are `@infrastructure` — the "persona" is the Lighthouse developer / the CI build; no end-user persona.

## Wave: DISCUSS / [REF] JTBD (mixed framing)

Per the locked MIXED framing: pure zero-behaviour-change refactors are `infrastructure-only`; the two user-visible stories trace to real jobs in `docs/product/jobs.yaml`.

### User-visible jobs (added to jobs.yaml, 2026-05-30)

| Job id | Story | Persona | Importance / Satisfaction / Gap |
|---|---|---|---|
| `job-config-edit-no-silent-lost-update` | #5100 | `config-admin` | 4 / 1 / **3** |
| `job-react-proactively-to-workitem-change` | #5122 | `flow-coach` | 4 / 1 / **3** |

### Opportunity-score comparison (the two scored jobs)

Both score importance 4, satisfaction 1, **gap 3** — tied. Tie-break rationale for prioritization (see prioritization section): #5100 is INDEPENDENT of the dispatcher and closes a *correctness defect* (silent data loss) that is live today, so it can ship early and de-risks a trust issue; #5122 depends on #5098 and is *enabling* (the value compounds only once downstream reactions land), so it follows the seam. Equal gap, but #5100 carries lower dependency risk and a here-and-now correctness payoff.

### Infrastructure-only stories (no jobs.yaml entry; `infrastructure_rationale`)

- **#5098** — `infrastructure_rationale`: "Introduces the `IDomainEventDispatcher` seam and moves ONE reaction (portfolio metric-cache invalidation) behind it with zero production behaviour change. No user-observable difference; value is the test-observable seam (publish→handlers-fire gold test + ArchUnitNET green) that every later slice builds on. Walking skeleton."
- **#5099** — `infrastructure_rationale`: "Decouples the remaining hand-wired add/delete reactions (families A/B/C) onto handlers; constructors shrink (`TeamController.DeleteTeam` 9→≤5) and the refresh-log-cleanup gap on four delete paths is closed as an emergent consequence. Zero production behaviour change; value is test-observable (ctor-count + cleanup-gap integration tests)."
- **#5101** — `infrastructure_rationale`: "Names and enforces the seven module boundaries + the seam invariants via ArchUnitNET in a single assembly. Pure test-time architecture enforcement; no runtime or user surface. Value is CI-observable (the module-rule suite is RED on a forbidden dependency)."

## Wave: DISCUSS / [REF] Scope Assessment

**PASS (pre-split into 5 ADO children).** The epic is oversized by the standard heuristics — 5 stories, ≥3 bounded contexts/modules touched (WorkItems/Sync, Forecasting, Portfolio/Delivery, RBAC/Identity, Platform/Persistence), estimated >2 weeks total. It is ALREADY split into five INDEPENDENT thin end-to-end slices = the ADO children, each delivering verifiable value (test-observable for the refactors, user-visible for #5100/#5122). No re-split proposed.

**Carpaccio risk flagged**: #5099 (slice 02) is the one child that may exceed ≤1 day; it carries a built-in split valve (sever Family B — lifecycle/cleanup — into its own follow-up if the whole migration runs long). #5122 (slice 05) is slightly over at ~1.5 days but its three events share delta-derivation plumbing, so shipping together is cleaner than splitting. Neither warrants re-splitting now.

## Wave: DISCUSS / [REF] Journey

Lightweight journey coverage (focused journey + emotional arc + Gherkin) exists for the two user-visible stories only, in `docs/product/journeys/epic-5121-domain-events-cqrs-concurrency.yaml`:

- `config-edit-conflict-surfaces-as-409` (#5100) — emotional arc: uneasy → reassured-at-conflict → confident. The 409 refusal is the feature, not a failure.
- `workitem-change-published-as-domain-event` (#5122) — emotional arc: resigned-to-pull-only → hopeful → forward-looking. Ends at reliable emission; reactions are downstream.

### Developer-experience note for the refactors (#5098 / #5099 / #5101)

These get no journey — their "journey" is the developer's. **Before**: to react to an add/delete you edit a controller with 9 constructor injections (`TeamController.DeleteTeam`) or a 6-step service-locator pipeline (`PortfolioUpdater.Update` resolving 7 services via `GetRequiredService`). **After**: you add an `IDomainEventHandler<TEvent>` — the reaction is a small, isolated, independently-tested class; the controller/updater just publishes. The ArchUnitNET suite (#5101) makes the boundaries that keep this clean fail-fast in CI rather than erode silently.

## Wave: DISCUSS / [REF] Story Map

### Backbone (developer/admin activities, left-to-right)

| Establish the seam | Decouple existing reactions | Protect shared config | Guard the boundaries | Emit the new signals |
|---|---|---|---|---|
| #5098 dispatcher + 1st reaction | #5099 migrate A/B/C reactions | #5100 concurrency 409 | #5101 module ArchUnit rules | #5122 work-item events |

### Walking skeleton

**#5098 — `PortfolioFeaturesRefreshed`.** The thinnest end-to-end slice that proves the dispatcher seam on the busiest real reactor with zero behaviour change. ADR-027 names it the natural first slice. Every other slice (#5099, #5101, #5122) depends on it; #5100 is independent.

### Release slices (by outcome)

- **R0 (skeleton)**: #5098 — the seam exists and is trustworthy (gold test + ArchUnit green).
- **R1 (correctness)**: #5100 — no config edit is ever silently lost (USER-VISIBLE). Independent of R0; can ship in parallel.
- **R2 (enforcement)**: #5101 — the modular-monolith boundaries are guarded at test time.
- **R3 (decoupling)**: #5099 — new add/delete reactions become "add a handler"; cleanup-gap fixed.
- **R4 (proactive rail)**: #5122 — work-item change moments are reactable events (USER-VISIBLE; reactions downstream).

### Priority Rationale

Learning-leverage and dependency first:

1. **#5098 (P1, walking skeleton)** — highest-uncertainty / highest-leverage. The whole epic's premise (a ~30-line in-house dispatcher dispatched after-commit, throwing-handler recovery via re-sync, no outbox) is unproven until this lands. Tie-break: walking skeleton wins.
2. **#5100 (P2)** — INDEPENDENT of the dispatcher; closes a live correctness defect (silent lost update). High value, no dependency risk, here-and-now payoff. Can run in parallel with #5098.
3. **#5101 (P3)** — depends on #5098's ArchUnit harness; should precede or accompany #5099's cross-module Forecasting handlers so the migration lands inside enforced boundaries.
4. **#5099 (P4)** — depends on the seam having earned trust (#5098) and benefits from boundaries being enforced first (#5101). Carpaccio risk; split valve ready.
5. **#5122 (P5)** — depends on #5098; the enabling rail whose value compounds only once downstream reactions (#4754/#4755) land, so it follows once the seam is proven.

## Wave: DISCUSS / [REF] Shared Artifacts

| Artifact | Source of truth | Consumers | Integration risk |
|---|---|---|---|
| `concurrency_token` (vT) | the tokened aggregate row (Postgres `xmin` / SQLite rowversion-style; DESIGN picks exact mechanism) | config GET payload, config save request, 409 comparison, CLI/MCP config-edit methods | HIGH — if read-token ≠ save-token check, the 409 cannot be raised and the silent lost update returns |
| `dispatcher_seam` | `IDomainEventDispatcher` / `IDomainEventHandler<TEvent>` (#5098) | #5099 reactions, #5122 producers, future reaction handlers | HIGH — anything publishing OUTSIDE the seam loses after-commit / throwing-handler-recovery / no-`GetRequiredService` guarantees |
| `sync_delta` | `WorkItemService.SyncStateTransitions` | #5122 event publication, existing read-time projections (time-in-state, aging-pace, blocked) | MEDIUM — events must derive from the same delta the projections use, else emission and on-screen signal disagree |
| `staleness_threshold` | Team/Portfolio threshold config (`job-team-admin-tune-staleness`) | `WorkItemBecameStale` payload, existing staleness badge | LOW — reused, single source already |

## Wave: DISCUSS / [REF] Stories

Each story below carries its Elevator Pitch, embedded testable AC (echoing the ADR enforcement table where applicable), and its `job_id` / `infrastructure_rationale`. Refactor stories are `@infrastructure` and frame "After → sees" as concrete test/CI output.

### US-5098 — In-process domain-event dispatcher + first slice (`PortfolioFeaturesRefreshed`)

`@infrastructure` · **job_id: infrastructure-only** · Slice 01 · WALKING SKELETON

**Problem**: A Lighthouse developer reacting to portfolio refresh must edit `PortfolioUpdater.Update`, a 6-step imperative pipeline that resolves 7 services via `IServiceProvider.GetRequiredService` (service-locator) and hand-calls metric-cache invalidation inline — the scattered "remembered invalidation" that is the #4778 bug class.

**Who**: Lighthouse developer | extending refresh reactions | wants reactions decoupled and testable in isolation.

**Solution**: A lightweight in-house `IDomainEventDispatcher` + `IDomainEventHandler<TEvent>` seam (mirrors `IUpdateQueueService`); publish `PortfolioFeaturesRefreshed` after-commit; move metric-cache invalidation into a handler. Ship the ArchUnitNET harness + the publish→handlers-fire gold test. Zero production behaviour change.

#### Elevator Pitch

- Before: to change how portfolio refresh reacts, a developer edits a 7-service-locator, 6-step `PortfolioUpdater.Update` and remembers to call invalidation inline (the #4778 smell).
- After: run `dotnet test` → the gold test output shows `publish PortfolioFeaturesRefreshed → all handlers fired; throwing handler caught, committed fact preserved, recovered on re-sync`, and the ArchUnitNET rules report `Services.Implementation ↛ API: PASS` and `dispatcher forbids GetRequiredService: PASS`. Invalidation now lives in its own handler class.
- Decision enabled: the developer decides to migrate the remaining reactions (#5099) and emit work-item events (#5122) ONTO this proven seam, rather than continuing to hand-wire.

#### Domain Examples

1. Happy path — Publishing `PortfolioFeaturesRefreshed` after a portfolio refresh invokes the `InvalidatePortfolioMetrics` handler; cached metrics for portfolio "Zenith-Platform" are invalidated exactly as the old inline call did.
2. Resilience — A second registered handler throws; the committed portfolio-refresh fact for "Zenith-Platform" is preserved (not rolled back), the first handler still ran, and the next scheduled re-sync re-drives the failed reaction.
3. Boundary — A developer adds `GetRequiredService` inside a handler; the ArchUnitNET rule turns the build RED before merge.

#### Acceptance Criteria

- [ ] Given a portfolio refresh commits, when `PortfolioFeaturesRefreshed` is published, then every registered handler is invoked exactly once and the metric-cache-invalidation handler invalidates the same caches the prior inline call did (golden equivalence).
- [ ] Given a registered handler throws, when the event is published, then the committed refresh fact is preserved, other handlers still run, and a subsequent re-sync re-drives the failed reaction.
- [ ] The dispatcher/handler types contain no `IServiceProvider.GetRequiredService`; `Services.Implementation` has no dependency on `API` — both enforced by ArchUnitNET (RED if violated).
- [ ] No-regression: the full backend suite stays green; `PortfolioUpdater.Update`'s observable outcome is unchanged.

#### Outcome KPI

- Who: the `PortfolioUpdater.Update` method · Does what: stops resolving services via the service locator · By how much: `GetRequiredService` resolves **7 → 0** in that method · Measured by: static grep / ArchUnitNET rule · Baseline: 7.

---

### US-5099 — Migrate remaining add/delete reactions to domain-event handlers

`@infrastructure` · **job_id: infrastructure-only** · Slice 02 · CARPACCIO RISK (split valve: Family B)

**Problem**: New add/delete reactions mean editing fat controllers — `TeamController` injects 9 collaborators and `DeleteTeam` hand-wires `RemoveRefreshLogsForEntity` + a `foreach TriggerUpdate` loop; `TeamDataService` reaches across module lines to call `forecastUpdater.TriggerUpdate`. Four of six delete paths silently SKIP refresh-log cleanup.

**Who**: Lighthouse developer | adding or changing lifecycle reactions | wants thin controllers and no cross-module reach-through.

**Solution**: Migrate three families (A pipeline, B lifecycle, C cross-aggregate) onto handlers. A unified `*Deleted` handler also closes the refresh-log-cleanup gap on the four paths that lacked it. Zero production behaviour change.

#### Elevator Pitch

- Before: `TeamController` constructor lists 9 injected services; `DeleteTeam` hand-wires cleanup + re-trigger; deleting a Connection/User/ApiKey/BlackoutPeriod leaves orphaned refresh logs.
- After: read the `TeamController` constructor → it now lists ≤5 injections; run `dotnet test` → integration tests report `refresh logs cleaned up on Delivery/Connection/User/ApiKey/BlackoutPeriod delete: PASS` (previously absent) and `TeamDataService no longer calls forecastUpdater directly: PASS`.
- Decision enabled: the developer adds future add/delete reactions as handlers, confident the controller stays thin and no delete path silently skips cleanup.

#### Domain Examples

1. Happy path — Deleting Team "Zenith" publishes `TeamDeleted`; the unified handler removes its refresh logs and a `TeamDataRefreshed` handler re-triggers dependent forecasts; `TeamController` ctor is ≤5 injections.
2. Gap-fix — Deleting a WorkTrackingSystemConnection (previously NOT cleaned up) now removes its refresh logs via the same `*Deleted` handler.
3. Cross-module — `TeamDataService` no longer calls `forecastUpdater.TriggerUpdate` directly; a `TeamDataRefreshed` handler in Forecasting performs the trigger.

#### Acceptance Criteria

- [ ] `TeamController` constructor injection count drops 9 → ≤5; `DeleteTeam` publishes events instead of hand-wiring cleanup + the trigger loop.
- [ ] A unified `*Deleted` handler removes refresh logs for ALL six delete paths; integration tests assert cleanup on the four previously-missing paths.
- [ ] `TeamDataService` no longer calls `forecastUpdater.TriggerUpdate`; a Forecasting-module `TeamDataRefreshed` handler does.
- [ ] No-regression: every existing add/delete reaction produces the same observable outcome; full suite green.

#### Outcome KPI

- Who: `TeamController.DeleteTeam` · Does what: stops hand-wiring reactions · By how much: constructor injections **9 → ≤5**, and refresh-log-cleanup coverage **2/6 → 6/6** delete paths · Measured by: ctor arity + integration tests · Baseline: 9 injections, 2/6 paths.

---

### US-5100 — Optimistic-concurrency tokens (HTTP 409) on config aggregates

**USER-VISIBLE** · **job_id: `job-config-edit-no-silent-lost-update`** · Persona `config-admin` · Slice 03

**Problem**: Anita and Bruno both administer Team "Zenith". Anita opens its settings, changes the staleness threshold to 5, and saves. Bruno — who loaded the page before her save — saves a forecast-filter change a minute later. Today his stale write silently overwrites Anita's threshold; her change vanishes with no warning to either of them. There is no concurrency token anywhere in Lighthouse, and the blanket `SaveWithRetry` reloads-and-retries (last-writer-wins), actively hiding the loss.

**Who**: `config-admin` | editing a shared config aggregate (Team/Portfolio/Connection/RBAC/Delivery) that a colleague may also be editing | wants no silent lost update and a clear path to recover from a conflict.

**Solution**: Optimistic-concurrency tokens on the 5 human-edited config roots; a stale save returns HTTP 409 with no write; `SaveWithRetry` is scoped to bypass tokened saves so it cannot swallow the conflict. Read-your-writes holds. High-churn sync entities are never tokened.

#### Elevator Pitch

- Before: Bruno saves a stale edit to Team "Zenith" and silently overwrites Anita's change; neither is warned; her threshold of 5 is gone.
- After: Bruno's `PUT /api/.../teams/{id}` against the stale token returns **HTTP 409 Conflict** with body indicating "this changed since you loaded it — reload and re-apply"; the settings page shows "Someone else changed this Team since you loaded it. Your edit was NOT saved. [Reload current values]"; after reloading he sees Anita's 5-day threshold and re-applies his filter change, which then saves.
- Decision enabled: Bruno decides to reload and merge his change intentionally onto Anita's, instead of unknowingly destroying it — no config change is ever silently lost.

#### Domain Examples

1. Happy path — Anita saves Team "Zenith" threshold 7→5 against the current token; it succeeds, the token advances, and reloading shows 5 (read-your-writes).
2. Conflict + recovery — Bruno saves against the stale token → HTTP 409, no write, Anita's 5 preserved; Bruno reloads, re-applies his forecast-filter edit, save succeeds.
3. RBAC boundary — Two RBAC admins edit the same group-to-role mapping; the second stale save returns 409 THROUGH `IRbacAdministrationService`; an unauthorized actor on the same edit still gets 403 (not 409).
4. Sync isolation — The sync engine writes WorkItem/Feature/WorkItemStateTransition rows for "Zenith" repeatedly; no token is required and no 409 is ever raised; throughput unaffected.

#### UAT Scenarios (BDD)

See `journeys/epic-5121-domain-events-cqrs-concurrency.yaml`, journey `config-edit-conflict-surfaces-as-409`, steps 1–4 (loading carries the token; first save succeeds and advances it; stale save → 409 → reload → re-apply; RBAC-aggregate edit gets the same protection through the RBAC port; `@property` sync entities never blocked).

#### Acceptance Criteria

- [ ] Given Anita has saved Team "Zenith" threshold 5 and Bruno still holds the older token, when Bruno saves against it, then HTTP 409 is returned, no change is written, and Anita's 5 is preserved (the ADR "two stale writes → second gets 409" enforcement row, for Team, Portfolio, and Connection).
- [ ] Given Bruno received a 409, when he reloads and re-applies, then the save succeeds against the current token and reloading shows both Anita's and Bruno's changes.
- [ ] Given a stale RBAC group-mapping edit, when saved, then 409 is returned THROUGH `IRbacAdministrationService`; an unauthorized actor on the same edit still receives 403.
- [ ] @property — Given the sync engine writes WorkItem/Feature/WorkItemStateTransition, then no token is required, no 409 is raised, and sync throughput is unaffected.
- [ ] Given a tokened-aggregate stale save, then `SaveWithRetry` does NOT reload-and-retry it (short-circuits to 409); non-tokened saves still benefit from the existing retry.

#### Outcome KPI

- Who: `config-admin`s editing shared config · Does what: stop silently losing each other's edits · By how much: **0 silent lost updates** on config edits (every concurrent stale edit surfaces as a 409 instead) · Measured by: integration test coverage of the 5 roots + a production two-session dogfood + 409-rate observability on config-edit endpoints · Baseline: silent last-writer-wins (0% conflict surfacing today). Guardrail: config-edit 409s must NOT appear on single-admin edits, and sync-entity throughput must not regress.

---

### US-5101 — Enforce the seven module boundaries with ArchUnitNET

`@infrastructure` · **job_id: infrastructure-only** · Slice 04

**Problem**: The seven logical modules exist only as namespace folders; nothing stops a developer introducing an illegal cross-module dependency (e.g. Metrics reaching into Sync internals) or re-introducing the service-locator in the dispatcher — the boundaries erode silently and a clean local build does not catch it.

**Who**: Lighthouse developer | working across module folders | wants the modular-monolith boundaries to fail fast in CI, not rot.

**Solution**: Name the seven modules as ArchUnitNET layers in a single assembly and enforce layered-dependency + seam-invariant rules, building on the #5098 harness.

#### Elevator Pitch

- Before: a developer can add a dependency from Metrics into Sync internals and a clean `dotnet build` says nothing; the boundary erodes silently.
- After: run `dotnet test` → the ArchUnitNET suite reports `seven module boundaries: PASS` (and turns RED listing the offending type→type edge the moment a forbidden dependency is introduced), plus `Services.Implementation ↛ API: PASS` and `dispatcher forbids GetRequiredService: PASS`.
- Decision enabled: the developer (and reviewers) treat a RED module rule as a blocking signal to refactor the dependency, keeping the monolith modular without a physical assembly split.

#### Domain Examples

1. Happy path — `dotnet test` runs the seven-module layered-dependency suite green on the current codebase.
2. Boundary — A throwaway branch adds a dependency from Metrics/Time-in-state into WorkItems/Sync internals; the module rule fails RED naming the edge.
3. Seam — A developer adds `GetRequiredService` to a handler; the named seam rule fails RED.

#### Acceptance Criteria

- [ ] An ArchUnitNET test defines all seven modules as layers and asserts no illegal cross-module dependency; RED on a forbidden edge.
- [ ] Named rule `Services.Implementation ↛ API` passes and is RED if violated.
- [ ] Named rule "dispatcher + handler types forbid `GetRequiredService`" passes and is RED if violated.
- [ ] The suite runs in CI as part of `dotnet test`; a deliberately-introduced violation fails the build (validated locally before merge).

#### Outcome KPI

- Who: the backend codebase · Does what: gains enforced module boundaries · By how much: **7 module layers + 2 seam invariants** enforced (RED on violation), up from **0** enforced rules · Measured by: ArchUnitNET rule count in CI · Baseline: 0 (boundaries are namespace-folder convention only).

---

### US-5122 — Emit work-item domain events on state transitions

**USER-VISIBLE** · **job_id: `job-react-proactively-to-workitem-change`** · Persona `flow-coach` · Slice 05 · GUARDRAIL: transport only (D7)

**Problem**: Today a flow-coach only learns an item went stale, got blocked, or changed state when they next open a chart (time-in-state, aging-pace, blocked, cumulative-state-time) — these signals are read-time projections only. Nothing fires at the moment of change, so every proactive reaction (a push, a notification) is blocked on someone opening a screen, and the early-action window is often missed.

**Who**: `flow-coach` (eventual beneficiary) | wants the tool to react proactively to stuck/blocked/transitioned work | currently has to go looking.

**Solution**: Off the `SyncStateTransitions` sync delta, publish `WorkItemTransitioned` / `WorkItemBecameStale` / `WorkItemBlocked` as in-process domain events through the #5098 dispatcher (after-commit). This delivers the EMISSION + the seam contract; the reactions (SignalR/notifications/webhooks, #4754/#4755) are downstream and OUT of scope. GUARDRAIL: transport only — NOT Event Sourcing; the transition history stays a projection; any persisted sink is separate (#5017-style).

#### Elevator Pitch

- Before: when ZEN-104 crosses 11 days in Review or ZEN-090 gets blocked, nothing happens until someone opens the aging chart days later.
- After: run the sync (or `dotnet test` with a diagnostic handler registered) → the moment the sync delta shows the change, a `WorkItemTransitioned` / `WorkItemBecameStale` / `WorkItemBlocked` event is published and the test handler's log shows it fired exactly once (and did NOT re-fire on the next unchanged sync).
- Decision enabled: the team (via a downstream handler added later) can be nudged proactively at the moment work goes stale/blocked — the rail to build #4754/#4755 on now exists, proven trustworthy.

#### Domain Examples

1. Happy path — ZEN-118 moves In Progress→Review in the sync delta → one `WorkItemTransitioned` published from "In Progress" to "Review", after commit.
2. Edge / no-double-fire — ZEN-104 crosses the 7-day staleness threshold → one `WorkItemBecameStale`; the next sync where it remains stale publishes none.
3. Resilience — Two handlers registered for `WorkItemTransitioned`, the second throws; the first still runs, the committed fact survives, recovery on next re-sync (inherits #5098's gold-test contract).

#### UAT Scenarios (BDD)

See `journeys/epic-5121-domain-events-cqrs-concurrency.yaml`, journey `workitem-change-published-as-domain-event`, steps 1–3 (delta→transitioned event, unchanged→no event; threshold-crossing→became-stale once, block-transition→blocked once; publish fires all handlers and survives a throwing one).

#### Acceptance Criteria

- [ ] A state change in the sync delta publishes one `WorkItemTransitioned` with correct from/to; an unchanged item publishes none.
- [ ] A staleness-threshold crossing publishes one `WorkItemBecameStale` referencing the threshold; a later sync where it remains stale publishes none.
- [ ] A block transition publishes one `WorkItemBlocked`; later syncs while still blocked publish none.
- [ ] Events publish AFTER the sync data is committed; a throwing handler does not lose the committed fact and recovers on next re-sync.
- [ ] Guardrail — the dispatcher persists nothing for these events; the `WorkItemStateTransition` table is unchanged by this slice (still a projection, not a sink).

#### Outcome KPI

- Who: the work-item sync · Does what: emits the three change-moment events reliably · By how much: **3 event types** published off the delta with **0 false re-fires** on unchanged items and **0 lost facts** under a throwing handler · Measured by: integration tests + a diagnostic handler observed on the production instance · Baseline: 0 events (signals are read-time projections only). Guardrail: no sync throughput regression; `WorkItemStateTransition` row count/shape unchanged (no ES drift). Mutation kill-rate ≥80% (CLAUDE.md) on the new dispatcher/handler/event-emission code.

## Wave: DISCUSS / [REF] Cross-Cutting Impact Checklist

CLAUDE.md DISCUSS gate (DoR Item 7). All three surfaces recorded per story; "N/A" is explicit with evidence.

### RBAC

- **#5100** — DIRECTLY interacts with authorization aggregates. Tokens are added to RBAC config roots (UserProfile/RbacGroupMapping/ApiKey). The 409 path on RBAC aggregates MUST flow through `IRbacAdministrationService` (per Architecture / ADR-001 RBAC port boundary); UI gating derives from `useRbac()`. Roles that gate config edits: System Admin (full config), Team/Portfolio Admin (their scope) — unchanged; the 409 is a concurrency outcome orthogonal to the existing 403 authorization decision (an unauthorized actor still gets 403, an authorized stale writer 409). No new authz surface is created.
- **#5098 / #5099 / #5101** — N/A, because these are decoupling/enforcement refactors with no authorization effect. #5099 touches `TeamController.DeleteTeam` but only re-routes its reactions through the dispatcher; the existing authorization on the delete endpoint is unchanged. #5101 enforces module boundaries (a test-time concern), not authorization.
- **#5122** — N/A, because event emission is a machine-side sync concern with no authorization decision; the events carry no new permission semantics. (Downstream reactions #4754/#4755 will need their own RBAC review — flagged forward, OUT of scope here.)

### Lighthouse-Clients (CLI + MCP)

- **#5100** — REQUIRES a matching client update. Config-edit responses gain a **409** path; the CLI + MCP client methods that PUT/POST config (Team/Portfolio/Connection, and any RBAC config they expose) must handle 409 DISTINCTLY — surface "this changed since you loaded it; re-fetch and retry" rather than a generic error. This changes an EXISTING contract (adds a status to existing endpoints), not a new endpoint, so no `FEATURE_REQUIRES_SERVER_NEWER_THAN` bump is strictly required for an endpoint; clients must tolerate older servers that never emit 409. **Version-gating rule recorded for completeness**: were any NEW endpoint added, the wrapping client method must pre-check the server version and fail with a clear "upgrade Lighthouse" error, pinned strictly newer than the last released Lighthouse version (bump the clients' `FEATURE_REQUIRES_SERVER_NEWER_THAN` baseline) — not applicable here because no new endpoint is added.
- **#5122** — N/A for THIS epic, because the events are in-process only; no new API endpoint, no contract change. (If/when downstream reactions expose a webhook or notification endpoint, THAT work must version-gate per the rule above — forward-pointer, OUT of scope.)
- **#5098 / #5099 / #5101** — N/A, because no API contract changes (internal seam, internal reaction migration, test-time rules).

### Website

- **#5098 / #5099 / #5100 / #5101** — N/A, because these are internal architecture / correctness changes with no marketable public-facing surface.
- **#5122** — N/A for this epic. Forward-pointer: the eventual proactive-notification capability that these events unlock (downstream #4754/#4755) could be a future premium surface worth marketing on the website — to be decided when those reactions are built, NOT here.

## Wave: DISCUSS / [REF] Outcome KPIs (consolidated)

### Objective

Lighthouse's add/delete reactions, config-edit safety, and work-item signals run on a clean, enforced event seam — decoupling shrinks, no config change is ever silently lost, and change-moments become reactable — without changing the single-instance modular-monolith architecture or the real-load behaviour.

### KPI table

| # | Who | Does what | By how much | Baseline | Measured by | Type |
|---|---|---|---|---|---|---|
| 1 | `PortfolioUpdater.Update` | stops using the service locator | `GetRequiredService` **7 → 0** | 7 | ArchUnit / grep | Leading |
| 2 | `TeamController.DeleteTeam` | stops hand-wiring reactions | ctor injections **9 → ≤5**; cleanup **2/6 → 6/6** delete paths | 9; 2/6 | ctor arity + integration tests | Leading |
| 3 | the backend codebase | gains enforced boundaries | **7 module layers + 2 seam invariants** enforced (from 0) | 0 | ArchUnitNET rule count | Leading |
| 4 | `config-admin`s | stop silently losing concurrent edits | **0 silent lost updates** (all surface as 409) | last-writer-wins | integration + prod 409-rate observability | Leading |
| 5 | the work-item sync | emits change-moment events reliably | **3 event types**, **0 false re-fires**, **0 lost facts** | 0 events | integration + diagnostic handler | Leading |
| 6 | the new dispatcher/handler/token/event code | is well-tested | **mutation kill-rate ≥ 80%** (CLAUDE.md) | n/a (new code) | Stryker.NET | Guardrail |

### Guardrail metrics (must NOT degrade)

- Sync throughput on high-churn entities (WorkItem/Feature/transition) — unaffected by the token mechanism (#5100) and by event emission (#5122).
- `WorkItemStateTransition` row count/shape — unchanged (no Event-Sourcing drift, #5122 / D7).
- Config-edit 409s must NOT appear on single-admin edits (false-positive guardrail, #5100).
- Full backend suite green; zero new SonarCloud violations; `TreatWarningsAsErrors` clean (CLAUDE.md quality gates).

## Wave: DISCUSS / [REF] DoR Validation

9-item hard gate (Item 7 = the cross-cutting checklist above). Evidence per item, per story.

| DoR Item | US-5098 | US-5099 | US-5100 | US-5101 | US-5122 |
|---|---|---|---|---|---|
| 1 Problem statement (domain language) | PASS | PASS | PASS | PASS | PASS |
| 2 Persona with specifics | PASS (developer/CI; infra) | PASS (developer/CI; infra) | PASS (`config-admin`) | PASS (developer/CI; infra) | PASS (`flow-coach`) |
| 3 ≥3 domain examples, real data | PASS (3, Zenith-Platform) | PASS (3) | PASS (4, Anita/Bruno/ZEN) | PASS (3) | PASS (3, ZEN-118/104/090) |
| 4 UAT Given/When/Then (3–7) | PASS (gold-test scenarios) | PASS | PASS (journey 4 steps, 6 scenarios) | PASS | PASS (journey 3 steps, 6 scenarios) |
| 5 AC derived from UAT | PASS | PASS | PASS | PASS | PASS |
| 6 Right-sized (1–3 days, 3–7 scen.) | PASS (~1d) | PASS w/ split valve (~1–1.5d) | PASS (~2d, single outcome) | PASS (~1d) | PASS (~1.5d) |
| 7 Technical notes / cross-cutting (RBAC, Clients, Website) | PASS (all N/A w/ evidence) | PASS (all N/A w/ evidence) | PASS (RBAC + Clients = change; Website N/A) | PASS (all N/A) | PASS (all N/A; fwd-pointers) |
| 8 Dependencies resolved/tracked | PASS (none; skeleton) | PASS (#5098; #5101 ordering flagged) | PASS (independent; token-set confirmed) | PASS (#5098 harness) | PASS (#5098) |
| 9 Outcome KPIs with measurable targets | PASS (KPI 1) | PASS (KPI 2) | PASS (KPI 4) | PASS (KPI 3) | PASS (KPI 5 + mutation guardrail) |

**DoR Status: PASSED (all 5 stories, all 9 items).**

Elevator-Pitch gate (review Dimension 0): all 5 stories carry a Before / After / Decision-enabled triplet. The two user-visible stories (#5100, #5122) reference real user-invocable entry points (config-edit PUT / sync) with observable output (409 + recovery affordance / published event). The three `@infrastructure` stories frame "After → sees" as concrete `dotnet test` / CI output and are labelled `@infrastructure`. **Slice-composition gate: PASS** — no slice is a pure-infra dead-end; the epic carries two user-visible value stories (#5100 R1, #5122 R4), and each refactor slice is independently shippable as a test-observable precursor.

## Wave: DISCUSS / [REF] Wave Decisions

### Key Decisions

- **D-5121-1** — MIXED JTBD framing applied: #5098/#5099/#5101 are `infrastructure-only` (with `infrastructure_rationale`); #5100/#5122 trace to two NEW real jobs added to `jobs.yaml`.
- **D-5121-2** — NEW persona `config-admin` created (no existing file fit a config-editor; `system-admin`/`team-admin`/`connector-admin` in jobs.yaml are file-less). #5122 reuses existing `flow-coach`.
- **D-5121-3** — Tokened-aggregate set CONFIRMED as the 5 config roots (Team, Portfolio, WorkTrackingSystemConnection, RBAC: UserProfile/RbacGroupMapping/ApiKey, Delivery) — resolves precursor open question 1.
- **D-5121-4** — Seven modules ENFORCED via ArchUnitNET (not named-only) — resolves precursor open question 2.
- **D-5121-5** — Prioritization: #5098 (skeleton) → #5100 (independent, parallel) → #5101 → #5099 → #5122. #5101 sequenced before #5099's cross-module families.
- **D-5121-6** — Carpaccio: #5099 carries a Family-B split valve; #5122 stays whole (~1.5d, shared plumbing). #5100 stays whole (~2d, single user outcome, not splittable by root without a confusing partial guarantee).
- **D-5121-7** — GUARDRAIL re-affirmed: the dispatcher is TRANSPORT only (D2/D7) — not Event Sourcing, no outbox, recovery via re-sync; persisted history is a separate sink (#5017-style), never the dispatcher.

### Requirements Summary

5 user stories (2 user-visible, 3 `@infrastructure`), 5 slice briefs, 1 lightweight journey (2 user-visible journeys + DX note for refactors), 2 new jobs, 1 new persona, 6 consolidated outcome KPIs + guardrails.

### Constraints (inherited / cross-cutting)

- Single-instance, single-writer modular monolith preserved (ADR-027 D4); no broker, no microservices, no ES (D7).
- OOP / hexagonal; RBAC via `IRbacAdministrationService`, UI gating via `useRbac()`; immutability and no-banned-comments conventions (CLAUDE.md).
- EF migrations (#5100 token columns) via the existing `CreateMigration` PowerShell script across all providers — NOT `dotnet ef migrations add` directly.
- Provider-switched persistence (SQLite ↔ Postgres) must express the token equivalently without forking (#5100).

### Upstream Changes

- `docs/product/jobs.yaml` — 2 jobs added; `feature_context` + `updated:` bumped to 2026-05-30.
- `docs/product/personas/config-admin.yaml` — NEW.
- `docs/product/journeys/epic-5121-domain-events-cqrs-concurrency.yaml` — NEW (2 user-visible journeys + embedded Gherkin).

### Risks / Open Questions for the orchestrator (pre-DESIGN)

1. **#5099 ↔ #5101 ordering** (MEDIUM) — if #5099 Family A/C handlers introduce a cross-module Forecasting dependency, #5101 should land first to keep them inside enforced boundaries. Prioritization already sequences #5101 before #5099; confirm acceptable.
2. **#5100 `SaveWithRetry` scoping** (HIGH, technical) — the careful change ADR-027 flags; getting it wrong re-hides the lost update. Bounded by the two-stale-writes gold test; surfaced as a DESIGN risk, not a DISCUSS blocker.
3. **#5100 provider-parity** (MEDIUM) — SQLite rowversion-style token must equal Postgres `xmin` semantics without forking persistence; a DESIGN spike candidate.
4. **#5099 carpaccio** (LOW) — may exceed 1 day; Family-B split valve ready, no action needed unless effort confirms.
