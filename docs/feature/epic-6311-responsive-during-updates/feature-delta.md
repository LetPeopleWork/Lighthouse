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
  pinned closure records) gains `beyondHorizon: boolean`, and `horizonYears` too (NMD-12).
- For a beyond percentile, `expectedDate` is a stand-in about 5 years out. *(Revised at the review gate, 2026-10-18: it is `today.AddYears(horizonYears)`, see DDD-7.)*
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

**Decided: not now** (maintainer, 2026-10-18). The per-run cap of 1,827 simulated days (revised from 1,826 at the review gate, DDD-6) is the kill switch.
- **Why:** stopping the whole forecast early changes how many runs stand behind every percentile and likelihood. Stop
  (slice 02) already covers the case a user sees.
- **Deferred follow-up:** designed only if AC-04.4 misses (the slowest Portfolio forecast at least 50% faster).

### NMD-7 — No usage-data event for Cancel all

**Decided: no event** (maintainer, 2026-10-18, at DEVOPS). The candidate `TaskManagerCancelAll` is not added.
- **Effect:** KPI-6 (Cancel all in use) is **dropped as unmeasured**; it had no target. `UsageDataEventName` and
  `docs/settings/usagedata.md` are untouched, and no DELIVER step emits anything.

### NMD-8 — What the docs say about replicas and cores

**Decided** (maintainer, 2026-10-18, at DEVOPS):
- The slice-05 docs do **not** mention that a replica still on the old version ignores Cancel all during a
  rolling upgrade. The window lasts only as long as the rollout, and per-row Stop still reaches that replica.
- The docs **do** name `DOTNET_PROCESSOR_COUNT` in one line: it is .NET's own setting for the core count, and
  through it an operator can change the forecast's half-the-cores limit without a Lighthouse setting. This is
  a slice-01 finalize item.

## Maintainer decisions — 2026-10-18, DISTILL UI walk-through

The UI walk-through settled UI-1 to UI-5 one decision at a time; each took the recommended option. DELIVER
re-sketches only what is left open here (nothing is).

### NMD-9 — UI-1, the words for a percentile beyond the horizon

**"More than 5 years"**, the 5 read from `horizonYears`. It replaces the date wherever one percentile's date
is shown as text: the Feature list forecast cell (`ForecastInfo`), the forecasted-start column, the manual
forecast result, the Delivery chip ("85%: More than 5 years"), the export table cell and the PDF export. It
matches the Jira Release block (NMD-4). The level icon stays. The tooltip keeps the level line and adds why:

```
● 50%  14 Mar 2029
● 70%  02 Nov 2030
● 85%  More than 5 years      tooltip:  Confident (85% Chance)
● 95%  More than 5 years                Fewer than 85% of simulated runs
                                        finished within 5 years.
```

It stays distinct from "Cannot forecast", which still means a Team has no throughput history at all.

### NMD-10 — UI-2, the two surfaces that draw rather than print

- **Delivery timeline:** a Feature whose end at the chosen percentile is beyond the horizon gets no bar. It goes
  in the existing list of Features not on the timeline, with the reason "Ends more than 5 years out at 85%".
- **Delivery predictability chart:** a day on which a percentile was beyond the horizon has no point, so that
  percentile's line breaks there. It is the chart's existing path for a day with no value. The tooltip for
  that day reads "85%  More than 5 years".

### NMD-11 — UI-3 to UI-5, Cancel all

- **Control:** a small text button, "Cancel all", at the right end of the Activity heading. `SectionHeading`
  gains an optional action. It is shown to System Admins only, and **hidden** when no row could be cancelled
  (an empty list, or removals only).
- **No confirm dialog.** The tooltip says: "Stops everything running or queued, except removals. Scheduled
  updates carry on as usual."
- **After the click:** every cancellable row reads `Stopping…` (the existing per-row copy). Removal rows stay
  as they are, with no extra hint. The button reads "Cancelling…" with a spinner and is disabled while the
  request is in flight. On failure the rows go back to how they were, and "Could not cancel. Try again."
  shows under the heading, the same way a per-row failure reverts. When the list is next read, only the
  removals remain.

```
in flight                                 failed
↻ Activity             [Cancelling… ◌]    ↻ Activity                  [Cancel all]
  ◌ … Team 'Payments' — Stopping…           ⚠ Could not cancel. Try again.
  ◌ … Portfolio 'PM' — Stopping…            ◌ … Team 'Payments' — Running    ⊗
  ⧗ Removing Team 'Old' — Queued            ⧗ … Portfolio 'PM' — Queued      ⊗
                                            ⧗ Removing Team 'Old' — Queued
```

### NMD-12 — `horizonYears` also travels on the Delivery history points

**Decided** (maintainer, 2026-10-18, raised by DISTILL): `WhenDistributionPointDto` carries `horizonYears`
beside `beyondHorizon`, as NMD-3 gave `WhenForecastDto`. Every view, including the predictability chart and
the archived Delivery views, then reads the number from the data rather than holding a second "5". It is
stored in the existing JSON column, so no migration. An archived Delivery keeps the horizon it was
forecast with.

### NMD-13 to NMD-16 — answers to the final review gate

**Decided** (maintainer, 2026-10-18), each the recommended option:
- **NMD-13, dependency circles:** a run that ends because Features wait on each other in a circle counts as
  beyond the horizon, so its percentiles read "More than 5 years". It did not finish within 5 years, which
  is true. The error log that names the circle stays, so the admin can fix the dependency. Only the code
  comment that calls those dates "not a forecast" is corrected.
- **NMD-14, the core limit is per simulation.** A manual forecast during a background one gets its own half
  of the cores, so the two can briefly use all of them. That is accepted: a manual forecast is one Team and
  finishes in seconds, and a shared gate would make it wait behind a long background run. The invariant
  says "per simulation".
- **NMD-15, Cancel all is delivered at most once.** A replica reconnecting to Redis at that moment misses
  the broadcast and keeps its work. That is accepted and stated as the guarantee: the list still shows the
  work, so the admin clicks again or stops the row. Per-row Stop already behaves the same way.
  **Scope note (maintainer):** the Redis multi-replica setup was a proof of concept that nobody is known to
  run. Cancel all must not break it, so the Redis notifier gets the cancel-all message with one simple
  real-Redis test, and nothing more: no hardening, no extra rolling-upgrade analysis, no docs about it.
- **NMD-16, old binaries after a rollback or during a rolling upgrade** read a percentile beyond the horizon
  as today's date until the Portfolio's next forecast rewrites it. That is accepted as bounded and
  self-healing. The release and rollback notes tell the operator to run a forecast update for every
  Portfolio after a rollback. Write-back writes nothing for those percentiles.

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

**PASS — right-sized.** 5 user stories across three ADO children (US-01 and US-02 under Bug #6308, US-03
and US-05 under Story #6309, US-04 under Story #6322), 5 slices, one story per slice, 2 contexts (forecast simulation; update
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
| Jira Release forecast block (a Delivery that broadcasts its forecast) | A beyond-horizon percentile's line reads "85%: More than 5 years" (NMD-4) | 03 |
| Simulation horizon (`ForecastSimulationLimits.Default`) | 100,000 days → 5 years | 04 |
| Header → Task Manager → **Cancel all** (new control); new endpoint (shape decided in DESIGN, e.g. `POST /api/latest/update/tasks/cancel`) | Stops the running update, clears the queue, follow-ups and holds | 05 |
| Lighthouse-Clients CLI/MCP | Renders the flag when `beyondHorizon` is present (clients release, NMD-1 c as refined by NMD-3) | 03 (finalize) |

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
forecast keeps all four cores busy and leaves nothing to answer the pages with (S1).

#### Elevator Pitch

```
Before: while a Portfolio forecast runs, every page in Lighthouse hangs; navigation shows a blank screen.
After:  open Portfolios → "Platform Modernisation" while its forecast is running → the Features list
        loads and the Task Manager row reads "Forecasting Portfolio 'Platform Modernisation' — Running".
Decision enabled: Ingrid waits for the forecast to finish instead of restarting the container.
```

**Domain examples.** (1) Ingrid, 4 cores: the forecast uses at most 2; the Teams page loads during it.
(2) A Raspberry-Pi-class 1-core host: the bound is 1, never 0, and forecasts still finish. (3) A 32-core SaaS
replica: at most 16 workers. The manual forecast a Team page runs is bounded the same way, with its own half
of the cores: the bound is per simulation, so a manual forecast during a background one can briefly use all
of them together (NMD-14).

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
- **AC-01.1** While a Portfolio forecast is still running, a request for the Portfolios list is answered.
  Pass when the answer arrives before the forecast has finished; elapsed time is never the measure (D13).
- **AC-01.2** A single forecast never has more than max(1, ⌊cores ÷ 2⌋) runs in progress at once: 2 on a
  4-core host, 16 on a 32-core host.
- **AC-01.3** With 1 core, the bound is 1 and the forecast completes.
- **AC-01.4** Seeded percentiles are identical bounded and unbounded. Runs are already combined
  independently of how they were split across workers (`ForecastService.cs:214-219`).
- **AC-01.5 (production data, manual gate)** On the dev instance restored from the production backup
  (`Restore-DbBackup.ps1`) and capped to 4 CPUs in Docker, while the slowest Portfolio forecast runs, the
  Portfolios, Teams and Feature pages each load. **Pass** when the p95 of 20 `GET /api/latest/portfolios`
  calls is under 1 s; **fail** otherwise. The reading goes in the slice notes (KPI-1).

**Outcome KPI**: KPI-1. **Technical notes**: one method (S2). The tests hold the forecast open with a draw
stream that blocks, count the runs in progress with an instrumented draw stream, and inject the core count,
so no assertion depends on the machine's cores or clock (DDD-1). The bound is per simulation (NMD-14). Whether
the bound honours container CPU limits, and whether the `Task.Run` wrapper stays, were DESIGN questions 1–2
(DDD-1). **Dependencies**: none.

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
- *Scenario: A forecast stops between runs* — Given Ingrid's "Platform Modernisation" forecast is running
  and has started run 3,200 of 10,000, When she clicks stop on its row, Then no further run starts and the
  row reaches `Cancelled`.
- *Scenario: The previous forecast is kept* — Given the Feature "Checkout Redesign" in "Platform
  Modernisation" shows "85%: 02 Nov 2027" from yesterday's forecast, When today's forecast is stopped
  part-way, Then "Checkout Redesign" still shows "85%: 02 Nov 2027".
- *Scenario: The tracker keeps the previous forecast dates* — Given forecast write-back of the 85% to the Jira
  field "Forecast end", which reads 2027-11-02 for "Checkout Redesign", and the Portfolio refresh in the same
  round has staged a status write, When the forecast is stopped, Then "Forecast end" still reads 2027-11-02
  and the status write still reaches Jira.
- *Scenario: The history says it was stopped* — Given Ingrid stopped the "Platform Modernisation" forecast
  at 10:42, When she opens Settings → System Info → refresh history, Then the 10:42 forecast run is listed
  as cancelled, not as failed or completed.
- *Scenario: A manual forecast is not affected by Stop* — Given no update is running for the Payments Team,
  When Tomás runs a manual forecast for 30 Work Items on the Payments Team page, Then it runs to the end and
  shows its 50%, 70%, 85% and 95% dates, as it does today.

**Acceptance criteria.**
- **AC-02.1** After the cancel is observed, no new run starts: the count of runs begun is at most the count
  at the cancel plus one in flight per worker.
- **AC-02.2** The update reaches `Cancelled` (not `Completed`, not `Failed`).
- **AC-02.3** The Features' persisted forecasts (histogram rows and `CreationTime`) are unchanged.
- **AC-02.4** No forecast write-back field update is staged; writes already staged by other work in the
  round are flushed.
- **AC-02.5** `RefreshLog` has `Type = Forecast`, `Cancelled = true`, `Success = false`.
- **AC-02.6** A forecast that is not part of an update, such as the manual forecast on a Team page, cannot be
  stopped by a Stop and runs exactly as today.
- **AC-02.7 (production data, manual gate)** On the restored dev instance, stop the slowest Portfolio
  forecast three times. **Pass** when each try goes from click to `Cancelled` in 5 s or less and every
  Feature shows the same dates before and after; **fail** otherwise. The readings go in the slice notes
  (KPI-2).

**Outcome KPI**: KPI-2. **Technical notes**: `UpdateCancellationContext` → simulation loop (DESIGN Q3).
**Dependencies**: none mechanically. Shares a method with slice 01.

---

### US-03 — A forecast that cannot finish in time says so instead of guessing (Story #6309, A3)

`job_id`: `job-forecast-no-false-certainty` · slice 03 · released together with US-05 (slice 04) under
Story #6309 (NMD-5)

**Problem.** Tomás Reyes, a delivery lead, forecasts "Billing Migration": 480 items left, and the Payments
Team closes about one item every three weeks. A run that does not finish is dropped, so the 85% date comes
only from the luckier runs (S9). If none finish, the Feature shows **today** (S10). Tomás would quote either
of those dates.

#### Elevator Pitch

```
Before: a Feature too far out for the simulation shows a date computed from only the runs that finished,
        or today's date.
After:  open Portfolios → "Payments 2027" → Features → "Billing Migration" → sees "50%: 14 Mar 2029" and
        "85%: More than 5 years", and the same in the Delivery chips, the Jira Release and the CLI.
Decision enabled: Tomás tells the sponsor this Feature is not plannable with the Payments Team's current
        throughput, and splits it or adds capacity, instead of committing to a date.
```

**Domain examples.** (1) "Billing Migration": 62% of runs finish within the horizon → 50% shows a date,
70/85/95% show "More than 5 years". (2) "Invoice PDF": every run finishes in about 40 days → unchanged.
(3) "Legacy Ledger Retirement": no run finishes → every percentile reads "More than 5 years", never today.
(4) The Delivery "Q4 Release" on 2026-12-15, containing "Billing Migration" → its likelihood counts the
unfinished runs as missing the date. (5) Write-back of the 85% to a Jira date field for "Billing Migration" →
no write (NMD-2). (6) "Billing Migration" is worked by both the Payments and the Ledger Team; 30% of the
Payments runs and none of the Ledger runs go beyond the horizon → the Feature's 85% reads "More than 5
years", because beyond counts as later than any day when the Teams are combined. (7) "Billing Migration"
and "Invoice Ledger" wait on each other in a dependency circle → those runs count as beyond the horizon, so
the 85% reads "More than 5 years"; the error log that names the circle stays, so the admin can fix it
(NMD-13).

**UAT.** Slice 03 is built with the horizon still at 100,000 days; the words name the years the forecast
carries, which read 5 once US-05 lands, and the two release together (NMD-5).
- *Scenario: Runs that do not finish count against every percentile* — Given "Billing Migration", where 6,200
  of 10,000 runs finish within the horizon and 3,800 do not, When it is forecast, Then the 50% shows a date
  and the 70%, 85% and 95% are beyond the horizon.
- *Scenario: A Feature nobody can finish is never shown as due today* — Given "Legacy Ledger Retirement",
  where none of the 10,000 runs finish, and today is 2026-10-18, When it is forecast, Then every percentile
  is beyond the horizon and none reads 18 Oct 2026.
- *Scenario: Likelihood counts unfinished runs as late* — Given the Delivery "Q4 Release" due 2026-12-15
  holds "Billing Migration", where 2,000 of 10,000 runs finish by that date and 3,800 never finish, When
  Tomás opens the Delivery, Then its likelihood reads 20%, not the 32% that 2,000 of the 6,200 finished
  runs would give.
- *Scenario: Every forecast surface says it the same way* — Given "Billing Migration"'s 85% is beyond the
  horizon and its 50% is 14 Mar 2029, When Tomás opens the Feature list, the forecasted-start column, the
  "Q4 Release" chips and the export table, Then each shows "More than 5 years" for the 85% and "14 Mar 2029"
  for the 50%, And the timeline lists "Billing Migration" under the Features not on the timeline with "Ends
  more than 5 years out at 85%", And the predictability chart's 85% line has no point for that day (NMD-9,
  NMD-10).
