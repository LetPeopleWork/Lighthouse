# Slice 11 - Metrics views: profile, universe, preserved set, delta

All timing figures in this slice are indicative (not quiet): the machine was shared with a parallel session (load
average about 2 on 4 cores during the runs, down from 5 to 7 an hour earlier).

## Pre-slice commit

`35a61e1abdba5ca254f9e518e5dcef53dc598821` (HEAD of main when this was declared).

## FE5 has not landed

FE5 (ADO #6393 "Split wide units") is still New, so the `BaseMetricsView` split has not happened. By the roadmap rule
(the 12-01 external-predecessor item), `src/pages/Common/MetricsView/BaseMetricsView.tsx` (2074 lines) is **left alone and
ratcheted**: no extraction, no edit. The only production file this slice could ever touch is
`src/pages/Overview/OverviewDashboard.tsx`. What follows from that is in "Recommendation for 12-02" below.

## Allow-list entries (`src/tests/timeBudget/slowTests.ts`)

None. `grep -n "Metrics\|Overview" src/tests/timeBudget/slowTests.ts` finds one line, the `DashboardHeader.test.tsx`
entry (a different file, not part of this slice). No entry names `OverviewDashboard*` or `BaseMetricsView*`, so there is
nothing to delete or lower, and the allow-list is unchanged by this slice.

## Touched files and universe

Test files measured (the universe of what a 12-02 rewrite could edit; none is edited in 12-01):

- `src/pages/Common/MetricsView/BaseMetricsView.test.tsx` (198 tests)
- `src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx` (133 tests)
- `src/pages/Common/MetricsView/BaseMetricsView.cumulativeStateTimeDialog.test.tsx` (6 tests)
- `src/pages/Overview/OverviewDashboard.test.tsx` (51 tests)
- `src/pages/Overview/OverviewDashboard.licenseCheck.test.tsx` (8 tests)

Production files: none changes. `BaseMetricsView.tsx` is ratcheted; `OverviewDashboard.tsx` has no extraction candidate
(see the findings).

Extractions named: **none**. No extraction is proposed, because the profile finds no real wait and no pure rule inside
the files that costs time (see below).

## Profile: where the time goes

Method: the same as the other slices. Touched files only, `LIGHTHOUSE_TEST_WORKERS=2`, `TZ=Europe/Zurich`, vitest JSON
reporter for per-test seconds; CPU profile per file with `--pool=forks --execArgv=--cpu-prof`, summarised by
`measurement/profile-2026-11-04/summarize.py`. Saved under `measurement/profile-2026-11-04/<file>/` (`command.txt`,
`run.log`, `summary.txt`, gzipped `.cpuprofile`). Commands:

```
pnpm exec vitest run <file> --pool=forks --execArgv=--cpu-prof \
  --execArgv=--cpu-prof-dir=measurement/profile-2026-11-04/<name>
```

Per-test spread (summed seconds is the figure the close pairs would compare):

| File | Tests | Summed s | Median ms | Tests > 100 ms | Tests > 200 ms | Share of the 5 slowest |
|---|---|---|---|---|---|---|
| `BaseMetricsView.loading.test.tsx` | 133 | 9.31 | 59 | 24 | 2 | 17 % |
| `BaseMetricsView.test.tsx` | 198 | 4.77 | 18 | 5 | 1 | 17 % |
| `BaseMetricsView.cumulativeStateTimeDialog.test.tsx` | 6 | 1.81 | 233 | 6 | 5 | 90 % |
| `OverviewDashboard.test.tsx` | 51 | 9.17 | 158 | 34 | 19 | 25 % |
| `OverviewDashboard.licenseCheck.test.tsx` | 8 | 2.15 | 267 | 8 | 4 | 83 % |
| Sum | 396 | 27.21 | | | | |

Cost attribution (self time of each file's CPU profile, indicative):

| Cost | `BaseMetricsView.loading` | `BaseMetricsView.test` | `OverviewDashboard.test` |
|---|---|---|---|
| Render (react + react-dom) | 27.5 % | 17.5 % | 26.9 % |
| jsdom (DOM, style lookups) | 8.9 % | 13.6 % | 10.9 % |
| MUI (material + system + utils, DataGrid in Overview) | 12.7 % | 3.5 % | 14.9 % |
| Provider stack (tanstack query-core, emotion) | 4.7 % | 2.8 % | 2.3 % (emotion styled) |
| Module import, vitest, node core, native | 34 % | 48 % | 30 % |
| Lighthouse source (data fixtures, components, mocks) | 2.4 % | 2.7 % | below 2.5 % |
| Real waits (`setTimeout` sleeps, `waitPastTheDebounce`) | none | none | none |

Findings:

- **No real wait anywhere.** The only `setTimeout` is a zero-delay microtask flush in `BaseMetricsView.test.tsx` (line
  ~6290); the two `new Promise(() => {})` in `OverviewDashboard.test.tsx` are held-open promises that pin a loading look, not
  sleeps. `useFakeTimers` has nothing to replace.
- **The time is render, not waiting.** In the loading file the cost is the whole dashboard mounted per test (React and
  ReactDOM 27 % of self time, MUI 13 %), 133 times at a median of 59 ms. It already mocks `DashboardHeader`, so the
  remaining cost is the widgets themselves, which is what the tests exist to pin. The main file is dominated by import and
  setup (47 % native, node, vitest): 198 tests at a median of 18 ms, 4.8 s in total.
- **The tail is flat.** Five slowest tests carry 17 % in both BaseMetricsView files and 25 % in `OverviewDashboard.test`.
  There is no hotspot test to delete or fake. The two files with a heavy top 5 (`cumulativeStateTimeDialog`,
  `licenseCheck`) total 4 s together.
- **No extractable rule.** `OverviewDashboard.tsx` (725 lines) holds only two small helpers (`connectionsRefusedMessage`,
  `reportsClauseOf`) and its tests drive them through the rendered dashboard at 0.3 to 0.5 s per deletion-report test; a
  pure-function extraction would save a few hundred milliseconds. The rule-like parts of `BaseMetricsView` (category fetch
  requirements, window presets) are already pure and tested in `categoryMetadata` and `dateWindow`.
- **Largest saving available without touching `BaseMetricsView.tsx`:** fewer widgets mounted per loading test (for example
  one category instead of all). That changes what each of the 133 tests pins (the loading and failure looks of the real
  widget set), so it is not behaviour-free and is not proposed.

## Recommendation for 12-02

**Do nothing: drop 12-02 (slice 11's rewrite) on measured payoff.** Numbers: 27.2 s summed over 396 tests (about 70 ms a
test), no real wait, no allow-list entry, a flat tail, 34 % to 47 % of the CPU in import and setup that a test rewrite cannot
touch. The optimistic ceiling for a rewrite that stays behaviour-free is the `OverviewDashboard` deletion-report tests and
a couple of `licenseCheck` renders, under 2 s (below 7 % of these files, about 0.2 % of the suite's summed seconds), against
the risk of editing the files that pin every loading and failure look. The original estimate ("about 15 s, low confidence")
is replaced by this measured ceiling. Revisit only after FE5 splits `BaseMetricsView`: smaller units would let the tests
mount one widget instead of the dashboard, which is where the render cost could really fall.

If the maintainer wants a rewrite anyway, the single candidate is `OverviewDashboard.tsx` plus its two test files, with a
saving under 2 s.

## Preserved set (before)

`measurement/slice-11-ids-before.txt`: 396 ids, one per line, sorted, as `file :: fullName` (paths relative to
`Lighthouse.Frontend/`), from a run of the five files (396 passed, 0 skipped, 0 failed; `LIGHTHOUSE_TIME_BUDGET=warn`,
vitest json reporter). Per file: `BaseMetricsView.test.tsx` 198, `BaseMetricsView.loading.test.tsx` 133,
`BaseMetricsView.cumulativeStateTimeDialog.test.tsx` 6, `OverviewDashboard.test.tsx` 51,
`OverviewDashboard.licenseCheck.test.tsx` 8. If 12-02 runs, surviving ids must be unchanged at its close.

Command (in `Lighthouse.Frontend`):

```
LIGHTHOUSE_TEST_WORKERS=2 LIGHTHOUSE_TIME_BUDGET=warn TZ=Europe/Zurich pnpm exec vitest run \
  src/pages/Common/MetricsView/BaseMetricsView.test.tsx \
  src/pages/Common/MetricsView/BaseMetricsView.loading.test.tsx \
  src/pages/Common/MetricsView/BaseMetricsView.cumulativeStateTimeDialog.test.tsx \
  src/pages/Overview/OverviewDashboard.test.tsx \
  src/pages/Overview/OverviewDashboard.licenseCheck.test.tsx --reporter=json
```

## Before figures (indicative, not quiet)

| File | Tests | Summed seconds |
|---|---|---|
| `BaseMetricsView.test.tsx` | 198 | 4.89 |
| `BaseMetricsView.loading.test.tsx` | 133 | 9.57 |
| `BaseMetricsView.cumulativeStateTimeDialog.test.tsx` | 6 | 1.73 |
| `OverviewDashboard.test.tsx` | 51 | 9.01 |
| `OverviewDashboard.licenseCheck.test.tsx` | 8 | 2.11 |
| Sum | 396 | 27.31 |

(A second run, used for the spread table above, summed 27.21 s.) Only one pair-side exists because no rewrite is declared;
if 12-02 runs, its close adds the 5 alternating pairs against this commit.

## Coverage (before)

`src/pages/Common/MetricsView/**` and `src/pages/Overview/**` from a run of both directories (49 files, 1190 tests, all
passed):

```
LIGHTHOUSE_TIME_BUDGET=off TZ=Europe/Zurich pnpm exec vitest run src/pages/Common/MetricsView src/pages/Overview \
  --coverage.enabled --coverage.include='src/pages/Common/MetricsView/**' \
  --coverage.include='src/pages/Overview/**' --coverage.reporter=text
```

| Scope | % Stmts | % Branch | % Funcs | % Lines |
|---|---|---|---|---|
| All files (both directories) | 95.33 | 93.51 | 94.70 | 95.59 |
| `src/pages/Common/MetricsView` | 97.47 | 94.75 | 97.19 | 97.66 |
| `src/pages/Overview` | 80.24 | 81.31 | 73.68 | 81.41 |

Line (95.59) and branch (93.51) coverage must not be lower if 12-02 runs.

## Tests that pin the loading and failure looks (must keep their assertions)

These stay as they are; a 12-02 rewrite, if any, may not weaken them.

| File | Test group (describe) | Assertion kept |
|---|---|---|
| `BaseMetricsView.loading.test.tsx` | "a dashboard as it opens" | every chart's frame is in place with a spinner before any data arrives; a first visit whose data cannot be loaded says so in the chart's frame; no chart is added or removed once data arrives |
| `BaseMetricsView.loading.test.tsx` | "a team's charts while the window is changing" and the light/dark "after the reader picks another window" groups | the charts dim under a spinner while the new window loads and show the new window's data, never the previous answer as current |
| `BaseMetricsView.loading.test.tsx` | "a team's chart when windows are picked in quick succession" | answers arriving out of order never land on the wrong window; the chart stays loading until the last one |
| `BaseMetricsView.loading.test.tsx` | "a team's chart whose data cannot be loaded" | one failed request marks only its chart, says it could not be loaded, stops its spinner, leaves the others alone, and comes back once the reader retries |
| `BaseMetricsView.loading.test.tsx` | "a team's aging chart while its Doing columns are on their way" | the aging chart says its columns are on their way instead of showing an empty area |
| `BaseMetricsView.cumulativeStateTimeDialog.test.tsx` | "a late answer for the bar clicked before ..." | a late answer for an earlier click never lands in the dialog |
| `OverviewDashboard.test.tsx` | "renders loading state initially", "renders portfolios section after loading", "when the connection list is refused" | the loading look first, then the sections; a refused connection list is reported as a refusal and is silent while roles are still loading |
| `OverviewDashboard.licenseCheck.test.tsx` | "the Overview's create actions while the licence is checked" and "the Get Started steps follow the same licence check" | create actions stay disabled while the licence is being checked or cannot be checked, and enable only once it is checked and there is room |

## Delta

12-01 changes no test file and no production file: the declaration, the profile and the preserved set only. `expect(`
removed or added: 0 / 0. Ids missing: 0. Allow-list unchanged (this slice has no entries). Coverage unchanged by
definition. File time: no after side exists; no rewrite is made.
