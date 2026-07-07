# Epic 5074 — Blocked Items: Slice 04 (Blocked → Stale Linkage)

**Date**: 2026-07-07
**Feature**: `epic-5074-blocked-items` | **Slice**: 04 (Blocked → Stale)
**Status**: COMPLETED — validated through all 5 delivery steps + architecture review + frontend build verification

---

## Feature Summary

When an item is blocked too long, it should trigger the same "stale" signal that time-in-state staleness already provides — with a distinct reason. Slice 04 extends the `deriveStaleness` selector from `boolean` → `StalenessResult {isStale, reasons[]}` per ADR-070, adds a `blockedStalenessThresholdDays` settings field (0 = disabled, range 0–365), and threads the new field through the full settings contract (model → DTO → write path → validation → EF migration) and the FE settings UI (Zod validation, Flow Metrics Configuration twin).

Blocked-duration staleness is OR'd with time-in-state staleness into one `isStale` result, preserving ADR-026's single-selector invariant while narrowing the exclusion to the time-in-state trigger. The blocked-duration driver uses `≥` (at-threshold = stale); time-in-state keeps `>`. When both triggers could fire, blocked-duration wins as the driver, and time-in-state is recorded as context.

## Business Context

**Persona**: flow-coach (Priya Nair) + config-admin (Carlos Mendez, setting the threshold).

Before slice 04, staleness only watched time-in-current-state. A long-blocked item (e.g., 12 days) could completely avoid the "needs a conversation" signal that flow coaches rely on. Slice 04 catches blocked rot in the existing stale signal without adding a new signal surface — the coach sees the same stale treatment (red badge, stale overview widget count, ageing chart dot) with a granular reason string.

This is slice 04 of 5 in Epic 5074. Slices 01 (rule-based blocked), 02 (per-item duration), and 03 (blocked-over-time chart) are already complete. Slice 05 (Jira flagged via predefined field) is DEFERRED pending a pre-slice SPIKE (ADR-071).

## Key Decisions

| ID | Decision | Source |
|----|----------|--------|
| DDD-5 | `deriveStaleness` → `StalenessResult {isStale, reasons[]}`; blocked-duration driver (`≥`) OR'd with time-in-state; ADR-026 exclusion narrowed to time-in-state trigger | ADR-070 |
| DDD-7 | `blockedStalenessThresholdDays` is additive → NO client version-gate (ADR-062); changed `blockedRuleSetJson` and new `blockedCountHistory` endpoint both GATE clients > v26.6.7.1 | ADR-072 |
| UC-1 | Driver+context split: blocked-duration = driver, time-in-state = context entry when blocked item is also state-aged; stale-once | ADR-070 |
| OQ1 | Boundary semantics: blocked-duration `≥` (at-threshold = stale); time-in-state `>` (at-threshold NOT stale) | ADR-070 |

## Architecture

**ADR-070** amends ADR-026: the original rule "a blocked item must NOT also be flagged stale" applied when there was only one staleness source (time-in-state). The amendment narrows the exclusion — blocked items are excluded from *time-in-state* staleness (their in-state clock is paused), but a *new* blocked-duration trigger fires precisely because the item is blocked too long. Both triggers resolve to a single `StalenessResult` with distinct reasons.

**ADR-072** establishes the contract-change matrix for slice 04: `blockedStalenessThresholdDays` is an additive settings field on the same DTO that already carries `stalenessThresholdDays` — clients don't need to gate because the field gracefully defaults to 0 on older servers (ADR-062 additive rule).

### Components Changed

| Component | Action | Details |
|-----------|--------|---------|
| `WorkTrackingSystemOptionsOwner` (base) | EXTEND | New virtual `blockedStalenessThresholdDays` (int, default 0) |
| `Team.cs` / `Portfolio.cs` | EXTEND | Override the virtual property |
| `SettingsOwnerDtoBase` | EXTEND | Constructor parameter + property |
| `TeamExtensions.cs` / `PortfolioExtensions.cs` | EXTEND | Persist on settings write (`SyncTeamWithTeamSettings` / `SyncWithPortfolioSettings`) |
| `TeamController.cs` / `PortfolioController.cs` | EXTEND | Range validation 0–365 (reuses `MinStalenessThresholdDays`/`MaxStalenessThresholdDays`) |
| EF Migrations (SQLite + Postgres) | EXTEND | New column via `CreateMigration.ps1`; real-provider migration test |
| `deriveStaleness.ts` (FE selector) | EXTEND | Return type `boolean` → `StalenessResult {isStale, reasons[]}`; new `StalenessReason` types (driver: `time-in-state` \| `blocked-duration`; context: `context-time-in-state`); `StalenessCandidate` gains `blockedSince?` + `currentStateName` |
| `TimeInStateBadge.tsx` | EXTEND | Reads `result.isStale` + `result.reasons`; accepts `blockedStalenessThresholdDays` prop |
| `BaseMetricsView.tsx` (StaleOverviewWidget) | EXTEND | Count filters on `result.isStale`; passes `blockedStalenessThresholdDays` to selector |
| `WorkItemAgingChart.tsx` | EXTEND | Stale dots use `result.isStale`; passes `blockedStalenessThresholdDays` to selector |
| `IBaseSettings` (FE) | EXTEND | New `blockedStalenessThresholdDays: number` |
| `FlowMetricsConfigurationComponent.tsx` | EXTEND | Twin UI (checkbox toggle + number field, identical pattern to `stalenessThresholdDays`) |
| Zod setting schemas | EXTEND | Validate `blockedStalenessThresholdDays ≥ 0` (rolling-adoption: new field only) |
| `TestDataProvider.ts` | EXTEND | Seeds default 0 |

