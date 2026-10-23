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
| V1b | **List slowness**: the 920-Feature Features list is slow on the current release. | One second-hand reading, ≈50 s. Whether a forecast overlapped it is unknown (A13); by the round frequency in the log, the prior for an overlap is about 20–80%, so this reading is not independent of V1a. | **Low-Medium.** A single reading; falsifier (a) in the Handoff waiver tests it (idle under ~10 s fails the claim; the target K2 is a separate < 2 s). | 14 |
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
| A13 | The ≈50 s reading was taken with no forecast running. | If a forecast was running, part of the 50 s belongs to O5, not O3/O4. **Prior (added in DIVERGE)**: several 4–8 minute rounds an hour keep a forecast running roughly 12–48 minutes of every hour, so a random reading overlaps one about **20–80%** of the time. The list symptom is therefore not independent of V1a. **Unknown; the measurement takes both readings.** | 10 |
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
| G1 Problem | **PASS on the stall symptom; CONDITIONAL on the list symptom** | **Passes on symptoms, not on a causal argument.**<br>(1) **Forecast stalls (V1a)**: nine heartbeat warnings across four forecast rounds on v26.10.19.10, the current release. Observed, second-hand. **Pass.**<br>(2) **List slowness (V1b)**: one second-hand ≈50 s reading, and whether a forecast overlapped it is unknown (A13). **Conditional** on an independent falsifier: if the idle 920-Feature list on the seeded scaled instance loads in under about 10 s, the 50 s is environmental (a forecast running, the reporter's network or host), and G1 fails for the list claim. The stall claim stands either way.<br>**Falsifier vs target (clarified in DIVERGE, 2026-10-23)**: the ~10 s falsifier is a go/no-go for the *claim*, not the target. The target is K2, first row under 2 s. Between 2 s and 10 s the claim stands and the target is unmet.<br>Forecast timings are used only to calibrate the data set, by ratio (forecast round vs list load), never as absolute seconds on hardware we do not know.<br>**Why the five-informant threshold does not fit:** that threshold tests whether a pain is shared across people's judgement. A performance symptom is a measurable property of the software at a given data size, so one reproduction on a representative data set tests it more directly than more opinions would. Who is affected is then a question of how many instances reach that size (Population served). |
| G2 Opportunity | **PENDING** | Ten opportunities, scores provisional. The causal claim moves here as a hypothesis, not a finding: the mechanism is deterministic code whose cost grows with every Feature of the instance (W2–W5) and every Feature of the involved Teams (W8b), so **if A11 holds**, any instance of that size would hit it. A11 is unvalidated. G2 passes when the baseline ranks opportunities by measured share of the time and the maintainer accepts the slice order. |
| G3 Solution | **PENDING** | Nothing tested. Here, tested means before/after readings on the scaled data set against K1–K4, taken by DELIVER for each slice. |
| G4 Viability | **CONDITIONAL** until G2 and G3 pass | Lean Canvas N/A: this is a free performance change to an existing product, with no new revenue, channel or pricing. The four risks, each with its evidence:<br>• **Value: amber.** The symptoms are observed on the current release, but on one instance (V1a, V1b). The maintainer states that instances will start at that size (Q7), which is stated intent.<br>• **Usability: green.** Same screens, with loading and failure looks added under the existing CLAUDE.md rule; nothing new to learn.<br>• **Feasibility: amber.** Each candidate fix uses a pattern already in the code: projections (`FeaturePositionMap.cs:24-28`), TanStack Query (`useFeatureChildItems.ts:31`), bounded parallelism (`ForecastService.cs:205-211`). Async EF on the request paths is a wide change, and its share of the stall is unmeasured.<br>• **Viability: green.** No licence change; dependency projection approved (Q8).<br>The maintainer's answers of 2026-10-23 are a direction, not yet a go on a measured case. |

---

## Wave: DISCOVER / [REF] Handoff waiver (maintainer, 2026-10-23)

**AGREED** by the maintainer on 2026-10-23. G2 and G3 stay pending; the measurement moves into the Epic itself.

- **DIVERGE starts now.** It works on directions, not on slice order.
- **DISCUSS goes ahead without readings first.** It makes the measurement the Epic's **first slice**: the dev-only
  scaled data set (P1) and a before/after harness. Every later slice proves its gain with it.
  - **Correction (DIVERGE, 2026-10-23)**: this used to say the slice "ships value on its own". It does not. It is an
    internal enabler, dev-only, with no user-visible value. The Epic's first user-visible value is the next slice,
    which takes the forecast round off the request pool (`recommendation.md`).
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

---

## Wave: DIVERGE / [REF] Maintainer answers (DIVERGE) — 2026-10-23

Answers to the eight questions that DIVERGE handed over. No customer, Portfolio, Team or tracker names.

| # | Question | Answer | Effect |
|---|---|---|---|
| 1 | Too-many-Features nudge | Threshold 100; a log line plus a non-blocking note; last slice. Yes. | Slice 7. Copy is DISCUSS's. |
| 2 | Overlap with Epic 6264 | Default accepted ("ok fair"). | S05b stays 6264's. This Epic only adds database-side reader methods for its hot callers. 6400 lands first, and 6264's S05c–e adopt them. The S05a ratchet only shrinks. S13's Feature → Forecast navigation is left in place. Recorded for 6264's DESIGN; 6264's files are not edited. |
| 3 | Lighthouse-Clients | Keep it simple. New endpoints are additive; the clients stay on `FeatureDto` and the current Features endpoint. | C6 unchanged. |
| 4 | Hide-completed | Stays client-side. Yes. | The lean list returns all Features. |
| 5 | K1's Settings half, K3, K4 | Accepted; figures set after the baseline. | K1's Settings half is credited only when slice 1 shows the server holds Settings back (A12). |
| 6 | Harness readings as CI budgets | Not in this Epic. | — |
| 7 | Journey | Seed `epic-6400-fast-with-many-features.yaml`, and link the 6311 journey's error path. | For DISCUSS. |
| 8 | Usage data | Extend the existing "A Portfolio tab was opened" and "A Team tab was opened" events (`docs/settings/usagedata.md`) with how many Features there are. | For DEVOPS, recorded as a **closed size-bucket enum** on those two events, for example `Under100` / `100To399` / `400Plus` (DEVOPS fixes the buckets). Never the raw count, and never which Portfolio or Team. |

**Orchestrator decision, 2026-10-23.** It was taken under the maintainer's "continue autonomously", and the
maintainer may overturn it: **K3 is an exit criterion of this Epic.** Option 3's round-isolation step becomes an
unconditional slice, right after the measurement.

## Wave: DIVERGE / [REF] Job and outcomes

Full text: `diverge/job-analysis.md` (revision 1). Desk estimates, 1–10; `jobs.yaml` uses 1–5, halved and
rounded.

- **Reused**: `job-operator-keep-lighthouse-usable-while-it-forecasts`, wording unchanged. It is still under-served
  on a release with the half-the-cores limit (X5).
- **New**: `job-reach-and-read-what-i-came-for-on-a-large-instance` (`delivery-lead-rte`, `delivery-forecaster`,
  `product-owner`; `platform-operator` on the way to Settings).
  - The job: "… I want to get what I came for without waiting on data I did not ask for, so I keep using Lighthouse
    for delivery decisions instead of falling back to the tracker or a spreadsheet."
  - It merges DISCOVER's two candidates.
  - It is not independent of the stall: a random reading overlaps a round about 20–80% of the time.
- **Outcomes**:

  | # | Outcome | Target | Score |
  |---|---|---|---|
  | OS1 | Minimise the time a page waits for an answer while a forecast runs | K3 | 16 |
  | OS2 | Minimise the time to read the first Feature of a large Portfolio or Team | K2 | 14 |
  | OS3 | Minimise the time to reach the screen I came for | K1 | 10 |
  | OS4 | Minimise the time one forecast round takes | K4 | 10 |
  | OS5 | Minimise the likelihood that a slow or failed load reads as empty | C5 | 10 |
  | OS6 | Minimise the likelihood of an oversized Portfolio built unknowingly | O7 | 5 |

## Wave: DIVERGE / [REF] Comparables

Full text: `diverge/competitive-research.md`. Sources fetched 2026-10-23.

| Comparable | What it does | What it means here |
|---|---|---|
| Jira Cloud boards | A 5,000-issue display cap with a warning | An honest state at the limit; the push-back-on-size direction |
| Jira Plans | A bounded calculation; "exclude completed work" | The forecast round is the thing to bound |
| ActionableAgile | Fetches less per load | Fetch only what is shown |
| Linear (non-obvious) | Local-first sync | Its cheap half, the shell from cache, is TanStack Query's stale-while-revalidate |
| Recording rules and CQRS read models | Precompute the expensive answer | Our forecasts already are precomputed; the reads around them are what is heavy |
| .NET thread-pool starvation guidance | Diagnose with dotnet-counters; async end to end | Isolation is a lever separate from cost |

## Wave: DIVERGE / [REF] Recommendation

Full text: `recommendation.md` · artifacts: `diverge/` · decisions DV-1..DV-11: `wave-decisions.md`.
Revision 1, after peer review round 1.

- **Matrix** (weights DVF 30 / Subtraction 15 / Concept count 15 / Staging 10 / Speed-as-trust 30, with numeric
  rubrics):
  1. "Scope every read to what the screen shows" **3.95**
  2. "The forecast gets its own lane" 3.60
  3. "Shell first, then a client cache" 3.05, which wins the tie with read models (3.05) on DVF
  4. In-flight first plus a nudge 2.95
  5. Forecast less often 2.75
- **Direction: a stated hybrid.** Option 1's direction, plus Option 3's round-isolation step as slice 2: the round
  runs off the request pool, on a dedicated worker with bounded parallelism, with no async request paths. The hybrid
  scores 3.85, and 3.95 in the most likely case (S7). Under the K3 exit criterion it is the best row that moves both
  conditions.
- **Why a hybrid**: the surer problem (the stalls, V1a) and the surer code change (the scoped reads, W2–W5) sit in
  different options.
- **Endpoints**: additive; the clients stay on `FeatureDto` (C6). Forecast results are unchanged (C12).
- **Falsifier and target, separately**:
  - The falsifier is the idle 920-Feature list under ~10 s. It fails the list claim.
  - The target is K2, under 2 s.
- **Triggers**, as ratios on the scaled set:
  - Q_r is the round-attributable share of queue and heartbeat time. S_q is the request-read share of request wall
    time during a round.
  - **Dissent A**: Q_r ≥ 0.6, S_q ≤ 0.3, and idle already met. Option 3 alone would have sufficed, so slices 4–5
    become optional.
  - **Dissent B**: K3 still fails after slice 2 or at the end, and S_q > 0.3. Open the async-request-paths Epic.
  - Prior: 9 of 9 warnings inside rounds has a chance probability of p⁹ ≤ 0.13.
- **Provisional slice order** (Handoff waiver):
  1. Measurement. An **internal enabler, not user value**.
  2. The forecast round off the request pool. **K3, and the first user-visible value.**
  3. Scaffold reads. K1 idle; the Settings half is conditional on A12.
  4. The Features list's instance-wide parts, with response shapes unchanged.
  5. The lean Features list with loading, failure and empty looks. **Depends on 4.**
  6. A lighter forecast gather. It touches C12; outputs and the Story 6354 fingerprint must be identical on SQLite
     and PostgreSQL.
  7. The too-many-Features nudge at 100.

  Slices 3, 4 and 6 swap freely; 5 must follow 4; 1 and 2 are fixed first; 7 is last. Every slice states its
  loading and failure look, or "N/A, because …". Slices 5 and 7 are sketched at the start of DISTILL.
- **Candidate follow-up Epics**: async request paths (Dissent B); forecast less often; client cache; tracker read
  (O10); paging.
- **Into DISCUSS**: DISCOVER's G4 is CONDITIONAL, and G2 and G3 are PENDING (Handoff waiver). Also: confirm the K3
  exit-criterion decision with the maintainer; the nudge copy; the journey's content.

---

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-10-23 · **Mode**: autonomous subagent under the maintainer's
"continue autonomously"; decisions that are the maintainer's carry a default and are listed under "Questions for the
maintainer". Config: feature type **cross-cutting** (backend reads and the forecast round; two slices change what a
user sees), walking skeleton **depends** (brownfield, evaluated below), research depth **lightweight**, JTBD **reused
from DIVERGE, not re-run**, density lean. Per-wave peer review not run (the consolidated review runs at the end of
DISTILL).

