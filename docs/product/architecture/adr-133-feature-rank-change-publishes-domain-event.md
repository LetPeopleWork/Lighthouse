# ADR-133: A rank change publishes a domain event and triggers a coalesced forecast recompute

**Status**: Accepted
**Date**: 2026-08-06
**Feature**: `epic-5375-manual-sorting` (ADO Epic #5375 "Manual Sorting")
**Decider**: Hera (DDD Architect), DESIGN domain layer, interaction mode = PROPOSE

---

## Context

Reordering Features changes forecast dates — `ForecastService.GetSimulationResultsOfFeatureToUpdate`
(`:201-209`) hands each simulated day's throughput to the first `FeatureWIP` remaining Features in
sequence. DISCUSS left the consequence implicit: AC-3.5/3.6 say only that the dates move "on the next
forecast run", which in practice means the user reorders, sees the `#` column renumber, and every date
on the board stays wrong until a sync happens to fire.

That is a product defect wearing a scheduling excuse. It is also exactly the class ADR-027 D6 exists to
fix structurally — a write path whose derived read is kept coherent by whoever remembers to call the
recompute (bug #4778).

The honest counter-argument is cost: `UpdateForecastsForPortfolio` runs 10,000 Monte Carlo trials over
every Feature the portfolio's teams touch (`ForecastService.cs:17`, `:63-79`). It is the heaviest
computation in the product, and a user re-sorting a backlog produces a burst of moves.

## Decision

**A completed move publishes `FeatureRankChanged(int FeatureId)`. A handler triggers a forecast update
for each Portfolio the Feature belongs to, through the existing `IForecastUpdater.TriggerUpdate`. No
new debouncing mechanism is added — the queue already coalesces.**

### The events

| Event | Published by | Carries |
|---|---|---|
| `FeatureRankChanged(int FeatureId)` | `IFeatureRankingService`, after the move transaction commits | the moved Feature's id only |
| `FeatureOrderingPolicyChanged(FeatureOrderingPolicy Policy)` | the settings command that flips the switch, after commit | the new policy value |

Both are past-tense POCO `record`s in `Models/Events/`, matching `PortfolioFeaturesRefreshed` and
`PortfolioForecastsUpdated` exactly, and both announce a fact **already persisted** — the ADR-027 D2
discipline that makes a lost reaction recoverable and an outbox unnecessary.

`FeatureRankChanged` deliberately carries only the moved Feature's id, not the shifted block and not
the affected Portfolio ids. The handler resolves Portfolios from the Feature, precisely as
`TeamDataRefreshedForecastTriggerHandler` (`:13-27`) resolves them from the Team. One fact, minimal
payload, no denormalised list to keep true.

### Why the moved Feature's Portfolios are the right blast radius

`UpdateForecastsForPortfolio` → `UpdateForecastsForTeams(portfolio.Teams)` → every Feature those teams
work on, across portfolio boundaries (`ForecastService.cs:63-79`). So triggering on the moved Feature's
Portfolios already recomputes every Feature whose forecast the move could have changed. The shifted
block needs no separate trigger.

### Why no new debounce is needed

`IForecastUpdater.TriggerUpdate(portfolioId)` enqueues on `UpdateQueueService` under
`UpdateKey(UpdateType.Forecasts, portfolioId)` — verified, not assumed: `ForecastUpdater` derives from
`UpdateServiceBase<Portfolio>` with `UpdateType.Forecasts` (`ForecastUpdater.cs:12-18`), and
`UpdateServiceBase.TriggerUpdate` is a direct call to `updateQueueService.EnqueueUpdate`
(`UpdateServiceBase.cs:18-20`). When a run is already in flight, `TryAdmit` fails and
the trigger is **parked as a single coalesced follow-up** rather than dropped or duplicated
(`UpdateQueueService.cs:78-88`, `:198-201`, `:208-230`). N rapid moves against one Portfolio therefore
collapse to at most two forecast runs: the one in flight, plus one follow-up that reads the newest
state. This coalescing is shipped and proven, not proposed.

### Cost, stated

- **Burst of moves**: bounded to two runs per Portfolio by the coalescing above. This is the common case
  — a user re-sorting a backlog clicks faster than a forecast completes.
- **Slow drip of moves** — one move every 30 s while a forecast takes 20 s — runs a full portfolio
  forecast per move. That is the honest worst case, and it is identical to what the existing manual
  "run forecast" path already costs. Not mitigated.
- **`FeatureOrderingPolicyChanged` triggers a forecast update for every Portfolio.** On enable this
  work is provably wasted (D6 seeds from the current order, so the sequence is unchanged); on disable
  and on re-enable-after-drift it is required. The unconditional form is chosen for simplicity: it is
  a rare `SystemAdmin` action, and each Portfolio's trigger is independently coalesced. A conditional
  "skip when the seeded order equals the source order" is a legitimate optimisation the solution
  architect may take; it is not required for correctness.

### What this is not

- **Not persisted.** The dispatcher is a thin in-process router that must not persist (ADR-027 D2 and
  its 2026-05-29 addendum). There is no move log, no event store, no audit trail of who moved what —
  consistent with the DISCUSS out-of-scope list. If an audit trail is ever wanted, the addendum's
  transport-vs-sink split makes it an additional opt-in subscriber to `FeatureRankChanged`, with **no
  change to this design**.
- **Not Event Sourcing.** ADR-027 D7 stands. The rank is current-state, and D5/D9's retention of the
  untouched source `Order` already provides the revert property that would otherwise be the one
  argument for replay.
- **Not synchronous.** Dispatch is after-commit; the HTTP response to a move returns as soon as the
  rank is persisted, without waiting for a forecast. AC-3.5's "the move persists" is satisfied by the
  transaction; AC-3.6's date change arrives over the existing SignalR completion push.

## Alternatives Considered

### A. No event — leave the recompute to the next scheduled sync (the DISCUSS default) — rejected

Cheapest, and defensible on the grounds that Lighthouse already labels forecast data "as-of last sync".

Rejected because reordering is the one action whose *entire purpose* is to change the dates, and this
option makes it the one action that visibly does not. The user's feedback loop for "did my priority
call help?" becomes a sync interval long, which turns K3 (a reorder actually moves the forecast) into
something the user cannot observe even when it is working.

### B. Recompute synchronously inside the move request — rejected

Gives an immediate, obviously-correct answer.

Rejected because it puts 10,000 Monte Carlo trials over every Feature of every affected team on a
button click, blowing K6's 500 ms budget by orders of magnitude, and because it re-couples the mutator
to a reactor — the exact shape ADR-027 exists to dissolve.

### C. Event plus a bespoke debounce (a timer, a "recompute now" button) — rejected

Rejected as duplicate machinery. `UpdateQueueService` already coalesces on `UpdateKey`, which is a
better debounce than a timer because the follow-up is scheduled off *completion* rather than off a
guessed interval, and it is already load-bearing for the sync path. An explicit user-triggered
recompute would also make the feature's central promise ("the forecast follows your priority")
conditional on remembering to press something.

### D. A coarser event — `FeatureOrderChanged(portfolioIds)` carrying the whole affected set — rejected

Rejected on the ADR-027 naming discipline: one fact per event, self-contained, minimal payload. The
Portfolio set is derivable from the Feature at handling time, and a denormalised list in the payload
is a second copy of a relationship that can already be read.

## Consequences

**Positive**

- The user's reorder changes the dates within one forecast run, closing the AC-3.5/3.6 gap.
- The mutator publishes and knows no reactors; a future reaction (an audit sink, a notification) is a
  new subscriber and zero edits to the ranking service.
- Reuses the shipped dispatcher, the shipped `IForecastUpdater`, and the shipped coalescing. No new
  infrastructure, no new background service.

**Negative / cost**

- A slow drip of moves runs one full portfolio forecast each. Bounded only by how slowly the user
  clicks. Accepted, not mitigated.
- Flipping the policy fans out a forecast trigger per Portfolio. Harmless on a 3-Portfolio instance,
  a visible burst on a 50-Portfolio one.
- The forecast handler is subject to the dispatcher's handler isolation
  (`DomainEventDispatcher.cs:20-34`): a throwing handler is logged and swallowed. A move whose
  recompute fails leaves the rank correct and the dates stale until the next scheduled sync — the
  ADR-027 D2 recovery path, correct but silent to the user.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| A move publishes exactly one `FeatureRankChanged`, after commit | Unit test on the ranking service with a mocked `IDomainEventDispatcher` (Moq); assert publish happens after the rank is readable |
| The handler triggers one forecast update per Portfolio of the moved Feature | Handler unit test mirroring `TeamDataRefreshedForecastTriggerHandlerTest` |
| Rapid repeated moves do not queue N forecast runs | Integration test: three triggers for one Portfolio while one is in flight produce at most one follow-up |
| The dispatcher does not persist the event | Existing `DomainEventDispatcherSeamArchUnitTest` (ADR-027 D8) — no new rule needed |
| Handlers stay idempotent / replayable | The forecast trigger recomputes from current state; asserted by running it twice and comparing output |

## Cross-reference

- Realises ADR-027 D2 (after-commit dispatch, no outbox) and D6 (derived reads maintained by
  subscription, not by remembered imperative calls) for a new write path.
- [ADR-132](./adr-132-feature-ordering-derived-total-order-no-ordering-aggregate.md) — what a move is
  permitted to change, and its transaction boundary. This ADR governs only what happens after it
  commits.
- Full analysis: `docs/product/architecture/brief.md` → `## Domain Model — epic-5375-manual-sorting`.
