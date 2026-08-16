# ADR-158: One pure policy decides whether a dependency is honoured; the trial only asks whether a blocker is finished yet

- **Status**: Proposed (2026-08-14, DESIGN) — awaiting maintainer ratification
- **Date**: 2026-08-14
- **Feature**: epic-4365-dependencies (ADO Epic #4365, slice 02 — the policy) and
  epic-5792-dependency-aware-forecasting (ADO Epic #5792, slices 01, 02 — the per-trial layer). This
  ADR spans both halves of the 2026-08-16 split, which is exactly why it insists there is only one
  honour-ability decision
- **Deciders**: Benjamin Huser-Berta (maintainer)

## Context

Four separate rules decide whether an edge affects a forecast: both Features must share at least one
Portfolio; an edge that closes a loop is excluded; a blocker that can never complete in this run is
dropped; and without a premium licence no edge is honoured at all.

Two consumers need that verdict. The warnings column and the dependency dialog render it as text; the
simulation acts on it. If they are computed twice, a Feature can show a warning that does not match
what its date actually did — a defect that is worse than either behaviour alone, because the screen
becomes evidence for something untrue. KPI-5 exists to catch exactly this and asks that the single
decision be *structurally* true rather than defended by a grep.

There is a second, different question hiding behind the first. "Is this edge honoured?" is a property
of the run and does not change during it. "Has the blocker finished yet?" is a property of *this trial
on this day* and changes several thousand times a second. Conflating them is what would push policy
into the hot loop.

## Decision

**Split eligibility into two layers with one owner each, and give the honour-ability layer exactly one
implementation that both consumers call.**

### Layer 1 — honour-ability, decided once, by `IDependencyHonourPolicy`

A pure function from a `DependencyContext` to a set of per-edge verdicts. The context carries the
Features in scope with their Portfolios, ranks and stored references, the licence flag, and a predicate
naming which Features can be simulated at all. The verdict per edge is `Honoured`, or `NotHonoured`
with one reason from a closed set: `OutsideThisPortfolio`, `InALoop`, `BlockerCannotBeForecast`,
`NotLicensed`. `BlockerRankedBelow` is carried alongside as an advisory that does **not** change
honouring, because Lighthouse never reorders to satisfy a dependency.

Four points that are part of the decision:

1. **Cycle detection lives inside the policy, not in the sync, and writes nothing.** A stored cycle
   flag would be a second source of truth for half of the verdict, which is the thing this ADR exists
   to prevent. The detector is an **iterative** depth-first search over the Portfolio's edge set —
   iterative rather than recursive because a long chain in a large Portfolio would otherwise be a stack
   overflow in a background service. It is O(V+E) and memoised for the life of one policy evaluation,
   never across evaluations.

2. **The licence is a field of the context, not a branch around the mechanic.** Unlicensed means every
   verdict is `NotHonoured(NotLicensed)`, so the run honours nothing and a forecast identical to a
   dependency-free run is *structural* rather than a code path that could be half-applied. Detection,
   the count, the dialog, the warnings and the Portfolio's dependency-field setting stay free.

3. **The two consumers differ in exactly one input, and the difference is named.** The read path passes
   `feature => feature.CanBeForecast`, which reads the last completed run. The forecast path passes
   `feature => run covers every one of its teams`, which is live. They differ because a Portfolio page
   has no run in flight and a run has no interest in last night's answer; everything else — the
   Portfolio rule, the loop rule, the rank advisory, the licence — is the same call on the same code.

4. **`ForecastService` does not construct verdicts.** It receives a `HonouredDependencies` value object
   and consults it. An ArchUnitNET rule keeps `Lighthouse.Backend.Services.Implementation.Forecast`
   free of any dependency on the cycle detector, the reason enum's construction, or the ordering
   service — so there is nowhere else the verdict *could* be decided.

### Layer 2 — readiness, decided per trial, by `TrialReadiness`

One collaborator, owned by the trial, holding an outstanding-row count per Feature and the honoured
blocker set per Feature. A Feature is ready when every honoured blocker's outstanding-row count is
zero. `GetSimulationResultsOfFeatureToUpdate` filters `Where(x => x.HasWorkRemaining)` today and gains
one predicate: `Where(x => x.HasWorkRemaining && readiness.IsReady(x.Feature))`. **Readiness aggregates
across all of a blocker's rows** — a blocker with work on two teams is not finished until both are —
which is why it cannot live on `SimulationResult`, a type that knows about one row.

### Where it deliberately does not live

- **Not on `SimulationResult`.** It would need knowledge of rows other than its own, and after ADR-155
  that type is output only. An ArchUnitNET rule forbids it from depending on any dependency type, so
  the option stops being available rather than being merely unchosen.
- **Not in the set handed to `InitializeSimulationResults`.** Honour-ability could be applied there, but
  readiness cannot — it changes within a trial — and splitting the two halves across a construction
  step and a hot-loop predicate is precisely the two-places-decide shape KPI-5 forbids.

## Alternatives considered

- **Store the honour-ability verdict at ingestion and read it everywhere.** Cheapest per read, and it
  is what DISCUSS's D7 assumed ("cycles are detected at ingestion"). **Rejected** — the verdict depends
  on the licence, on the Feature ordering and on which Features a run covers, none of which are known
  at ingestion and two of which change without a sync. A stored verdict would be stale in ways the
  screen cannot show, and it would be a second source of truth for the honoured set. The guarantee D7
  actually wanted — no cycle logic inside the simulation loop — is delivered here in full and by a
  stronger mechanism.
- **Incremental cycle detection per changed Feature.** Sounds cheaper at ten thousand Features.
  **Rejected as premature** — it requires a stored graph plus an invalidation rule, which is the stored
  verdict with extra steps, to save a whole-set pass that is O(V+E) over a set the same request already
  materialised. If the read path's measurement in slice 02 says otherwise, the answer is a
  request-scoped memo of a derived value, never a persisted one.
- **Two implementations, one tuned for display and one for the simulation.** **Rejected** — this is the
  failure mode, not an option.
- **Put readiness inside `SimulationResult` as a `Ready` flag maintained by the loop.** **Rejected** —
  a second piece of per-trial state needing its own reset is the obvious place for one trial to leak
  into the next, and the journey's own artifact note already rules that "has this blocker finished" is
  derived from the existing remaining count rather than tracked separately.

## Consequences

- **Positive**: KPI-5 becomes an architecture test rather than a grep. Exactly one type produces a
  verdict, and two ArchUnitNET rules make the alternatives uncompilable.
- **Positive**: the hot loop gains one boolean predicate and no policy. Cycles, Portfolios, licences and
  ordering are all resolved before the first trial starts.
- **Positive**: turning the mechanic off is one field. That is what makes AC-6.2 — an unlicensed
  instance's percentiles being byte-identical to a dependency-free run — assertable rather than hoped
  for.
- **Negative**: the read path evaluates the policy per request, including a cycle pass over the
  instance's edge set on `/features`. Bounded by O(V+E) on data the request already loads, and given a
  measurement gate in slice 02 so the number exists before anyone argues about it.
- **Negative**: one input legitimately differs between the two consumers, so "one place decides" is true
  of the rule and not of every argument to it. Written down here rather than discovered later.
- **Reuse verdict**: `IFeatureOrdering` → **READ, NOT EXTENDED** — this feature consumes the order for
  the ranked-below advisory and never writes it (ADR-132/134). `ILicenseService.CanUsePremiumFeatures`
  → **REUSED AS IS**. `ForecastService.GetSimulationResultsOfFeatureToUpdate` → **EXTEND** (one
  predicate). `IDependencyHonourPolicy`, `DependencyCycleDetector`, `HonouredDependencies`,
  `TrialReadiness` → **CREATE NEW**; nothing in the backend combines edges, membership, ordering and
  licence into a verdict, and no existing graph algorithm is present to extend.
- Cross-refs [ADR-157](./adr-157-dependency-references-stored-on-the-feature.md) (the stored edges this
  reads), [ADR-155](./adr-155-joint-trial-clock-replaces-per-team-simulation.md) (the loop that
  consults layer 2),
  [ADR-159](./adr-159-un-forecastable-blocker-drops-and-the-date-reads-as-a-floor.md) (the
  `BlockerCannotBeForecast` verdict in detail),
  [ADR-132](./adr-132-feature-ordering-derived-total-order-no-ordering-aggregate.md) and
  [ADR-134](./adr-134-ordering-policy-appsetting-enum-single-selection-point.md) (the order this reads
  and never writes),
  [ADR-136](./adr-136-feature-move-authorization-and-non-disclosing-block-reason.md) (the
  non-disclosing pattern the dialog's redacted rows follow).
