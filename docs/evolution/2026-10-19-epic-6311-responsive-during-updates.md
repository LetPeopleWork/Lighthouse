# Keep Lighthouse responsive during updates — Epic 6311

Delivered 2026-10-18 to 2026-10-19 in five slices, across three ADO children that each ship on their own:
Bug #6308, Story #6309 and Story #6322. Free, on every licence, no setting. Raised by one self-hosted (Docker)
user's incident report.

While one Portfolio forecast ran, every page hung, every Team refresh sat "Queued behind" it, and Stop left the row
reading "Stopping…" for more than ten minutes, until the operator restarted the container. The code explained all
three: the Monte Carlo simulation took every core with an unbounded `Parallel.For`, no cancellation token reached it,
and a single run could simulate up to 100,000 days (about 274 years) for a low-throughput Team with a long backlog.
Runs that never finished were silently dropped from the distribution, so a percentile was computed from the finished
runs only and read earlier than the simulation supported.

A forecast now runs on at most half the cores, Stop ends it between runs and keeps the previous forecast, a run
covers at most five years, a percentile that fewer runs reached than it needs reads "More than 5 years", and a System
Administrator can clear everything running or queued with **Cancel all**.

Personas: `platform-operator` (primary, slices 01, 02 and 05: runs the instance and was the one restarting it) and
`delivery-forecaster` (slices 03 and 04: reads and shares Feature and Delivery dates). No new persona.

At DISCUSS the maintainer kept the scope to these four items. The rest of the incident — refreshes ahead of
forecasts and queue lanes, showing upcoming and held updates, pausing automatic updates, skipping forecasts whose
inputs did not change, and a toast when a background update fails — moved to Epic #6321 "See and steer what
Lighthouse updates next".

## What shipped

### Bug #6308 — The forecast leaves cores free, and Stop stops it (slices 01, 02)

| Step | What it changed |
| --- | --- |
| 01-01 | A simulation runs on at most half the cores (minimum 1), whoever asked for it: background Portfolio forecasts, manual forecasts, reality checks and backtests share one method (`2858da9f3`). |
| 01-02 | The startup banner says how many cores a forecast may use, read from the registered limits (`c8ecfbfb0`, `b4a918d3b`, `33d583dbd`). |
| 02-01 | The update's cancellation token reaches the simulation and is checked between runs, so Stop ends a running forecast and the previous forecast stays, in Lighthouse and in the tracker. The row reads Cancelled, and only a real stop counts as one; the browser is told how the forecast ended (`49c099fa9`, `0507f0a29`, `820b8fc7f`). |

### Story #6309 — Honest counting and a five-year horizon (slices 03, 04; released together)

| Step | What it changed |
| --- | --- |
| 03-01 … 03-03 | `TotalTrials` counts every simulated run. A percentile is beyond the horizon exactly when fewer runs finished than it needs, and a likelihood counts an unfinished run as late. Start forecasts, dependency circles and multi-Team aggregates follow the same rule (`c18b58072`, `cb845e4b1`, `15ba9174e`). |
| 03-04 … 03-05 | Delivery percentiles, snapshots and pinned closure records carry `beyondHorizon` and the `horizonYears` they were forecast with, additively. Write-back writes nothing for a beyond percentile, the blackout window reads it honestly, and the Jira Release block says "85%: more than 5 years" (`2522dd683`, `2974d42c2`). |
| 03-06 … 03-09 | The web model keeps both fields. The forecast info, forecasted-start column, manual forecaster, Delivery chips and export table read "More than 5 years"; the timeline draws no bar and the predictability chart no point for such a percentile, and a Team whose percentile lies beyond gets no timeline lane (`5774305f5` … `6c93264b6`). |
| 04-01 … 04-02 | One run covers at most the five-year horizon (1,827 simulated days, one constant the copy also reads). A run that reaches it is counted, and the count is logged at Debug (`cfc32dac6`, `0233c10ee`, `e232e7ad7`, `77a2d3cf4`, `175e841f2`). |

