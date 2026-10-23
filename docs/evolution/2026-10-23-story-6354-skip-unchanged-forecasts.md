# Updates that change nothing skip the forecast — Story 6354

Delivered 2026-10-23 in two slices and 12 roadmap steps, ADO Story #6354, pulled out of Epic #6321 "See and steer
what Lighthouse updates next". Free, on every licence, no setting. Raised by Steve Pereira's logs from a large
self-hosted instance (Portfolios of 400 to 920 Features, 6 to 9 Teams).

On that instance every Team or Portfolio refresh re-forecast every Feature of the Teams involved, and each forecast
took 4 to 8 minutes even when the refresh found nothing new
(`… | mode=Delta | scanned=71 | fetched=0 | duration=16448ms | success=True | forecast=269719ms`). While those
forecasts ran, Kestrel reported heartbeat delays of up to 49 s, so the whole app felt slow.

A background round whose forecast inputs have not changed since the last recorded forecast of that Portfolio now
confirms the existing forecast instead of recomputing it: the summary line says
`forecast=skipped after {N}ms (inputs unchanged)`, Refresh History marks the run "skipped (nothing changed)" and
keeps it out of the duration figures, and the Features' forecasts carry the confirmation time as their "last
updated". Every button a person presses, and the first round after every start of the app, computes in full.

Personas: `platform-operator` (primary: reads the log, the Task Manager and Refresh History, gets the "Lighthouse is
slow" complaint) and `delivery-forecaster` (secondary: needs a current forecast after a real change, and an honest
"last updated"). Job reused: `job-operator-keep-lighthouse-usable-while-it-forecasts` (Epic 6311). No new persona or
journey.

## What shipped

### Slice 01 — Automatic rounds that change nothing skip the forecast; every button still recomputes

| Step | What it changed |
| --- | --- |
| 01-01 | `RefreshLog.Skipped` (database default `false`) and `Portfolio.ForecastInputFingerprint`, one expand-only migration on both providers (`590c7d836`). |
| 01-02 | `ForecastInputFingerprint`: a Portfolio's forecast inputs, as the simulation is handed them, reduce to one stable 128-bit fingerprint, with a guard test naming every member (`4967860fa`). |
| 01-03 | The recorded fingerprint changes only by compare-and-set, a forget token or forget-all (`47595ab4f`). |
| 01-04 | A round's summary line says its forecast was skipped and how long the skip took (`aabc6ca3f`). |
| 01-05 | The forecast port is told the instance day and answers with what the run did (`ForecastRunOutcome`); behaviour-preserving (`831f25372`). |
| 01-06 | A forecast compares its gathered inputs with the recorded ones and simulates only when they differ; the Feature read is narrowed to the Portfolio's Teams (`48a8f57c6`). |
| 01-07 | A quiet background round skips, records a skipped `RefreshLog` row, and still stages write-back and publishes `PortfolioForecastsUpdated` (`f8fd7ddda`). |
| 01-08 | All six buttons (two "Update forecast", Team and Portfolio refresh, both refresh-all) forget the confirmed inputs of the Portfolios they touch before they queue (`16ee43cf8`). |
| 01-09 | Every start of the app forgets every Portfolio's recorded inputs (`ForecastInputsForgottenAtStartSeeder`) (`cf320bd65`). |
| 01-10 | Every change to what the forecast reads makes the next background round compute, pinned per input (`bb5db1299`). |
| 01-11 | Refresh History tells a skipped forecast from a computed one: the Last Run marker, the "Skipped (nothing changed): N of the last M runs" line, durations over computed runs only, `—` when none computed, gaps in the duration line (`2816a636c`). |

### Slice 02 — A skipped forecast says it is confirmed now

| Step | What it changed |
| --- | --- |
| 02-01 | A skipped round stamps every completion and start forecast of the Features it read with the round's time, set-based, dates and histograms untouched (`c952805a3`). |

Around the steps: the approved roadmap (`6566d08e8`), two refactor passes (`be6ca44d1`, `683716b43`), a fix to an
unrelated health-check test that raced host disposal in CI (`c3d1bfe82`), and two merges of origin (`8b0a49726`,
`ead31f93f`). Every step has a complete DES trace (RED, GREEN, COMMIT; 01-05's RED skipped as a behaviour-preserving
refactor).

## The origin merge

Origin moved every production clock read onto the injected clock and banned the ambient one
(`fix-test-clock-midnight-boundary`) while this Story was in flight. The trunk was **merged, not rebased**
(`ead31f93f`), following the standing rule never to rebase autonomously. The merge kept both sides: the computed
forecasts take their creation instant from the injected `TimeProvider`, `ForecastUpdater` stamps `ExecutedAt` and the
confirmed-now stamp from the base class's `CurrentInstant` (the stamp had read `DateTime.UtcNow`, which the new
banned-API analyser rejects), and the tests that no longer compiled against origin's signatures were adapted. The
maintainer approved committing that merge with `--no-verify`.

## Review fix round

After the Opus review of the delivered Story, on branch `fix/story-6354-review`, merged as `4de2a9c62`
(`dadfe5a07..0e36fbcf9`):

- **The run mark is written before saving: a two-phase compare-and-set, chosen by the maintainer** (`dadfe5a07`).
  A computed forecast used to save its new rows first and record its fingerprint only at the very end, after staging
  and publishing, so a failure or crash in between left the old fingerprint next to the new forecasts. Now
  `ForecastService` swaps the stored value, by compare-and-set from what the run read, for a `computing:` mark just
  before it saves, and `ForecastUpdater` swaps that mark for the fresh fingerprint at the end, only from the mark.
  Anything that interrupts in between leaves a value no round can match. The best-effort forget on failure now runs
  only where no computed outcome came back.