- *Scenario: The tracker is not given an invented date* — Given forecast write-back of the 85% to the Jira
  field "Forecast end", which reads 2028-06-30 for "Billing Migration", When the forecast puts the 85% beyond
  the horizon, Then no update is sent and the field still reads 2028-06-30.
- *Scenario: The Jira Release says "more than 5 years"* — Given the Delivery "Q4 Release" broadcasts its
  forecast to the Jira Release "Payments 2026.4", and its 50% is 2029-03-14 while its 85% is beyond the
  horizon, When the forecast block is written, Then its lines read "50%: 2029-03-14" and "85%: More than 5
  years" (NMD-4).

**Acceptance criteria.**
- **AC-03.1** A run that does not finish within the horizon counts as beyond it, and so does a run ended by
  a dependency circle (NMD-13); neither is dropped. Percentile p is beyond the horizon exactly when fewer
  than p% of all runs finished. The circle part is traced to domain example (7).
- **AC-03.2** No percentile with zero finished runs projects to today (S10).
- **AC-03.3** A Delivery's likelihood is the share of all runs that finished by its date, unfinished runs
  included.
- **AC-03.4** The wire carries an additive beyond-horizon marker, with the number of years the horizon
  covers, on each completion and start percentile, for Features, Deliveries, manual forecasts and the
  Delivery history points (snapshots and pinned closure records, NMD-12). Existing fields keep their meaning
  (D12, NMD-1, NMD-3).
- **AC-03.5** Every web surface in S12 renders the marker with one shared wording. A pinned closure record
  and a snapshot written after this slice round-trip it, and an archived Delivery keeps the horizon it was
  forecast with (NMD-12).
- **AC-03.6** When several Teams' forecasts are combined, beyond the horizon counts as later than any day.
  Traced to domain example (6).
- **AC-03.7** Write-back resolves no value for a beyond-horizon percentile (NMD-2).
- **AC-03.8 (production data, manual gate)** On the restored dev instance, the Features whose forecasts
  change are listed, before and after, in the slice notes. **Pass** when 0 percentiles project to today and
  the maintainer has read and accepted the list; **fail** otherwise (KPI-4).
- **AC-03.9** A Delivery that broadcasts its forecast to a Jira Release writes "85%: More than 5 years" on
  the line of each beyond-horizon percentile, and the date on the others (NMD-4).

**Outcome KPI**: KPI-4. **Technical notes**: runs that do not finish are counted in `TotalTrials`, and the
likelihood (`GetLikelihood`) divides by all runs (DDD-3, DDD-4); multi-Team combination uses one bucket
later than any day (DDD-5); the wire shape is `beyondHorizon` + `horizonYears` (DDD-7); the Jira Release
block is `DeliveryForecastBlockRenderer` (DDD-9); Epic #6298 coordination. **Dependencies**: none before it;
US-05 follows it and they release together (D7, NMD-5).

---

### US-05 — A forecast never simulates more than five years (Story #6309, A3)

`job_id`: `job-forecast-no-false-certainty` · slice 04 · released together with US-03 (slice 03) under
Story #6309 (NMD-5). Numbered US-05 because US-04 is already Cancel all (slice 05).

**Problem.** Every night Ingrid's "Payments 2027" Portfolio forecast runs for minutes, because each run of
"Billing Migration" may step through up to 100,000 simulated days, about 274 years, before it gives up (S7).
That time buys an answer nobody can use: a date centuries away. Tomás waits for the refreshed dates before
his sponsor meeting.

#### Elevator Pitch

```
Before: the slowest Portfolio forecast spends minutes simulating centuries for a Feature nobody can plan.
After:  Task Manager → "Forecasting Portfolio 'Payments 2027' — Running" leaves the list in at most half
        the time it took before, and Features → "Billing Migration" reads "85%: More than 5 years".
Decision enabled: Tomás refreshes the forecast before the sponsor meeting and takes the "More than 5 years"
        to it as the reason to split "Billing Migration".
```

**Domain examples.** (1) "Billing Migration": no run steps past 1,827 simulated days, and its 85% reads "More
than 5 years". (2) "Invoice PDF": every run finishes in about 40 days → the same dates as before. (3) "Data
Warehouse Sunset": a run that finishes on simulated day 1,800 counts as finished, and its date, about 4.9
years out, is shown as a date. (4) On 2026-10-18, a released CLI that does not know the marker shows
"Billing Migration"'s 85% as 2031-10-18, the stand-in five years out, until it upgrades (NMD-1, DDD-7).

**UAT.**
- *Scenario: A Feature too far out reads "More than 5 years"* — Given "Billing Migration", where 62% of runs
  finish within 5 years, When the "Payments 2027" Portfolio is forecast, Then its 50% shows a date and its
  85% reads "More than 5 years".
- *Scenario: Near-term Features are unchanged* — Given "Invoice PDF", whose runs all finish within about 40
  days, and its 50%, 70%, 85% and 95% dates recorded before the change, When it is forecast with the
  five-year horizon, Then all four dates equal the recorded ones.
- *Scenario: The long forecast is shorter* — Given the "Payments 2027" Portfolio, whose "Billing Migration"
  runs used to step up to 100,000 simulated days, When it is forecast, Then no run steps past 1,827
  simulated days.
- *Scenario: The years in the words come from the forecast* — Given today is 2026-10-18 and "Billing
  Migration"'s 85% is beyond the horizon, When its forecast is read, Then it carries a horizon of 5 years and
  the stand-in date 2031-10-18, And every surface prints "More than 5 years" from that 5.

**Acceptance criteria.**
- **AC-04.1** The horizon is 5 years: no simulated run goes past **1,827 simulated days** (5 × 365 + 2, so
  it spans 5 calendar years even with two leap days, DDD-6).
- **AC-04.2** The years in the copy come from the `horizonYears` the forecast carries, on live forecasts and
  on Delivery history points alike (NMD-12). No surface holds a second literal "5".
- **AC-04.3** The seeded baselines of the forecast acceptance and probe fixtures (S18) are unchanged for
  Features that finish within 5 years. Any that move are listed and explained in the commit body.
- **AC-04.4 (production data, manual gate)** On the restored dev instance, the slowest Portfolio forecast's
  `RefreshLog.DurationMs` is read before slice 01 and after this slice. **Pass** when the after reading is at
  least 50% lower; **fail** otherwise, which opens the deferred early-stop design (NMD-6) (KPI-3).

**Outcome KPI**: KPI-3. **Technical notes**: the horizon is `ForecastHorizon`, the run cap in
`ForecastSimulationLimits.Default` and the source of `horizonYears` (DDD-6, DDD-12). Only `Default` changes;
fixtures that read the cap from it follow. **Dependencies**: after US-03 (D7); released with it (NMD-5).

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
control. (6) SaaS with 2 replicas, the "Platform Modernisation" forecast running on the other replica → it
stops too. A replica that is reconnecting at that moment misses the message and keeps its work; the list
still shows that work, so Ingrid clicks again or stops the row (NMD-15).

**UAT.**
- *Scenario: Everything queued leaves at once* — Given the Task Manager lists "Portfolio 'Platform
  Modernisation' — Running" and 17 queued Team refreshes and forecasts, When Ingrid clicks Cancel all, Then no
  confirm dialog appears, every queued row leaves the list, and the running row reads `Stopping…` and then
  leaves.
- *Scenario: Stopped work does not come back as a follow-up* — Given "Team 'Payments' — Running" has a
  follow-up refresh waiting, When Ingrid clicks Cancel all, Then the Payments follow-up never runs.
- *Scenario: Scheduled updates carry on* — Given Ingrid used Cancel all at 13:55, When the Payments Team's
  scheduled refresh is due at 14:00, Then it runs.
- *Scenario: Deletions are left to finish* — Given "Removing Team 'Old' — Running", When Ingrid clicks Cancel
  all, Then the removal completes and Team 'Old' is gone.
- *Scenario: Only a System Administrator can cancel everything* — Given Tomás is signed in as a Viewer,
  When he opens Lighthouse and also asks the server directly to cancel all, Then he sees no Task Manager and
  no Cancel all control, and the server refuses him; And given Ingrid is a System Administrator and nothing
  is running, When she asks for the same, Then it succeeds.
- *Scenario: Cancel all fails and leaves the rows as they were* — Given the list shows "Team 'Payments' —
  Running" and "Portfolio 'PM' — Queued" and the server cannot be reached, When Ingrid clicks Cancel all,
  Then the button reads "Cancelling…" and is disabled while it waits, And then "Could not cancel. Try again."
  shows under the Activity heading, And the rows read "Running" and "Queued" again.
- *Scenario: Nothing to cancel, no button* — Given the list holds only "Removing Team 'Old' — Queued", When
  Ingrid opens the Task Manager, Then no Cancel all button is shown.

