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

## Baseline capture (2026-08-21, dogfood `:5169`, commit `4c0dea826`)

Taken before any production commit of this epic, on the released build. Portfolio 34 *Lighthouse*
(Azure DevOps, 82 Features, real history), Team 34 *Lighthouse Dev Team*.

### Run-to-run spread over unchanged data

Three consecutive forecast-only triggers (`POST /forecast/update/34`), no refetch between them,
diffed per Feature at each percentile.

| percentile | Features moved (of 82) | max move | mean move |
|---|---|---|---|
| 50 % | 0, 0, 0 | **0 days** | 0 |
| 70 % | 1, 4, 3 | **1 day** | 0.01 - 0.05 d |
| 85 % | 5, 10, 5 | **1 day** | 0.06 - 0.12 d |
| 95 % | 5, 2, 2 | **1 day** | 0.02 - 0.06 d |

**The spread is one day, and the 50 % date never moves.** Against this slice's own test - *"if the
spread is under a day at the percentiles people actually read, the question is closed"* - forecast
determinism does not earn an epic. ADR-154's fresh production seed stands.

**KPI-2's magnitude floor is therefore 1 day.** Restated: at least one Feature ranked below a waiting
one reads **earlier by two days or more**, reproduced across three runs. AC-5.2's own >= 3 working-day
target clears it.

### Wall clock

Trigger-to-idle for a whole Portfolio-34 forecast: **5-6 s**, measured by external polling at 1 s
granularity, so this is a ceiling rather than a measurement. A precise figure needs the timing
restoration.

### Write-back volume per Portfolio refresh (the AC-10.3 number)

One flush per refresh, 160 staged updates, of which the connector was actually called for 24 and 17 on
two consecutive refreshes. Forecast-only triggers staged 29 updates and called for 1, 4 and 3. After
this slice the flush count becomes two by design; **the assertion is on connector calls, not flushes.**

### Forecast executions per refresh - and what the count actually revealed

| path | forecast executions |
|---|---|
| Portfolio refresh alone | 1 |
| Team refresh alone | **0** |
| Both together (the Update All shape) | **1** |

Not the two or three this slice was written to remove. The reason is a defect this wave found, and it
is not the one the slice describes.

## The Team-triggered forecast never fires on this Portfolio

`Portfolio.Teams` (`Models/Portfolio.cs:9`) is a **derived** property - `Features.SelectMany(f =>
f.FeatureWork)`, recomputed on read, always right. `Team.Portfolios` is a **persisted** many-to-many
over the `PortfolioTeam` table (`LighthouseAppContext.cs:278-280`). They are two different relations,
and the forecast trigger reads the persisted one: `TeamDataRefreshedForecastTriggerHandler:21`
iterates `team.Portfolios`, as does `ForecastController.UpdateForecastsForTeamPortfolios:41`.

On the dogfood instance `PortfolioTeam` holds two rows - `(35,35)` and `(36,36)`. **Portfolio 34 has
none**, so for the one Portfolio with real history a Team refresh triggers nothing. Confirmed in both
directions with debug logging on: Team 34 logs `Getting Team by Id. Id 34` and stops; Team 36 logs
`Queuing Update for Forecasts with ID 36` and forecasts.

**No production code writes a `PortfolioTeam` row at all.** `Team.Portfolios`
(`Models/Team.cs:27`) is EF-populatable, but nothing assigns to it or adds to it anywhere outside
tests - `DemoDataService`'s `Teams.Add(...)` calls are on a `DemoDataScenario` descriptor holding
team *names*, not the entity relation. The two rows on the dogfood database are residue from an
older schema. On any Portfolio created since, `TeamDataRefreshedForecastTriggerHandler` and
`ForecastController.UpdateForecastsForTeamPortfolios` iterate an empty collection and do nothing.

Three consequences for this slice:

- **The redundancy is conditional on the persisted link.** A linked Portfolio gets 1 inline forecast
  plus 1-2 on the `(Forecasts, id)` key - the 2-3 the slice describes. An unlinked one gets 1. The
  debounce US-11 builds has nothing to debounce on an unlinked Portfolio.
- **Scenario #12's silent success already ships.** `POST /forecast/update-portfolios-for-team/34`
  returns `true` and HTTP 200 having run no forecast at all. The scenario was written for a failure
  the debounce would introduce; the failure is already there, for a different reason.
- **US-10 is unaffected and still worth doing.** Moving `PortfolioUpdater`'s inline forecast onto the
  shared `(Forecasts, portfolioId)` key is correct regardless of the link, and it is what makes the
  two paths visible to one another at all.

### The same wrong read costs more than the forecast

`team.Portfolios` has a **third** production reader: `WorkItemService.cs:70`, inside
`UpdateWorkItemsForTeam`, iterating it to call `UpdateRemainingWorkForPortfolio`. On a Portfolio with
no `PortfolioTeam` row that loop never runs, so **a Team refresh does not recalculate the Portfolio's
remaining work** either. That is core data staleness rather than a forecasting-frequency problem, and
it is the strongest evidence that the missing row is a product defect and not stale dogfood data.

**Deliberately not fixed in this slice.** Repairing that read would change Feature remaining-work
values on refresh, which changes forecast output - and this slice changes how often a forecast runs,
never what it computes. Recorded here so it is neither lost nor "helpfully" fixed in passing.

### The derivation, proved against the dogfood data

`FeatureWork.TeamId` -> `FeaturePortfolio` -> `PortfoliosId`, run over the captured database:

| team | derived | persisted (`PortfolioTeam`) |
|---|---|---|
| 34 | **[34]** | [] |
| 35 | [35] | [35] |
| 36 | [36] | [36] |

`FeaturePortfolio` holds 88 rows and is maintained normally in production; `FeatureWork` holds 67, of
which 56 belong to Team 34. The derivation recovers exactly what the join table lost, and agrees with
it wherever it still has rows.

### Why the suite never caught this

Two acceptance fixtures populate `team.Portfolios` **by hand** -
`ManualSortingAcceptanceTest.cs:176` and `QuietWriteBackAcceptanceTest.cs:305`. The tests are green
because they establish a relation production never writes, so no test has ever exercised the case
where it is missing.

**Not decided here.** Whether the missing `PortfolioTeam` row is stale dogfood data or a product
defect - and whether repairing it belongs in this slice, this epic, or its own bug - is the
maintainer's call.

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
