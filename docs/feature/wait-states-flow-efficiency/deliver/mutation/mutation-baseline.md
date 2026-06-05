# Mutation Report — wait-states-flow-efficiency (Story #5173)

Feature-scoped mutation testing for the Flow Efficiency tile and its wait-states
overlay. Both stacks use the whole-file mutate + filter-the-report-to-the-feature-
surface method (per `docs/ci-learnings.md` Stryker.NET line-range glob caveat:
`.NET` line-range globs silently mute mutants, so whole-file mutate is the only
reliable backend scope; the feature surface is isolated at report time). The
frontend scopes by TS line-range (which Stryker JS honours), so the FE numbers are
already the feature surface.

- Date: 2026-06-05
- Threshold: >= 80% on core LOGIC
- Configs:
  - Backend `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.wait-states-flow-efficiency.json`
  - Frontend `Lighthouse.Frontend/stryker.config.wait-states-flow-efficiency.mjs`
    + `vitest.stryker.wait-states-flow-efficiency.config.ts`
- Backend report read from `StrykerOutput-wait-states/reports/mutation-report.json`
  (a feature-dedicated output dir; the default `StrykerOutput/<timestamp>` dirs
  hold STALE reports from other features and were NOT used).

## Headline — core-logic surface only

| Stack | Core-logic kill rate | Tested | Killed | Survivors |
|-------|----------------------|--------|--------|-----------|
| Backend (C#) | **86.2%** | 29 | 25 | 4 (all logging-only, equivalent) |
| Frontend (TS/React) | **89.0%** (conservative) / 99.1% (excl. equivalents) | 118 | 105 | 13 (6 presentational, 6 equivalent/defensive, 1 low-value edge) |

The backend whole-file Stryker score (23.15%) is NOT the feature number — it
includes 639 NoCoverage and 8820 mutate-filtered mutants plus all the unrelated
metric methods (cycle time, percentiles, throughput, predictability, baseline,
trend arrows) that live in the same large service files. The 86.2% above is the
filtered feature surface: only `ComputeFlowEfficiency` + the two service entry
points + the two controller actions.

Flow-efficiency feature surface (filter ranges):
- `BaseMetricsService.cs:160-179` — `ComputeFlowEfficiency` (the fold + percentage)
- `TeamMetricsService.cs:354-367` — `GetFlowEfficiencyInfoForTeam`
- `PortfolioMetricsService.cs:606-619` — `GetFlowEfficiencyInfoForPortfolio`
- `TeamMetricsController.cs:271-281` — `GetFlowEfficiencyInfo` (date-validation + RBAC dispatch)
- `PortfolioMetricsController.cs:280-290` — `GetFlowEfficiencyInfo` (date-validation + RBAC dispatch)
- `Models/Metrics/InfoWidgetDtos.cs` — `FlowEfficiencyInfoDto` (record, no logic → no mutants)

The shared `ComputeCumulativeStateTime` / `BuildCumulativeStateTimeRow` helpers
that the two service entry points call are NOT part of this feature surface — they
belong to `state-time-cumulative-view` and carry their own mutation baseline
(`docs/feature/state-time-cumulative-view/deliver/mutation/mutation-baseline.md`).

---

## Backend — feature-surface breakdown

| Method | Tested | Killed | Survivors |
|--------|--------|--------|-----------|
| `BaseMetricsService.ComputeFlowEfficiency` (160-179) | 13 | 13 | 0 |
| `TeamMetricsService.GetFlowEfficiencyInfoForTeam` (354-367) | 4 | 2 | 2 (logging) |
| `PortfolioMetricsService.GetFlowEfficiencyInfoForPortfolio` (606-619) | 4 | 2 | 2 (logging) |
| `TeamMetricsController.GetFlowEfficiencyInfo` (271-281) | 4 | 4 | 0 |
| `PortfolioMetricsController.GetFlowEfficiencyInfo` (280-290) | 4 | 4 | 0 |
| **Total** | **29** | **25** | **4** |

`ComputeFlowEfficiency` — the actual business math (the `totalDoingDays <= 0`
guard, the `!isConfigured` guard, the `OrdinalIgnoreCase` wait-state set membership,
the `(totalDoingDays - waitDays) / totalDoingDays * 100` percentage, and the
`HasDataInScope` / `EfficiencyPercent` / `WaitDays` field wiring) is **100% killed
(13/13)**. The integration suite drives a team/portfolio with a known Doing-vs-Wait
split and asserts the exact efficiency percent, so arithmetic-operator, equality-
boundary and field-swap mutants all die.

Both controllers are **100% killed (4/4)**: the `startDate.Date > endDate.Date`
date-validation mutant dies on the inverted-date 400 test plus the new
single-day-window (`startDate == endDate` → 200) test that pins the comparison as a
strict `>` (an `>=` mutant would 400 the valid single-day window); the RBAC class
guard dispatch dies on the unauthenticated / viewer / admin role tests.

### Backend surviving mutants — all justified (equivalent)

| File:Line | Mutator | Justification |
|-----------|---------|---------------|
| `TeamMetricsService.cs:356` | Statement removal | Removes the `logger.LogDebug(...)` call. Logging is non-observable; we never assert on log output (asserting on logs would be implementation-coupling). Equivalent. |
| `TeamMetricsService.cs:356` | String → `""` | Empties the debug-log message string. Same justification — the log message is not a business outcome. Equivalent. |
| `PortfolioMetricsService.cs:608` | Statement removal | Same logging-statement removal, portfolio scope. Equivalent. |
| `PortfolioMetricsService.cs:608` | String → `""` | Same log-message emptying, portfolio scope. Equivalent. |

All 4 survivors are on the two `logger.LogDebug(...)` lines. Chasing them would
require asserting on log emission — an anti-pattern (implementation-mirroring,
non-business behaviour). They are intentionally left alive and justified equivalent.

---

## Frontend — feature-surface breakdown

The FE full-run overall score was 67.31%, dragged down almost entirely by the
shared `CumulativeStateTimeChart.tsx` (a 4-series chart with colour splits, a
custom tooltip, a custom legend and MUI props — presentational). That chart is
SHARED with `state-time-cumulative-view` and already carries its own presentational
mutation baseline; its survivors are not a gap introduced by this feature.

Core-logic figures below are post-improvement (after the gap-closing tests added in
this pass — confirmed by re-mutating `ragRules.ts:855-882` + `WaitStatesEditor.tsx`
in isolation):

| File / surface | Killed | Survivors | Score | Classification |
|----------------|--------|-----------|-------|----------------|
| `flowEfficiency.ts` (resolveWaitRawStates + fold) | 38 | 0 | **100%** | core logic — perfect |
| `ragRules.ts` `computeFlowEfficiencyRag` (855-882) | 20 | 0 | **100%** | core logic — perfect after amber/green tip tests |
| `FlowEfficiencyOverviewWidget.tsx` tri-state | 14 | 0 logic / 5 presentational | **100%** logic | core branches all killed; survivors are `sx`/`data-testid` only |
| `WaitStatesEditor.tsx` | 33 | 8 | **80.49%** | core logic ≥ 80%; survivors presentational/equivalent |
| **Core-logic blended** | **105** | **1 genuine** | **89.0%** raw / **99.1%** excl. equivalents | ✅ ≥ 80% |
| `CumulativeStateTimeChart.tsx` | 113 | 81 | 58.25% | presentational (shared chart) — separate aggregate |

`flowEfficiency.ts` — the fold itself (`waitStates.length === 0` not-configured
guard, `totalDoingDays <= 0` no-data guard, the `resolveWaitRawStates` mapping
expansion, the case-insensitive wait-state `Set` membership, and the
`(activeDays / totalDoingDays) * 100` percentage) is **100% killed (38/38)**.

`computeFlowEfficiencyRag` — both inverted boundaries are pinned: 39→red, 40→amber,
50→amber, 60→green, 72→green, plus the red/amber/green tip-text content
(the amber tip names the 40–60 band, the green tip names the 60% target). The two
remaining StringLiteral survivors on the amber/green tip text from the baseline run
were killed by the two tip-content tests added this pass — **now 100% (20/20)**.

`FlowEfficiencyOverviewWidget.tsx` — the tri-state (`info === null` → spinner,
`!info.isConfigured` → not-configured, `!info.hasDataInScope` → no-data, else
percent) is **100% killed**. Its 5 survivors are all presentational: emptying a
`sx` styling object or a `data-testid` string — non-business, equivalent.

### Frontend surviving mutants — classification

| File:Line | Mutator | Class | Justification |
|-----------|---------|-------|---------------|
| `FlowEfficiencyOverviewWidget.tsx:102,116,133` | ObjectLiteral / StringLiteral ×5 | presentational | `sx` styling + `data-testid` strings on the not-configured / no-data / percent Typography — no business behaviour. |
| `WaitStatesEditor.tsx:49,61,62` | ObjectLiteral ×3 | presentational | `<Grid size={{ xs: 12 }}>` layout props. |
| `WaitStatesEditor.tsx:74` | BooleanLiteral | presentational | `isLoading={false}` → `true` toggles a loading spinner that never renders meaningfully here. |
| `WaitStatesEditor.tsx:38,39` | ConditionalExpression + MethodExpression ×3 | equivalent | The editor's own `if (state.trim())` / `state.trim()` guard is redundant: `ItemListManager` already trims (`onAddItem(... ?? value.trim())`) and rejects empty input (`inputValue.trim()`) before calling the handler, so the inner guard can never observably change behaviour. |
| `WaitStatesEditor.tsx:25` | MethodExpression | low-value edge | Dropping `.trim()` from the mapping-name filter only matters for a mapping named with whitespace-only characters — not a realistic configuration; not worth a brittle test. |

The 81 `CumulativeStateTimeChart.tsx` survivors are colour hex strings (37
StringLiteral), MUI prop objects (15 ObjectLiteral), and tooltip/legend/axis/
bar-click rendering branches (ConditionalExpression / OptionalChaining / ArrowFunction).
This is a shared presentational chart; per the task scope and mirroring the sibling
`state-time-cumulative-view` baseline, these are classified UI-only/equivalent and
are NOT chased with brittle DOM-structure tests.

---

## Tests added during this pass

Frontend (5 tests, killing the genuine logic survivors):

- `ragRules.test.ts` → "names the 40-60 band and the rounded value in the amber tip"
  and "names the 60% target and the rounded value in the green tip" — kill the two
  amber/green tip-text StringLiteral survivors (ragRules 90% → 100%).
- `WaitStatesEditor.test.tsx` → "clears the wait states when the toggle is switched
  off" — kills the toggle-off `if (!checked)` / `onChange([])` mutants (L32-33).
- `WaitStatesEditor.test.tsx` → "trims a typed wait state before adding it" and
  "ignores a whitespace-only wait state" — exercise the free-text add path.
- `WaitStatesEditor.test.tsx` → the suggestions test now asserts exactly 3 options,
  killing the `.filter(name !== "")` removal mutant (empty-name mapping must NOT be
  suggested). WaitStatesEditor 65.9% → 80.49%.

The 3 backend single-day-window kill-tests (`startDate == endDate` → 200) that pin
the controller date comparison as a strict `>` were authored in the interrupted
prior run and are included in this pass (de-commented to satisfy the no-explanatory-
comments-in-tests convention):

- `FlowEfficiencyReadApiIntegrationTest.GetFlowEfficiency_StartDateEqualsEndDate_IsAccepted`
- `FlowEfficiencyPortfolioReadApiIntegrationTest.GetFlowEfficiency_PortfolioStartDateAfterEndDate_ReturnsBadRequest`
- `FlowEfficiencyPortfolioReadApiIntegrationTest.GetFlowEfficiency_PortfolioStartDateEqualsEndDate_IsAccepted`

These kill the `>` → `>=` date-validation boundary mutant on both controllers
(without the single-day-window cases, an `>=` mutant survives because no test
exercises the equal-date boundary).
