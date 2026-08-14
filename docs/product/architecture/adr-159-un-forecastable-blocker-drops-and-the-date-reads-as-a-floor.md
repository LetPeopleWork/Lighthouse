# ADR-159: A blocker that cannot be simulated drops the edge, and the dependent's date is presented as an earliest-possible rather than a forecast

- **Status**: **Accepted** (maintainer, 2026-08-14) — drop the dependency for that run and warn
  clearly. The warnings column carries it today; the planned task-manager surface will carry it too.
  Ratified against the ADR-112 objection recorded below
  DESIGN flagged this as the call in the epic most likely to be overruled and wrote it so that
  overruling it would be a one-place change. It was not overruled.
- **Date**: 2026-08-14
- **Feature**: epic-4365-dependencies (ADO Epic #4365, slices 02, 03)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

A dependency is honoured by excluding the dependent from the eligible set until its blocker has no
remaining work *in this trial*. If the blocker can never reach zero, the dependent is never eligible
and `while (remaining > 0)` never terminates. Three cases:

- The blocker is **already finished**. It has no simulation row at all
  (`InitializeSimulationResults` only admits `RemainingWorkItems > 0`), so it imposes no constraint and
  costs nothing. Free, and no warning is owed.
- The blocker's **team has no throughput**. `RunMonteCarloSimulation` excludes such teams from the run
  entirely, so the row is never consumed.
- The blocker is **absent from this run's Feature set** for any other reason.

DISCUSS D8 drops the edge in the last two and warns the dependent. That is what stops the epic
shipping a hang — and the hang would appear only on instances that have a team with no recent
throughput, which is most of them eventually.

But [ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md) ruled the opposite way
one level down. When a *contributing* team cannot be forecast, the Feature reports **unknown**, because
"a forecast that silently ignores a team that must finish is exactly the class of dishonesty this epic
exists to remove". The dependent's date under D8 also assumes work that provably cannot be simulated.
It is fair to ask whether D8 is the same dishonesty, one level out.

## Decision

**Keep the drop — the edge is not honoured — but stop calling the result a forecast. The dependent's
dates are presented as an earliest-possible, and the row points at the blocker, which already says it
cannot be forecast.**

Four points that are part of the decision:

1. **The drop is a typed verdict, not a warning string.** The edge carries
   `NotHonoured(BlockerCannotBeForecast)` from the one policy that also drives the simulation
   ([ADR-158](./adr-158-one-dependency-honour-policy-two-eligibility-layers.md)), so what the dialog
   says and what the dates did cannot disagree.

2. **The dependent's dates are a lower bound and are labelled as one.** The excluded constraint can
   only move a date later, never earlier, so the number is not unknown — it is directionally known and
   incomplete. The surface says so plainly ("earliest possible — waits on X, which cannot be
   forecast"), rather than presenting a percentile date with a footnote.

3. **ADR-112 already fires on the blocker, on its own row.** In the sub-case that will actually occur —
   the blocker's team has no throughput — that team is excluded from `throughputByTeam`, so the
   blocker's own `WhenForecast` carries `TotalTrials == 0`, `Feature.CanBeForecast` is false, and the
   blocker already renders as unknown with the team named. The gap that needs closing is therefore
   *visibility*, not honesty, and it is closed by pointing at the row that is already telling the truth.
   Blanking the dependent as well would restate a fact already on screen.

4. **Propagating unknown would cascade, and the cascade is the reason not to.** Unknown is transitive:
   one team with no recent throughput would blank every Feature transitively waiting on anything that
   team touches. On the population the delta names — "most of them, eventually" — that removes large
   parts of the product's core output to express a fact one row away. ADR-112's blast radius was one
   Feature and its delivery; this one is a closure.

**The overrule is cheap, and that is deliberate.** If the maintainer prefers ADR-112's rule applied
uniformly, the change is: the policy returns a Feature-level `CannotBeForecast(WaitsOnUnforecastable)`
instead of an edge-level drop, and the run excludes that Feature's rows from both the eligible set and
the remaining-items sum. **Termination is preserved** — the dependent leaves the run rather than
waiting forever inside it — so the alternative is genuinely available rather than theoretically so. It
is one file: `DependencyHonourPolicy`, plus the run's row filter.

## Alternatives considered

- **Propagate ADR-112's unknown to the dependent.** The strictly consistent reading, terminating, and
  arguably what ADR-112 already implies. **Rejected as the default**, on the cascade above and on the
  asymmetry that matters: ADR-112's un-forecastable team owns *work inside the Feature*, so no honest
  distribution exists for it at all. Here the dependent's own work is fully forecastable and only its
  start constraint is unknown, which makes the honest statement "not before this", not "no idea". This
  is the point the maintainer is most likely to weigh differently, and the paragraph above exists so
  that weighing it differently costs one change.
- **Keep D8 exactly as written — drop and warn, dates presented as ordinary forecasts.** **Rejected** —
  it puts a fully-confident percentile date on a Feature whose start date is unknown, with the
  correction living in a tooltip. That is the shape ADR-112 was written to remove.
- **Simulate the blocker with a synthetic throughput** so it completes eventually and the edge can be
  honoured. **Rejected** — it invents evidence. A team with no throughput history has no distribution,
  and manufacturing one puts a fabricated number *inside* the simulation where nothing downstream can
  tell it apart from a measured one.
- **Refuse to run the forecast at all while any dropped edge exists.** **Rejected** — one stale team
  would take down a Portfolio's entire forecast, which is a larger dishonesty by omission than the one
  being avoided.

## Consequences

- **Positive**: the run terminates on every input, and the reason it terminates is a verdict a user can
  read rather than a guard nobody sees.
- **Positive**: the three cases collapse to one rule with one reason code, and the already-finished case
  needs no code at all because it has no row.
- **Negative**: a Feature can present an earliest-possible date that a reader skims as a forecast. The
  mitigation is presentational and therefore the weakest part of this decision — it is the reason the
  alternative above is kept live and cheap.
- **Negative**: the "blocker absent from the run for another reason" sub-case is narrow and hard to
  provoke on real data, so its acceptance is fixture-led. Recorded rather than hidden.
- **The warning text is assembled client-side.** The DTO carries the reason code and the blocker's
  name, never a sentence, because every word around it — Feature, Features, Portfolio — resolves
  through the instance's own terminology, and because *blocked* is a renameable term already owned by a
  different shipped concept.
- **Reuse verdict**: `Feature.CanBeForecast` / `Feature.TeamsWithoutForecast` → **REUSED AS IS** — they
  are exactly the predicate this needs and this ADR adds nothing to them.
  `FeatureDto.TeamsWithoutForecast` → **REUSED AS IS** for the blocker's own row. The dependent's
  earliest-possible labelling → **EXTEND** of `FeatureDto` (one reason code per edge). No new type.
- Cross-refs [ADR-112](./adr-112-unknown-forecast-when-contributor-cannot-be-forecast.md) (the rule
  this consciously does not extend one level out, and the row that already carries the truth),
  [ADR-158](./adr-158-one-dependency-honour-policy-two-eligibility-layers.md) (the policy that owns the
  verdict), [ADR-155](./adr-155-joint-trial-clock-replaces-per-team-simulation.md) (the loop whose
  termination this protects),
  [ADR-111](./adr-111-aggregate-forecast-field-provenance.md) (how an aggregate reads when a
  contributor is missing).