**Acceptance criteria.**
- **AC-05.1** After Cancel all, the task list holds no `Queued` row except deletes. The running non-delete
  row reaches a terminal state through the per-key path (S14).
- **AC-05.2** No coalesced follow-up and no held update admitted before the call runs afterwards. Rounds
  they held are left, so other work's staged write-back still flushes.
- **AC-05.3** Work triggered after the call, scheduled or manual, is admitted and runs normally.
- **AC-05.4** `TeamDelete`/`PortfolioDelete` are never cancelled.
- **AC-05.5** Non-administrators are refused and administrators succeed, also when nothing is running;
  asking twice does no harm. Only a System Administrator sees the Cancel all control.
- **AC-05.6** With several replicas, work running on another replica is stopped too. The message is
  delivered at most once: a replica reconnecting at that moment keeps its work, and the list still shows it
  (NMD-15). Traced to domain example (6).
- **AC-05.7 (production data, manual gate)** On the restored dev instance with *Update All* pressed (every
  Team and Portfolio queued), click Cancel all. **Pass** when, once the running row ends, the list holds no
  row but removals and the next periodic tick re-admits stale entities; **fail** if any cancelled row comes
  back before that tick.
- **AC-05.8** The Cancel all control looks as NMD-11 settled: no confirm dialog; while the request is in
  flight the button reads "Cancelling…" and is disabled; on failure "Could not cancel. Try again." shows
  under the Activity heading and every row goes back to how it was; the button is hidden when no row could
  be cancelled (an empty list, or removals only).

**Outcome KPI**: KPI-5. **Technical notes**: the endpoint carries `[RbacGuard(RbacGuardRequirement.SystemAdmin)]`
(403 for non-admins, 204 for admins, idempotent), and the control renders only under
`useRbac().isSystemAdmin` (D9); mechanics and the race are DDD-10 and DDD-11; the Redis notifier gets the
cancel-all message with one real-Redis test and nothing more (NMD-15). **Dependencies**: after slice 02 (D11).

---

## Wave: DISCUSS / [REF] Story Map

**Backbone**: *Lighthouse is busy* → *Keep using it* → *Stop what is hurting it* → *Trust what it says*.

| Slice | Story | ADO | Activity | Ships | Size | Released |
|---|---|---|---|---|---|---|
| 01 | US-01 | Bug #6308 | Keep using it | Bounded simulation parallelism | ~3h | Alone |
| 02 | US-02 | Bug #6308 | Stop what is hurting it | Cancellation reaches the simulation | ~4h | Alone |
| 03 | US-03 | Story #6309 | Trust what it says | Unfinished runs counted; beyond-horizon on the wire, every web surface, the Jira Release block, write-back | ~1 day | With 04 (NMD-5) |
| 04 | US-05 | Story #6309 | Keep using it / Trust | Horizon 100,000 days → 5 years (1,827 simulated days) | ~3h | With 03 (NMD-5) |
| 05 | US-04 | Story #6322 | Stop what is hurting it | Cancel all | ~1 day | Alone |

Briefs: `slices/slice-01-forecast-leaves-cores-free.md`, `slice-02-stop-ends-a-forecast.md`,
`slice-03-unfinished-runs-count.md`, `slice-04-five-year-horizon.md`, `slice-05-cancel-all.md`.

## Wave: DISCUSS / [REF] Slice Taste Tests

| Test | Verdict |
|---|---|
| Ships 4+ new components | **Pass.** No slice adds a new component. Slice 03 changes about 8 existing surfaces behind one flag and one shared wording; slice 05 adds one control and one endpoint. |
| Every slice depends on a new abstraction | **Pass.** Only 03/04 share one (the beyond-horizon marker). Slice 03 builds it first, and 03 and 04 are released together under Story #6309 (NMD-5): alone, 03 would give released clients a stand-in date about 274 years out. |
| No slice disproves a pre-commitment | **Pass.** 01 can disprove "the starvation is CPU/thread-pool" (pages still hang → the cause is elsewhere, e.g. the database). 02 can disprove "a between-runs check is prompt enough". 03 can disprove "surfaces can carry a non-date". 04 can disprove "real Features rarely sit beyond 5 years". 05 can disprove "per-key cancel plus dropping follow-ups and holds empties the list". |
| Synthetic data only | **Pass.** Every slice has a production-data AC on the dev instance restored from the production backup. Tenant Zero is offline. |
| 2+ slices identical but for scale | **Pass.** 01 and 02 touch the same loop but deliver different outcomes, each releasable alone. 03 and 04 are not identical either (counting versus the horizon), but they are one release (NMD-5). |
| Every slice has a user-visible story | **Pass.** |

## Wave: DISCUSS / [REF] Prioritization

01 → 02 → 03 → 04 → 05 (D11), that is US-01 → US-02 → US-03 → US-05 → US-04. **01 first** for learning
leverage: if the pages still hang with the bound, the incident has a second cause, and every later slice
should know that before building. **02 next**: same loop, and it turns a 10-minute `Stopping…` into seconds.
**03 before 04** on dependency (D7), and the two are released together under Story #6309 (NMD-5). **05
last**: it depends on 02 for a running forecast, and it is the only slice with new UI copy beyond 03. Each
slice is dogfooded on the restored dev instance the day it is built; 03 and 04 are dogfooded together before
their joint release.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| # | KPI | Target | Measurement | Baseline |
|---|---|---|---|---|
| KPI-1 | Page answers during a forecast | p95 of 20 `GET /api/latest/portfolios` < 1 s while the slowest Portfolio forecast runs, 4-CPU Docker cap | Script against the restored dev instance, after slice 01 (AC-01.5) | Pages hang (report) |
| KPI-2 | Stop latency for a running forecast | ≤ 5 s from click to `Cancelled`, on each of 3 tries | Restored dev instance, slowest Portfolio (AC-02.7) | > 10 min (report) |
| KPI-3 | Slowest forecast wall time | ≥ 50% lower `RefreshLog.DurationMs` for the slowest Portfolio forecast | Same DB before slice 01 and after slice 04 (AC-04.4) | Measured at the start of slice 01 |
| KPI-4 | Forecasts that show a guessed date | Count of percentiles projected to today = **0**, and count computed from finished runs only = **0** | Slice 03 ACs plus the before/after list (AC-03.8) | Unknown count today (S9, S10) |
| KPI-5 | "Had to restart Lighthouse during updates" reports | 0 in the 60 days after release | Support and community reports, against the 60 days before | ≥ 1 (this report) |
| KPI-6 | ~~Cancel all in use~~ | **Dropped at DEVOPS (NMD-7)**: no usage-data event, so it cannot be counted | — | — |

## Wave: DISCUSS / [REF] Definition of Done

1. Two separate gates:
   - **Automated ACs**: every slice's ACs that are not marked "production data" pass as automated tests
     (NUnit; Vitest for web; the client package's tests for the clients change).
   - **Manual gate — "production-data readings on the restored dev instance"**: AC-01.5, AC-02.7, AC-03.8,
     AC-04.4 and AC-05.7 are run by hand on the dev instance restored from the production backup. Each one's
     reading and its pass or fail go in the slice notes before the slice is closed. These are never part of
     the automated suite (D13).
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
8. Lighthouse-Clients release rendering the marker (NMD-1 option c). The clients key on the presence of
   `beyondHorizon` (NMD-3), not on the server version: an older server never sends the field.
9. `ARCHITECTURE.md` / `brief.md` updated if the simulation's threading or the beyond-horizon
   representation becomes an ADR.
10. Release notes written from the user's pain (pages hung, Stop did nothing, restart), with the reporter
    credited. No customer identifiers. The release and rollback notes tell the operator to run a forecast
    update for every Portfolio after a rollback, because older binaries read a beyond-horizon percentile as
    today's date until then (NMD-16).

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Evidence |
|---|---|---|
| 1 | Problem in domain language | Each story opens with a named persona and the incident's symptoms |
| 2 | Persona with specifics | Ingrid (Docker, 4 cores, 14 Teams); Tomás (delivery lead, low-throughput Team) |
| 3 | 3+ domain examples | US-01: 3, US-02: 4, US-03: 7, US-04: 6, US-05: 4 |
| 4 | UAT G/W/T, 3–7 per story | US-01: 4, US-02: 5, US-03: 6, US-04: 7, US-05: 4. Every scenario has a Given, a When and a Then with example values |
| 5 | AC from UAT | US-01: 5, US-02: 7, US-03: 9, US-04: 8, US-05: 4. Each AC is tied to a UAT scenario or a named domain example: AC-03.6 to US-03 example (6), the circle part of AC-03.1 to US-03 example (7), AC-05.6 to US-04 example (6). The five production-data ACs are checked at the manual gate (DoD 1), not by a scenario |
| 6 | Right-sized | 5 stories, each with 3–7 scenarios; slices are 3h to 1 day; about 4 days in total. The split of the old US-03 (8 scenarios) into US-03 and US-05 brought it under the cap |
| 7 | Technical notes | Inventory S1–S18 with file:line; DESIGN questions 1–9; mechanisms moved out of the ACs into each story's technical notes |
| 8 | Dependencies tracked | D7, D11; NMD-5 (US-03 and US-05 release together); Epic #6298 coordination; NMD-1/2 |
| 9 | Outcome KPIs | KPI-1..5 with targets and methods (KPI-6 dropped at DEVOPS, NMD-7) |

**DoR: PASSED**, re-validated on 2026-10-18 after the final review gate: the old US-03 is split into US-03
(slice 03) and US-05 (slice 04), both under Story #6309; the AC IDs did not change. NMD-1/2 were answered by
the maintainer on 2026-10-17 (c and a), and NMD-3 to NMD-16 on 2026-10-18. Requirements completeness about
0.95. Expansion triggers: none (2 personas, 2 contexts, no regulatory terms, WS strategy C).

---

## Wave: DISCUSS / [REF] Project DISCUSS Checklist

1. **RBAC** — no new permission. The new Cancel all endpoint takes
   `[RbacGuard(RbacGuardRequirement.SystemAdmin)]`, like `CancelTask`. The control sits inside the popover
   that `TaskManagerIcon` gates on `useRbac().isSystemAdmin` (S17), and nothing fetches `my-summary`.
   Forecast changes alter rendered values, not who sees them.
2. **Lighthouse-Clients CLI/MCP** — **impacted by slice 03.** Forecast percentiles are read by
   `forecastWording`, `featureWording`, `deliveryWording` and `cli/forecastOutput` (S12). The change is
   additive (NMD-1); a clients minor release renders it, keyed on the presence of `beyondHorizon` (NMD-3),
   not on the server version. The Task Manager is not exposed to clients, so Cancel all has no client impact.
3. **Website** — **N/A, because** this fixes a failure mode and makes an existing number more honest. It is
   release-notes material, not a new capability to advertise.
4. **Usage-data event** — **N/A, decided at DEVOPS (NMD-7):** no event. The candidate `TaskManagerCancelAll`
   is not added, so KPI-6 is **dropped at DEVOPS (NMD-7)**. A beyond-horizon event is not proposed; KPI-4 is
   verified by tests, not telemetry. Slices 01, 02 and 04 have no user action to count.
5. **Docs at finalization** — `docs/settings/taskmanager.md`, `docs/concepts/howlighthouseforecasts.md`, and
   the forecast-date and likelihood pages found by grep (DoD 6). `docs/settings/usagedata.md` is untouched
   (NMD-7).

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
   true. *(Answered at DESIGN: 1,827 simulated days, DDD-6.)*
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

DESIGN, 2026-10-18, Morgan (propose mode); revised the same day after the final review gate (NMD-12 to NMD-16).
Application scope; no migration, no new package, no new external system.

| ID | Decision | ADR |
|---|---|---|
| DDD-1 | **Per simulation**, a When/Start simulation runs on at most max(1, ⌊cores ÷ 2⌋) workers. The bound is carried by `ForecastSimulationLimits` and derived from `Environment.ProcessorCount`, which honours container CPU limits and `DOTNET_PROCESSOR_COUNT`. The core count is injectable. `Task.Run` + `Parallel.For` stay. The startup banner states the bound. Two simulations at once (a manual one during a background one) may together use all cores (NMD-14) | 248 |
| DDD-2 | `ForecastUpdater` reads the update's token and passes it to `IForecastService.UpdateForecastsForPortfolio(portfolio, token)`, and from there to `ParallelOptions.CancellationToken`. The check sits between runs. Other callers pass none | 249 |
| DDD-3 | `TotalTrials` counts every run simulated for a row; the histogram keeps finished days only. Beyond = `TotalTrials − Σ histogram`. Runs ended by a dependency circle count as beyond (NMD-13). Rows with no throughput keep `TotalTrials = 0` ("Cannot forecast") | 250 |
| DDD-4 | A percentile is read through one value object, `ForecastPercentile` (a day / beyond the horizon / no answer). `-1` stops meaning two things. `HowManyForecast` is unchanged | 250 |
| DDD-5 | **Multi-Team aggregation** (details below). The combinators stay closed. Each contributor enters `Combine` / `Min` with one **virtual bucket at key `int.MaxValue`** (last in their ascending order) holding `TotalTrials − Σ histogram`. The bucket **stays** in the per-Team carriers `DeliveryCompletionForecast.CarrierFor` builds. It is stripped **only at aggregate output** (`AggregatedWhenForecast`), whose `TotalTrials` is then the carried total N, **not** the sum of its stripped histogram | 250 |
| DDD-6 | The horizon is `ForecastHorizon`: 5 years → **1,827 simulated days** (5 × 365 + 2), which covers 5 calendar years even with two leap days. It is the run cap in `Default` and the source of the copy's years | 251 |
| DDD-7 | The wire carries `beyondHorizon` + `horizonYears` on `WhenForecastDto` **and** on `WhenDistributionPointDto` (NMD-3, NMD-12). A beyond `expectedDate` = `today.AddYears(horizonYears)`. Points pinned or snapshotted before the change carry neither and read `beyondHorizon = false` | 252 |
| DDD-8 | Blackout-window sizing by the 95th percentile uses the horizon when that percentile is beyond | 252 |
| DDD-9 | The Jira Release block writes "85%: More than 5 years" | 254 (NMD-4) |
| DDD-10 | Cancel all = `POST api/{latest,v1}/update/tasks/cancel` (SystemAdmin, 204, idempotent) → `IUpdateCancellationNotifier.PublishCancelAllAsync` / `SubscribeToCancelAll`. Each replica drops its non-delete follow-ups, then every hold (holds are forecast-only; their rounds are left), then stops its own non-delete keys through the per-key path. Delivered **at most once** (NMD-15) | 253 |
| DDD-11 | Race: what a replica holds when the message arrives is cancelled; later work runs. `EnqueueAndAwaitAsync` moves `cancellations.Admit` **before** `statusStore.TryAdmit`, as `EnqueueUpdate` already does, so no admitted key lacks a token source | 253 |
| DDD-12 | Only `ForecastSimulationLimits.Default` changes; fixtures that read the cap from `Default` follow it | 251 |
| DDD-13 | Web: one model field set on `WhenForecast`, and one shared formatter for every text surface (NMD-9). The timeline and the predictability chart follow NMD-10. `Feature.ts:138` and `services/Api/ForecastService.ts:104` carry the flag through | 252 |
| DDD-14 | `ForecastService` logs: the "ran out of days" `LogError` goes. Runs that reach the horizon are counted, not an error, so one Debug line with the count is enough. The circle `LogError` stays, and its "not a forecast" sentence is corrected (NMD-13). The `HowTheRunEnded.RanOutOfDays` comment is corrected | 250 |

**DDD-5 in full — why and how.**
- **The gap:** `Feature.Forecast` and a Delivery's forecast are `AggregatedWhenForecast`s (`Feature.cs:57`,
  `DeliveryCompletionForecast.cs:39,60-70`). Today `AggregatedWhenForecast` calls `SetSimulationResult(joint)`, which sets
  `TotalTrials` to the histogram's sum (`AggregatedWhenForecast.cs:19`). Without the rule below, every multi-Team Feature
  and every Delivery would report nothing beyond the horizon.
