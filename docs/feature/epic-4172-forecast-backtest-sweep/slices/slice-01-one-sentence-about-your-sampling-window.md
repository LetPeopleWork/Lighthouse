# Slice 01 — One sentence about your sampling window, and which numbers held up

**Feature**: epic-4172-forecast-backtest-sweep · **ADO**: to create under Epic #4172 · **Story**: US-01
**Estimate**: ~1h probe + ~7h · **Job**: `job-forecaster-check-the-forecast-against-what-happened`

**Reference class**: `ForecastController.RunBacktest` (`:195-234`) — a synchronous, non-async action that
runs one `HowMany`, two throughput reads and one `CreateForecastDtos(50, 70, 85, 95)` inside the request.
This slice runs that same shape sixteen times and adds a reading on top of it.

## Goal

A delivery forecaster presses one button on the Team's Forecast tab and gets back, in seconds and without
typing a date, a sentence saying whether the sampling window behind every forecast this Team publishes is
sound, and which of the four confidence levels actually held up against what the Team really delivered.

## The probe runs first

**AC-1.1 gates everything else in this slice.** Nothing below it is written until the number exists.

Measure, on the development instance, against a Team holding at least twelve months of Work Items:

1. The median and maximum wall-clock of a single shipped `POST /api/latest/forecast/backtest/{teamId}`,
   twelve samples. This is the baseline.
2. The median and maximum of a sixteen-run sweep in the same process, twelve samples.

Budget: **median at most 5 s, maximum at most 10 s.** Both numbers go into this brief before any UI is
written.

**If the budget is missed**, the slice stops. The fallback is the existing `UpdateQueueService` — but read
the ADR-195 caveat before reaching for it: the queue holds one `Channel.CreateUnbounded` with one
sequential reader, its three-lane version shipped and was reverted the same day (`f216ef558`) because
concurrent refreshes destroyed Feature ownership, and its own field report records a Portfolio refresh
running 77.7 minutes and starving every Team refresh for 3h38m. A short, read-only, user-initiated
computation that contacts no work tracking system is a different kind of work from a tracker sync, and
putting it behind one is how it becomes unusable.

### Measured

> To be filled in before any other work in this slice. Leaving this section empty when the slice closes
> means AC-1.1 was skipped, not passed.

| | Single backtest | Sixteen-run sweep |
|---|---|---|
| Median | *(pending)* | *(pending)* |
| Maximum | *(pending)* | *(pending)* |

## IN scope

- **The endpoint.** `POST /api/latest/forecast/reality-check/{teamId}` and the `/api/v1/…` twin, following
  `ForecastController`'s existing two-route pattern. Guard
  `[RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]`, byte-identical to the shipped
  backtest. Body at most `{ "applyFilterOverride": bool? }`. **No dates.**
- **Sixteen runs, four horizons × four sampling windows**, every one ending today and reaching back by its
  own length; each run's history window sits immediately before its scored period. Reuses
  `forecastService.HowMany`, `GetBlackoutAwareThroughputForTeam`, `GetThroughputForTeam`,
  `GetEffectiveBlackoutDays` / `CountWorkingDays`, `GetForecastThroughputStatus` and `CreateForecastDtos`
  **unchanged**.
- **Per-cell sufficiency** via `ForecastDataSufficiencyPolicy.HasEnoughData` called on each cell's own
  `RunChartData`. `MinimumActiveDays` is not touched and no second bar is introduced.
- **The verdict logic, carrying all three honesty requirements.** The region of sampling windows that
  behaved alike (never a winner); the null result as a first-class answer; the denominator with its
  correlation disclosure; the per-level coverage against each level's nominal rate.
- **The card on the Forecast tab**, inside the existing `InputGroup title="Forecast Backtesting"`
  (`TeamForecastView.tsx:438-460`), above the shipped date pickers. One button; the button becomes a
  spinner in place; the result replaces the previous one. No notification, because it is on screen.
- Unevaluable cells named in the sentence with their reason.

## OUT of scope

- **The evidence view.** Slice 02. The endpoint returns every cell, but this slice speaks only the
  sentence.
- **Apply.** Slice 03. The verdict names a value; nothing writes it.
- **The one-pager.** Slice 04.
- **Any entity, table, migration or `UpdateType` member.** D8. A migration appearing in this slice is a
  signal D8 was violated.
- **Any change to the forecast engine.** Asserted: the existing forecast assertions pass unchanged.
- **A date picker anywhere.** D6 removed the need for one, and that is what makes the single click viable.

## Learning hypothesis

**Disproves, if it fails**: that a sixteen-run sweep is a request. That is the load-bearing assumption of
the entire recommendation — `recommendation.md` §0 states it as the first of two conditions. If it fails,
the direction changes shape before any UI exists, which is the whole reason the probe is first.

Three ways it could fail:

1. **Cost.** Evidence is strong: `RunBacktest` is already synchronous, and every `TeamMetricsService` read
   goes through `GetFromCacheIfExists` over Work Items already in the database, so **no work tracking
   system is contacted**. Sixteen cells is a bounded multiple of something the product already does inside
   a request. Strong — but an inference until AC-1.1 runs.
2. **Cache shape.** The sixteen runs ask for sixteen *different* history windows. If `GetFromCacheIfExists`
   keys on the window, every one of them is a cache miss, and the bounded multiple is a multiple of the
   uncached cost rather than the cached one. The probe must be run on a cold cache as well as a warm one.
3. **The null result may be the answer for every Team.** If every sampling window behaves alike on every
   Team the maintainer can reach, the sentence is always "your setting is fine" — which is *the honest
   finding of the source study* and the strongest thing the feature can say, but it is worth knowing
   before slice 02 invests in an evidence view for it.

**Confirms, if it succeeds**: slices 02, 03 and 04 are all presentation over a response that already
exists. No further forecasting work is required anywhere in this Epic.

## Acceptance criteria

AC-1.1 through AC-1.8, in `feature-delta.md` under US-01. AC-1.1 gates the rest of the slice.

## Notes for the implementer

- Route style: kebab-case segments are this codebase's convention (`my-summary`, `group-mappings`,
  `system-admins`, `bootstrap/system-admin`). `reality-check` is idiomatic.
- `BacktestInputDto`'s validation (`ForecastController.cs:169-193`) is **not** reused as written. D6 makes
  three of its four rules vacuous; the 14-day minimum survives as a property of the 2-week horizon, not as
  input validation.
- **Do not hard-code one Team into the result shape.** "Check every Team at once" is a plausible next unit
  of work, and ADR-207 carries this as an explicit forward-compatibility constraint.
- The response must carry no field naming a single winning window. If a `recommendedWindow` field feels
  natural, that is §4.1 reasserting itself and the answer is a *range* plus a boolean for whether the
  current setting is inside it.
- Dogfood against this project's own Lighthouse instance with real history before the slice closes. Its
  data is real; the checked-in `.db` is not.
