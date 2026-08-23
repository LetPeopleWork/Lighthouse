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

**Lifted by the maintainer on 2026-08-23**, before this slice was dispatched: the standing "no commit
without explicit approval" rule caused more trouble than it prevented. The slice runs on the normal
slice-boundary discipline, and the diff is reviewed after it is written rather than before each commit.

## Learning hypothesis verdict

**CONFIRMED, 2026-08-23.** Three Features on one Team, work-in-progress limit two, remaining work
7 / 5 / 9, one pinned starting number, two runs of one build - the second with F-2 recorded as waiting
on F-1. 85% dates in working days:

| Feature | Nothing waiting | F-2 waits on F-1 | Moved |
|---|---|---|---|
| F-1 | 17 | 16 | in 1 |
| F-2 (the one waiting) | 13 | 22 | **out 9** |
| F-3 (below it) | 22 | 20 | **in 2** |

KPI-2 wanted at least one Feature's 85% date to move by three working days or more, and at least one
Feature ranked below a waiting one to move earlier. Both hold, and the second is the one that matters:
the work-in-progress window really does close up over the Feature that was left out, so the capacity
goes to the Features below rather than evaporating. D2's argument for leaving a Feature out of the
running, rather than shifting its date afterwards, stands - a shift would have moved F-2 and left F-1
and F-3 exactly where they were.

The reference class warning at the top of this brief was right about the cost, though: the estimate was
~6h and the eligibility rule itself was the smallest part of it.

## What this slice does not carry, and why

Three acceptance claims turned out to need slice 02's joint trial clock, and are re-tagged in
`acceptance/milestone-1-the-forecast-jumps-over-what-cannot-start.feature` rather than quietly dropped:

- **A Feature worked by several Teams is only finished when all of them are done.** Each Team runs its
  own trials, so one Team's run has no moment at which it can see the other two finish, and reading
  their remaining counts across runs is a race. Slice 01 does not act on such a wait at all: it reports
  it as crossing a Team.
- **A day on which everything is waiting is simply an idle day.** Unreachable here. Every wait this
  slice acts on is one Team's work at both ends and the honoured set has no circles in it, so whatever
  still has work always has something at the front of it waiting for nothing.
- **A dropped wait presents the Feature waiting as an earliest-possible rather than a forecast**
  (SA-15 / ADR-159). A Feature waited on that cannot be forecast is one whose Team has nothing
  measured; since both ends are the same one Team, the Feature waiting is un-forecastable too and never
  gets a date to present as a floor. The row warning and the completing run are delivered.

## Termination, and where it actually comes from

The brief expected two guards. What holds the run up is simpler and stronger than either: the waits the
simulation is handed have no circles in them, because the one decision drops every edge inside one, and
every wait it acts on is one Team's work at both ends - so the Feature waited on has a row in the same
run and a Team with delivery to draw from. A set of waits with no circles always has something at the
front of it waiting for nothing, and the run gets there.

That is a property of the decision, not of the loop. If it is ever broken, the loop would otherwise
spend a background thread counting simulated days forever with nothing anywhere saying why. So an
empty eligible set with work still left ends the run and logs one error naming the Teams and how many
trials were abandoned. It is not how termination is achieved - it is how a mistake in achieving it
becomes visible, which is what SA-5 asked for.
