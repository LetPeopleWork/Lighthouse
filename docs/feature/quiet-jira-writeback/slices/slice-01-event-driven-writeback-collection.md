# Slice 01 - Event-driven write-back collection

**Type:** slice | **Est:** ~1-1.5 days | **Stories:** US-04 | **Connectors:** Jira + ADO

## Why this exists

Write-back fires **inline, multiple times per refresh cycle**, from three call sites:

- `PortfolioUpdater.cs:79-85` - `TriggerFeatureWriteBackForPortfolio` after features refresh, then
  `TriggerForecastWriteBackForPortfolio` after forecasts. **Two independent passes** over overlapping issues.
- `ForecastUpdater.cs:43-44` - a third pass, triggered per-team via
  `TeamDataRefreshedForecastTriggerHandler.cs:21-24`, which loops `team.Portfolios` and calls
  `forecastUpdater.TriggerUpdate(portfolio.Id)` for each. A portfolio with **N teams gets N forecast
  write-back passes per refresh round.**
- `TeamUpdater.cs:53-54` - team-level write-back.

The passes do not deduplicate. `WriteBackService.WriteFieldsToWorkItems` calls the connector and returns
results; it never writes the new value back into the local `AdditionalFieldValues`. So the stored copy
holds the pre-write value until the next inbound sync, and pass 2 compares fresh values against a stale
local copy - `currentAdditionalFieldValue != update.Value` fires again, and the same field is written again.

Collecting write intents across the cycle and flushing **once at the end** removes the duplicate passes.
This is the architectural seam the rest of the epic builds on.

## Value story

**Before:** a portfolio with 4 teams triggers 4 forecast write-back passes per refresh round; the same
percentile field is written to the same issue 4 times, and every write emails every watcher.
**After:** one write-back flush per cycle, one write per genuinely-changed field.
**Decision enabled:** the admin's inbox reflects real forecast movement, not Lighthouse's internal
refresh topology.

## Design constraints (verified, non-negotiable)

- **The dispatcher does not share the publisher's scope.** `DomainEventDispatcher.cs:11-12` is registered
  singleton (`Program.cs:1052`) and calls `serviceScopeFactory.CreateScope()` per publish. Handlers run in
  a **fresh scope**. A scoped accumulator injected into the updaters and read by a handler would be **two
  different instances**. The naive design silently collects nothing. Choose deliberately: payload carried
  in the event, a correlation-keyed singleton accumulator, or an explicit end-of-cycle flush in the updater.
- **Publishing does not defer.** `PublishAsync` awaits handlers inline, sequentially, in DI registration
  order. Deferral needs an explicit terminal signal (e.g. a `PortfolioUpdateCompleted` event published
  last) or an outbox - it is not a free consequence of using events.
- **Ordering must not become registration-order-coupled.** Today the sequence (features -> write-back ->
  forecasts -> write-back) is explicit and readable in `PortfolioUpdater`. If it becomes handler-driven,
  the ordering contract moves into `Program.cs` registration order, which is fragile and untested. Prefer
  a design where ordering stays explicit.
- **Handler exceptions are swallowed.** `DomainEventDispatcher.InvokeHandlerSafely` catches everything and
  logs (`CA1031` suppressed with justification). Not a regression - `WriteBackTriggerService.cs:56` already
  swallows all exceptions itself - but it means a flush failure will never surface to a caller. Keep the
  existing log-and-continue semantics; do not silently widen the blast radius.

## Acceptance criteria

- AC-04.1: Given a portfolio with N teams, when a refresh round completes, then write-back executes
  **once**, not N+2 times.
- AC-04.2: Given the same field on the same issue is resolved by more than one pass in a cycle, when the
  flush runs, then exactly one write is issued for that issue+field.
- AC-04.3: Given a cycle where no mapped value changed, when the flush runs, then no connector call is
  made at all (preserves the D8 no-op guard).
- AC-04.4: Given the ADO connector, when write-back runs via the new collection path, then
  `suppressNotifications: true` is still passed and behaviour is otherwise unchanged.
- AC-04.5: Given `WriteBackResult` / `WriteBackItemResult`, when the flush completes, then per-item
  success/failure semantics are identical to today's for every caller.
- AC-04.6: Given a flush that throws, when the cycle completes, then the failure is logged per item and
  the refresh round still completes (parity with today's swallow-and-log).
- AC-04.7: Given a team-level write-back (`TriggerWriteBackForTeam`), then it participates in the same
  collection and flush.

## IN scope

- A collection seam for write-back intents spanning a refresh cycle, plus a single terminal flush.
- Both write-back-capable connectors (Jira, ADO) - the seam is above the connector, so it is
  connector-agnostic by construction.
- Deduplication by issue + field within a cycle.

## OUT of scope

- **Per-issue field batching** - slice 02. This slice reduces the number of *passes*; slice 02 reduces the
  number of *calls per pass*. Kept separate so the event seam and the connector payload change are
  independently revertible and bisectable.
- Any Jira-specific suppression (slices 03-06).
- Linear and CSV - both `throw new NotSupportedException` (D8).
- Forecast re-simulation jitter (D11 - explicitly out of scope, user decision 2026-07-17).

## Taste tests

- Value-bearing: yes - fewer duplicate writes = fewer watcher emails, before any Jira work lands. PASS.
- Right-sized: one seam, two connectors, three call sites. PASS.
- Disproves a pre-commitment: yes - disproves D8's "cadence is already correct". PASS.
- New abstraction required? Yes, one (the collector) - justified: it is the epic's architectural seam and
  slices 02-06 all sit on it. PASS.