| Read | Status |
|---|---|
| `CLAUDE.md` (no silent N/A; per-feature docs; DEVOPS usage-data question; configurable terms; sketch UI at the start of DISTILL; loading and failure looks at DESIGN; each Epic ships on its own) | ✓ |
| `feature-delta.md` DISCOVER + DIVERGE sections | ✓ |
| `recommendation.md`, `wave-decisions.md` (DV-1..DV-11) | ✓ — locked inputs |
| `diverge/job-analysis.md`, `diverge/review.yaml` | ✓ |
| `diverge/options-raw.md`, `diverge/taste-evaluation.md` | ✓ structure only; the recommendation and DV-3..DV-6 carry their results |
| `diverge/competitive-research.md` | ⊘ not read; summarised in the DIVERGE Comparables section |
| `docs/product/jobs.yaml` (both jobs) | ✓ — unchanged, no refinement needed |
| `docs/product/journeys/epic-6311-responsive-during-updates.yaml` | ✓ — its error path is linked from the new journey |
| `docs/product/vision.md` | ⊘ not found |
| `docs/product/architecture/brief.md` | ✓ outline only; nothing in it is binding beyond CLAUDE.md's hexagonal rule |
| Precedents: DISCUSS of `epic-6303-configure-once-per-connection` and `epic-6298-report-pdf-export`; `story-6354-skip-unchanged-forecasts/slices/` | ✓ |
| `epic-6264-modular-monolith/feature-delta.md` S05a–S05g, S13 | ✓ — DV-9 kept as is |
| Brownfield: `UsageDataEventName.cs`, `docs/settings/usagedata.md`, `PortfolioService.ts` (validate), `WidgetShell.tsx`, `docs/portfolios/` | ✓ (targeted, below) |

No DISCUSS decision contradicts DISCOVER evidence. Two DIVERGE wordings change (see Changed Assumptions).

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory (brownfield)

| # | What exists | Where | Consequence |
|---|---|---|---|
| S1 | Header links render without waiting (W1) | `components/App/Header/Header.tsx` | K1's navigation half may already hold; slice 01a measures it before anything is credited. |
| S2 | `GET /portfolios`, `GET /teams` read every Portfolio with every Feature and its histograms (W2); their bodies are read by the clients | `PortfoliosController.cs`, `TeamsController.cs`, `PortfolioRepository.cs` | Slice 02 reads less behind **unchanged** bodies (C6). |
| S3 | 23 `useLicenseRestrictions()` mounts each fetch `/license`, `/teams`, `/portfolios` to count (W3) | `hooks/useLicenseRestrictions.ts` | Slice 02 asks once per page; the count keeps today's meaning. |
| S4 | `PortfolioRepository.GetById` loads every Portfolio and filters in memory (W3b) | `PortfolioRepository.cs:60-66` | Slice 02 (detail pages), slice 05 (forecast gather). |
| S5 | Features list: `features/ids`, `features/references`, T × `featuresInProgress`; dependency verdicts and in-progress read the whole Feature table with histograms (W4–W6b) | `FeaturesController.cs`, `TeamMetricsService.cs`, `FeatureRepository.cs` | Slice 03 (same responses), slice 04 (lean endpoint). |
| S6 | Features list shows loading placeholders forever on failure or when empty (W7); hide-completed and paging (100 rows) are client-side | `PortfolioFeatureList.tsx`, `TeamFeatureList.tsx`, `FeatureListDataGrid.tsx` | Slice 04 adds failure and empty looks. |
| S7 | Forecast round: `Parallel.For` at half the cores inside `Task.Run`; synchronous gather and save on pool threads; Story 6354's skip decides after the gather (W8, W8b) | `ForecastService.cs`, `UpdateQueueService.cs`, `ForecastUpdater.cs` | Slice 01 moves the round off the request pool; slice 05 lightens the gather. |
| S8 | Task Manager rows and Settings → System Info → Refresh History show forecast runs, skips and durations | Epic 6311, Story 6354 | The operator's existing window on a round; slices 01 and 05 are read there. |
| S9 | Usage data: `TeamTabOpened = 0`, `PortfolioTabOpened = 1`, with "which tab" as their only property; last integer on this checkout `WorkTrackingSystemFieldsChanged = 19` | `Models/UsageData/UsageDataEventName.cs`; `docs/settings/usagedata.md:43-44, :91` | The size bucket is a new closed property on two existing events, not a new event. |
| S10 | `WidgetShell` status (dimmed under a spinner, lone spinner, could-not-load) and `data-widget-status`; TanStack Query set up | `pages/Common/MetricsView/WidgetShell.tsx`; `App.tsx` | Reuse candidates for slice 04's looks and slice 02's shared count. |
| S11 | `POST /portfolios/validate` answers a boolean only; a Portfolio's Features count exists only after an update stored them | `services/Api/PortfolioService.ts:129-143` | Slice 06's note reads the stored count, not the validation. |
| S12 | Docs: `docs/portfolios/detail.md`, `edit.md`, `portfolios.md`; Team docs under `docs/teams/`; `docs/settings/usagedata.md` | `docs/` | Finalize targets. |
| S13 | Dev DB backup: 102 Features in 3 Portfolios (largest 86), 3 Teams | DISCOVER Measurement plan | Real-history **regression** data for every slice; too small for timings or the nudge. |
| S14 | Epic 6264's S05a baseline lists `FeaturesController`, `TeamMetricsService`, `PortfolioMetricsService` | `epic-6264-modular-monolith/feature-delta.md` | Every slice keeps or shrinks their entries (DV-9). |

