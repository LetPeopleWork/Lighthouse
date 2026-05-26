# ADR-027: Target Architecture — Keep the Modular Monolith, Add an In-Process Domain-Event Dispatcher + CQRS-lite (No Broker, No Microservices, No Event Sourcing)

**Status**: Accepted (2026-05-26 — full-stack DESIGN, interaction mode PROPOSE; user sign-off received)
**Date**: 2026-05-26
**Feature**: target-architecture-4618 (ADO User Story #4618 "Analyze best target Architecture" — analysis-only)
**Deciders**: nw-system-designer, nw-ddd-architect, nw-solution-architect (Morgan) — consolidated; accepted by the user (Benjamin Huser-Berta).

---

## Context

ADO #4618 asks where Lighthouse's architecture is now and where it should go — explicitly an analysis, "Not the implementation." The story raises four concrete pressures:

1. **Coupling smell** — "having to inject so many services to react on deletion/adding" suggests an event bus.
2. **Data handling / CQRS** — motivated by the "not so nice fix" for bug #4778 ("Delivery Team Update Issue").
3. **Scalability** — the architecture should scale to the hosting story (dockerized enterprise #4599, future SaaS) while staying honest: realistic load is **~20–150 users total, rarely concurrent — NOT thousands**.
4. **Topologies that must all survive** — standalone single-binary (easy entry), dockerized + PostgreSQL, k8s example chart (#4599), possible future cloud SaaS.
5. **Operational correctness** — two users hitting refresh on the same entity; one queuing an update while another deletes it; "eventual consistency and stuff."

A full-stack DESIGN analysis (System → Domain → Application) was run in propose mode. The dominant finding, verified against the codebase rather than assumed: **Lighthouse is already a correctly-sized modular monolith.** It has an in-process work queue (`UpdateQueueService`, a single-reader `Channel<Func<Task>>`), SignalR push, a process-singleton maintenance lock (`DatabaseMaintenanceGate`), provider-switched persistence (`DatabaseConfigurator.AddDbContext` switches SQLite↔Postgres on a config string with two migration assemblies), and a tested serialization-based concurrency story (`PortfolioDeleteSerialisationTests`). The "target architecture" question is therefore mostly *what to deliberately keep, name, and harden* — and the dominant risk is **over-engineering**, exactly as the story warns.

Two facts shape the answers:

- The **single-instance / single-writer property is load-bearing**: the in-process queue, the maintenance-gate singleton, and the SignalR hub all assume one process owns the data. Anything that breaks this (horizontal scale-out, an out-of-process broker) trades away proven correctness for throughput the load profile does not need.
- There is **no optimistic-concurrency token anywhere** (zero `RowVersion` / `xmin` / `IsConcurrencyToken` domain mappings). Concurrency today is coarse serialization plus a blanket reload-and-retry `SaveWithRetry` that is **last-writer-wins**. The one genuine correctness gap is **two humans editing the same config aggregate** (Team/Portfolio/Connection/RBAC) — a silent lost update.

Two concrete instances of the coupling smell were verified in source:

- `API/TeamController.cs` — constructor injects **9** collaborators; `DeleteTeam` hand-wires the reactions (`refreshLogService.RemoveRefreshLogsForEntity` + a `foreach portfolioUpdater.TriggerUpdate` loop).
- `Services/Implementation/BackgroundServices/Update/PortfolioUpdater.cs` — `Update` resolves **7** services via `serviceProvider.GetRequiredService` (service-locator) and runs a fixed 6-step imperative pipeline, including imperative metric-cache invalidation (`InvalidatePortfolioMetrics`) — the scattered "remembered invalidation" that is the #4778 bug class.

---

## Decision

Keep the modular-monolith / ports-and-adapters style. Harden it with eight sub-decisions:

| ID | Decision | Verdict |
|----|----------|---------|
| **D1** | Event-bus mechanism: a lightweight in-house `IDomainEventDispatcher` + `IDomainEventHandler<TEvent>` (mirrors the existing `IUpdateQueueService` seam). | **ACCEPT** |
| **D1a** | Use MediatR instead of the in-house dispatcher. | **REJECT** — MediatR is commercial since v12/13; poor ROI vs ~30 lines and a licensing liability for a simplicity-first/standalone product. |
| **D2** | The dispatcher is an inbound (driving) **application port**; its implementation is a thin **router**, not a driven adapter. Heavy after-commit work routes onto the existing `UpdateQueueService` channel (reused unchanged). Default dispatch is **after-commit**; an in-transaction tier is reserved for true invariants only. | **ACCEPT** |
| **D3** | Hexagonal invariants preserved: event records are POCO `record`s in the model layer (`Models/Events/`), below both `API` and `Services.Implementation`, so `Implementation ↛ API` holds. The dispatcher resolves handlers via typed `IEnumerable<IDomainEventHandler<TEvent>>`, **never** `GetRequiredService` (so the seam does not re-introduce the service-locator it removes). `TeamController`'s ctor shrinks (9 → ~5). | **ACCEPT** |
| **D4** | Microservices. | **REJECT** — breaks the single-writer singletons and the standalone single-binary; no throughput driver at 20–150 users. #4599 (k8s) is a **packaging** concern (`replicas: 1` Helm chart), not an architecture split. |
| **D5** | Module boundaries: seven logical modules (WorkTracking-Integration, WorkItems/Sync, Forecasting, Portfolio/Delivery, Metrics/Time-in-state, RBAC/Identity, Platform/Persistence) — already namespace folders. Name them and **enforce via ArchUnitNET in a single assembly**. | **ACCEPT** (reject physical assembly split — complicates single-binary publish for enforcement the test-time rules already give) |
| **D6** | CQRS: **lightweight command/query separation on the same store** — write = `*Updater` + repository; read = `BaseMetricsService` + cached DTOs. Move metric-cache invalidation from the imperative mutator call to an **event subscriber** (structurally fixes the #4778 scattered-invalidation class). | **ACCEPT** (reject full CQRS / separate read store — no read-throughput bottleneck; fights the no-fork + standalone goals) |
| **D7** | Event Sourcing. | **REJECT** — `WorkItemStateTransition` is a *historical projection of an external changelog*, not domain event sourcing; keep it, do not generalise it. |
| **D8** | Add `TngTech.ArchUnitNET` (test-only) to realise the dependency rules + the dispatcher/seam invariants + a gold-test (publish → all handlers fire; a throwing handler does not lose the committed fact, which recovers on the next scheduled re-sync). Note: ArchUnitNET is **not yet a dependency** — prior brief references to it are aspirational; this is the work that realises them. | **ACCEPT** |

**Concurrency (cross-cutting, from the System + Domain layers):** add optimistic-concurrency tokens (Postgres `xmin` / SQLite rowversion-style) → **HTTP 409** on the **human-edited config aggregate roots only — Team, Portfolio, WorkTrackingSystemConnection, RBAC (UserProfile/RbacGroupMapping/ApiKey), Delivery (light)**. **NOT** on high-churn single-writer sync entities (WorkItem, Feature, FeatureWork, WorkItemStateTransition). The blanket `SaveWithRetry` reload-retry must be **scoped to bypass tokened-aggregate saves**, or it silently swallows the 409 it is meant to surface. Reject pessimistic locking (breaks SQLite). Consistency contract: **read-your-writes** for a user's own config edits; **eventual / temporal** ("as-of last sync") for sync-derived metrics and forecasts, honestly labeled.

**Scale & topology (from the System layer):** stay **single-instance / vertically scaled** (sizing ≈ 30 QPS peak, 30–100× headroom). One provider-switched architecture serves all four topologies **without forking**; k8s = `replicas: 1` + readiness/liveness probes + Secret-sourced credentials; SaaS kept *open* via the clean EF boundary but not built. **No out-of-process broker** — the standalone single-binary forbids a mandatory broker dependency, and the load profile shows no need.

---

## Alternatives Considered

- **Horizontal scale-out / multi-instance now.** Rejected: would destroy the in-process correctness singletons (queue, maintenance gate, SignalR) and drag in the very broker D-events avoids, for throughput 30–100× beyond the real load. Revisit only if a measured throughput ceiling appears.
- **Out-of-process message broker (RabbitMQ/Azure Service Bus/etc.).** Rejected: incompatible with the zero-dependency standalone single-binary; at-least-once/ordering semantics are unnecessary when all subscribers are in-process and facts already live in the DB.
- **MediatR (D1a).** Rejected on licensing + ROI (above).
- **Full CQRS with a separate read store.** Rejected: no read-throughput bottleneck exists; a second store fights the no-fork and standalone-friendliness goals and adds a sync/consistency surface the scale does not justify.
- **Event Sourcing (D7).** Rejected: needless complexity at this scale; the domain has no audit/temporal-replay requirement that DB-of-record + the existing transition history does not already meet.
- **Microservices (D4).** Rejected: Conway/scale mismatch (small team, single deployable, single writer); #4599 is packaging, not decomposition.
- **Physical assembly split for modules (D5).** Rejected: buys compile-time enforcement that test-time ArchUnitNET rules already provide, while complicating the single-binary publish.

---

## Consequences

**Positive**
- The 9-injection `DeleteTeam` and the 7-locator `PortfolioUpdater.Update` collapse into a mutator-that-publishes + decoupled handlers; new reactions to add/delete become "add a handler," not "edit a controller."
- The #4778 bug class (scattered, remembered cache invalidation) is fixed *structurally* — invalidation becomes a subscriber to the relevant event.
- The lost-update gap on config aggregates closes with a clear 409 contract, without touching the high-churn sync path.
- Every topology stays on one architecture; SaaS is not foreclosed; standalone stays dependency-free.
- Net new infrastructure is small: one ~30-line dispatcher, event POCOs, optimistic tokens on ~5 roots, and a test-only ArchUnitNET suite.

**Negative / cost**
- A migration to perform (incrementally — see below). Handlers must be idempotent/id-keyed/replayable; recovery relies on the periodic re-sync rather than an outbox (an accepted trade for simplicity, valid because facts are DB-derivable).
- Scoping `SaveWithRetry` away from tokened aggregates is a careful change — getting it wrong re-hides the lost update.
- ArchUnitNET adds a test dependency and a small suite to maintain.

**Neutral**
- Paradigm unchanged (OOP / hexagonal, C# backend). No frontend architecture change. No new runtime dependency in the shipped binary.

---

## Recommended Migration Stance — incremental, never big-bang

The story strongly prefers step-by-step-alongside-other-work. The seam is designed for exactly that: each slice is independently shippable and reuses the existing queue, so nothing lands all at once.

**Natural first slice — `PortfolioFeaturesRefreshed`.** Introduce the minimal `IDomainEventDispatcher` / `IDomainEventHandler<TEvent>` + one event record, then peel exactly one reaction — the metrics invalidation — out of `PortfolioUpdater.Update` into an `IDomainEventHandler<PortfolioFeaturesRefreshed>`. This single slice (i) proves the seam end-to-end on the busiest real reactor, (ii) structurally fixes the highest-value bug class (#4778), (iii) lets the remaining six `PortfolioUpdater` reactions migrate one handler at a time in later unrelated PRs, and (iv) ships the ArchUnitNET suite + the publish→handlers-fire gold-test with the first handler so the invariants are guarded from day one. No production behaviour changes — the same work runs, just published-then-subscribed instead of hand-wired, which makes it safe to interleave with feature work. **`TeamController.DeleteTeam` (the 9-injection smell) is the natural second slice** once the seam has earned trust. Optimistic tokens and the module-boundary ArchUnitNET rules can each land as their own small slices independently.

---

## Architectural Enforcement (when migration proceeds)

| Rule | Mechanism |
|---|---|
| `Services.Implementation` never depends on `API`; controllers depend only on interfaces | ArchUnitNET test (D8) |
| The dispatcher resolves handlers via typed `IEnumerable<IDomainEventHandler<TEvent>>`, never `IServiceProvider.GetRequiredService` | ArchUnitNET test forbidding `GetRequiredService` in the dispatcher + handler types |
| The seven modules do not form illegal cross-module dependencies | ArchUnitNET layered-dependency rules per module namespace |
| Publishing an event runs all registered handlers; a throwing handler does not lose the committed fact and the work recovers on next re-sync | Gold integration test (publish → all handlers fire; throwing handler → fact survives + recovers) |
| Tokened config aggregates surface a 409 on concurrent edit; `SaveWithRetry` does not silently retry them | Integration test: two stale writes to the same Team/Portfolio/Connection → second gets 409 |

---

## Cross-reference

- Supersedes nothing; complements the existing hexagonal invariants (ADR-001 RBAC port boundary, brief.md core Application Architecture).
- Full propose-mode analysis backing this ADR: `docs/product/architecture/brief.md` sections `## System Architecture / ## Domain Model / ## Application Architecture — target-architecture-4618 (analysis)`.
- Related work items: #4778 (motivating bug, Closed), #4599 (k8s example, New).
