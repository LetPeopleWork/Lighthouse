# Slice 2 A/B table: configuration candidates against the unchanged baseline (2026-11-03, 17:00 to 20:00)

Both arms at `614772515` (main with rewrite slices 4 to 6), `pnpm install --frozen-lockfile --ignore-scripts` in each,
6 workers, `TZ=Europe/Zurich`, time budget off, quiet machine (the maintainer's other session idle; desktop apps open;
one disturbance noted below). Every run is a full `pnpm exec vitest run` through `scripts/measure.sh`, alternating
baseline (A) and candidate (B). Test-id lists come from each run's saved JSON results, not from source.
Raw data: `screen-2026-11-03/<candidate>/` (first pass) and `ab-2026-11-03/<candidate>/` (full measurement), each with
`runs.txt`, the gzipped logs and results, and for the full ones `report.txt` (`scripts/ab_report.py`).

**Verdict: no candidate passes the adoption rule. Nothing goes to slice 3 (04-01 adopts nothing), so no CI arm is
needed (03-04) and the CI-shaped coverage run on a leading candidate has no candidate to run on.**

## How the candidates were narrowed

The baseline itself moved 246 to 267 s between undisturbed runs (about 8 %), so a single pair cannot show a 10 % gain.
A first pass gave each candidate one pair; the ones that were slower, used much more memory, or failed tests were
dropped there, with their numbers kept below. The two that looked faster got the full measurement.

## Table

Wall and memory are the full run's wall seconds and the peak memory of the run's process tree. "Ids" compares the
sorted `file :: full name` list with the baseline's 9,191.

| Candidate | What it changes | Pairs | Baseline wall s | Candidate wall s | Change | Peak memory | Ids, failures | Verdict |
|---|---|---|---|---|---|---|---|---|
| a node project | DOM-free `.test.ts` files (125) run in `node` with a trimmed setup file | 5 + 3 plain + 3 shuffled | median 249 (247-381) | median 254 (236-315) | **+2.0 %** | -0.7 % | identical; 0 failed in B runs | **Out**: no gain |
| b0 inline off | MUI and `react-transition-group` no longer inlined | 1 | 247 | 273 | +10.5 % | -14.4 % | identical; 0 | Out: slower |
| b1 inline narrow | only `@mui/x-*` and `react-transition-group` inlined | 1 | 247 | 272 | +10.1 % | -20.8 % | identical; 0 | Out: slower |
| b2 optimizer | MUI pre-bundled through `deps.optimizer.web` instead of inlined | 1 | 249 | 282 | +13.3 % | -7.1 % | identical; 0 | Out: slower |
| e1 forks | `pool: "forks"` | 1 | 246 | 268 | +8.9 % | +8.1 % | identical; 0 | Out: slower |
| e2 vmThreads | `pool: "vmThreads"` | 1 | 257 | 151 | -41.2 % | **+298 % (17.1 GB; 5.8 GB left on the machine)** | **22 ids missing, 68 failed in 9 files** | Out: memory and failures |
| f 12 workers | `LIGHTHOUSE_TEST_WORKERS=12` in the candidate arm | 1 | 246 | 232 | -5.7 % | **+63 %** | identical; 0 | Out: memory; and CI has 4 cores, so the gain would not carry |
| g module cache | `fsModuleCache: true` | 3 (stopped) | median 247 (246-248) | median 247 (247-254) | **0.0 %** | +3.8 % | identical; 0 | **Out**: no gain once warm |
| c happy-dom (03-03) | `environment: "happy-dom"` (happy-dom 21.0.4, MIT, candidate checkout only) | 1 | 267 | 246 | -7.9 % | **+30 %** | **28 ids missing, 77 failed in 21 files** | Out: failures and memory |
| d node + isolate off | not run | | | | | | 173 failed in its earlier smoke run | Out before timing |

Notes per row:

- **a**: The first-pass pair showed -4.9 %; the full measurement does not hold it. Pair 1's candidate run took 315 s and
  baseline run 5 took 381 s (Slack started during it and free memory fell to 12.8 GB; that run also failed 1 test, see
  below). The five pairs (baseline/candidate) were 249/315, 263/254, 247/236, 248/250, 381/254;
