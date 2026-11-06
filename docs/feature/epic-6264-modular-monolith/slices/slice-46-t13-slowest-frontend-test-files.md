# Slice 46 — T13 The slowest frontend test files run faster, and the frontend budget comes down with them

Epic #6506 · ADO #6561 · `@infrastructure` · job_id: infrastructure-only · **Size M** (split per file group at DESIGN)

## Goal

The seven slowest frontend test files run faster, so the per-file budget can fall from 56 s to about 37 s.

## Why

T4 #6462 set the budget at 56 s because the budget must be at least 2.1 x the slowest file seen, and that file took
26.5 s. The next files sit close behind, so all seven have to move for the budget to fall far. Below them the suite is a
flat tail at 14 to 17 s.

| File | Max over 7 plain runs (s) | Median (s) |
|---|---|---|
| `ModifyTeamSettings.refinementNeed.test.tsx` | 26.5 | 20.5 |
| `ModifyTeamSettings.readiness.test.tsx` | 22.9 | 17.7 |
| `OverviewDashboard.test.tsx` | 22.0 | 14.3 |
| `BaseMetricsView.loading.test.tsx` | 21.7 | 16.3 |
| `ReportsPage.combinedPdf.test.tsx` | 21.0 | 14.5 |
| `ReportsPage.removal.test.tsx` | 20.2 | 14.4 |
| `ModifyTeamSettings.test.tsx` | 17.6 | 12.8 |

The next file is `ReportsTab.createWizard.test.tsx` at 17.5 s, so 2.1 x 17.5 gives about 37 s.

## IN

- Measure what each file spends its time on before changing it.
- Rewrite the seven files under FE7 #6457's contract: logic asserted directly, one test per wiring, every removed
  assertion accounted for, coverage not lower, each call site proven by a deliberate break.
- Lower the frontend budget once CI shows the new times.

## OUT

- Other test files, production behaviour and pixels.
- Backend, container and live-connector classes (T5, T10).

## Dependencies

Hard: T4 #6462 (pushed 2026-11-06). Soft: T12 #6539, which touches some of the same files for order and load.

## Acceptance criteria

- [ ] Each file's largest time on plain CI runs is under 17.5 s, or the reason it is not is recorded.
- [ ] Coverage and the Stryker kill rate of the touched area do not drop; production code and pixels are unchanged.
- [ ] The frontend budget is lowered in the same Story, derived on at least 5 `main` runs after the rewrites.
