# Backend Mutation Report — work-item-age-percentiles (ADO #5257)

Tool: Stryker.NET 4.14.2 · DELIVER Phase 5 · 2026-06-09

## Scope

New backend production surface only:

| File | Method / action | Lines |
|------|-----------------|-------|
| `Services/Implementation/TeamMetricsService.cs` | `GetWorkItemAgePercentilesForTeam` | 320–330 |
| `Services/Implementation/PortfolioMetricsService.cs` | `GetWorkItemAgePercentilesForPortfolio` | 264–274 |
| `API/TeamMetricsController.cs` | `workItemAgePercentiles` action | 138–149 |
| `API/PortfolioMetricsController.cs` | `workItemAgePercentiles` action | 119–130 |

Config: `Lighthouse.Backend.Tests/stryker-config.work-item-age-percentiles.json` (whole-file `mutate` — the Stryker.NET 4.14.2 `{a..b}` line-range glob silently mutes all mutants per ci-learnings 2026-05-26, so the report is filtered to the target lines in post-processing). Test filter scoped to `WorkItemAgePercentilesReadApiIntegrationTest`, `WorkItemAgePercentilesPortfolioReadApiIntegrationTest`, `TeamMetricsServiceTests`, `PortfolioMetricsServiceTests`.

## Kill rate (target surface, lines above)

| Metric | Value |
|--------|-------|
| Killed | 10 |
| Survived | 12 |
| **Raw detected rate** | **10 / 22 = 45.5%** |
| Equivalent mutants (excluded) | 8 |
| Genuine survivors | 2 |
| **Adjusted rate (excl. equivalents)** | **10 / 12 = 83.3% — PASS (≥80%)** |

The whole-file overall score (18.76%) is irrelevant: the `mutate` glob covers the entire two service files + two controllers, so 494 NoCoverage + hundreds of survivors come from unrelated, out-of-scope methods. Only the four new code spans above are in scope.

## Tests added to kill genuine survivors

The first run left the start-date-after-end-date guard `startDate.Date > endDate.Date` exposed: the team integration suite only exercised *strictly-after* (still 400 under `>=`), and the portfolio suite had no guard test at all. Three boundary tests added (RED → GREEN, behaviour asserted through the driving port = HTTP endpoint):

- `WorkItemAgePercentilesReadApiIntegrationTest.GetWorkItemAgePercentiles_StartDateEqualsEndDate_IsAccepted` — `startDate == endDate` must return 200.
- `WorkItemAgePercentilesPortfolioReadApiIntegrationTest.GetWorkItemAgePercentiles_StartDateAfterEndDate_ReturnsBadRequest` — strictly-after must return 400.
- `WorkItemAgePercentilesPortfolioReadApiIntegrationTest.GetWorkItemAgePercentiles_StartDateEqualsEndDate_IsAccepted` — `startDate == endDate` must return 200.

Result: all guard mutants on both actions now KILLED — `>=`, `<`, negate, and BadRequest block-removal (10 killed mutants total, see list below).

### Killed on target surface
```
TeamMetricsController.cs:141       > → <    , > → >=   , negate(>)        KILLED
TeamMetricsController.cs:142       BadRequest block → {}                  KILLED
PortfolioMetricsController.cs:122  > → <    , > → >=   , negate(>)        KILLED
PortfolioMetricsController.cs:123  BadRequest block → {}                  KILLED
TeamMetricsService.cs:326          age > 0 → age < 0                      KILLED
PortfolioMetricsService.cs:270     age > 0 → age < 0                      KILLED
```

## Surviving mutants — verdicts

### Equivalent (8) — not worth killing

| File:line | Mutation | Verdict |
|-----------|----------|---------|
| `TeamMetricsService.cs:322` | `logger.LogDebug(...)` → `;` and msg → `""` | **Equivalent** — debug logging only, no observable outcome. No test asserts log output (correctly). |
| `PortfolioMetricsService.cs:266` | `logger.LogDebug(...)` → `;` and msg → `""` | **Equivalent** — as above. |
| `TeamMetricsController.cs:146` | `LogDateBoundaries(...)` → `;` and arg → `""` | **Equivalent** — debug logging only. |
| `PortfolioMetricsController.cs:127` | `LogDateBoundaries(...)` → `;` and arg → `""` | **Equivalent** — debug logging only. |
| `TeamMetricsService.cs:326` | `age > 0` → `age >= 0` | **Equivalent** — a `Doing` work item's `WorkItemAge` is `≥ 1` by construction (`WorkItemBase.WorkItemAge` returns `GetDateDifference(...) + 1`, or `1` for a future/unset start; non-`Doing` items are not in the WIP snapshot). Age 0 is unreachable for the filtered population, so `> 0` and `>= 0` select the identical set. The defensive filter still matters against a hypothetical age-0 row — the `age < 0` variant (which would empty the set) IS killed. |
| `PortfolioMetricsService.cs:270` | `age > 0` → `age >= 0` | **Equivalent** — same reasoning; feature `WorkItemAge` is `≥ 1` for in-progress features. |

### Genuine but marginal (2) — documented, not killed

| File:line | Mutation | Verdict |
|-----------|----------|---------|
| `TeamMetricsService.cs:324` | cache key `$"WorkItemAgePercentiles_{endDate:yyyy-MM-dd}"` → `$""` | **Genuine, low value.** Collapsing the interpolated key to a constant breaks per-`endDate` cache isolation. It survives because WIP `WorkItemAge` is measured against `DateTime.UtcNow` (not `endDate`) — so two distinct `endDate`s rarely yield a different result, making a deterministic killing fixture contrived. The high-value half of this concern (age key ≠ cycle-time key) is already pinned by `..._KeyedOnEndDateOnly_DoesNotCollideWithCycleTimePercentilesCache`, and start-date exclusion by `..._SameEndDateDifferentStartDate_ReturnsIdenticalPercentiles`. Not killed — marginal correctness (stale-cache-across-endDates), not a behavioural defect on any tested path. |
| `PortfolioMetricsService.cs:268` | cache key `$"WorkItemAgePercentiles_{endDate:yyyy-MM-dd}"` → `$""` | **Genuine, low value** — same as above for the portfolio service. |

## Bottom line

- **Adjusted kill rate on the new surface: 83.3% (10/12) — PASS.**
- All high-value mutants (the start/end-date 400 guard on both actions, the `> 0` filter direction, the BadRequest block) are killed.
- Remaining survivors are 8 equivalent (logging + an unreachable `>= 0` boundary) and 2 marginal (constant-cache-key) mutants; none represents a behavioural test gap on a tested path.
