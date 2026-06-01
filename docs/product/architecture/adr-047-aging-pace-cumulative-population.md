# ADR-047: Aging-Pace Bands — Cumulative Reached-At-Least-This-State Population (Connector-Agnostic)

**Status**: Accepted (2026-06-01 — Morgan, interaction mode PROPOSE)
**Date**: 2026-06-01
**Feature**: aging-pace-percentiles (Epic 4144 MVP bundle, slice F) — bug #5145 metric redesign
**Decider**: Morgan (Solution Architect)
**Supersedes**: the D12 / DDD-1 metric of ADR-019 (per-state-exit population). ADR-019's membership rule (§2, `ClosedDate ∈ window`), percentile function (§4, `PercentileCalculator`), and caching policy (§6) are **retained**.

ADO: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5145

---

## Context

The shipped aging-pace bands (ADR-019, D12/DDD-1 as amended 2026-05-25) computed, per mapped
`Doing` state `S`, a percentile distribution over the observations `exitTransition.TransitionedAt − StartedDate`
for items that **exited** `S`. That metric carried two user-facing promises locked in D12:

1. **Monotonicity** — bands rise left→right across the workflow, because total age accumulates.
2. **Last-column alignment** — the rightmost mapped state's band sits at ≈ the cycle-time
   percentile line, because the last `Doing` exit is ≈ the end of cycle time.

Bug #5145's RCA established that **both promises are mathematically false on non-linear flow data**:

- The per-state-exit *population* differs per state. A state that downstream items skip (or
  reach faster) has a **systematically faster cohort** than an upstream state whose population
  includes the slow items that never reached the downstream state. So an upstream band can sit
  *above* a downstream band — non-monotonic.
- The rightmost mapped state is only reached by the items that got that far; items that finished
  via a different path are absent from its population, so its band does **not** align with the
  end-to-end cycle-time line.

The masking factor: the original integration fixture was a **synthetic, perfectly-linear**
flow (every item visits every state in order, once). On that shape the two cohorts coincide and
both promises hold vacuously. Real work-tracking data is non-linear — skips, backward moves,
unmapped/intermediate statuses, and faster-downstream cohorts. The fixture lied about the
environment.

**Connector-agnostic requirement (user-locked, threads through this whole decision).** The
metric MUST be correct for Jira, ADO, Linear, CSV, and any future work-tracking system, with
**no per-connector branching**. The "Jira-shaped" data that surfaced the bug is not a Jira
special case — it is a non-linear-flow *data shape* (skips, backward moves, unmapped statuses,
faster-downstream cohorts) that **any** source can produce. Jira just surfaces it more often.
The computation must therefore operate only on the **normalized** `WorkItem.SyncedTransitions`
model plus the team's configured mapped `DoingStates` — both of which every connector populates
identically. No code path may branch on `WorkTrackingSystem`.

This is an Earned-Trust failure of the original design: a dependency (the *shape* of real
transition data) was assumed honest and never probed. The redesign must probe it — the
replacement verification exercises the lie directly.

---

## Decision

Adopt **Option A — cumulative reached-at-least-this-state population**.

