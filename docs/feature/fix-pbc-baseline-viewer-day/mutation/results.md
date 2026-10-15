# Mutation testing — fix-pbc-baseline-viewer-day

Run 2026-10-15 against the fix branch at `0721cc90c` (backend) and `2c82d62d0` (frontend). Gate is
80 % kill rate on both stacks, scored on the code this fix changed.

| stack | scope | score | killed | survived | wall clock |
| --- | --- | --- | --- | --- | --- |
| Backend (Stryker.NET 5.0.0) | changed regions | **90.8 %** (59 / 65); new logic 59 / 60 before the kill test, 60 / 60 after | 59 | 6 | 5 m 13 s |
| Backend (Stryker.NET 5.0.0) | whole files, headline | 39.48 % | 275 | 173 (+250 no coverage) | — |
| Frontend (StrykerJS) | `FlowMetricsConfigurationComponent.tsx:725-725` | n/a — 0 mutants | 0 | 0 | — |

Configs: `stryker.pbc-baseline.backend.json`, `stryker.pbc-baseline.frontend.json`,
`vitest.stryker.pbc-baseline.config.ts` (copy into `Lighthouse.Frontend/` before running; that root
pattern is gitignored). Backend runs from `Lighthouse.Backend/Lighthouse.Backend.Tests/`; frontend from
`Lighthouse.Frontend/` with `TZ=Europe/Zurich pnpm exec stryker run ../docs/feature/fix-pbc-baseline-viewer-day/mutation/stryker.pbc-baseline.frontend.json`.

## Backend

Stryker.NET only mutates whole files, so `BaseMetricsService.cs` (~1400 lines) and
`PortfolioMetricsService.cs` (~780 lines) were mutated whole and the gate is scored on the lines this
fix wrote: all of `BaselineValidationService.cs`, `BaseMetricsService.cs` 580–630 / 655–665 / 698–708
(`ResolveBaseline`, `InvalidBaselineChart` and the three call sites) and `PortfolioMetricsService.cs`
73–82 (the Feature Size call site). The headline 39.48 % is the untouched remainder of the two
services against a unit-only test filter and says nothing about this change. 449 mutants tested.

| file (changed region) | killed | survived |
| --- | --- | --- |
| `BaselineValidationService.cs` | 42 | 0 |
| `BaseMetricsService.cs` | 15 | 3 |
| `PortfolioMetricsService.cs` | 2 | 3 |

### Closed by this pass

- `BaseMetricsService.cs:592` — `configuredStart != null || configuredEnd != null` → `&&` survived: a
  baseline with only one bound saved would have been charted against the display window instead of
  being refused. Killed by `GetThroughputProcessBehaviourChart_OnlyOneBaselineBoundConfigured_IsInvalidRatherThanFallingBackToTheDisplayWindow`
  (start-only and end-only cases), confirmed by applying the mutant by hand: the test fails with
  Ready / empty reason / not configured, and passes on the real code.

### Accepted survivors (pre-existing code on lines the fix re-touched)

- `BaseMetricsService.cs:708` ×2 — `OrderBy`/`ThenBy` → descending on the cycle-time baseline items.
  The fix only changed the arguments (`baseline.Start!.Value`), not the ordering, which predates it.
  Reversing the whole sequence leaves the average and the set of moving ranges unchanged, so
  `OrderBy → OrderByDescending` is close to equivalent; the `ThenBy` mutant only reorders items closed
  at the same instant. Not chased in this fix.
- `PortfolioMetricsService.cs:73` ×2 and `:75` — the debug log statement and its message, and the
  cache-key string literal. Log wording is not behaviour; the cache key is unchanged by this fix.

## Frontend

StrykerJS matched the file ("Found 1 of 1106 file(s) to be mutated") and instrumented it with
**0 mutants**: `max: formatLocalDate(new Date())` holds no literal, operator or condition any mutator
applies to. The line is pinned instead by
`offers the viewer's own today as the latest baseline end, even where the UTC day is a different one`,
whose RED against the old `new Date().toISOString().slice(0, 10)` was proven in step 02-01
(`max="2026-10-14"` received, `"2026-10-15"` expected). Working tree verified clean after the
in-place run.