## Work Completed (5 Steps)

| Step | Description | Dependencies | Phase | Outcome |
|------|-------------|-------------|-------|---------|
| 04-01 | `blockedStalenessThresholdDays` — model column, DTO, write path, validation, EF migration | — | TDD | Model + DTO + write + validation + SQLite/Postgres migrations + real-provider test |
| 04-03 | `deriveStaleness` selector — `StalenessResult` types, blocked-duration logic, comprehensive Vitest | — | TDD | Return type widened; ADR-026 preserved; driver+context; `≥` boundary; Vitest green |
| 04-04 | 3 call sites — `StalenessResult` consumption + `blockedStalenessThresholdDays` prop threading + Vitest | 04-03 | TDD | All 3 surfaces consume `.isStale` + `.reasons[]`; exhaustive selector-use audit passed |
| 04-05 | FE settings pass-through + Zod + Flow Metrics Configuration UI twin | 04-04 | TDD | Full settings chain (IBaseSettings → defaults → read chain → Zod → twin UI → TestDataProvider) |
| 04-02 | Enable DISTILL scenarios 17–21 (settings contract acceptance) | 04-01 | AT-GREEN | 5 scenarios un-ignored: round-trip (#17), default zero (#18), below-range reject (#19), above-range reject (#20), RBAC 403 (#21 PASS-WHEN-ENABLED) |

### Execution Timeline (2026-07-07)

```
04-01  06:00 → 06:12  (entity + migration)     ██████████████
04-03  06:21 → 06:22  (selector)                █
04-04  06:35 → 06:35  (call sites)              █
04-05  06:47 → 06:47  (FE settings)             █
04-02  06:48 → 06:48  (AT enable)               █
```

Note: Deliberately out-of-order execution (04-02 last) — migration + selector + call sites + FE chain all built and tested, then ATs enabled as final gate. This is the pattern for settings-contract slices where the implementation must be complete before the acceptance scenarios can go green.

## Quality Gates

| Gate | Target | Actual | Status |
|------|--------|--------|--------|
| Backend ATs (BlockedItems filter) | All pass | 27 pass, 1 deferred (#16, per-type) | PASS |
| Frontend Vitest | All green | 639 pass | PASS |
| TypeScript build | Zero warnings | pnpm build clean | PASS |
| Biome | Zero errors/warnings | Clean | PASS |
| ArchUnitNET | No new violations | 0 new violations | PASS |
| deriveStaleness call-site audit | No residual boolean reads | All consume `.isStale` + `.reasons[]` | PASS |
| EF Migration tests (real-provider) | SQLite + Postgres pass | Both pass | PASS |
| Settings range validation | 0–365 enforced | Team + Portfolio controllers enforce | PASS |

## Review

**Reviewer**: nw-software-crafter-reviewer
**Outcome**: APPROVED

| Severity | Count | Notes |
|----------|-------|-------|
| **Critical/Blocker** | 0 | None |
| **Medium** | 3 | — |
| **Low** | 2 | — |

Key finding resolved: **D2 (hasStaleConfig)** — fixed before approval.

## Issues Encountered

1. **Out-of-order step execution**: Steps executed 01→03→04→05→02 rather than sequential 01→02→03→04→05. The reasoning was correct — build the full implementation chain (model through FE), then enable the ATs as final verification. The execution log correctly records each step's dependency satisfaction.

2. **AT enablement as final gate (step 04-02 last)**: The DISTILL scenarios for slice 04 (17–21) test the entire settings contract end-to-end. Running them last — after model, selector, call sites, and FE chain were all in place — was the safest approach to avoid RED-then-blocked cycles. This is the standard pattern for settings-contract work in this project.

## Artifacts

| Artifact | Location | Status |
|----------|----------|--------|
| ADR-070 (blocked-duration staleness, amends ADR-026) | `docs/product/architecture/adr-070-blocked-duration-staleness-amends-026.md` | Permanent |
| ADR-072 (contract changes + client version-gate matrix) | `docs/product/architecture/adr-072-blocked-contract-changes-and-client-version-gate.md` | Permanent |
| AT scenarios (Gherkin, NUnit) | `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/BlockedItems/Slice04BlockedStalenessScenarios.cs` | Permanent (test project) |
| AT specifications | `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/BlockedItems/Slice04BlockedStalenessSpecifications.cs` | Permanent (test project) |
| deriveStaleness selector + tests | `Lighthouse.Frontend/src/utils/staleness/deriveStaleness.ts` + `.test.ts` | Permanent (source) |
| EF Migrations (SQLite + Postgres) | Generated via `CreateMigration.ps1` | Permanent (source) |

## Deferred / Out of Scope

| Item | Reason |
|------|--------|
| Slice 05 (Jira flagged via predefined field) | Gated by pre-slice-05 SPIKE (ADR-071); MoSCoW Could, last slice in the Epic |
| Scenario #16 (per-type blocked-count filtering) | UC-2 deferred; additive follow-up with no-rework guarantee |
| Playwright E2E for slice 04 | Stale rendering covered by backend ATs + FE Vitest per component |
| Lighthouse-Clients `blockedStalenessThresholdDays` wrapper | Additive field — no version gate needed (ADR-072); wrap in clients repo |
