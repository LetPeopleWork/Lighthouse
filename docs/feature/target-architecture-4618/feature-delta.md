# Feature Delta — target-architecture-4618

ADO User Story #4618 "Analyze best target Architecture" — **analysis-only** ("Not the implementation"). No DISCUSS/DISTILL/DELIVER waves: the deliverable is a decision, captured as ADR-027 (Proposed) plus analysis sections in the architecture brief. There is no code change and no acceptance-test surface in this workspace.

## Wave: DESIGN

### [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| n/a | Lighthouse stays a single-instance, single-writer modular monolith (ports-and-adapters, OOP C#) — the in-process queue, maintenance-gate singleton, and SignalR hub all depend on one process owning the data | D4 | Forecloses horizontal scale-out and out-of-process brokers; keeps proven correctness for the real 20–150-user load |
| n/a | One provider-switched architecture (SQLite↔Postgres) serves standalone, docker, k8s (#4599), and future SaaS without forking | D4 | k8s is packaging (`replicas: 1` chart), not an architecture split; SaaS kept open via the EF boundary but not built |
| n/a | Introduce a lightweight in-house `IDomainEventDispatcher` + `IDomainEventHandler<TEvent>` seam, dispatched after-commit onto the existing `UpdateQueueService` channel | D1, D2 | Dissolves the 9-injection `TeamController.DeleteTeam` and 7-locator `PortfolioUpdater.Update` smells; new add/delete reactions become "add a handler" |
| n/a | Event records are POCO `record`s under `Models/Events/`; the dispatcher resolves handlers via typed `IEnumerable<…>`, never `IServiceProvider.GetRequiredService` | D3 | Preserves the hexagonal invariant `Services.Implementation ↛ API` and prevents re-introducing the service-locator |
| n/a | Lightweight command/query separation on the **same** store; metric-cache invalidation moves from the imperative mutator to an event subscriber | D6 | Structurally fixes the #4778 "Delivery Team Update Issue" scattered-invalidation bug class; full CQRS / separate read store rejected |
| n/a | Optimistic-concurrency tokens (→ HTTP 409) on human-edited config roots only (Team, Portfolio, WorkTrackingSystemConnection, RBAC, Delivery); blanket `SaveWithRetry` scoped to bypass them | n/a | Closes the silent lost-update gap on config edits without touching high-churn sync entities (WorkItem/Feature/FeatureWork/transition) |
| n/a | Reject MediatR (D1a), microservices (D4), full CQRS (D6), and Event Sourcing (D7); add test-only `TngTech.ArchUnitNET` (D8) to enforce the seam + module rules | D1a, D4, D6, D7, D8 | Right-sizes for 20–150 users; ArchUnitNET realises the brief's previously-aspirational dependency rules + a publish→handlers-fire gold test |

### [REF] Decisions

See `docs/product/architecture/adr-027-target-architecture-modular-monolith-domain-events-cqrs-lite.md` (Status: **Accepted** 2026-05-26) for the full D1–D8 decision table, alternatives, consequences, enforcement, and the incremental migration stance.

### [REF] Analysis sections (SSOT)

Full propose-mode analysis lives in `docs/product/architecture/brief.md`:
- `## System Architecture — target-architecture-4618 (analysis)` — scale, concurrency, topologies, eventing infra
- `## Domain Model — target-architecture-4618 (analysis)` — aggregates/tokens, event vocabulary, CQRS/ES verdicts
- `## Application Architecture — target-architecture-4618 (analysis)` — dispatcher component design, module boundaries, CQRS-lite mapping, Reuse Analysis table

### [REF] Open questions

- Confirm the tokened-aggregate set (the 5 config roots) before any concurrency slice scopes `SaveWithRetry`.
- Decide whether the seven modules become ArchUnitNET-enforced boundaries now or are named-only until the dispatcher seam lands.
- Migration sequencing beyond the first slice (`PortfolioFeaturesRefreshed`) is deferred to whenever the work is scheduled — this story does not commit a timeline.
