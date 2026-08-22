# Slice 01 — The forecast jumps over a Feature that cannot start (same team, premium)

**Feature**: epic-5792-dependency-aware-forecasting · **ADO**: Epic #5792 ·
**Stories**: US-05, US-06 · **Estimate**: ~6h
**Reference class**: none in this repository. The Monte Carlo eligibility rule has never been changed
since it was written. Treat every estimate here as less certain than the others.

## Goal

For the first time, a Lighthouse date accounts for the fact that work cannot start yet — and the
Features behind it get the capacity, so their dates move **in**.

## IN scope

- The eligibility rule, at **one** place (KPI-5, OQ-4): `GetSimulationResultsOfFeatureToUpdate`
  filters `Where(x => x.HasWorkRemaining)` today and gains one predicate,
  `Where(x => x.HasWorkRemaining && ready)`. `featuresRemaining` narrows; the `FeatureWIP` window
  slides down to the Features below it. One predicate, one call site.
- **No new per-trial state.** "Has this blocker finished in this trial" is derived from the existing
  remaining counts, which `ResetRemainingItems()` already resets per trial. Readiness must aggregate
  across **all** of a blocker's rows — `InitializeSimulationResults` creates one per (Feature, Team)
  pair, so a Feature worked by three teams is finished only when all three are at zero.
- If every eligible Feature is waiting, the day's throughput is **discarded**, not carried forward.
  The team simply has an idle day.
- **Termination guards, both of them, in this slice** (D7, D8): a cycle member is never treated as
  waiting (Epic #4365's slice 02 already excluded the closing edge); a blocker with no
  `SimulationResult` row, or on a team excluded from the run for having no throughput, is dropped for
  the run and the dependent warned. Without both, `while (GetRemainingItems() > 0)` does not end.
- Cross-team edges are **not** honoured here and carry a warning saying so (AC-5.4) — the warning
  slice 02 exists to delete.
- Cross-Portfolio edges change nothing (AC-5.8).
- The free-tier hint (US-06): unlicensed instances get the count, the dialog and a warning naming what
  is withheld. Forecast values on an unlicensed instance are byte-identical to a dependency-free run.
- **A log warning when an unlicensed instance forecasts a Team that has dependencies** (maintainer,
  2026-08-22). One line per Team per forecast — not per edge, not per Feature — following the same
  aggregation rule the loop and unforecastable-blocker warnings already use. It tells an operator
  reading logs, rather than the UI, that the dates they are looking at ignore real dependencies.
  **Two constraints, both discovered rather than assumed:**
  - `TheOperatorVisibleLines` in Epic #5687's acceptance suite is
    `CapturedLogs.AtOrAbove(LogEventLevel.Information)`, so a `WARN` counts toward the "no more than
    two lines an operator has to read" guarantee. This warning must therefore ride inside the refresh
    round's reporting rather than adding a line beside it, or it breaks a shipped promise exactly when
    it fires.
  - It must not fire on a licensed instance, and it must not fire when a Portfolio ignores its
    dependencies — both of those present an empty honoured set, and a warning about an empty set would
    be noise. That falls out of reading the honoured set rather than the licence, which is the single
    decision point KPI-5 protects.
- A fixed-seed regression asserting no percentile moves when no dependency exists (AC-5.3).

## OUT of scope

- Touching `RunMonteCarloSimulation`'s grouping. That is slice 02's precursor commit, and keeping it
  out is the whole reason this slice exists separately.
- Cross-team honouring (slice 02).
- Jira and Linear ingestion (Epic #4365, slice 03) — this slice inherits whichever connectors that
  epic has already delivered and adds nothing per connector.

## Learning hypothesis

**Disproves** "excluding a waiting Feature redistributes capacity to the Features below it" **if**,
on real dogfood data, the waiting Feature's date moves out and **no** Feature ranked below it moves in
(KPI-2). That would mean the `FeatureWIP` window is not actually sliding — most likely because
`GetSimulationResultsOfFeatureToUpdate` picks randomly *within* the window, so with a wide
`FeatureWIP` the excluded Feature's share is spread thinly rather than handed to a specific
successor, and the effect vanishes into noise at 10,000 trials.

If it fails, D2's whole argument for in-simulation exclusion over a post-hoc date shift collapses, and
the design has to be reconsidered before slice 02 spends a simulation rewrite on it. **This is the
slice that decides whether the epic was worth building.**

**Confirms**, if it holds, that the mechanic is right and slice 02 is generalising something known to
work rather than hoping.

## Verify the premise first (45 min, before touching the loop)

On `:5169` with a premium licence, take a Portfolio where one team owns several Features, note the
current 85% dates, then run the forecast twice by hand with the waiting Feature's remaining work
zeroed and restored. If the Features below it do not move, the effect is not there to find and the
loop change is premature.

## Acceptance criteria

AC-5.1 … AC-5.8 and AC-6.1 … AC-6.4 verbatim from `feature-delta.md`. The three that carry the slice:

- C's 85% date is **earlier** with the dependency honoured than without (AC-5.2) — the one that
  distinguishes this design from a date shift.
- A cycle and a throughput-less blocker both produce a forecast that completes in normal time
  (AC-5.5, AC-5.6).
- Unlicensed forecast values are byte-identical to a dependency-free run (AC-6.2).

## Dependencies

Epic #4365 shipped through at least its slices 01 and 02 — the stored edges, the honour-ability
verdict and the warnings column this slice drives from and writes into. **Real Predecessor links
created in the dogfood ADO project before this slice starts** — a two-Feature loop, a blocker on a
team with no recent throughput, a blocker ranked below its dependent. Lighthouse has no way to author
these (D4), so they are made with `az boards work-item relation add` and are a hard prerequisite
rather than a convenience. Premium licence **and** an unlicensed profile. Mutation testing is
non-negotiable on the eligibility rule: a surviving mutant here is a hang or a wrong date.

## Dogfood moment

Same day: record the before/after 85% dates for every Feature in one Portfolio in this brief. "The
dates moved" is the only evidence that matters and it does not appear in a test run.

## Commit gate

**No commit without the maintainer's explicit approval.** This slice edits the eligibility rule inside
the Monte Carlo loop.

## Learning hypothesis verdict

_Not yet run._
