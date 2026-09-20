# ADR-199: A Feature's forecasted start day is observed inside the trial, at Feature grain and at team grain — not derived from the preceding Feature, and not derived from the marginals

- **Status**: Accepted
- **Date**: 2026-09-20
- **Feature**: epic-6033-forecasted-start-dates (ADO Epic #6033, Story #6045)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

Epic #6033 asks for a forecasted start date per Feature at P50/P70/P85/P95, so that a Jira Plan bar can
be populated at both ends from measured flow rather than from typed dates. Completion already ships
through write-back (epic-5565).

The idea-board entry proposed deriving a Feature's start from the forecasted completion date of the
**preceding** Feature, and correctly noted that this needs a notion of sequence and does not answer what
"preceding" means with zero or several predecessors, or across parallel teams.

**The run already knows.** `SimulatedRun.WorkOneDayOf` holds the row it is working in `worked` on every
delivered item, and `completions.RecordThat(worked, day)` fires only when that row reaches zero — the day
it *finished*. The day it *started* is the same variable, one branch earlier. The number is computed ten
thousand times per forecast and discarded.

The derivation is also only well defined for a single linear chain at Feature WIP 1. Recording the pull
day is defined at every Feature WIP, every branching order, every number of teams and every dependency
shape, because [ADR-155](./adr-155-joint-trial-clock-replaces-per-team-simulation.md)'s shared clock and
epic-5792's readiness gating already model all of them.

**The second question is the grain.** A Feature worked by several teams starts when the *first* of them
starts. The mirror of that for completion — the last team — is
[ADR-110](./adr-110-multi-team-forecast-joint-probability.md)'s product of CDFs, computed from the
per-team marginals after the run. The start mirror exists and would work: `1 - Π(1 - Fᵢ(d))`.

[ADR-156](./adr-156-per-trial-max-replaces-product-of-cdfs.md) is why it is not used. It proposes
replacing ADR-110's product with a per-trial observation, precisely because a dependency breaks the
independence both formulas assume, and it is **Deferred rather than refused** — for reasons that do not
apply to a number that does not exist yet:

> **One change to forecasting at a time** (maintainer). This epic already rewrites the simulation loop;
> also replacing the aggregation would mean two changes to the core forecast in one release.

ADR-156 also retires ADR-110's original cost objection to per-trial recording:

> Both stated costs are now obsolete. The hot loop is being rewritten anyway, and trial-level storage is
> not needed: with a shared clock, the maximum over a Feature's rows is a running count, not a retained
> array.

A minimum is a running value for the same reason.

## Decision

**Record the start day inside the trial, at two grains, and derive nothing.**

1. **Per `(Feature, Team)` row** — the first day on which `worked` names that row. Same grain as the
   per-team completion histograms, recorded in the same pass and merged by the same fold.

2. **Per Feature** — the earliest across that Feature's participating rows, within the same simulated
   run. Not `1 - Π(1 - Fᵢ(d))` over the marginals. The formula is exact only under independence, which
   is exactly what epic-5792 exists to model away: within a trial a Feature's contributing teams are held
   back by the same blocker completion and released together. Observing the minimum reads the actual
   joint realisation and assumes nothing.

3. **"Started" means pulled, not eligible.** With Feature WIP 3 and five ready rows the draw ranges over
   the top three; all three are eligible and on any given day only some are drawn. A Feature nobody has
   worked has not started. So the marker is set where `worked` is assigned, whether or not that item
   finishes the row.

4. **Completion is untouched.** ADR-110 stands, `JointCompletionDistribution` stays, ADR-156 stays
   deferred. This ADR adds a number that did not exist; it moves none that did. That is the whole reason
   it does not violate "one change to forecasting at a time" — a reader comparing the two should see that
   the constraint was honoured, not sidestepped.

**Cost.** One `int[]` per worker indexed by row and one indexed by Feature, each initialised to a
not-started sentinel at `StartAgain()` and written at most once per entity per trial, folded into
histograms by the existing merge. No lock, no trial-level retention, no change to
`limits.Trials`. Asserted rather than assumed: AC-1.8 measures the same forecast before and after and
allows 110 % of the `main` wall-clock.

## Alternatives considered

- **Derive from the preceding Feature's completion date** (the idea-board proposal). **Rejected.** Only
  well defined for a single linear chain at Feature WIP 1, which is the case that needs it least. It
  would reconstruct, less accurately and with a new notion of "preceding" to define, a number the run
  already computes exactly.
- **Derive the Feature grain from the per-team marginals** with `1 - Π(1 - Fᵢ(d))`, mirroring ADR-110.
  Cheapest, symmetric with completion, and needs no change to the hot loop. **Rejected** — it inherits
  an independence assumption that epic-5792 made false, to save one comparison per pull. ADR-156 already
  documents that bias as real; there is no reason to take on a known-wrong assumption for a new number
  when the exact answer is cheaper than the approximation.
- **Record only the Feature grain.** Half the storage, and it is what the table and the summary bar
  render. **Rejected** — the per-team rows cost nothing extra (the marker is per row before it is folded
  per Feature), and slice 06's sub-lanes need them. Recording one and reconstructing the other later
  would mean a second pass over the hot loop for something available free in the first.
- **Un-defer ADR-156 in the same change**, so both start and completion are observed per trial and the
  two grains are produced by one mechanic. Genuinely attractive, and it is where the architecture points.
  **Rejected** — it moves completion dates for every multi-team Feature, which AC-1.5 forbids and which
  ADR-156 itself deferred for the "one change at a time" reason. See Consequences: this ADR makes that
  change materially cheaper without making it.
- **Record eligibility rather than the pull.** Would make the start earlier and arguably models
  "available to start". **Rejected** — it reports work as started that nobody has touched, which is the
  opposite of what the number is for.

## Consequences

- **Positive**: the start date is an observation of the model rather than an assumption layered on it —
  the same property ADR-156 wants for completion, obtained on a number with no legacy to preserve.
- **Positive, and worth naming: this builds the machinery ADR-156 needs.** Its mechanic is a per-Feature
  outstanding-row count decremented to zero; this one is a per-Feature marker set on first touch. Same
  array lifecycle, same recorder, same fold, mirrored. Un-deferring ADR-156 after this lands is a
  smaller change than it is today. **This ADR does not un-defer it** and must not be read as doing so.
- **Single-team Features have an identical Feature-grain and team-grain start**, by construction — the
  minimum over one row is that row's day. This bounds the surface where the two can disagree to the
  multi-team minority, exactly as ADR-110's SPIKE-00 found for completion.
- **The two grains can legitimately disagree at a percentile.** The Feature-level P85 is not the earliest
  per-team P85, and on a dependent pair it will not be — the Feature value is taken inside the trial, the
  team values are separate marginals. This is correct and is why slice 06's sub-lanes are severable: if
  the presentation cannot make the disagreement read as correct, the sub-lanes do not ship.
- **Nothing is recorded for a Feature whose contributing team is absent from the run.** That team has no
  throughput, so `ForecastRunPlan.For` admits no row for it, and
  [ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md)'s unknown state already
  governs the whole Feature via `Feature.CanBeForecast`. No new gate is written. See ADR-201 for why
  ADR-159's competing "present it as a bound" precedent does not apply here.
- **Reuse verdict**: `TrialState` → **EXTEND** (one marker array, cleared in `StartAgain`).
  `TrialCompletions` → **EXTEND** (parallel arrays and one more fold; consider renaming, since it no
  longer records only completions). `SimulatedRun.WorkOneDayOf` → **EXTEND** (one call where `worked` is
  assigned). `ForecastRunPlan` → **EXTEND** (hoist the row→Feature grouping it already builds privately
  inside `WhatEachRowWaitsFor`). `ForecastService` → **EXTEND** (a second fold beside
  `RecordTheDaysEachRowFinishedOn`). **No new type in the simulation.**
- Cross-refs [ADR-156](./adr-156-per-trial-max-replaces-product-of-cdfs.md) (the deferred completion
  equivalent, and the costing this relies on),
  [ADR-110](./adr-110-multi-team-forecast-joint-probability.md) (the aggregation this deliberately does
  not touch), [ADR-155](./adr-155-joint-trial-clock-replaces-per-team-simulation.md) (the shared clock
  that makes a per-trial minimum well defined),
  [ADR-154](./adr-154-addressable-draw-streams-for-the-feature-forecast.md) (addressable draws —
  unchanged, and what keeps the added recording deterministic),
  [ADR-200](./adr-200-start-forecasts-live-in-their-own-collection.md) (where the result is stored),
  [ADR-202](./adr-202-the-run-forecasts-the-board-as-configured.md) (why in-flight Features are not
  seeded into the WIP slots).