- **The rule:** for every input, the input histogram = its histogram plus `{int.MaxValue: TotalTrials − Σ}` when that is
  > 0. A carrier already holds its bucket and has `TotalTrials = Σ`, so the rule adds nothing to it.
- **The output:** the aggregate removes the `int.MaxValue` key and sets `TotalTrials` = the joint's full sum (N).
- **The guard:** no histogram a reader projects ever contains `int.MaxValue`.
- **Tests:** a multi-Team Feature, and a Delivery over two Teams, where one Team has unfinished runs, report the joint
  beyond share (`1 − Π` of the finished shares) and the right percentiles.

## Wave: DESIGN / [REF] Component decomposition

| Component (path) | Change |
|---|---|
| BE `Models/Forecast/ForecastHorizon.cs` | **new** |
| BE `Models/Forecast/ForecastPercentile.cs` (value object) | **new** |
| BE `Models/Forecast/ForecastSimulationLimits.cs`, `ForecastBase.cs`, `WhenForecast.cs`, `StartForecast.cs`, `AggregatedWhenForecast.cs` (bucket in, strip out, carried total), `DeliveryCompletionForecast.cs` (carriers keep the bucket) | modified |
| BE `Models/Forecast/JointCompletionDistribution.cs`, `ComonotonicCompletionDistribution.cs`, `CompletionHistogram.cs` | **unchanged** |
| BE `Models/SimulationResult.cs`, `Models/Feature.cs` (start answer), `Models/Delivery.cs`, `Models/DeliveryMetricsProjection.cs`, `Models/DeliverySources/DeliveryForecastBlock.cs` | modified |
| BE `Services/Implementation/Forecast/ForecastService.cs`: bound, token, runs-simulated total, and the log changes (the ran-out-of-days `LogError` goes, the circle `LogError` stays with corrected wording); `HowTheRunEnded.cs` (comment); `Services/Interfaces/Forecast/IForecastService.cs` | modified |
| BE `Services/Implementation/BackgroundServices/Update/ForecastUpdater.cs` | modified |
| BE `API/DTO/WhenForecastDto.cs`, `DtoExtensions.cs`, `DeliveryWithLikelihoodDto.cs`, `DeliveryMetricsHistoryDto.cs`, `Archived/ArchivedDeliveryProjection.cs`; `Services/Implementation/DeliveryMetricValuesProjector.cs` | modified |
| BE `API/FeatureForecastWindow.cs`, `API/ForecastController.cs` | modified |
| BE `Services/Implementation/WriteBackTriggerService.cs` | modified (behaviour unchanged) |
| BE `Services/Implementation/DeliverySources/DeliveryForecastBlockRenderer.cs`, `DeliveryForecastPublishingService.cs` | modified |
| BE `Services/Interfaces/Update/IUpdateQueueService.cs`, `IUpdateCancellationNotifier.cs`; `…/Update/UpdateQueueService.cs` (`CancelAllAsync`, `Admit` before `TryAdmit` in `EnqueueAndAwaitAsync`), `AdmittedCancellations.cs`, `InProcessUpdateCancellationNotifier.cs`, `RedisUpdateCancellationNotifier.cs` | modified |
| BE `API/UpdateController.cs` | modified (4th route) |
| BE startup banner | modified (one line) |
| FE `models/Forecasts/WhenForecast.ts`, `forecastSchemas.ts`, `models/Feature.ts`, `models/Delivery/ArchivedDelivery.ts`, `models/Delivery/DeliveryMetricsHistory.ts`, `services/Api/ForecastService.ts` | modified |
| FE `components/Common/Forecasts/forecastPercentileText.ts` (shared formatter) | **new** |
| FE `ForecastInfo.tsx`, `DeliverySection.tsx`, `deliveryExportTable.ts`, `deliveryTimelineModel.ts`, `DeliveryPredictabilityChart.tsx` | modified |
| FE `components/App/Header/TaskManager/SectionHeading.tsx` (optional action), `ActivitySection.tsx`, `useTaskManagerPopover.ts`, `services/UpdateSubscriptionService.ts` | modified |
| Clients `forecastWording.ts`, `featureWording.ts`, `deliveryWording.ts`, `cli/src/forecastOutput.ts`, schemas | modified (clients minor, at finalize) |

## Wave: DESIGN / [REF] Driving ports

| Port | Change | Slice |
|---|---|---|
| Feature, Delivery, manual-forecast, snapshot-history, archived-Delivery GETs | additive `beyondHorizon` + `horizonYears` on percentiles and on history points | 03 |
| `POST api/latest/update/tasks/{type}/{id}/cancel` and `api/v1/…` (the controller serves both prefixes) | unchanged; now effective on a running forecast | 02 |
| `POST api/latest/update/tasks/cancel` and `api/v1/update/tasks/cancel` | **new**, `[RbacGuard(SystemAdmin)]`, 204, idempotent | 05 |
| `IForecastService.UpdateForecastsForPortfolio` | + `CancellationToken` | 02 |
| `IUpdateQueueService` | + `CancelAllAsync()` | 05 |
| Lighthouse-Clients CLI/MCP | read the flag when present | 03 (finalize) |

## Wave: DESIGN / [REF] Driven ports + adapters

| Port | Adapter(s) | Change |
|---|---|---|
| `IUpdateCancellationNotifier` | `InProcessUpdateCancellationNotifier`, `RedisUpdateCancellationNotifier` | + `PublishCancelAllAsync()` / `SubscribeToCancelAll(Action)`. Redis uses its own channel; it is a proof-of-concept path, so it gets the minimum plus one real-Redis test (NMD-15) |
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
| Q1 | The bound | max(1, ⌊cores ÷ 2⌋) per simulation | recommendation; NMD-14 |
| Q2 | Pool vs dedicated threads | keep the pool; dedicated threads only if AC-01.5 fails | recommendation |
| Q3 | Token path | explicit from `ForecastUpdater` (S107 rules out injecting it into `ForecastService`) | recommendation |
| Q4 | Representation | `TotalTrials` = all runs; aggregates carry N (DDD-5); no migration | recommendation |
| Q5 | Unit | 1,827 simulated days; copy always true | recommendation |
| Q6 | Wire | NMD-3 a, plus `horizonYears` on history points (NMD-12) | **maintainer** |
| Q7 | Cancel all | one broadcast, local apply, at most once | recommendation; NMD-15 |
| Q8 | Race | cancel what is held when the message arrives; `Admit` before `TryAdmit` | recommendation |
| Q9 | Fixtures | only `Default` changes | recommendation |
| F-1 | Release block | NMD-4 a | **maintainer** |
| F-3 | Window sizing | horizon for a beyond 95th | recommendation |
| F-4 | Circle runs | count as beyond; circle error log stays | **maintainer** (NMD-13) |
| F-5 | Concurrent simulations | per-simulation bound; no global gate | **maintainer** (NMD-14) |
| F-6 | Release packaging | NMD-5 | **maintainer** |
| — | Early stop for a whole forecast | NMD-6: not now | **maintainer** |
| — | Usage-data event | NMD-7: none | **maintainer** |
| — | Old binaries | NMD-16: accepted, rerun forecasts after a rollback | **maintainer** |

**Loading and failure looks.**
- **"More than N years"** adds no data fetch. Each host keeps its existing looks (WidgetShell `status`, keyed TanStack
  queries). A beyond percentile is never drawn as a date, as an empty cell, or as "Cannot forecast" (wording NMD-9,
  drawn surfaces NMD-10).
- **Cancel all** looks as NMD-11 describes:
  - hidden when nothing is cancellable;
  - "Cancelling…" with a spinner while in flight;
  - a "Could not cancel. Try again." line on failure, with the rows reverted;
  - a re-read list when done.
- The list keeps "keep what was there on a failed read".

**Old binaries and pinned records** (NMD-16):
- **Exposure window:** from a rollback, or during a rolling upgrade, until each Portfolio's next forecast run.
- **What an old binary shows:** a percentile beyond the horizon reads as today's date. Its rows have `TotalTrials > 0`,
  and the old read projects `-1` to today. Write-back writes nothing for it.
- **Rerun step:** the release and rollback notes say to run a forecast update for every Portfolio after a rollback.
- **Mixed denominators:** until every row of an aggregate has been rewritten by the new binary, a multi-Team aggregate
  mixes rows that count unfinished runs with rows that do not.
- **Pinned and snapshot points** written before the change have no `beyondHorizon` and read `false`. No gate is
  designed.

## Wave: DESIGN / [REF] Reuse Analysis

Contract shapes:
- **pure** — returns a value, writes nothing;
- **bounded-change** — named universe, named delta;
- **unbounded-preservation** — must return a plan, never mutate.

No component here is unbounded-preservation.

