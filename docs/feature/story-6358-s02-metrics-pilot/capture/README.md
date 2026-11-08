# Flow metrics response capture (Story 6358, slice 0)

The safety net for the Flow metrics move. The later slices change namespaces only, so every HTTP
response of the metrics read surface must stay byte-for-byte the same. The repository has no
response snapshots, so this folder holds a one-off capture tool instead of a committed golden-file
test. The captures themselves are not committed.

## The rule: capture base and candidate back to back, on the same day

Many answers depend on "today": item ages, the "current" branch of `blockedItemsAtDate`, SLE risk,
the feature list, refinement, and the last day the over-time endpoints may ask to fill. So a capture
is only comparable with a capture made on the **same calendar day (UTC)** on the same backup file.

Run the script on the base revision and on the candidate revision **one after the other**, then
compare those two. Never compare a candidate with an old capture. `compare.sh` refuses captures from
different days or from different backup files. Do not start a pair just before midnight UTC.

```bash
cd /storage/repos/Lighthouse
CAP=docs/feature/story-6358-s02-metrics-pilot/capture
OUT=$HOME/.cache/nwdes/story-6358

$CAP/capture-metrics.sh <base-rev>      $OUT/capture-base-$(git rev-parse --short <base-rev>)
$CAP/capture-metrics.sh <candidate-rev> $OUT/capture-candidate-$(git rev-parse --short <candidate-rev>)
$CAP/compare.sh $OUT/capture-base-<sha> $OUT/capture-candidate-<sha>   # exit 0 = identical
```

One run took about 2.5 minutes on `dccce57e3`: about 1 minute to build, then three request rounds.
It needs `git`, `dotnet` (.NET 10 SDK), `curl`, `jq`, `sqlite3` and `python3`. In a Claude session
the lean-ctx hook needs `LEAN_CTX_DISABLED=1` in front of the call (the script runs `git worktree`),
and it must be called by its path, not through a variable.

## What a run does

1. Makes a detached `git worktree` of the revision under `$HOME/.cache/nwdes/story-6358/run-*`
   (outside the repository; the physical path is used, because `$HOME/.cache` is a symlink here and a
   symlinked path makes the build ignore `.editorconfig`). It builds both migration projects, then
   the backend with `--no-incremental`, because the backend references the migration DLLs by path.
   The main checkout's `bin`/`obj`, key rings and database are not touched.
2. Copies `Lighthouse.Backend/Lighthouse.Backend/DB_Backup/LighthouseAppContext.db` (and its
   `-wal`/`-shm`) from the main checkout into the work folder. Real history: 3 teams and 3
   portfolios. The file is gitignored, so it is not in the worktree. `CAPTURE_DB_DIR` selects another
   folder. The copy is made safe to run offline:
   - `PeriodicRefresh:{Throughput,Features,Forecasts}:{StartDelay,Interval}` are set to 40000 minutes,
     so the team and portfolio updaters never start a pass during the run. Connection health derives
     its cadence from the same intervals, so it does not probe the connections either. 40000 minutes
     stays under the ~49.7-day ceiling of `Task.Delay`.
   - Every tracker URL option is set to `http://127.0.0.1:9`, a closed local port.
3. Starts the backend from the built DLL with:
   - `Kestrel__Endpoints__Http__Url` and `Kestrel__Endpoints__Https__Url` on two free local ports
     (`ASPNETCORE_URLS` is ignored, because `appsettings.Development.json` pins 5169/7246). Both
     endpoints use plain http, so no certificate is needed.
   - A new, empty key ring in the work folder (`Encryption__KeyStorePath`, never `data-protection-keys`)
     and `Encryption__StartEvenIfNothingStoredCanBeRead=true`. The stored credentials cannot be
     decrypted, so no connector can authenticate.
   - `HTTP_PROXY`/`HTTPS_PROXY`/`ALL_PROXY` set to the closed port, so any outbound HTTP call fails
     locally.
   - `Lighthouse__TimeZone=UTC` and `TZ=UTC`.

   That is four separate layers between the run and a real Jira or Azure DevOps. No secret is printed.
4. Builds the request list (see below), then replays it in **rounds** until two consecutive rounds
   are byte-identical (at most `CAPTURE_MAX_ROUNDS`, default 10). After each round it waits until the
   snapshot tables did not change for 20 seconds.
5. Writes the output, stops the server, removes the worktree and deletes the work folder with the
   database copy and key ring. `CAPTURE_KEEP_WORK=1` keeps the work folder for debugging. It then
   holds a copy of the live (encrypted) credentials: delete it when done.

### Why rounds: the over-time history filler

