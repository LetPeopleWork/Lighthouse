<!-- markdownlint-disable MD024 -->
# Feature Delta — story-6354-skip-unchanged-forecasts

> ADO User Story #6354 "Updates that change nothing skip the forecast", pulled out of the ideas listed on Epic 6321
> ("skip unchanged forecasts"). The evidence is the logs Steve Pereira sent from a large instance (Portfolios of 400 to
> 920 Features, 6 to 9 Teams). On that instance every Team or Portfolio refresh re-forecasts every Feature of the
> Teams involved, and each forecast takes 4 to 8 minutes even when the refresh found nothing new:
>
> `ForecastUpdater: Update completed | Team 'FSS' | mode=Delta | scanned=71 | fetched=0 | duration=16448ms | success=True | forecast=269719ms`
>
> While those forecasts run, Kestrel reports heartbeat delays of up to 49 s, so the whole app feels slow. The goal is
> to skip a forecast whose inputs have not changed since the last one, so a refresh that found nothing new costs
> seconds instead of minutes. Thread-pool starvation itself is analysed later, after this Story and Story 6351.

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

- ✓ `docs/product/jobs.yaml`: reused `job-operator-keep-lighthouse-usable-while-it-forecasts` (Epic 6311) and added
  a dated reuse note to it. No new job (see JTBD).
- ✓ `docs/product/personas/platform-operator.yaml` and `delivery-forecaster.yaml`, by reference. No persona or
  journey change: the existing personas fit, and no existing journey covers background refresh rounds closely
  enough to extend naturally.
- ⊘ DISCOVER / DIVERGE: none, skipped as for every Story-sized change. The ADO Story quotes the reporter's log
  lines, and those are the evidence.
- ✓ Code read (read-only investigation of 2026-10-23, cited from it; paths relative to
  `Lighthouse.Backend/Lighthouse.Backend/`): `Services/Implementation/RefreshRoundSummaryLine.cs:32`,
  `WriteBackRound.cs:51-54`, `TeamUpdater.cs:55`, `TeamData/TeamDataService.cs:22-25`,
  `TeamDataRefreshedForecastTriggerHandler.cs:14-16`, `PortfolioUpdater.cs:123`, `ForecastUpdater.cs:37-65,
  :88-92, :114-136, :153, :189-196, :205-223`, `Forecast/ForecastService.cs:72, :81, :342`,
  `UpdateQueueService.cs:231-241`, `ForecastController.cs:46, :69`, `TeamMetricsService.cs:110-167, :896-950`,
  `WorkItemService.cs:85-129, :144, :278-289, :681-859, :917-922`, `FeatureRepository.cs:19-21`,
  `SimulatedRun.cs:110-115`, `Dependencies/WhatTheForecastWaitsFor.cs:9-19`, `DrawStreamFactory.cs:14`,
  `API/DTO/WhenForecastDto.cs:25`, `API/DTO/FeatureDto.cs:25`, `Models/Feature.cs:289-317`, `ForecastBase.cs:19`,
  `Services/Implementation/WorkItems/FetchFingerprint.cs`.
- ✓ Frontend read (read-only): `pages/Settings/SystemInfo/RefreshHistorySection.tsx` (Refresh History: per-entity
  average, minimum and maximum duration), `services/Api/TeamService.ts:121-148` and `PortfolioService.ts:111-125`
  (the refresh and "update forecast" buttons' endpoints), `models/Feature.ts:15` (`lastUpdated`, carried but not
  rendered by any page).
- ✓ Docs read: `docs/settings/taskmanager.md` (the "Forecasting Portfolio '<name>'" row, stop semantics),
  `docs/settings/systeminfo.md`, ADR-249 (a cancelled forecast leaves `CreationTime` unchanged), ADR-197 (forecasts
  have no periodic refresh of their own).

## Wave: DISCUSS / [REF] Current-State Surface Inventory (brownfield)

