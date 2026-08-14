# ADR-155: One trial, one clock, every team — the per-team independent simulations become a joint loop, and the shared mutable row becomes per-trial state

- **Status**: Proposed (2026-08-14, DESIGN) — awaiting maintainer ratification
- **Date**: 2026-08-14
- **Feature**: epic-4365-dependencies (ADO Epic #4365, slice 04)
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

`ForecastService.RunMonteCarloSimulation` (`:108-131`) groups `simulationResults` by team and runs each
group's 10 000 trials in its own `Task.Run`, each advancing its own day counter. Inside one trial of
team T, no other team exists and no other team's completion day is knowable. Team A's "day 5" and team
B's "day 5" are not the same moment and not even the same trial.

A dependency between two Features in one Portfolio very often crosses a team boundary, and the epic's
mechanic — exclude a Feature from the eligible set *until its blocker has no remaining work in this
trial* — needs "has the blocker finished yet?" to have an answer. Under the current nesting the
question is not merely unanswered; it is not well formed.

The concurrency the current shape buys is also the thing that blocks the fix. `ResetRemainingItems()`
mutates the shared `SimulationResult` rows and each row's `SimulationResults` dictionary is written
without synchronisation. Both are safe today only because each team's task owns its group exclusively.

## Decision

**Swap the loop nesting from `team → trial → day` to `trial → day → team`, and move the per-trial
working state off the shared `SimulationResult` rows onto state the trial owns.**

Six points that are part of the decision:

1. **One day counter per trial.** On each simulated day, every team that still has remaining rows draws
   its own throughput from its own history and consumes from its own rows. The per-day work — draw
   throughput, close that many items, pick which Feature each comes from — is unchanged. *A joint clock
   shares time, never throughput.*

2. **The run is planned once into an immutable `ForecastRunPlan`** — dense row indices, the initial
   remaining count per row, the team of each row, the rows of each team, the rows of each Feature. The
   plan is read-only for the whole run, so every trial can read it concurrently without a copy.

3. **`SimulationResult` stops being run state and becomes output only.** `RemainingItems`,
   `ResetRemainingItems()` and `HasWorkRemaining` leave it; what remains is identity plus the
   completion histogram. Per-trial remaining counts live in a `TrialState` allocated by the trial that
   owns it. This is the structural fix rather than a guard: the shared mutable field that made
   parallelism unsafe is *removed*, so "two trials raced on a row" stops being a bug that can be
   written.

4. **Histograms are accumulated per partition and folded once.** Each parallel partition owns its own
   per-row completion counts; a single-threaded fold merges them after the trials finish. No lock, no
   `ConcurrentDictionary`, and the fold order is fixed by row index so the result does not depend on
   partitioning.

5. **A team with no remaining rows is skipped for the rest of the trial; a team whose rows are all
   waiting is not.** The first keeps the joint loop's total draw count equal to today's — the trial
   runs for `max` days over the teams, but the inner work is still the sum over teams of their own day
   counts. The second is the honest idle day the mechanic depends on: the throughput is drawn and
   discarded, because carrying it forward would invent capacity.

6. **Correctness lands before speed, in that order, as separate commits.** The serial joint loop first,
   proved by exact histogram equality against the recorded pre-change run; then per-trial parallelism,
   proved to leave that equality untouched. The serial intermediate is slower than today's release and
   is never released on its own. The equality is *exact* rather than statistical because
   [ADR-154](./adr-154-addressable-draw-streams-for-the-feature-forecast.md) lands first and makes each
   team's draw sequence independent of the interleaving.

**Termination.** The `while (remaining > 0)` condition is unchanged and no dependency logic enters the
loop: an edge that could not terminate is excluded before the run
([ADR-158](./adr-158-one-dependency-honour-policy-two-eligibility-layers.md)). Because a hang here
stops a background refresh service rather than failing a request, the design adds a last-resort ceiling
on simulated days per trial that aborts the run with a structured `forecast.trial.aborted` event naming
the trial coordinates. The ceiling is not how termination is achieved — it is how a mistake in
achieving it becomes visible in minutes instead of never.

**Budget.** The parallel joint run must complete within **1.5× the pre-epic wall clock** for the
dogfood instance's full Feature set (AC-7.2). The expectation is at or below 1.0×: the unit of
parallel work goes from a handful of teams to 10 000 trials, and ADR-154 removes an allocation per
draw. 1.5× is a ceiling that still flags a regression, not a target.

## Alternatives considered

- **Same-team dependencies only; leave the loop alone.** Ships sooner and is what slice 03 does. As an
  *end state* it was rejected by the maintainer on 2026-08-14, because most real dependencies cross a
  team boundary — it would ship the mechanic for the minority case and leave the majority carrying a
  warning that says the product cannot do the thing it advertises.
- **Post-hoc date shift: forecast as today, then set the dependent's date to `max(own, blocker)`.**
  Cross-team capable and needs no restructure. **Rejected** — it never frees the waiting Feature's
  capacity, so every Feature ranked below one keeps a date that assumes work nobody is doing. It gets
  one Feature roughly right and every other Feature wrong, which is a worse failure than the one it
  fixes because it is invisible.
- **Keep per-team tasks and synchronise the completion days between them.** **Rejected** — teams would
  have to advance in lockstep to compare days, which is the joint loop with a barrier and a race
  instead of a loop.
- **Guard the shared `SimulationResult` rows with a lock or `Interlocked` instead of moving the state.**
  **Rejected** — it makes the current shape parallel-safe without making it *correct*: two trials
  sharing one remaining count is a correctness bug even when the writes are atomic.

## Consequences

- **Positive**: "has the blocker finished in this trial?" becomes a well-formed question, which is the
  whole mechanism. The dependency rule then costs exactly one predicate in exactly one place.
- **Positive, unplanned**: per-trial state removes an aliasing hazard that has always been latent —
  `SimulationResult` is an EF-adjacent type that was also the mutable scratchpad of a hot loop.
- **Negative**: the highest-risk change in the epic, in the code path every date in the product comes
  from. Mitigated by the exact-equality assertion, by the commit sequence, and by the fact that
  distribution preservation is a *structural* argument — a team only ever consumes its own rows, so
  interleaving other teams between its days cannot change what happens to any row.
- **Negative**: an intermediate commit is slower than the released product. Deliberate, and both
  commits land inside one slice.
- **Reuse verdict**: `ForecastService` → **EXTEND** (same class, same public surface, same per-day
  work; the nesting and the state ownership change). `SimulationResult` → **EXTEND (narrowed)** — run
  state removed, output kept. `ForecastRunPlan`, `TrialState`, `TrialReadiness` → **CREATE NEW**; the
  backend has no existing type that flattens a simulation into indexed arrays, and putting per-trial
  arrays on `SimulationResult` would recreate the sharing this decision removes.
- Cross-refs [ADR-154](./adr-154-addressable-draw-streams-for-the-feature-forecast.md) (the
  prerequisite that makes the equality exact),
  [ADR-156](./adr-156-per-trial-max-replaces-product-of-cdfs.md) (the multi-team aggregate that the
  shared clock now makes directly observable),
  [ADR-158](./adr-158-one-dependency-honour-policy-two-eligibility-layers.md) (the eligibility
  predicate this loop consults),
  [ADR-110](./adr-110-multi-team-forecast-joint-probability.md) (which named this restructure as the
  door it was deferring),
  [ADR-058](./adr-058-blackout-forecast-date-shift-translation-placement.md) (day→date translation runs
  after the simulation and is unaffected).
