# Evolution: Epic 5074 — Blocked Items Improvements (Slice 01)

**Feature**: `epic-5074-blocked-items` | **Slice**: 01 — Rule-based blocked definition (FOUNDATION / Walking Skeleton)
**Date**: 2026-07-05 | **Status**: IMPLEMENTED

## Business Context

Replace the hardcoded `BlockedStates` + `BlockedTags` mechanism with a flexible `BlockedRuleSet` (the existing `WorkItemRuleSet`/`RuleEvaluator<WorkItem>`, Include semantics: matched = blocked). This is the walking skeleton and riskiest-assumption slice for the whole epic: does the rule engine that already powers deliveries + forecast filters expressively replace the hardcoded blocked mechanism without losing config?

**Persona**: Carlos Mendez (config-admin) — defines exactly what counts as blocked for the team, the definition every downstream blocked signal (badge, duration, trend, stale) derives from.

## Key Decisions

| ID | Decision | Verdict |
|---|---|---|
| ADR-067 | Blocked definition stored as `BlockedRuleSetJson` JSON column on `WorkTrackingSystemOptionsOwner` (Team + Portfolio); evaluated via `IBlockedItemService` → `RuleEvaluator<T>` (Include); legacy columns retained (expand-only) but no longer read for `IsBlocked` | Accepted |
| ADR-067 (migration) | Auto-migration backfill: each `BlockedStates` → `workitem.state equals X`, each `BlockedTags` → `workitem.tags contains Y`, OR-combined; idempotent, null-guarded | Accepted |
| ADR-013 reuse | Third Include consumer of the rule engine; no new operators, no new schema; `MaxRules` = 20, `MaxValueLength` = 500 | Reused unchanged |
| Expand-only | Legacy `BlockedStates`/`BlockedTags` columns + DTO members retained (expand-only) this release — the committed ATs set `settings.BlockedStates` and must keep compiling | Honoured |
| RBAC | All blocked config writes ride existing team/portfolio settings gate (`IRbacAdministrationService`, UI via `useRbac()`) | Reused unchanged |
| Non-premium | Blocked items is non-premium (no premium gate anywhere in this epic) | Confirmed |

## Work Completed (Slice 01)

### 01-01 — Foundation: BlockedRuleSetJson column + IBlockedItemService + blockedRuleSetJson settings contract + auto-migration backfill + EF migration
- **Commit**: `396258a1`
- Created `IBlockedItemService` port and `BlockedItemService` implementation (thin delegator over `RuleEvaluator<WorkItem>`/`RuleEvaluator<Feature>`, mirroring `ForecastFilterRuleService`)
- Added `BlockedRuleSetJson` nullable string column on `WorkTrackingSystemOptionsOwner` (shared base for Team + Portfolio)
- Added `blockedRuleSetJson` member on `SettingsOwnerDtoBase`
- Implemented auto-migration backfill: legacy `BlockedStates`/`BlockedTags` → OR'd `WorkItemRuleSet` conditions
- Generated EF migration across all providers via `Create-Migration.ps1`
- Backend test: `Slice01RuleBasedBlockedScenarios.cs` (3 scenarios enabled)
- Unit tests on backfill synthesis + `IBlockedItemService` edge cases

### 01-02 — Single rule-based IsBlocked read path + ArchUnit lock
- **Commit**: `fc53e46b`
- Removed inline `WorkItem.IsBlocked` and `Feature.IsBlocked` legacy reads; routed `WorkItemDto.IsBlocked` and `WorkItemService.WasBlocked` through `IBlockedItemService`
- Added `BlockedItemSinglePathArchUnitTest.cs` asserting: exactly one evaluation path, evaluator purity, no production code reads `BlockedStates`/`BlockedTags` for `IsBlocked`
- Backend test: 3 more scenarios enabled (custom field, blocked-everywhere, empty-config)

### 01-03 — Settings-write validation errors on blocked rule set
- **Commit**: `00482fb3`
- Added `ValidateBlockedRuleSet` in `TeamController` + `PortfolioController`: rejects `MaxRules` exceeded and unknown field keys with HTTP 400
- Batched validation on the existing settings PUT seam
- Backend test: 2 scenarios enabled (max-conditions, unknown-field)

### 01-04 — RBAC verify-only: non-admin cannot change blocked definition
- **Commit**: `82115adb`
- PASS-WHEN-ENABLED: pre-existing `TeamWrite` RBAC gate already covers the extended blocked-rule write
- Backend test: 1 scenario un-ignored and confirmed GREEN with no production code change

### 01-05 — FE: reuse DeliveryRuleBuilder for blocked rule set + Zod at the changed boundary
- **Commit**: `2cccbaba`
- Replaced two `ItemListManager` blocked lists in `FlowMetricsConfigurationComponent` with reused `DeliveryRuleBuilder` (third UI consumer)
- Added `blockedRuleSet` to `BaseSettings.ts` model with Zod schema validation at the changed boundary
- Vitest coverage: builder renders migrated conditions, add-condition threads through save, Zod rejects malformed input, non-admin sees no editor

### 01-06 — Walking-skeleton E2E (Playwright, demo-data POM)
- **Commit**: `7d9568ca`
- Authored `BlockedItems.spec.ts` with `@walking_skeleton` scenario
- All element access via POM (`MetricsPage`)
- Confirmed against locally-started app with demo data: config-admin saves blocked rule → item matching it reads blocked in overview widget

### Post-delivery fixes
- `c800a856`: scope forecast-filter POM + clear 8 Sonar new-violations
- `7ddb77e2`: replace `Assert.Multiple` with `Assert.EnterMultipleScope` for readability

### Mutation testing
- **Run**: 2026-07-05 (Stryker.NET 4.15.0, 138 tests)
- **Feature-surface kill rate**: 81.6% (40/49 covered mutants) — **threshold ≥80% PASSED**
- Report: `docs/feature/epic-5074-blocked-items/deliver/mutation/mutation-report.md`

## Issues Encountered

| Issue | Resolution |
|---|---|
| `DefaultValueSafeConverter` couldn't handle custom-field IDs starting with `customfield_10001` — parsing failed on the underscore | Switched to `GetProperty` and retrieved the value by index |
| `BlockedItemService.cs:26` `PropertyNameCaseInsensitive = true` mutant survived initial run | `WorkItemRuleSet.Conditions` has a default `[]`, so case-sensitive deserialization still produces an empty list instead of null — added explicit `JsonNamingPolicy.CamelCase` serialization + uppercased-property-name JSON to force detection |

## Lessons Learned

- The rule-engine reuse hypothesis was validated: the existing `WorkItemRuleSet`/`RuleEvaluator<T>` (Include semantics) expressively replaces the hardcoded blocked mechanism with full custom-field support and zero config loss across migration
- The expand-only approach (retaining legacy columns + DTO members) correctly kept the acceptance suite compiling throughout the slice
- Mutation testing revealed a subtle case-insensitivity deserialization gap that unit tests missed — `WorkItemRuleSet.Conditions` default initializer `[]` masks case-sensitive deserialization failures

## Permanent Artifacts

- Architecture: `docs/architecture/epic-5074-blocked-items/` (migrated from `design/`)
- Evolution (this document): `docs/evolution/2026-07-05-epic-5074-blocked-items-slice-01.md`

## Slice Completion Metadata

- **Total steps**: 6 (all PASS — RED → GREEN → COMMIT)
- **Backend ATs enabled**: 7 (+ 1 PASS-WHEN-ENABLED)
- **Production commits**: 6 (one per step) + 2 post-delivery
- **Mutation gate**: PASSED (81.6%)