the three undisturbed ones point both ways, so the change is within the noise. The
  3 plain runs passed (255, 252, 255 s, 9,191 tests each). The 3 shuffled runs found 2 failing tests in 2 of 3 seeds,
  which belong to the suite, not to this candidate (see "Found in passing").
- **b0, b1, b2**: the reason MUI is inlined still holds as a speed reason even though the old directory-import failure
  is gone on Vitest 5.0.5: Vite transforms MUI once per worker and caches it, while Node's own loader evaluates the
  many small external files per test file. Not inlining saves memory (up to -21 %) and costs 9 to 13 % wall.
- **e2**: the speed comes from sharing modules between files in one VM context, and so does the damage: memory grew
  to 17 GB and 9 files failed (including `WorkItemsDialog`, `LegendChip`, `PredictabilityScore`, the DataGrid toolbar
  and the print pages), 22 tests were not even collected.
- **g**: the first-pass -15.7 % came from a baseline run slowed by the 17 GB `vmThreads` run just before it (that
  baseline run took 313 s and failed 3 tests on the 5 s timeout). In the full measurement, pair 1's candidate run
  (cold cache) took 254 s and the warm runs 247 s, against 248 and 246 s for the baseline; stopped after 3 pairs
  because the -10 % bar was out of reach (`ab-2026-11-03/g/STOPPED.txt`). CI starts cold every time, so it would gain
  even less.
- **c (03-03)**: measured because environment set-up was 18.5 % of worker time in the 03-01 split, above the 10 % line,
  and no other candidate reduced it. The 21 failing files under happy-dom were run unmodified: the PDF export and print
  pages (`ReportView.exportPdf`, `ReportView.exportPdf.failures`, `ReportsTab.exportPdf`, `ReportsPage.combinedPdf`,
  `ReportsPage.usageData`, `ReportPrintPage` and its `runningHeader` and `lighthouseMark` files, `ReportsPrintPage`,
  `browserPrintEffects`, `ReportMarks.drawn`, `ReportView`), the chart pieces (`LegendChip`, `PredictabilityScore`,
  `WorkDistributionChart`, `scatterMarkerUtils`), `WorkItemsDialog`, `DataGridToolbar`, `ForecastLikelihood`,
  `TimeInStateBadge` and `LighthouseLogo`. Passing them would mean editing tests, which makes the candidate eligible per
  file at best; at -7.9 % on one pair and +30 % memory it is not worth a per-file split. The DataGrid, Dialog and
  user-event test files that are not on that list ran unmodified and passed.

## What this says about where the time is

Configuration moves the wall by a few percent at most. The 03-01 split explains why: 46 % of worker time is the tests
themselves and 30 % is importing what they render; the slowest files are slow in their tests (`WorkItemsDialog.contexts`
23.5 of 25.1 s). From today's CI timings (run 41691382136): `.ts` logic tests have a median of 0.3 ms and take 28 s in
total; `.tsx` component tests have a median of 74 ms and take 1,096 s, 97 % of the suite. The payoff is in the rewrite
slices, which move rules from component tests into logic tests.

## Found in passing

- **Order-dependent tests**: `PortfolioDetail.test.tsx`, group "a refresh that failed", tests "says so on the refresh
  button" and "keeps saying so when a forecast run finishes afterwards" fail with seeds 11 and 33 of
  `--sequence.shuffle` ("Unable to find an element with the title: Last Features refresh failed"). The file runs in
  jsdom in both arms, so the order dependence is the suite's. Seed 22 instead hit one unhandled error
  (`TypeError: Cannot read properties of undefined (reading 'nodeName')`). Follow-up for the track.
- **Tests that time out under heavy load**: in baseline runs on a loaded machine, `ReportsTab.exportPdf` ("gives the
  keyboard focus back to the row's PDF icon when the reader chooses Retry"), `ReportsPage.usageData` ("a Retry and a
  Leave it out on one PDF of several reports count nothing more"), `ThirdPartyPackagesSection.paging` ("keeps the
  reader's sort and column filter when a search opens the first page") and `ReportsPage.combinedPdf` ("when part of
  the page cannot be loaded no PDF is made and it says so") ran into the 5 s test timeout. All passed in every
  undisturbed run. Candidates for the flaky-test watch.

## CI arm (03-04)

Skipped: no candidate passed the local rule, so there is nothing to run on CI. No branch was pushed.
