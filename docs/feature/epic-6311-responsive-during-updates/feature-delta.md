# Feature Delta — epic-6311-responsive-during-updates

**ADO**: Epic #6311 — *Keep Lighthouse responsive during updates* · children Bug #6308 (forecast takes every
core; Stop does not stop it), Story #6309 (5-year simulation horizon), and Story #6322 (Cancel all).
Evidence: one self-hosted (Docker) user's incident report.

**Waves**: DISCUSS (2026-10-17). DISCOVER and DIVERGE skipped by explicit maintainer decision (D1).

**One line**: while one Portfolio forecast ran, every page hung, every Team refresh sat "Queued behind" it,
and Stop left it "Stopping…" for 10+ minutes until the container was restarted. The forecast takes every
core, ignores Stop, and can simulate up to 274 years per run.

**Density**: `lean` + `ask-intelligent`. Tier-1 `[REF]` only.

---

## Maintainer decisions — 2026-10-17

Two questions turned up in the code reading that the locked scope did not answer. Neither changes the
scope. Both change what users or trackers see. The maintainer took both recommendations: **NMD-1 → c**,
**NMD-2 → a**.

### NMD-1 — How "more than 5 years" travels to clients that read forecast dates

**Finding.** Every forecast percentile goes out on the wire as a non-nullable `ExpectedDate`
(`API/DTO/WhenForecastDto.cs:21,32`; `Models/Delivery.cs:676`). Lighthouse-Clients reads it as a required
day: `readChanceByDay` keeps a chance only when `isDay(value.expectedDate)`
(`lighthouse-clients/packages/client/src/forecastWording.ts:40-42`), and `featureWording.ts:46,78` and
`deliveryWording.ts:77,433` print it. The web app coerces it with `z.coerce.date()`
(`models/Forecasts/forecastSchemas.ts:20`, `models/Delivery/ArchivedDelivery.ts:60`). So "a Feature shows
'more than 5 years' instead of a date" (D5) cannot be done by sending no date without breaking released
clients.

| Option | Released CLI/MCP clients see | Web app | Contract |
|---|---|---|---|
| **a** — `ExpectedDate` becomes null | The chance silently drops out of the CLI/MCP answer | `z.coerce.date(null)` gives 1 Jan 1970 unless every reader changes in the same release | **Breaking** |
| **b** — additive flag (e.g. `beyondHorizon: true`), `ExpectedDate` kept as the horizon's date | A real-looking date about 5 years out, until they upgrade | Renders the flag | Additive |
| **c** — option b plus a coordinated Lighthouse-Clients release that renders the flag, gated on server version | Old clients as in b; upgraded clients say "more than 5 years" | Renders the flag | Additive + clients minor |

