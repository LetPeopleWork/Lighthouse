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
  cumulative-state-time chart or any other metric.
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
dishonest**: the clamped value is no longer a real percentile of any population — it manufactures
a number the data does not support, which is precisely the kind of "lie to the user" the
methodology forbids. Acceptable only as an *interim* visual stopgap if a backend fix had to wait;
since Option A is a contained pace-path change with no migration, no interim clamp is warranted.

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
  cognitive-complexity risk (`S3776`). **Extract a named helper** for the per-item cumulative
  observation (and keep the imputation in its own well-named method) so neither the walk nor the
  leaf service methods cross the threshold. Apply the `docs/ci-learnings.md` rule families
  pre-emptively.

**Neutral / preserved**

- The **ArchUnit transition-repo rule is preserved**: metrics services still read transitions only
  via `IWorkItemStateTransitionRepository` / `IFeatureStateTransitionRepository` (the
  `BaseMetricsService` helper stays repo-free, taking pre-loaded transitions). No new port.
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
