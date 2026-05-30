# Evolution: epic-5121-domain-events-cqrs-concurrency

- **Date finalized**: 2026-05-30
- **Parent Epic**: #5121 — Target Architecture: Domain Events, CQRS-lite & Concurrency (implements ADR-027)
- **ADO**: Epic 5121; children #5098, #5101, #5100, #5099, #5122 all delivered (Resolved). #5103-style notional scope trimmed (see D-B).
- **Status**: Shipped to `main`, CI-green across all five slices. Mutation gate met on both stacks.
- **Workspace (history)**: `docs/feature/epic-5121-domain-events-cqrs-concurrency/`
- **SSOT**: `docs/product/architecture/adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md`

## What shipped

The accepted ADR-027 target architecture, delivered incrementally as five
independent thin slices. The Lighthouse modular monolith now reacts to
add/delete/refresh events over a clean in-process seam, refuses silent
lost-updates on shared config edits, enforces its module boundaries in CI, and
emits work-item change-moments as domain events — all without changing the
single-instance, single-writer, no-broker architecture.

- **In-process domain-event dispatcher** (`IDomainEventDispatcher` /
  `IDomainEventHandler<TEvent>`) — publishes *after commit*, resolves handlers
  from a fresh scope via `GetServices` (not `GetRequiredService`), runs each
  handler log-and-continue so one throwing handler never loses the committed
  fact, and recovers failed reactions on the next re-sync. **Not** Event
  Sourcing, no outbox, no broker (D7).
- **Reactions migrated onto the seam** — portfolio metric-cache invalidation
  (the walking-skeleton reaction), team-data-refreshed forecast re-trigger, and
  team-deleted refresh-log cleanup now live in small, independently-tested
  handler classes instead of fat controllers / service-locator pipelines.
- **Optimistic concurrency on the human-edited config roots** — an app-managed
  `Guid ConcurrencyToken` (`IConcurrencyTokenEntity`) on Team / Portfolio /
  Connection / RBAC config / Delivery. A stale save returns **HTTP 409**
  (`concurrency-conflict` ProblemDetails) with no write; the front end surfaces
  a "someone else changed this — reload" affordance with in-flight save
  coalescing; the Lighthouse CLI/MCP clients map 409 distinctly. High-churn sync
  entities (WorkItem/Feature/transition) are never tokened.
- **Seven module boundaries + two seam invariants enforced** via TngTech
  ArchUnitNET as part of `dotnet test` — RED on a forbidden cross-module edge or
  a `GetRequiredService` re-introduction in the dispatcher.
