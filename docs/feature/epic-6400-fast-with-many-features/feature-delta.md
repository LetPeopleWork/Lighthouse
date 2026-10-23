# Feature Delta — epic-6400-fast-with-many-features

**ADO**: Epic #6400 *Lighthouse stays fast with many Features* (state Planned, no child Stories). It replaces Story
#6351 (*Features list opens fast on large Portfolios*, removed so its scope is re-sliced here). Related, shipped:
Epic #6311 *Keep Lighthouse responsive during updates* (forecast bounded to half the cores, in v26.10.19.10) and
Story #6354 *Updates that change nothing skip the forecast* (Resolved, on `main` after the v26.10.19.10 tag).

**Waves**: DISCOVER (2026-10-23), revised the same day after the maintainer's answers and the peer review. Reporter:
Steve Pereira, a customer with Portfolios of 400–920 Features across 8 Teams. No live interview. The evidence:
- the reporter's own description of the Overview and the Features list (Epic #6400 text, and the maintainer's
  answers, 2026-10-23);
- a log excerpt from 2026-10-20 on v26.10.19.10, as transcribed by the maintainer (see Maintainer answers);
- a read of the code at `ead31f93f`.

**One line**: on a large instance, the screens that list Portfolios, Teams and Features read far more than they show,
and much of it scales with **every Feature of the instance**. On top of that, every Team or Portfolio update starts a
forecast that runs for 4–8 minutes over all Features of the Teams involved, and the server stalls while it runs.

**Population served**: instances with Portfolios of a few hundred Features or more. The maintainer expects people to
start out with Portfolios that size (Maintainer answers, 7). Today the evidence comes from one such instance.

**Density**: `lean`, Tier-1 `[REF]` only. `evidence_standard: past_behavior`. Each claim is graded as
**observed behaviour** (a log or measurement), **second-hand report** (of past behaviour), **stated intent**, or
**desk/codebase fact**. Paths are relative to `Lighthouse.Frontend/src/` or `Lighthouse.Backend/Lighthouse.Backend/`.

**Evidence ledger**:
- **Opportunities**: of 10, 4 rest on observed or second-hand behaviour (O3, O5, O9, O10), 2 on stated intent (O1's
  staggered-loading shape, O7), and 4 on desk/codebase fact alone (O2, O4, O6, O8).
- **Where-the-time-goes rows**: all 13 (W1–W10) are desk/codebase fact.
- **Observed behaviour**: limited to the 2026-10-20 log readings and the one ≈50 s list reading.
- **Measurements**: none of our own yet.

---

## Wave: DISCOVER / [REF] Maintainer answers — 2026-10-23

These answer the open questions from the first pass of this wave. No customer, Portfolio, Team or tracker names are
recorded here.

| # | Question | Answer | Effect here |
|---|---|---|---|
| 1 | Version when the heartbeat log was taken | **v26.10.19.10**, the latest release. It **includes** Epic 6311's half-the-cores limit and does **not** include Story 6354. | Stalls during forecasts persist with the limit in place (X5). |
| 2 | Does the 920-Feature Features tab ever finish? | **Yes, after about 50 seconds.** One second-hand reading; whether a forecast was running then is unknown (A13). | Reference symptom for the list. The "never loads, URL too long" hypothesis is invalidated (X6). |
| 3 | Is navigation clickable while the Overview's wheel spins? | **Unknown.** The reporter's own description, paraphrased: opening the Overview often shows a loading wheel when they only want to get into Settings. They would like loading staggered, so the basic scaffold and links (a cached list of Portfolios, Teams and connections) build the page at once and the rest loads in the background. Being held on the Overview is second-hand past behaviour; staggered loading is **stated intent**. | The code finding (W1, links render without waiting) stands. The gap between the report and the code is recorded as unvalidated (A12). |
| 4 | Instance size, database, cores, hosting | **Unknown** beyond a log excerpt of 2026-10-20 on v26.10.19.10 (readings below). | Modelling defaults kept and marked **assumed** (Measurement plan). |
| 5 | System Administrator, RBAC | **RBAC off.** | Every caller is treated as a System Administrator, so the header's Update All button and its extra `/teams` + `/portfolios` pair are on every page. |
| 6 | HAR from the reporter | **None.** | Measurement uses only the scaled data set. |
| 7 | Is a 920-Feature Portfolio a mistake? | **Probably too big, but assume people will start out with Portfolios that size. Lighthouse must cope with it; the warning is a nudge, not the fix.** Threshold 100 stays as the default for DISCUSS to confirm. | O7 stays last; it cannot stand in for the speed work. |
| 8 | Dependency verdicts from a light projection | **Yes.** | D3 holds. |
| 9 | Targets | **Accepted**: first navigation paint < 1 s and first Feature row < 2 s at 920 Features on 4 cores, idle and during a forecast. Revised after the baseline. | K1, K2 (Targets). |
| 10 | Scaled-data generator | **Dev-only.** | Not shipped, no demo scenario. |
| 11 | Paging | **A later Epic.** | Out of scope. |

**Log readings, 2026-10-20, v26.10.19.10.** Grade: observed behaviour, second-hand. These are the maintainer's
transcription of a raw log that is not retained in this repository.

| Round | Forecast | Notes |
|---|---|---|
| Team update (mode=Delta), scanned 71, fetched 0 | 269,719 ms | Nothing changed. On `main`, Story 6354 would skip this round's simulation and save; the gather still runs (W8b). |
| 400-Feature Portfolio update, fetched 1 | 269,746 ms | Update duration 372,736 ms. |
| Team update, scanned 454, fetched 10 | 483,083 ms | Update duration 22,879 ms; the forecast takes 21× as long as the refresh. |
| 920-Feature Portfolio update, fetched 7 | 263,949 ms | Update duration 856,032 ms (≈14 min). About 13 min of that passes between "Updating Features for Portfolio" and the parent-details fetch, so most of it is the read from the work tracking system. |