Slices 01–04 run `2858da9f3..c792e7dee`, including the two mutation-gate commits (`f9b2008ff`, `c792e7dee`).

### Story #6322 — Cancel all (slice 05)

| Step | What it changed |
| --- | --- |
| 05-01 | `POST update/tasks/cancel` (latest and v1, System Administrator, idempotent) stops every queued and running refresh at once and leaves removals to finish (`e31e04efd`, walking skeleton `ca06e5a1a`). |
| 05-02 | Cancel all drops parked follow-ups and lets held forecasts go, and stops work admitted while the click is handled: the cancellation is admitted before the status store's (`3cc9497c9`, `5c435a99b`). |
| 05-03 | Cancel all clicked on one replica stops the refresh running on another, over its own Redis channel, with one real-Redis test and nothing more (`7f79d8b11`). |
| 05-04 | A "Cancel all" text button at the right end of the Task Manager's Activity heading, System Administrators only, hidden when nothing could be cancelled, no confirmation; "Cancelling…" while in flight and "Could not cancel. Try again." on failure (`e82326d99`). |

Slice 05 runs `e31e04efd..b6d2c25b5`, with the review fixes and mutation commits listed below; the finalize commits
follow up to `53a3c0fbd`. No migration in any slice, no new permission, no licence gate.

## Decisions

### Maintainer decisions (feature-delta.md)

- **NMD-1 (c):** "more than 5 years" travels as an additive flag beside a kept date, plus a Lighthouse-Clients minor
  that renders it. **NMD-2 (a):** write-back writes nothing for a beyond percentile, as for "no answer".
- **NMD-3 (a):** `beyondHorizon` and `horizonYears` on `WhenForecastDto` and `WhenDistributionPointDto`; clients key on
  whether the field is present. **NMD-4 (a):** the Jira Release block reads "85%: more than 5 years".
- **NMD-5:** slices 03 and 04 are built in order and released together. **NMD-6:** no early stop for the whole
  forecast now; the per-run cap is the kill switch.
- **NMD-7:** no usage-data event for Cancel all; KPI-6 dropped as unmeasured. **NMD-8:** the docs name
  `DOTNET_PROCESSOR_COUNT` in one line and say nothing about old replicas during a rolling upgrade.
- **NMD-9:** "More than 5 years", the 5 read from `horizonYears`, with the reason in the tooltip; distinct from
  "Cannot forecast". **NMD-10:** no timeline bar, and a break in the predictability chart's line.
- **NMD-11:** the Cancel all control, copy and in-flight and failure looks as sketched at DISTILL.
  **NMD-12:** `horizonYears` also travels on Delivery history points, in the existing JSON column.
- **NMD-13:** a run ended by a dependency circle counts as beyond the horizon; the error naming the circle stays.
  **NMD-14:** the core limit is per simulation, so a manual forecast beside a background one gets its own half.
- **NMD-15:** Cancel all is delivered at most once; Redis multi-replica is a proof of concept and gets the minimum.
  **NMD-16:** old binaries after a rollback read a beyond percentile as today until the next forecast rewrites it.

### ADRs (`docs/product/architecture/`, all Accepted 2026-10-18 at DESIGN)

- **ADR-248:** a forecast simulation runs on at most half the cores, whoever asked for it.
- **ADR-249:** a cancelled forecast stops between runs on a token its updater hands down.
- **ADR-250:** `TotalTrials` counts every simulated run, and a run that does not finish is beyond the horizon.
- **ADR-251:** the horizon is five years of simulated days, held in one constant the copy also reads.
- **ADR-252:** a beyond-the-horizon percentile travels as an additive flag beside a kept date.
- **ADR-253:** Cancel all is one broadcast each replica applies to its own work, follow-ups and holds.
- **ADR-254:** a published release forecast says "more than five years" instead of a date.

### DELIVER, slice 05

- **05-01's walking skeleton failed on a test-stub race, not on the product.** The tracker stub returned normally
  after its token was cancelled, so the refresh finished instead of stopping. The orchestrator fixed the stub to
  honour a stop made while it answers (`ca06e5a1a`).
