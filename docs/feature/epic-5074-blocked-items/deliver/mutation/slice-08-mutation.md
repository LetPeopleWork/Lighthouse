# Slice-08 Mutation Report — Blocked-Over-Time Drill-Through

**Feature**: epic-5074-blocked-items (Story 5436, slice-08 / B1)
**Date**: 2026-07-10
**Tool**: Stryker.NET 4.15 (backend) · Stryker 9.6 (frontend)
**Verdict**: **PASS** — both stacks ≥ 80% tested-mutant kill rate.

## Scope

Slice-08 delivers the drill-through from a Blocked-Over-Time bar into a `WorkItemsDialog`
of the items blocked at that date (interval-reconstruct query + Team/Portfolio
`blockedItemsAtDate` twin endpoints + reconciliation guard + FE bar-click + capture-gap note),
plus the post-review fixes (blocked = To Do + In Progress, honest capture-gap note, red RAG when
unconfigured). Mutation targets are the slice-08 implementation files only.

## Backend (Stryker.NET)

| Metric | Run 1 | Run 2 (after test augmentation) |
|--------|-------|---------------------------------|
| Tested mutants | 64 | 70 |
| Killed | 54 | **65** |
| Survived | 10 | **5** |
| **Tested-mutant kill rate** | 84.4% | **92.9%** |
| Threshold | 80% | 80% |

Mutated files: `WorkItemBlockedTransitionRepository`, `BlockedCountSnapshotRecordingHandler`,
`TeamMetricsService`, `PortfolioMetricsService`, `TeamMetricsController`, `PortfolioMetricsController`.
(The `%` overall score is ~7% — a whole-file-mutate / scoped-test-filter artifact; NoCoverage
dominates because unrelated methods in the broad service/controller classes are mutated but not in
the slice-08 test filter. The tested-mutant rate is the meaningful figure.)

### Survivors killed in run 2
- `TeamMetricsController` / `PortfolioMetricsController` `isBlocked: true` DTO literal (live + past
  branches) — tests asserted only `ReferenceId`, not the blocked flag. Added
  `items.All(i => i.IsBlocked)` assertions.
- Reconciliation-guard snapshot predicate `s.OwnerId == ownerId && s.OwnerType == ownerType &&
  s.RecordedAt == targetDate` — the divergence tests mocked `GetByPredicate(It.IsAny<…>)`, so a
  mis-scoped lookup was invisible. Converted the mocks to **predicate-applying** (`FirstOrDefault(p)`
  over a candidate set), so equality mutations now select nothing → the warning would not fire → the
  test fails. Added a Portfolio divergence-warning test (`PastDateWithCapturedSnapshot…`) to cover the
  guard call there.

### Accepted survivors (5) — documented
- **4× Logical `&& → ||` on the reconciliation-guard predicate** (`TeamMetricsController:508`,
  `PortfolioMetricsController:507`). The guard's only effect is a diagnostic `LogWarning`; the
  endpoint return value is unaffected. A `||` swap still matches the single relevant snapshot in every
  realistic scenario, so it warns identically. Killing these would require per-mutant adversarial
  multi-snapshot candidate sets to prove a **log-only** side effect — disproportionate. Consistent with
  the existing `// Stryker disable once all: diagnostic log text is not behaviour` treatment of the
  warning line itself.
- **1× `PortfolioMetricsService:717` `InvalidateMetrics(portfolio, logger);` statement** — a
  pre-existing cache-invalidation method unrelated to slice-08, pulled into scope only because the
  whole file is mutated. Out of slice scope.

## Frontend (Stryker)

| Metric | Run 1 | Run 2 (after re-scope + test augmentation) |
|--------|-------|--------------------------------------------|
| Tested mutants | 64 | 54 |
| Killed | ~44 | **54** |
| Survived | 20 | **0** |
| **Mutation score** | ~69% | **100%** |
| Threshold | 80% | 80% |

Mutated: `blockedMaxAgeRag.ts` (full), `BlockedItemsOverTimeChart.tsx` (drill helpers `58-82` +
bar-click handler `94-99`), `useMetricsData.ts:213-217`.

### Changes
- **Re-scope**: run 1 mutated `BlockedItemsOverTimeChart.tsx:58-99`, which pulled in the JSX
  styling band (`sx`/`style` object + string literals on lines 83-93) — presentation, not behaviour.
  Narrowed to the two logic ranges `58-82` (capture-gap-note helpers) and `94-99` (`onItemClick`
  guard). 10 styling survivors removed from scope.
- **`blockedMaxAgeRag.ts`**: the RAG test asserted only `ragStatus`, so every `tipText` string
  literal survived and the null-branch mutation fell through to the same status. Added `tipText`
  content assertions (RED/AMBER/GREEN/null) and boundary tests at exactly the threshold and at the
  `0.75 × threshold` aging band — killing the `>=`→`>` equality mutants and the null-branch condition.
- **`BlockedItemsOverTimeChart.tsx`**: added `.not.toHaveTextContent("·")` to the note-match test
  (kills the empty-string return of `buildCaptureGapNote`), and a new test firing the real
  `onItemClick` contract with an out-of-range `dataIndex` asserting no fetch / no dialog / no throw
  (kills the `if (clicked)` guard).

## Post-run safety
Stryker restored all mutated production sources. Working tree after the run contains only the
intended test/config edits; production source is byte-unchanged. Affected suites re-run green
(backend controller tests 161 passed; FE `blockedMaxAgeRag` 8 + `BlockedItemsOverTimeChart` 14).
