# Slice 1a calibration (2026-11-03)

Quiet machine (the parallel session paused), `main` at `e1518eab7`, 12-core / 31 GB Linux reference machine,
`dotnet build-server shutdown` beforehand, `TZ=Europe/Zurich`. Every run was
`pnpm exec vitest run --reporter=default --reporter=json` (the `test` script's `--reporter=default` drops the config
reporters until step 01-07 removes it), sampled with `story-6461-t3-test-runs-leave-room/measurement/measure.sh`.
Raw data: `calibration-2026-11-03/` (`runs.txt`, a JSON and a log per run, `offenders.json`, `seed-allow-list.json`).
Analysis scripts were run from the session scratchpad; their logic is described below so it can be repeated.

## Runs

| Run | Workers | Kind | Wall (s) | Vitest Duration (s) | Peak RSS (MB) |
|---|---|---|---|---|---|
| plain-1 | 6 | plain | 254 | 252.08 | 5,028 |
| cov-1 | 6 | `--coverage` | 326 | - | 5,157 |
| plain-2 | 6 | plain | 248 | 246.25 | 4,523 |
| cov-2 | 6 | `--coverage` | 319 | - | 5,230 |
| over-1 | **12** | plain, oversubscribed | 225 | 223.57 | 7,159 |
| over-2 | **12** | plain, oversubscribed | 220 | 219.32 | 7,707 |
| over-3 | **12** | plain, oversubscribed | 222 | 221.00 | 7,570 |

The 12-worker override (`LIGHTHOUSE_TEST_WORKERS=12`) was used for the three oversubscribed runs only. All seven runs
green (578 files). Oversubscribed runs finish sooner on the wall but each test takes longer, which is what they are for:
they bound the slowdown a busy machine adds per test.

CI samples: the 7 saved artifacts in `ci-artifacts-2026-11-02/` (all usable; at least 5 were required).

## Results

| Figure | Value | How |
|---|---|---|
| **A** (first-test allowance) | **0.31 s** | Per file, the first test that ran (local JSON, file order) minus the median of the file's other tests, floored at 0; 90th percentile over 568 files with at least two tests; median of the two plain runs |
| CI / local plain, per test | median 1.98, p90 2.61 | 1,954 tests that took at least 0.1 s locally; CI = median over the 7 artifacts, local = median of the two plain runs; key `file :: full test name` |
| **F** (CI factor) | **3.0** | p90 2.61 rounded up to the next 0.5 |
| Local coverage / plain, per test | median 1.35, p90 1.78 | Same machine, so this is the coverage effect alone; the rest of CI's factor is the machine |
| Oversubscribed (12) / plain (6), per test | median 1.66, p90 2.10 | |
| Plain run-to-run spread, per test | median 8.6 %, p90 28.3 % | |
| **B** (budget) | **2 s, unchanged from DESIGN** | Rule below |

### B by the written rule

Threshold per sample: `B x F (+ A for the first test of a file)`, F = 1 for the local runs (plain and oversubscribed),
F = 3.0 for the CI artifacts. A test "offends" when it is strictly over its threshold in any of the 12 samples.

| B | Tests over the threshold | In files a rewrite slice names | Outside (need their own reason) |
|---|---|---|---|
| 1 s | 165 | 138 | 27 |
| **2 s** | **30** | **27** | **3** |
| 3 s | 4 | 4 | 0 |

The smallest B for which every offender sits in a named file or can carry its own reason is 2 s: at 1 s, 27 tests in
files no slice plans to touch would each need a standing reason, which turns the list into a permanent exemption
register rather than a plan. 3 s would hide 26 tests the rewrite slices are meant to make fast. B stays 2 s, so no dated
re-check note is owed to the feature-delta's D4 row or the Epic KPI 14 row.

The three outside offenders at B = 2 s, each given its own reason when the list is seeded:

- `src/pages/Common/MetricsView/DashboardHeader.test.tsx :: DashboardHeader shows label and formatted date range and
  opens popover which calls handlers` (plain 1.53 s, worst 2.80 s oversubscribed)
- `src/pages/Reports/ReportsPage.usageData.test.tsx :: What the Reports page tells the usage data a Retry and a Leave it
  out on one PDF of several reports count nothing more` (plain 1.23 s, worst 3.15 s on CI)
- `src/pages/Settings/SystemInfo/ThirdPartyPackagesSection.paging.test.tsx :: Third-party packages pages like every
  other list keeps the reader's sort and column filter when a search opens the first page` (plain 1.22 s, worst 3.24 s
  on CI)

### Flake bound and the seed for step 01-08

The seed is all 30 offenders at B = 2 s, so the calibration samples show **zero unlisted tests over the threshold**.
Each entry's ceiling (quiet-local seconds) is the highest value any sample implies for it, `(seconds - A if first) / F`
for that sample's F, never below its plain median, rounded up to the next 0.5 s. Ceilings run from 2.5 s to 5.0 s
(the slowest, `RefinementView.paging` "Jonas pages to GR-0131", plain 2.77 s). The oversubscribed runs set most
ceilings, which is the point of the flake bound: a busy machine must not fail a listed test that has not got slower.
A residual false failure on an unmeasured, busier machine is answered by a re-run, never a retry in the reporter.

Review finding rejected (2026-11-04): the `RefinementView.paging` ceiling of 5.0 s does not break the rule above. The
oversubscribed sample over-1 read 4.94 s, F = 1 for that sample and the first-test allowance is 0.31 s, giving 4.63,
rounded up to 5.0. The seed's `worst` field shows the largest raw value (CI 7.34 s), not the sample that set the
ceiling, which is what misled the review.

`calibration-2026-11-03/seed-allow-list.json` holds, per entry: key, family (rewrite slice or none), first-in-file,
plain median, ceiling and worst sample. Step 01-08 seeds `slowTests.ts` from it without a fresh full run.

## CI log Duration fetch (for the CI after-measurements)

```
gh api --allow-escape-sequences repos/LetPeopleWork/Lighthouse/actions/jobs/125221344639/logs \
  | sed 's/\x1b\[[0-9;]*m//g' | grep -aE '^\S+\s+Duration\s+[0-9.]+s' | tail -1
```

Output: `2026-11-02T10:44:03.9304835Z    Duration  1069.85s (tests 53%, import 22%, environment 14%, transform 5%, setup 4%, worker 1%)`,
the same value saved for run 41428581254 in `ci-vitest-duration.txt`. The pattern must be anchored: a loose
`grep Duration` matches the coverage table's `formatDuration.ts` row first.