- **Three work-item domain events** off the `SyncStateTransitions` delta —
  `WorkItemTransitioned`, `WorkItemBlocked`, `WorkItemBecameStale` — published
  after-commit, each fired exactly once per change with no false re-fire on an
  unchanged item. Emission only; reactions (#4754/#4755) are downstream and out
  of scope. The `WorkItemStateTransition` table stays a projection (no ES drift).

## Slices (delivery order)

| Slice | Story | Scope |
|-------|-------|-------|
| 01 | #5098 | Dispatcher seam + first reaction (`PortfolioFeaturesRefreshed`); ArchUnit harness + publish→handlers-fire gold test. Walking skeleton. |
| 02 | #5101 | Seven-module ArchUnitNET boundary rules + seam invariants (sequenced before #5099 so its cross-module handlers land inside enforced boundaries). Precursor DTO relocation out of `API` + two dependency-cycle breaks. |
| 03 | #5100 | Optimistic-concurrency tokens, HTTP 409, `SaveWithRetry` scoping; FE 409 affordance; clients 409 mapping. |
| 04 | #5099 | Migrate remaining add/delete reactions to handlers (Family B scoped to team-delete; see D-B). |
| 05 | #5122 | Work-item domain events on state transitions (transitioned / blocked / became-stale). |

> The DISCUSS prioritization proposed #5098 → #5100 → #5101 → #5099 → #5122; the
> ArchUnit harness (#5101) was pulled forward to slice 02 so #5099's cross-module
> forecasting handlers were written under enforced boundaries from the start.

## Key decisions

Full decision log (D-5121-1..7 + the slice ADRs) lives in the workspace
`feature-delta.md` and ADR-027. The load-bearing ones:

- **Transport, not Event Sourcing (D7)** — the dispatcher carries facts to
  in-process reactions and nothing more. Durability/history, if ever needed, is a
  separate sink, never the dispatcher. Recovery is "re-sync re-drives it", which
  the single-writer model makes safe.
- **App-managed `Guid` token, not provider `xmin`/rowversion** — a single
  `ConcurrencyToken` column expressed identically on SQLite and Postgres via
  `.IsConcurrencyToken()`, avoiding a provider fork. EF `OriginalValue` is wired
  from the client-supplied token so the comparison happens in the UPDATE.
- **Token advances ONLY on the human-edit path** — the load-bearing correction
  (see lessons). System/sync saves must not churn the token or they manufacture
  spurious 409s.
- **`SaveWithRetry` short-circuits tokened saves** — the blanket reload-and-retry
  that *hid* the lost update is scoped out for token-bearing entities
  (`when (… && !InvolvesConcurrencyTokenEntity(ex))`), so a real conflict surfaces
  as 409 while non-tokened saves keep the retry.
- **ArchUnitNET adopted as the arch-test mechanism** — replaced the bespoke
  reflection harness; the seven modules become named layers in one assembly.
- **D-B — Family-B scope trimmed to team-delete.** The DISCUSS premise of a
  "4-path refresh-log cleanup gap" turned out notional: no refresh logs exist for
  the other delete paths (Connection/User/ApiKey/BlackoutPeriod), so there was
  nothing to clean. Scope was reduced to the real case (team-delete) on user
  confirmation rather than building handlers for a gap that does not exist.

## Mutation testing

Feature-scoped Stryker (whole-file mutate + test-case-filter on the backend per
the 2026-05-26 `{a..b}`-line-range defect; TS line-ranges on the frontend).
Report: workspace `deliver/mutation/mutation-report.md`.

- **Backend feature surface: 85.71%** (18/21; baseline 61.90%). The gap-closing
  pass added a dedicated `ConcurrencyConflictExceptionFilterTest` — the filter
  had no unit test — driving `OnException` directly and asserting 409 /
  ProblemDetails / `code` / `ExceptionHandled` against the production
  `ConflictCode` constant (no hardcoded oracle). The 3 survivors are all
  log-message strings → presentational.
- **Frontend feature surface: 80.52%** (124/154; baseline 16.23%). The flagship
  `useModifySettings` hook — auto-save, optimistic concurrency, token-chaining,
  conflict recovery — started at **1.77%**: the pre-existing tests covered only
  the manual `handleSave` path, never `dispatchSave`, the auto-save effect,
  `reloadAfterConflict`, or `retry`. +16 tests raised it to 78.8% (scoped total
  80.52%). The 30 survivors are classified equivalent (monotonic-counter
  direction, `isLatest()` stale-request guards under the hook's own
  serialization, React dep-arrays/cleanup, optional-chaining on always-defined
  refs) or real-but-impractical (4 — see lessons).
- **Big-file exclusion (qualitative, flagged).** `LighthouseAppContext.cs` and
  `WorkItemService.cs` carry the new concurrency-token and event-emission logic
  but were **not** whole-file mutated (large + the .NET line-range defect makes it
  dilutive). Their new logic is covered by the shipped integration + gold tests
  (`TeamConcurrencyTokenIntegrationTest`, `WorkItemDomainEventsGoldTest`,
  `DomainEventDispatcherGoldTest`), not by a mutation score — the headline number
  is strong evidence on the small files, weaker on the core files. Recorded so a
  future reader does not over-trust the percentage.

## Lessons learned

- **Live E2E caught a concurrency-correctness bug unit tests missed.** The first
  cut of #5100 advanced the token on *every* save including system/sync saves, so
  a ForecastFilter save (a system write) churned the token → spurious 409 →
  incomplete rule application. The two-session premium-license dogfood reproduced
  it; the fix was "advance the token only on the human-edit path". EF InMemory
  ignores concurrency tokens entirely, so this could only be seen against
  real-SQLite via `TestWebApplicationFactory` and in the live app.
- **Re-throw inside a fire-and-forget handler escapes the test harness.** Four FE
  mutants survive because production re-throws into an unhandled rejection that
  vitest cannot observe through its assertion channel — they are unkillable
  without restructuring error-propagation. A genuine limitation, not laziness.
- **ArchUnitNET has sharp edges worth knowing.** Namespace collision forced an
  `ArchitectureModel` alias for the `Tests.Architecture` namespace; it cannot
  match generic-extension-method calls (needed a custom `MethodCallDependency`
  walk) nor target closed generics (`DbSet<T>`); name predicates need
  `HaveNameStartingWith("X(")` and `.WithoutRequiringPositiveResults()` where a
  rule may legitimately match nothing.
- **DISCUSS premises must be re-validated against the code.** The "4-path cleanup
  gap" (#5099 Family B) did not exist — there were no logs to orphan. Building the
  full handler set would have been speculative scope. Surfaced and trimmed.
- **CI-learnings recurrences still bite.** Slices pre-applied / newly recorded:
  CA1859 (declare concrete module fields, not `IObjectProvider<IType>`),
  S6544-vs-S3735 (Sonar bans the `void` operator while requiring promises handled
  → `.catch()` + a void-returning `dispatchSave`), S3218 (a nested-record property
  shadowing a static method → renamed `WasBlockedBeforeSync`), and
  `[NonParallelizable]` on the static-`MetricsCache` test classes to stop
  cross-test contamination.
- **The workspace had no execution log.** This epic was driven manually slice-by-
  slice (DISCUSS via Luna, then hand-delivered), not through the nw-develop
  engine. Completeness is established by git history + CI-green + ADO Resolved,
  not by a `roadmap.json`/execution log.

## Cross-cutting outcomes

- **RBAC** — the 409 path on RBAC config aggregates flows through
  `IRbacAdministrationService`; the concurrency outcome is orthogonal to the 403
  authorization decision (unauthorized → 403, authorized-but-stale → 409). No new
  authz surface.
- **Lighthouse-Clients** — CLI + MCP map the new 409 to a distinct
  "changed since you loaded it; re-fetch and retry" error. No new endpoint (409 is
  added to existing endpoints), so no `FEATURE_REQUIRES_SERVER_NEWER_THAN` bump;
  clients tolerate older servers that never emit 409.
- **Architecture doc** — root `ARCHITECTURE.md` (the discoverable overview) now
  documents the dispatcher seam, the concurrency model, and the seven enforced
  modules as implemented concepts.

## Follow-ups (open)

- **Downstream reactions (#4754/#4755)** — the proactive-notification rail that
  the #5122 events unlock (SignalR/notifications/webhooks). Each will need its own
  RBAC review; a webhook/notification endpoint must version-gate per the clients
  rule. The eventual capability could be a premium website surface.
- **`WorkItemService` / `LighthouseAppContext` mutation depth** — if these files
  are ever split or refactored smaller, revisit whole-file mutation for the
  concurrency-token + event-emission logic now covered only qualitatively.

## Pointers

- Decision log + slices + ACs: `docs/feature/epic-5121-domain-events-cqrs-concurrency/feature-delta.md`, `slices/`
- ADR: `docs/product/architecture/adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md`
- Mutation report: `docs/feature/epic-5121-domain-events-cqrs-concurrency/deliver/mutation/mutation-report.md`
- Backend: `Services/Implementation/DomainEvents/`, `Models/Events/`, `Data/LighthouseAppContext.cs`, `API/Filters/ConcurrencyConflictExceptionFilter.cs`, `Services/Implementation/BackgroundServices/Update/TeamDataRefreshed*/TeamDeleted*Handler.cs`
- Frontend: `src/hooks/useModifySettings.ts`, `src/components/Common/Connection/ModifyConnectionSettings.tsx`
- Clients: `lighthouse-clients/packages/client/src/index.ts` (409 → `concurrency-conflict`)
