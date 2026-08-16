# ADR-154: The feature forecast draws from an addressable stream, so a draw is a function of where it sits and not of when it ran

- **Status**: Proposed (2026-08-14, DESIGN) — awaiting maintainer ratification
- **Date**: 2026-08-14
- **Feature**: epic-5792-dependency-aware-forecasting (ADO Epic #5792, slice 02 precursor) — recorded
  2026-08-14 under epic-4365-dependencies, re-homed 2026-08-16 when the forecasting half became its
  own epic
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

Every random number in the Monte Carlo comes through `IRandomNumberService`, whose only implementation
is:

```csharp
public int GetRandomNumber(int maxValue) => new Random().Next(maxValue);
```

Three properties follow, and all three block this epic.

1. **There is no seed anywhere.** A "fixed-seed equality test" is today only possible through a test
   double that hands out a recorded sequence. Such a double asserts *draw order*, not *distribution*.
2. **Draw order is about to change.** [ADR-155](./adr-155-joint-trial-clock-replaces-per-team-simulation.md)
   interleaves the teams inside one trial. A sequence-replaying double therefore fails on the
   restructure even when every histogram is identical — which destroys the exact assertion the
   maintainer asked the restructure to be proved by.
3. **A shared sequence cannot be read from 10 000 parallel trials** without either a lock in the
   hottest loop in the product or a result that depends on thread scheduling.

There is a fourth, smaller problem that is worth fixing while the path is open: allocating a `Random`
per draw is the dominant allocation in a run whose draw count is (trials × days × throughput).

## Decision

**Replace the draw abstraction on the feature-forecast path with an *addressable* stream: a draw is a
pure function of its coordinates, not of a position in a sequence.**

```
Draw(runSeed, trialIndex, teamIndex, dayIndex, drawOrdinal, maxExclusive) -> int
```

Five points that are part of the decision, not implementation detail:

1. **No stream state exists.** The function is a counter-based hash (SplitMix64-class mixing of the
   five coordinates, reduced to `[0, maxExclusive)` by Lemire's unbiased multiply-shift). Nothing is
   allocated, nothing is mutated, nothing is shared. Per-trial parallelism therefore needs no lock and
   no thread-local state on this path, and its result is provably identical to the serial loop.

2. **Team draws are addressed by `(trial, team, day, ordinal)`, never by a running counter.** This is
   the property that makes ADR-155 assertable: a team's draw sequence is *unchanged* by interleaving
   other teams between its days, so the histogram equality between the per-team loop and the joint loop
   is **exact and byte-for-byte**, not "within Monte Carlo noise". Acceptance criteria AC-5.3, AC-7.1
   and AC-8.6 should be tightened to exact equality on that basis.

3. **A day whose throughput is discarded still consumes its coordinates.** When every eligible Feature
   for a team is waiting, that day's throughput is drawn and thrown away. Addressing by day rather than
   by a counter is what keeps this free: it costs nothing and it keeps the coordinate space rectangular.

4. **The seed is per run in production, injected in tests.** `IDrawStreamFactory.ForRun(seed)`;
   production supplies a fresh seed per run, so each refresh remains an independent sample exactly as
   today. Freezing a constant seed in production was considered and deliberately not taken — it would
   stop dates wobbling between refreshes, which is a real product improvement, and it is not this
   epic's to make, because it permanently bakes one draw of sampling error into every date.

5. **Scope is the feature forecast only.** `HowMany`, `PredictWorkItemCreation` and every other
   `IRandomNumberService` caller are untouched; the interface stays. Widening this to the whole product
   would enlarge a change whose entire purpose is to make one other change provable.

## Alternatives considered

- **`new Random(seed)` per (trial, team).** Smallest change, deterministic, and stream-per-team gives
  ADR-155 its ordering independence. **Rejected on two counts.** It allocates ~(trials × teams)
  `Random` instances per run. And .NET documents `Random`'s algorithm as an implementation detail free
  to change between releases — so a fixed-seed regression test asserting exact histogram equality
  would be a test that a future .NET upgrade breaks for no defect. A version-stable generator is a
  requirement here precisely *because* the equality assertion is the safety net.
- **A shared `Random` behind a lock.** Deterministic only if draw order is deterministic, which it is
  not under `Parallel.For`, and a lock in the innermost loop of the product's hot path. **Rejected.**
- **An off-the-shelf PRNG package.** No package supplies the property that actually matters here —
  addressability by coordinate — so one would still be wrapped in the same function, at the cost of a
  dependency on the core forecasting path. **Rejected**; the mixing function is roughly twenty lines
  with no branches, and its correctness is asserted by a distribution test rather than by trust.
- **Keep `IRandomNumberService` and add a seed to it.** **Rejected** — it keeps a sequence, and a
  sequence is the thing that cannot survive interleaving or parallelism.

## Consequences

- **Positive**: the restructure in ADR-155 becomes provable by exact equality rather than by a
  statistical argument, which is what makes it safe to land in the loop every date in the product comes
  from. Per-trial parallelism becomes result-identical to the serial loop by construction, so
  "parallelising moved a percentile" becomes a class of bug that cannot occur rather than one that is
  tested for.
- **Any individual trial is reproducible in isolation** from its coordinates alone, which turns "trial
  4 217 hangs" from a bisect into a single test.
- **Production forecast values change on the release that lands this**, in the same sense and by the
  same magnitude that they already change between any two refreshes: there is no seed today, so
  successive runs already differ by Monte Carlo noise. Nothing new is introduced; the noise merely
  becomes addressable. Worth stating in the release notes so a moved date is not attributed to the
  dependency mechanic.
- **Negative**: one hand-written mixing function in the codebase, which must be covered by a
  distribution test (uniformity across the modulus, no correlation between adjacent coordinates)
  rather than assumed.
- **Reuse verdict**: `IRandomNumberService` / `RandomNumberService` → **NO CHANGE** (kept for
  `HowMany` and the work-item-creation forecast; widening it would put a seed on an interface whose
  other callers do not want one). `IDrawStreamFactory` / addressable stream → **CREATE NEW** — a search
  of the backend for `seed`, `Xoshiro`, `SplitMix` and `deterministic` returns nothing, and no existing
  type exposes a coordinate-addressed draw.
- Cross-refs [ADR-155](./adr-155-joint-trial-clock-replaces-per-team-simulation.md) (the restructure
  this exists to make provable), [ADR-156](./adr-156-per-trial-max-replaces-product-of-cdfs.md) (the
  one commit in the sequence that legitimately moves dates).