| Existing component | File | Overlap | Decision | Contract shape · justification |
|---|---|---|---|---|
| `ForecastService` | `Services/Implementation/Forecast/ForecastService.cs` | the simulation | EXTEND | **bounded-change**. Universe: the forecast rows of the Features passed in, and log lines. Delta: those rows rewritten; ∅ on cancel. Adds options, the runs-simulated total and the log changes |
| `ForecastSimulationLimits` | `Models/Forecast/` | limits | EXTEND | **pure** value: + bound; cap from `ForecastHorizon` |
| `ForecastBase` / `WhenForecast` / `StartForecast` | `Models/Forecast/` | percentile reads | EXTEND | **pure** reads over its own state; `TotalTrials ≥ Σ` |
| `AggregatedWhenForecast` | `Models/Forecast/` | multi-Team aggregate | EXTEND | **pure**: adds a virtual bucket on input, strips it on output, `TotalTrials` = N (DDD-5) |
| `DeliveryCompletionForecast` | `Models/Forecast/` | per-Team carriers | EXTEND | **pure**: carriers keep the bucket |
| `JointCompletionDistribution`, `ComonotonicCompletionDistribution`, `CompletionHistogram` | `Models/Forecast/` | combine maths | REUSE unchanged | **pure**; the maths and bit-identity are kept |
| `WhenForecastDto`, `DtoExtensions.CreateForecastDtos`, `WhenDistributionPointDto`, `DeliveryWhenPercentile` | `API/DTO/`, `Models/` | wire | EXTEND | **pure** projection from a forecast and today to a DTO; additive fields only |
| `WriteBackTriggerService` | `Services/Implementation/` | forecast write-back | EXTEND | **bounded-change**. Universe: field updates it returns for staging, one per mapped Feature field. Delta for a beyond percentile: **none staged**. Same as today's "no answer"; it reads through `ForecastPercentile` |
| `DeliveryForecastBlockRenderer` | `Services/Implementation/DeliverySources/` | Release text | EXTEND | **pure**: block in, text out. A beyond percentile renders the words (NMD-4); the merge keeps text outside the markers |
| `DeliveryForecastPublishingService` | same | Release publish | EXTEND | **bounded-change**: one Release description per broadcasting Delivery, unchanged universe |
| `ForecastUpdater`, `IForecastService` | `…/Update/`, `Services/Interfaces/Forecast/` | the update | EXTEND | **bounded-change**: as today (forecast rows, refresh-log row, staged write-back), now cancellable |
| `UpdateQueueService`, `AdmittedCancellations`, notifiers | `…/Update/` | per-key cancel | EXTEND | **bounded-change**. Universe: this replica's token sources, follow-ups, holds and status rows. Delta: non-delete sources cancelled, non-delete follow-ups removed, all holds removed (forecast-only) with their rounds left, waiting non-delete rows → `Cancelled`. Deletes untouched |
| `UpdateController` | `API/` | Task Manager HTTP | EXTEND | **bounded-change** through the queue only. The route itself holds no state; 204 whatever was there |
| `WhenForecast` (FE) + schemas | `models/Forecasts/`, `models/Delivery/` | percentile model | EXTEND | **pure** parse; optional fields default `false` / undefined |
| `ForecastInfo`, `DeliverySection`, `deliveryExportTable`, `deliveryTimelineModel`, `DeliveryPredictabilityChart` | FE | surfaces | EXTEND | **pure** render from props; the timeline and chart follow NMD-10 |
| `ActivitySection`, `useTaskManagerPopover`, `UpdateSubscriptionService` | FE | per-row Stop | EXTEND | **bounded-change** of component state only (`stopAsked`, the in-flight flag, the error line), plus one POST through the service. Reuses `IconButton` / `Tooltip` / `stopAsked` |
| `SectionHeading` | FE `components/App/Header/TaskManager/` | heading | EXTEND | **pure** render; gains an optional action slot (NMD-11) |
| `ForecastHorizon` | `Models/Forecast/` | — | **CREATE NEW** | **pure** constant: nothing holds "the horizon in years" that both the simulation and the copy can read |
| `ForecastPercentile` | `Models/Forecast/` | — | **CREATE NEW** | **pure** value object: the `-1` return cannot say "beyond" and "no answer" apart, and `HowManyForecast` must keep it |
| `forecastPercentileText` | FE `components/Common/Forecasts/` | — | **CREATE NEW** | **pure** function: six text surfaces need one wording from `horizonYears`. No shared formatter exists (each surface formats its own date today) |

## Wave: DESIGN / [REF] Open questions

**For DISTILL:**
- UI-1..UI-5 are settled (NMD-9 to NMD-11); the timeline and chart behaviour is NMD-10.
- The demo data needs a low-throughput Team for the screenshot (DoD 7).
- A Delivery whose rows have only unfinished runs now shows "More than 5 years" with likelihood 0, where it used to
  show "cannot forecast" (Changed Assumption 2).
- Tests required by this design: a multi-Team beyond test (DDD-5), and holds are forecast-only (Cancel all assumes it;
  `ForecastUpdater.HoldTheForecastUntil` is the only caller of `HoldUntilQueuedWorkClears`).

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
   - After DDD-3 both read "More than 5 years" (likelihood 0).
3. **S11** listed `ForecastPredictabilityScore` and `ForecastRealityCheckService` as consumers of a forecast
   percentile. Both read `HowManyForecast` (`Models/Forecast/ForecastPredictabilityScore.cs:7`); neither is touched.
4. **NMD-1 c** said *"gated on server version"*. The gate is now on field presence (NMD-3). This refines c and does
   not reverse it.
5. **Pre-requisites** named only Epic #6298 as a second renderer of forecast dates. The Jira Release block
   (`Services/Implementation/DeliverySources/DeliveryForecastBlockRenderer.cs`) is a second tracker-facing write (NMD-4).
6. **D7** said *"Slice 03 … ships before slice 04"*. Build order unchanged; release together (NMD-5).
7. **AC-04.1 and the slice-04 UAT** say *"never pass 1,826 days"*. The horizon is 1,827 simulated days (DDD-6), so that
   5 calendar years are covered even with two leap days.

## Wave: DESIGN / [REF] Handoff to DEVOPS

- **Usage data:** none (NMD-7).
- **KPIs:** KPI-1..5 are measured on the restored dev instance with Docker capped to 4 CPUs. The startup banner states
  the forecast's worker bound, which makes KPI-1's setup checkable.
- **Infrastructure:** one more Redis pub/sub channel on the existing connection, used only when Redis is configured. No
  hardening (NMD-15), no migration, no new configuration key.
- **Rollback:** after a rollback, run a forecast update for every Portfolio (NMD-16).
- **External integrations needing contract tests:**
  - Jira Release description (forecast block, existing publisher): a renderer and merge test with a beyond percentile.
  - Lighthouse-Clients ← Lighthouse API (additive `beyondHorizon` / `horizonYears`): a consumer-driven schema test in
    the clients repo against a recorded server payload, with and without the fields.
- **Paradigm:** OOP, ports and adapters (unchanged).
- **Mutation testing:** per feature, both stacks, run last on frozen code.

---

## Wave: DEVOPS / [REF] Scope and prior-wave reading

**Agent**: Apex (`nw-platform-architect`) · **Date**: 2026-10-18 · **Mode**: autonomous subagent, documents only (no
code, no commit, no test run). Application change inside the existing product: no new workflow, job, dependency,
configuration key, migration or chart value. One more Redis pub/sub channel on the existing connection.