---

## Wave: DISCUSS / [REF] Persona IDs

All names below are fictional, and so are the scaled data set's Portfolio and Team names (D29). No customer, Portfolio,
Team or tracker name from the report appears here.

| Persona | Example | Role here |
|---|---|---|
| `delivery-lead-rte` | **Martin Achterberg**, Head of Delivery. Opens Portfolio *Northwind Platform* (920 Features, 8 Teams) several times a day to read dates before steering meetings. | **Primary**, slices 03–04. |
| `product-owner` / `delivery-forecaster` | **Aisha Khan**, Product Owner of Team *Borealis*, one of the 8 Teams, with 310 Features across 3 Portfolios. Opens the Team's Features tab. | **Primary**, slices 03–04 (Team tab). |
| `platform-operator` | **Jonas Weber**. Self-hosts Lighthouse in Docker on a 4-core VM, RBAC off, hourly Team and Portfolio updates. Wants Settings without waiting; has pressed Cancel all. | **Primary**, slices 01, 02, 05. |
| `config-admin` | **Sofia Lindqvist**. Wrote Northwind Platform's query. | **Secondary**, slice 06. |

No new persona.

## Wave: DISCUSS / [REF] JTBD One-Liners

- `job-operator-keep-lighthouse-usable-while-it-forecasts` (reused, wording unchanged): while Lighthouse recalculates
  forecasts, every page keeps answering. **US-01, US-05.**
- `job-reach-and-read-what-i-came-for-on-a-large-instance` (DIVERGE, reused): on an instance with hundreds of Features
  per Portfolio, get what I came for without waiting on data I did not ask for. **US-02, US-03, US-04, US-06** (US-06
  through outcome OS6). US-01 also serves its "while one runs" half.
- The precursor Task 01a is `job_id: infrastructure-only` with a rationale (D17).
- `jobs.yaml` is not changed: both job statements fit every story as written.

---

## Wave: DISCUSS / [REF] Locked Decisions

DISCOVER used D1–D16 and DIVERGE DV-1..DV-11; this wave continues at D17. All are taken under the maintainer's
"continue autonomously". The ones the maintainer may want to overturn are repeated under "Questions for the
maintainer".

- [D17] **Slice-composition gate: the measurement is a precursor step inside Story 01, not a slice.** DIVERGE's slice
  1 holds only `@infrastructure` work, so it cannot be released alone. It lands **first, as its own commits, inside
  the slice-01 Story** (option (b) of the gate: a precursor, not a separately shipped slice), tracked as an ADO
  **Task** under that Story. Why not merge it into a later slice instead: its baseline must exist before *any* change,
  and slice 01 is the first change. Why not leave it separate: it has no user value. `job_id: infrastructure-only`;
  rationale: dev-only tooling that proves every later slice's gain; nothing a user runs.
- [D18] **Renumbering.** DIVERGE's slices 1+2 → **01**, 3 → **02**, 4 → **03**, 5 → **04**, 6 → **05**, 7 → **06**.
  The rules carry over: 01 first; 02, 03 and 05 swap freely after the baseline; 04 follows 03; 06 last. If the
  falsifier fires (idle 920-Feature list under ~10 s), 04 moves to just before 06.
- [D19] **K3 stays an exit criterion** (DV-5, orchestrator decision), flagged again for the maintainer.
- [D20] **No existing response shape changes in any slice.** `GET /portfolios`, `GET /teams`, `GET /portfolios/{id}`,
  `GET /features`, `GET /features/ids`, `GET /features/references`, `featuresInProgress` and `FeatureDto` keep their
  bodies. Slice 04's endpoint is additive. **No Lighthouse-Clients release in this Epic** (maintainer, DIVERGE Q3).
- [D21] **One lean-list slice serves both the Portfolio and the Team Features tab.** They share the grid and the
  cost (V4); two slices would differ only by owner.
- [D22] **The Features list's looks** (slice 04): loading placeholders until the first answer; after a completed
  update the rows stay, dimmed under a small spinner, and a late answer for an older request never lands; failure:
  "The {Features} couldn't be loaded." with Retry; empty: "This {Portfolio} has no {Features} yet." (or {Team}); all
  hidden by hide-completed: "All {N} {Features} are completed. Turn off Hide completed to see them." Copy is a
  default; DISTILL's sketch settles it.
- [D23] **The nudge** (slice 06): shown when a Portfolio holds **more than 100** Features (101 and up), counted from
  what its last update stored. Two places: one log line at the end of each such Portfolio update, and a non-blocking
  note on the Portfolio's **edit page**, above the query, for whoever can edit it. It never blocks Save or an update.
  Portfolio only; no Team nudge. Copy default: "This {Portfolio} holds {N} {Features}. Above 100, its pages and
  forecasts get slower. If you don't plan with all of them, narrow the query." The threshold is one constant shared
  by the note and the log line.
- [D24] **Usage data**: a closed size-bucket property on the existing `PortfolioTabOpened` and `TeamTabOpened` events
  (DEVOPS fixes the buckets, e.g. `Under100` / `100To399` / `400Plus`; never a raw count, never which Portfolio or
  Team). Emitted in **slice 01**, the slice that first makes the Epic usable, and listed in
  `docs/settings/usagedata.md` there.
- [D25] **Production-data checks, since timings come only from synthetic data.** Every slice: the dev DB backup (S13,
  real history) gives identical screens, dependency verdicts, positions and forecast outputs before and after.
  Slice 01: the reporter's first log after the release is the K3 production reading. All slices: the size bucket
  (D24) shows how many real instances are this large.
- [D26] **Evidence per slice**: the before/after readings of the harness (median of 5 after one warm-up, scaled set
  at 100 / 400 / 920, 4 cores, SQLite and PostgreSQL, idle and during a round) are recorded in this delta. They are
  not CI budgets (maintainer, DIVERGE Q6).
- [D27] **Re-score gate after the precursor.** G2 is re-scored on measured shares, Q_r and S_q are recorded, Dissent
  A/B (DV-6) are checked, and the order of 02, 03, 05 is fixed then. The result is written here before slice 01's
  isolation step starts.
- [D28] **Epic 6264 rules apply to every slice** (DV-9): only new database-side reader methods for the hot callers;
  no new `GetAllByPredicate` outside the adapters; no new controller dependency on a repository; S05a entries only
  shrink; S13's Feature → Forecast navigation stays.
- [D29] **Fictional names** for personas and for the scaled data set (*Northwind Platform* 920, *Northwind Payments*
  400, *Northwind Mobile* 100, Teams *Atlas*, *Borealis*, *Cobalt*, *Delta Ops*, *Ember*, *Fjord*, *Granite*,
  *Harbour*).
- [D30] **Licence counts keep today's meaning.** Whatever slice 02 shares, a gated action is allowed or refused
  exactly as today, with RBAC on and off. A count that failed to load never reads as "under the limit".
- [D31] **Scope: one Epic, by decision.** Two oversize signals fire (four modules touched; outcomes that could ship
  apart); see Scope Assessment.

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS by decision — 6 Stories plus 1 precursor Task, one code base (backend + frontend), ~10½ days.**

Signals: under 10 Stories (pass); **4 modules touched** — forecasting, the Portfolio/Team catalogue reads, the
Features list read, the licence UI (fires); walking skeleton needs no new integration point (pass); ~2 weeks (at the
edge); **outcomes that could ship apart** — the stalls, the lists, the nudge (fires).

Kept as one Epic because every slice already ships alone (CLAUDE.md), so a split would relabel slices without
changing what ships when; the four K's are one measured set from one harness; and DIVERGE and the maintainer framed
one Epic. The slices are ordered, not interleaved; the only dependency is 04 on a finished 03.

---

## Wave: DISCUSS / [REF] Journey

SSOT: `docs/product/journeys/epic-6400-fast-with-many-features.yaml` (seeded this wave). It links the 6311 journey's
error path "Pages still hang with the bounded simulation", which the 2026-10-20 log observed.

```
Reader:   [Open Lighthouse] ─> [Open Northwind Platform] ─> [Features tab] ─> [Read dates, dependencies]
          Resigned              Hopeful                      Focused            Confident
          nav < 1 s (K1)        one Portfolio read (02)      first row < 2 s (K2), same verdicts (03)
                                                             fails? "couldn't be loaded" + Retry; empty says so (04)

Operator: [Update runs, round starts] ─> [Opens System Settings] ─> [Round finishes on its own] ─> [Refresh History]
          Wary                           Reassured                  Relieved                        In control
          Task Manager: Forecasting      Settings < 1 s (K1, K3)    0 heartbeat warnings (K3)       round duration (K4, 05)

Admin:    [Opens Northwind Platform's edit page] ─> [Reads the size note] ─> [Narrows the query, or not]
          Unaware                                   Informed (not blocked)    Deliberate
```

