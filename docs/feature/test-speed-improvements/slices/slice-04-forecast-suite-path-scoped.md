# Slice 04 — CS-J: Path-scoped forecast simulation tests

**Status**: PLANNED, not implemented. Written 2026-08-23.

**Goal (one sentence)**: Extend the CS-H path-scoping mechanism to the heavy forecast-simulation
fixtures, so a push that touches no forecast code stops paying for three fixtures that re-run the
benchmark Portfolio eleven times — while the one fixture whose whole purpose is catching an
*unrelated* change stays on every build.

**Owner story**: new. ADO **#5020 and #5258 are both Closed**, so this is not a resume of either; it
needs a Story of its own before any commit.

**Estimated effort**: half a day. The mechanism already exists and has a test suite; this is one more
lane through it.

**Learning hypothesis**:
- *Confirms*: a push touching no forecast path drops ~166 s of summed test time, ~50 s of `Verify
  Backend` wall clock, and the gated fixtures still run on every push that does touch forecast code.
- *Disproves*: if the path regex is too narrow, a change elsewhere moves a forecast date and no build
  says so until somebody next edits the simulation. This is the risk the always-on canary exists to
  bound — see *What is deliberately not gated*.

---

## Today's picture

Measured from `test-timings-backend.csv` on run `32640240106` (gates green, 2026-08-23). Backend job
**12m13s** = 47 s setup + 2m06s build + **8m21s test**. Runner is `ubuntu-latest`, 4 vCPU; 1647.3 s
summed over a 501 s window is **3.3× effective parallelism at 82 % packing**, so summed savings
convert to wall clock at roughly ÷3.3.

| Fixture | n | Summed | Gate? |
|---|---|---|---|
| `TheDrawSourceChangedTheDistributionDidNotTest` | 1 | 79.0 s | **yes** |
| `TheJointForecastIsAffordableTest` | 2 | 62.5 s | **yes** |
| `SharedClockBaselineTest.TheSameForecastTwice_ProducesTheSameDates` | 1 | 24.1 s | **yes** |
| `SharedClockBaselineTest.ForecastOfTheBenchmarkPortfolio_MatchesTheRecordedBaselineExactly` | 1 | 11.8 s | **no — canary** |
| `Slice00OneForecastPerBatchScenarios` | 7 | 112.0 s | **no — see below** |

Gated total **165.6 s summed ⇒ ~50 s wall**. Job 12m13s → **~11m20s** on a push that touches no
forecast code, unchanged on one that does.

Be honest about the size of this: it is fifty seconds. It is worth doing because it is cheap,
reversible, and touches no test's meaning — not because it solves the twelve minutes.

### Why these fixtures cost what they do

The forecast namespace runs **7.4× slower under coverage instrumentation**. Measured locally, 12
tests, `MaxCpuCount=0`: **16 s without `--collect`, 119 s with** `--collect:"XPlat Code Coverage"` and
`Include="[Lighthouse]*"` — which is exactly what `ci_backend.yml` runs whenever `run_sonar` is true,
i.e. every push to `main`. Coverlet instruments `[Lighthouse]*`, and that is precisely where the Monte
Carlo inner loop lives.

Two consequences worth carrying forward. Any local stopwatch on these tests under-states their CI cost
by about seven times, so a local "that's fast now" measurement proves nothing. And this slice does not
remove that multiplier — it stops paying it on most days.

### Why `Slice00OneForecastPerBatchScenarios` is not in scope

It mocks `IForecastService` and never forecasts. Its cost is a per-test
`TestWebApplicationFactory` + `EnsureDeleted`/`EnsureCreated` + seeders, and one anomalous test at
92.2 s where its six siblings sum to 20 s. That points at the failure path — the unreachable-tracker
branch, or the twice-called `WaitUntilTheQueueStaysIdle` settle loop — not at the simulation. Its
paths are `UpdateQueueService` and `WriteBackRound`, so a forecast lane would not classify it anyway.
Separate investigation.

---

## What is deliberately not gated, and why

`SharedClockBaselineTest.ForecastOfTheBenchmarkPortfolio_MatchesTheRecordedBaselineExactly` exists to
catch a change that moved a forecast date without meaning to. **By definition that is a change whose
paths do not classify as forecast.** Path-gating it would remove exactly the case it was written for,
which is how a green build starts meaning less than it did.

This is where the analogy to the connector lanes stops holding. Those tests are gated because they are
non-hermetic — real network, real credentials, rate limits — and a connector's blast radius is narrow
enough for a regex. A forecast date depends on `TeamMetricsService`, throughput history, the clock and
time zone, blackout days, Feature WIP, the Portfolio and Feature models, and EF eager-loading. A regex
tight enough to be useful under-triggers; one wide enough to be safe is most of the backend.

So one ~12 s test stays on every build as the canary, and the three that only ever break from changes
to the simulation or the draw stream go behind the lane:

- `TheDrawSourceChangedTheDistributionDidNot` — a regression net for one specific historical change,
  the draw-source replacement.
- `TheJointForecastIsAffordableTest` — a hang guard; a hang is introduced by simulation code.
- `TheSameForecastTwice_ProducesTheSameDates` — determinism; breaks only if the draw source or the
  loop changes.

Giving up a third of the available saving to keep the canary is the trade this slice makes on purpose.

---

## Mechanism

### 1. Categories on the fixtures

Add `[Category("Integration")]` **and** `[Category("ForecastIntegration")]`, following the shape
slice-03B established. The umbrella `Integration` tag means the existing base filter
`Category!=Integration` already excludes them, so **the filter builder's base string does not change** —
only a new `Category=ForecastIntegration` term is OR'd in.

