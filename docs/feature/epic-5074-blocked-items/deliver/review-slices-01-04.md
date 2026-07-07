# Code Review — Epic 5074 Blocked Items, Slices 01–04

**Date**: 2026-07-07
**Reviewers**: nw-software-crafter-reviewer (BE + FE), main-thread verification pass
**Verdict**: NEEDS_REVISION — 2 confirmed bugs (already in Phase C bugfix plan), plus low-value cleanups. No new blockers beyond the known bugs.

## Verification note
Both Haiku reviewers raised "blocking" issues that verification **disproved**:
- BE "no unique index / upsert race" — FALSE. `LighthouseAppContext.cs:241-242` already has `.HasIndex(OwnerId, OwnerType, RecordedAt).IsUnique()`. Residual only: an unhandled `DbUpdateException` if two same-day refreshes for one owner race (low likelihood — per-owner refresh serializes). Logged as thought, not blocker.
- FE D1 "duplicate tooltip (time-in-state + context-time-in-state)" — FALSE. The `!isBlocked` guard in `deriveStaleness` makes those two reason kinds mutually exclusive; they never co-occur. `TimeInStateBadge.tsx:54-58` is correct.

## Confirmed findings

### BLOCKING (already scheduled — Phase C bugfix)
1. **Recorder counts Done/Closed items** — `BlockedCountSnapshotRecordingHandler.cs:50-54` counts every team item via `IsBlocked` with no done-state exclusion; overview widget counts only active items → over-time chart (5) > overview (1). Fix: exclude done/closed to match overview semantics. Regression test = seeded done+blocked item excluded. → **Bug 1**.
2. **Effective ruleset not surfaced to settings** — `BlockedItemService.cs:54-66` synthesizes rules from legacy `BlockedStates`/`BlockedTags` at evaluation time only; never persisted, settings DTO ships empty `BlockedRuleSetJson`; FE `FlowMetricsConfigurationComponent.tsx:81-89` reads it raw with no legacy fallback → legacy-config owner sees "Add at least one rule". Fix (pick one, decide in bugfix): (a) surface effective ruleset in settings DTO, or (b) persist synthesis on read/save. Regression test = legacy-config owner returns non-empty ruleset. → **Bug 3** (BE #2 + FE D3 are the same defect, two ends).

### NON-BLOCKING (fold into Phase C where cheap; otherwise backlog)
- **FE D5** — `blockedRuleConditionSchema` (BaseSettings.ts:12-16) validates only `z.string()` for fieldKey/operator/value. Backend validates authoritatively, but tightening operator to an enum + value max-length at the Zod trust boundary is cheap defense. suggestion.
- **FE D2** — `blockedRuleSetJson` not explicitly initialized to `null` in CreateTeamWizard/EditTeam default DTOs (undefined works but explicit null signals intent). nitpick.
- **BE upsert race** — wrap `snapshotRepository.Save()` insert branch to tolerate the unique-index violation (reload + update). Low likelihood; thought.

### NOISE (rejected on verification)
- FE D1 (duplicate tooltip) — cannot occur, see verification note.
- FE D4 (unstable useEffect dep) — `blockedRuleSet` is memoized; no loop. reject.
- FE D6 (reasons[] discarded in WorkItemAgingChart) — by design, chart uses boolean only. reject.
- BE #3/#5 (missing unique index) — index exists. reject.

## Praise
- ArchUnit `BlockedItemSinglePathArchUnitTest` locks the single-blocked-definition invariant (ADR-067).
- `BlockedItemService` classical TDD with real rule engine, both Team+Portfolio paths.
- Recorder Earned-Trust probes (freshness / idempotency / single-definition / owner-isolation).
- `blockedStalenessThresholdDays` range validation + expand-only migration.

## Disposition
- Bugs 1 + 3 → Phase C (`/nw-bugfix`), RCA banked here.
- D5 + D2 + upsert-race → address opportunistically in Phase C commits or backlog; none block finalize.
- Proceed to Phase B (slice-04 mutation) — no finding blocks it.
