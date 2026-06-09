# Multiple Cycle Times — Mutation Testing Report (Epic 5251)

Per-feature mutation gate (CLAUDE.md: ≥80% kill on the feature surface). Stryker.NET
(backend) + Stryker (frontend), scoped to the named-cycle-time code.

## Frontend — PASS (91.7% overall)

Config: `stryker.config.multiple-cycle-times.mjs` + `vitest.stryker.multiple-cycle-times.config.ts`.

| File | Score |
|------|-------|
| `CumulativeStateTimeScopeControl.tsx` | 93.2% |
| `isCycleTimeDefinitionValid.ts` | 90.6% |
| **Overall** | **91.7%** |

Tests added to kill real survivors: whitespace/case-insensitive boundary matching,
empty-mapping → invalid (both start and end), whitespace-only boundary → invalid,
multi-state mapping `.every` (any absent → invalid), case-insensitive dedup,
multi-state `cycleTimeBoundaryIndex`, and the scope-control no-reset paths
(null scope, still-valid scope).

Remaining survivors are **equivalent / presentational** and accepted:
- `isCycleTimeDefinitionValid` — the `state !== ""` guard is redundant because
  `present.has("")` is always false; `toLowerCase` vs `toUpperCase` dedup keys
  produce identical groupings.
- `CumulativeStateTimeScopeControl` — the `sx={{ minWidth: 200 }}` object literal
  (presentational), the `useEffect` dependency array, and a redundant
  `definition.id === scopeDefinitionId` conditional already covered behaviourally.

## Backend — named-cycle core covered; aggregate is a whole-file artifact

Config: `stryker-config.multiple-cycle-times.json`. Stryker.NET mutates **whole files**,
and the named-cycle logic lives inside three very large, shared metrics service files
(`BaseMetricsService` / `TeamMetricsService` / `PortfolioMetricsService`) that also
contain throughput, WIP, arrivals, PBC, forecast, flow-efficiency, etc. The named-cycle
test filter does not run those unrelated suites, so their mutants survive as
**false negatives** and drag the aggregate down.

| Scope | Score |
|-------|-------|
| Aggregate (whole files) | 43.1% — dominated by unrelated-method + logger-string survivors |
| `CycleTimeDefinitionValidator.cs` (pure cycle-time logic) | **82.1%** |
| `BaseMetricsService.cs` named-cycle math (L292–435) | **84.5%** (49 killed / 9 survived, after the harness tests below) |

Tests added (the genuinely-real named-cycle survivors):
- `CycleTimeDefinitionValidatorTest` — end-after-start ordering incl. the **equal**
  (zero-span) case, blank-name, duplicate-name, and missing-boundary-is-read-time.
- `BaseMetricsServiceNamedCycleTimeTest` — equal/reversed/absent boundary → empty span
  (`ScopedCumulativeStateOrder`), start==end → null and never-reaches-start → null
  (`NamedCycleTimeWindow`), and `ResolveBoundaryState` unresolvable → fall back to the
  boundary literal.

Accepted survivors in the named-cycle math (equivalent / out-of-feature):
- `ScopedCumulativeStateOrder` L296 `endThreshold <= startThreshold` Equality — the
  `Skip(start).Take(end-start)` slice already yields empty for the equal case
  (`Take(0)`), so `<=` vs `<` is behaviourally equivalent there.
- `IsInFlight` (L435) — a general cumulative helper, not named-cycle code; its killing
  tests (the cumulative read integration suites) are outside this run's test filter.
- A handful of `OrderedStateEntries` degenerate-input mutants (no `StartedDate`,
  `RankOfState` absent) on paths the named feature never produces.

The named-cycle orchestration in `TeamMetricsService` / `PortfolioMetricsService`
(the `definitionId` branch, cache-key suffix, logger lines) is functionally covered by
the integration ATs (`NamedCycleTimeCumulativeScopeIntegrationTest`,
`NamedCycleTimePortfolioIntegrationTest`, `NamedCycleTimeReadApiIntegrationTest`); its
residual survivors are logger-string and `_Def_{id}` cache-suffix mutations
(non-behavioural) plus `definitionId is > 0` patterns whose mutated form yields the same
unscoped result.

## Verdict

Frontend ≥80% met (91.7%). Backend named-cycle **core math and validator** meet ≥80%
(84.5% / 82.1%); the low aggregate is an artifact of whole-file mutation over shared
service files and is justified above rather than chased with vacuous tests.
