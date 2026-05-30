# Slice 01: In-process domain-event dispatcher + first slice (PortfolioFeaturesRefreshed)

**Feature**: epic-5121-domain-events-cqrs-concurrency
**ADO child**: #5098
**Story shipped**: US-5098 (`@infrastructure`)
**Role**: WALKING SKELETON — the thinnest end-to-end proof of the dispatcher seam on the busiest real reactor.
**Estimate**: ~1 crafter day
**ADR**: D1, D2, D3, partial D8

## Goal

Introduce the minimal in-house `IDomainEventDispatcher` + `IDomainEventHandler<TEvent>` seam (mirrors the existing `IUpdateQueueService`), publish the first event `PortfolioFeaturesRefreshed` after-commit, and peel exactly ONE reaction — metric-cache invalidation — out of `PortfolioUpdater.Update` into an `IDomainEventHandler<PortfolioFeaturesRefreshed>`. Ship the test-only `TngTech.ArchUnitNET` harness plus the publish→handlers-fire gold test. **Zero production behaviour change**: the same invalidation work runs, just published-then-subscribed instead of hand-wired.

## IN scope

- `IDomainEventDispatcher` (inbound application port) + thin router implementation that resolves handlers via typed `IEnumerable<IDomainEventHandler<TEvent>>` — NEVER `IServiceProvider.GetRequiredService` (D3).
- `IDomainEventHandler<TEvent>` interface.
- Event record `PortfolioFeaturesRefreshed` as a POCO `record` under `Models/Events/` (below `API` and `Services.Implementation`, so `Implementation ↛ API` holds — D3).
- After-commit dispatch routing heavy work onto the existing `UpdateQueueService` channel, reused unchanged (D2).
- Move `InvalidatePortfolioMetrics` from the imperative step inside `PortfolioUpdater.Update` into a `PortfolioFeaturesRefreshed` handler. `PortfolioUpdater.Update` publishes the event instead of calling invalidation directly.
- Test-only `TngTech.ArchUnitNET` NuGet added to the test project (D8) + the first two enforcement rules: dispatcher/handlers forbid `GetRequiredService`; `Services.Implementation ↛ API`.
- Gold integration test: publish `PortfolioFeaturesRefreshed` → all registered handlers fire; a throwing handler does NOT lose the committed fact and the work recovers on the next scheduled re-sync.

## OUT scope

- Migrating the remaining `PortfolioUpdater` reactions or the `TeamUpdater`/delete/cross-aggregate reactions (slice 02 / #5099).
- The seven module-boundary ArchUnitNET rules (slice 04 / #5101) — only the two seam invariants land here.
- Any work-item events (slice 05 / #5122), concurrency tokens (slice 03 / #5100).
- An outbox or any persisted event log — recovery is via the next re-sync (D2/D7); explicitly NOT built.

## Learning hypothesis

**Confirms if it succeeds**: a ~30-line in-house dispatcher dispatched after-commit onto the existing queue can carry a real reaction (metric invalidation) end-to-end with zero behaviour change; the gold test proves publish→all-handlers-fire and throwing-handler-survives-and-recovers; ArchUnitNET enforces the seam invariants from day one. The seam is trustworthy enough to migrate the rest onto (slices 02, 05).
**Disproves if it fails**: either (a) after-commit dispatch onto `UpdateQueueService` cannot reliably guarantee the committed fact survives a throwing handler without an outbox (would force reopening the no-outbox decision D2), or (b) resolving handlers via typed `IEnumerable<>` without the service locator does not compose with the existing DI registration and we are pushed back toward `GetRequiredService` (would violate D3 and need a DESIGN rethink).

## Acceptance criteria

See US-5098 in `../feature-delta.md` (Wave: DISCUSS / Stories). Slice specifics:

- Integration test: publishing `PortfolioFeaturesRefreshed` invokes every registered handler exactly once; the metric-cache-invalidation handler runs and the post-refresh cached portfolio metrics are invalidated identically to the pre-change behaviour (golden equivalence).
- Resilience test: with a registered handler that throws, the committed portfolio-refresh fact is preserved (not rolled back), the other handlers still run, and a subsequent re-sync re-drives the failed reaction.
- ArchUnitNET test: the dispatcher and handler types contain no `IServiceProvider.GetRequiredService` usage; `Services.Implementation` has no dependency on `API`. Both rules fail RED if violated.
- No-regression: the full existing backend suite stays green; `PortfolioUpdater.Update` produces the same observable outcome (metrics invalidated, features refreshed) as before.

## Dependencies

**None upstream** — this is the walking skeleton and the seam everything else hangs off. **Downstream**: #5099, #5101, #5122 all depend on this slice landing first.

## Production data requirement

**Not required.** Zero production behaviour change; verifiable entirely via integration + ArchUnitNET tests against fixtures. Dogfood: after deploy, a portfolio refresh on the project's own Lighthouse instance still invalidates and recomputes metrics exactly as before (no user-visible difference — that IS the success criterion).

## Carpaccio taste tests

- **Independently shippable?** YES — ships as a precursor with test-observable value (gold test + ArchUnitNET green); no other slice required.
- **One day or less?** YES (~1 day) — one event, one handler, one moved reaction, two ArchUnit rules, one gold test.
- **End-to-end?** YES — publish→handler→invalidation→commit, exercised by the gold test.
- **`@infrastructure` (no user-visible behaviour change)?** YES — labelled `@infrastructure`. The epic's user-visible value lives in slices 03 and 05; this slice is NOT a pure-infra dead-end because the epic as a whole ships value.