Emotional arc: **resigned → confident** (reader), **wary → in control** (operator), **unaware → deliberate**
(admin). Shared artifacts and their single sources are in the YAML: `features_count`, `size_threshold`,
`dependency_verdicts`, `feature_positions`, `in_progress_ids`, `forecast_outputs`, `forecast_input_fingerprint`,
`terms`. Integration checkpoint: every screen shows the same dependency verdicts, positions and forecast dates as
before its slice, on the dev backup and the scaled set.

---

## Wave: DISCUSS / [REF] Story Map & Slices

**Backbone**: *open Lighthouse* → *reach the screen I came for* → *open a large Portfolio or Team* → *read its
Features* → *keep working while it forecasts* → *see the round end* → *learn what a Portfolio's size costs*.

| Open Lighthouse | Reach Settings | Open a Portfolio / Team | Read Features | Work during a round | Round ends | Size cost |
|---|---|---|---|---|---|---|
| (exists) | **02** lists read names and counts | **02** one Portfolio read | **03** light verdicts and in-progress · **04** lean list, honest looks | **01** round off the request pool | **05** lighter gather | **06** nudge |

| # | ADO Story title (user-facing) | US | job_id | Moves | UI sketch at DISTILL | Changes a fetch (DESIGN looks) | Est. | Ships alone | Learning hypothesis — disproves … if it fails |
|---|---|---|---|---|---|---|---|---|---|
| 01 | **Pages keep answering while Lighthouse forecasts** (Task 01a: *Measure with a scaled data set, dev-only*) | US-01 | `job-operator-keep-lighthouse-usable-while-it-forecasts` | **K3**; K1, K2 during a round | No | N/A — no screen's fetch or response changes; the size bucket rides an existing event | ~3d (01a ~1.5d + ~1.5d) | Yes | "The round's own threads starve the requests" — if during-a-round readings stay above 1.2 × idle, or heartbeat warnings remain |
| 02 | **The Overview and Settings open without waiting on every Feature** | US-02 | `job-reach-and-read-what-i-came-for-on-a-large-instance` | K1 idle (Settings half only if A12 holds) | Only if DESIGN gives the licence count a new failure look | **Yes** — the licence count becomes one shared request | ~1.5d | Yes | "Scaffold reads hold the Overview and Settings back" — if the Overview → Settings time moves less than 30% |
| 03 | **Large Features lists open sooner, with the same dependencies** | US-03 | `job-reach-and-read-what-i-came-for-on-a-large-instance` | K2; K3 in part | No | N/A — same requests and responses; only the server's work behind them | ~1.5d | Yes | "Instance-wide reads dominate the list" (A11) — if the first row moves less than 40% |
| 04 | **The Features list opens fast and says when it cannot load** | US-04 | `job-reach-and-read-what-i-came-for-on-a-large-instance` | **K2** | **Yes** (loading, failure, empty, all-hidden) | **Yes** — new endpoint per Portfolio / Team | ~2d | Yes (after 03) | "A lean payload brings the first row under 2 s" — if K2 is still missed at 920 idle |
| 05 | **Forecast rounds finish sooner** | US-05 | `job-operator-keep-lighthouse-usable-while-it-forecasts` | **K4**; K3 in part | No | N/A — no screen's fetch changes | ~1.5d | Yes | "The gather dominates a skipped round" (A14) — if a skipped round still takes over 15% of a computed one |
| 06 | **Lighthouse says when a Portfolio grows past 100 Features** | US-06 | `job-reach-and-read-what-i-came-for-on-a-large-instance` | OS6 (a nudge) | **Yes** (the note) | **Yes**, if the count needs a read of its own (DESIGN) | ~1d | Yes | "A note at 100 leads some admins to narrow" (A10) — read later from the size bucket; not testable inside the Epic |

Briefs: `slices/slice-NN-*.md`. Release points: every slice leaves `main` releasable and is released on its own.

### Priority Rationale

1. **01** (value 5, urgency 5, effort 3): the only symptom observed on the current release (V1a), K3 is an exit
   criterion, and its precursor is the baseline every other slice needs. Riskiest assumption first (Q_r).
2. **02, 03, 05** in this provisional order, re-fixed by D27: 02 is cheapest and lifts every page; 03 removes the
   instance-wide reads and unblocks 04; 05 shortens what 01 isolated.
3. **04** after 03 (consumes the light verdicts and in-progress ids).
4. **06** last (maintainer).

### Slice taste tests

| Test | Verdict |
|---|---|
| 4+ new components in one slice | Borderline in 01: generator, harness, worker, event property. The first two are dev-only tooling in the precursor Task; the shipped change is the worker and one property. Borderline in 04: an endpoint, a hook, and four looks of one grid. Kept. |
| A new abstraction ships first | Pass. The harness is the shared abstraction and lands first (01a). Each slice adds its own reader methods (D28). |
| A slice disproves a pre-commitment | Pass; see the hypothesis column. 06 is the exception: A10 is read after release from usage data. |
| Synthetic data only | **Fails for timings, by documented reason**: the reporter shared no data or HAR (Q6), the dev DB holds 102 Features, and the maintainer chose a dev-only generator (Q10). Required instead (D25): dev-backup regression on every slice, the reporter's next log for K3, the size bucket for the population. |
| Two slices alike except scale | Pass. 03 changes server work behind the same responses; 04 changes the payload and the looks. |
| ≤ 1 day per slice | **Not met**, as in 6298 and 6303 (½–1½ d). 01 is ~3 d because the gate puts the precursor inside it; 04 is ~2 d. Both stay within 1–3 days. |

### Slice-composition gate

Resolved by D17. Slice 1 of DIVERGE held only `@infrastructure` work; it is now Task 01a, the first commits of Story 01.
Every slice listed above carries one user-visible story with an Elevator Pitch.

---

## Wave: DISCUSS / [REF] User Stories

<!-- markdownlint-disable MD024 -->

System constraints (all stories): RBAC readability unchanged (C1): unreadable Portfolios never named, unreadable
blockers withheld · dependency verdicts over the whole graph (C2) · positions global (C3) · UI gating only through
`useRbac()` (C4) · loading and failure looks for every changed fetch (C5) · existing response shapes unchanged (C6,
D20) · expand-only schema (C7) · copy in the configured terms, seeded defaults {Feature}, {Work Item}, {Team},
{Portfolio} (C8) · no customer names in git (C9) · forecast outputs unchanged (C12) · Epic 6264 rules (D28).

**Measured** below means: the harness of Task 01a, scaled set (D29) at 920 Features, 8 Teams, ~3,000 Features in the
instance, 4 cores, SQLite and PostgreSQL, median of 5 after one warm-up, idle and during a round (DISCOVER
Measurement plan).

### US-01 — Pages keep answering while Lighthouse forecasts (slice 01)

`job_id: job-operator-keep-lighthouse-usable-while-it-forecasts` · persona `platform-operator` (Jonas Weber);
`delivery-lead-rte` (Martin) for the list during a round

**Problem**: Jonas runs Lighthouse on a 4-core VM with hourly Team and Portfolio updates. Each update starts a forecast
round of 4–8 minutes over every Feature of the 8 Teams involved. On v26.10.19.10, which already caps the simulation at
half the cores, the log shows nine Kestrel heartbeat warnings of 1.4–49.3 s, all during rounds. Pages wait meanwhile;
Jonas pressed Cancel all to get his instance back.

#### Elevator Pitch

Before: while a forecast round runs, a page can wait tens of seconds and the log fills with heartbeat warnings, so
Jonas cancels the round.
After: with Task Manager showing the Forecasting row for Northwind Platform, Jonas clicks **System Settings** in the
header → sees the Settings page in **under 1 s** (measured), and the log of the whole round carries **no** "Heartbeat
took longer than" warning; the Forecasting row leaves Task Manager when the round ends on its own.
Decision enabled: let the round run instead of pressing Cancel all or restarting the container.

**Precursor Task 01a** — `@infrastructure` · `job_id: infrastructure-only` · `infrastructure_rationale`: dev-only
tooling (scaled data generator and before/after harness) that records the baseline every slice is judged against; no
user runs it and nothing of it ships. It lands first, as its own commits inside this Story (D17).

**Examples**:
1. 10:00, Northwind Platform's hourly update starts a computed round over 8 Teams. At 10:02 Jonas opens System
   Settings: it shows in 0.6 s; the round's log has no heartbeat warning.
2. 10:03, same round. Martin opens Northwind Platform → Features: the first row comes no later than 1.2 × his idle
   reading.