| Target | Level |
|---|---|
| `TheDrawSourceChangedTheDistributionDidNotTest` | fixture |
| `TheJointForecastIsAffordableTest` | fixture |
| `SharedClockBaselineTest.TheSameForecastTwice_ProducesTheSameDates` | **method** |

The third is per-method on purpose, so its sibling in the same fixture stays ungated. NUnit composes
class and method categories, and the connector files already rely on the per-method form
(`JiraWriteBackTest`, `AzureDevOpsWriteBackTest`), so this is a shape the repo already runs.

### 2. `classify_forecast` in `Scripts/test-selection/path-classifier.sh`

```
FORECAST_REGEX='^Lighthouse\.Backend/(Lighthouse\.Backend|Lighthouse\.Backend\.Tests)/(Services/(Implementation|Interfaces)/(Forecast|Dependencies)/|Models/(Forecast|Dependencies)/)|^Lighthouse\.Backend/Lighthouse\.Backend\.Tests/API/Integration/DependencyAwareForecasting/'
```

`classify_forecast` must **also return true whenever `classify_shared` does**. `SHARED_REGEX` already
covers `Program.cs`, the solution and the csproj files — a change to the DI graph can move a date as
easily as it can break a connector, and the forecast lane has no reason to be less careful than the
connector lanes about it.

### 3. Plumbing

`ci_changes.yml` gains a `forecast_suite` output alongside the six existing ones; `ci.yml` passes it to
`ci_backend.yml`; the *Compute Test Backend filter* step appends
`[ "${{ inputs.forecast_suite }}" == "true" ] && parts+=("Category=ForecastIntegration")` next to the
connector lines. `force_full` already covers it via step 2.

### 4. Extend `Scripts/test-selection/test-path-classifier.sh`

New scenarios, at minimum: forecast-only diff ⇒ `forecast=true`, connectors false; connector-only diff
⇒ `forecast=false`; a `Program.cs` diff ⇒ `forecast=true` via shared. The classifier has a test suite;
a new lane without new scenarios is a lane nobody checked.

### 5. `Scripts/test-selection/dev-test.sh` and `dev-test.ps1`

Same lane, so a local run behaves like CI.

Nothing to change in `CLAUDE.md`'s local `dotnet test` line — locally, without `--collect`, the whole
namespace is 16 s.

---

## Settle before merging

1. **Is the SonarCloud gate new-code-only?** Tests that do not run produce no coverage. New-code
   gating is fine — touching forecast code fires the lane — but the *overall* coverage figure will
   step down and up between runs. Confirm rather than assume; if the gate reads overall coverage, this
   slice trips it.
2. **There is no nightly full run.** `ci.yml` fires on push, PR and `workflow_dispatch` only; the two
   crons in the repo are the update feed and the demo environment. Connectors get exercised whenever
   someone touches a connector, which is often; the simulation is touched rarely, so a gated forecast
   suite could go weeks unrun with nothing scheduled to catch drift. Either add a weekly full run or
   accept the gap knowingly.
3. **It optimises the steady state, not the working state.** Any push touching forecast code fires the
   lane and pays the full 12m13s — which is exactly when somebody is iterating on forecasting. The
   person this costs most gets no relief from it. That is the argument that keeps the trial-count cut
   (below) alive as a follow-up.
4. **Resolve the coverage-induced failure first.** Running the namespace locally under `--collect`
   turned one of 12 tests red; without coverage all 12 pass. It cannot be the 180 s hang guard — the
   whole run was 119 s — so it is something else, most likely the baseline equality assertion or the
   `P85 > 0` check. If coverage instrumentation can change a forecast result, that matters more than
   any of this, **and gating would hide it.** Identify it by running one fixture, not the namespace.

---

## Verification

- `test-timings-backend.csv` from the first green run after the change contains none of the three
  gated fixtures, and the canary is still there.
- A follow-up push touching `Services/Implementation/Forecast/` shows all four back in the CSV.
- `bash Scripts/test-selection/test-path-classifier.sh` green.
- `dotnet build` zero warnings; the filtered `dotnet test` from `CLAUDE.md` green.

## Rollback

Delete the `Category=ForecastIntegration` line from the filter builder. One line, and every gated test
is back on every build. The category attributes are inert without it.

## Follow-ups, not this slice

- **Trial count.** Give the benchmark fixtures their own `ForecastSimulationLimits(2_000, …)` instead
  of `.Default`, production untouched, and re-record `gold/slice-02-shared-clock-percentiles.json`
  deliberately. Worth ~another 65 s of wall clock and, more to the point, it makes the always-on canary
  and the working state cheap. Needs a decision on re-recording the gold file.
- **Portfolio size for `TheDrawSourceChangedTheDistributionDidNot`** if it is ever brought back
  always-on. Its tolerance is derived from the observed spread of five unpinned runs, so cutting *its*
  trials silently loosens its own bound; cutting the Portfolio cuts cost with no loss of statistical
  power. Right lever, different knob.
- **`[NonParallelizable]` on `TheJointForecastIsAffordableTest`** — 62.5 s with three cores idle. Worth
  ~45 s, but removing it means editing `BackendTestParallelizationGuardTest.AllowedSerialFixtures` in
  the same commit, because that guard asserts in both directions. Only safe once the fixture is cheap;
  today's 51.4 s reading was taken with all four cores, and under contention it would approach the
  180 s guard.
- **`API.Integration.Dependencies`** (211.9 s over 53 tests) and **`Integration.Containers.*`**
  (221.3 s over 63) — the largest structural levers left, both per-test fixture setup. Real refactors
  with flake risk, and some container fixtures exist precisely to test isolated startup. Separate
  Story.