For each mapped `Doing` state `S` (in the team's configured `DoingStates` workflow order), the
band's population is **every completed-in-window item that reached `S` or any state at-or-after
`S` in the mapped order** ("reached at least this state"). Each such item contributes one
observation: its **last-exit cumulative age** — `(TransitionedAt of the item's last exit from S) − StartedDate`.

- **Skip / never-exited imputation.** An item that reached a state at-or-after `S` but has **no**
  recorded exit transition *from* `S` (it skipped `S`, or jumped past it, or is captured only at
  a downstream state) is imputed: its observation for `S` is taken from the **earliest exit it
  does have at-or-after `S`** (the first moment we can prove the item had passed `S`). This keeps
  the population complete and the cohort identical across adjacent states, which is what restores
  monotonicity.
- **Mapped-state filter (B folded in).** Only states in the team's mapped `DoingStates` participate;
  unmapped / intermediate statuses are collapsed into the surrounding mapped span by the existing
  state-category mapping. This filter is **scoped to the pace path only** — it does not touch the
  cumulative-state-time chart or any other metric. The concrete location and mechanism of this
  restriction is specified in **§Implementation Notes → Mapped-state restriction** below.
- **Percentiles, window membership, day-counting, caching** — unchanged from ADR-019
  (`PercentileCalculator.CalculatePercentile`, defaults 50/70/85/95; `ClosedDate ∈ window`;
  `GetDateDifference` day convention; `GetFromCacheIfExists`).

Because the population for state `S` is a **superset** of the population for the next mapped state
(every item that reached `S+1` necessarily reached `S`), and each item's contribution is its
cumulative age at the same monotonic clock, the bands **rise monotonically left→right on every
connector**, and the rightmost mapped state's band ≈ the cycle-time percentile line — the two D12
promises are now **true by construction**, not by accident of fixture shape.

**Connector-agnostic by construction.** The cumulative walk reads only `WorkItem.SyncedTransitions`
(normalized: every connector emits the same `(FromState, ToState, TransitionedAt)` triples after
its own mapping) and the team's mapped `DoingStates`. There is **no** `switch (WorkTrackingSystem)`,
no per-source code path, no connector-specific imputation. A future connector inherits correct
bands the moment it populates the normalized transition model — zero code change here.

This decision lives entirely on the **pace path** (`BuildWorkflowStateOrder` + the cumulative
population walk in `BaseMetricsService` and the two leaf `GetAgeInStatePercentilesFor*` methods).
`BuildWorkflowStateOrder` is pace-exclusive; the sibling cumulative-state-time chart never calls
it and is provably untouched.

### Monotonicity safety clamp (backend, final defensive layer)

Option A makes the metric genuinely monotonic **only given a correct user-configured `DoingStates`
order**. The team configures that order, and Lighthouse cannot guarantee its correctness: a
**misconfigured** order — a logically-left state placed right in config — could still produce a
downward band even after the Option A data fix. This is **not** the cause of bug #5145's
screenshots (those came from the per-state-exit population flaw, which Option A fixes), but it is a
**residual path** that remains open while the order is a user-supplied input we do not validate.

**Decision (binding).** As the **FINAL step** of `ComputeAgeInStatePercentiles` — **after** the
cumulative reached-at-least-this-state percentiles are computed and the states are in
`doingStatesInWorkflowOrder` — walk the ordered states and **clamp each percentile value to the
running maximum of that SAME percentile rank seen so far**. Each rank is clamped independently:
p50 to the max prior p50, p70 to the max prior p70, p85→p85, p95→p95. A state's clamped value
`= max(itsTrueValue, runningMaxForThatRank)`. **Omitted states** (no observations — DDD-4) are
**skipped**, and the running max **carries across the gap** to the next present state.

**Result.** The API itself **guarantees** non-decreasing per-rank bands across the returned
workflow-ordered states. The frontend stays faithful — **no FE logic change** (consistent with the
existing no-FE-change decision; the FE renders exactly what the API returns). CLI/MCP and any
future API consumer **inherit** the guarantee, because it is enforced at the single source of truth
(the API), not re-implemented per consumer. The clamp is **connector-agnostic**: it operates only
on the normalized, already-ordered percentile output, with no `switch (WorkTrackingSystem)`.

**No-op under correct config.** With a correct `DoingStates` order, Option A's superset-population
argument already yields monotonic values, so `runningMaxForThatRank` never exceeds `itsTrueValue`
and the clamp **never adjusts anything**. It only ever **raises** values, and only in the
misconfigured-order (or any residual degenerate) case.

**Last-column invariant is NOT broken.** The "last mapped Doing column ≈ cycle-time lines"
invariant holds under the clamp: in the normal (correct-config) case the rightmost column is the
largest value in a monotonic series, so the running-max clamp leaves it unchanged. In a
misconfigured case the alignment is already moot (the order itself is wrong). The clamp is applied
to the **same workflow order the chart renders** (the returned state order), so the backend and the
chart agree on both the ordering and the values — there is no divergence between what is clamped and
what is drawn.

---

## Implementation Notes (binding contract for DELIVER; impl/fixtures are DELIVER's)

These notes resolve the precision gaps the DESIGN review flagged. They constrain DELIVER without
prescribing internal structure beyond the named helper signature in §Consequences.

### Mapped-state restriction — location and mechanism

**Problem.** The current `BaseMetricsService.BuildWorkflowStateOrder` (lines 31-47) seeds the order
from the team's `doingStates` and then **APPENDS observed unmapped exit states** —
`orderedStates.AddRange(observedExitStates.Where(state => knownStates.Add(state)))`. Left as-is, the
cumulative walk would produce a band for every unmapped/intermediate status the data happens to
contain, which is exactly the spurious non-monotonicity Option B's filter is meant to remove.

**Decision (binding).** The pace path MUST restrict its state set to the team's mapped `DoingStates`
**at the point the cumulative population walk consumes the order**, NOT by changing
`BuildWorkflowStateOrder`. Concretely: `ComputeAgeInStatePercentiles` filters its
`doingStatesInWorkflowOrder` input down to the states that are in the scope's mapped `DoingStates`
(category names), preserving that configured order, **before** iterating. `BuildWorkflowStateOrder`
itself is left untouched — it is also called by the cumulative-state-time path's neighbours, and the
project convention (CLAUDE.md, ADR-024 cross-call rule) is to keep pace-vs-cumulative logic from
leaking into each other; narrowing the consumer is the smaller, provably pace-local blast radius.
The sibling cumulative chart uses the separate `BuildCumulativeWorkflowStateOrder` and is untouched
either way.

**Empty / unconfigured / mismatched `DoingStates` fallback.** `DoingStates` lives on
`WorkTrackingSystemOptionsOwner` (the base of `Team` and `Portfolio`) as a `List<string>` that is
`[Required]` and ships a non-empty default (`["Active", "Resolved", "In Progress", "Committed"]`), so
in practice it is never empty — but it is also not guaranteed to match the workflow the data actually
exhibits (a team may run on the default while its real statuses are e.g. `In Progress / Review / Test`).
The binding fallback contract therefore has **two** triggers, both implemented in
`RestrictToMappedDoingStates`:

1. **Empty** — if the scope's mapped `DoingStates` is empty (or no exit states were observed at all),
   the filter is a no-op and the pace path uses the observed order `BuildWorkflowStateOrder` produced.
2. **Mismatched (majority-coverage guard)** — the filter is applied only when the configured
   `DoingStates` **cover the majority of the observed exit states** (`observedCoveredByConfig * 2 >
   observedStates.Count`). When the configured set covers half or fewer of the observed states, the
   config clearly does not describe this team's real workflow, so filtering to it would drop most of
   the chart's columns; the pace path falls back to the observed order instead. This keeps a stale or
   default `DoingStates` from silently emptying the chart, while still excluding stray
   unmapped/intermediate statuses for any team whose config genuinely describes its workflow (the
   common case, and the one the unmapped-status-exclusion acceptance test pins). The DDD-1b clamp
   guarantees non-decreasing bands either way, so the fallback never reintroduces a visible drop.

The filter compares against the mapped category names in `DoingStates`; the existing state-category
mapping (`GetRawStatesForCategory` / `MapStateToStateCategory`) already collapses raw statuses into
those categories, so no second mapping pass is introduced here.

### Imputation rule boundaries (precise definitions)

The cumulative walk's "reached at least this state" + "skip / never-exited imputation" wording is made
exact here. All predicates read **only** the pre-loaded `IReadOnlyList<WorkItemStateTransition>` the
repository supplied for the item (no new query, no connector branch). Note
`WorkItemStateTransition.FromState` / `ToState` are non-nullable `string` (default `string.Empty`); an
"exit" is therefore detected via `!string.IsNullOrEmpty(FromState)`, the predicate the existing code
already uses — read every "`FromState != null`" below as "`!string.IsNullOrEmpty(FromState)`".

- **"reached state `S`"** = `item.SyncedTransitions.Any(t => t.FromState == S || t.ToState == S)`
  (entry OR exit touches `S`).
- **"last exit from `S`"** = `max(t.TransitionedAt where t.FromState == S)`, or none if the item never
  exited `S`. This is the item's observation for `S` when it exists:
  `lastExit.TransitionedAt − item.StartedDate` (via the existing `CumulativeAgeAtExit` day convention).
- **"earliest at-or-after exit"** (the imputation source for an item that reached `S` but has no exit
  *from* `S`) = `min(t.TransitionedAt where t.FromState is a mapped Doing state whose position in the
  filtered `DoingStates` order is ≥ the position of `S`)`. This is the first moment we can prove the
  item had already passed `S`.
- **Tie-break.** "earliest" / "latest" are over `TransitionedAt`: "earliest" = smallest
  `TransitionedAt`, "last/latest exit" = largest `TransitionedAt`. Ties (identical timestamps) are
  immaterial to the percentile because the contributed age is identical.
- **Completeness invariant (assert in tests).** A completed item always has ≥1 exit transition
  (`!string.IsNullOrEmpty(FromState)` for at least one transition — a finished item left at least one
  state), so for any `S` the item is proven to have reached, imputation **always** finds a candidate
  at-or-after `S`. The walk never produces a "reached but no observation" gap.
- **Monotonicity under backward moves.** Using the **last** exit from `S` (largest `TransitionedAt`)
  means a backward move that re-enters and re-exits `S` later only ever raises the observation —
  cumulative age is non-decreasing, so the superset-population monotonicity argument still holds.
- **Item closing from an intermediate state.** An item that completes from a state partway through the
  mapped `Doing` order contributes (via real exit or imputation) to every state **≤** its
  furthest-reached state, and is simply **absent** from states it never reached. That absence is
  precisely why the last (rightmost) mapped column's band is **≈** the cycle-time percentile line, not
  **=** it: items that finished without reaching the last mapped state are not in its population.

### Monotonicity safety clamp — helper shape and purity

The clamp runs once, as the last step of `ComputeAgeInStatePercentiles`, over the already-computed
ordered `AgeInStatePercentilesDto` list (states in `doingStatesInWorkflowOrder`, omitted states
absent). It is **pure**: it reads only the in-memory ordered DTOs, performs **no repository access**
and **no query** (so the ArchUnit transition-repo rule continues to hold unchanged), and returns a
new ordered list (immutability per CLAUDE.md — no in-place mutation of the input DTOs). To keep
`ComputeAgeInStatePercentiles` under McCabe ≤ 15 / `S3776`, extract a small named helper. Suggested
signature (DELIVER may adjust names but keep the shape, purity, and the per-rank-independent
running-max semantics):

```csharp
// returns the same states in the same order, each percentile value raised (if needed) to the
// running max of that SAME percentile rank seen across prior states; omitted states do not reset
// the running max. Pure: no repository, no query, no mutation of the input DTOs.
protected static IReadOnlyList<AgeInStatePercentilesDto> ClampPercentilesNonDecreasing(
    IReadOnlyList<AgeInStatePercentilesDto> orderedPercentilesByState)
```

The running max is kept **per percentile rank** (a small immutable map `percentile → maxSeen`,
rebuilt functionally as the walk proceeds — e.g. via an `Aggregate` over the ordered states), so
each rank (50/70/85/95) is clamped independently and a higher rank is never pulled up by a lower
one. The helper must tolerate states whose `Percentiles` list omits a rank (defensive) without
throwing.

### Pre-window transitions are NOT clipped

Transition timestamps feeding the cumulative age are **not** clipped to the history window: an item's
cumulative age runs from its `StartedDate` and may include transitions that occurred **before** the
window's `startDate`. This mirrors the existing `cycleTimePercentiles`, which measures whole cycle
time even for items that started before the window. The window governs **membership**
(`ClosedDate ∈ window`, ADR-019 §2), not the per-item age clock.

---

## Alternatives Considered

**Option B — mapped-state filter only (no cumulative population).** Keep the per-state-exit
population but first collapse unmapped/intermediate statuses into mapped `Doing` states. This
removes the *worst* non-monotonicity (spurious bands for transient statuses) but leaves the
**residual** non-monotonicity intact: even among purely mapped states, the per-state-exit cohort
still differs per state (faster-downstream cohorts), so an upstream band can still exceed a
downstream one and the last-column alignment still fails. Insufficient on its own — but its
mapped-state filter is **correct and necessary**, so it is **folded into Option A** rather than
discarded.

**Option C — frontend clamp (`cummax`).** Leave the backend metric as-is and have the chart force
each band to `max(thisBand, previousBand)` so it *looks* monotonic. **Rejected as semantically
dishonest in that form**: with the real per-state-population flaw still in place, a clamp *across
the board* would manufacture numbers the data does not support and mask the actual defect — the
"lie to the user" the methodology forbids.

> **Note — the once-rejected clamp returns as a defensive layer, not a substitute.** The backend
> Monotonicity safety clamp adopted in §Decision is the **same mechanical operation** as Option C
> but plays a **fundamentally different, defensible role**, and is deliberately placed in the
> **backend** rather than the FE. Option-C-alone was dishonest because it substituted fabricated
> values for a real, broken metric across every team. With **Option A now fixing the real
> per-state-population flaw**, the clamp fires **only** when the user-supplied `DoingStates` order
> is misconfigured (or some residual degenerate case) — an **input error where the "true" per-state
> value is itself meaningless**. In that case flattening a drop to the previous level (at worst, the
> same level) is the **lesser evil** versus rendering a confusing dip from a known-bad input. It is
> a safety net layered **on top of** the honest data fix, never in place of it — which is why the
> mechanism rejected above is correct here.

---

## Consequences

**Positive**

- **True monotonicity and ≈ cycle-time-line alignment on all connectors** — the two D12 promises
  hold by construction (superset population + single monotonic age clock), not by fixture luck.
  Correct for Jira, ADO, Linear, CSV, and any future source with no per-connector branching.
- The flow coach's chart-glance reading ("this in-flight dot is past where 85% of items that got
  this far had reached") is now defensible on real, non-linear data.
- Net-new computation is still a single transition walk per request; reuses `PercentileCalculator`,
  `GetFromCacheIfExists`, `GetWorkItemsClosedInDateRange`, and the normalized `SyncedTransitions`
  model. No new primitive.

**Negative / cost**

- **Numbers change for every team and portfolio** — the bands shift the moment this ships (the
  population and the per-item observation both change). This is a deliberate correction of a wrong
  metric, not a regression. Mitigation relies on the **existing** delivery mechanics:
  - the in-process **static metric cache** is process-lifetime and is dropped on every deploy
    (restart-on-deploy), so the recomputed values appear immediately after rollout; **and**
  - the existing **post-sync `InvalidateMetrics`** hook recomputes per team/portfolio on the next sync.
  - **No data migration. No cache-key bump.** The cache key stays `AgeInStatePercentiles_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}`.
    State this explicitly to forestall a redundant future "we changed the metric, bump the cache key"
    change: the restart-on-deploy + post-sync-invalidate path already guarantees no stale value
    survives the rollout, and the key does not encode the metric's *formula* — only its scope and
    window. Bumping it would be dead ceremony.
- **Sonar new-violations = 0** must hold. The cumulative-population walk (reached-at-least filter +
  skip/never-exited imputation) is more branch-dense than the old per-state-exit loop and is a
  cognitive-complexity risk (`S3776`, McCabe ≤ 15). **Extract a named helper** for the per-item
  cumulative observation (and keep the imputation in its own well-named nested method) so neither the
  walk nor the leaf service methods cross the threshold. Suggested signature (DELIVER may adjust names
  but keep the shape and the ≤ 15 budget):

  ```csharp
  // returns the item's cumulative-age observation for targetState, or null if the item never reached it
  protected static int? CumulativeAgeObservationForItemAtState(
      WorkItem item,
      string targetState,
      IReadOnlyList<string> mappedDoingStatesInOrder,
      IReadOnlyList<WorkItemStateTransition> itemTransitionsInOrder)
  ```

  The skip/imputation lives in a small nested helper (e.g. `EarliestAtOrAfterExit`) so each method
  stays under McCabe 15. The signature takes the item's transitions **as a pre-loaded
  `IReadOnlyList<WorkItemStateTransition>`** — it does NOT take a repository and performs no query, so
  the existing ArchUnit transition-repo rule (metrics services read transitions only via
  `IWorkItemStateTransitionRepository` / `IFeatureStateTransitionRepository`; the `BaseMetricsService`
  helper stays repo-free) continues to hold unchanged. Apply the `docs/ci-learnings.md` rule families
  (notably `S3776`, `S107`, `S3267`, `CA1859`) pre-emptively.

- **Monotonicity safety clamp — net consequences.**
  - *No-op under correct config*: with a correct `DoingStates` order Option A already produces
    monotonic values, so the clamp never adjusts anything; it adds a single ordered pass with no
    behavioural change for correctly-configured teams.
  - *Honesty reframing (record explicitly)*: this is the **same** clamp previously **declined as
    "Option C — dishonest"**. Its role is now different and defensible — a **safety net layered on
    top of** the honest Option A data fix, not a substitute. Option-C-alone masked the real
    per-state-population flaw with fabricated values across the board; now, with Option A fixing that
    flaw, the clamp fires **only** on a misconfigured `DoingStates` order (an input error where the
    "true" per-state value is meaningless), so flattening a drop to the previous level is the lesser
    evil versus rendering a confusing dip. A future reader should understand from this note **why the
    once-rejected clamp is now correct as a defensive layer**.
  - *Last-column non-interaction*: the clamp does NOT break the "last mapped Doing column ≈
    cycle-time lines" invariant — the rightmost column is the largest in a monotonic series, so the
    running-max clamp leaves it unchanged in the normal case; in a misconfigured case alignment is
    already moot. The clamp operates on the **same workflow order the chart renders**, so backend and
    chart agree.
  - *FE faithful*: no FE logic change — the guarantee is enforced at the API; the chart renders what
    the API returns. CLI / MCP / any future consumer inherit the guarantee.

**Neutral / preserved**

- The **ArchUnit transition-repo rule is preserved**: metrics services still read transitions only
  via `IWorkItemStateTransitionRepository` / `IFeatureStateTransitionRepository` (the
  `BaseMetricsService` helper stays repo-free, taking pre-loaded transitions). No new port. The
  Monotonicity safety clamp is **pure** — it operates on the already-computed ordered DTOs with no
  repository access and no query — so it does not perturb this rule.
- **Sibling `state-time-cumulative-view` is provably unaffected** — it uses a different membership
  rule and never calls `BuildWorkflowStateOrder` or the pace population walk; the ADR-024 reflection
  rule (sibling helpers do not cross-call) continues to hold.
- **No schema change, no contract change.** The DTO (`AgeInStatePercentilesDto`), the endpoints,
  the FE model, and the response shape are all unchanged. Only the *values* differ.

---

## Verification Strategy (specified here; fixtures/impl are DELIVER's)

The masking **synthetic perfectly-linear integration fixture is REPLACED** by non-linear-flow
fixtures. This is the Earned-Trust correction — the verification must exercise the lie the original
fixture hid.

| Layer | What it proves | Connector-agnostic anchor |
|---|---|---|
| Integration fixtures (replace the synthetic linear one) | Bands are monotonic and align with the CT line on a **non-linear** flow: items that skip a mapped state, move backward, pass through unmapped/intermediate statuses, and form faster-downstream cohorts | Fixtures assert behavior on the **normalized `SyncedTransitions` model**, NOT a Jira-specific payload — the "non-linear shape" is the contract, expressible by any source |
| Live walking-skeleton check | The end-to-end path renders correct rising bands on real-ish data | Drives the **CSV / demo-data path** (which every connector feeds into the same normalized model) carrying **non-linear flows** — skips, backward moves, unmapped states — **plus a real connector if available**. The demo CSV already carries state history via the `StateEnteredDate_<state>` columns (per the demo-data time-in-state CSV work) and MUST be able to express skips / backward-moves / unmapped statuses so the live check exercises the non-linear shape, not just linear demo journeys |
| ArchUnit (preserved) | No per-connector branching leaked in; transitions read only via the repositories | Existing transition-repo rule + a guard that the pace walk contains no `WorkTrackingSystem` switch |

DELIVER owns the fixtures, the imputation implementation, and the helper extraction. DESIGN's
responsibility is this contract: **the replacement verification must run the non-linear shape on
the normalized model, and the live check must use a path every connector shares.**

### Merge gate (binding)

- **REQUIRED before merge (block CI green): the non-linear-flow NUnit integration fixtures.** The
  replacement non-linear fixtures (skips, backward moves, unmapped/intermediate statuses,
  faster-downstream cohorts) MUST exist and pass before the change merges. The synthetic
  perfectly-linear fixture that masked the bug is replaced, not merely supplemented.
- **REQUIRED before merge: the live walking-skeleton on the CSV / demo-data non-linear path.** The
  demo CSV can express the non-linear shape via its `StateEnteredDate_<state>` state-history columns
  (per the demo-data time-in-state work), so the live check MUST drive a non-linear demo journey
  (skip / backward-move / unmapped status) and confirm the bands render monotonically. Per this
  project's convention, **Claude runs this live walking-skeleton locally itself** — it is not punted
  to the user.
- **STRONGLY RECOMMENDED, deferrable: a live real-Jira connector check.** A real-connector smoke
  against a Jira sandbox is the highest-fidelity probe of the non-linear shape, but if no Jira sandbox
  is available during DELIVER it MAY be deferred to a post-release validation. **Any such deferral
  MUST be recorded** (in the DELIVER notes and back here under §Verification) with the reason and the
  follow-up owner; it is not silently skipped.