A dated request to `percentiles-over-time` or `process-behavior-over-time` asks the hosted
`OverTimeHistoryFiller` to work out the missing days of the window in the background. The filler
writes at most 90 days and runs at most 10 seconds per pass. So the first answers depend on how far
the filler got, which depends on machine speed. The capture keeps the **settled** answers (on
`dccce57e3`, the answers settled after round 3). The filler's writes are part of what moves, so the
snapshot tables at the end of the run are compared too (`db/`). Periodic refresh is off, so the
filler is the only background writer during the run.

### Excluded values: the Monte Carlo predictability score

`multiitemforecastpredictabilityscore` (both views) and `predictabilityScoreInfo` run a Monte Carlo
"how many" forecast. Its random source is not seeded (`DrawStreamFactory` uses `Random.Shared`), so
the trial histogram, the percentiles, the score and the score's comparison sentence change from run
to run while the code is unchanged. Inside one run the metrics cache can hold a result for some time,
so rounds can agree, but two runs do not. In these responses those values are replaced by a marker.
The status, the shape, the labels and the total number of trials are still compared. The raw bodies
are kept in `raw-monte-carlo/`, which `compare.sh` does not compare.

## Output layout

```
<out>/_manifest.txt           revision, capture time, today (UTC), window, request count, rounds, sha256 of the backup files
<out>/requests/<owner>/<endpoint>[__variant].json   {status, request, body} (or bodyText), keys sorted (jq -S)
<out>/db/<table>.tsv          PercentilesOverTimeSnapshots, ProcessBehaviorSnapshots, BlockedCountSnapshots at the end of the run, without Id, sorted
<out>/raw-monte-carlo/...     raw predictability-score bodies, not compared
```

## Requests (366 per round on the current backup)

Fixed window `startDate=2026-07-20&endDate=2026-10-17`. The backup's history ends on 2026-10-17, so no
day of the window lies in the future. `asOfDate=2026-10-17`. `blockedItemsAtDate` uses `2026-10-01`
(reconstructed past) and `2099-01-01` (the current-state branch). Teams and portfolios come from
`GET /api/v1/teams` and `GET /api/v1/portfolios`.

Per team (`/api/v1/teams/{id}/metrics/…`, from `TeamMetricsController` and
`TeamFeatureSizeMetricsController`): `throughput` (default and `view=filtered`), `arrivals`,
`wipOverTime`, `featuresInProgress`, `wip`, `forecastInputCandidates`, `cycleTimePercentiles`,
`workItemAgePercentiles`, `sleRisk`, `ageInStatePercentiles`, `cumulativeStateTime`,
`cumulativeStateTime/items` (once per state the chart returns), `cumulativeStateTime/candidates`,
`cycleTimeData`, `multiitemforecastpredictabilityscore` (both views), `totalWorkItemAge`,
`throughputInfo`, `arrivalsInfo`, `wipOverviewInfo`, `flowEfficiencyInfo`, `featuresWorkedOnInfo`,
`totalWorkItemAgeInfo`, `predictabilityScoreInfo`, `cycleTimePercentilesInfo`, `throughput/pbc`
(both views), `arrivals/pbc`, `wipOverTime/pbc`, `totalWorkItemAge/pbc`, `cycleTime/pbc`,
`estimationVsCycleTime`, `blockedCountHistory`, `percentiles-over-time` (CycleTime with horizons 30, 60
and 90, and WorkItemAge, each over the window and over the whole history),
`process-behavior-over-time` (all six types, over the window and over the whole history),
`blockedItemsAtDate` (past and current), `allFeaturesForSizeChart`, `sizePercentiles`,
`featureSizePercentilesInfo`.

Per portfolio (`/api/v1/portfolios/{id}/metrics/…`, `PortfolioMetricsController`): the same shared
set (without the team-only `view=filtered` variants, `featuresInProgress`, `featuresWorkedOnInfo`,
`forecastInputCandidates`, `sleRisk`), plus `started`, `featureSize/pbc` and `featureSizeEstimation`.

Other consumers of the metrics services: `GET /api/v1/teams/{id}/feature-list`,
`GET /api/v1/portfolios/{id}/feature-list`, `GET /api/v1/teams/{id}/refinement`, the report lists,
templates and each report of every team and portfolio, and `GET /api/v1/reports`.

Not covered:
- The `POST` forecast endpoints (`ForecastController`, `ForecastRealityCheckController`), which use
  `ITeamMetricsService`. They run unseeded Monte Carlo simulations, and some write forecasts.
- The optional `definitionId` (named cycle-time definitions) and `itemIds` filters. Only the
  defaults are requested.
- The over-time endpoints' first, unsettled answers (see "Why rounds").