- ✓ This file: NMD-1..7, DISCUSS Outcome KPIs, Pre-requisites, DESIGN in full; ADR-248..254; `brief.md` 6311 section.
- ✓ `CLAUDE.md` (gates, usage-data rule); `docs/ci-learnings.md` (Program.cs rule, test-time budget, D13's wall-clock rule).
- ✓ `.github/workflows/` (`ci.yml` and its called jobs); `Scripts/test-selection/path-classifier.sh`;
  `Scripts/test-timings/test-time-budget.json`; `chart/values.yaml`; `Dockerfile`.
- ✓ `Program.cs:1561` (`AddSingleton(ForecastSimulationLimits.Default)`), `:1761` (`StartupBanner.BuildInfoLines(new
  StartupBannerFacts(…))`); `RedisUpdateCancellationNotifier.cs:16` (channel `lighthouse:update-cancelled`);
  `Integration/Containers/TaskManagerMultiReplicaTests.cs` (real Redis via `RedisContainerFixture`).

## Wave: DEVOPS / [REF] Pre-requisites

| # | Pre-requisite | State | Blocks |
|---|---|---|---|
| P1 | Dev instance restored from the production backup (`Restore-DbBackup.ps1`), run in Docker with `--cpus=4` (KPI-1 also once at `--cpus=2`) | Procedure | KPI-1/2/3 readings; production-data ACs |
| P2 | KPI-3 baseline: `RefreshLog.DurationMs` of the slowest Portfolio forecast, read **before** slice 01 changes anything. **Hard entry criterion of slice 01** (see Slice 01 entry and finalize) | **baseline read 2026-10-18: New Portfolio, 1,766 ms** (see Slice 01 entry) | Slice 01 entry (AC-04.4 compares against it) |
| P3 | Premium licence fixture copied into the worktree | Procedure | Local `dotnet test` only (2 Licensing tests) |
| P4 | Docker running locally (Testcontainers Redis for the cancel-all channel test) | Procedure | Slice 05 local run |
| P5 | `/storage/repos/lighthouse-clients` on clean `main` | Procedure | Clients release at finalize |

## Wave: DEVOPS / [REF] Slice 01 entry and finalize

**Entry (hard): slice 01 changes nothing until the KPI-3 baseline is recorded here.** On the restored dev instance
(P1), still on the release before slice 01, run a forecast update for every Portfolio, then read the slowest one
(`Type` is stored as an integer; `RefreshType.Forecast` = 2; the query runs on SQLite and PostgreSQL):

```sql
SELECT "EntityId", "EntityName", "DurationMs", "ExecutedAt"
FROM "RefreshLogs"
WHERE "Type" = 2 AND "Success" = true AND "Cancelled" = false
ORDER BY "DurationMs" DESC
LIMIT 1;
```

- **baseline** — Portfolio: `New Portfolio` (id 1) · `DurationMs`: `1766` · read on: `2026-10-18` · release:
  `ghcr.io/letpeoplework/lighthouse:dev-latest` built 2026-10-18 01:04 UTC from `main` (production code as of
  `7dac84590`), `--cpus=4`, on a throwaway copy of `DB_Backup/LighthouseAppContext.db`.
  - The 1,766 ms is the **first forecast after a cold start**. Two warm repeats on the same container read 987 ms and
    357 ms, and the other two Portfolios took 100 ms and 106 ms. The run-to-run spread is wider than the 50% KPI-3
    asks for, so AC-04.4 has to compare like with like: a cold start, then the first forecast of `New Portfolio`,
    read three times.
  - The backup's slowest forecast is under 2 s, far from the multi-minute freeze in the incident report. KPI-3 on
    this database checks for a regression; it does not reproduce the reported freeze.
- AC-04.4 reads the same Portfolio with the same query after slice 04.

**Finalize (slice 01), on top of the DELIVER checklist:** one docs line naming `DOTNET_PROCESSOR_COUNT` as .NET's own
setting for the core count, through which an operator changes the half-the-cores limit (NMD-8). The KPI-1 script is
committed with slice 01 at `Scripts/kpi/epic-6311-portfolios-p95.sh` (planned path).

## Wave: DEVOPS / [REF] Environment matrix

| Axis | Values | What differs | Covered by |
|---|---|---|---|
| Cores seen by .NET | 1, 2, 4, 32; Docker `--cpus=N`; k8s with no CPU limit | Worker bound max(1, ⌊cores ÷ 2⌋). `Environment.ProcessorCount` honours the cgroup quota and rounds up (`--cpus=1.5` → 2 → 1 worker). The chart sets `requests.cpu: 100m` and **no** CPU limit, so a pod sees the node's cores and uses half the node | Unit test with injected core counts 1, 2, 4 (ADR-248); the banner line on the dev instance; KPI-1 read at `--cpus=4` and `--cpus=2`. **Helm (no CPU limit) is unmeasured** |
| Replicas | single (in-process notifier), several (Redis) | Cancel all: direct call vs one broadcast on its own channel | Queue tests in process; a container test against real Redis (Testcontainers) for the new channel |
| Server version during a rolling deploy | old + new replica | An old replica ignores the cancel-all channel; per-row Stop still works across versions (old channel unchanged). An old replica reads a beyond percentile as today's date until the next forecast run (NMD-16) | ADR-253 consequence; no test (two binaries) |
| Client version | released lh/MCP, upgraded lh/MCP, web app | Old clients read `expectedDate` (a date 5 years out); upgraded clients key on `beyondHorizon` presence | Clients schema test with and without the field |
| Database provider | SQLite, PostgreSQL | Nothing: no migration, no new column, no query change | `verifysqlite` / `verifypostgres` start the app as today |
| Tracker writes | Write-back (all trackers), Jira Release block | Write-back writes nothing for beyond (unchanged code path); Jira block writes "85%: more than 5 years" | Renderer + merge test (ADR-254); no live connector test needed |

## Wave: DEVOPS / [REF] CI/CD pipeline outline

No new workflow, job or step. What the existing `ci.yml` pipeline does with this change:

| Existing job | What it proves here |
|---|---|
| `ci_changes` → `path-classifier.sh` | No connector path changes. `Program.cs` **is** in the shared whitelist: if a slice edits it, every live connector category joins that push (see below) |
| `ci_backend` | Build (`TreatWarningsAsErrors`), unit + acceptance (`Category!=Integration`), including `Integration/Containers/*` Redis tests through Testcontainers. Ends with the test-time budget check (`--warn-only` today) |
| `ci_frontend` | Vitest, `tsc -b`, `vite build`, Biome (prebuild runs `biome check --write`) |
| `ci_sonar_gates` | No new issue. A red backend job skips Sonar, so a red live-connector run must be cleared first |
| `ci_verifysqlite`, `ci_verifypostgres` | App start + Playwright E2E. **Single replica, no Redis**: E2E never exercises the cancel-all channel. Any new Task Manager or "more than 5 years" POM step runs here |
| `ci_verifywindows`, `ci_verifymacos`, `ci_docker`, packaging, `ci_chart` | Unchanged. No chart change |

**CI points this feature triggers:**
- **Program.cs (ci-learnings 791-798, 1467-1469): no edit needed, checked.** `Program.cs:1561` registers
  `ForecastSimulationLimits.Default`, a static whose `CoresSeen` defaults to `Environment.ProcessorCount`, and
  `StartupBanner.BuildInfoLines` (`Startup/StartupBanner.cs:114`) is a static that already reads facts it is not handed
  (OS, runtime, process id). So the banner line can read `ForecastSimulationLimits.Default` inside `BuildInfoLines`, or
  take a new trailing optional fact `ForecastSimulationLimits? Limits = null` (falling back to `Default`) that tests set
  and `Program.cs:1761` leaves out, as `AllowsStartWithUnreadableSecrets` already does. Either way the banner states the
  same instance the container resolves, and `Program.cs` is untouched. Slice-01 DELIVER takes that route first. If
  `Program.cs` must change after all, that push runs the full Integration suite (every live connector): budget the full
  run plus one flake re-run. Slices 02-05 need no `Program.cs` edit (the notifiers and `UpdateQueueService` are already
  registered); if DELIVER finds one, call it out.
- **Test-time budget (ADR-237).** Backend budget 34 s per class; ceilings today (`Scripts/test-timings/test-time-budget.json`,
  read 2026-10-18): `ForecastServiceTest` **36 s**, `TaskManager.Slice04StopARefreshTest` 95 s,
  `DeliverySources.Slice04ForecastPublishingTest` 60 s, `DependencyAwareForecasting.Slice00OneForecastPerBatchScenarios`
  59 s. `WriteBackTriggerServiceTest`, `UpdateQueueService` tests, the block renderer tests and
  `TaskManagerMultiReplicaTests` are not allowlisted (under 34 s). Two opposite effects: slice 01 halves the workers
  (2 on a 4-vCPU runner), so a class running real simulations on `Default` can get slower until slice 04; slice 04 cuts
  the run cap from 100,000 to 1,827 days, so fixtures with unfinished runs get faster. **Slice 01 records the durations
  of the new classes `ForecastWorkerBoundTest` and `ForecastBeyondTheHorizonTest` and sets their ceilings in the same
  commit**, each with a reason; each later slice does the same for the `ResponsiveDuringUpdates.Slice0N…Scenarios` class
  it un-marks. The cores test stays on small injected counts (1, 2, 4); larger counts check the bound's arithmetic, never
  that many real workers. The backend step is `--warn-only`, so a breach is a `::warning::` on a green job: read the annotation on
  the slice-01 and slice-04 pushes. If an existing class crosses, speed it up first (tests may pin the core count,
  ADR-248), or adjust its ceiling with a reason (ci-learnings 442). Re-check after slice 04: a ceiling raised for slice
  01 may then be loose. The budget file is not edited in DEVOPS.
- **D13 / ci-learnings 552-558.** No wall-clock assertion; timings stay in KPI readings and `[Explicit]` probes.

**Local gates (mirror CI)**, every one green before a step is committed: `dotnet build` (zero warnings); the analyzer
sweep `dotnet format analyzers Lighthouse.sln --severity info --verify-no-changes --no-restore`, filtered to this
change's files (the worktree runs no ledger hook); `dotnet test` with the non-connector filter from `CLAUDE.md`;
`pnpm test`; `pnpm build`. Clients: the repo's own `pnpm ci` pre-commit hook. Existing pre-push hooks unchanged.

## Wave: DEVOPS / [REF] Monitoring contracts (KPI → instrument)

Self-hosted: no vendor dashboard, no alert to the vendor, **no new dashboard**. Contracts in
`docs/product/kpi-contracts.yaml` (`OUT-6311-*`).

| KPI | Instrument | Method | Threshold |
|---|---|---|---|
| KPI-1 page answers during a forecast | Script `Scripts/kpi/epic-6311-portfolios-p95.sh` (planned, committed with slice 01): 20 × `GET /api/latest/portfolios`, p95; the banner line "Forecasts use up to N of M cores" proves the cap | Restored dev instance (P1), while the slowest Portfolio forecast runs, after slice 01 (AC-01.5): once at `--cpus=4` (2 of 4) and once at `--cpus=2` (1 of 2). Helm (no CPU limit, half the node) is **unmeasured** | p95 < 1 s at both. Miss → ADR-248's dedicated-threads fallback |
| KPI-2 Stop latency | Task Manager click time vs the update's `RefreshLog` row (bool `Cancelled` = true; `ExecutedAt` is written when the update ends) and its Information log line | Same instance, slowest Portfolio, 3 tries (AC-02.7) | ≤ 5 s each. Miss → ADR-249's in-run checkpoint |
| KPI-3 slowest forecast wall time | `RefreshLog.DurationMs` of the slowest Portfolio forecast | Same DB, P2 baseline (1,766 ms cold, New Portfolio, read 2026-10-18; see Slice 01 entry and finalize) vs after slice 04 (AC-04.4) | ≥ 50% lower. Miss → NMD-6 follow-up |
| KPI-4 no guessed date | Test suite (slice 03 ACs, `TotalTrials ≥ Σ histogram` invariant); AC-03.8 before/after list on the restored instance | CI every push; once at slice 03 | 0. Any failure = build red |
| KPI-5 restart reports | Support mail, GitHub issues, community Slack | Maintainer, 60 days after the release vs the 60 days before. **Before snapshot: TO BE COUNTED** — support mail + community Slack + GitHub issues, window `YYYY-MM-DD` to `YYYY-MM-DD` (the 60 days before the release carrying slices 03 + 04), count `…`, counted on `YYYY-MM-DD` | 0 |
| KPI-6 Cancel all in use | **None (NMD-7)** | Not measured | **Dropped**; not entered in `kpi-contracts.yaml` |

KPI-1 and KPI-2 thresholds are **one-off soft gates** read once on the dev instance; no runtime alert watches them in
production. A miss triggers the named fallback, not an alarm.

## Wave: DEVOPS / [REF] Observability stack

Existing, unchanged: Serilog / `ILogger` lines (an update's cancel line, plus one Information line per cancel-all apply on
each replica, with counts only), the `RefreshLog` table and Refresh History, the Task Manager list, and **one new
StartupBanner line** stating the forecast worker bound. No metric, trace, dashboard or alert is added. OpenTelemetry
stays off by default. No new data leaves the instance.

## Wave: DEVOPS / [REF] Usage-data event

**N/A, because the maintainer chose not to count Cancel all** (NMD-7, 2026-10-18). KPI-6 is dropped as unmeasured.
Slices 01, 02 and 04 have no user action to count; KPI-4 is verified by tests, not telemetry. Nothing appended to
`UsageDataEventName`; `docs/settings/usagedata.md` unchanged.

## Wave: DEVOPS / [REF] Deployment strategy and rollback

Ships in ordinary releases (standalone, Docker image, Helm chart appVersion); the release process is unchanged. No flag.
Slices 01, 02 and 05 can each go out in a release; **03 and 04 release together** under Story #6309 (NMD-5).

**Rollback first.** Roll back by installing the previous release (re-pin the image tag, `helm rollback`, or reinstall the
standalone). What happens:
- **No migration** and no persisted shape change (`TotalTrials` keeps its column and now counts every run; ADR-250).
  But a rollback is **not clean for forecasts** (NMD-16): the old binary reads a beyond percentile the new one stored
  through its `-1` path (`ForecastBase.cs:55-69`) and shows **today's date**, a guessed date, until that Portfolio's
  next forecast run rewrites the row. Bounded by that next run and self-healing; write-back writes nothing for it.
  The same holds for an old replica during a rolling upgrade.
- **After a rollback: run a forecast update for every Portfolio.** This step goes into the release notes and the
  rollback notes.
- **KPI-4 on rollback:** until that rerun the old binary breaks KPI-4 by design; afterwards, Refresh History shows a
  forecast row newer than the rollback for every Portfolio.
- **The wire change is additive.** The old server stops sending `beyondHorizon` / `horizonYears`; upgraded clients key on
  presence and fall back to the date.
- **The new endpoint disappears**; a web app still loaded on the new bundle gets 404 on Cancel all and shows its failed
  look ("Could not cancel. Try again."). A reload fixes it.
- **Trackers**: a Jira Release block saying "more than 5 years" is overwritten by the old binary's next publish (the
  block is found by its opening and "Target" lines, not the percentile lines; ADR-254).

**Clients release order** (NMD-1 c, NMD-3): **server first, clients any time after.** Clients key on whether the field
is present, so a new client against an old server is safe and needs no version gate. At finalize: land the change with
its `.changeset/*.md` in lighthouse-clients (client and cli minor; mcp packages patch through
`updateInternalDependencies`), run `pnpm release:version`, commit the bump (`SKIP_SIMPLE_GIT_HOOKS=1`), push, and only
then is the `Release` gate approved by the maintainer on the new run.

**Production validation** (per instance, advisory): the banner shows the bound; a Stop on a running forecast lands
`Cancelled` in Refresh History; Cancel all empties the list except deletes.

## Wave: DEVOPS / [REF] Multi-replica and containers

- **Redis.** One more channel on the existing connection, used only when `ConnectionStrings:Redis` is set. No Helm,
  k8s or compose change, and no new key: the chart's `redis.connectionString` already carries it.
- **Rolling upgrade.** The chart's default `RollingUpdate` overlaps an old and a new pod for seconds. An old replica
  ignores the cancel-all channel (ADR-253), so its running work goes on; per-row Stop still reaches it. Acceptable for
  that window; **decided** (NMD-8): the docs do not mention it. Redis multi-replica is a proof of concept (NMD-15), so
  no further rolling-upgrade analysis.
- **Test coverage of Redis.** E2E and the verify workflows run one replica without Redis, so they never touch the
  channel. The only real-Redis proof is a `Integration/Containers` test (Testcontainers, in `ci_backend`), following
  `TaskManagerMultiReplicaTests`. DISTILL places the cancel-all broadcast test there.
- **CPU limit.** `Environment.ProcessorCount` honours the container's CPU quota. Docker image (no CPU setting in the
  `Dockerfile`): `--cpus=4` → 2 workers; a 2-core host → 1 worker, so a forecast there takes about twice as long as with
  every core, which is the trade D3 chose and slice 04 offsets. Helm: no CPU limit, so half the node. No config or chart
  change. .NET's own `DOTNET_PROCESSOR_COUNT` overrides the count without a Lighthouse key; **decided** (NMD-8): the
  docs name it in one line, a slice-01 finalize item.