- **05-02's two pre-authored acceptance tests could not pass as written.** One asserted InProgress notifications the
  queue never sends; the other held a forecast behind running work, but holds only wait on queued work. The
  orchestrator redesigned both to assert what the queue can actually show, and proved each fails on the code before
  05-02 (`5c435a99b`).
- **Cancel all walks this instance's own cancellation sources, not the shared status store.** Each replica stops
  what it runs; the broadcast carries the click to the others.
- **Removal rows no longer show a per-row Stop.** The instance refuses it, so offering it was a lie; folded into the
  Epic at the maintainer's request (`ecef800f2`).
- **Slice review: APPROVE WITH FIXES.** A dropped hold's round lost its write-back; fixed, also for abandoned and
  never-started work (`8f1051ce9`). An admission race could leave a run nobody could stop; a per-instance lock closes
  it (`4e7be6658`). A stale refusal message came back over rows the click never touched (`7b882a15b`). The Cancel
  all tests were tightened to tell a wrong guard and a finished caller from the real thing (`3666adbcf`).
- **Final review: APPROVE WITH FIXES.** A let-go round's write-back now holds off the database maintenance gate, so a
  restore or clear cannot start under it (`a3cd499b2`). A status store that throws leaves no cancelled source behind
  for the next refresh of that key (`cf0622cdf`). A refusal after the Task Manager was closed no longer greets its
  next opening (`e28d25d3a`). **Behaviour change:** at shutdown, a round write-back still owed is abandoned with a
  Warning rather than sent.

### DEVOPS

- **Usage-data event: none** (NMD-7). Slices 01, 02 and 04 have no user action to count, and KPI-4 is verified by
  tests. `UsageDataEventName` and `docs/settings/usagedata.md` are unchanged.
- **Rollback is the previous release.** No migration; the wire change is additive. A rollback is not clean for
  forecasts (NMD-16) and is healed by a forecast update for every Portfolio. The maintainer chose to leave the
  rollback note out of this Epic's finalize; it is a release-time item.

## Quality gates

