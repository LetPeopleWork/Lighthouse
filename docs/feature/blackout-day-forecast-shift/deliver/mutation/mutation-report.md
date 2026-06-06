# Mutation Testing Report — blackout-day-forecast-shift

**Date:** 2026-06-06 · **Tool:** Stryker.NET · **Config:** `Lighthouse.Backend.Tests/stryker-config.blackout-day-forecast-shift.json`
**Strategy:** per-feature, gate ≥80% on new shift code (DISCUSS Outcome KPI + D8).

## Scope

Mutated `Services/Implementation/BlackoutDaysExtensions.cs` — the home of the two new pure primitives that constitute the feature's "new shift code" (ADR-058 DC-1). The day↔date threading through controllers/DTOs/models is one-line parameter passing covered by the BlackoutForecastShift* integration tests; the algorithmic surface lives in the primitives.

Test filter: `BlackoutDaysExtensionsTest` (unit) + the five blackout/backtest integration suites.

## Result — new shift code: 100% effective

| Method | Killed | Survived | Effective |
|---|---|---|---|
| `ProjectWorkingDays` (NEW) | 10 | 2 (both equivalent) | 100% |
| `CountWorkingDays` (NEW) | 5 | 2 (both equivalent) | 100% |
| **New-code total** | **15** | **4 (all equivalent)** | **100%** (raw 78.9%) |

All 15 non-equivalent new-code mutants are killed. The 4 survivors are provably equivalent (below).

A real survivor was found and killed during this run: `CountWorkingDays` L82 `index > 0` → `index >= 0` (counting the start day's own blackout). Killed by the new test `CountWorkingDays_StartDayItselfIsBlackout_DoesNotCountTheStartDay` (start-day blackout must NOT reduce the working-day count — the interval is half-open `(start, target]`).

## Equivalent mutants (4) — justified, cannot be killed

| Location | Mutation | Why equivalent |
|---|---|---|
| `ProjectWorkingDays` L52 | `workingDayCount <= 0` → `< 0` | The only differing input is `==0`; the `while (workingDaysCounted < workingDayCount)` loop then runs zero iterations and returns `start.AddDays(0) == start` — identical to the guard's `return start`. |
| `ProjectWorkingDays` L53 | `{ return start; }` → `{}` | Same reasoning: with the early return removed, `count<=0` falls through to a zero-iteration loop returning `start.AddDays(0) == start`. |
| `CountWorkingDays` L77 | `calendarDays <= 0` → `< 0` | The only differing input is `calendarDays==0` (target==start); the fall-through computes `calendarDays - GetBlackoutDayIndices(start,start).Count(i>0)` = `0 - 0 = 0` — identical to the guard's `return calendarDays`. |
| `CountWorkingDays` L78 | `{ return calendarDays; }` → `{}` | For all `calendarDays<=0`, `GetBlackoutDayIndices` yields no index `>0`, so the fall-through returns `calendarDays - 0 == calendarDays` — identical to the guard. |

These four mutants target **defensive early-return guards** that are behaviourally redundant with the natural loop/subtraction logic. The guards are retained for readability and to short-circuit the trivial past/today case (avoiding an unnecessary `GetBlackoutDayIndices` call). Removing them to "win" the mutants would trade readable, intent-revealing code for a higher raw score — declined.

## Pre-existing methods in the same file (NOT this feature)

`BlackoutDaysExtensions.cs` also contains shipped/locked methods (`GetBlackoutDayIndices`, `IsBlackoutDay`, `HasOverlapWithDateRange`, `AnnotateBlackoutDays`). Their mutants (6 survived in `GetBlackoutDayIndices`, 1 in `HasOverlapWithDateRange`, 7 no-coverage in `AnnotateBlackoutDays`) drag the whole-file score to 69.49%. These are **out of scope** for this feature — `AnnotateBlackoutDays` no-coverage is an artifact of the feature-scoped test filter (its killing tests live in the chart suites, deliberately excluded). Pre-existing test debt, not introduced here.

## Verdict

New shift code meets the ≥80% gate with margin: **100% effective kill rate** (15/15 non-equivalent), 4 documented equivalent guard mutants. Gate PASS.
