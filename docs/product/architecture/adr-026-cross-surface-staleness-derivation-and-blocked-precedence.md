# ADR-026: Cross-Surface Staleness Derivation — Single Client-Side `deriveStaleness` Selector + Blocked-Excludes-Stale Business Rule

**Status**: Accepted (2026-05-25 — Morgan, interaction mode PROPOSE; slice-03 delta)
**Date**: 2026-05-25
**Feature**: time-in-state-and-staleness (Epic 4144, slice 03 — staleness as a first-class Flow Signal)
**Decider**: Morgan (Solution Architect)

---

## Context

Slices 01 (US-01 time-in-state badge) and 02 (US-02 red highlight, US-03/04 per-team/portfolio threshold config) shipped to main. Product-owner dogfooding produced three intents that reshape staleness from a one-off into a first-class Flow Signal modelled on the existing "Blocked Items" feature:

1. The staleness threshold moves under the existing "Flow Metrics Configuration" settings group, opt-in like the other metrics (default `0 = off`).
2. Two new visualisation surfaces appear alongside the existing WIP-dialog badge: a "Stale Items" overview widget (RAG + count + view-data, mirroring `BlockedOverviewWidget` + `computeBlockedOverviewRag`) and stale emphasis in the Work Item Aging Chart (red bubbles + Time-in-State in the bubble detail).
3. A new business rule: **a blocked item must NOT also be flagged stale** (blocked takes precedence), applied uniformly on every surface.

Slice 02 locked **DDD-8**: per-render staleness comparison is client-side, driven by `currentStateEnteredAt` + `stalenessThresholdDays` on the FE (DISCUSS US-02 AC line 3 — threshold changes take effect on next render, no sync). That decision stands. What slice 03 adds is **three** independent FE surfaces that must each answer "is this item stale?" identically, AND a precedence rule that couples staleness to the already-existing blocked flag (`IWorkItem.isBlocked`, consumed today by the aging chart and the blocked widget).

The architectural question slice 03 must settle: **where does the staleness-derivation logic — including the blocked-exclusion rule — live, so that three surfaces can never disagree about which items are stale, and so the precedence rule exists in exactly one place?**

The existing codebase already demonstrates the failure mode this ADR prevents: blocked-derivation is centralised (`inProgressItems.filter((item) => item.isBlocked)` in `BaseMetricsView`, the `isBlocked` flag itself sourced once on the model), so the blocked widget count and the aging-chart red bubbles never diverge. Staleness must earn the same property by construction.

Two plausible placements:

- **A. One shared pure selector** — `deriveStaleness(item, thresholdDays): boolean` (and/or a `markStaleness(items, thresholdDays)` mapper) in a single module under `Lighthouse.Frontend/src/utils/`, consumed by the WIP-dialog badge, the new Stale Items widget, and the aging chart. The blocked-exclusion rule lives inside this one function.
- **B. Per-surface inline derivation** — each surface computes `daysInState > threshold` locally (the badge already does a variant of this in `TimeInStateBadge`), and each surface separately re-checks `!isBlocked`.

---

## Decision

**Option A — one shared client-side selector.**

Introduce a single pure function (software-crafter picks the exact name/signature at GREEN; the architectural contract is fixed):

```
// the architectural contract — NOT an implementation mandate
isStale = thresholdDays > 0
          && daysInState(item.currentStateEnteredAt, now) > thresholdDays
          && !item.isBlocked
```

Properties this function MUST have:

- **Pure / referentially transparent** — `(item, thresholdDays, now) → boolean`. No service call, no fetch. This is what keeps DDD-8's "client-side, no sync" invariant true and makes the function trivially unit-testable across all boundary cases (threshold 0, exactly-at-threshold, one-over, blocked-and-over).
- **Single home for the blocked-exclusion rule** — the `&& !item.isBlocked` clause exists in exactly one place. No surface re-implements precedence.
- **Single home for the day-count convention** — `daysInState` reuses the same rounding convention already established for the badge in slice 01/02 (the `WorkItemBase.GetDateDifference`-equivalent the FE badge already applies). The selector and the badge share it so the widget count can never disagree with the badge a user reads in the dialog.