- **Mutation** (`mutation/results.md`, gate 80 %):

  | Scope | Backend | Frontend |
  | --- | --- | --- |
  | Slices 01–04 (#6308, #6309) | 80.21 % → **87.29 %** whole scope after one kill round; 16 → 1 survivor on changed lines (a log message's wording) | 91.58 % → **98.95 %** verified (94 / 95) |
  | Slice 05 (#6322) | **93.83 %** on changed lines (65.43 % before the kill round); **74.19 %** whole-file | 82.86 % → **92.86 %** (65 / 70) |

  Slice 05's whole-file backend score misses the gate. Of its 80 survivors, 34 are log statements or their wording
  and 18 are pre-existing `UpdateController` code reached only by acceptance suites the filter leaves out; without
  the log-text mutants it is 83.3 %. Suppressing log-text mutants needs a production comment, left to the maintainer.
- **Known gaps left:** `UpdateQueueService.cs:399`, the early `return` after a leave that was not the last. Without
  it, two holds on one round, the first released with nothing to queue, could send the round's write-back in two
  conversations instead of one. A test needs that two-hold arrangement. Separately, the existing
  `UpdateQueueServiceTests.EnqueueUpdate_CancelledBeforeItRuns_StillLeavesTheRoundItJoined` cannot fail: it reads an
  `AsyncLocal` round from the test's own flow, where it is always null. The path is still killed through
  `RoundWriteBackDuringDatabaseMaintenanceTest`.
- **DES.** The DES audit log in `deliver/` covers slice 05 only: all four steps with RED, GREEN and COMMIT. The log for
  slices 01–04 was not present when slice 05 started, so git history is the record for those steps.

## Manual checks

- **Maintainer, 2026-10-19, local instance.** Pages keep answering while a forecast runs, Stop ends a forecast,
  Cancel all clears the list, and removal rows behave. "More than 5 years" could not be checked locally (no
  low-throughput Team), so it rests on the automated tests. The Redis multi-replica path was skipped (NMD-15).
- **Production-data readings not recorded.** AC-01.5, AC-02.7, AC-03.8, AC-04.4 and AC-05.7 on the restored backup
  are owed to the maintainer. The KPI-3 baseline is 1,766 ms (New Portfolio, cold start, read 2026-10-18); warm
  repeats read 987 ms and 357 ms, so the backup's run-to-run spread is wider than the 50 % target.

## Issues encountered

- **Acceptance tests written ahead of the code can encode wrong assumptions about it.** Two of slice 05's could not
  pass for any correct implementation. They were rewritten and proven to fail before 05-02, not loosened.
- **Cancellation paths leak write-back.** Both reviews found the same family: a round let go by Cancel all, by
  shutdown or by work that never started still owes its tracker writes, and must still free the database gate.
- **A Stryker.NET score over whole files measures the filter as much as the code.** Without the one acceptance suite
  that pins Cancel all's drop and release paths, every mutant on them read as a survivor.
- **CI flake:** the Detect Changes job calls the GitHub API with `retries: 0`; a 502 failed run 39277702189 on its
  first attempt.

## Finalize checklist

| Item | Answer |
| --- | --- |
| User docs | Done (`35ea835b6`): `docs/settings/taskmanager.md` (Cancel all, Stop ends a forecast), `docs/concepts/howlighthouseforecasts.md` (half the cores, `DOTNET_PROCESSOR_COUNT`, the five-year horizon, "More than 5 years", write-back), and the Portfolio, Team, settings and connection pages. |
| Screenshots | `docs/assets/settings/taskmanager.png` shows Cancel all (`6ed2e349e`). The shot routes the Activity and Recent problems rows through the page, so it is stable between runs (`53a3c0fbd`). "More than 5 years" is described in text, with no screenshot (maintainer, 2026-10-19). |
| `ARCHITECTURE.md` and `brief.md` | Updated (`e8180133b`): the bounded simulation, the horizon marker and the cancel-all channel. |
| Release notes | Deferred to release: the "Release Notes" tag on #6308, #6309 and #6322 and the copy. |
| Lighthouse-Clients | Deferred to release: a minor that renders `beyondHorizon` / `horizonYears`, keyed on presence; in progress. |
| Rollback note | Not included (maintainer decision); the release notes carry the forecast-update step. |
| Usage-data page | N/A, because no event (NMD-7). |
| RBAC | No new permission. Cancel all is `SystemAdmin`, as the Task Manager and per-row Stop. |
| Website | N/A, because this is a fix, not a new capability. |
| Migrations | None. |
| Epic #6298 (PDF export) | Not yet noted in its workspace that the PDF must render a beyond percentile; owed. |

## Follow-ups worth raising

- A two-hold test for `UpdateQueueService.cs:399`, and a rewrite of the vacuous
  `EnqueueUpdate_CancelledBeforeItRuns_StillLeavesTheRoundItJoined` to capture the round inside the execution.
- Whether to mark log-text mutants in `UpdateQueueService` with the project's `Stryker disable` comment.
- `retries` on the Detect Changes job's GitHub API call.
- The production-data readings above, and NMD-6's early-stop design if AC-04.4 misses.

## Migrated out of the workspace

Nothing was moved. The lasting artifacts were written under `docs/product/` from DESIGN on:

- ADR-248 to ADR-254 in `docs/product/architecture/`, all Accepted, matching the shipped code.
- The Epic's section in `docs/product/architecture/brief.md` ("Application Architecture —
  epic-6311-responsive-during-updates").
- The journey `docs/product/journeys/epic-6311-responsive-during-updates.yaml` and the jobs in `docs/product/jobs.yaml`.

The workspace `docs/feature/epic-6311-responsive-during-updates/` stays in place as delivery history: the feature
delta, the slice briefs, the roadmap, the wrap-up plan and the mutation results and configs. Nothing was deleted.

## ADO

Bug #6308, Story #6309 and Story #6322 under Epic #6311. The Epic's state is the maintainer's to move.

Commits: `2858da9f3..53a3c0fbd` (with unrelated trunk commits between `c792e7dee` and `e31e04efd`), plus this record.
