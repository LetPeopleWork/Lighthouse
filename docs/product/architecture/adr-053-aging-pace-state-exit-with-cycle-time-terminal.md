# ADR-053: Aging-Pace Bands — Per-State Exit Age with a Cycle-Time Terminal Column (Connector-Agnostic)

**Status**: Accepted (2026-06-04)
**Date**: 2026-06-04
**Feature**: aging-pace-percentiles (Epic 4144 MVP bundle, slice F) — bug #5145 metric redesign, second pass
**Decider**: User (interaction mode: direct specification), implemented via /nw-bugfix
**Supersedes**: [ADR-047](./adr-047-aging-pace-cumulative-population.md) (the "cumulative reached-at-least-this-state population + imputation" metric, incl. DDD-1/DDD-1b as written there). ADR-019's membership rule (`ClosedDate ∈ window`), percentile function (`PercentileCalculator`), and caching policy are **retained**. The non-decreasing clamp from ADR-047 (DDD-1b) is **retained** but re-rooted (see Decision §3).

ADO: https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5145

---

## Context

ADR-047 (Option A) redesigned the aging-pace bands to a **cumulative reached-at-least-this-state
population** with **imputation** for skip/never-exited items, plus a **>50% majority-coverage
fallback** and **observed-transition axis padding**, all in service of forcing the two user-facing
promises (monotonic rise + last-column ≈ cycle-time line) to hold "by construction."

It shipped, and **bug #5145 recurred**: the dev build was "better but still wrong" on real Jira
boards. The second RCA (`docs/feature/aging-pace-percentiles/bug-5145-rca-v2.md`) found two causes:

- **RC-A (structural).** The horizontal cycle-time lines are percentiles of `CycleTime` over **all**
  closed items. The last band column's population — whatever construction — is only the items that
  reached the last *Doing* state. On any real board (universally on Jira) items close from earlier
  states, so that population is a strict subset and the two **cannot** align. No population trick on
  the *Doing*-state axis fixes this; the last column must literally *be* the cycle-time distribution.
- **RC-B (added wrongness).** Option A's imputation, majority-coverage fallback, and cumulative
  population were not neutral scaffolding — they injected synthetic observations and admitted
  unexpected columns, distorting the very bands they meant to stabilise. The clamp then only masked
  the largest residual drops, which is why it looked "better but still wrong."

## Decision

Compute the bands the simplest honest way the user specified.

1. **Per non-terminal state `S` (the configured `DoingStates`, in order):** the band is the
   percentiles (50/70/85/95) of `(last exit from S).TransitionedAt − StartedDate`, over **only the
   items that actually left `S`** (`∃ transition with FromState == S`; rework → the **last** such
   exit). **No imputation. No `ToState`-reached arm. No majority-coverage fallback. No axis padding**
   from observed transitions — the columns are **exactly** the team's mapped `DoingStates`, in
   configured order. A state with zero "left-it" observations is omitted.

2. **Terminal (rightmost) column:** **reuse the existing cycle-time percentiles**
   (`GetCycleTimePercentilesForTeam` / `…ForPortfolio`) verbatim. The last column therefore sits on
   the horizontal cycle-time lines **by definition**, not by coincidence — resolving RC-A honestly.
   (User: "we don't need to calculate it for the last state, we can simply use the cycle-time
   percentiles we already have — it's what we want anyway.")

3. **Non-decreasing clamp (retained from DDD-1b), over ALL columns including the terminal one:** each
   percentile rank is clamped to its running maximum left→right, so a misconfigured `DoingStates`
   order can at worst render a band **equal** to its predecessor, never a drop. In normal data the
   cycle-time terminal column is already ≥ every upstream column, so the clamp is a no-op there and
   the last column stays exactly on the lines.

**Connector-agnostic** — operates only on the normalized `WorkItem.SyncedTransitions` model + the
team's mapped `DoingStates` + the cycle-time percentiles; **no `switch (WorkTrackingSystem)`**. The
non-linear shapes that broke both prior metrics (skips, backward moves, faster-downstream cohorts,
unmapped/intermediate statuses) are a data *shape* any source can produce, not a Jira special case.

## Consequences

- **Honest last-column alignment.** Equality with the cycle-time lines is structural, proven by the
  per-connector regression test rather than tuned.
- **A misconfigured board renders equal (zero-height) bands**, not drops — the truthful "no extra
  time accrued here" rendering, which also explains the original "skipped colour" report.
- **Frontend unchanged.** The response contract (`[{ state, percentiles:[{percentile,value}] }]`) is
  identical; the FE renders the same shape and draws the cycle-time lines as before.
- **Less code.** Deletes `BuildWorkflowStateOrder`, `RestrictToMappedDoingStates`,
  `GroupCumulativeObservationsByState`, `CumulativeAgeObservationForItemAtState`, and the
  `EarliestAtOrAfterExit` imputation. Smaller surface, fewer failure modes.
- **Sibling `state-time-cumulative-view` is untouched** — it uses a separate
  `BuildCumulativeWorkflowStateOrder` and compute path; verified by the existing seam ArchUnit test.
- **No schema / contract / cache-key change.** In-process `MetricsCache` clears on deploy-restart and
  post-sync `InvalidateMetrics`; reads transitions only via the repository port (ArchUnit rule held).

## Implementation Notes

- `BaseMetricsService.ComputeAgeInStatePercentiles(IEnumerable<WorkItem> completedItemsInWindow,
  IReadOnlyList<string> doingStatesInOrder, IReadOnlyList<int> requestedPercentiles,
  IReadOnlyList<PercentileValue> cycleTimePercentiles)`. New helpers `LastExitCumulativeAge`
  (last `FromState == S` exit, else `null` — no observation) and `CloneCycleTimeColumn`. Retained:
  `ClampPercentilesNonDecreasing`, `CumulativeAgeAtExit` (`(exit.Date − started.Date).Days + 1`,
  matching `CycleTime`). `WorkItemStateTransition.FromState` is non-nullable `string` (default `""`).
- `TeamMetricsService` / `PortfolioMetricsService` pass `team.DoingStates` (no `BuildWorkflowStateOrder`)
  and the cycle-time percentiles list into the compute.
- **Test-axis note:** integration fixtures must configure `DoingStates` — the original pace tests set
  none and only "worked" because of the now-removed observed-transition padding. Regression test:
  `AgeInStatePercentilesNonLinearFlowReadApiIntegrationTest
  .GetAgeInStatePercentiles_ItemsCloseFromDifferentStates_LastColumnEqualsCycleTimeLines…` (×ADO/Jira/Linear).
- Verified: backend 2983 pass / 2 skip, FE aging chart 48 pass, zero-warning build, pace-region
  mutation 100% (21/21 covered; lone uncovered mutant = the redundant `StartedDate` compile-time
  guard, equivalent). Commits `381bd748` (fix) + `62f68429` (test). CI green; user-confirmed on real Jira.