3. 11:00, nothing changed; Story 6354 skips the simulation. The gather still runs, off the request pool; no warning.
4. A 2-core container: the round runs with one simulation worker; Settings still opens within 1 s.
5. 12:00, Jonas presses Cancel all mid-round: the round stops within seconds and yesterday's forecasts stay (6311
   behaviour unchanged).
6. On the dev backup, Feature dates and percentiles read exactly as before the change.

```gherkin
Scenario: Settings opens at once while a forecast round runs
  Given a computed forecast round over Northwind Platform's 8 Teams is running
  When Jonas clicks System Settings in the header
  Then the Settings page is shown in under 1 second
  And the round's log carries no heartbeat warning

Scenario: A large Features list opens as fast during a round as without one
  Given Martin's idle first-row time for Northwind Platform has been measured
  And a computed forecast round over its 8 Teams is running
  When Martin opens Northwind Platform's Features tab
  Then the first row appears within 1.2 times the idle time

Scenario: A round that changes nothing stays out of the way too
  Given nothing has changed since Northwind Platform's last forecast
  When the 11:00 update runs and its round is skipped
  Then Settings opens in under 1 second while the round's gather runs
  And the round's log carries no heartbeat warning

Scenario: Cancel all still stops a round and keeps yesterday's forecasts
  Given a computed round over Northwind Platform's 8 Teams is running
  When Jonas presses Cancel all in Task Manager
  Then the Forecasting row leaves within seconds
  And every Feature keeps the forecast it had before the round

@property
Scenario: Forecasts read the same wherever the round runs
  Given the dev backup and the same random seed
  Then every Feature's percentiles and histogram rows equal those computed before the change
  And Story 6354's input fingerprint is unchanged
```

**AC**:
- AC-1.0 (Task 01a, precursor) The dev-only generator builds the scaled set (D29) through the CSV connector path,
  with child Work Items, dependency links and forecasts, at 100 / 400 / 920 Features; it is not reachable in a release
  build. The calibration ratio holds (a computed round lasts minutes and is several times the idle list time) before
  any baseline counts. The harness records the Measurement plan's readings 1–7, Q_r and S_q, and the falsifier, on
  SQLite and PostgreSQL, on `main` and on the v26.10.19.10 tag. The baseline table and the D27 re-score are written
  into this delta before AC-1.1's change starts.
- AC-1.1 During a computed and a skipped round, Overview → System Settings shows Settings in under 1 s (measured).
- AC-1.2 During a round, the first Feature row of Northwind Platform and of Team Borealis comes within 1.2 × its
  idle reading (measured).
- AC-1.3 No heartbeat warning in the log across 5 computed and 5 skipped rounds at 920 Features, 8 Teams.
- AC-1.4 Cancel all and per-row Stop still stop the round within seconds and keep the previous forecasts.
- AC-1.5 Forecast outputs and Story 6354's fingerprint are identical before and after, on the dev backup and the
  scaled set, on SQLite and PostgreSQL (C12).
- AC-1.6 `PortfolioTabOpened` and `TeamTabOpened` carry the closed size bucket (D24); never a count or an id; listed
  in `docs/settings/usagedata.md`.
- AC-1.7 Production reading (D25): the reporter's first log after the release is checked for heartbeat warnings
  during forecast rounds; the result is recorded here (not a release gate).

**KPI**: K3, K1 and K2 during a round. **Tech**: where the round runs is DESIGN's (DV-10); no async conversion of
the request paths (Dissent B decides that later); Epic 6311's half-cores bound and Cancel-all token stay. Premium
licence fixture in the worktree if the scaled set uses dependency-aware forecasting (P2). **Looks**: N/A, because no
screen's fetch or response changes.

### US-02 — The Overview and Settings open without waiting on every Feature (slice 02)

`job_id: job-reach-and-read-what-i-came-for-on-a-large-instance` · persona `platform-operator` (Jonas),
`delivery-lead-rte` (Martin)

**Problem**: Jonas opens Lighthouse only to change a setting and gets a loading wheel on the Overview. To show names
and counts, the Overview reads every Portfolio with every Feature and its forecast histograms, six times; each
Portfolio detail page reads all Portfolios twice to show one.

#### Elevator Pitch

Before: the Overview and every detail page read every Feature of the instance to show names and counts.
After: Jonas opens Lighthouse → the Overview lists Portfolios and Teams in **under 1 s** idle (measured) with the same
names and counts as before → clicks **System Settings** → sees Settings in **under 1 s**; `GET /api/latest/portfolios`
returns the same body as before.
Decision enabled: go straight to the setting he came for, instead of waiting the Overview out.

**Examples**:
1. Idle, Jonas opens the Overview at 09:00: 6 Portfolios and 8 Teams listed in 0.7 s; Settings 0.4 s later.
2. Martin opens Northwind Payments (400 Features): `GET /portfolios/{id}` takes the same time with the other five
   Portfolios present as without them (± 15%).
3. Community licence at its Team limit: "Add Team" is disabled with today's reason; the page asked for the count once.
4. The count cannot be loaded: "Add Team" is not offered as allowed (DESIGN picks the look, D30).
5. RBAC on, Aisha is a Viewer on Team Borealis only: the Overview names only what she can read, as today.

```gherkin
Scenario: The Overview lists Portfolios and Teams at once
  Given the instance holds 6 Portfolios with about 3,000 Features
  When Jonas opens the Overview with no forecast running
  Then the Portfolio and Team lists appear in under 1 second
  And they show the same names and counts as before

Scenario: Reading one Portfolio does not read the others
  Given Northwind Payments holds 400 Features
  When Martin opens Northwind Payments with and without the other five Portfolios present
  Then the Portfolio's own read takes the same time within 15 percent

Scenario: A licence limit is enforced exactly as before
  Given the Community licence allows no more Teams
  When Jonas opens the Teams overview
  Then "Add Team" is disabled with today's reason

Scenario: A count that could not be loaded never reads as room to spare
  Given the licence count cannot be loaded
  When Jonas opens the Teams overview
  Then "Add Team" is not offered as allowed

Scenario: Readers see only what they may read
  Given RBAC is on and Aisha may read only Team Borealis
  When Aisha opens the Overview
  Then only Team Borealis and the Portfolios she may read are named
```

**AC**: AC-2.1 Overview lists < 1 s idle at 920 (measured); same names and counts. AC-2.2 Overview → Settings < 1 s
idle (measured); credited to K1's Settings half only if 01a showed the server held Settings back (A12). AC-2.3 One
Portfolio's read scales with that Portfolio, not the instance (± 15%). AC-2.4 Licence decisions unchanged, RBAC on and
off (D30); the count is requested once per page, not per mount. AC-2.5 A failed count never enables a gated action.
AC-2.6 Bodies of `/portfolios`, `/teams`, `/portfolios/{id}` unchanged (D20). AC-2.7 Dev backup: every page identical.
**KPI**: K1. **Tech**: reader methods per D28; S05d stays 6264's. **Looks**: the Overview keeps its body spinner while
its lists are on their way; DESIGN states the licence count's loading and failure looks (recommendation §3).

### US-03 — Large Features lists open sooner, with the same dependencies (slice 03)

`job_id: job-reach-and-read-what-i-came-for-on-a-large-instance` · persona `delivery-lead-rte` (Martin),
`product-owner` (Aisha)

**Problem**: Martin opens Northwind Platform's Features tab before a steering meeting and waits about 50 s. To answer
"what does each Feature wait on" and "which Teams are working on it", the server reads every Feature of the instance
with all its forecasts, twice, and once more per in-progress Feature.

#### Elevator Pitch

Before: a 920-Feature list reads the whole instance's Features with their forecasts for its dependency and
in-progress columns.
After: Martin opens Northwind Platform → **Features** → sees the first row **at least 40% sooner** than the 01a
baseline (measured, idle), with the same "Waits on" verdicts, the same withheld blockers and the same in-progress
Teams; `GET /api/latest/features/ids` and `/features/references` return the same bodies.
Decision enabled: read dates and dependencies in the meeting instead of opening the tracker.

**Examples**:
1. Northwind Platform, idle: first row 40% sooner; Feature "Checkout redesign" still waits on "Payment provider
   switch" in Northwind Payments.
2. With the instance at ~1,000 instead of ~3,000 Features, the list's server time differs by ≤ 15%.
3. RBAC on, Aisha may not read Northwind Payments: the blocker reads "withheld", as today (C1).
4. Team Borealis's update runs: its in-progress refresh no longer reads the whole table per in-progress Feature.
5. Dev backup: every dependency verdict and position identical.