- Each forecast run names **8 Teams** ("for all Features with involved Teams"). Every run took **264–483 s**.
- A forecast is started by every Team update and every Portfolio update, and covers all Features of all Teams
  involved. With hourly Team and Portfolio updates, that is several 4–8 minute runs per hour.
- **Kestrel heartbeat warnings appear only during forecast runs**: nine readings across the four rounds, 18.5,
  11.8, 18.4, 29.2, 12.7, 1.4, 49.3, 23.9 and 20.0 s.
- Afterwards the operator used **Cancel all**: 5 refreshes stopped, 2 held updates let go.

---

## Wave: DISCOVER / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `delivery-lead-rte` | **Primary.** Opens a large Portfolio's Features list to read dates and progress; waited ≈50 s on the 920-Feature list. |
| `delivery-forecaster`, `product-owner` | **Primary, same screens.** Read the Features list on a Portfolio or a Team. |
| `platform-operator` | **Primary for the forecast stalls; secondary for the Overview.** Wants to get into Settings without waiting on the Overview, and pressed Cancel all after the long forecasts on 2026-10-20. |
| `config-admin` | **Secondary, area 3 only.** Sets up the Portfolio query that returns too many Features. |

No new persona.

| Job | Status |
|---|---|
| `job-operator-keep-lighthouse-usable-while-it-forecasts` (Epic 6311, reused by Story 6354) | **Reused**, now with evidence that it is still under-served on a release with the 6311 limit (heartbeat warnings only during forecasts, 1.4–49.3 s). |
| *(candidate)* "When I open a large Portfolio or Team, I want its Features to appear quickly, so I read dates without waiting through a spinner on every visit." | **Not created.** DISCUSS adds it to `docs/product/jobs.yaml`. It is distinct from the reused job if the list is slow with no forecast running, which is falsifier (a) in the Handoff waiver. |
| *(candidate)* "When I open Lighthouse to change a setting, I want the navigation and the lists of Portfolios, Teams and connections at once, so I am never held by data I did not come for." | **Not created.** Its shape comes from the reporter's stated intent (Maintainer answers, 3). DISCUSS decides whether it is its own job. |

No journey created or extended. The closest is `docs/product/journeys/epic-6311-responsive-during-updates.yaml`. Its
error path reads: "Pages still hang with the bounded simulation → the incident has a second cause". The 2026-10-20 log
is that situation, observed. DISCUSS decides whether to extend that journey or seed
`epic-6400-fast-with-many-features.yaml`.

---

## Wave: DISCOVER / [REF] Opportunity statement

The outcome: on an instance with hundreds of Features per Portfolio, a user can move around Lighthouse, open a
Portfolio's or Team's Features list, and keep doing both while forecasts run. They never wait on work the page does
not need, and they never mistake a slow or failed load for an empty or finished one.

| # | Opportunity (outcome form) | Evidence | Grade |
|---|---|---|---|
| O1 | Minimise the time from opening Lighthouse to a usable navigation and list of Portfolios, Teams and connections | Reporter held on the Overview (second-hand); staggered-loading wish; W1, W2, W3 | **stated intent** for the shape; second-hand report for the wait; desk/codebase fact |
| O2 | Minimise what the server reads to answer "which Portfolios and Teams exist" and "this one Portfolio" | W2, W3, W3b | desk/codebase fact |
| O3 | Minimise the time to the first Feature row on a large Portfolio or Team | One ≈50 s reading on the 920-Feature list; W4–W6, W10 | second-hand report (one reading); desk/codebase fact |
| O4 | Minimise the share of each list request that scales with every Feature of the instance (dependency graph, in-progress lookup) | W4, W5 | desk/codebase fact |
| O5 | Minimise the time requests wait while a forecast runs (isolation: responsiveness, whatever the forecast costs) | Nine heartbeat warnings, 1.4–49.3 s, only during four forecast rounds, on v26.10.19.10; W8 | observed behaviour (log, second-hand) |
| O6 | Minimise the likelihood that a failed or empty list reads as "still loading" | W7 | desk/codebase fact |
| O7 | Minimise the likelihood that a user builds a Portfolio far larger than they need without knowing what it costs | Epic text, area 3; maintainer: "a nudge, not the fix" | **stated intent** |
| O8 | Minimise repeat cost on return to a list the user saw minutes ago | W6 (full refetch on every completed update) | desk/codebase fact |
| O9 | Minimise how long a forecast round runs and how often it repeats (cost reduction) | 264–483 s per run, several per hour, 8 Teams per run; a skipped round still gathers (W8b) | observed behaviour (log, second-hand); desk/codebase fact |
| O10 | Minimise how long a large Portfolio's read from the work tracking system takes | ≈13 of 14 min on the 920-Feature Portfolio update | observed behaviour (log, second-hand). **Candidate for a separate Epic** |

### Opportunity tree

All scores below are **provisional desk estimates**, re-scored on measured shares once the baseline exists (G2).

