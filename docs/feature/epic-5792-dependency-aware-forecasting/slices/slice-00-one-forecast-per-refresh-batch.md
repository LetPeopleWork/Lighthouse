# Slice 00 — One forecast per refresh batch `@infrastructure`

**Feature**: epic-5792-dependency-aware-forecasting · **ADO**: Epic #5792 ·
**Stories**: US-10, US-11 · **Estimate**: ~5h
**Reference class**: ADR-144's staging seam, which changed the same four call sites and landed inside
its estimate. Treat that as the closest comparable.

## Goal

Hitting **Update All** produces **one** forecast per Portfolio instead of two or three — and therefore
one date per Portfolio instead of two or three different ones.

## Why this is in the epic

Discovered while designing slice 02, on 2026-08-18. Two things make it belong here rather than in a
backlog it would never leave:

- **Slice 02 makes every redundant run expensive.** Today the forecast is one task per team, and each
  ends as soon as its own team is done. After the joint clock, every trial runs until the slowest team
  finishes. A wasted run is cheap now and is not cheap afterwards.
- **The redundant run visibly moves the date.** `RandomNumberService.GetRandomNumber` is
  `new Random().Next(maxValue)` — unseeded — so two runs over byte-identical data return different
  percentiles. A user watching a refresh sees a date settle, then change again seconds later for no
  reason they can see. Removing the second run removes that, and it is the only fix available:
  ADR-154 deliberately keeps a fresh production seed per run.

## The two paths, and why there are two stories

**US-10 — the Portfolio path stages its own forecast.** `PortfolioUpdater` runs
`UpdateForecastsForPortfolio` inline under the `(Features, portfolioId)` key, so it is invisible to the
`(Forecasts, portfolioId)` admission check and cannot coalesce with a Team-triggered forecast. Route it
through `IForecastUpdater` so both paths share one key.

**US-11 — the trigger waits for its siblings.** `TeamDataRefreshedForecastTriggerHandler` fires
`TriggerUpdate(portfolio.Id)` for every Portfolio the Team belongs to, the moment that one Team
finishes. Under **Update All** — which is the ordinary case, not an edge: `TeamsController`'s
`update-all`, `PortfoliosController`'s `refresh-all`, the periodic refresh, and the standalone
offline auto-update all fan out this way — the first Team to finish triggers a forecast that the
remaining Teams then invalidate. Park the trigger while any sibling Team or the Portfolio itself still
stands **`Queued`**, so the last one to finish is the one that triggers.

`InProgress` is deliberately excluded, and getting this backwards ships a system that never forecasts:
`TeamDataService.UpdateTeamData` publishes `TeamDataRefreshed` while `(Team, id)` is still `InProgress`,
so a rule counting `InProgress` would park every trigger on its own execution. Excluding it is safe
because `UpdateQueueService` runs a single-reader loop, making the only `InProgress` key the caller's
own (DESIGN, SA-18).

## IN scope

- `IUpdateStatusStore` gains a per-Portfolio active-work query. `HasActiveWork()` exists but is global,
  and both implementations (`InProcessUpdateStatusStore`, `RedisUpdateStatusStore`) need it.
- `PortfolioUpdater`'s inline forecast call replaced by a trigger on the shared key.
- The debounce in `TeamDataRefreshedForecastTriggerHandler`, expressed as parking rather than dropping —
  a dropped trigger loses the write that caused it, which is the failure the existing coalescing was
  built to avoid.
- A test that an Update All over N Teams in one Portfolio produces exactly one forecast execution.

## OUT of scope

- **Making the forecast deterministic.** ADR-154 rules a fixed production seed out and its reasoning
  stands: a constant seed does not remove sampling error, it freezes one draw of it into every date
  forever. A seed derived from a hash of the run inputs is the only shape worth having and is an epic
  of its own — the hash has to cover every input the simulation reads, and missing one produces sticky
  dates that silently fail to move, which is worse than the wobble. See the measurement below.
- **The cluster gap.** `pendingReruns` is pod-local and the advisory lock blocks the consumer loop —
  recorded as a Known Gap on ADR-076, deliberately not scheduled. Every deployment is single-pod today.
- Any change to what a forecast computes. This slice changes how often one runs, never its output.

## What ADR-144 permits, and what it does not

Splitting `PortfolioUpdater`'s single flush into two executions is **safe**, and the code says so twice:

- The two staging passes are **disjoint by construction**. `ResolvePortfolioWriteBack` partitions
  mappings on `ForecastSources.Contains(m.ValueSource)`, one resolver taking the set and the other its
  complement, so the collector's last-stage-wins dedup never fires between them. The single flush is
  saving nothing.
- ADR-144 already treats the Team-triggered forecast as a separate execution with its own flush, and
  closed that residue by persisting the written value into the local `AdditionalFieldValues` after a
  successful write. Cross-execution duplicates are suppressed by the `!=` guard, not by a shared flush.

**The line not to cross**: ADR-144's local-copy write is a narrow exception — *persist only a value that
was just successfully written to the tracker*. Nothing in this slice may extend it into damping forecast
movement. That is D11, and it stays.

One residual edge, worth knowing rather than designing around: two mappings configured onto the same
field reference, one forecast-sourced and one not, would now produce two writes where they produced one.
Pathological configuration, and the local-copy guard absorbs it on the next refresh.

## Acceptance criteria

- **AC-10.1** An Update All across N Teams sharing one Portfolio runs exactly one Forecasts execution
  for that Portfolio.
- **AC-10.2** A Portfolio refresh and a Team refresh overlapping in time produce one forecast, not two.
- **AC-10.3** Write-back volume per Portfolio refresh does not increase. Asserted against the
  connector call count, which is the number ADR-144 was written to protect.
- **AC-11.1** A trigger raised while sibling work is in flight is **parked**, never dropped: the
  forecast that eventually runs reflects every Team's refreshed data. Asserted on **both** terminal
  paths — the sibling that succeeds and the last sibling that fails.
- **AC-11.2** A Team update with no sibling work in flight still triggers its forecast immediately —
  the debounce must not add latency to the single-Team case.
- **AC-11.3** `IUpdateStatusStore` gains a per-Portfolio active-work query, implemented in **both**
  `InProcessUpdateStatusStore` and `RedisUpdateStatusStore`. The existing `HasActiveWork()` scans the
  whole store and cannot answer it, so a debounce built on it would park every forecast behind every
  other update in the instance.

## Dogfood moment

On `:5169`, hit **Update All** and count Forecasts executions per Portfolio in the refresh log, before
and after. Record both counts here.

**Measure the wobble in the same sitting** — this is the observation that decides whether determinism
is ever worth an epic, and it costs ten minutes inside work already planned. Run the forecast twice
back-to-back over unchanged data and diff the 50/70/85/95% dates. If the spread is under a day at the
percentiles people actually read, the question is closed. If it is several days on an eight-week
forecast, it is a credibility problem and earns its own epic. **Write the number down either way.**

## Dependencies

None. This slice touches the update queue and no part of the forecasting change, which is why it can
land first.

## Sequencing

**Before slice 01.** Not because slice 01 needs it, but because it is cheapest to prove "one forecast
per batch" while a forecast is still the thing it is today. Landing it after slice 02 would mean
changing the trigger topology and the simulation loop in the same window.

## Learning hypothesis

**Disproves** "the redundant forecast is what users are seeing" **if** the date still visibly changes
after a single execution per batch. That would point at inbound data changing between refreshes rather
than at Monte Carlo noise, and the wobble measurement above tells the two apart.

## Learning hypothesis verdict

_Not yet run._
