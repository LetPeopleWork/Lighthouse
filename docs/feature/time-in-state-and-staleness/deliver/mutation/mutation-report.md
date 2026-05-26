# Mutation Testing — time-in-state-and-staleness

Wave: QUALITY_GATE · Date: 2026-05-26 · Threshold: ≥80% kill rate (per-feature strategy).

Scoped to the feature's **new** logic only (diff range `4f2e1774` .. `aa325466`), so the
kill rate reflects this feature's tests rather than the hundreds of pre-existing mutants in
large files (e.g. the 884-line Linear connector). Per-stack detail in
`mutation-report-backend.md` and `mutation-report-frontend.md`.

## Verdict: PASS (both stacks ≥80%)

| Stack | Tool | Feature-surface kill rate | Verdict |
|-------|------|---------------------------|---------|
| Backend (C#) | Stryker.NET 4.14.2 | **87.1%** (88/101) | PASS |
| Frontend (TS/React) | Stryker 9.6 | **85.0%** (102/120) | PASS |

Post-run verification (normal, non-Stryker run): backend 85/85 green, frontend 152/152 green.
No production source modified (Stryker sandboxes; verified pristine vs HEAD).

## Backend per-file (feature surface)

| File | Kill % | Killed/Total |
|------|-------:|-------------:|
| WorkItemStateTransitionMapper.cs | 100.0% | 2/2 |
| CsvWorkTrackingConnector.cs | 89.5% | 17/19 |
| LinearWorkTrackingConnector.cs | 86.2% | 69/80 |
| LinearResponses.cs | — | auto-property DTOs, covered transitively (no behavioral mutants) |

8 survivors killed via 6 new history-parsing tests + a CSV fixture row + strengthened
assertions (null-endpoint filtering, `?? []` history guards, the project-history-rejection
downgrade/requery path, the CSV synthesized-transition state). 13 survivors remain, all
justified: log-only statements, provably-unreachable post-downgrade fallbacks, equivalent
early-returns, pre-existing pagination mechanics, and one coverage-analysis artifact.

## Frontend per-file (feature surface)

| File | Kill % | Killed/Total |
|------|-------:|-------------:|
| ragRules.ts (`computeStaleOverviewRag`, scoped) | 100.0% | 56/56 |
| scatterMarkerUtils.tsx (aging-chart `isStale`, scoped) | 100.0% | 7/7 |
| TimeInStateBadge.tsx | 88.9% | 16/18 |
| deriveStaleness.ts | 87.5% | 21/24 |
| StaleOverviewWidget.tsx | 13.3% | 2/15 |

The 2 behavioral mutants in StaleOverviewWidget (rendered count, title text) are killed; the
13 survivors are pure Material-UI `sx`/layout styling literals — asserting them would be
Testing Theater. The `deriveStaleness`/`TimeInStateBadge` survivors are redundant defensive
null-guards, genuinely equivalent (`daysInState(null)=0`, `x > undefined === false`) or only
reachable by calling the internal helper directly (forbidden by the public-API-only rule).

## Tooling limitation found

Stryker.NET 4.14.2's glob line-range suffix (`file.cs{a..b}`) silently filtered out **all**
mutants in this environment. Worked around by mutating the target files whole and filtering
survivors to the feature line ranges during analysis. Captured in `docs/ci-learnings.md`.

## Artifacts

- Backend config: `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.time-in-state.json`
- Frontend config: `Lighthouse.Frontend/stryker.config.time-in-state.mjs` + `vitest.stryker.time-in-state.config.ts`
- New/strengthened tests: `LinearWorkTrackingConnectorHistoryParsingTest.cs` (6), `CsvWorkTrackingConnectorTest.cs` + `team-valid-state-since.csv` (ITEM-005), `ragRules.test.ts` (4), `TimeInStateBadge.test.tsx` (1)