```gherkin
Scenario: A large list opens sooner and shows the same dependencies
  Given Northwind Platform holds 920 Features and its baseline first-row time is recorded
  When Martin opens its Features tab with no forecast running
  Then the first row appears at least 40 percent sooner
  And "Checkout redesign" still waits on "Payment provider switch"

Scenario: The list's cost follows the Portfolio, not the instance
  Given the instance holds about 1,000 Features in one reading and about 3,000 in another
  When Martin opens Northwind Platform's Features tab in each
  Then the server time differs by at most 15 percent

Scenario: An unreadable blocker stays withheld
  Given RBAC is on and Aisha may not read Northwind Payments
  When Aisha opens Team Borealis's Features tab
  Then the blocker of "Checkout redesign" reads as withheld

Scenario: In-progress Teams are the same as before
  Given Team Atlas and Team Cobalt work on "Checkout redesign"
  When Martin opens Northwind Platform's Features tab
  Then both Teams are shown as working on it
```

**AC**: AC-3.1 First row ≥ 40% sooner, idle, 920 (measured). AC-3.2 Server time scale-invariant to instance size
(≤ 15%). AC-3.3 Verdicts over the whole graph, withheld and unnamed rules unchanged (C1, C2). AC-3.4 In-progress
answers unchanged, on the list and in a Team update. AC-3.5 Response bodies unchanged (D20). AC-3.6 Dev backup
identical. **KPI**: K2, K3 in part. **Tech**: projection mechanism is DESIGN's (DV-10); `FeaturesController` and
`TeamMetricsService` S05a entries only shrink. **Looks**: N/A, because the screen's requests and responses are
unchanged.

### US-04 — The Features list opens fast and says when it cannot load (slice 04)

`job_id: job-reach-and-read-what-i-came-for-on-a-large-instance` · persona `delivery-lead-rte` (Martin),
`product-owner` (Aisha)

**Problem**: even with lighter server work, the list asks for every Feature's full forecast payload, ids travel in
the URL, and parents arrive in a second round. When the request fails or the Portfolio is empty, the list shows
loading placeholders forever, so Martin cannot tell "slow" from "broken" from "nothing there".

#### Elevator Pitch

Before: the list's payload grows with every Team of every Feature, and failure or emptiness looks like loading.
After: Martin opens Northwind Platform → **Features** → sees the first row in **under 2 s** at 920 Features on 4 cores,
idle and during a round (measured); if it cannot load he sees "The Features couldn't be loaded." with **Retry**; an
empty Portfolio says "This Portfolio has no Features yet."
Decision enabled: trust that the list is complete, or retry, instead of waiting on a spinner.

**Examples**:
1. Northwind Platform, idle: first row 1.4 s; the same dates, progress, dependencies and parent as today.
2. Team Borealis (310 Features across 3 Portfolios): first row 0.9 s.
3. The server is restarting: "The Features couldn't be loaded." with Retry; Retry after it is back shows the rows.
4. Northwind Mobile was just created and not yet updated: "This Portfolio has no Features yet."
5. Northwind Mobile's 100 Features are all completed and Hide completed is on: "All 100 Features are completed. Turn
   off Hide completed to see them."
6. A Features update completes while Martin reads: the rows stay, dimmed under a small spinner, then refresh; an
   older answer never replaces a newer one.
7. The instance calls Features "Themes" in its terms: every message uses "Themes".

```gherkin
Scenario: A 920-Feature list shows its first row within two seconds
  Given Northwind Platform holds 920 Features across 8 Teams on a 4-core host
  When Martin opens its Features tab, with and without a forecast round running
  Then the first row appears in under 2 seconds
  And dates, progress, dependencies and parent read as before

Scenario: A Team's list is just as quick
  Given Team Borealis has 310 Features across 3 Portfolios
  When Aisha opens Team Borealis's Features tab
  Then the first row appears in under 2 seconds

Scenario: A list that cannot be loaded says so and can be retried
  Given the Features of Northwind Platform cannot be loaded
  When Martin opens its Features tab
  Then he sees "The Features couldn't be loaded." with Retry
  And once the server answers again, Retry shows the rows

Scenario: An empty Portfolio says it is empty
  Given Northwind Mobile has no Features yet
  When Martin opens its Features tab
  Then he sees "This Portfolio has no Features yet."

Scenario: A list whose Features are all completed says why it looks empty
  Given all 100 Features of Northwind Mobile are completed and Hide completed is on
  When Martin opens its Features tab
  Then he sees "All 100 Features are completed. Turn off Hide completed to see them."

Scenario: A list refreshed by an update never shows an older answer
  Given Martin reads Northwind Platform's Features
  When a Features update completes for Northwind Platform
  Then the rows stay visible, dimmed, until the new answer arrives
  And an answer to an earlier request never replaces it

Scenario: Messages use the instance's terms
  Given the instance calls a Feature a "Theme"
  When Martin opens an empty Portfolio's tab
  Then the message reads "This Portfolio has no Themes yet."
```

**AC**: AC-4.1 First row < 2 s at 920, Portfolio and Team tab, idle and during a round (measured; K2). AC-4.2 Columns
and values identical to today's list (dev backup and scaled set). AC-4.3 Failure look with Retry; empty look;
all-hidden look; copy per D22, settled at DISTILL. AC-4.4 Refresh after a completed update keeps rows dimmed; a late
answer never lands (keyed query). AC-4.5 The new endpoint is additive, guarded by Portfolio / Team read as today;
`GET /features` and `FeatureDto` unchanged (D20). AC-4.6 Hide-completed stays client-side (maintainer). AC-4.7 The
E2E page object waits for the list's ready state, not mere visibility. **KPI**: K2. **Tech**: endpoint shape is
DESIGN's; reuse `WidgetShell` status and a keyed TanStack query (S10). **Looks**: DESIGN pins loading, failure, empty
and all-hidden; **sketched at the start of DISTILL**.

### US-05 — Forecast rounds finish sooner (slice 05)

`job_id: job-operator-keep-lighthouse-usable-while-it-forecasts` · persona `platform-operator` (Jonas)

**Problem**: a round that Story 6354 skips still reads every Feature of the 8 Teams with all their forecasts before
it decides to skip, and a computed round reads and rewrites far more than the simulation uses. Rounds of 264–483 s,
several an hour, keep the instance busy most of the hour.

#### Elevator Pitch

Before: a skipped round costs nearly as much reading as a computed one, and every round reads what it does not use.
After: Jonas opens **Settings → System Info → Refresh History** → sees Northwind Platform's skipped Forecast rows take
**at most 15%** of the baseline computed round and computed rows **at least 30% shorter** than the baseline (measured),
while every Feature's dates are unchanged.
Decision enabled: keep hourly updates instead of spacing them out to protect the instance.

**Examples**:
1. 11:00, nothing changed: the skipped round's Forecast row reads 28 s against a 240 s baseline round.
2. 12:00, Team Atlas closed 3 Work Items: the computed round reads 160 s against 240 s.
3. SQLite and PostgreSQL: identical percentiles and histogram rows before and after, same seed.
4. Dev backup: the same rounds skip and compute as before (fingerprint unchanged).

```gherkin
Scenario: A round that changes nothing costs little
  Given nothing has changed since Northwind Platform's last forecast
  When the 11:00 update's round is skipped
  Then its Forecast row in Refresh History takes at most 15 percent of the baseline computed round

Scenario: A computed round is shorter
  Given Team Atlas closed 3 Work Items since the last forecast
  When the 12:00 round computes Northwind Platform's forecasts
  Then the round takes at least 30 percent less time than the baseline

@property
Scenario: Forecasts and skip decisions are unchanged
  Given the same data and seed on SQLite and on PostgreSQL
  Then every percentile, histogram row and input fingerprint equals its value before the change
```

**AC**: AC-5.1 Skipped round ≤ 15% of baseline computed round (measured). AC-5.2 Computed round ≥ 30% shorter.
AC-5.3 Outputs and fingerprint identical on both providers (C12). AC-5.4 Dev backup: identical skip / compute
decisions. AC-5.5 S13's Feature → Forecast navigation and cascade stay (D28). **KPI**: K4, K3 in part. **Tech**: how
the save replaces histograms is DESIGN's. **Looks**: N/A, because no screen's fetch changes.

### US-06 — Lighthouse says when a Portfolio grows past 100 Features (slice 06)

`job_id: job-reach-and-read-what-i-came-for-on-a-large-instance` (outcome OS6) · persona `config-admin` (Sofia)

**Problem**: Sofia's query for Northwind Platform brings in 920 Features, most of which nobody plans with. Nothing
tells her that every page and forecast for that Portfolio pays for each one.

#### Elevator Pitch

Before: a Portfolio can grow to 920 Features without anyone being told what it costs.
After: Sofia opens Northwind Platform → **Edit** → sees above the query "This Portfolio holds 920 Features. Above 100,
its pages and forecasts get slower. If you don't plan with all of them, narrow the query." — and Save works as
before; after each update the log carries one line with the count.
Decision enabled: whether to narrow the query, knowingly.

**Examples**:
1. Northwind Platform, 920 Features: the note shows; Sofia saves unchanged; the update runs.
2. Northwind Mobile, exactly 100 Features: no note, no log line.
3. A new Portfolio never updated: no note (count unknown).
4. Jonas reads the log after the 10:00 update: one line, Northwind Platform holds 920 Features, over 100.
5. Terms: an instance that calls Features "Themes" reads "This Portfolio holds 920 Themes. …".