The three consumers wire to it:

- **WIP-dialog badge** (existing `TimeInStateBadge` via `WorkItemsDialog.timeInStateColumn`): the badge's red-emphasis decision routes through the shared selector instead of its own inline `daysInState > threshold` check — this is the surface where blocked-excludes-stale is RETROACTIVELY applied (a blocked item over threshold stops rendering red).
- **Stale Items widget** (new `StaleOverviewWidget` + `computeStaleOverviewRag`): the count is `inProgressItems.filter((i) => deriveStaleness(i, threshold)).length` — structurally parallel to the existing `inProgressItems.filter((item) => item.isBlocked)` that feeds the blocked widget.
- **Aging chart** (`WorkItemAgingChart`): the per-item shape extends from `{ workItemAge, isBlocked }` to `{ workItemAge, isBlocked, isStale }`, where `isStale` is computed by the shared selector before grouping; the chart RAG (`computeWorkItemAgeChartRag`) factors `isStale` the same way it factors `isBlocked` today.

`stalenessThresholdDays` is the existing field on the entity (already on `Team`/`Portfolio` via `WorkTrackingSystemOptionsOwner`, already in the read DTO and FE settings model from slice 02). It is the single `thresholdDays` input to the selector. No new persisted field; no backend round-trip on the derivation (DDD-8 upheld).

---

## Alternatives Considered

**Option B — per-surface inline derivation.**

- Pros: no new shared module; each surface is self-contained; smallest diff if only one surface existed.
- Cons:
  1. **Three copies of the staleness predicate** (`thresholdDays > 0 && daysInState > thresholdDays`) drift independently — exactly the "DRY = don't repeat knowledge" violation the project conventions forbid. A future change to the comparison (e.g. `>=` vs `>`, or a different day-rounding) would have to be made in three places, and the widget count would silently disagree with the badge the first time someone updated two of the three.
  2. **Three copies of the blocked-exclusion rule.** The PO's intent #3 is a single business rule applied uniformly; replicating `&& !isBlocked` across the badge, the widget filter, and the chart-item mapper means three chances to forget it. The aging chart and blocked widget already prove the project's pattern is single-sourcing such flags.
  3. Boundary correctness (threshold 0 = off; exactly-at-threshold not stale; one-over stale; blocked-over not stale) would need its unit coverage triplicated.
- **Rejected** because staleness now spans three surfaces with a cross-cutting precedence rule; a single selector is the only placement that makes "the three surfaces can never disagree" true by construction rather than by reviewer vigilance.

**Option C — move derivation server-side (compute `isStale` in `WorkItemDto`).**

- Pros: one home for the rule; the FE surfaces just read a boolean.
- Cons:
  1. **Violates DDD-8.** A server-computed `isStale` would be baked into the response at fetch time; a threshold edit would then require a re-fetch (or a stale boolean until the next render fetch), breaking US-02 AC line 3 ("threshold changes take effect on the next page render — no sync required"). Slice 02 deliberately kept the comparison client-side for exactly this reason.
  2. The threshold is per-owner (team/portfolio) and the comparison is cheap (one subtraction + two comparisons per visible item); there is no read-path performance argument for moving it server-side, unlike `currentStateEnteredAt` (ADR-016) where the N+1 query cost justified persistence.
- **Rejected** as a DDD-8 regression with no compensating benefit. *Noted as an OPEN QUESTION, not a decision*: if a future need arises for **aggregate stale counts without loading all items** (e.g. a portfolio roll-up tile that does not fetch every child item), server-side derivation becomes worth revisiting — at which point a superseding ADR would introduce it for that specific aggregate surface only, leaving the per-item per-render comparison client-side.

---

## Consequences

**Positive**:

- The Stale Items widget count, the WIP-dialog red badge, and the aging-chart red bubbles are derived from one function — they cannot disagree about which items are stale.
- The blocked-excludes-stale precedence rule (PO intent #3) lives in exactly one place; applying it retroactively to the existing dialog badge is a one-line consequence of routing the badge through the selector, not a separate change to remember.
- DDD-8 (client-side, no-sync) is preserved: the selector is pure and runs per render.
- Boundary correctness is unit-tested once, against the single selector, with full coverage of threshold-0 / at-threshold / over-threshold / blocked-over cases — feeding the Stryker ≥80% kill-rate gate cleanly.
- The widget and chart follow established codebase patterns (`computeBlockedOverviewRag` shape, `inProgressItems.filter(...)` count, `{ workItemAge, isBlocked }` chart-item shape) — minimal novelty, maximal reuse.

**Negative**:

- One new shared module (the selector) plus three call-site rewires. Quantified as small: the badge already computes a near-identical predicate (it is moved, not invented); the widget filter mirrors the blocked filter; the chart-item shape gains one field.
- The selector's correctness is a convention enforced by tests rather than the type system (TS cannot express "every staleness decision routes through this function"). Mitigated by the enforcement table below.

**Neutral**:

- No backend change is caused by this ADR (the backend default-value flip in DDD-12 is mechanical and independent). No DTO change. No new route. No premium gate. No breaking change.
- `currentStateEnteredAt` (ADR-016) and `isBlocked` (Epic 5074 mechanism) are consumed as-is; this ADR adds no new persisted field.

---

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| All three surfaces (WIP-dialog badge, Stale Items widget, aging chart) derive staleness through the single shared selector — no surface re-implements `daysInState > threshold` | Code-review gate with this ADR as canonical reference (TS has no ArchUnit/import-linter method-presence enforcement in this codebase — same limitation recorded in ADR-025 §Architectural Enforcement). Reinforced by a Vitest test asserting the widget count and the badge red-state agree for the same `(items, threshold)` input. |
| The blocked-exclusion rule (`&& !isBlocked`) exists in exactly one place | Vitest unit test on the selector: a blocked item over threshold returns `isStale === false`; an identical non-blocked item returns `true`. No second copy of the clause exists (grep-able single occurrence). |
| Threshold `0` disables staleness on every surface (badge not red, widget count 0 regardless of ages, no chart bubble reddened for staleness) | Vitest test per surface asserts threshold-0 → no staleness anywhere; the selector returns `false` when `thresholdDays <= 0`. |
| Day-count convention matches the badge (the widget count equals the number of items the badge would redden, minus blocked items) | Vitest test feeds a mixed set through the selector and the badge's day computation and asserts agreement at the threshold boundary (at-threshold not stale, one-over stale). |
| Derivation is client-side and pure (no fetch / no service in the selector) — DDD-8 upheld | Code review + the selector's signature `(item, thresholdDays, now) → boolean` carries no service dependency; a Vitest test re-runs the selector with a changed threshold and asserts the result flips with no re-fetch. |

---

## Cross-feature impact

- `aging-pace-percentiles` (sibling F, ADR-019/020/021): UNCHANGED. Its `WorkItemAgingChart` per-state bands are independent of the per-item `isStale` flag; the chart-item shape gains `isStale` but the bands compute from `workItemAge` only. The two coexist on the same chart canvas.
- `state-time-cumulative-view` (sibling B3, ADR-022/023/024/025): UNCHANGED. Reads transition rows; does not consume the staleness flag.
- `epic-5074-blocked-items`: the source of `IWorkItem.isBlocked`. This ADR consumes that flag and establishes that blocked takes precedence over stale on the FE surfaces. No change to the blocked mechanism itself.
- ADR-016 (`currentStateEnteredAt` persisted column): consumed unchanged as the selector's day-count input.
- DDD-8 (client-side comparison): upheld and extended — the comparison now lives in one named selector instead of inline in the badge.