```
Outcome: Lighthouse stays quick to move around and read on instances with hundreds of Features per Portfolio,
         also while it forecasts
 |
 +-- Responsiveness (isolation): other work does not stall the page you are on
 |     +-- O5 Requests keep answering while a forecast runs                                    provisional 17
 |           +-- request paths async end to end, so a long read does not hold a pool thread
 |           +-- simulation and the forecast's gather/save off the shared request pool
 |
 +-- Cost reduction: each page and each forecast does less work
 |     +-- O2 Scoped server reads (lists and by-id reads stop loading forecasts and histograms)  provisional 15
 |     |     +-- Portfolio/Team list endpoints project id+name+counts; GetById filters in the database
 |     |     +-- FeatureRepository.GetByPredicate filters in the database
 |     +-- O3 + O4 Features list: lighter payload and light instance-wide parts                 provisional 15
 |     |     +-- dependency graph from a projection (ids, references, Portfolios)  (maintainer: yes)
 |     |     +-- in-progress answered as ids from one query, not one whole-table read per Feature
 |     |     +-- a list endpoint keyed by Portfolio/Team id, only the shown columns, parents folded in
 |     +-- O1 Scaffold first (navigation and entity lists before heavy data)                    provisional 14
 |     |     +-- licence-restriction counts from one cheap, shared call instead of /teams + /portfolios per hook
 |     |     +-- one shared, cached authorization summary and licence status (TanStack Query, already in App.tsx)
 |     +-- O9 Fewer and shorter forecast rounds                                                  provisional 13
 |     |     +-- Story 6354 reaches the reporter in the next release (saves simulation and save, not gather)
 |     |     +-- a cheaper gather, so a skipped round is cheap too
 |     |     +-- coalesce rounds that overlap (a Team in N Portfolios, Team then Portfolio update)
 |     +-- O8 Client caching on return; no full refetch on every completed update               provisional 9
 |
 +-- Honest states
 |     +-- O6 A failure look and an empty look for the Features list                            provisional 12
 |
 +-- Nudge
 |     +-- O7 Too-many-Features warning (log and/or Portfolio validation)                        provisional 7 (intent)
 |
 +-- (separate Epic) O10 Faster large reads from the work tracking system
 +-- (later Epic, maintainer) Paging / server-side sort and filter
```

Scores use Importance + max(0, Importance − Satisfaction), 1–10 each, as desk estimates from the code and the log,
not from user ratings. They rank by strength of evidence, not by measured cost.

**Independently shippable** (CLAUDE.md "Each Epic ships on its own"):
- O5, O2, O3+O4, O1, O6 and O7 each deliver value alone.
- O9 partly ships with Story 6354 in the next release; a cheaper-gather or coalescing slice stands alone.
- O8 is worth little until O2 and O3 cut the cost of each read.
- O10 belongs to the connectors, not to this Epic.

---

## Wave: DISCOVER / [REF] Where the time goes (static evidence)

Read on 2026-10-23 at `ead31f93f`. Counts below are **requests per open**, as a function of the Portfolio's Teams
(T). Payload and server work also grow with the Portfolio's Features (F) and, where marked, with **all Features of
the instance (F_all)**.

Two kinds of read recur:
- **P\***: one read of every Portfolio with every Feature, its Team work and its completion forecasts with their
  histogram rows.
- **F\***: one read of every Feature with its Portfolios, Team work, completion and start forecasts with their
  histogram rows, and its dependency links.

All reads below are synchronous EF calls on the request thread unless noted.