```gherkin
Scenario: An admin learns a Portfolio's size cost where the query is written
  Given Northwind Platform's last update stored 920 Features
  When Sofia opens its edit page
  Then she sees the note naming 920 Features and the threshold of 100
  And Save works as before

Scenario: No note at the threshold
  Given Northwind Mobile's last update stored exactly 100 Features
  When Sofia opens its edit page
  Then no size note is shown

Scenario: No note before the first update
  Given a Portfolio that has never been updated
  When Sofia opens its edit page
  Then no size note is shown

Scenario: The log names an oversized Portfolio once per update
  Given Northwind Platform holds 920 Features
  When its update completes
  Then the log carries one line with the count and the threshold
```

**AC**: AC-6.1 Note when > 100 stored Features; none at ≤ 100 or unknown. AC-6.2 Never blocks Save or an update.
AC-6.3 One log line per update of such a Portfolio. AC-6.4 One threshold constant for note and log. AC-6.5 Copy in
the configured terms, settled at DISTILL. AC-6.6 Shown to whoever can open the edit page; no new guard.
**KPI**: OS6 (read from the size bucket after release, A10). **Looks**: no note while the count is unknown or failed
to load — a missing nudge cannot be misread as data; **sketched at the start of DISTILL**.

---

## Wave: DISCUSS / [REF] Outcome KPIs

Objective: on an instance with hundreds of Features per Portfolio, people reach and read what they came for at once,
also while it forecasts. **North star: K3** (exit criterion, D19). Self-hosted, so every K is measured on the scaled
set per the protocol (D26) plus the production checks of D25. Targets are provisional and revised after the 01a
baseline (D16, DIVERGE Q5).

| # | Who | Does what | Target | Baseline | Measured by | Type |
|---|---|---|---|---|---|---|
| K1 | Operators and readers on a large instance | reach navigation and Settings | first navigation paint < 1 s; Overview → Settings usable < 1 s; idle and during a round | unknown until 01a; reporter: "a loading wheel" | harness, Playwright marks, 920 scale, 4 cores | Leading |
| K2 | Delivery leads and POs | read the first Feature row of a large list | < 2 s at 920 Features, Portfolio and Team tab, idle and during a round | ≈ 50 s (one second-hand reading); 01a records the scaled value | harness, first-row mark | Leading |
| K3 | Every user during a forecast round | keep getting answers | **0** heartbeat warnings across 5 computed + 5 skipped rounds at 920 / 8 Teams; K1, K2 hold; during-round reading ≤ 1.2 × idle | 9 warnings, 1.4–49.3 s, in 4 rounds (2026-10-20, v26.10.19.10) | harness (log scan, `dotnet-counters`); production: reporter's first log after release | Leading (north star) |
| K4 | The instance, per round at 8 Teams | spend less time per round | skipped ≤ 15% of the baseline computed round; computed ≥ 30% shorter | 264–483 s per round (reporter); 01a records the scaled value | harness round split; Refresh History durations | Leading (lever for K3) |
| K5 | Opted-in instances | open large Portfolios and Teams | descriptive, no target: share of tab openings per size bucket | none | size bucket on the tab-opened events (D24) | Descriptive |

Guardrails: **G1** forecast outputs and Story 6354 fingerprints identical (C12); **G2** dependency verdicts, positions
and RBAC readability identical (C1–C3); **G3** existing response bodies unchanged (D20); **G4** dev-backup screens
identical; **G5** a Team or Portfolio update takes no longer than its baseline.

Hypothesis: we believe that running the forecast round off the request pool and scoping every read to what the screen
shows will let users of large instances reach and read what they came for at once. We will know when K3 shows no
heartbeat warning through a round and K1–K2 hold on the scaled set, and the reporter's next log carries none.

---

## Wave: DISCUSS / [REF] Definition of Done

1. Every UAT scenario of the story passes as an automated acceptance test, except the timing ACs, which are evidence
   readings from the harness recorded in this delta (D26); their scenarios are checked by the harness, not CI.
2. Unit and component tests green; frontend `pnpm test`; backend `dotnet test` with the CLAUDE.md filter.
3. `pnpm build` and `dotnet build` with zero warnings; Biome clean; no new SonarCloud issues; `docs/ci-learnings.md`
   pre-applied.
4. Guardrails G1–G4 shown on the dev backup and the scaled set, on SQLite and PostgreSQL.
5. RBAC readability unchanged; UI gating only via `useRbac()`.
6. Copy through Terminology; no "Epic", "Story", "Initiative" for product concepts.
7. E2E: no new walking skeleton; existing Features-tab E2E stays green; from slice 04 its page object waits for the
   list's ready state.
8. Mutation testing ≥ 80% on the slice's code, run last on frozen code (Stryker, both stacks where touched).
9. At feature finalization: docs, usage-data entry, release notes per the Checklist; no clients release (D20).

---

## Wave: DISCUSS / [REF] Out of Scope

Paging and server-side sort or filter (D15, a later Epic) · async conversion of the request paths (Dissent B, a
follow-up Epic) · forecasting less often, coalescing rounds (follow-up) · client cache and stale-while-revalidate
beyond slice 02's shared count (follow-up) · faster tracker reads, O10 (separate Epic) · read models (wait for Epic
6264) · harness readings as CI budgets (maintainer) · a nudge on Teams, or a nudge that blocks · moving the clients off
`FeatureDto` · shipping the scaled-data generator or a demo scenario from it (maintainer Q10) · changing Epic 6264's
ports or files.

---

## Wave: DISCUSS / [REF] WS Strategy, Driving Ports, Pre-requisites

**Walking skeleton**: brownfield, none new. Every journey step exists and works, only slowly. Slice 01 is the first
end-to-end proof: the scaled set through the real HTTP surfaces and the real update queue, read by the harness.

**Driving ports**: header links (Overview, System Settings); `GET /portfolios`, `GET /teams`, `GET /portfolios/{id}`
and settings; the Portfolio and Team Features tabs with `features/ids`, `features/references`, `featuresInProgress`
(unchanged) and slice 04's new list endpoint; the update queue's Team and Portfolio updates (scheduled and buttons);
Task Manager (Stop, Cancel all); Settings → System Info → Refresh History; the Portfolio edit page; the server log.

**DESIGN must answer**: where the round runs (01); how the licence count is shared and its loading and failure looks
(02); the projection and in-progress query (03); the lean endpoint's shape and the four looks (04); the lighter gather
and save (05); the nudge's data source (06); new reader names fitted to 6264's S01 glossary.

**DISTILL must sketch with the maintainer first**: slice 04's loading, failure, empty and all-hidden looks; slice 06's
note; slice 02's count look only if DESIGN makes one.

**Pre-requisites**: P1 Task 01a before any other DELIVER step · P2 Premium licence fixture in the worktree · P4 the dev
DB backup restored for regression (`Restore-DbBackup.ps1`) · P5 a PostgreSQL instance for the provider readings · P6
Story 6354 on `main` (it is).

---

## Wave: DISCUSS / [REF] Project DISCUSS Checklist (no silent N/A)

