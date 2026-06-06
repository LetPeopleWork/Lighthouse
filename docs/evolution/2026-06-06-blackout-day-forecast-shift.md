# Evolution — blackout-day-forecast-shift (Epic 4974)

**Shipped:** 2026-06-06 · **Branch:** `main` · **Paradigm:** OOP / ports-and-adapters (C# .NET) · **Wave run:** DISCUSS→DESIGN→DISTILL (prior) → DELIVER (this run)

## Summary

The forward **day↔date working-day translation** layer for forecasts. Turns the Monte Carlo's *days* into a calendar date that steps over configured `BlackoutPeriod`s and never lands on one (days→date, D3), and converts a target date into a working-day count for likelihood / how-many-by-date (date→working-days). The Monte Carlo simulation itself is never touched (D4) — only the date *inputs* and *outputs* are wrapped at the assembly layer.

Epic 4974 was **half-shipped already** (March 2026): `BlackoutPeriod` CRUD, blackout-aware historical-throughput stripping, and chart overlays. This feature delivered the missing forward-shift half (decision D1 locked the shipped half).

## Business context

A passing weekend or company shutdown hit forecasts twice — diluting throughput history *and* shrinking days-remaining — so a P85 could land on a Saturday and a number shared Friday looked worse Monday with nothing changed. Now the forecast *days* hold across known non-working time; only the calendar date moves, and it lands on a day work can actually finish. Stakeholder-trust win (release-notes flagged).

## Architecture (as-built)

- **Two pure statics on `BlackoutDaysExtensions`** (ADR-058, Option A): `ProjectWorkingDays(periods, start, n) → DateTime` and `CountWorkingDays(periods, start, target) → int`. Pure — clock and period list are parameters. No new service (`IWorkingDayProjector` Option C rejected).
- **A1 (locked):** periods threaded as `IReadOnlyList<BlackoutPeriod>` parameters; **Models never gain a repository** (ArchUnitNET guard `BlackoutForecastShiftSeamArchUnitTest`). The DI assembly layer fetches the global set (`GetAll()`, D9) **once per request** and passes it inward.
- **D6 byte-identical as a property of the math:** empty periods ⇒ `ProjectWorkingDays == AddDays`, `CountWorkingDays == calendar diff`. Four no-blackout regression guards.
- **D4 / orthogonality (DDD-6):** historical stripping acts on the past *sample*; forward projection acts on the future *date* — opposite sides of "today", proven non-double-counting by the US-04 compose guard.

## Slices delivered (DES-verified, 7 steps)

| Slice | Story | What shipped | Commit |
|---|---|---|---|
| 01 | US-01 | `ProjectWorkingDays` primitive + When percentile dates step over blackouts | `aea36ea5`, `a51ab4b5` |
| 02 | US-02 | `CountWorkingDays` primitive + by-date likelihood/how-many on working days | `2d205cf0`, `dbe62463` |
| 03 | US-03 | Feature/Delivery dates + likelihood; snapshot recorder; FeatureDto on every read surface | `4b1112dd` |
| 04 | US-04 | Write-back date shift + historical×forward compose guard | `53109065` |
| 05 | UI-2 | Backtest forecast horizon counts working days | `a5137088` |

Quality hardening (post-review): `4951d075` (zero-day write-back boundary test), `2505427d` (mutation kill test + report), `e78e968b` (CI CA1859 fix).

## Key decisions & deviations

- **D8 manual-review gate honoured per slice** — every forecast-result commit was briefed and user-approved before commit (DES recorded `CHECKPOINT_PENDING` → `EXECUTED`).
- **UI-1 (item-creation prediction left calendar-based):** user caught during Slice 02 review that `/forecast/itemprediction`'s history (`GetCreatedItemsForTeam`) is NOT blackout-aware, so a working-day horizon there would under-predict. The initial Slice-02 change to that endpoint was **reverted**; only the (consistent) manual how-many path keeps `CountWorkingDays`. Tracked as a follow-up.
- **UI-2 (backtest horizon, shipped):** same asymmetry class on the backtest surface — its sample was blackout-aware but its horizon was raw calendar days, over-predicting. Fixed in Slice 05 on explicit user request, deliberately touching the D1-locked backtest area (the shipped stripping itself unchanged).
- **Scope additions for consistency:** `FeatureDto` (+ 4 controllers) and `DeliveryMetricSnapshotRecordingHandler` made blackout-aware so feature dates shift everywhere and recorded snapshots match the live read — not in the original DESIGN decomposition, added during DELIVER.
- **`HowManyForecast.TargetDate` dropped from scope:** no forecast-path consumer.

## Quality gates

- **Tests:** full backend suite green (3038 passed). 14 story scenarios + 2 ArchUnit guards + primitive unit tests.
- **Adversarial review:** 0 blockers, no Testing Theater, A1/D4/D6/D8 verified. 1 finding accepted (zero-day write-back test); 2 declined (comment violates house no-comments rule; rename nitpick).
- **Mutation (Stryker, new shift code):** 100% effective (15/15 non-equivalent killed); the start-day-blackout mutant was found and killed; 4 survivors proven equivalent (defensive `<=0` guards). Report: `deliver/mutation/mutation-report.md`.
- **DES integrity:** all 7 steps complete traces.
- **CI:** one cycle lost to the recurring **CA1859** (private test factory typed as `IReadOnlyList<>` instead of `List<>`) — fixed and ledger recurrence bumped to 3. Lesson: don't echo a consumer's interface param type onto a private test factory's return.

## Lessons

1. **Sample/horizon symmetry is a recurring trap.** Three surfaces (item-creation, backtest, and the manual how-many path) each pair a *rate sample* with a *horizon*; whenever one side is blackout-aware the other must be too, or the forecast skews. Two of these (UI-1, UI-2) surfaced only in manual review, not tests — worth a checklist item for future blackout/throughput work.
2. **DES path resolution from a sub-dir cwd** caused a stray `docs` symlink + `.git/info/exclude` workaround in one early dispatch. Fix going forward (applied in later slices): run `des-log-phase` from repo root with an **absolute** `--project-dir`; never cd into the source tree before logging.
3. **Defensive guards generate equivalent mutants.** The `<=0` early returns in both primitives are behaviourally redundant with the loop/subtraction; they produce un-killable mutants. Kept for readability and justified in the report rather than removed to chase the score.

## Links

- ADR: `docs/product/architecture/adr-058-blackout-forecast-date-shift-translation-placement.md`
- Architecture (as-built): `docs/product/architecture/brief.md` → "Application Architecture — blackout-day-forecast-shift" → "DELIVER outcome — as-built"
- Feature workspace (preserved): `docs/feature/blackout-day-forecast-shift/` (feature-delta.md, roadmap.json, slices/, mutation report)
- KPI contract: `docs/product/kpi-contracts.yaml` → `OUT-blackout-forecast-shift`
- Job: `docs/product/jobs.yaml` → `job-forecast-skip-known-nonworking-days`
