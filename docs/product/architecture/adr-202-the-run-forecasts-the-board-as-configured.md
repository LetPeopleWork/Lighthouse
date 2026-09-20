# ADR-202: The run forecasts the board as configured. In-flight Features are not seeded into the WIP slots, and where reality disagrees the picture is meant to look odd

- **Status**: Accepted (maintainer, 2026-09-20)
- **Date**: 2026-09-20
- **Feature**: epic-6033-forecasted-start-dates (ADO Epic #6033)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

`ForecastRunPlan` builds its rows in board order — `WorkItemBase.Order`, the order manual sorting
maintains — and `SimulatedRun.WorkOneDayOf` bounds the draw to the first `FeatureWIP` of the ready rows:

```csharp
var howManyItMayWorkOnAtOnce = Math.Min(HowManyFeaturesAtOnce(teamIndex), ready.Length);
var worked = ready[draws.Draw(trial, teamId, day, TheDrawsThatPickAFeature + closed, howManyItMayWorkOnAtOnce)];
```

Every simulated run therefore begins by working ranks 1, 2 and 3 at Feature WIP 3. **The run does not
know which Features are actually in flight.** If the team is really working ranks 1, 4 and 7, the run is
modelling a different board from the one that exists.

This has always been true, and completion forecasts have always carried it. It was invisible: a
completion date absorbs the discrepancy into a distribution and nothing on screen points at it.

[ADR-199](./adr-199-start-day-observed-per-trial-at-two-grains.md) makes it visible.
[ADR-201](./adr-201-observed-start-supersedes-the-forecast-in-the-domain.md) covers most of it — ranks
1, 4 and 7 report their observed start dates, so no forecast for them is shown. What remains is rank 2:
not started, and forecast to start today, while the Feature WIP it would need is spent on rank 7. **The
start forecast is optimistic for a not-started Feature sitting above in-flight work.**

The obvious remedy is to seed the in-flight Features into the WIP slots at day 0, so the run begins from
the board being worked rather than the board as ordered.

## Decision

**Do not seed. The run forecasts the board it was given, and the resulting oddness is a signal to be
documented rather than a defect to be corrected.**

Three points that are part of the decision:

1. **A board that disagrees with what the team is working is the team's own signal.** Either the order
   is not being honoured, or it is not being kept up to date. Both are facts about that team's process,
   and both are things a forecasting tool should surface rather than absorb. Seeding would launder
   precisely the discrepancy worth seeing.

2. **Seeding would move completion forecasts for every existing user.** Starting the run from a
   different set of in-flight Features changes which rows contend for capacity from day 0, which moves
   dates on Features that have no start forecast and no involvement in this Epic. That is a change to
   the core forecast, and it would arrive as a side effect of a feature about start dates. If it is ever
   wanted it needs its own item, its own before-and-after measurement on the dogfood instance, and its
   own release note — the treatment ADR-110 and ADR-156 both received.

3. **It must be documented, and that is not free.** A forecast that is meant to look odd, and is not
   written down anywhere as such, arrives as a bug report. The timeline is where a reader meets it
   first, so the docs obligation lands there (Story #6048, AC-4.9): a Feature nobody has started, drawn
   as starting now while work sits lower in the order, is the timeline reporting the board rather than
   misreading it.

## Alternatives considered

- **Seed the in-flight Features into the WIP slots at day 0.** The honest model of what the team is
  actually doing, and it would fix the completion bias too. **Rejected** — on (1), it hides a real
  signal; on (2), it is a core-forecast change riding on an unrelated Epic. Deferred rather than
  refused: if the optimism is ever measured to matter, this is the door, and it should be opened with
  the ceremony ADR-156 describes.
- **Suppress the start forecast for any Feature below an in-flight Feature in the order.** Narrower than
  seeding, and it blanks exactly the rows that are wrong. **Rejected** — it is the "infer from silence"
  shape [ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md) exists to remove,
  and it would blank a large fraction of a Portfolio's start dates to express a fact about board hygiene
  that the board itself already shows.
- **Warn on the Feature when the board order and the in-flight set disagree.** Keeps the number and
  explains it. **Rejected for now, and the closest call here** — it needs a definition of "disagree"
  that holds across Feature WIP changes, partial starts and multi-team Features, and a wrong definition
  produces a warning nobody can act on. Worth revisiting if the documentation in (3) proves
  insufficient; the evidence for that would be support threads about odd timelines, which is a signal
  that costs nothing to watch for.
- **Order the run by what is in flight rather than by board order.** **Rejected** — it replaces one
  assumption with another, and board order is the one the user controls and can see.

## Consequences

- **Negative, and accepted**: the start forecast for a not-started Feature above in-flight work reads
  earlier than it should. Bounded to that case by ADR-201, which gives every started Feature its
  observed date.
- **Negative, and accepted**: the completion forecast keeps the same bias it has always had. Unchanged
  by this Epic in either direction.
- **The documentation in (3) is load-bearing, not a nicety.** It is the whole mitigation. If it is cut
  from Story #6048 the decision is not implemented, only the code is.
- **Positive**: the run stays a function of configured state. A reader comparing the board with the
  timeline can reason about the difference, which they could not do if the run silently corrected for
  it.
- **Reuse verdict**: no code change. This ADR records a decision **not** to change
  `ForecastRunPlan`/`TrialState`, which is worth a file because the alternative is the first thing a
  reader of ADR-199 will propose.
- Cross-refs [ADR-199](./adr-199-start-day-observed-per-trial-at-two-grains.md) (what made this
  visible), [ADR-201](./adr-201-observed-start-supersedes-the-forecast-in-the-domain.md) (what bounds
  it), [ADR-155](./adr-155-joint-trial-clock-replaces-per-team-simulation.md) and
  [ADR-156](./adr-156-per-trial-max-replaces-product-of-cdfs.md) (the ceremony a core-forecast change
  receives, and why seeding does not get to skip it).