**Recommendation: c.** It is additive (expand-only, matching the project's migration rule), and the gap it
leaves is bounded: released clients show a date about 5 years out for a Feature that really is that far
away. That gap is already the case today for a forecast that hits the cap, and it closes when the clients
upgrade.

**Decided: c** (maintainer, 2026-10-17).

### NMD-2 — What write-back sends to the tracker for a percentile beyond the horizon

**Finding.** Forecast write-back already writes nothing when a forecast has no answer:
`ProjectFromToday` returns null for `daysAway < 0` (`Services/Implementation/WriteBackTriggerService.cs:326-331`),
and a null value produces no field update (`:210-219`). The project's stated precedent is *"No answer means
no write, not an empty one"* (`:244`, `:254-257`). Writing nothing leaves the tracker field showing its
**last** date, which may be an old date that is now wrong.

| Option | Tracker field |
|---|---|
| **a** — write nothing (today's behaviour for "no answer") | Keeps its last value |
| **b** — clear it | Empty. The precedent calls this "louder and less true", and some field types may not accept it |
| **c** — write the horizon's date | A real-looking date nobody forecast |

**Recommendation: a**, in line with the precedent. The stale-date risk is named in the slice-03 docs, so the
tracker-facing behaviour is written down and not discovered.

**Decided: a** (maintainer, 2026-10-17).

---

## Maintainer decisions — 2026-10-18

Taken at DESIGN, on the proposal's open choices. Every other DESIGN item took its recommendation (decisions table
below).

### NMD-3 — The wire shape of "beyond the horizon"

**Decided: a** (maintainer, 2026-10-18).
- `WhenForecastDto` gains `beyondHorizon: boolean` and `horizonYears: number`. `WhenDistributionPointDto` (snapshots,
  pinned closure records) gains `beyondHorizon: boolean`.
- For a beyond percentile, `expectedDate` = today + the horizon's simulated days, in calendar arithmetic.
- Lighthouse-Clients key on **whether the field is present**. This **refines** NMD-1 c's "gated on server version" and
  does not reverse it: an older server never sends the field, which says what a version check would. ADR-252.

### NMD-4 — What the Jira Release forecast block says for a beyond percentile

**Finding.** A Delivery that broadcasts its forecast writes "70%: yyyy-MM-dd" lines into a Jira Release description
(`DeliveryForecastBlockRenderer.cs:67-73`). This is a second tracker-facing write of forecast dates that NMD-2 does not
cover.

**Decided: a** — write "85%: more than 5 years" on that line (maintainer, 2026-10-18). The block is prose, and
finding the previous block anchors on its opening and "Target" lines, not the percentile lines. ADR-254.

### NMD-5 — Slices 03 and 04 ship together

**Decided** (maintainer, 2026-10-18): build slice 03, then slice 04 (D7 holds), and **release them together** under
Story #6309. Shipped alone, slice 03 would give released clients a stand-in date about 274 years out.

### NMD-6 — No early stop for the whole forecast now

**Decided: not now** (maintainer, 2026-10-18). The per-run cap of 1,826 simulated days is the kill switch.
- **Why:** stopping the whole forecast early changes how many runs stand behind every percentile and likelihood. Stop
  (slice 02) already covers the case a user sees.
- **Deferred follow-up:** designed only if AC-04.4 misses (the slowest Portfolio forecast at least 50% faster).

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `platform-operator` | **Primary.** Runs the instance. In the incident a self-hoster on Docker watched every page hang, pressed Stop, waited 10+ minutes, and restarted the container. Owns slices 01, 02 and 05. |
| `delivery-forecaster` | **Secondary, slices 03–04.** Reads Feature and Delivery dates and shares them. Today, when part of the simulated runs never finish, gets a date computed only from the runs that did finish (S9). |

No new persona.

---

## Wave: DISCUSS / [REF] JTBD One-Liners

| Job ID | One-liner | Status |
|---|---|---|
| `job-operator-keep-lighthouse-usable-while-it-forecasts` | When Lighthouse is recalculating forecasts in the background, I want every page to keep answering, so a forecast reads as "busy" and not as "dead". | **New** (jobs.yaml) |
| `job-operator-stop-a-refresh-that-is-doing-harm` | When an update is hurting the instance, I want to stop it, or everything waiting, without restarting the container. | Reused (epic-5511); now covers forecasts (02) and Cancel all (05) |
| `job-forecast-no-false-certainty` | When I share a forecast, I want it to admit what it does not know, so nobody holds me to a certainty the simulation never had. | Reused (forecast-confidence-cap); slices 03–04 |

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

Read from the code on 2026-10-17 (worktree at `e18f0d0fd`). The ADO analysis was not copied: each claim was
checked again in the code.

| # | Fact | Evidence |
|---|---|---|
| S1 | **The simulation takes every core.** `Parallel.For(0, limits.Trials, …)` with no `ParallelOptions`: no `MaxDegreeOfParallelism`, no `CancellationToken`. It is wrapped in `Task.Run`, so it also occupies a pool thread while it waits. | `Services/Implementation/Forecast/ForecastService.cs:110` (`Task.Run`), `:143-153` (`Parallel.For`) |
| S2 | **Every simulation goes through that one method**: background Portfolio forecasts (`UpdateForecastsForPortfolio`), the manual forecast an HTTP request runs (`When` → `ForecastFeatures`), and the reality-check and backtest probes. A bound placed there covers them all. | `ForecastService.cs:55-66,68-71,101-113` |
| S3 | **No cancellation reaches the simulation.** `UpdateCancellationContext.Current` is set for every update execution, but only `WorkItemService` reads it. `ForecastService` takes no token. | `UpdateQueueService.cs:594`; `UpdateCancellationContext.cs:16-25`; searching for `UpdateCancellationContext` finds only `WorkItemService.cs:35`, `UpdateQueueService.cs`, `Program.cs:1604` |
| S4 | **So Stop on a running forecast cancels a token nobody reads.** `RunUpdateAsync` maps an `OperationCanceledException` to `Cancelled` only when one is thrown. The simulation never throws one, so the row keeps reading `Stopping…` until the simulation ends on its own. That is the 10+ minutes in the report. | `UpdateQueueService.cs:421-443`; `ActivitySection.tsx:28` |
| S5 | **A cancelled forecast already keeps the previous forecast, if the cancel lands inside the simulation.** Feature forecasts are written by `UpdateFeatureForecasts` *after* `RunMonteCarloSimulation` returns, and saved after that. A cancel thrown inside the loop reaches neither. `ForecastUpdater` already records `Cancelled` on the `RefreshLog` row and rethrows. | `ForecastService.cs:110-112,78-83`; `ForecastUpdater.cs` `Update` (catch `OperationCanceledException` → `cancelled = true; throw`) |
| S6 | **A cancelled forecast still flushes its round.** `UpdateServiceBase.TriggerUpdate` runs `FlushWriteBack` in a `finally`. Writes the Portfolio refresh already staged into the shared round still go out; the forecast's own date writes were never staged. | `UpdateServiceBase.cs:35-49`; `ForecastUpdater.cs` `ForecastPortfolio` (stages after the simulation) |
| S7 | **The horizon is 100,000 simulated days (about 274 years) per run.** A run steps one day at a time while any Team has work left, so a low-throughput Team with many items costs about items ÷ throughput days per run, × 10,000 runs. | `Models/Forecast/ForecastSimulationLimits.cs:11`; `SimulatedRun.cs:21-50` |
| S8 | **The code claims no data can reach the horizon. Bug #6308 shows data can.** `HowTheRunEnded.RanOutOfDays` is documented as "no data can cause it". A Team that delivers 0.01 items a day with 1,000 items left reaches 100,000 days. | `HowTheRunEnded.cs:14-19`; `ForecastService.cs:252-279` (logged as an error, "Those teams have no dates") |
| S9 | **A run that does not finish is dropped from the distribution, not counted as late.** It records no finish day, and `TotalTrials` is the sum of what *was* recorded. Every percentile and likelihood is then computed over the finished runs only, so it is biased early. This is rare at 274 years and becomes **common at 5 years** — the main risk of slice 04. | `SimulatedRun.cs:29-31`; `OneWorkersShareOfTheRuns.cs`; `ForecastBase.cs:115` (`TotalTrials = simulationResult.Values.Sum()`), `:53-70`, `:72-94` |
| S10 | **When no run finishes, the API shows today's date.** `GetProbability` returns `-1`, and `ProjectWorkingDays` returns `start` for any count ≤ 0, so `ExpectedDate` reads *today*. | `ForecastBase.cs:69`; `BlackoutDaysExtensions.cs:50-55`; `WhenForecastDto.cs:21`; `Delivery.cs:676` |
| S11 | **Consumers of a forecast percentile** — the blast radius of slices 03–04: `WhenForecastDto`, `Delivery.ToWhenPercentile` and the delivery likelihoods, `Feature.GetLikelhoodForDate`, `FeatureForecastWindow.EndFor` (95th, `-1` → 0), `ForecastController.EstimateForecastWindowEnd`, `ForecastPredictabilityScore`, `ForecastRealityCheckService`, write-back `ProjectFromToday`, and the multi-team `AggregatedWhenForecast` / `JointCompletionDistribution`. Start forecasts (`StartForecast`) use the same histogram, so they share the horizon. | the `GetProbability(` call sites listed; `AggregatedWhenForecast.cs:16-19`; `ForecastService.cs:167-196` |
| S12 | **Places a forecast date appears or is stored**: web `ForecastInfo.tsx:42`, `Feature.ts:138`, `DeliverySection.tsx:754`, `deliveryExportTable.ts:51`, `deliveryTimelineModel.ts:105`, `DeliveryPredictabilityChart.tsx:97`, `ArchivedDelivery.ts:60` (pinned closure record), `DeliveryMetricsHistory.ts:115` (snapshot history); clients `forecastWording.ts`, `featureWording.ts`, `deliveryWording.ts`, `cli/src/forecastOutput.ts:44`. The Jira app reads none of them. | listed files; searching `lighthouse-jira-app` for `expectedDate` finds nothing |
| S13 | **Write-back writes nothing when there is no answer** (see NMD-2). | `WriteBackTriggerService.cs:210-219,324-331` |
| S14 | **Per-row Stop**: `POST api/latest/update/tasks/{type}/{id}/cancel`, `[RbacGuard(SystemAdmin)]`. Refuses the two delete types. It publishes through `IUpdateCancellationNotifier`, so it reaches whichever replica runs the work. A queued key is advanced to `Cancelled` at once (`CancelIfStillWaiting`); a running key keeps its row until its own terminal path runs. | `API/UpdateController.cs` `CancelTask`; `UpdateQueueService.cs:90-100,121-127` |
| S15 | **Things that look queued but are not on the list, or come back after a Stop**: coalesced follow-ups (`pendingReruns`) are requeued when a run ends, *including a cancelled one*. Held forecasts (`heldUpdates`) are parked until the Teams they wait on clear, and are not admitted, so `/update/status` does not count them. Both are in-process, per replica. | `UpdateQueueService.cs:19-20,164,300-330,454-474,502-532`; `ci-learnings.md:521` |
| S16 | **Queue structure is one lane.** `Channel.CreateUnbounded` drained by one loop. #5877's per-type lanes were shipped and **reverted** (`f216ef558`) because concurrent refreshes wiped Feature→Team ownership. | `UpdateQueueService.cs:11,602-621`; `ci-learnings.md:515-521` |
| S17 | **The Task Manager is System-Administrator-gated at both ends**: the endpoints by `RbacGuard(SystemAdmin)`, the header icon and popover by `useRbac().isSystemAdmin`. The *Update All* button beside it is gated the same way. | `UpdateController.cs`; `TaskManagerIcon.tsx:30,34,48`; `Header.tsx:43,81,160` |
| S18 | **Tests pin the limits** by replacing the `ForecastSimulationLimits` singleton (acceptance fixtures, wall-clock probes, ArchUnit). Changing the default moves their baselines. | `ForecastRealityCheckAcceptanceTest.cs:144-147`, `ForecastedStartDateAcceptanceTest.cs:54-57`, `BenchmarkPortfolio.cs:30-31`, `DependencySingleDecisionArchUnitTest.cs:654` |

What follows from these facts:

- **S1 + S2**: one bound, in one method, fixes the starvation for background and HTTP-run forecasts alike.
- **S3 + S4 + S5**: cancellation needs only a token reaching a loop that already has a place to check it
  between runs. "The previous forecast is kept" is already true once the throw happens inside the loop.
- **S9 is why slice 04 cannot be only a constant change.** Lowering the horizon without changing how
  unfinished runs are counted would quietly make many forecasts *more optimistic*, the opposite of
  Story #6309's intent. So honest counting (slice 03) comes before the lower horizon (slice 04).
- **S15 is why Cancel all is more than "Stop, for every row".** A cancelled run with a parked follow-up
  starts again at once. An operator who pressed Cancel all would see the row come back.

---

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Source |
|---|---|---|
| **D1** | DISCOVER and DIVERGE are skipped. Evidence is one Docker self-hoster's incident report. Feature type cross-cutting (backend plus a small UI); no walking skeleton (brownfield); UX depth lightweight; JTBD yes. | Maintainer, 2026-10-17 |
| **D2** | Scope is exactly A1, A2, A3 and B1 (below). Everything else from the incident moves to Epic #6321 (Out of Scope). | Maintainer, 2026-10-17 |
| **D3** | **A1** — the simulation leaves cores free for web requests: a bounded degree of parallelism (for example half the cores, minimum 1). It applies to every simulation, because they share one method (S2). | Maintainer (Bug #6308); "every simulation" from S2 |
| **D4** | **A2** — Stop ends a running forecast promptly: the update's cancellation reaches the simulation and is checked between runs. The previous forecast is kept, in Lighthouse and in the tracker (S5, S6). The `RefreshLog` row records `Cancelled`. | Maintainer (Bug #6308) |
| **D5** | **A3** — the horizon drops from 100,000 days to 5 years. A Feature whose runs do not finish within it shows "more than 5 years" instead of a date. | Maintainer (Story #6309) |
| **D6** | **Runs that do not finish count as "beyond the horizon"; they are not dropped.** "More than 5 years" is a **per-percentile** answer: a percentile shows it when fewer runs than that percentile finished inside the horizon. The 50% can therefore show a date while the 85% shows "more than 5 years". Likelihoods are taken over *all* runs. Start forecasts follow the same rule. | DISCUSS, from S9/S10. Required for D5 to be honest; confirm at the DISTILL walk-through |
| **D7** | **Slice 03 (honest counting and display) ships before slice 04 (the lower horizon).** Lowering the horizon first would ship S9's bias at scale. | DISCUSS, dependency reason |
| **D8** | **B1** — *Cancel all* in the Task Manager stops the running update and clears everything queued, **including** coalesced follow-ups and held forecasts (S15). Scheduled runs carry on as usual afterwards. The two delete types are never cancelled and stay on the list (S14). | Maintainer (scope); follow-ups/held from S15, confirm at DISTILL |
| **D9** | **RBAC: System Administrator only**, the same as the Task Manager and per-row Stop. No new permission. The new endpoint carries `[RbacGuard(RbacGuardRequirement.SystemAdmin)]`; the control lives inside the popover that `useRbac().isSystemAdmin` already gates. Nothing fetches `my-summary` directly. | Maintainer |
| **D10** | **UI sketches are not made in DISCUSS.** The "more than 5 years" look and the Cancel all control are walked through with the maintainer at the **start of DISTILL** (open questions below). Copy uses the seeded terminology — Feature, Work Item, Team, Portfolio — never "Epic" or "Story". | Maintainer; project rule |
| **D11** | Order **01 → 02 → 03 → 04 → 05**: A1, A2, A3 as two slices, then B1. B1 comes after A2 for a dependency reason: Cancel all on a running forecast is only prompt once A2 has made Stop work for forecasts. | Maintainer order; reasons from S4, S9 |
| **D12** | Wire changes are **additive only**. No field changes meaning or becomes null without a maintainer decision (NMD-1). | Project rule (expand-only) |
| **D13** | ACs **never assert a wall clock** measured on a developer machine. "Answers while a forecast runs" and "stops promptly" are asserted with a forecast held open and counted runs. Timings appear only in the production-data checks and KPIs. | `ci-learnings.md:552-558` |
| **D14** | Free, not premium. An instance that appears dead is a correctness problem, as in #5511 and #5877. | Precedent |

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — right-sized.** 4 user stories (one per ADO child), 5 slices, 2 contexts (forecast simulation; update
queue / Task Manager), about 4 days of crafter work in total. The oversized signal that comes closest is
slice 03's display blast radius (S11, S12). It touches many existing surfaces but adds no new component, and
it is bounded by a single wire flag.

## Wave: DISCUSS / [REF] WS Strategy

**C — no walking skeleton.** Brownfield. Every path runs in production today: the simulation, the update
queue, per-key cancellation, the Task Manager, forecast write-back.

---

## Wave: DISCUSS / [REF] Driving Ports

| Surface | Change | Slice |
|---|---|---|
| Any page while a forecast runs (`GET /api/latest/*`) | Keeps answering | 01 |
| Header → Task Manager → stop control on a `Forecasting` row | The row leaves promptly; the previous forecast stays | 02 |
| Every forecast date surface (S12) and `WhenForecastDto` / delivery percentiles | A beyond-horizon percentile is flagged and rendered as "more than N years" (additive, NMD-1) | 03 |
| Forecast write-back | Nothing is written for a beyond-horizon percentile (NMD-2) | 03 |
| Simulation horizon (`ForecastSimulationLimits.Default`) | 100,000 days → 5 years | 04 |
| Header → Task Manager → **Cancel all** (new control); new endpoint (shape decided in DESIGN, e.g. `POST /api/latest/update/tasks/cancel`) | Stops the running update, clears the queue, follow-ups and holds | 05 |
| Lighthouse-Clients CLI/MCP | Renders the flag (clients release, NMD-1 option c) | 03 (finalize) |

## Wave: DISCUSS / [REF] Pre-requisites

- None blocking. #5511 (Task Manager, per-row Stop, `CancelIfStillWaiting`) is shipped.
- **Coordination: Epic #6298 Report PDF Export** (held before DISTILL) renders forecast dates. Whichever of
  the two ships second has to render the beyond-horizon state.
- #5877's lanes are reverted (S16). Nothing here depends on them or reintroduces them.

---

## Wave: DISCUSS / [REF] Out of Scope

Moved to **Epic #6321 "See and steer what Lighthouse updates next"** (maintainer, 2026-10-17):

- Refreshes ahead of forecasts / queue lanes → Epic #6321.
- Showing upcoming scheduled updates.
- Pausing automatic updates.
- "Stopping…" visible server-side to every administrator.
- Showing held updates.
- Skipping forecasts whose inputs have not changed.
- A toast when a background update fails.

Also out:

- Cancelling a manual (HTTP) forecast when the browser goes away. It benefits from D3's bound, but a token
  from `RequestAborted` is not part of this Epic.
- A wall-time bound on refreshes (#5877 US-02's subject).
- Hard abort of in-flight connector work. Cancellation stays cooperative (epic-5511 D5).
- A configurable horizon or a configurable parallelism setting. DESIGN may add one only if it is free; it is
  not a requirement.
- Changing the number of runs (10,000).

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — Pages keep answering while a forecast runs (Bug #6308, A1)

`job_id`: `job-operator-keep-lighthouse-usable-while-it-forecasts` · slice 01

**Problem.** Ingrid Halvorsen runs Lighthouse in Docker on a 4-core host for 14 Teams and 3 Portfolios. When
the "Platform Modernisation" Portfolio is forecast, every page goes blank and navigation stops, because the
simulation takes all four cores and the pool threads web requests need (S1).

#### Elevator Pitch

```
Before: while a Portfolio forecast runs, every page in Lighthouse hangs; navigation shows a blank screen.
After:  open Portfolios → "Platform Modernisation" while its forecast is running → the Features list
        loads and the Task Manager row reads "Forecasting Portfolio 'Platform Modernisation' — Running".
Decision enabled: Ingrid waits for the forecast to finish instead of restarting the container.
```

**Domain examples.** (1) Ingrid, 4 cores: the forecast uses at most 2; the Teams page loads during it.
(2) A Raspberry-Pi-class 1-core host: the bound is 1, never 0, and forecasts still finish. (3) A 32-core SaaS
replica: at most 16 workers. The manual forecast a Team page runs is bounded the same way (S2).

**UAT.**
- *Scenario: A page answers while a forecast is held mid-run* — Given Ingrid's Portfolio forecast is held
  partway through its runs, When she requests the Portfolios list, Then it answers before the forecast is
  released.
- *Scenario: The forecast never uses every core* — Given a host with 4 cores, When a forecast runs, Then no
  more than 2 runs are ever in progress at once.
- *Scenario: A 1-core host still forecasts* — Given a host reporting 1 core, When a forecast runs, Then it
  completes with the same dates a seeded unbounded run produces.
- *Scenario: Bounding does not change the answer* — Given a seeded forecast, When it runs bounded, Then
  every percentile equals the recorded unbounded baseline.

**Acceptance criteria.**
- **AC-01.1** With a forecast held open (a draw-stream double that blocks), an HTTP read of the Portfolios
  list completes. Asserted on completion while the forecast is still held, not on elapsed time (D13).
- **AC-01.2** Observed concurrent runs never exceed max(1, ⌊cores ÷ 2⌋), counted by an instrumented draw
  stream; the core count is injected.
- **AC-01.3** With 1 core, the bound is 1 and the forecast completes.
- **AC-01.4** Seeded percentiles are identical bounded and unbounded. Runs are already combined
  independently of how they were split across workers (`ForecastService.cs:214-219`).
- **AC-01.5 (production data)** On the dev instance restored from the production backup
  (`Restore-DbBackup.ps1`) and capped to 4 CPUs in Docker, while the slowest Portfolio forecast runs, the
  Portfolios, Teams and Feature pages each load. The p95 of 20 `GET /api/latest/portfolios` calls is recorded
  in the slice notes (KPI-1).

**Outcome KPI**: KPI-1. **Technical notes**: one method (S2). Whether the bound honours container CPU
limits, and whether the `Task.Run` wrapper stays, are DESIGN questions 1–2. **Dependencies**: none.

---

### US-02 — Stop ends a running forecast and keeps the last one (Bug #6308, A2)

`job_id`: `job-operator-stop-a-refresh-that-is-doing-harm` · slice 02

**Problem.** Ingrid presses stop on the "Platform Modernisation" forecast row. It reads `Stopping…` for more
than 10 minutes, because nothing in the simulation listens (S3, S4). She restarts the container, which also
throws away every queued Team refresh.

#### Elevator Pitch

```
Before: stopping a running forecast leaves its row on "Stopping…" for 10+ minutes; only a restart ends it.
After:  click stop on "Forecasting Portfolio 'Platform Modernisation' — Running" → the row leaves the list
        within seconds, and the Portfolio's Features still show the dates they showed before.
Decision enabled: Ingrid stops a forecast that is hurting the instance without losing the dates her
        stakeholders already have, and without a restart.
```

**Domain examples.** (1) Stop during run 3,200 of 10,000: no run starts after the cancel is seen; the
Features keep yesterday's dates. (2) Stop on a forecast still queued behind a Team refresh: it is removed at
once, as today (S14). (3) A stopped forecast whose Portfolio refresh staged write-back in the same round:
those writes still go to Jira; the forecast fields are not touched (S6). (4) A forecast with a coalesced
follow-up: the follow-up runs afterwards, as per-row Stop does today.

**UAT.**
- *Scenario: A forecast stops between runs* — Given Ingrid's Portfolio forecast is running, When she stops
  it, Then no further run starts and the row reaches `Cancelled`.
- *Scenario: The previous forecast is kept* — Given the Features of "Platform Modernisation" show forecast
  dates from the last run, When a new forecast is stopped part-way, Then every Feature shows the same dates
  as before.
- *Scenario: The tracker keeps the previous forecast dates* — Given forecast write-back to a Jira field,
  When the forecast is stopped, Then no forecast field update is sent, and the Portfolio refresh's own
  staged writes still are.
- *Scenario: The history says it was stopped* — When it is stopped, Then Settings → System Info → refresh
  history shows the forecast run as cancelled.

**Acceptance criteria.**
- **AC-02.1** After the cancel is observed, no new run starts: the count of runs begun is at most the count
  at the cancel plus one in flight per worker.
- **AC-02.2** The update reaches `Cancelled` (not `Completed`, not `Failed`).
- **AC-02.3** The Features' persisted forecasts (histogram rows and `CreationTime`) are unchanged.
- **AC-02.4** No forecast write-back field update is staged; writes already staged by other work in the
  round are flushed.
- **AC-02.5** `RefreshLog` has `Type = Forecast`, `Cancelled = true`, `Success = false`.
- **AC-02.6** Outside an update (manual forecast, reality check) the simulation runs uncancelled, exactly as
  today.
- **AC-02.7 (production data)** On the restored dev instance, stopping the slowest Portfolio forecast takes
  it off the list. The click-to-`Cancelled` time is recorded (KPI-2), and the Features' dates are compared
  before and after.

**Outcome KPI**: KPI-2. **Technical notes**: `UpdateCancellationContext` → simulation loop (DESIGN Q3).
**Dependencies**: none mechanically. Shares a method with slice 01.

---

### US-03 — A forecast that cannot finish in time says so instead of guessing (Story #6309, A3)

`job_id`: `job-forecast-no-false-certainty` · slices 03 and 04

**Problem.** Tomás Reyes, a delivery lead, forecasts "Billing Migration": 480 items left, and the Payments
Team closes about one item every three weeks. Today a run may simulate up to 274 years. A run that still does
not finish is dropped, so the 85% date comes only from the luckier runs (S9). If none finish, the Feature
shows **today** (S10). Tomás would quote either of those dates.

#### Elevator Pitch

```
Before: a Feature too far out for the simulation shows a date computed from only the runs that finished,
        or today's date; the forecast also spends minutes simulating centuries.
After:  open Portfolios → "Payments 2027" → Features → "Billing Migration" → sees "50%: 14 Mar 2029" and
        "85%: more than 5 years", and the same in the Delivery chips and the CLI.
Decision enabled: Tomás tells the sponsor this Feature is not plannable with the Payments Team's current
        throughput, and splits it or adds capacity, instead of committing to a date.
```

**Domain examples.** (1) "Billing Migration": 62% of runs finish within 5 years → 50% shows a date, 70/85/95%
show "more than 5 years". (2) "Invoice PDF": every run finishes in about 40 days → unchanged. (3) "Legacy
Ledger Retirement": no run finishes → every percentile reads "more than 5 years", never today. (4) The
Delivery "Q4 Release" on 2026-12-15, containing "Billing Migration" → its likelihood counts the unfinished
runs as missing the date. (5) Write-back of the 85% to a Jira date field for "Billing Migration" → no write
(NMD-2).

**UAT (slice 03 — honest counting, horizon still 100,000 days).**
- *Scenario: Runs that do not finish count against every percentile* — Given a Feature where 40% of runs do
  not finish, When it is forecast, Then the 50% shows a date and the 70%, 85% and 95% are beyond the horizon.
- *Scenario: A Feature nobody can finish is never shown as due today* — Given no run finishes, Then no
  percentile reads today's date.
- *Scenario: Likelihood counts unfinished runs as late* — Given a Delivery date inside the horizon, Then its
  likelihood is the share of *all* runs that finished by it.
- *Scenario: Every forecast surface says it the same way* — The Feature list, the Feature's start forecast,
  the Delivery chips, the export table, the timeline and the predictability chart show the same
  beyond-horizon wording (copy settled at DISTILL).
- *Scenario: The tracker is not given an invented date* — Write-back sends nothing for a beyond-horizon
  percentile.

**UAT (slice 04 — five years).**
- *Scenario: A Feature too far out reads "more than 5 years"* — Given "Billing Migration" as above, When it
  is forecast, Then its 85% reads "more than 5 years".
- *Scenario: Near-term Features are unchanged* — Given seeded Features that all finish within 5 years, Then
  their percentiles equal the recorded baseline.
- *Scenario: The long forecast is shorter* — the runs simulated for the low-throughput Portfolio never pass
  1,826 days.

**Acceptance criteria.**
- **AC-03.1** A run that does not finish counts in `TotalTrials` as beyond the horizon. Percentile p is
  beyond the horizon exactly when fewer than p% of runs finished.
- **AC-03.2** No percentile with zero finished runs projects to today (S10).
- **AC-03.3** `GetLikelihood` divides by all runs.
- **AC-03.4** The wire carries an additive beyond-horizon marker on each completion and start percentile,
  for Features, Deliveries and manual forecasts. Existing fields keep their meaning (D12, NMD-1).
- **AC-03.5** Every web surface in S12 renders the marker with one shared wording. A pinned closure record
  and a snapshot written after this slice round-trip it.
- **AC-03.6** Multi-team aggregation treats beyond-horizon as later than any day.
- **AC-03.7** Write-back resolves no value for a beyond-horizon percentile (NMD-2).
- **AC-03.8 (production data)** On the restored dev instance, the Features whose forecasts change are listed,
  before and after, in the slice notes, and the maintainer reads them.
- **AC-04.1** `ForecastSimulationLimits.Default` horizon = 5 years of simulated days (unit in DESIGN Q5).
- **AC-04.2** The copy reads "5 years" from the same constant, not a second literal.
- **AC-04.3** The seeded baselines of the forecast acceptance and probe fixtures (S18) are unchanged for
  Features that finish within 5 years. Any that move are listed and explained in the commit body.
- **AC-04.4 (production data)** On the restored dev instance, the slowest Portfolio forecast's
  `RefreshLog.DurationMs` before and after is recorded (KPI-3).

**Outcome KPIs**: KPI-3, KPI-4. **Technical notes**: representation of beyond-horizon (DESIGN Q4); the
horizon unit (Q5); Epic #6298 coordination. **Dependencies**: slice 04 after slice 03 (D7).

---

### US-04 — Cancel all clears the backlog in one click (Story #6322, B1)

`job_id`: `job-operator-stop-a-refresh-that-is-doing-harm` · slice 05

**Problem.** After a Jira outage, Ingrid's Task Manager lists one running Portfolio refresh and 17 queued
Team refreshes and forecasts. She wants the instance quiet now and the scheduled runs to pick things up
later. Today that means 18 clicks, and refreshes she stopped come back as follow-ups (S15).

#### Elevator Pitch

```
Before: emptying the queue takes one stop click per row, and a stopped refresh with a parked follow-up
        reappears straight away.
After:  open the Task Manager → click "Cancel all" → the running row reads "Stopping…" then leaves, every
        queued row leaves, and the list reads "Nothing is being refreshed right now."
Decision enabled: Ingrid quiets the instance during an incident without restarting it, knowing the
        scheduled updates will resume on their own.
```

**Domain examples.** (1) 1 running + 17 queued → all gone, and the next scheduled Team refresh at 14:00 runs
normally. (2) A running Team refresh with a coalesced follow-up → the follow-up does not run. (3) A held
forecast waiting on two Teams → released never. Its round is let go, so write-back staged earlier still
flushes. (4) A Team deletion in progress → stays on the list, not cancelled. (5) A Viewer → never sees the
control. (6) SaaS with 2 replicas, the running work on the other replica → it stops too.

**UAT.**
- *Scenario: Everything queued leaves at once* — Given one running and 17 queued updates, When Ingrid clicks
  Cancel all, Then every queued row leaves the list and the running row stops.
- *Scenario: Stopped work does not come back as a follow-up* — Given a running refresh with a follow-up
  waiting, When Cancel all, Then the follow-up never runs.
- *Scenario: Scheduled updates carry on* — Given Cancel all was used, When the next scheduled refresh is due,
  Then it runs.
- *Scenario: Deletions are left to finish* — Given a Team deletion running, When Cancel all, Then it
  completes.
- *Scenario: Only a System Administrator can cancel everything* — Given a non-administrator, Then the
  control is absent and the endpoint answers 403.

**Acceptance criteria.**
- **AC-05.1** After Cancel all, the task list holds no `Queued` row except deletes. The running non-delete
  row reaches a terminal state through the per-key path (S14).
- **AC-05.2** No coalesced follow-up and no held update admitted before the call runs afterwards. Rounds
  they held are left, so other work's staged write-back still flushes.
- **AC-05.3** Work triggered after the call, scheduled or manual, is admitted and runs normally.
- **AC-05.4** `TeamDelete`/`PortfolioDelete` are never cancelled.
- **AC-05.5** The endpoint is `RbacGuard(SystemAdmin)`: 403 for non-admins, 204 when the list is empty, and
  idempotent. The control renders only for `useRbac().isSystemAdmin`.
- **AC-05.6** Under the Redis substrate, work running on another replica is stopped.
- **AC-05.7 (production data)** On the restored dev instance with *Update All* pressed (every Team and
  Portfolio queued), Cancel all empties the list. The next periodic tick re-admits stale entities.

**Outcome KPI**: KPI-5. **Technical notes**: DESIGN Q7–Q8. **Dependencies**: after slice 02 (D11).

---

## Wave: DISCUSS / [REF] Story Map

**Backbone**: *Lighthouse is busy* → *Keep using it* → *Stop what is hurting it* → *Trust what it says*.

| Slice | Story | Activity | Ships | Size |
|---|---|---|---|---|
| 01 | US-01 | Keep using it | Bounded simulation parallelism | ~3h |
| 02 | US-02 | Stop what is hurting it | Cancellation reaches the simulation | ~4h |
| 03 | US-03 | Trust what it says | Unfinished runs counted; beyond-horizon on the wire, every web surface, write-back | ~1 day |
| 04 | US-03 | Keep using it / Trust | Horizon 100,000 days → 5 years | ~3h |
| 05 | US-04 | Stop what is hurting it | Cancel all | ~1 day |

Briefs: `slices/slice-01-forecast-leaves-cores-free.md`, `slice-02-stop-ends-a-forecast.md`,
`slice-03-unfinished-runs-count.md`, `slice-04-five-year-horizon.md`, `slice-05-cancel-all.md`.

## Wave: DISCUSS / [REF] Slice Taste Tests

| Test | Verdict |
|---|---|
| Ships 4+ new components | **Pass.** No slice adds a new component. Slice 03 changes about 8 existing surfaces behind one flag and one shared wording; slice 05 adds one control and one endpoint. |
| Every slice depends on a new abstraction | **Pass.** Only 03/04 share one (the beyond-horizon marker), and slice 03 ships it first. |
| No slice disproves a pre-commitment | **Pass.** 01 can disprove "the starvation is CPU/thread-pool" (pages still hang → the cause is elsewhere, e.g. the database). 02 can disprove "a between-runs check is prompt enough". 03 can disprove "surfaces can carry a non-date". 04 can disprove "real Features rarely sit beyond 5 years". 05 can disprove "per-key cancel plus dropping follow-ups and holds empties the list". |
| Synthetic data only | **Pass.** Every slice has a production-data AC on the dev instance restored from the production backup. Tenant Zero is offline. |
| 2+ slices identical but for scale | **Pass.** 01 and 02 touch the same loop but deliver different outcomes, each releasable alone. |
| Every slice has a user-visible story | **Pass.** |

## Wave: DISCUSS / [REF] Prioritization

01 → 02 → 03 → 04 → 05 (D11). **01 first** for learning leverage: if the pages still hang with the bound,
the incident has a second cause, and every later slice should know that before building. **02 next**: same
loop, and it turns a 10-minute `Stopping…` into seconds. **03 before 04** on dependency (D7). **05 last**:
it depends on 02 for a running forecast, and it is the only slice with new UI copy beyond 03. Each slice is
dogfooded the day it ships on the restored dev instance.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| # | KPI | Target | Measurement | Baseline |
|---|---|---|---|---|
| KPI-1 | Page answers during a forecast | p95 of 20 `GET /api/latest/portfolios` < 1 s while the slowest Portfolio forecast runs, 4-CPU Docker cap | Script against the restored dev instance, after slice 01 | Pages hang (report) |
| KPI-2 | Stop latency for a running forecast | ≤ 5 s from click to `Cancelled` | Restored dev instance, slowest Portfolio, 3 tries | > 10 min (report) |
| KPI-3 | Slowest forecast wall time | ≥ 50% lower `RefreshLog.DurationMs` for the slowest Portfolio forecast | Same DB before slice 01 and after slice 04 | Measured at the start of slice 01 |
| KPI-4 | Forecasts that show a guessed date | 0 percentiles projecting to today or computed from finished runs only | Slice 03 ACs plus the before/after list (AC-03.8) | Unknown count today (S9, S10) |
| KPI-5 | "Had to restart Lighthouse during updates" reports | 0 in the 60 days after release | Support and community reports, against the 60 days before | ≥ 1 (this report) |
| KPI-6 | Cancel all in use | Counted, no target in the first 60 days | Usage-data event if DEVOPS adds one (checklist item 4) | n/a |

## Wave: DISCUSS / [REF] Definition of Done

1. Every slice's ACs pass as automated tests (NUnit; Vitest for web; the client package's tests for the
   clients change).
2. `dotnet build` with zero warnings; `dotnet test` green on the non-connector filter.
3. `pnpm test` green; `pnpm build` with zero errors and zero warnings; Biome clean.
4. No new SonarQube Cloud issue.
5. Stryker.NET and Stryker (frontend) per feature, ≥ 80% kill rate, run last on frozen code.
6. Docs updated at finalization: `docs/settings/taskmanager.md` (Cancel all; Stop on forecasts),
   `docs/concepts/howlighthouseforecasts.md` (5-year horizon, what "more than 5 years" means, the write-back
   behaviour from NMD-2), and every page that explains forecast dates or Delivery likelihood (grep at
   finalize).
7. Per-feature screenshots: the Task Manager with Cancel all; a Feature showing "more than 5 years" (demo
   data needs a low-throughput Team — check at DISTILL).
8. Lighthouse-Clients release rendering the marker (NMD-1 option c), with the server-version gate.
9. `ARCHITECTURE.md` / `brief.md` updated if the simulation's threading or the beyond-horizon
   representation becomes an ADR.
10. Release notes written from the user's pain (pages hung, Stop did nothing, restart), with the reporter
    credited. No customer identifiers.

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Evidence |
|---|---|---|
| 1 | Problem in domain language | Each story opens with a named persona and the incident's symptoms |
| 2 | Persona with specifics | Ingrid (Docker, 4 cores, 14 Teams); Tomás (delivery lead, low-throughput Team) |
| 3 | 3+ domain examples | US-01: 3, US-02: 4, US-03: 5, US-04: 6 |
| 4 | UAT G/W/T, 3–7 per story | 4 / 4 / 5 + 3 / 5 |
| 5 | AC from UAT | 5 / 7 / 12 / 7, each tied to a scenario |
| 6 | Right-sized | Slices are 3h to 1 day; 5 slices, about 4 days |
| 7 | Technical notes | Inventory S1–S18 with file:line; DESIGN questions 1–9 |
| 8 | Dependencies tracked | D7, D11; Epic #6298 coordination; NMD-1/2 |
| 9 | Outcome KPIs | KPI-1..6 with targets and methods |

**DoR: PASSED**. NMD-1/2 were answered by the maintainer on 2026-10-17 (c and a). Requirements completeness
about 0.95. The open gap is the DISTILL copy. Per-wave peer review skipped: no trigger fired
beyond those decisions, and the consolidated review runs at the end of DISTILL. Expansion triggers: none (2
personas, 2 contexts, no regulatory terms, WS strategy C).

---

## Wave: DISCUSS / [REF] Project DISCUSS Checklist

1. **RBAC** — no new permission. The new Cancel all endpoint takes
   `[RbacGuard(RbacGuardRequirement.SystemAdmin)]`, like `CancelTask`. The control sits inside the popover
   that `TaskManagerIcon` gates on `useRbac().isSystemAdmin` (S17), and nothing fetches `my-summary`.
   Forecast changes alter rendered values, not who sees them.
2. **Lighthouse-Clients CLI/MCP** — **impacted by slice 03.** Forecast percentiles are read by
   `forecastWording`, `featureWording`, `deliveryWording` and `cli/forecastOutput` (S12). The change is
   additive (NMD-1); a clients minor release renders it, gated on the server version. The Task Manager is not
   exposed to clients, so Cancel all has no client impact.
3. **Website** — **N/A, because** this fixes a failure mode and makes an existing number more honest. It is
   release-notes material, not a new capability to advertise.
4. **Usage-data event** — **open for DEVOPS.** Candidate: a name-only `TaskManagerCancelAll` (KPI-6). A
   beyond-horizon event is not proposed; KPI-4 is verified by tests, not telemetry. Slices 01, 02 and 04
   have no user action to count.
5. **Docs at finalization** — `docs/settings/taskmanager.md`, `docs/concepts/howlighthouseforecasts.md`, and
   the forecast-date and likelihood pages found by grep (DoD 6). Plus `docs/settings/usagedata.md` if an
   event is added.

---

## Wave: DISCUSS / [REF] Open Questions

### For DESIGN

1. The bound: `Environment.ProcessorCount` (respects container CPU limits in .NET) ÷ 2, minimum 1. Is
   that right on a 2-core host (1 worker)? Is it applied to every caller (S2)?
2. Thread-pool versus CPU starvation: keep `Task.Run` plus `Parallel.For`, or use dedicated threads? Slice
   01's production AC tells which one mattered.
3. How the token reaches the loop: read from `UpdateCancellationContext` inside the forecast service, or
   passed down from `ForecastUpdater`. A check between runs via `ParallelOptions.CancellationToken` is the
   obvious candidate.
4. Representing beyond-horizon in the histogram: a sentinel day (horizon + 1) in the persisted
   `IndividualSimulationResult` rows, or a counted field (expand-only migration). The effect on
   `JointCompletionDistribution`, `ComonotonicCompletionDistribution`, `StartForecast`, pinned closure
   records and `DeliveryMetricSnapshot` history.
5. The horizon's unit: simulated days are working days projected over blackouts (`ProjectWorkingDays`), so
   1,826 simulated days can land later than 5 calendar years. Pick the count that keeps the "5 years" copy
   true.
6. Wire shape (NMD-1).
7. Cancel all mechanics: enumerate admitted keys from the status store and reuse per-key publish; drop
   `pendingReruns` and `heldUpdates` on every replica (they are in-process) and leave their rounds, as
   `AbandonUnqueuedWork` does. `UpdateController` already carries an S6960 pragma, so judge whether a fourth
   route belongs there.
8. Race: work admitted between the list read and the server handling Cancel all. Recommendation: cancel
   what is admitted at the moment the server handles it.
9. Whether slice 04 lowers the probes' and benchmarks' limits, or only the default (S18).

### For the DISTILL UI walk-through (maintainer, one decision at a time)

- **UI-1 "more than 5 years" wording**: "More than 5 years" / "> 5 years" / "Not within 5 years" /
  "After {date 5 years out}". Per-percentile label versus a single Feature-level note. Tooltip text. How it
  differs from the existing "Cannot forecast".
- **UI-2 each surface**: the Feature list forecast cell (`ForecastInfo`), the start-date column, the manual
  forecast result, the Delivery chip "85%: …", the export table cell, the timeline bar (what does a bar do
  with no end?), the predictability chart point, the PDF export (#6298).
- **UI-3 the Cancel all control**: placement (Activity section heading or popover footer), label ("Cancel
  all" / "Stop all"), icon, whether it reuses `IconButton` + `Tooltip` from `ActivitySection`, and
  disabled/hidden when there is nothing cancellable or only deletes.
- **UI-4 confirmation**: confirm dialog or none. The cost is low (scheduled runs carry on), but it is
  one click for N rows. Whether the tooltip says "scheduled updates will carry on".
- **UI-5 after the click**: the running row reads `Stopping…` (existing); delete rows stay. Do they need a
  hint that they cannot be cancelled?

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

- **Key decisions**: D3 (bound, every simulation), D4 (Stop reaches the loop; last forecast kept), D5+D6
  (5 years; per-percentile, unfinished runs counted), D7 (honest counting before the lower horizon), D8
  (Cancel all includes follow-ups and holds; deletes exempt), D9 (System Admin), D12 (additive wire), D13
  (no wall-clock ACs).
- **Requirements summary**: the instance stays usable and stoppable while it forecasts, and a forecast that
  cannot finish in 5 years says so. Feature type cross-cutting; no walking skeleton.
- **Constraints**: one simulation method serves background and HTTP; cancellation is cooperative; the
  clients read `expectedDate` as a required day; the queue stays a single lane.
- **Upstream changes**: no DISCOVER or DIVERGE ran. Story #6309's "show more than 5 years" is refined by
  D6 (per percentile, unfinished runs counted). The code's own claim that no data can reach the horizon
  (`HowTheRunEnded.cs:14-19`) is wrong (S8). The comment is corrected when slice 03 touches the file.

## Wave: DISCUSS / [REF] SSOT Updates

| File | Change |
|---|---|
| `docs/product/jobs.yaml` | New `job-operator-keep-lighthouse-usable-while-it-forecasts`; `epic-6311-responsive-during-updates` added to `feature_context`. Reused jobs unchanged. |
| `docs/product/journeys/epic-6311-responsive-during-updates.yaml` | New journey `use-and-steer-lighthouse-while-it-updates`. |
| `docs/product/personas/platform-operator.yaml` | New job appended to `primary_jobs`. |

## Wave: DISCUSS / [REF] Handoff

**To** `nw-solution-architect` (DESIGN): this file, the slice briefs, NMD-1/2 (answered first), and DESIGN
questions 1–9. **To** `nw-platform-architect` (DEVOPS): KPIs and the usage-data candidate. Expected to be a
near no-op beyond the event decision.

---

## Wave: DESIGN / [REF] DDD list

DESIGN, 2026-10-18, Morgan (propose mode). Application scope; no migration, no new package, no new external system.

| ID | Decision | ADR |
|---|---|---|
| DDD-1 | The When/Start simulation runs on at most max(1, ⌊cores ÷ 2⌋) workers. The bound is carried by `ForecastSimulationLimits` and derived from `Environment.ProcessorCount` (which honours container CPU limits). The core count is injectable. `Task.Run` + `Parallel.For` stay. The startup banner states the bound | 248 |
| DDD-2 | `ForecastUpdater` reads the update's token and passes it to `IForecastService.UpdateForecastsForPortfolio(portfolio, token)`, and from there to `ParallelOptions.CancellationToken`. The check sits between runs. Other callers pass none | 249 |
| DDD-3 | `TotalTrials` counts every run simulated for a row; the histogram keeps finished days only. Beyond = `TotalTrials − Σ histogram`. Runs ended by a dependency circle count as beyond. Rows with no throughput keep `TotalTrials = 0` ("Cannot forecast") | 250 |
| DDD-4 | A percentile is read through one value object (day / beyond / no answer). `-1` stops meaning two things. `HowManyForecast` is unchanged | 250 |
| DDD-5 | The combinators stay closed. `AggregatedWhenForecast` and `DeliveryCompletionForecast` feed each contributor's unfinished runs in as a virtual last bucket and strip it from the result | 250 |
| DDD-6 | The horizon is `ForecastHorizon`: 5 years → 1,826 simulated days. It is the run cap in `Default` and the source of the copy's years | 251 |
| DDD-7 | The wire carries `beyondHorizon` + `horizonYears` on `WhenForecastDto` and `beyondHorizon` on `WhenDistributionPointDto`. A beyond `expectedDate` = today + horizon days (calendar) | 252 (NMD-3) |
| DDD-8 | Blackout-window sizing by the 95th percentile uses the horizon when that percentile is beyond | 252 |
| DDD-9 | The Jira Release block writes "85%: more than 5 years" | 254 (NMD-4) |
| DDD-10 | Cancel all = `POST api/latest/update/tasks/cancel` (SystemAdmin, 204, idempotent) → one cancel-all broadcast. Each replica drops its non-delete follow-ups, then every hold (leaving their rounds), then stops its own non-delete keys through the per-key path | 253 |
| DDD-11 | Race: what a replica holds when the message arrives is cancelled; later work runs | 253 |
| DDD-12 | Only `ForecastSimulationLimits.Default` changes; fixtures that read the cap from `Default` follow it | 251 |
| DDD-13 | Web: one model field set on `WhenForecast` and one shared formatter for every S12 surface. `Feature.ts:138` and `services/Api/ForecastService.ts:104` carry the flag through | 252 |

## Wave: DESIGN / [REF] Component decomposition

| Component (path) | Change |
|---|---|
| BE `Models/Forecast/ForecastHorizon.cs` | **new** |
| BE `Models/Forecast/` percentile value object | **new** (one type) |
| BE `Models/Forecast/ForecastSimulationLimits.cs`, `ForecastBase.cs`, `WhenForecast.cs`, `StartForecast.cs`, `AggregatedWhenForecast.cs`, `DeliveryCompletionForecast.cs` | modified |
| BE `Models/Forecast/JointCompletionDistribution.cs`, `ComonotonicCompletionDistribution.cs`, `CompletionHistogram.cs` | **unchanged** |
| BE `Models/SimulationResult.cs`, `Models/Feature.cs` (start answer), `Models/Delivery.cs`, `Models/DeliveryMetricsProjection.cs`, `Models/DeliverySources/DeliveryForecastBlock.cs` | modified |
| BE `Services/Implementation/Forecast/ForecastService.cs`, `HowTheRunEnded.cs` (comment), `Services/Interfaces/Forecast/IForecastService.cs` | modified |
| BE `Services/Implementation/BackgroundServices/Update/ForecastUpdater.cs` | modified |
| BE `API/DTO/WhenForecastDto.cs`, `DtoExtensions.cs`, `DeliveryWithLikelihoodDto.cs`, `DeliveryMetricsHistoryDto.cs`, `Archived/ArchivedDeliveryProjection.cs`; `Services/Implementation/DeliveryMetricValuesProjector.cs` | modified |
| BE `API/FeatureForecastWindow.cs`, `API/ForecastController.cs` | modified |
| BE `Services/Implementation/WriteBackTriggerService.cs` | modified (behaviour unchanged) |
| BE `Services/Implementation/DeliverySources/DeliveryForecastBlockRenderer.cs`, `DeliveryForecastPublishingService.cs` | modified |
| BE `Services/Interfaces/Update/IUpdateQueueService.cs`, `IUpdateCancellationNotifier.cs`; `…/Update/UpdateQueueService.cs`, `AdmittedCancellations.cs`, `InProcessUpdateCancellationNotifier.cs`, `RedisUpdateCancellationNotifier.cs` | modified |
| BE `API/UpdateController.cs` | modified (4th route) |
| BE startup banner | modified (one line) |
| FE `models/Forecasts/WhenForecast.ts`, `forecastSchemas.ts`, `models/Feature.ts`, `models/Delivery/ArchivedDelivery.ts`, `models/Delivery/DeliveryMetricsHistory.ts`, `services/Api/ForecastService.ts` | modified |
| FE `ForecastInfo.tsx`, `DeliverySection.tsx`, `deliveryExportTable.ts`, `deliveryTimelineModel.ts`, `DeliveryPredictabilityChart.tsx` + one shared formatter | modified (+1 util) |
| FE `components/App/Header/TaskManager/ActivitySection.tsx`, `useTaskManagerPopover.ts`, `services/UpdateSubscriptionService.ts` | modified |
| Clients `forecastWording.ts`, `featureWording.ts`, `deliveryWording.ts`, `cli/src/forecastOutput.ts`, schemas | modified (clients minor, at finalize) |

## Wave: DESIGN / [REF] Driving ports

| Port | Change | Slice |
|---|---|---|
| Feature, Delivery, manual-forecast, snapshot-history, archived-Delivery GETs | additive `beyondHorizon` (+ `horizonYears` on percentiles) | 03 |
| `POST api/latest/update/tasks/{type}/{id}/cancel` | unchanged; now effective on a running forecast | 02 |
| `POST api/latest/update/tasks/cancel` | **new**, `[RbacGuard(SystemAdmin)]`, 204, idempotent | 05 |
| `IForecastService.UpdateForecastsForPortfolio` | + `CancellationToken` | 02 |
| `IUpdateQueueService` | + `CancelAllAsync()` | 05 |
| Lighthouse-Clients CLI/MCP | read the flag when present | 03 (finalize) |

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter(s) | Change |
|---|---|---|
| `IUpdateCancellationNotifier` | `InProcessUpdateCancellationNotifier`, `RedisUpdateCancellationNotifier` | + cancel-all publish/subscribe; Redis on a channel of its own |
| `IUpdateStatusStore` | in-process / Redis | unchanged (`CancelIfStillWaiting` reused) |
| `IRepository<Feature>` | EF | unchanged; save not reached on cancel |
| `IWriteBackCollector` | — | unchanged; nothing staged on cancel or for beyond |
| `IDeliveryForecastPublisher` | Jira | unchanged port; block text changes (NMD-4) |
| `IRefreshLogService` | EF | unchanged |

## Wave: DESIGN / [REF] Technology choices

None new. .NET `ParallelOptions` (BCL), the existing StackExchange.Redis pub/sub, System.Text.Json, zod. ArchUnitNET
(already in the test project) gets one rule: `Lighthouse.Backend.Models` does not depend on
`Services.Implementation.BackgroundServices.Update`.

## Wave: DESIGN / [REF] Decisions table

| # | Question | Decision | Status |
|---|---|---|---|
| Q1 | The bound | max(1, ⌊cores ÷ 2⌋), every When/Start simulation | recommendation |
| Q2 | Pool vs dedicated threads | keep the pool; dedicated threads only if AC-01.5 fails | recommendation |
| Q3 | Token path | explicit from `ForecastUpdater` (S107 rules out injecting it into `ForecastService`) | recommendation |
| Q4 | Representation | `TotalTrials` = all runs; no migration | recommendation |
| Q5 | Unit | 1,826 simulated days; copy always true | recommendation |
| Q6 | Wire | NMD-3 a | **maintainer** |
| Q7 | Cancel all | one broadcast, local apply | recommendation |
| Q8 | Race | cancel what is held when the message arrives | recommendation |
| Q9 | Fixtures | only `Default` changes | recommendation |
| F-1 | Release block | NMD-4 a | **maintainer** |
| F-3 | Window sizing | horizon for a beyond 95th | recommendation |
| F-4 | Circle runs | count as beyond | recommendation |
| F-5 | Concurrent simulations | per-call bound; no global gate | recommendation |
| F-6 | Release packaging | NMD-5 | **maintainer** |
| — | Early stop for a whole forecast | NMD-6: not now | **maintainer** |

**Loading and failure looks.**
- **"More than N years"** adds no data fetch. Each host keeps its existing looks (WidgetShell `status`, keyed TanStack
  queries). A beyond percentile is never drawn as a date, as an empty cell, or as "Cannot forecast".
- **Cancel all** has four looks:
  - *idle*: enabled when there is at least one cancellable row;
  - *in flight*: "Cancelling…" with a spinner, and every cancellable row reads "Stopping…" through `stopAsked`;
  - *failed*: an inline "Could not cancel. Try again." line, and the rows revert, as per-row Stop does;
  - *done*: the list is re-read ("Nothing is being refreshed right now."; deletes stay).
- The list keeps "keep what was there on a failed read". Sketches are in the proposal; copy and placement are settled
  at DISTILL UI-3..5.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing component | File | Overlap | Decision | Justification / contract shape |
|---|---|---|---|---|
| `ForecastService` | `Services/Implementation/Forecast/ForecastService.cs` | the simulation | EXTEND | options + runs-simulated total. Bounded-change: only the passed Features' forecasts; ∅ on cancel |
| `ForecastSimulationLimits` | `Models/Forecast/` | limits | EXTEND | + bound; cap from `ForecastHorizon`. Pure value |
| `ForecastBase` / `WhenForecast` / `StartForecast` | `Models/Forecast/` | percentile reads | EXTEND | `TotalTrials ≥ Σ`; value-object read. Pure |
| `AggregatedWhenForecast`, `DeliveryCompletionForecast` | `Models/Forecast/` | combinator callers | EXTEND | virtual bucket in/out. Pure |
| `JointCompletionDistribution`, `ComonotonicCompletionDistribution`, `CompletionHistogram` | `Models/Forecast/` | combine maths | REUSE unchanged | the maths and bit-identity are kept |
| `WhenForecastDto`, `WhenDistributionPointDto`, `DeliveryWhenPercentile` | `API/DTO/`, `Models/` | wire | EXTEND | additive |
| `WriteBackTriggerService` | `Services/Implementation/` | write-back | EXTEND | already writes nothing for "no answer" |
| `DeliveryForecastBlockRenderer` | `Services/Implementation/DeliverySources/` | Release text | EXTEND | NMD-4 |
| `ForecastUpdater`, `IForecastService` | `…/Update/`, `Services/Interfaces/Forecast/` | the update | EXTEND | token; one caller |
| `UpdateQueueService`, `AdmittedCancellations`, notifiers | `…/Update/` | per-key cancel | EXTEND | Bounded-change: local sources and rows of non-delete keys, non-delete follow-ups, all holds and their rounds. Deletes untouched |
| `UpdateController` | `API/` | Task Manager HTTP | EXTEND | same subsystem, same guard |
| `ActivitySection`, `useTaskManagerPopover`, `UpdateSubscriptionService` | FE | per-row Stop | EXTEND | reuse `IconButton` / `Tooltip` / `stopAsked` |
| `WhenForecast` (FE) | `models/Forecasts/` | percentile model | EXTEND | one formatter for all surfaces |
| `ForecastHorizon` | `Models/Forecast/` | — | **CREATE NEW** | nothing holds "the horizon in years" that both the simulation and the copy can read |

## Wave: DESIGN / [REF] Open questions

**For DISTILL** (UI walk-through first):
- UI-1..UI-5 as listed.
- UI-2's timeline bar for a beyond end: open-ended to the window's right edge is suggested. A beyond percentile sizes the
  window to the horizon (DDD-8), no further.
- The demo data needs a low-throughput Team for the screenshot (DoD 7).
- Confirm with the maintainer: a Delivery whose rows have only unfinished runs now shows "more than 5 years" with
  likelihood 0, where it used to show "cannot forecast" (Changed Assumption 2).

**For DELIVER:**
- If AC-01.5 still shows pages hanging, move to dedicated threads (ADR-248's fallback).
- If AC-02.7 misses KPI-2, add a checkpoint inside a run (ADR-249's fallback).
- If AC-04.4 misses, design the early stop for a whole forecast (NMD-6).

## Wave: DESIGN / [REF] Changed Assumptions

1. **S2** said: *"Every simulation goes through that one method: … and the reality-check and backtest probes."*
   Reality checks and backtests use `HowMany` (`Services/Implementation/Forecast/ForecastRealityCheckService.cs:90`,
   `API/ForecastController.cs:137,220`), a separate single-threaded loop with no horizon. The bound covers every
   When/Start simulation (background, manual `When`, multi-team). US-01's "manual forecast bounded the same way" holds
   for `When` only.
2. **S10** said: *"When no run finishes, the API shows today's date."*
   - True for the raw DTO date only. A Feature row with no finished run has `TotalTrials = 0`, so the Feature reads
     "Cannot forecast" (`Models/Feature.cs:189-216`) and a Delivery reads "cannot forecast"
     (`Models/Forecast/DeliveryCompletionForecast.cs:29`).
   - After DDD-3 both read "more than 5 years" (likelihood 0).
3. **S11** listed `ForecastPredictabilityScore` and `ForecastRealityCheckService` as consumers of a forecast
   percentile. Both read `HowManyForecast` (`Models/Forecast/ForecastPredictabilityScore.cs:7`); neither is touched.
4. **NMD-1 c** said *"gated on server version"*. The gate is now on field presence (NMD-3). This refines c and does
   not reverse it.
5. **Pre-requisites** named only Epic #6298 as a second renderer of forecast dates. The Jira Release block
   (`Services/Implementation/DeliverySources/DeliveryForecastBlockRenderer.cs`) is a second tracker-facing write (NMD-4).
6. **D7** said *"Slice 03 … ships before slice 04"*. Build order unchanged; release together (NMD-5).

## Wave: DESIGN / [REF] Handoff to DEVOPS

- **Usage data:** the candidate is still a name-only `TaskManagerCancelAll` (KPI-6), for DEVOPS to decide. No forecast
  event.
- **KPIs:** KPI-1..5 are measured on the restored dev instance with Docker capped to 4 CPUs. The startup banner now
  states the forecast's worker bound, which makes KPI-1's setup checkable.
- **Infrastructure:** one more Redis pub/sub channel on the existing connection, used only when Redis is configured. No
  migration; no new configuration key.
- **External integrations needing contract tests:**
  - Jira Release description (forecast block, existing publisher): a renderer and merge test with a beyond percentile.
  - Lighthouse-Clients ← Lighthouse API (additive `beyondHorizon` / `horizonYears`): a consumer-driven schema test in
    the clients repo against a recorded server payload, with and without the fields.
- **Paradigm:** OOP, ports and adapters (unchanged).
- **Mutation testing:** per feature, both stacks, run last on frozen code.