- A confirmation never moves a forecast's time back: both stamp statements touch only rows older than the
  confirmation (`aa12d6ae5`).
- The Debug timing line is named for what it measures: `forecast={F}ms | publish={P}ms` (`b120ede0c`).
- Refresh-all reads the Portfolios of every Team in one query instead of one per Team (`be1e635d0`).
- The compare-and-set's null handling is pinned on Postgres too, through the shared container (`4ec63b6ec`).
- Refresh History's All view draws no zero-second bar for an entity whose every run was skipped (`c4ad00732`).
- ADR-256 and the feature delta name the run mark and the renamed timing line (`0e36fbcf9`).

## Decisions

- **The skip rule is a fingerprint of what the simulation is handed**, taken at the simulation's input boundary, not
  of the settings upstream of it. Rejected: "every Team fetched 0", because a Delta refresh with `fetched=0` still
  removes items, recomputes remaining work and moves automatic Feature WIP.
- **Today's instance day is a member**, so the first round of each day always computes and the daily Delivery
  snapshot keeps its cadence.
- **A user-started refresh forgets in the database before it queues**, instead of carrying a "user asked" mark through
  the queue, which Redis admission on another replica would lose. "Update forecast" guarantees a recompute; the four
  refresh buttons recompute unless a forecast of that Portfolio ran in between.
- **A skip still publishes and still stages write-back**, so snapshots, Delivery publishing and their retries keep
  today's cadence; only the simulation is saved.
- **Task Manager unchanged; Refresh History shows a count line** (maintainer, at DISTILL). Durations count computed
  runs only; `—` when none computed; ` · ` between the run's time and the marker.
- **Keep forget-all at every start** (maintainer, confirmed at finalize). The first round of each Portfolio after a
  restart, an upgrade, a rollback or a restore computes, which closes the same-day roll-forward edge without a
  release-note step. It costs one full forecast per Portfolio per start.
- **Skip the mutation re-run on the review-fix files** (maintainer, 2026-10-23). The fixes are covered by their own
  new tests; see `mutation/results.md`.
- Recorded in [ADR-256](../product/architecture/adr-256-an-unchanged-portfolio-forecast-is-confirmed-not-recomputed-unless-a-person-asked.md)
  (now Accepted) and the Story's section of [`brief.md`](../product/architecture/brief.md).

## Quality

- Mutation, run on frozen code at `ead31f93f` before the review fixes: **frontend 96.77 %** (60 of 62); **backend
  82.61 % on the Story's lines** (57 of 69) and **71.37 % on whole files** (367 of 524), which includes the untouched
  code of the five controllers and `UpdateServiceBase` that were mutated whole for evidence on their one new line.
  One kill round closed three survivors. Details and accepted survivors: `docs/feature/story-6354-skip-unchanged-forecasts/mutation/results.md`.
- CI: the run on `ead31f93f` was green; the run on `4de2a9c62` was in flight at finalize.
- No E2E added; the existing Refresh History and Task Manager coverage stays the thin sanity check.
- Usage data: N/A, because a skip is not a user action (DEVOPS).

## Lessons

- **"fetched=0" is not "nothing changed".** A cheap proxy for unchanged inputs would have shipped stale forecasts; the
  fingerprint over what the simulation is handed is the only rule that stays right when a later change adds an input,
  and the guard test makes adding one a decision.
- **Record-last is not enough when the save comes first.** The original design recorded the fingerprint last and
  argued that stored forecasts could only be newer than it. The review found the window between the save and the
  record; a mark taken before the save closes it.
- **Merging origin in place of a rebase kept the Story's history intact** and put every conflict resolution in one
  reviewed commit message.

## Open items for the maintainer

- **Shared-Team other-Portfolio one-round lag.** When a Portfolio refresh changes which Features a shared Team works
  on, that Team's other Portfolios are not re-forecast at once; they catch up on that Team's next round. Pre-existing;
  the fingerprint makes it at most one round stale.
- **Older test gap at `ForecastService.cs` ~386.** Removing the block that copies a Team's throughput-filter status
  (`FilterApplied`, `ExcludedSummary`, `HasSufficientData`) onto its When forecast survives mutation. It predates this
  Story and belongs to the forecast-chip work.
- **Two CI test-time budget warnings (warn-only today):** `Slice01FieldChangeUpdatesItsOwnersTest`, already over budget
  before this Story, and `ConnectionDefaultsUpgradeMigrationTests` at 39.4 s.
- KPI readings K1, K2 and K4 come from production: the reporter's next no-change logs and Refresh History.
- ADO: the Release Notes tag on Story #6354 is the maintainer's call.

## Artifacts

- Delivery history (preserved): `docs/feature/story-6354-skip-unchanged-forecasts/` — `feature-delta.md`, `slices/`,
  `distill/red-classification.md`, `environments.yaml`, `deliver/roadmap.json`, `mutation/`.
- Decision: `docs/product/architecture/adr-256-an-unchanged-portfolio-forecast-is-confirmed-not-recomputed-unless-a-person-asked.md`.
- KPI contracts: `docs/product/kpi-contracts.yaml` (`OUT-6354-*`).
- User docs: `docs/settings/systeminfo.md`, `docs/settings/taskmanager.md`, `docs/settings/configuration.md`.
- Release notes draft: `docs/feature/story-6354-skip-unchanged-forecasts/release-notes-draft.md`.