## Wave: DEVOPS / [REF] Mutation testing strategy

`per-feature` (CLAUDE.md, unchanged), ≥ 80% kill rate, run **last on frozen code**, one survivor-kill round plus one
cheap re-run. Backend config `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker.6311.backend.json`, created at
mutation time: whole-file `mutate` entries only (Stryker.NET ignores line spans) and a `test-case-filter` excluding the
`API.Integration` containers and the live categories. StrykerJS in place for the frontend, after the backend, never
concurrently.

| Scope | Files (whole file) |
|---|---|
| Backend, in | `Models/Forecast/ForecastHorizon.cs`, the percentile value object, `ForecastSimulationLimits.cs`, `ForecastBase.cs`, `WhenForecast.cs`, `StartForecast.cs`, `AggregatedWhenForecast.cs`, `DeliveryCompletionForecast.cs`; `Services/Implementation/Forecast/ForecastService.cs`; `…/Update/ForecastUpdater.cs`, `AdmittedCancellations.cs`, `InProcessUpdateCancellationNotifier.cs`; `DeliverySources/DeliveryForecastBlockRenderer.cs` |
| Backend, in with triage | `UpdateQueueService.cs`, `Models/Delivery.cs`, `API/DTO/DtoExtensions.cs`: score and triage only survivors in the changed members, per line from `mutation-report.json` |
| Backend, out | `RedisUpdateCancellationNotifier.cs` (container test excluded from the filter; mutants unscored; its cancel-all is an **accepted gap**, proof-of-concept path, NMD-15), `UpdateController.cs` (one route, acceptance-covered), DTO property files, `StartupBanner.cs` |
| Frontend | the shared "more than N years" formatter, `models/Forecasts/WhenForecast.ts`, `ActivitySection.tsx`, `useTaskManagerPopover.ts`, `UpdateSubscriptionService.ts` |
| Clients | out of the project rule (both stacks = backend and frontend); the clients repo's own tests cover the wording |

Disbelieve any score whose per-file coverage is not checked in `mutation-report.json`.

## Wave: DEVOPS / [REF] Branching strategy

Trunk-based on `main`, unchanged: one focused commit per step, pushed to `origin main` only when green and when the
maintainer releases the hold. Releases cut from `main` by `/release`. No branch, no PR. Clients: trunk on their `main`.

## Wave: DEVOPS / [REF] Coexistence matrix

| Existing thing | Must not break | Guard |
|---|---|---|
| Per-row Stop and its `lighthouse:update-cancelled` channel | yes | Unchanged message and channel; cancel-all on its own channel |
| Deletes on the Task Manager list | yes | Never cancelled (D8); queue test |
| Scheduled updates after Cancel all | yes | Queue test: the next scheduled round runs |
| Write-back rounds of other work | yes | Holds leave their rounds (ADR-253); staged writes still flush |
| Released lh/MCP clients | yes | `expectedDate` kept as a date (NMD-3) |
| Archived Deliveries, snapshot history, pinned closure records | yes | Additive `beyondHorizon` on `WhenDistributionPointDto`; old rows read without it |
| Jira Release block merge over an older block | yes | Renderer + merge test (ADR-254) |
| `HowMany` (reality check, backtests) | yes | Not touched (DESIGN Changed Assumption 1) |
| Seeded forecast baselines finishing inside 5 years | yes | Unchanged; any that move are listed (ADR-251) |
| SonarCloud gate, E2E POMs on the Task Manager | yes | Pre-applied ledger rules; grep `Lighthouse.EndToEndTests/` before any label change |
| Epic #6298 PDF export | yes | Whichever ships second renders the beyond state (DISCUSS pre-requisite) |
| Cancel all across Redis replicas | at most once | **Accepted** (NMD-15): a replica reconnecting at that moment misses it; proof-of-concept path, one real-Redis test |
| Blackout-window sizing when the 95th percentile is beyond | yes | Window sized by the horizon (DDD-8); `FeatureForecastWindowBeyondTheHorizonTest` |
| Standalone desktop on 1-2 cores | runs | Bound 1 worker, forecasts finish, slower; **accepted, not optimised** (1 core is not a realistic host) |

## Wave: DEVOPS / [REF] Changed Assumptions

- **CA-O1: DEVOPS first expected slice 01 to edit `Program.cs` for the banner fact.** Checked at the review gate: the
  banner can read `ForecastSimulationLimits.Default` itself, so `Program.cs` stays untouched (CI points above). Now:
  avoid it in every slice; if one needs it, budget the full Integration run and one re-run.
- **CA-O2: KPI-6 said "Usage-data event if DEVOPS adds one".** Now: none added (NMD-7); KPI-6 dropped as unmeasured.

None changes a product decision.

## Wave: DEVOPS / [REF] Wave decisions

- **[O1]** No new infrastructure, workflow, job, flag, key or dependency; one Redis channel on the existing connection.
- **[O2]** No slice edits `Program.cs`; the banner reads the bound itself. If one must, full Integration run + one re-run.
- **[O3]** Rollback = previous release; no migration, additive wire, the new endpoint goes away. Old binaries show
  today's date for beyond percentiles until the next forecast run (NMD-16): rerun forecasts for every Portfolio.
- **[O4]** Usage data **N/A** (NMD-7); KPI-6 dropped.
- **[O5]** KPI-1..3 on the restored dev instance under `--cpus=4` (KPI-1 also `--cpus=2`; Helm unmeasured); KPI-3
  baseline is a hard slice-01 entry criterion; KPI-4 in CI; KPI-5 by the maintainer. Contracts appended to
  `kpi-contracts.yaml`; no dashboard or alert (KPI-1/2 are one-off soft gates).
- **[O6]** Mutation per feature, whole-file scope as tabled; Redis notifier out.
- **[O7]** Release order: server first, clients any time after; 03 + 04 in one release.
- Final review gate (2026-10-18): conditionally approved, 2 high + 5 medium findings addressed in this section.

## Wave: DEVOPS / [REF] Handoff to DISTILL

**To** `nw-acceptance-designer`: this file, ADR-248..254, `environments.yaml`, and the `OUT-6311-*` contracts.
- Start with the UI walk-through (UI-1..UI-5), then write scenarios.
- Place the cancel-all broadcast test under `Integration/Containers` against real Redis; E2E has no Redis.
- Parametrize the bound over injected core counts 1, 2, 4; never a wall clock (D13).
- Clients: schema test against a recorded payload, with and without `beyondHorizon`.
- Tag KPI scenarios `@kpi-OUT-6311-*` and fill `acceptance_scenarios` in `kpi-contracts.yaml`.
- **Decided** (NMD-8): the slice-05 docs do not mention the rolling-upgrade window; the docs name
  `DOTNET_PROCESSOR_COUNT` in one line (slice-01 finalize).

---

## Wave: DISTILL / [REF] Scope and reconciliation

Slices 01–05 (US-01, US-02, US-03, US-05 = slice 04, US-04 Cancel all = slice 05). Read: this delta (DISCUSS, DESIGN with ADR-248..254, DEVOPS), the
five slice briefs, `docs/architecture/atdd-infrastructure-policy.md`, `docs/product/kpi-contracts.yaml`,
`docs/ci-learnings.md`. `[lang-mode] csharp + typescript` · `[policy-mode] inherit` (two rows appended) ·
`[port-mode] n/a` (this repo asserts state through NUnit/Vitest, no `state_delta` port). Reconciliation:
**0 contradictions** across DISCUSS / DESIGN / DEVOPS. UI copy and layout are NMD-9, NMD-10 and NMD-11, pinned
verbatim and not reopened.

## Wave: DISTILL / [REF] Scenarios

Backend: NUnit, pending marker `[Ignore(PendingEpic6311.SliceNN)]`, tags as `[Category]`. Frontend: Vitest,
`it.skip` with a `pendingEpic6311` reason. "active" = runs green today as a guard. Revised after review: US-05 is
slice 04 (AC-04.1..04.4); the horizon is 1,827 days and a beyond `expectedDate` is `today.AddYears(horizonYears)`
(NMD-12..16).

