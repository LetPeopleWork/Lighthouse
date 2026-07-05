# Slice 02: Blocked-Time Capture — Evolution Summary

**Feature**: epic-5074-blocked-items  
**Date**: 2026-07-05  
**Baseline**: `b1c78622` (slice 02 roadmap)

## Summary

Implemented blocked-time capture infrastructure and end-to-end `blockedSince` surface across backend and frontend. Six DELIVER steps + two mutation-test augmentation steps.

## Business Context

Epic 5074 (rule-based blocked-items identification) required tracking how long items have been blocked. Slice 02 adds the capture substrate (`WorkItemBlockedTransition` entity), emits a `WorkItemUnblocked` event when an item is no longer blocked, and surfaces a `blockedSince` duration on team board WIP cards.

## Key Decisions

- ADR-068: `WorkItemBlockedTransition` as owned entity (not reused `WorkItemStateTransition`). At most one open spell per item.
- Enter seam: existing `WorkItemBlocked` event from slice 01.
- Leave seam: new `WorkItemUnblocked` event emitted symmetrically at `WasBlockedBeforeSync && !IsBlocked` in `WorkItemService`.
- Idempotent handlers: capture handler checks for existing open spell before creating a new one.

## Work Completed

| Step | Commit | Summary |
|------|--------|---------|
| 02-01 | `3eb72a34` | `WorkItemBlockedTransition` entity, capture/close handlers, repository, migrations (SQLite + Postgres) |
| 02-02 | `c376f717` | `blockedSince` on `WorkItemDto`, WIP endpoint population, first acceptance scenario enabled |
| 02-03 | `3725630a` | Second acceptance scenario: unblocked item shows no blocked duration (implicitly satisfied) |
| 02-04 | `5756419c` | Third acceptance scenario: first-observation guard (implicitly satisfied) |
| 02-05 | `bfe5479d` | Fourth acceptance scenario: non-blocked items have no blockedSince (implicitly satisfied) |
| 02-06 | `20242ff2` | Frontend "Blocked for Xd Yh" badge/tooltip on WIP cards |
| 02-MUTATION-BACKEND | `78bfa16c` | Test augmentation: handler idempotency/log, WorkItemDto, leave-detection, WIP blockedSince |
| 02-MUTATION-FRONTEND | `7fbfa326` | Test augmentation: edge-case blockedSince inputs, SLE boundaries, component null-guard fix |

## Mutation Testing

| Stack | Score | Verdict |
|-------|-------|---------|
| Backend (tested-mutant kill rate) | 94.12% (16/17) | Accepted — NoCoverage dominated by test filter scope |
| Frontend | 58.86% | Accepted — `blockedDuration.ts` at 88.24%, structural MUI survivors in `WorkItemsDialog.tsx` |

Full reports: `docs/feature/epic-5074-blocked-items/deliver/mutation/`

## Issues Encountered

- DES CLI tools (`des-roadmap`, `des-log-phase`, `des-commit`) had broken Python imports — fixed by adding `.pth` file to nwave-ai venv
- `des-roadmap init` remained broken even after fix; roadmap created manually
- Stryker test filter scoping: slice-2-scoped filter excluded broader test classes (`WorkItemServiceTest`, `TeamMetricsControllerTest`) causing false NoCoverage on files that were only partially touched by slice 2

## Migrated Artifacts

- Architecture: `docs/architecture/epic-5074-blocked-items/adr-068-blocked-transition-capture-and-unblocked-event.md`
- Evolution: `docs/evolution/2026-07-05-epic-5074-blocked-items-slice-01.md` (prior slice)
- Scenarios: `docs/scenarios/epic-5074-blocked-items/` (acceptance test docs)
- Mutation reports: `docs/evolution/epic-5074-blocked-items/mutation-report-slice-01.md` (prior slice)

## Git History

```
7fbfa326 test(epic-5074): augment slice 2 frontend tests to kill mutation survivors
78bfa16c test(epic-5074): augment slice 2 backend tests to kill mutation survivors
20242ff2 feat(epic-5074): add blocked duration badge/tooltip to WIP card
bfe5479d feat(epic-5074): confirm non-blocked items have no blocked duration
5756419c feat(epic-5074): null blockedSince on first observation before baseline
3725630a feat(epic-5074): enable unblocked item scenario — no blocked duration shown
c376f717 feat(epic-5074): expose blockedSince on WorkItemDto and enable first acceptance scenario
3eb72a34 feat(epic-5074): add WorkItemBlockedTransition entity, capture/close handlers, and migrations
```
