# ADR-201: A started Feature reports the day it started, and that rule lives on Feature — not in the DTO that first needed it

- **Status**: Accepted
- **Date**: 2026-09-20
- **Feature**: epic-6033-forecasted-start-dates (ADO Epic #6033, Stories #6045 and #6047)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

[ADR-199](./adr-199-start-day-observed-per-trial-at-two-grains.md) records a start day for every Feature
with work remaining — including Features that started weeks ago, because the simulation restarts every
item pool from today and knows nothing about what is in flight
([ADR-202](./adr-202-the-run-forecasts-the-board-as-configured.md)). For a Feature already in a Doing
state, that forecast is a prediction of an event that has demonstrably happened.

The observed date already exists. `WorkItemBase` carries `StateCategory` and `DateTime? StartedDate`,
both inherited by `Feature`.

The maintainer's decision at DISCUSS (D5, 2026-09-20) was that the forecast still runs and is still
stored, and what changes is only what is *read back*: a Feature in a Doing state reports its
`StartedDate`, marked as observed. No forecasting logic changes.

**Where "read back" happens is the open question, and it is not one place.** Two consumers need the
identical rule:

- `FeatureDto` — the table column and the timeline both render from the portfolio response.
- `WriteBackTriggerService.ResolveForecastValue` — Story #6047's start resolver, which must write the
  observed date to the work tracking system so a Jira Plan bar begins where work began (AC-3.3).

The write-back path does not go through `FeatureDto`. It reads `feature.Forecast` directly and projects
it through `ProjectWorkingDays`. So putting the override in the DTO would leave the resolver to
re-implement it.

ADR-156 names that failure mode when rejecting its own hybrid alternative:

> it keeps two definitions of one number alive, which is exactly the "two independent implementations of
> the same verdict" failure mode KPI-5 exists to catch

## Decision

**The rule is a domain concern on `Feature`. Both consumers read it; neither owns it.**

1. **`Feature` answers "when did this start, and is that observed or forecast?"** — one member,
   returning the observed `StartedDate` when `StateCategory == StateCategories.Doing`, and otherwise
   deferring to the stored Feature-grain start forecast. The provenance travels with the answer; a
   caller cannot receive a date without being told which kind it is.

2. **The distinction is carried explicitly, not inferred.** A consumer must not have to deduce
   "observed" from the absence of percentiles, for the same reason
   [ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md) refused to let callers
   infer *unknown* from an empty histogram — inference-from-silence is what produced the `return 100`
   trap. The DTO surfaces the provenance so the table can mark it and the timeline can start the bar
   there.

3. **An observed start carries no percentiles.** Not four copies of the same date: there is nothing left
   to be uncertain about, and four identical dates invite a reader to believe a distribution exists.

4. **Done Features are covered by the same member**, and the write-back resolver keeps its existing
   refusal to write anything for a Feature in a Done state — unchanged from the completion sources.

5. **`StateCategory`, not `StartedDate` alone, is the predicate.** A Feature can carry a `StartedDate`
   from the tracker while sitting in a state Lighthouse maps to To Do; the state mapping is what the
   instance's own configuration says has started, and that is the answer the rest of the product uses.

## Alternatives considered

- **Put the override in `FeatureDto`.** Smallest diff, and the DTO is where the first consumer lives.
  **Rejected** — Story #6047's resolver does not pass through it, so the rule would be written twice and
  could drift. A Plan bar starting on a forecast date while the table beside it shows the observed one
  is a defect nobody would find quickly.
- **Put it in the write-back resolver and let the DTO read a flag.** Same objection, mirrored.
- **Apply the override at write time** — do not store a start forecast for a Feature in a Doing state.
  Tempting: less stored data, no read-time branch. **Rejected** — it makes the simulation's output
  depend on current board state, so a Feature that starts between two refreshes would have its stored
  history change meaning rather than its presentation. It also forecloses the deferred over-time view
  (DISCUSS D12), which wants the forecast series intact regardless of what happened since.
- **Present the forecast for started Features too, labelled.** Consistent, one code path. **Rejected**
  at DISCUSS by the maintainer: the column would say "today" for work that began in July.
- **Blank the column for in-progress Features.** **Rejected** — the timeline would then have to source
  the bar's left edge separately, which is the same rule in a second place.

## Consequences

- **Positive**: one rule, two consumers, and the table and the Jira Plan bar cannot disagree about when
  a Feature started.
- **Positive**: the write-back resolver stays a projection. It gains a branch for which member to read,
  not a copy of the decision.
- **The stored start forecast for a started Feature is never rendered.** That is deliberate — it is
  still computed, still stored, and still available to the deferred snapshot, which wants the series
  whether or not the current presentation shows it.
- **A Feature whose state mapping is wrong reports the wrong provenance.** An unmapped or
  mis-categorised state means Lighthouse does not think the Feature has started, and it will show a
  forecast for work in flight. That is the existing consequence of state mapping everywhere in the
  product, not a new one here, and it is not worked around.
- **Reuse verdict**: `Feature` → **EXTEND** (one member; `StateCategory` and `StartedDate` are
  inherited and unchanged). `WorkItemBase` → **REUSED AS IS**. `FeatureDto` → **EXTEND** (reads the
  member, surfaces provenance). `WriteBackTriggerService.ResolveForecastValue` → **EXTEND** (reads the
  same member; `ProjectWorkingDays` and the blackout handling are unchanged per
  [ADR-058](./adr-058-blackout-forecast-date-shift-translation-placement.md)). **No new type.**
- Cross-refs [ADR-199](./adr-199-start-day-observed-per-trial-at-two-grains.md) (the forecast this
  supersedes at read time),
  [ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md) (the carry-it-explicitly
  principle, and the `CanBeForecast` gate that already handles the un-forecastable case so this ADR
  does not),
  [ADR-159](./adr-159-un-forecastable-blocker-drops-and-the-date-reads-as-a-floor.md) — considered and
  **deliberately not extended here**. Its "directionally known, present it as a bound" rule applies
  where the un-forecastable team is only a *start constraint* on a Feature whose own work is
  forecastable. When a contributing team owns work *inside* the Feature, ADR-159 itself defers to
  ADR-112, and that is our case.
  [ADR-058](./adr-058-blackout-forecast-date-shift-translation-placement.md) (day-to-date translation,
  unchanged and applied to a start exactly as to a completion).