| Slice | File (tests) | Scenarios | Tags | ACs |
|---|---|---|---|---|
| 01 | `API/Integration/ResponsiveDuringUpdates/Slice01PagesKeepAnsweringScenarios.cs` | The_Portfolios_list_answers_while_a_Portfolio_forecast_is_held_part_way_through | driving_port, kpi-OUT-6311-KPI-1 | 01.1, 01.2 |
| 01 | `Services/Implementation/Forecast/ForecastWorkerBoundTest.cs` | bound is half the cores, never below one (table of 1–64 cores, edge) · exactly max(1, cores/2) runs at once, read off a barrier that holds every run as it begins (TestCase 1/2/4) · manual forecast held to its own bound · **active**: seeded percentiles identical at 1 and 64 cores (fixed starting number) | edge | 01.2, 01.3, 01.4 |
| 01 | `StartupBannerForecastCoresLineTest.cs` | banner line "Forecasts use up to N of M cores", read from the resolved limit, no `Program.cs` change | — | 01.2 |
| 02 | `…/Slice02StopEndsAForecastScenarios.cs` | **WS** A_stopped_forecast_starts_no_further_run_and_its_row_ends_as_cancelled · Features keep the forecast they had · the tracker receives no forecast dates (positive control first: an unstopped forecast does write) · Refresh history records Cancelled · **active**: manual forecast outside an update runs every run | walking_skeleton, driving_port, kpi-…-KPI-2, error ×3, edge | 02.1–02.6 |
| 03 | `Services/Implementation/Forecast/ForecastBeyondTheHorizonTest.cs` | every run counts · beyond flag ⇔ share×10 < p (shares 0–10 × 4 percentiles) · no run finishes → never today · likelihood counts unfinished runs as late · start forecast behind a never-finishing Feature: wire flags 50 false / 85 true, `horizonYears` 5 · circular waits count as beyond · two Teams · **active**: every run finishes → every flag false · Team without history still "cannot forecast" | error ×6, edge ×3 | 03.1–03.4, 03.6 |
| 03 | `Models/Forecast/AggregatedBeyondTheHorizonTest.cs` | two-Team Feature beyond (TotalTrials = N, no `int.MaxValue` key leaks) · two-Team Delivery beyond · **active**: both Teams finish every run → the later Team's day | error ×2, edge | 03.6 |
| 03 | `…/Slice03BeyondTheHorizonScenarios.cs` | date at 50, beyond above it (wire, `horizonYears`, `expectedDate` kept) · no run finishes → never due today · every run finishes → all flags false | driving_port, kpi-…-KPI-4, error, edge | 03.1, 03.2, 03.4 |
| 03 | `…/DeliverySources/DeliveryForecastBlockRendererBeyondTheHorizonTest.cs` | block reads "85%: More than 5 years", others keep dates · every percentile beyond still publishes · merge replaces older block, Team's words kept | error ×2 | 03.5, **03.9** |
| 03 | `API/FeatureForecastWindowBeyondTheHorizonTest.cs` | blackout window for a beyond 95th reaches at least (≥) as far as a day 1,827 days out | error | DESIGN find (blackout window) |
| 03 | `Services/Implementation/WriteBackTriggerServiceTest.BeyondTheHorizon.cs` | two-Team Feature: beyond 85% writes nothing; 50% still written (clock pinned at the fixture's 2026-03-10) | error | 03.7, 03.6 |
| 03 | `API/DTO/WhenDistributionBeyondTheHorizonDtoTest.cs` | snapshot beyond reads back beyond · pre-flag snapshot reads not beyond · history point keeps the `horizonYears` it was forecast with | edge ×2 | 03.5 (chart history), 04.2 |
| 03 | FE `components/Common/Forecasts/ForecastInfo.beyondTheHorizon.test.tsx` | "More than 5 years" not a date · years from payload (7 → "More than 7 years") · icon stays, tooltip "Confident (85% Chance)" + "Fewer than 85% of simulated runs finished within 5 years." · **active**: never "Cannot forecast" (icon and text present first) · within horizon shows a date | edge ×2, error | 03.5, 04.2 |
| 03 | FE `models/Forecasts/WhenForecast.beyondTheHorizon.test.ts` | flag + years survive Feature forecasts, start forecast, Team share, `fromBackend` (each) · manual forecast · **active**: older backend (no flag) reads as a date | edge | 03.4, 03.5 |
| 03 | FE `components/Common/FeatureListDataGrid/columns.forecastedStart.beyondTheHorizon.test.tsx` | forecasted-start column: "More than 5 years" ×3, 50% keeps "10/12/2026" · **active**: drawn as a forecast, not "Cannot forecast" | error | 03.4, 03.5 |
| 03 | FE `pages/Teams/Detail/ManualForecaster.beyondTheHorizon.test.tsx` | manual forecast answer "More than 5 years" ×3 · **active**: answer shown with its 2029 date | error | 03.5 |
| 03 | FE `…/DeliveryGrid/deliveryExportTable.beyondTheHorizon.test.ts` | Feature cells "More than 5 years", 50% keeps date · Delivery row the same · 2 rows × 4 cells, then never the stand-in date, an empty cell or "Cannot forecast" | error | 03.5 |
| 03 | FE `…/DeliveryGrid/timeline/deliveryTimelineModel.beyondTheHorizon.test.ts` | no bar, reason "Ends more than 5 years out at 85%" (each 70/85/95) · **active**: bar within the horizon · finished Feature ends where it stopped | edge ×2 | 03.5 (NMD-10) |
| 03 | FE `components/Common/Charts/DeliveryPredictabilityChart.beyondTheHorizon.test.tsx` | no point that day · tooltip "More than 5 years" · **active**: other days keep their points | edge | 03.5 (NMD-10) |
| 03 | FE `…/DeliveryGrid/DeliverySection.beyondTheHorizon.test.tsx` | chips "85%: More than 5 years" · four chips drawn, none with the stand-in date · **active**: 50% keeps its date | error, edge | 03.5 |
| 04 | `ForecastBeyondTheHorizonTest.cs` | default cap 1,827 days · a centuries-long Feature stops at 1,827 days and reads beyond at 85% · **active**: near-term Features unchanged under the 5-year horizon | edge ×2 | 04.1, 04.3 |
| 04 | `Slice03BeyondTheHorizonScenarios.cs` | beyond percentile carries `horizonYears = 5` and `expectedDate = today.AddYears(5)` | — | 04.2, 03.4 |
| 05 | `…/Slice05CancelAllScenarios.cs` (`POST /api/latest/update/tasks/cancel`) | **WS** Cancel_all_clears_every_queued_refresh_at_once_and_stops_the_running_one · removal left to finish · parked follow-up never runs (read off update notifications) · held forecast let go without running (`IsHeld` false, never completed) · refresh asked for afterwards runs · nothing to cancel → 204 twice · non-admin → 403. Waits are queue quiescence, never a fixed delay | walking_skeleton, driving_port, error ×5, edge | 05.1–05.5 |
| 05 | `…/BackgroundServices/Update/CancelAllWhileWorkIsAdmittedTest.cs` | a click landing between `Admit` and `TryAdmit` still stops that work | error | 05.1 |
| 05 | `Architecture/UpdateHoldsAreForecastOnlyArchUnitTest.cs` | **active** ×2: only forecast work is ever held | — | 05.3 |
| 05 | `Integration/Containers/CancelAllMultiReplicaTests.cs` (requires-docker, real-io) | proof of concept: Cancel all on replica B stops the refresh running on replica A | adapter-integration | 05.6 |
| 05 | FE `components/App/Header/TaskManagerIcon.cancelAll.test.tsx` | "Cancel all" in the Activity heading · tooltip copy · one call, no dialog · "Cancelling…" + spinner, disabled · rows "— Stopping…", removal row "— Queued" · failure "Could not cancel. Try again.", rows back to "— Running"/"— Queued" · list re-read · **active**: hidden when empty ("Nothing is being refreshed right now." first) · hidden for removals only · non-admin subsumed (the control is rendered from the admin check alone) | error ×2, edge ×2 | 05.5, **05.8**, NMD-11 |

**Counts** (TestCase/each rows counted singly): backend 51 (44 pending, 7 active), frontend 40 (28 pending, 12
active), total 91. Backend: 34 of 51 tests carry an `error` or `edge` tag (≈ 67%, ≥ 40%).

**Manual gates, read by the maintainer** (thresholds in the slice briefs): AC-01.5 (p95 of 20
`GET /api/latest/portfolios`, `--cpus=4`), AC-02.7 (Stop latency, 3 tries), AC-03.8 (Features whose dates
change), AC-04.4 (slowest Portfolio forecast vs the P2 baseline), AC-05.7 (Update All then Cancel all). Each is a
reading on the dev instance restored with `Restore-DbBackup.ps1`. **PDF export: not automated here, covered by
Epic 6298** (it shares `deliveryExportTable`'s cells, which the export spec pins).

## Wave: DISTILL / [REF] Walking-skeleton strategy

Two skeletons, both pending at hand-off (6301 precedent): **Stop a forecast** (slice 02) and **Cancel all**
(slice 05). The driving ports are real HTTP calls into the `WebApplicationFactory` host; the triggers behind them
(updates, follow-ups, forecasts) run in-process in that host, not in a separate deployment. Slices 01, 03 and 04
change behaviour behind routes that already exist; their `driving_port` scenarios read the same host. **No
Playwright spec** (E2E stays a thin sanity check); Playwright was not run.

## Wave: DISTILL / [REF] Adapter and driving-port coverage

| Port / adapter | Real or double | Covered by |
|---|---|---|
| `GET /api/latest/portfolios`, `/features` (driving) | HTTP into the test host | Slice01, Slice03 |
| `POST /api/latest/update/tasks/Forecasts/{id}/cancel` (driving, existing) | HTTP into the test host | Slice02 |
| `POST /api/latest/update/tasks/cancel` (driving, **new**) | HTTP into the test host | Slice05 (204, 403, idempotent, removals untouched) |
| `IForecastService` / `ForecastService` | real, with `CountedDrawStream` (fixed seed 6311, barrier) | Slice01–04, unit classes |
| `IUpdateQueueService` + in-process notifier | real | Slice02, Slice05, admit-race test |
| Redis cancellation notifier (**new channel**) | real Redis, Testcontainers, one proof-of-concept test | `CancelAllMultiReplicaTests` |
| Write-back to the tracker | real service, connector mocked; asserted on the field updates the connector receives | Slice02, write-back partial |
| Jira Release block renderer | real | renderer test |
| Delivery snapshot JSON (`WhenDistributionPointDto`) | real | DTO test |
| Startup banner | real `StartupBanner.BuildInfoLines` | banner test |
| Frontend `IUpdateSubscriptionService.cancelAllTasks` | mock (`MockApiServiceProvider`) | cancel-all spec |

Policy rows appended: `CountedDrawStream` for `IDrawStreamFactory`, and real Redis (proof of concept) for the stop
broadcast. The `ITeamMetricsService` waiver and `FakeLighthouseClock` reuse their existing rows.

## Wave: DISTILL / [REF] Scaffolds (RED-ready, zero warnings)

- `ForecastSimulationLimits.MostRunsAtOnce`: throws `NotImplementedException("SCAFFOLD: …")`.
- `DeliveryForecastBlockPercentile(…, bool BeyondHorizon = false)`: accepted, not yet rendered.
- `IUpdateQueueService.CancelAllAsync()` + `UpdateQueueService.CancelAllAsync` throwing SCAFFOLD.
- FE `IWhenForecast` / `WhenForecast`: optional `beyondHorizon`, `horizonYears` (not yet decoded).
- FE `IUpdateSubscriptionService.cancelAllTasks()`: implementation throws SCAFFOLD; the mock resolves.
- Test doubles: `TestDoubles/CountedDrawStream.cs`, `Forecast/OnePortfolioForecast.cs`,
  `ResponsiveDuringUpdates/ForecastWhileItUpdatesAcceptanceTest.cs`, `PendingEpic6311.cs`.

Not scaffolded, left to DELIVER: the cancel-all route (404 today is the RED), `WhenForecastDto.BeyondHorizon` /
`HorizonYears`, `WhenDistributionPointDto.BeyondHorizon` / `HorizonYears`, `ForecastHorizon.Years`, the zod
schema fields, the banner line.

## Wave: DISTILL / [REF] RED classification

Every pending test was un-marked once, run, and re-marked (backend: 51 run, 44 failed, 7 passed, the 7 being the
active guards; frontend: 40 run, 28 failed, 12 passed, the 12 being the active guards, `it.skip.each` rows
included). Every failure is **MISSING_FUNCTIONALITY**; none is BROKEN.

| Group | Failure seen | Class |
|---|---|---|
| Slice01 bound / banner | 6, 5, 5 runs at once against bounds 1, 1, 2; manual forecast 2 > 1; `MostRunsAtOnce` SCAFFOLD; banner has no cores line | MISSING_FUNCTIONALITY |
| Slice02 stop | runs begun after the stop; stored forecast replaced; tracker still receives the 85% date; RefreshLog not Cancelled | MISSING_FUNCTIONALITY |
| Slice03 counting | `TotalTrials` 0/60 not 100; likelihood 100 not 60; window 2027-01-20; write-back plan carries the 85; aggregate drops unfinished runs | MISSING_FUNCTIONALITY |
| Slice03/04 wire + copy | flags and `horizonYears` null; block reads "85%: 2031-10-18"; cap 100,000 not 1,827 | MISSING_FUNCTIONALITY |
| Slice05 backend | `POST …/tasks/cancel` 404; `CancelAllAsync` SCAFFOLD (admit race, Redis) | MISSING_FUNCTIONALITY |
| FE beyond surfaces | stand-in dates rendered ("2031-10-18"), flag and years dropped by the rebuilds, bar drawn, chart point present, no "More than 5 years" | MISSING_FUNCTIONALITY |
| FE Cancel all | no "Cancel all" button in the Activity heading | MISSING_FUNCTIONALITY |

## Wave: DISTILL / [REF] Existing-test disposition (for DELIVER)

- `ForecastRunThatCannotEndTest` asserts `TotalTrials Is.Zero` for the slow Feature; slice 03 contradicts it.
  Rewrite it to the new count in the slice-03 step, not before.
- Fixtures constructing `ForecastService` and `DependencySingleDecisionArchUnitTest` follow any constructor or
  `IForecastService` signature change the bound needs (slice 01).
- `AggregatedWhenForecast` recomputes `TotalTrials` from the histogram; the `int.MaxValue` bucket it gains must
  not leak into the percentiles (the aggregate test pins both).

## Wave: DISTILL / [REF] Pre-requisites and run notes

- Docker for `Integration/Containers` (the class runs in the filtered suite, no `Integration` category).
- Scoped run: `dotnet test --filter "Category=epic-6311-responsive-during-updates&TestCategory!=Integration&…"`
  (the standard exclusions). Frontend: `pnpm vitest run <the nine *.beyondTheHorizon / *.cancelAll files>`.
- P2 (the KPI-3 baseline) was read on 2026-10-18 before slice 01: 1,766 ms cold (see Slice 01 entry and finalize).

## Wave: DISTILL / [REF] Known gaps (for DELIVER)

1. AC-02.4's second half ("staged writes still flush") is not observable at the tracker without a non-forecast
   staged write in the round; the epic-5511 slice 04 scenarios already pin the flush itself.
2. The ran-out-of-days `LogError` that becomes `Debug` has no test (a log level is not a user outcome); DELIVER
   changes it in the slice-04 step.

## Wave: DISTILL / [REF] Upstream issues

1. Resolved by NMD-12: `WhenDistributionPointDto` carries `horizonYears`; the DTO test pins it.
2. Slice 03's Jira Release block wording needs `ForecastHorizon.Years`, which slice 04 introduces. NMD-5 releases
   them together; DELIVER should land the constant at the start of slice 03.
3. Changed Assumption 2 (a Delivery whose runs all fail to finish reads "More than 5 years", not "Cannot
   forecast") is consistent with NMD-9; recorded, no action.

## Wave: DISTILL / [REF] Handoff to DELIVER

- Order: slice 01 → 02 → (03 + 04 together) → 05, Epic 6311 only. Un-mark one scenario at a time, starting with
  each slice's `driving_port` / walking-skeleton scenario, then its unit-level ones.
- Gates at hand-off: `dotnet build` 0 warnings; scoped backend run 51 total, 7 passed, 44 skipped, 0 failed;
  neighbouring classes plus the epic's own 322 total, 278 passed, 44 skipped, 0 failed; Vitest on the nine new
  specs 12 passed / 28 skipped; `pnpm build` clean (Biome, `tsc`, Vite).
- Mutation (per DEVOPS) runs last, on frozen code.
