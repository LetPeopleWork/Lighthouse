# Epic 5074 — Blocked Items: Slice 01 (Rule-Based Blocked Definition)

**Date**: 2026-07-05
**Feature**: `epic-5074-blocked-items` | **Slice**: 01 (FOUNDATION / Walking Skeleton)
**Status**: COMPLETED — validated through all 6 delivery steps + mutation testing + manual verification

---

## Feature Summary

Replace the hardcoded `BlockedStates`/`BlockedTags` with a flexible `BlockedRuleSet` using the existing `WorkItemRuleSet`/`RuleEvaluator<WorkItem>` (ADR-013, Include semantics), reusing the `DeliveryRuleBuilder` UI, and auto-migrating existing config into equivalent OR-combined rule conditions.

`IsBlocked` now computes from a single `BlockedRuleSet` via `RuleEvaluator<WorkItem>` — the consolidated definition every downstream signal (badge, overview widget, and future slices 02–05) derives from.

## Business Context

[Epic 5074 (Blocked Items)](https://dev.azure.com/letpeoplework/Lighthouse/_workitems/edit/5074) was promoted out of Epic #4144 (More Detailed State Info). The core insight: "blocked" is conventionally defined per-team — sometimes a tag, sometimes a custom field, sometimes a special state — and varies even within the same connector. There is no portable source-system concept to read.

Slice 01 is the **walking skeleton foundation** for all later slices (02: per-item duration, 03: over-time chart, 04: blocked→stale, 05: Jira flagged-field cleanup). It proves the central hypothesis: the rule engine that powers deliveries + throughput can also define "blocked."

**Persona grounding**: config-admin (Carlos Mendez, Team Phoenix) — editing Team/Portfolio Flow Metrics Configuration, motivated to make "blocked" match the team's real workflow without losing existing config.

## Key Decisions

| ID | Decision | Details | Source |
|----|----------|---------|--------|
| D-SCOPE | 5 thin end-to-end slices | Slice 01 is the walking skeleton foundation; slices 02–05 are future work | DISCUSS |
| D-ENGINE | Blocked = third Include consumer | Reuses `WorkItemRuleSet`/`RuleEvaluator<WorkItem>` (ADR-013). No new engine, no new operators | DISCUSS + ADR-067 |
| D-MIGRATE | Auto-migration from BlockedStates/BlockedTags | Each `BlockedStates` → `State equals X`, each `BlockedTags` → `Tags contains Y`; OR-combined. Loss-free verified | ADR-067 |
| D-CAPTURE | Blocked-time = Lighthouse-side per-sync | NOT `WorkItemStateTransition` (L1 locked). New `WorkItemBlockedTransition` entity for slice 02 | DISCUSS |
| D-CHART | Forward-only daily blocked-count | Delivery-metrics history pattern; slice 03 | ADR-069 |
| D-STALE | Distinct blocked-duration staleness trigger | OR'd with time-in-state; `blockedStalenessThresholdDays` (0=off, `>=`); slice 04 | ADR-070 |
| D-FLAGGED | Predefined/system additional field | Jira flag auto-registered; slice 05 SPIKE-gated | ADR-071 |
| D-PREMIUM | Non-premium | Verified: `BlockedOverviewWidget` has no premium gate | DISCUSS |
| DDD-1 | Rule storage = JSON column | `BlockedRuleSetJson` on `WorkTrackingSystemOptionsOwner`; existing idiom reused | ADR-067 |
| DDD-2 | Auto-migration = app-layer + EF backfill | Pure SQL can't reuse C# rule synthesis; idempotent, null-guarded | ADR-067 |
| DDD-7 | Version-gate contract changes | Changed settings contract (`blockedRuleSet`) → GATE; baseline > v26.6.7.1 | ADR-072 |
| Client version-gate | Strictly newer-than-last-released | Pre-check server version; fail with "upgrade Lighthouse" error | ADR-072 |

## Steps Completed

All 6 delivery steps (slice 01) completed with RED → GREEN → COMMIT cycles, all PASS:

| SID | Description (inferred from execution context) | RED | GREEN | COMMIT |
|-----|-----------------------------------------------|-----|-------|--------|
| 01-01 | Foundation: `BlockedRuleSetJson` column + `IBlockedItemService` + auto-migration | 2026-07-04 06:36 | 2026-07-04 06:53 | 2026-07-04 06:56 |
| 01-02 | `BlockedRuleSet` settings write/read endpoint | 2026-07-04 08:23 | 2026-07-04 08:59 | 2026-07-04 09:00 |
| 01-03 | `DeliveryRuleBuilder` reuse in blocked config UI | 2026-07-04 09:07 | 2026-07-04 09:13 | 2026-07-04 09:13 |
| 01-04 | `IsBlocked` computed via `RuleEvaluator<WorkItem>`; old `IsItemInList` path removed | 2026-07-04 09:16 | 2026-07-04 09:21 | 2026-07-04 09:21 |
| 01-05 | Migration integration + real-provider test | 2026-07-04 18:28 | 2026-07-04 18:32 | 2026-07-04 18:34 |
| 01-06 | RBAC guard, validation (MaxRules, unknown field), FE badge/wire-up | 2026-07-04 18:42 | 2026-07-04 18:46 | 2026-07-04 18:50 |

## Mutation Testing

- **Stryker.NET 4.15.0** | Feature-surface mutants: 40 killed, 9 survived (tolerated), 40 no-coverage
- **Kill rate: 81.6%** — exceeds the 80% threshold ✅
- All surviving mutants classified as acceptable: null-guard equivalence, premium-feature gating, string-trivial equivalence, logical-not-of-initial-value

## Quality Gates

| Gate | Result | Evidence |
|------|--------|----------|
| Backend ATs (slice 01 walking skeleton) | ✅ GREEN | 1 active, 8 `[Ignore]`-pending for slice 01 |
| Mutation test (slice 01 feature surface) | ✅ 81.6% killed | Per-file breakdown in mutation report |
| Manual testing | ✅ Complete | Confirmed by user |

## Lessons Learned / Issues Encountered

- **Manual testing**: Completed successfully; no blocking issues encountered during manual verification
- **Pre-slice-05 SPIKE required**: ADR-071 confirmed that slice 05 (predefined/system flagged field) requires a dedicated SPIKE (~half-day) before sizing/commitment. Does not block slices 01–04.
- **UC-2 (per-type historical filter)**: Deferred — forward-only `BlockedCountSnapshot` stores total count per owner per day; per-type breakdown is additive follow-up, no contract break.
- **Real-provider migration tests**: Required for every slice touching schema (slices 01–04). InMemory-only coverage fails the gate.

## Links to Migrated Artifacts

| Artifact | Location |
|----------|----------|
| Feature delta (DISCUSS + DESIGN + DISTILL) | `docs/architecture/epic-5074-blocked-items/feature-delta.md` |
| Upstream changes (DESIGN back-propagation) | `docs/architecture/epic-5074-blocked-items/upstream-changes.md` |
| RED classification (DISTILL) | `docs/scenarios/epic-5074-blocked-items/red-classification.md` |
| Upstream issues (DISTILL back-propagation) | `docs/scenarios/epic-5074-blocked-items/upstream-issues.md` |
| Slice 01 definition | `docs/scenarios/epic-5074-blocked-items/slice-01-rule-based-blocked.md` |
| Mutation test report (slice 01) | `docs/evolution/epic-5074-blocked-items/mutation-report-slice-01.md` |

## ADRs Created

| ADR | Title | Status |
|-----|-------|--------|
| ADR-067 | Rule-based blocked definition + storage + migration | Accepted |
| ADR-068 | Blocked-transition capture entity + WorkItemUnblocked | Accepted |
| ADR-069 | Blocked-count snapshot + endpoint | Accepted |
| ADR-070 | Blocked→stale (AMENDS ADR-026) | Accepted |
| ADR-071 | Predefined/system additional field + SPIKE | Accepted |
| ADR-072 | Contract changes + client version-gate matrix | Accepted |

## Wave Decisions Source Files

All wave decisions are consolidated in `docs/architecture/epic-5074-blocked-items/feature-delta.md` (no separate `*/wave-decisions.md` files exist — the feature-delta serves as the single source of truth for DISCUSS/DESIGN/DISTILL decisions).

## What's Next

Slices 02–05 remain in the `docs/feature/epic-5074-blocked-items/slices/` workspace for future implementation:
- **Slice 02**: Blocked-time capture + per-item duration
- **Slice 03**: Blocked-over-time chart
- **Slice 04**: Blocked→stale staleness trigger
- **Slice 05**: Jira flagged-field cleanup (SPIKE-gated)
