# Bug #5145 "Pace Percentiles Wrong" — RCA & Design Escalation

**Status:** Escalated to DESIGN (`/nw-design`). Not fixable as an in-place bug.
**Source:** ADO Bug #5145 (Severity 3, Jira connections). Investigated via `/nw-bugfix` 2026-06-01.
**Verdict:** The pace-percentile band code faithfully implements feature-delta.md Decision **D12**. D12's central promise — bands that rise monotonically left→right and a last column that equals the cycle-time lines — is **mathematically false** for the per-state-different-population metric on real (especially Jira) data. The metric definition must be revised.

## Reported symptoms

Both examples from **Jira** connections:

1. **Drop on the right.** "The colored boundaries can never be lower on the right side, but here it almost looks like they reset." Example 1: "Waiting for feedback" is a huge drop, then it's "starting over."
2. **Skips a color.** Example 2: "In Review drops, but skips one specific color (maybe that's the issue?)."
3. **Last column mismatch.** "The last column must always match with the horizontal percentile lines, otherwise something is off."

## Root cause

All three symptoms reduce to **one design flaw plus its degenerate small-sample face** — there is no separable frontend bug.

### A/C/D — the per-state-population design flaw (the real defect)

Each state's percentiles are computed over a **different population**: only the items that produced an exit transition `FromState == thatState`.

- `BaseMetricsService.GroupAgeAtExitObservationsByState` (`BaseMetricsService.cs:62-84`) buckets `CumulativeAgeAtExit` under `transition.FromState`, iterating `item.SyncedTransitions.Where(t => !string.IsNullOrEmpty(t.FromState))`. A state's observation list therefore contains only items that *left* that state.
- `ComputeAgeInStatePercentiles` (`BaseMetricsService.cs:49-60`) computes percentiles per bucket with **no cross-bucket coupling**.
- `CumulativeAgeAtExit` (`BaseMetricsService.cs:95-98`) = `(exitedAt.Date − startedDate.Date).TotalDays + 1`, anchored to each item's own `StartedDate`.

Monotonic left→right rise holds **only** if the *same items* flow through *every* state *in order*. Nothing guarantees that. On real workflows items skip states, move backward, or only a faster cohort reaches/exits a downstream state, so a downstream percentile of cumulative age can be numerically **lower** than an upstream one → the band visibly drops (Symptom 1).

The frontend does **not** enforce cross-column ordering: `computePaceBandRects` sorts only *within* a state (`WorkItemAgingChart.tsx:117`), so it renders the non-monotonic backend values faithfully.

**Last-column mismatch (Symptom 3)** is the same flaw from another angle. The horizontal lines are percentiles of `CycleTime` over **all** items closed in the window (`TeamMetricsService.cs:313-322`); the last column's band is percentiles of *cumulative age at exit from the last Doing state* over **only** items that emitted a `FromState == lastDoingState` transition. Different metric, different population — items often close from various Doing states, so the last-column exit population ≠ the completed-items population. The two are equal only by coincidence.

**Why Jira (D):** Jira captures the full status changelog and passes **unmapped** statuses through verbatim (`WorkTrackingSystemOptionsOwner.MapRawStateToMappedName` returns `mapping?.Name ?? rawState`; `IssueFactory.ExtractStatusTransitionsFromHistory` emits a transition per status change). Unmapped intermediate statuses (e.g. "Waiting for feedback") generate real `FromState` observations and get appended to the axis by `BuildWorkflowStateOrder` (`BaseMetricsService.cs:38-44`). That non-linear, polluted data shape is exactly what triggers A/C. ADO data is cleaner, so the bug is Jira-correlated but not Jira-specific.

### B — the "skipped color" is NOT a separable bug

