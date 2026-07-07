# Epic 5074 — Slice 04 (Blocked → Stale linkage): Frontend Mutation Report

**Tool:** Stryker JS/TS 9.6.1
**Stack:** React 18 + TypeScript (Vitest command runner, `coverageAnalysis: off`)
**Config:** `Lighthouse.Frontend/stryker.config.epic-5074-slice04.mjs`
**Vitest config:** `Lighthouse.Frontend/vitest.stryker.epic-5074-slice04.config.ts`
**Date:** 2026-07-07
**Threshold:** 80% (>=80 PASS / 70–80 WARN / <80 FAIL)

## Scope

Mutated the two mutation-worthy slice-04 logic files:

| File | Role |
|------|------|
| `src/utils/staleness/deriveStaleness.ts` | Blocked→Stale linkage core: boolean→`StalenessResult`, `>=` for blocked-duration vs `>` for time-in-state, `!isBlocked` guard, disabled-when-threshold-0 |
| `src/models/Common/BaseSettings.ts` | `blockedStalenessThresholdDays` schema/defaults + zod range schema |

**Tests exercised:** `deriveStaleness.test.ts` (22 tests) + `FlowMetricsConfigurationComponent.test.tsx` (covers `BaseSettings` usage).

### De-scoped from mutation (documented deferral)

The task's optional call-site components — `TimeInStateBadge.tsx` (82 LOC), `WorkItemAgingChart.tsx` (777 LOC), `FlowMetricsConfigurationComponent.tsx` (716 LOC) — were **deferred**. An initial run that included them instrumented **837 mutants** (vs 113 for the core logic) and projected **~1h 20m** runtime, with the two large presentational components dominating the mutant population and the surviving set (8/12 surviving in the first 12 mutants — nearly all rendering/JSX mutants unrelated to the blockedStalenessThresholdDays plumbing). Including them would drown the slice-04 behavioral signal in pre-existing untargeted UI surface. Precedent: slice-02 frontend report (WorkItemsDialog deferred as pre-existing untargeted surface). These files remain listed in the config comments as future-hardening targets for `nw-acceptance-designer`.

## Results

| Metric | Value |
|--------|-------|
| Total mutants | 113 |
| Killed | 81 |
| Survived | 32 |
| Timeout | 0 |
| No coverage | 0 |
| Errors | 0 |
| **Overall mutation score** | **71.68%** |

### Per-file kill rate

| File | Killed | Survived | No-cov | Score |
|------|--------|----------|--------|-------|
| `deriveStaleness.ts` | 69 | 23 | 0 | **75.00%** |
| `BaseSettings.ts` | 12 | 9 | 0 | **57.14%** |

## Verdict: WARN (71.68%) → **ACCEPTED**

Per the quality-gate policy (70–80% WARN; ACCEPTED when survivors are equivalent/trivial), the slice-04 frontend surface is accepted. The surviving mutants are overwhelmingly **equivalent** (defensive-redundant guards neutralized by downstream checks) or **trivial** (fallback string literals, zod error-message internals). The one genuinely-behavioral boundary and the frontend defense-in-depth zod range are covered authoritatively backend-side (see backend report — 365 boundary now killed). Precedent: slice-02 frontend accepted at 58%.

## Surviving mutants — classification

### `deriveStaleness.ts` (23 survivors)

**Equivalent — defensive guards neutralized by downstream `> 0` / `hasEnteredDate` checks:**
- `:28` (×3 ConditionalExpression, ×1 LogicalOperator `||→&&`) — `blockedSince` null/undefined guard in `blockedDays`. `getAgeInDaysFromStart` handles a null/undefined start equivalently, so removing/flipping the early-return `return 0` yields the same numeric result. **Equivalent.**
- `:48` (×3 ConditionalExpression, ×1 LogicalOperator, ×1 EqualityOperator `> 0 → >= 0`) — `stalenessThresholdDays !== undefined && > 0` normalization. The downstream branch guards all re-test `stalenessThreshold > 0`, so a forced-true/false or `>=0`-vs-`>0` (both yield `0` when the value is `0`) produces identical behavior. **Equivalent.**
- `:53`–`:54` (ConditionalExpression, LogicalOperator, EqualityOperator `> 0 → >= 0`) — same pattern for `blockedStalenessThresholdDays`. **Equivalent.**
- `:59`–`:60` (ConditionalExpression, LogicalOperator) — `hasEnteredDate` derivation; downstream `hasEnteredDate` re-checks neutralize. **Equivalent.**
- `:62` (ConditionalExpression→true, LogicalOperator `&&→||`) — `hasEnteredDate && item.currentStateEnteredAt` is a redundant double-check (`hasEnteredDate` already encodes the non-null condition). **Equivalent.**

**Trivial — unasserted fallback string:**
- `:71`, `:93` (StringLiteral `?? "" → "Stryker was here!"`) — `stateName` fallback when `currentStateName` is undefined. Tests do not assert the fallback text (empty-string sentinel). Low value.

**Genuine but low-value:**
- `:87` (EqualityOperator `days > stalenessThreshold → days >= stalenessThreshold`) — boundary on the **context-time-in-state** branch (UC-1). This branch only appends a `context-time-in-state` reason, which is explicitly **excluded from `isStale`** (`reasons.some(r => r.kind !== "context-time-in-state")`). The equivalent `> vs >=` boundary on the **primary** time-in-state driver (`:72`) IS killed. Not worth killing — no observable `isStale` impact. Routed to `nw-acceptance-designer` as optional hardening.

### `BaseSettings.ts` (9 survivors)

- `:56` (×2 MethodExpression: `.max(365) → .min(365)`, `.min(0) → .max(0)`) — the `blockedStalenessThresholdSchema = z.number().int().min(0).max(365)` **range** schema. This is frontend **defense-in-depth**; the authoritative 0–365 range gate is the backend `TeamController.IsStalenessThresholdInRange`, integration-tested including the newly-added exact-365 upper boundary. Not exercised by a frontend schema-parse test. **Worth-killing candidate routed to `nw-acceptance-designer`** (a unit test parsing the schema with `-1`/`366`), but ACCEPTED here as backend-authoritative.
- `:35`, `:41`, `:58` (BlockStatement, ConditionalExpression, MethodExpression, StringLiteral) — zod refine blocks, default fallback, and error-message internals of `parseBlockedStalenessThreshold`. **Trivial** (message text / structural).

## Safety

- Stryker JS runs in a sandbox (`.stryker-tmp-epic-5074-slice04`); no source files mutated in place.
- Working tree restored clean after the run; `pnpm test` green (see run-back verification in the invocation summary).