| Surface | Today | Gap |
|---|---|---|
| Team refresh (periodic, `UpdateServiceBase.cs:262-316`, or the Team's refresh button) | Always publishes `TeamDataRefreshed`, which triggers a forecast of every Portfolio of the Team (`TeamDataService.cs:22-25`, `TeamDataRefreshedForecastTriggerHandler.cs:14-16`) | Forecasts even when nothing it reads changed |
| Portfolio refresh (periodic or button) | Triggers the Portfolio's forecast unconditionally (`PortfolioUpdater.cs:123`) | Same |
| One forecast | Every Feature of every Team in the Portfolio, including Features of other Portfolios (`ForecastService.cs:81`); a Team in N Portfolios causes N forecasts over overlapping Features | 4–8 min each on the large instance |
| Refresh summary log line (`RefreshRoundSummaryLine.cs:32`) | `forecast=269719ms` on the Team round that caused it | Cannot say "nothing changed, skipped" |
| Settings → Task Manager | Row "Forecasting Portfolio '<name>'" for the whole run | Sits there for minutes after a no-change refresh |
| Settings → System Info → Refresh History | Forecast rows from `RefreshLog` with their durations | Shows the minutes, with no way to tell a needed forecast from a wasted one |
| Feature `lastUpdated` (API, `FeatureDto.cs:25` = forecast `CreationTime`) | Time the forecast was last computed | Not rendered on any web page; read through the API (and the clients) |
| Existing coalescing (`ForecastUpdater.cs:37-53, :88-92, :114-136`; `UpdateQueueService.cs:231-241`) | Holds a Portfolio's forecast while its Teams are queued; turns duplicates into one follow-up | Still a full forecast per round |

**fetched=0 does not mean nothing changed.** A Delta refresh with `fetched=0` still removes items that left the
query (`WorkItemService.cs:144, :278-289`), recomputes remaining work for every Portfolio of the Team (`:85-93`),
moves automatic Feature WIP (`TeamMetricsService.cs:896, :901-910`), and a Portfolio delta can shrink Feature
membership (`WorkItemService.cs:917-922`). Throughput windows end today (`TeamMetricsService.cs:126, :919-924`).
Hence MD-1.

## Wave: DISCUSS / [REF] Persona IDs

- **platform-operator** (primary): runs the instance, reads the log and the Task Manager, gets the "Lighthouse is
  slow" complaint.
- **delivery-forecaster** (secondary): needs the forecast current after a real change, and the forecast to say
  when it was last confirmed.

## Wave: DISCUSS / [REF] JTBD One-Liners

- #6354 → reused `job-operator-keep-lighthouse-usable-while-it-forecasts`: when Lighthouse recalculates a
  Portfolio's forecasts in the background, I want every page to keep answering. Epic 6311 made a forecast share
  the machine; this Story removes the forecasts that compute nothing new. A reuse note is dated in `jobs.yaml`;
  the wording is unchanged.

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Why |
|---|---|---|
| D1 | **Skip rule is a fingerprint of what the forecast reads** (MD-1). The members are: remaining work per Feature and Team (with the owning Team / Feature-owner split), Feature order, each Team's Feature WIP, dependencies as the forecast resolves them (licence included), each Team's throughput samples after the window, blackout removal and the forecast filter, Feature sizes including defaults and percentile sizes, the forecast settings and simulation limits, the set of Teams and Features in scope, and today's date. | One rule covers every input, including those no event announces. |
| D2 | The fingerprint is **per Portfolio**, because a forecast is per Portfolio. Comparing it never needs the simulation itself. | `TriggerUpdate(portfolioId)` is the unit everywhere |
| D3 | **A user-started refresh always forecasts** (MD-2). That covers the Portfolio's and Team's "update forecast" buttons (`ForecastController.cs:46, :69`), a Team's or Portfolio's refresh button, and the "refresh all" buttons. Only periodic and event-triggered rounds may skip. | Escape hatch if the rule is ever wrong. See the reading note under the MD block |
| D4 | **A skip still counts as a forecast round** (MD-3). The summary line says the forecast was skipped because nothing changed. A Forecast row is still written to `RefreshLog`, marked as skipped (MD-7): Refresh History and the Task Manager show it as a skip, and the forecast duration figures count only computed runs. The Features' forecast "last updated" becomes the time of confirmation. | Silence would look like a lost forecast; a confirmation the reader cannot see is not one |
| D5 | **Unchanged unseeded randomness is a feature, not a bug.** Skipping means the dates stop jittering between no-change rounds. Results are not "refreshed by luck". | `DrawStreamFactory.cs:14` is unseeded; seeding is out of scope |
| D6 | **Rank and ordering-policy triggers stay immediate** (`FeatureRankChangedForecastTriggerHandler.cs:26`, `FeatureOrderingPolicyChangedForecastTriggerHandler.cs:30`). They are not user-started in the MD-2 sense, but they change Feature order and so never match the old fingerprint. | No special case needed |
| D7 | **Fail towards forecasting.** No stored fingerprint, a fingerprint that cannot be computed, or a failed or cancelled last run all count as "changed". | The FetchFingerprint convention: null stored = changed |
| D8 | The fingerprint's member list is pinned by a guard test that names every member, as `FetchFingerprint`'s is, so a new forecast input cannot be added without deciding its place. | Prevents silent staleness later |

### Maintainer decisions — 2026-10-23, before DISCUSS

| ID | Decision | Options weighed |
|---|---|---|
| MD-1 | **Skip rule = input fingerprint.** Store a fingerprint of everything a Portfolio's forecast reads (remaining work per Feature and Team, Feature order, Feature WIP, dependencies, throughput samples incl. blackout removal and forecast filter, sizes incl. defaults, forecast settings / simulation limits, today's date) and skip when it matches the last run's. Mirrors the existing `FetchFingerprint` pattern. | Fingerprint (chosen) vs. the simple rule "every Team fetched 0, no settings change, forecast already from today" (rejected: fetched=0 still removes items, shrinks membership and moves automatic Feature WIP). |
| MD-2 | **Manual "Update forecast" and refresh buttons always forecast** and bypass the skip; only background and automatic refreshes skip. This is the escape hatch if the rule is ever wrong. | Bypass for anything user-started (chosen) vs. skipping everywhere (rejected: no way out if the fingerprint misses an input). |
| MD-3 | **A skipped run stamps the forecasts as confirmed now.** A Feature's forecast "last updated" shows when the forecast was confirmed current, not only when it was last computed. The refresh summary line says the forecast was skipped because nothing changed; DESIGN settles the exact wording. | Confirmed-now stamp (chosen) vs. keeping the compute time (rejected: an old stamp on a current forecast reads as stale). |
| MD-4 | **Coalescing back-to-back Team refreshes' forecasts is a follow-up, out of scope.** The fingerprint already turns repeated no-change runs into cheap skips, and the queue already holds forecasts while Teams are queued. | Follow-up (chosen) vs. in this Story (rejected: a second mechanism for a case the skip mostly covers). |
| MD-5 | **Today's date is part of the fingerprint**, so the first refresh of each day always forecasts. The daily Delivery snapshot (`PortfolioForecastsUpdated` → `GetOrCreateForDay`) and the forecast write-back still happen at least once a day. Settings changes that never triggered a forecast themselves (blackouts, Feature WIP, throughput window, forecast filter, dependency edits) are picked up at the next refresh, as today, because they change the fingerprint. *(Consequence recorded by the orchestrator.)* | Date in the fingerprint (chosen) vs. a date-free fingerprint plus a separate daily trigger (rejected: two mechanisms, and a day boundary is itself an input of the throughput window). |
| MD-6 | **Every refresh a user starts forecasts** (confirms the reading of MD-2): the two "update forecast" buttons, a Team's or Portfolio's refresh button, and "refresh all" for Teams or Portfolios. Only periodic and event-triggered rounds may skip. | Every user-started refresh (chosen) vs. only the two "update forecast" buttons (rejected: a user who presses a Team's refresh would see the forecast skipped). |
| MD-7 | **A skipped forecast keeps its own row in the refresh log, marked as skipped.** Refresh History and the Task Manager show it as a skip, and Refresh History's forecast duration figures count only forecasts that were computed. Showing the marker is a UI change: DISTILL sketches it with the maintainer before the scenarios are written. | Own row, marked skipped (chosen) vs. an ordinary short row (rejected: average, minimum and maximum forecast durations would drop sharply and stop saying how long a real forecast takes) vs. no row (rejected: no trace of the skip in the UI). |

## Wave: DISCUSS / [REF] Scope Assessment

PASS. 1 Story, 1 bounded context (update and forecast background services). Backend only, no new endpoint, no UI
change. 2 slices, each about a day. No walking skeleton, because this is brownfield work on an existing, isolated
path.

## Wave: DISCUSS / [REF] Story Map & Slices

Backbone (platform-operator, one background round): *a refresh runs* → *the forecast is triggered* → *the forecast
decides compute or confirm* → *the round is reported (log, Task Manager, Refresh History)* → *readers see the
forecast and its date*.

| Slice | Outcome | Brief | Est. |
|---|---|---|---|
| 01 | Automatic rounds with unchanged inputs skip the forecast and say so; the "update forecast" buttons still forecast | `slices/slice-01-automatic-rounds-skip-unchanged-forecasts.md` | 1 day |
| 02 | Every user-started refresh forecasts, and a skip stamps the forecasts "confirmed now" | `slices/slice-02-user-refresh-forecasts-and-confirmed-now.md` | ½–1 day |

### Priority Rationale

01 first. It carries all of the production value (the reporter's no-change rounds go from minutes to seconds) and
the riskiest assumption: that the fingerprint captures every input, so that no stale forecast survives a change.
It already keeps the cheapest escape hatch, the two explicit "update forecast" buttons through
`ForecastController`, so it can ship alone. 02 widens the escape hatch to every refresh button and makes the
forecast's date honest. It builds on 01 and is worth less without it.

### Slice taste tests

- 01 cannot be cut thinner safely: a skip that knows only part of the inputs ships stale forecasts. So all of D1 is
  in 01, and the "still re-forecasts" cases are its acceptance tests.
- Neither slice is infrastructure-only. Each changes what an operator sees: the Task Manager row and Refresh
  History durations in 01; a refresh button's result and the forecast date in 02.
- Each has a hypothesis that can fail (see briefs). Each is checked against production-shaped data on the dev
  instance (`:5169`, real history) with the periodic refresh running and nothing changing remotely.
- They are not scale variants of each other. A slice proven only on synthetic data: none.

## Wave: DISCUSS / [REF] User Stories

### US-01 — Automatic rounds that change nothing skip the forecast (slice 01, ADO #6354)

`job_id: job-operator-keep-lighthouse-usable-while-it-forecasts`

As a platform-operator on a large instance, I want a background refresh that changed nothing the forecast reads to
skip the forecast, so the app stays responsive and the round costs seconds instead of minutes.

#### Elevator Pitch
Before: the periodic refresh of Team "FSS" fetches 0 items, then forecasts its Portfolios for 4.5 minutes
(`forecast=269719ms`). Meanwhile "Forecasting Portfolio '…'" sits in the Task Manager and pages stall behind 49 s
heartbeat delays.
After: the next periodic refresh of "FSS" changes nothing the forecast reads. Its log line reads `forecast=skipped
(nothing changed)` (draft wording), the "Forecasting Portfolio" row in **Settings → Task Manager** is gone within
seconds, **Settings → System Info → Refresh History** shows that Portfolio's forecast run in seconds, and the pages
keep answering. Change one input (a blackout, a Feature's order, an item closed in the tracker) and the next round
forecasts as before.
Decision enabled: whether this instance's refresh interval is affordable, judged from forecasts that are needed
rather than from wasted ones.

#### Domain Examples
1. **Happy path.** At 10:15 the periodic refresh of Team "FSS" scans 71 items and fetches 0. Nothing else changed
   since the 10:00 forecast of Portfolio "Payments Platform" (412 Features). The forecast is skipped and the round
   completes in about 17 s instead of 4.5 min.
2. **fetched=0 that still changes something.** At 10:30 "FSS" fetches 0, but item FSS-2210 left the Team's query
   (closed and filtered out). Remaining work for Feature PAY-88 drops from 9 to 8, so "Payments Platform" is
   forecast in full.
3. **New day.** The first periodic round after midnight UTC on 2026-10-24 finds nothing new. The throughput window
   now ends a day later, so the forecast runs, and the day's Delivery snapshot is recorded.
4. **Setting changed between rounds.** Maria adds a blackout for 2026-12-24 to 2026-12-31 on Team "FSS" at 11:02.
   Saving starts no forecast, as today. The 11:15 periodic round fetches 0 but forecasts, because the throughput
   samples changed.
5. **Escape hatch.** At 11:20 the operator presses **Update forecast** on "Payments Platform". The forecast runs
   in full although nothing changed.

#### UAT Scenarios (BDD)

##### Scenario: A background round that changed nothing skips the forecast and says so
Given Portfolio "Payments Platform" was forecast at 10:00 today
And nothing the forecast reads has changed since
When the periodic refresh of Team "FSS" completes with fetched=0
Then no forecast of "Payments Platform" is computed
And the round's summary line says the forecast was skipped because nothing changed
And a Forecast entry for "Payments Platform" is recorded with the round's short duration

##### Scenario Outline: A background round re-forecasts when an input changed
Given Portfolio "Payments Platform" was forecast at 10:00 today
And since then <change>
When the next periodic refresh of a Team of "Payments Platform" completes with fetched=0
Then "Payments Platform" is forecast in full

Examples:
| change |
| the day has turned |
| an item left a Team's query |
| a Team's automatic Feature WIP moved from 2 to 3 |
| a Team's or the Portfolio's forecast settings were saved with a new value |
| a blackout period was added |
| a Feature gained a dependency |
| two Features swapped order |
| a Feature joined or left the Portfolio |
| a Team joined or left the Portfolio |

##### Scenario: A Portfolio forecast that was never stored, or whose last run did not finish, is computed
Given Portfolio "Payments Platform" has no stored fingerprint, or its last forecast failed or was stopped
When the periodic refresh completes with fetched=0
Then "Payments Platform" is forecast in full

##### Scenario: "Update forecast" always forecasts
Given nothing "Payments Platform" reads has changed since its last forecast
When the operator presses "Update forecast" for the Portfolio, or "Update forecasts" for one of its Teams
Then "Payments Platform" is forecast in full

##### Scenario: The day's Delivery snapshot is still recorded
Given it is the first background round of 2026-10-24 and nothing was fetched
When the round completes
Then the Portfolio is forecast and the Delivery snapshot for 2026-10-24 exists

#### Acceptance Criteria
- AC-1.1 A periodic or event-triggered forecast whose fingerprint (D1) equals the last completed run's fingerprint
  for that Portfolio computes nothing and leaves the stored forecasts as they are.
- AC-1.2 Each "still re-forecasts" case forecasts in full: new day, an item that left the query with fetched=0,
  automatic Feature WIP change, Team or Portfolio settings change, blackout change, dependency change, Feature
  order change, a Feature added to or removed from the Portfolio, a Team added or removed.
- AC-1.3 No stored fingerprint, or a failed or cancelled last run, means forecast (D7).
- AC-1.4 Both `ForecastController` "update forecast" endpoints always forecast (MD-2, explicit half).
- AC-1.5 The refresh summary line of a round whose forecast was skipped says so in place of a duration (wording in
  DESIGN). A `RefreshLog` Forecast row is written for the skipped run.
- AC-1.6 The first round of a UTC day forecasts, and the day's Delivery snapshot is recorded (MD-5).
- AC-1.7 A guard test lists every fingerprint member (D8).

#### Outcome KPIs
See K1–K3 under Outcome KPIs.

#### Technical Notes
- Driving ports: `TeamUpdater` / `PortfolioUpdater` / `ForecastUpdater` background services through
  `UpdateQueueService`, and the `ForecastController` endpoints. Assert through `RefreshLog`, the summary line and the
  persisted forecasts, not internals.
- The skip check sits after the inputs are gathered and before the simulation. Where it lives and where the
  fingerprint is stored (likely on the Portfolio, expand-only migration through `CreateMigration`) are DESIGN's call.
- `Clock.TodayAsUtcMidnight` is the date member; the percentile size window reads `DateTime.UtcNow`
  (`WorkItemService.cs:837-840`), which the date member covers.

### US-02 — Every refresh a user starts forecasts, and a skip says the forecast is confirmed now (slice 02, ADO #6354)

`job_id: job-operator-keep-lighthouse-usable-while-it-forecasts`

As a platform-operator, I want any refresh I start by hand to forecast, and I want a skipped forecast to carry the
time it was confirmed current, so I can always force a fresh forecast and never read a current forecast as stale.

#### Elevator Pitch
Before (after slice 01): Maria presses a Team's refresh on the Team page to "make sure", and the forecast is quietly
skipped. Meanwhile the Features' forecast `lastUpdated` stays at 06:00, although the forecast was confirmed at every
round since.
After: on the **Team** or **Portfolio** page, a press of **refresh** (or **refresh all** on the overview) ends in a
full forecast, shown as its "Forecasting Portfolio" row in **Settings → Task Manager** for the usual minutes. After
a skipped background round, each Feature's forecast `lastUpdated` (API, CLI/MCP) reads the round's time.
Decision enabled: whether to trust the forecast's date as "current as of", and when a forced refresh is worth its
minutes.

#### Domain Examples
1. **Team refresh button.** At 14:05 Maria presses refresh on Team "Atlas". It fetches 0, and the forecasts of
   "Payments Platform" and "Onboarding" both run in full.
2. **Refresh all.** At 14:20 the operator presses "refresh all" for Portfolios. Every Portfolio forecasts, whether or
   not its inputs changed.
3. **Confirmed-now stamp.** Feature PAY-88 was computed at 06:00. Background rounds at 06:15 through 09:45 skip. At
   09:46 its forecast `lastUpdated` reads 09:45, and the forecast dates are the 06:00 ones.
4. **A stopped skip-check run changes no stamp.** A round is stopped from the Task Manager before it finishes, so
   `lastUpdated` stays as it was (as ADR-249 does for a cancelled forecast).

#### UAT Scenarios (BDD)

##### Scenario: A Team refresh the user starts forecasts even when nothing changed
Given nothing "Payments Platform" reads has changed since its last forecast
When Maria presses refresh on Team "Atlas", a Team of "Payments Platform"
Then "Payments Platform" is forecast in full

##### Scenario: A Portfolio refresh or refresh-all the user starts forecasts
Given nothing "Onboarding" reads has changed since its last forecast
When the operator presses refresh on "Onboarding", or refresh all for Teams or Portfolios
Then "Onboarding" is forecast in full

##### Scenario: A skipped forecast is stamped as confirmed now
Given Feature PAY-88's forecast was computed at 06:00
When the 09:45 background round skips the forecast of "Payments Platform"
Then PAY-88's forecast reads last updated 09:45
And its forecast dates are unchanged

##### Scenario: A computed forecast still stamps the compute time
Given an input of "Payments Platform" changed
When the next background round forecasts it
Then each Feature's forecast reads last updated at that round's time, with new dates

#### Acceptance Criteria
- AC-2.1 Team refresh, Portfolio refresh and the refresh-all endpoints, when user-started, end in a full forecast of
  every Portfolio they trigger (MD-2, D3). Periodic rounds of the same entities may still skip.
- AC-2.2 A skipped forecast sets each in-scope Feature's forecast "last updated" (`FeatureDto.LastUpdated`) to the
  skip time and leaves the forecast values and histograms untouched (MD-3).
- AC-2.3 A stopped or failed round changes no stamp.
- AC-2.4 A computed forecast stamps as today.

#### Outcome KPIs
See K3 (guardrail) and K4 under Outcome KPIs.

#### Technical Notes
- A user-started refresh reaches the forecast today only through the queue and `TeamDataRefreshed` /
  `PortfolioUpdater.cs:123`. The "user-started" origin has to travel with it; how is DESIGN's call. Mind
  `UpdateQueueService.cs:231-241`: a manual request that merges into a queued automatic one must keep the bypass.
- `FeatureDto.LastUpdated` is read by the API clients. The payload shape does not change, but its meaning does
  (see Standing Checklist).

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measure | Baseline |
|---|---|---|---|
| K1 Share of background rounds with unchanged forecast inputs whose forecast is skipped | ≥ 95 % (100 % by design) | Refresh summary lines and `RefreshLog` Forecast rows on the reporter's instance and the dev instance; DEVOPS instruments | 0 % |
| K2 Forecast wall clock on a skipped round, large instance | < 5 s | `forecast=` on the summary line | 4–8 min (`forecast=269719ms`) |
| K3 Guardrail: a forecast left stale after an input change | 0 (every input change → the next round forecasts) | Acceptance tests per D1 member; a maintainer spot check on the dev instance; no "forecast did not update" report in the 60 days after release | n/a |
| K4 Kestrel heartbeat warnings on a no-change day, large instance | Clear drop (direction, not a number: thread-pool starvation is analysed later) | The reporter's log excerpt after the release, compared with the one in the Story | up to 49 s delays during each forecast |

Usage-data event: left to DEVOPS. The expected answer is "N/A, because a skip is not a user action". The operator
already sees the effect in the log and Refresh History.

## Wave: DISCUSS / [REF] Definition of Done

1. ACs green as NUnit acceptance tests through the driving ports. No new E2E, because no UI changes. The existing
   Task Manager E2E keeps passing.
2. `dotnet build` (zero warnings) and the filtered `dotnet test` are clean. The frontend is untouched.
3. Sonar: no new issues; `docs/ci-learnings.md` pre-applied.
4. Docs at finalization: `docs/settings/taskmanager.md` says a forecast whose inputs did not change is confirmed in
   seconds rather than recomputed, and that a button press always recomputes. `docs/settings/systeminfo.md` gets a
   sentence on short Forecast runs in Refresh History if DESIGN keeps the row.
5. Screenshots: N/A, because no captured image shows a duration or a forecast's last-updated time; finalization
   checks this. Demo data: N/A, because no data shape changes.
6. Copy (log line) uses the configurable terms where it names entities (Portfolio, Feature, Team).
7. ADO: Story #6354 Active on first commit, Resolved on push, Closed on green CI. The Release Notes tag is for the
   maintainer to confirm (a user-noticeable speed-up).
8. Mutation testing (Stryker.NET) on touched backend files ≥ 80 %, run last on frozen code.
9. `ARCHITECTURE.md` updated if the forecast trigger concept changes.

## Wave: DISCUSS / [REF] Out of Scope

- Coalescing the forecasts of back-to-back Team refreshes (MD-4, follow-up).
- Thread-pool starvation analysis (after this Story and Story 6351).
- Story 6351 (Features list speed).
- Seeding the simulation (D5).
- Any UI change beyond what MD-3 changes about "last updated". No new control, badge or column.
- Making settings saves (blackouts, WIP, filter, dependencies) trigger a forecast themselves (MD-5: as today).
- Narrowing a forecast to the Features whose inputs changed (a partial forecast).

## Wave: DISCUSS / [REF] WS Strategy, Driving Ports, Pre-requisites

- **WS strategy**: A (no walking skeleton; brownfield, isolated path).
- **Driving ports**: the Team, Portfolio and Forecast update background services (through `UpdateQueueService`),
  the `ForecastController` endpoints (`forecast/update/{id}`, `update-portfolios-for-team/{teamId}`), and the
  Team/Portfolio refresh and refresh-all endpoints. The HTTP contracts do not change.
- **Pre-requisites**: none. Existing fixtures in `Tests/API/Integration/DependencyAwareForecasting/
  Slice00OneForecastPerBatchScenarios`, `FasterUpdates/*` and `ResponsiveDuringUpdates/*` are the natural homes.

## Wave: DISCUSS / [REF] Standing Checklist

- **RBAC**: N/A, because no endpoint, guard or gated control is added or changed. The refresh and "update forecast"
  endpoints keep their existing write guards, and the skip happens inside the background service.
- **Lighthouse-Clients (CLI + MCP)**: no version bump needed, because no payload shape changes. However, the meaning
  of a Feature's `lastUpdated` changes in slice 02, from "computed at" to "confirmed current at" (MD-3). DESIGN greps
  `lighthouse-clients` (outside this checkout, so not verified in this wave) for `lastUpdated` and adjusts any
  wording that says "computed" or "calculated". If none reads it, this item is N/A.
- **Website**: expected N/A, because no feature or copy the website names changes. A faster refresh is not
  advertised as a feature. The website repo is outside this checkout, so finalization confirms with a grep for
  "forecast" speed claims.
- **Usage data**: deferred to DEVOPS, as the standing rule requires. Expected "N/A, because a skip is not a user
  action".
- **Terminology**: the log line and docs use Portfolio, Feature and Team (the seeded defaults).
- **UI sketch**: needed, because MD-7 marks a skipped forecast in Refresh History and the Task Manager. DISTILL
  sketches that marker, and which duration figures exclude skips, with the maintainer before writing scenarios.
  The log-line wording is settled in DESIGN (MD-3).
- **Loading and failure looks**: N/A, because there is no new or changed view.