| Item | Answer |
|---|---|
| **RBAC impact** | **No new permission, no new guard.** Slice 04's endpoint uses Portfolio / Team read, as the Features tabs do today; unreadable Portfolios never named, blockers withheld (C1). Slice 02's shared count keeps today's licence decisions with RBAC on and off (D30). Slice 06's note shows on the edit page to whoever can open it. UI gating via `useRbac()` only. `docs/settings/rbac.md`: **N/A, because** no permission changes. |
| **Lighthouse-Clients CLI/MCP** | **N/A, because** every existing response shape stays (D20) and the clients stay on `FeatureDto` and `GET /features` (maintainer, DIVERGE Q3). Slice 04's endpoint is additive and the clients do not adopt it in this Epic. **No changeset, no version bump.** |
| **Website / marketing surface** | **N/A, because** no new capability or screen the website shows. The release notes carry the speed-up. Whether to claim "hundreds of Features per Portfolio" on the website once K1–K3 are met is a question for the maintainer (Q7). |
| **Docs + screenshots** | **Owed at finalize, per feature**: `docs/portfolios/detail.md` and the Team docs (the list's failure, empty and all-hidden looks); `docs/portfolios/edit.md` (the size note and threshold); `docs/settings/usagedata.md` (size bucket, in slice 01); a short note on forecast rounds in the Task Manager / Refresh History docs if DESIGN changes what they show. **Screenshots**: none changed by default (the loaded list looks the same; demo Portfolios are under 100, so no note in demo screenshots); the note is described in text. Docs wait for the maintainer's confirmation. |
| **Demo data** | **No change.** The generator is dev-only (Q10). Demo Portfolios stay under 100 Features, so the note does not show in demo; slice 06 is covered below E2E. |
| **Terminology** | **No new key.** Messages use {Feature(s)}, {Portfolio}, {Team} (D22, D23). "Hide completed" keeps today's label. |
| **Usage-data event** | **Wanted, designed in DEVOPS (D24)**: a closed size-bucket property on `PortfolioTabOpened` (1) and `TeamTabOpened` (0); no new event, no renumbering. Emitted in slice 01, listed in `docs/settings/usagedata.md` there. Never a raw count, never which Portfolio or Team. |
| **EF migrations** | **N/A unless DESIGN adds an index** for the projections; if it does, expand-only through `CreateMigration` (C7). No new column is planned (the nudge reads the stored count). |
| **Premium gating** | **None new.** Dependency verdicts keep their existing licence check. |
| **Loading / failure looks** | 02 and 04 change fetches and get looks at DESIGN; 06 if its count needs a read; 01, 03, 05 **N/A, because** no screen's fetch changes. |
| **UI sketch at DISTILL** | 04 and 06 yes; 02 only if DESIGN adds a look; 01, 03, 05 **N/A, because** nothing a user sees changes. |
| **Epic split** | One Epic (D31); each slice ships alone; the only sibling dependency is 04 on a finished 03. |
| **ADO** | Epic #6400 (Planned, no children). One Story per slice with the titles in the Story Map; Task 01a under Story 01. Creating them is `/ado-sync`'s, not this wave's. Epic state is the maintainer's. |
| **Release notes** | The maintainer's. Proposed lead: the pain (pages that wait during forecasts, a 920-Feature list that takes about 50 s) and the win (answers while Lighthouse forecasts; large lists in about 2 s). |

---

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|---|---|---|
| 1 | Problem clear, domain language | PASS — clear; evidence is one reporting instance (DISCOVER G1: pass on stalls, conditional on the list) | Each story opens with Jonas's, Martin's, Aisha's or Sofia's situation in the product's words. |
| 2 | Persona specific | PASS | Fictional but specific: 4-core Docker operator with hourly updates; Head of Delivery on a 920-Feature Portfolio; PO of a 310-Feature Team; the query's author. |
| 3 | 3+ domain examples, real data | PASS | 4–7 per story, with Portfolio and Team names, sizes, times (10:00, 11:00), Feature names and readings. Names are fictional by rule C9. |
| 4 | UAT G/W/T, 3–7 | PASS | US-01 5, US-02 5, US-03 4, US-04 7, US-05 3, US-06 4. Each has a failure or boundary path (Cancel all, failed count, RBAC withheld, failed list, exactly 100, never updated, identical outputs). |
| 5 | AC from UAT | PASS | AC-n.m per story trace to scenarios and D-decisions; timing ACs name the measured reading. |
| 6 | Right-sized | PASS, two at the edge | 01 ~3 d (the gate puts the precursor inside it; the user-visible part is ~1.5 d), 04 ~2 d; others 1–1.5 d. All within 1–3 days. |
| 7 | Technical notes | PASS | Inventory S1–S14; DESIGN questions; D28; looks per story. |
| 8 | Dependencies tracked | PASS | 01a before everything; 04 after 03; P2–P6; D27 re-score. |
| 9 | Outcome KPIs | PASS | K1–K5 with numeric targets (K5 descriptive), baselines, method; guardrails G1–G5. |

Elevator Pitch check: all six stories have Before / After (a UI action or endpoint with a measured or visible output) /
Decision enabled. The only `@infrastructure` item, Task 01a, sits inside Story 01 (D17). Every story has a `job_id`.

**DoR status: PASSED (9 of 9).**

---

## Wave: DISCUSS / [REF] Risks

| Risk | P | I | Mitigation |
|---|---|---|---|
| Q_r is low: slice 01 helps little and K3 rests on 02–05 | M | H | D27 re-score; Dissent B opens the async Epic if S_q > 0.3 |
| The scaled set does not resemble the reporter's instance | M | M | Calibration ratio before any baseline; the reporter's next log (D25) |
| The falsifier fires (idle list < ~10 s) | M | M | 04 moves before 06; the job narrows to the way to Settings |
| Provider differences (SQLite vs PostgreSQL) in projections or the save | M | M | Both providers in every reading and identity check |
| Slice 01 runs past 3 days | M | L | 01a is a Task with its own commits; the isolation step does not start before the baseline |
| The shared licence count changes a gated action under RBAC | L | H | D30; scenarios with RBAC on and off |
| Collision with Epic 6264's port Stories | L | M | D28; 6400 lands first |
| Stalls come from GC or the database, not the pool | L | H | 01a records GC pauses and DB waits; troubleshoot before opening an Epic |

---

## Wave: DISCUSS / [REF] Questions for the maintainer

Each has a default this wave took; none blocks DESIGN.

1. **K3 as an exit criterion** (D19, DV-5, orchestrator decision). *Default: it stands; if overturned, the isolation
   step in slice 01 becomes trigger-gated and Task 01a stays first.*
2. **Measurement as a Task under Story 01** (D17), not its own Story. *Default: yes; `/ado-sync` creates it as a
   Task.*
3. **The nudge's place, boundary and copy** (D23): edit page only, strictly more than 100, the copy above. Also on
   the Features tab? *Default: edit page only, because only an editor can act on it.*
4. **Where the size bucket is emitted** (D24). *Default: slice 01 (the rule's "first usable" slice); alternative:
   slice 04, where the Features tab changes anyway.*
5. **The Features list's messages** (D22), including the all-hidden one. *Default: as written; settled in DISTILL's
   sketch.*
6. **K4's figures** (skipped ≤ 15%, computed −30%) and K2/K3's 1.2 × idle tolerance. *Default: as written, revised
   after the 01a baseline.*
7. **Website claim** once K1–K3 are met. *Default: none; release notes only.*

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

- **Key decisions**: D17 (measurement as precursor Task inside Story 01), D18 (renumbering to 01–06), D19 (K3 exit
  criterion, flagged), D20 (no response-shape change, no clients release), D22–D23 (looks and nudge), D24 (size bucket
  in slice 01), D25 (production-data checks), D27 (re-score after 01a), D28 (6264 rules), D31 (one Epic).
- **Requirements summary**: two reused jobs; six Stories, each releasable alone; K3 is the north star.
- **Walking skeleton**: brownfield, none new; slice 01 is the first end-to-end proof.
- **Feature type**: cross-cutting.
- **Constraints established**: C1–C12 carried; D20, D26, D28, D30 added.
- **Upstream changes**: see Changed Assumptions.

---

## Wave: DISCUSS / [REF] Changed Assumptions

DISCOVER and DIVERGE sections are not edited; these supersede them from DESIGN onward.

| Was (source, verbatim) | Now | Why |
|---|---|---|
| DISCOVER Handoff waiver: "It makes the measurement the Epic's **first slice**: the dev-only scaled data set (P1) and a before/after harness." | The measurement is **Task 01a, the first commits of Story 01**, not a slice (D17). | nWave's slice-composition gate: a slice of only `@infrastructure` work cannot be released. |
| DIVERGE provisional order: "1. Measurement. An **internal enabler, not user value**. 2. The forecast round off the request pool." | Both are **slice 01**; the later slices are renumbered 02–06 (D18). | Same gate; the order is unchanged. |
| DIVERGE: "Slices 3, 4 and 6 swap freely; 5 must follow 4; 1 and 2 are fixed first; 7 is last." | 02, 03, 05 swap freely; 04 follows 03; 01 first; 06 last. | Renumbering only. |

---

## Wave: DISCUSS / [REF] Handoff

To DESIGN (`nw-solution-architect`): these DISCUSS sections, `slices/slice-01..06`, the journey SSOT. To DEVOPS:
the Outcome KPIs and D24 (the size bucket). Open for DESIGN: the questions under "DESIGN must answer", each slice's
loading and failure look or its N/A, and the architecture summary CLAUDE.md asks for at the end of DESIGN.

---

## Wave: DISCUSS / [REF] Maintainer answers and ADO mirror — 2026-10-23

The maintainer accepted every default in "Questions for the maintainer" (K3 is an exit criterion; the measurement
is a Task under Story 01; the size note on the edit page only, above 100 Features, with the drafted copy; the size
bucket emitted in slice 01; the list messages settled at DISTILL's sketch; the provisional K4 and during-forecast
figures; no website claim).

| Slice | ADO |
|---|---|
| 01 Pages keep answering while Lighthouse forecasts | User Story #6404 (Task #6405: Measure with a scaled data set, dev-only) |
| 02 The Overview and Settings open without waiting on every Feature | User Story #6406 |
| 03 Large Features lists open sooner, with the same dependencies | User Story #6407 |
| 04 The Features list opens fast and says when it cannot load | User Story #6408 |
| 05 Forecast rounds finish sooner | User Story #6409 |
| 06 Lighthouse says when a Portfolio grows past 100 Features | User Story #6410 |