- The pace percentiles are a fixed set of four: `DefaultPacePercentiles = [50, 70, 85, 95]` (`TeamMetricsService.cs:25`, `PortfolioMetricsService.cs:18`), and `BuildAgeInStatePercentilesDto` computes all four for every present state. Each rendered state always carries exactly 4 percentiles → 5 bands → 5 colors.
- Color is `paceBandColorForPosition(position)` where `position` is the `flatMap` index over `upperBoundaries` (`WorkItemAgingChart.tsx:138-156`). `flatMap`'s index is the position in the **original** array — returning `[]` for a zero-height band does **not** renumber later callbacks. Surviving bands keep their correct percentile-rank color; **colors do not shift.** Keying color to the percentile name instead of `position` is a no-op with a fixed 4-percentile set.
- A color disappears only when two adjacent percentile *values* are equal (e.g. p85 == p95 days), producing a genuine zero-height band that is correctly not rendered (`if (height === 0) return []`, `WorkItemAgingChart.tsx:143`). That coincidence arises on **small per-state samples** through the nearest-rank `PercentileCalculator` (`index = floor(p/100 * count) − 1`) — the small-sample face of the **same A/C/D population flaw**.

### The synthetic test masks the flaw

`AgeInStatePercentilesReadApiIntegrationTest.SeedTeamWithKnownStateExitAges` (lines 226-257) seeds 10 items that each flow `InProgress→Review→Test→Done` in order with hand-picked element-wise-rising ages, then `BandValuesRiseAcrossStatesInWorkflowOrder` (lines 102-130) asserts monotonicity. Monotonicity is *manufactured* by the clean, same-population, fully-ordered fixture. No test seeds skipped states, partial flows, unmapped statuses, or a downstream cohort faster than the upstream population.

## Design questions for `/nw-design` (revise D12)

1. **Restore genuine monotonicity & last-column alignment.** Candidate: make each state's observation population **cumulative** (every item that reached *at least* that state), so downstream populations are supersets of upstream ones and the last Doing column's population ≈ the completed-items population (aligning with the cycle-time lines). Handle skips/backward moves/unmapped states explicitly.
2. **Filter unmapped states out of the pace-percentile path** (Symptom D) without disturbing `BuildWorkflowStateOrder` for the other metrics that share it (e.g. the cumulative-state-time chart). Scope the filter to the pace path only.
3. **Degenerate small samples** (Symptom 2 / "skipped color"): decide the intended UX when adjacent percentiles coincide — accept the missing band, or merge/label. This is a UX decision tied to (1), not a standalone rendering fix.
4. **Optional interim mitigation** (if a hotfix is wanted before the redesign lands): a presentation-only non-decreasing clamp in `computePaceBandRects` (carry running max left→right). Restores the monotonic *appearance* but the displayed value may no longer be the column's true percentile — semantically dishonest; ship only with a follow-up. **Declined for now** in favour of a proper redesign.

## Files the redesign will touch

- `Lighthouse.Backend/.../Services/Implementation/BaseMetricsService.cs` — `GroupAgeAtExitObservationsByState`, `ComputeAgeInStatePercentiles`, `BuildWorkflowStateOrder`, `CumulativeAgeAtExit`.
- `Lighthouse.Backend/.../Services/Implementation/WorkTrackingConnectors/WorkItemStateTransitionMapper.cs` and/or `TeamMetricsService.cs` / `PortfolioMetricsService.cs` — unmapped-state filtering.
- `Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.tsx` — `computePaceBandRects` (only if a clamp/UX change is chosen).
- `Lighthouse.Backend.Tests/API/Integration/AgeInStatePercentilesReadApiIntegrationTest.cs` — replace the synthetic monotonicity test (lines 102-130, 226-257) with **Jira-shaped non-linear / skipped-state / unmapped-state** fixtures asserting the corrected invariant.
- `Lighthouse.Frontend/src/components/Common/Charts/WorkItemAgingChart.test.tsx` — non-monotonic-input and equal-adjacent-percentile cases.
- `docs/feature/aging-pace-percentiles/feature-delta.md` — revise **D12 / DDD-1 / DDD-2**.

## Risk notes carried into DESIGN

- A cumulative-population backend change alters returned numbers for **every** team/portfolio. Guard the persisted-model/EF and the `AgeInStatePercentiles_{…}` cache key (`TeamMetricsService.cs:330` — invalidate on deploy). InMemory tests miss data-shape regressions (persisted-model migration trap) — new Jira-shaped fixtures **and a live Jira walking-skeleton check** are required before merge.
- Unmapped-state filtering could remove bands a team currently relies on — verify against a real Jira config.
- All backend changes must pass the SonarCloud new-violations gate and the ArchUnit rule that metrics services read transitions only via `IWorkItemStateTransitionRepository`.