| # | Fact | Evidence |
|---|---|---|
| W1 | **Navigation is not gated.** The desktop header renders Overview, Features and System Settings without waiting on anything; the Overview's wheel wraps only the page body. Why the reporter still feels held there is open (A12). | `components/App/Header/Header.tsx:61-66, :150-158`; `pages/Overview/OverviewDashboard.tsx:427` |
| W2 | **The Overview reads every Portfolio's Features and forecast histograms to show names.** `GET /portfolios` loads all Portfolios with Features → FeatureWork → Team → connection options, and Features → Forecasts → SimulationResults. The DTO keeps only each Feature's id and name. `GET /teams` loads all Portfolios the same way. | `API/PortfoliosController.cs:36-61`; `Services/Implementation/Repositories/PortfolioRepository.cs:54-58, :100-111`; `API/DTO/PortfolioDto.cs:16-18`; `API/TeamsController.cs:38-70` (`:47`) |
| W3 | **`/teams` + `/portfolios` are fetched again by every `useLicenseRestrictions()` instance, only to count them.** Each instance keeps its own state and fires `/license`, `/teams` and `/portfolios`. 23 call sites outside tests (search: `useLicenseRestrictions\(\)` in `Lighthouse.Frontend/src`, excluding `*.test.ts*`), among them the header's Update All button (on every page with RBAC off), the Overview, `PortfolioDetail`, `useFeatureOrdering` (inside every Features list) and `DataGridBase` (every grid). First Overview load: 3× `/portfolios` + 3× `/teams` = **6 P\***. | `hooks/useLicenseRestrictions.ts:28-54` (`:31-35`); `components/App/Header/UpdateAllButton.tsx:18`; `OverviewDashboard.tsx:152, :186-190`; `pages/Portfolios/Detail/PortfolioDetail.tsx:193-194`; `hooks/useFeatureOrdering.ts:46`; `components/Common/FeatureListDataGrid/FeatureListDataGrid.tsx:48, :136`; `components/Common/DataGrid/DataGridBase.tsx:181` |
| W3b | **Reading one Portfolio reads all of them.** `GetById` runs `SingleOrDefault` on an `IEnumerable`, so every Portfolio with its Features and histograms is loaded and then filtered in memory. The detail page calls it twice in series (portfolio, then settings), then reads T Team settings. The Team page and the forecast updater read all Portfolios the same way. | `PortfolioRepository.cs:60-66`; `API/APIHelpers.cs:20-31`; `PortfolioDetail.tsx:196-231` (`:209, :211-213`); `API/TeamController.cs:57`; `Services/Implementation/BackgroundServices/Update/ForecastUpdater.cs:125, :156` |
| W4 | **The Features list reads the whole Feature table, with histograms, to answer dependencies, twice.** `GET /features/ids` loads the requested Features (filtered in the database, ordered in memory). Then `WhatTheseFeaturesWaitOn` loads **every** Feature with all includes for the dependency verdicts. The parent column's `GET /features/references` takes the same path, so the whole table is read a second time. The whole graph is read on purpose, so two screens never disagree on a dependency. But it needs only ids, references and Portfolios, not forecasts. | `API/FeaturesController.cs:76-100, :214-231` (`:217` sync `ToList`), `:241-266` (`:254-256`); `Services/Implementation/Repositories/FeatureRepository.cs:40-43, :66-80`; `hooks/useParentWorkItems.ts:24-65` (`:42`) |
| W4b | **Move permissions and the global order are cheap.** Move verdicts are one RBAC lookup per page and an in-memory loop. Positions are a three-column `AsNoTracking` projection of all Features, ordered in memory. | `Services/Implementation/Authorization/FeatureMoveAuthorization.cs:15-30`; `Services/Implementation/FeaturePositionMap.cs:24-38`; `FeaturesController.cs:219, :225` |
| W5 | **"In progress" reads the whole Feature table once per in-progress Feature, per Team.** `GetCurrentFeaturesInProgressForTeam` calls `featureRepository.GetByPredicate` per parent reference. That method loads every Feature with all includes and filters in memory. The result is cached per Team per day for the refresh interval, and every Team refresh runs it too (`UpdateFeatureWipForTeam`). The endpoint returns full `FeatureDto`s; the list uses only `referenceId`. | `Services/Implementation/TeamMetricsService.cs:31-72` (`:43`), `:908`; `FeatureRepository.cs:50-53`; `Services/Implementation/BaseMetricsService.cs:947-965, :1357`; `API/TeamMetricsController.cs:126-138`; `pages/Portfolios/Detail/PortfolioFeatureList.tsx:25-41, :84-99` |
| W6 | **Requests per Portfolio Features tab open** (arriving from the Overview, header already mounted), in order:<br>1. The detail page reads `getPortfolio`, then settings (2 P\*, in series), then T Team settings.<br>2. The list reads `features/ids` (F\*) and T × `featuresInProgress` (up to k_t × F\* each on a cache miss).<br>3. After the rows arrive, it reads `features/references` (F\*).<br>4. Alongside, three `useLicenseRestrictions` mounts each fire `/license`, `/teams` and `/portfolios` (6 P\*), plus one authorization summary and two update-status reads.<br>In all, about **2T + 15 requests, 8 P\* and 2 F\*** before the list is complete. Every completed Features or Forecasts update for the Portfolio re-runs the whole chain. | `PortfolioDetail.tsx:196-231, :304-318, :344-366`; `PortfolioFeatureList.tsx:61, :70-99`; W3, W4, W5 |
| W6b | **The Team Features list has the same shape**: `features/ids`, one `featuresInProgress`, `features/references`; the Team page reads all Portfolios (W3b). A Team's Features span Portfolios, so its list can be longer than any one Portfolio's. | `pages/Teams/Detail/TeamFeatureList.tsx:41, :56-80, :139` |
| W6c | **Each row's payload grows with the Feature's Teams.** Per Feature: four completion and four start percentiles. Per Team: a start and a completion forecast at four percentiles, plus remaining and total work. Also dependencies and Portfolios. | `API/DTO/FeatureDto.cs:22-80` |
| W7 | **The Features list has no failure look and no empty look.** `loading={features.length === 0}`, so a failed request (logged to the console only) or a Portfolio with no Features shows the loading placeholders forever. Minor note: the ids travel in the query string, but the 920-Feature list does load (X6), so this is not a cause of the slowness. | `PortfolioFeatureList.tsx:74-82, :142`; `TeamFeatureList.tsx:139`; `services/Api/FeatureService.ts:25-43` |
| W8 | **The forecast round is the prime suspect for the stalls.** On a release that already has Epic 6311's limit (`Parallel.For` with `MaxDegreeOfParallelism = max(1, cores/2)` inside `Task.Run`), heartbeat warnings appear only during forecasts. Each run still holds pool threads:<br>• up to half the cores' worth of `Parallel.For` workers, plus the thread that waits on them;<br>• the update queue's own loop thread;<br>• a synchronous gather that reads every Portfolio (`GetById`, P\*) and every Feature of the involved Teams with histograms;<br>• a save that rewrites the histograms of all those Features.<br>Every request that arrives meanwhile blocks one more pool thread in its own synchronous P\*/F\* read. Which of these dominates is unmeasured: simulation, gather and save, request reads, or GC pauses from large tracked graphs. | `Services/Implementation/Forecast/ForecastService.cs:82-107, :112-119, :145-154, :205-221`; `Models/Forecast/ForecastSimulationLimits.cs:17, :25`; `Services/Implementation/BackgroundServices/Update/UpdateQueueService.cs:796-800`; `ForecastUpdater.cs:125, :156` |
| W8b | **A forecast covers every Feature of every Team involved, not only the Portfolio's.** Every Team update and every Portfolio update starts one. **Story 6354 saves the simulation and the save, not the gather.** `TheFeaturesWorkedOnBy` and `Gather` run before `IsUnchangedSinceRecorded`, because the fingerprint is built from the gathered inputs. A skipped round therefore still reads all Features of the involved Teams and their throughput. | `ForecastService.cs:89-96` (`:89` read, `:93` gather, `:94` fingerprint, `:96` compare), `:102-104` (simulate, save); log readings above |
| W9 | **Small but real.** `useRbac()` fires its own authorization-summary request per mount (16 call sites, 3 on the Overview). TanStack Query is set up but used only for child items. `ApiHelpers` blocks on `.Result` only for an already-completed task. SQLite runs in WAL mode, so a forecast's save does not block readers. | `hooks/useRbac.ts:29-55`; `App.tsx:237`; `components/Common/FeatureListDataGrid/useFeatureChildItems.ts:31`; `API/APIHelpers.cs:12`; `Data/DatabaseConfigurator.cs:67-68` |
| W10 | **Client-side work for the list**, expected small at 100 rendered rows but not measured:<br>• Every `FeatureDto` is deserialised.<br>• Hide-completed filters in memory.<br>• The community DataGrid paginates on the client at 100 rows a page, with `getRowHeight={() => "auto"}`, so each visible row is measured after it renders.<br>• Per row: `FeatureProgressIndicator` maps the row's Teams, and `getActiveWorkTeams` filters involved Teams against in-progress ids.<br>• `useFeatureChildItems` and `useParentWorkItems` run once per list, not per row.<br>• When the parents arrive, the columns `useMemo` (keyed on `parentMap`) changes, so the whole grid re-renders once more. | `services/Api/FeatureService.ts:41`; `services/Api/BaseApiService.ts:206`; `FeatureListDataGrid.tsx:54-58`; `components/Common/DataGrid/RowRangeFooter.tsx:11`; `DataGridBase.tsx:358-373` (`:369`, `:372`); `components/Common/FeatureListDataGrid/FeatureProgressIndicator.tsx:90`; `PortfolioFeatureList.tsx:61, :69, :104-125` (`:124`), `:127-134` |

What follows:

- **W2 + W3 + W3b** explain a slow Overview and a slow start to every detail page, even with no forecast running.
  The scaffold is expensive because the cheap lists are served, several times, from the most expensive read in the
  product.
- **W4 + W5** are the instance-wide part of the Features list (the Epic's "whole dependency graph"). By the code,
  the global order and move permissions (W4b) are not where the time goes.
- **W8 + W8b**: on a release with the half-cores limit, the stalls still line up with forecast runs, so the forecast
  round stays the prime suspect. The request reads make it worse. Story 6354 removes the simulation from unchanged
  rounds but not the gather. The measurement has to split simulation, gather, save and request reads.
- **W10** has to be measured so the ≈50 s can be split into server, network and client time.

---

## Wave: DISCOVER / [REF] Validated assumptions

Risk score = Impact×3 + Uncertainty×2 + Ease×1 (interviewing-techniques skill); >12 test first. The two symptom
claims are kept apart; neither carries the other.

| # | Assumption | Evidence | Confidence | Risk |
|---|---|---|---|---|
| V1a | **Forecast stalls**: requests stall while forecasts run, on the current release. | Nine heartbeat warnings (1.4–49.3 s) across four forecast rounds, only during those rounds; v26.10.19.10, 2026-10-20. Observed, second-hand (maintainer transcription). | **High** that the stalls happen and coincide with forecasts; their cause inside the round is unmeasured (W8). | 13 |
| V1b | **List slowness**: the 920-Feature Features list is slow on the current release. | One second-hand reading, ≈50 s. Whether a forecast overlapped it is unknown (A13). | **Low-Medium.** A single reading; falsifier (a) in the Handoff waiver tests it. | 14 |
| V2 | The Features list's server work scales with the whole instance, not the Portfolio. | W4, W5. | **High** for the mechanism; its share of any slowness is unmeasured (A11). | 9 |
| V3 | The Overview and every detail page pay for reads they do not show. | W2, W3, W3b. | **High** for the mechanism; share unmeasured. | 9 |
| V4 | The Team Features list has the same cost as the Portfolio one. | W6b. | **High** (same endpoints). | 6 |
| V5 | Client caching is cheap to add. | TanStack Query already set up (`App.tsx:237`) and in use (`useFeatureChildItems.ts:31`). | **High** for feasibility. | 5 |
| V7 | Lighthouse must cope with 920-Feature Portfolios; a warning does not replace the speed work. | Maintainer decision 2026-10-23 (Q7). | **Decision**, not evidence. | — |

---

## Wave: DISCOVER / [REF] Invalidated assumptions

| # | Assumption (as stated or implied) | Evidence against |
|---|---|---|
| X1 | "Move permissions are part of the list's cost." | One RBAC lookup per page and an in-memory verdict per row (`FeatureMoveAuthorization.cs:15-30`, written for 500 Features). **Challenged on the code**; the measurement confirms. |
| X2 | "Reading the global forecast order is part of the cost." | A three-column `AsNoTracking` projection (`FeaturePositionMap.cs:24-28`): instance-wide, but light. **Challenged on the code.** |
| X3 | "The Overview's loading wheel is a page-side gate in front of navigation." | The header's links render without waiting on anything (`Header.tsx:150-158`). **Challenged on the code.** The reporter's experience is real but unexplained (A12). |
| X4 | "The Overview needs heavy data and should load it in the background." | It shows names and counts. The heavy data is read only because the repository's includes load it (W2). The reporter's wish is met by not reading it at all. |
| X5 | "Epic 6311's half-the-cores limit removed the stalls during forecasts" (leaned on in the first pass of this wave). | **Invalidated.** The 2026-10-20 log on v26.10.19.10, which has the limit, shows nine heartbeat warnings, only during forecast runs. |
| X6 | "The 920-Feature list never loads, because 920 ids in the URL exceed Kestrel's 8 KB request line" (was A8). | **Invalidated.** It loads, after about 50 s (maintainer, Q2). |
| X7 | "Story 6354 removes the cost of an unchanged forecast round." | **Corrected.** It removes the simulation and the save; the gather still runs before the comparison (W8b, `ForecastService.cs:89-96`). |

---

## Wave: DISCOVER / [REF] Unvalidated assumptions (need measurement or answer)

| # | Assumption | Why it matters | Risk |
|---|---|---|---|
| A11 | The instance-wide reads dominate over the Portfolio-sized ones. | Instance size, database, cores and hosting are unknown; the scaled data set uses assumed values. **Unvalidated — needs measurement.** | 13 |
| A12 | The reporter is held on the Overview because the Settings page's own requests wait behind a busy server, not because navigation is blocked. | It separates client work (O1) from server work (O2, O5). Alternatives: the reporter's network, browser or proxy. **Unvalidated — settled by the disconfirmation check.** | 12 |
| A13 | The ≈50 s reading was taken with no forecast running. | If a forecast was running, part of the 50 s belongs to O5, not O3/O4. **Unknown; the measurement takes both readings.** | 10 |
| A14 | Story 6354, once released, shortens a large share of the reporter's forecast rounds. | Only the no-change round in the log would be skipped, and that one only in simulation and save; its gather still runs. The three rounds that fetched 1, 7 and 10 items would compute in full. **Unvalidated — gather time measured in step 6; reporter's log after the next release.** | 10 |
| A10 | A warning at about 100 Features leads some users to narrow their query. | A nudge only (V7). Stated intent. **Unvalidated — DISCUSS confirms the threshold.** | 7 |

---

## Wave: DISCOVER / [REF] Dropped options

| Option | Why dropped |
|---|---|
| Defer the Overview's heavy data to the background (area 1 as written) | The Overview needs none of it (X4). Not reading it is cheaper than reading it later. |
| Answer dependencies only from the requested Features | Rejected in code on purpose: the same dependency would be warned about on one screen and silent on another (`FeaturesController.cs:241-248`). The graph stays whole and becomes lighter (maintainer, Q8). |
| Paging | Maintainer: a later Epic (Q11). |
| The warning as the answer to large Portfolios | Maintainer: Lighthouse must cope with them; the warning is a nudge (Q7). |
| Faster reads from the work tracking system (O10) inside this Epic | ≈13 min of the 920-Feature Portfolio update is the tracker read. That is a connector concern with its own evidence and risks, so it is recorded as a candidate separate Epic. |
| Moving the id list out of the URL as a fix | Not a cause (X6). A list endpoint keyed by Portfolio or Team id removes it anyway. |

---

## Wave: DISCOVER / [REF] Targets (proposed KPIs)

| # | Target | Status |
|---|---|---|
| K1 | First navigation paint < 1 s, and Settings usable < 1 s after opening the Overview, idle and during a forecast round. | Accepted by the maintainer (Q9) for navigation; the Settings half is added here, proposed. |
| K2 | First Feature row < 2 s at 920 Features on 4 cores, idle and during a forecast round. | Accepted (Q9). Revised after the baseline. |
| K3 | While a forecast round (computed or skipped) runs over a 920-Feature Portfolio and its 8 Teams, K1 and K2 still hold, and Kestrel logs no heartbeat warning. | Proposed. This is O5's target. |
| K4 | Duration of a computed forecast round, and of a skipped one, at 8 Teams. | Proposed. The figure is set after the baseline; O9 is in scope through this KPI. |
| — | The ≈856 s 920-Feature Portfolio update, ≈13 min of it the tracker read (O10). | **Out of scope** for this Epic's KPIs. A candidate separate Epic, because the time is spent in the connector's read, not in anything this Epic changes. |

---

## Wave: DISCOVER / [REF] Decision gate

| Gate | Status | Why |
|---|---|---|
| G1 Problem | **PASS on the stall symptom; CONDITIONAL on the list symptom** | **Passes on symptoms, not on a causal argument.**<br>(1) **Forecast stalls (V1a)**: nine heartbeat warnings across four forecast rounds on v26.10.19.10, the current release. Observed, second-hand. **Pass.**<br>(2) **List slowness (V1b)**: one second-hand ≈50 s reading, and whether a forecast overlapped it is unknown (A13). **Conditional** on an independent falsifier: if the idle 920-Feature list on the seeded scaled instance loads in under about 10 s, the 50 s is environmental (a forecast running, the reporter's network or host), and G1 fails for the list claim. The stall claim stands either way.<br>Forecast timings are used only to calibrate the data set, by ratio (forecast round vs list load), never as absolute seconds on hardware we do not know.<br>**Why the five-informant threshold does not fit:** that threshold tests whether a pain is shared across people's judgement. A performance symptom is a measurable property of the software at a given data size, so one reproduction on a representative data set tests it more directly than more opinions would. Who is affected is then a question of how many instances reach that size (Population served). |
| G2 Opportunity | **PENDING** | Ten opportunities, scores provisional. The causal claim moves here as a hypothesis, not a finding: the mechanism is deterministic code whose cost grows with every Feature of the instance (W2–W5) and every Feature of the involved Teams (W8b), so **if A11 holds**, any instance of that size would hit it. A11 is unvalidated. G2 passes when the baseline ranks opportunities by measured share of the time and the maintainer accepts the slice order. |
| G3 Solution | **PENDING** | Nothing tested. Here, tested means before/after readings on the scaled data set against K1–K4, taken by DELIVER for each slice. |
| G4 Viability | **CONDITIONAL** until G2 and G3 pass | Lean Canvas N/A: this is a free performance change to an existing product, with no new revenue, channel or pricing. The four risks, each with its evidence:<br>• **Value: amber.** The symptoms are observed on the current release, but on one instance (V1a, V1b). The maintainer states that instances will start at that size (Q7), which is stated intent.<br>• **Usability: green.** Same screens, with loading and failure looks added under the existing CLAUDE.md rule; nothing new to learn.<br>• **Feasibility: amber.** Each candidate fix uses a pattern already in the code: projections (`FeaturePositionMap.cs:24-28`), TanStack Query (`useFeatureChildItems.ts:31`), bounded parallelism (`ForecastService.cs:205-211`). Async EF on the request paths is a wide change, and its share of the stall is unmeasured.<br>• **Viability: green.** No licence change; dependency projection approved (Q8).<br>The maintainer's answers of 2026-10-23 are a direction, not yet a go on a measured case. |

---

## Wave: DISCOVER / [REF] Handoff waiver (maintainer, 2026-10-23)

**AGREED** by the maintainer on 2026-10-23. G2 and G3 stay pending; the measurement moves into the Epic itself.

- **DIVERGE starts now.** It works on directions, not on slice order.
- **DISCUSS goes ahead without readings first.** It makes the measurement the Epic's **first slice**: the dev-only
  scaled data set (P1) and a before/after harness, which ships value on its own because every later slice proves its
  gain with it.
  - That slice takes **(a)** the idle 920-Feature list (time to first row and requests per open, G1's independent
    falsifier for the list claim: < ~10 s means the list claim fails), **(b)** the client-side split of that load
    (server, network and client time, W10), and the forecast-round timings (gather, simulation and save apart) with
    the thread-pool and GC statistics during a round.
- **DISCUSS orders the later slices on the code evidence**, provisionally. Once the first slice's baseline is in, G2
  is re-scored on measured shares and the later slices are re-ordered if the shares say so. Each slice ships on its
  own, so a re-order costs little.
- **Accepted risk:** the order of the slices after the first rests on reading the code, not on measurement.

---

## Wave: DISCOVER / [REF] Measurement plan

**Constraints:**
- The dev instance on :5169 is not running.
- The checked-in dev DB backup holds 102 Features in 3 Portfolios (largest 86) and 3 Teams, so it cannot reproduce
  400–920-Feature cost.
- The reporter has no HAR to share (Q6).
- No measurement has been taken.

**Data set: scaled synthetic seed only**, from a dev-only generator (Q10). It writes through the CSV connector path
that the demo data uses (`Services/Implementation/DemoDataService.cs`, `WorkTrackingConnectors/Csv/`). It creates child
Work Items and dependency links, then runs forecasts so histograms exist.

| Parameter | Value | Status |
|---|---|---|
| Teams | 8, all working on the large Portfolios | from the log |
| Scale points (Features per Portfolio) | 100, 400, 920 | from the reporter, plus 100 for the warning threshold |
| Features in the whole instance | about 3,000 over 6 Portfolios | **assumed** |
| Database | both SQLite and PostgreSQL | **assumed** |
| Host | 4 cores (the target's reference), container or zip | **assumed** |
| RBAC | off | from the maintainer (Q5) |

**Calibration, by ratio**: the reporter saw forecast rounds of 264–483 s and a list of ≈50 s, about 5–10 to 1. The
seeded instance should show a forecast round that is several times its idle list time, and lasts minutes rather than
seconds. If the round takes seconds, the data set's shape is wrong (Features per Team, remaining Work Items,
throughput) and must be fixed before any baseline counts. Absolute seconds are not compared, because the reporter's
hardware is unknown.

**Protocol**:
- **Scale points**: 100, 400 and 920 Features per Portfolio, 8 Teams.
- **Repeats**: each reading is the **median of 5 runs** after one warm-up run, with the min–max spread recorded.
- **Cold start**: a reading right after a restart is recorded separately.
- **Conditions**: every reading is taken idle, and again while a forecast round over the 8 Teams runs.

**What to measure:**

1. **Requests per open** (Overview; Overview then Settings; Portfolio Features tab; Team Features tab): count and
   order, from the Playwright request log.
2. **Server time per request**, from ASP.NET Core `http.server.request.duration` or request logging, for:
   - `/portfolios`, `/teams`;
   - `/portfolios/{id}`, `/portfolios/{id}/settings`;
   - `/features/ids`, `/features/references`;
   - `/teams/{id}/metrics/featuresInProgress`.
3. **Payload bytes** per request.
4. **Database and client work**:
   - queries and rows read per request (EF command logging);
   - row counts of Features, forecasts and histogram rows in the data set;
   - on the client, a Playwright performance trace of the same opens, with long-task count and total long-task time
     and scripting versus rendering time.
5. **Times the user sees**, against K1 and K2, from Playwright performance marks:
   - time to first navigation paint;
   - time until Settings is usable after opening the Overview (A12);
   - time to first Feature row.

   Each is split into server, network and client time (W10). This yields readings (a) and (b) of the Handoff waiver.
6. **Inside a forecast round, computed and skipped** (K3, K4):
   - time in gather (including on a skipped round, W8b), simulation and save;
   - thread-pool queue length, thread count and GC pause time (`dotnet-counters`, `System.Runtime`);
   - Kestrel heartbeat warnings.
7. **Release comparison**: the same rounds on `main` (with Story 6354) and on the v26.10.19.10 tag. This shows what
   the reporter gains from the next release before any of this Epic's slices are credited with it.

**Disconfirmation check** (H4): repeat steps 1, 5 and 6 with the browser throttled to a slow connection, as CLAUDE.md
asks. If, on the scaled set, Settings is usable within K1 after opening the Overview, both idle and during a round, the
reporter's Settings wait has another cause (their network, browser or proxy), and A12 is rejected.

**Reference symptoms**: about 50 s to the 920-Feature list (one reading), forecast rounds of 264–483 s, and heartbeat
warnings up to 49.3 s. Baseline readings are recorded in this delta before any slice; each slice re-reads its own
numbers.

---

## Wave: DISCOVER / [REF] Constraints established

| # | Constraint | Evidence source |
|---|---|---|
| C1 | List reads keep RBAC readability: unreadable Portfolios are never named, and unreadable blockers are "withheld", not dropped. This holds with RBAC on, even though the reporter runs with it off. | `FeaturesController.cs:224, :273-300`; `FeatureDto.cs:66-74` |
| C2 | Dependency verdicts stay computed over the whole graph, so two screens never disagree. The data gets lighter; the answer does not change. | `FeaturesController.cs:241-248`; maintainer, Q8 |
| C3 | A Feature's position is global across the instance, numbered before RBAC filtering. | `FeaturePositionMap.cs:8-11` (ADR-135) |
| C4 | UI gating comes only from `useRbac()`; any sharing or caching of the summary lives inside it. | CLAUDE.md, Architecture |
| C5 | Every new or changed view has a loading look and a failure look. Today the Features list has neither a failure look nor an empty look (W7). | CLAUDE.md "Design for a slow connection" |
| C6 | `GET /api/latest/features` and `FeatureDto` are read by Lighthouse-Clients (CLI/MCP). New lean endpoints are additive; the existing shape stays until the clients move. | Story 6354 delta (US-02 reads `GET /api/latest/features`) |
| C7 | Schema changes (indexes, columns) are expand-only. | project memory, expand-only migrations |
| C8 | Copy uses the configurable terms (Feature, Work Item, Team, Portfolio). | CLAUDE.md; `TerminologySeeder.cs` |
| C9 | No customer company, Portfolio, Team, tracker project or issue names in git. | project rule; maintainer 2026-10-23 |
| C10 | Each Epic that DISCUSS splits off ships alone. | CLAUDE.md |
| C11 | K1–K4 (Targets). | maintainer, Q9; proposed here |
| C12 | A forecast's results stay the same. Isolating it or shortening rounds must not change what it computes. | Epic 6311 and Story 6354 invariants |

---

## Wave: DISCOVER / [REF] Key decisions

- [D1] One workspace, `epic-6400-fast-with-many-features`, carrying Story 6351's scope (see: Epic #6400 description).
- [D2] DISCOVER for a performance Epic asks three things: is it real, who it hits, and where the time goes. It does
  not test market fit. G1 passes on observed symptoms. The causal claim belongs to G2 and waits on A11 (see: Decision
  gate).
- [D3] The Epic's named suspects are re-ranked on the code. The dependency-graph read (W4) and the in-progress lookup
  (W5) are heavy; move permissions and the global order are not (X1, X2). The graph is to be built from a light
  projection (see: Where the time goes; maintainer, Q8).
- [D4] Area 1 is reframed from "load heavy data later" to "stop reading data the scaffold does not show". That answers
  the reporter's stated wish (see: X4; Maintainer answers, 3).
- [D5] Two causes the Epic did not name are recorded: the licence-restriction hook's repeated `/teams` +
  `/portfolios` (W3), and `GetById` reading every Portfolio (W3b) (see: Where the time goes).
- [D6] **Superseded** by D10. The first pass held forecast isolation until the reporter's version was known.
- [D7] No new persona, no new job in `jobs.yaml`, no journey file yet. Two candidate jobs are named for DISCUSS (see:
  Persona IDs).
- [D8] Measurement uses only a scaled synthetic data set from a dev-only generator, with assumed sizes marked and a
  median-of-5 protocol (see: Measurement plan; maintainer, Q6, Q10).
- [D9] **Handoff with G2 and G3 pending goes through the Handoff waiver, agreed 2026-10-23.** DIVERGE now; DISCUSS
  without readings first. The measurement (readings (a), (b) and the forecast-round timings) is the Epic's first
  slice; the later slices are ordered provisionally and re-ordered once G2 is re-scored (see: Handoff waiver).
- [D10] Isolation (O5) and cost reduction (O2, O3+O4, O9) are separate branches. The forecast round is the prime
  suspect for the stalls, and its gather, simulation and save are measured apart (see: W8, W8b; log 2026-10-20).
- [D11] O9 is in scope through K4. Story 6354 saves the simulation and save of an unchanged round, not its gather; its
  gain is measured on its own (step 7) before this Epic claims any of it (see: W8b, X7, A14).
- [D12] The tracker read (≈13 of 14 min on the 920-Feature Portfolio update) is out of scope, as candidate separate
  Epic O10, and is not one of this Epic's KPIs (see: Targets).
- [D13] The URL-length hypothesis is closed as a cause (see: X6).
- [D14] Lighthouse must cope with 920-Feature Portfolios. The too-many-Features warning is a nudge, with threshold 100
  for DISCUSS to confirm (see: V7; maintainer, Q7).
- [D15] Paging is out of scope, for a later Epic (see: maintainer, Q11).
- [D16] Targets K1–K2 are accepted; K3–K4 are proposed; all are revised after the baseline (see: Targets).

---

## Wave: DISCOVER / [REF] Pre-requisites

- **P1** The dev-only scaled data set and the measurement harness above, calibrated by ratio. Built as the Epic's
  first slice; the baseline it records comes before any other slice's DELIVER.
- **P2** The Premium licence fixture in the worktree, if the data set uses Premium dependency-aware forecasting
  (dependency verdicts read the licence, `DependencyDecision.cs:18-25`).
- **P3** ✓ The maintainer's yes on the Handoff waiver (2026-10-23).

---

## Wave: DISCOVER / [REF] Open questions

The maintainer answered Q1, Q2 and Q5–Q11 of the first pass on 2026-10-23 (see Maintainer answers). Still open:

1. ✓ **Handoff waiver**: answered yes on 2026-10-23, with the measurement moved into the Epic's first slice.
2. **Instance size, database, cores, hosting.** Unknown; the data set uses assumed values. *Default: keep the
   assumptions; replace them if the reporter ever shares sizes.*
3. **Is navigation clickable while the Overview's wheel spins?** (A12). Settled by the measurement and the
   disconfirmation check. *Default: no gate on the client; Settings waits behind the server.*
4. **Was a forecast running during the ≈50 s reading?** (A13). Unknown; the measurement takes both readings.
   *Default: assume it may have been, since there are several rounds an hour.*
5. **Too-many-Features threshold and placement** (A10). Left to DISCUSS. *Default: 100, a log line plus a
   non-blocking validation note.*
6. **The reporter's log after the next release** (A14), to see what Story 6354 removed on their instance. *Default:
   ask when the release is out; do not block on it.*
7. **K1's Settings half and K3–K4** are proposed here, not yet accepted. *Default: accept, revise after the baseline.*
