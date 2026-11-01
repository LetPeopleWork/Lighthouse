# Slice 04 — S03b Package WorkItems / Sync

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L**

## Goal

Move the sync context (Work Item refresh, sync delta, state-transition capture, Team data) into its own folder.

## IN

- `Services/*/WorkItems`, `TeamData`, `WorkItemRules`, `Dependencies`/`Parents` if S01 assigns them here.
- `SyncModeResolver`, `FetchFingerprint`, `BlockedItemService`.
- Context-owned `Models` types (sync outcome, transitions, rule types). DTOs only after S12c.
- Seam rules that name sync types ("`WorkItemService` is the sole writer of transitions") restated by namespace.

## OUT

- Splitting `WorkItemService` (S07a; ideally done first).

## Hypothesis

Sync can be one folder that depends only on WorkTracking-Integration and the kernel. **Disproved if** it pulls in Forecasting or Portfolio types.

## Acceptance criteria

- [ ] No file of this context remains in `Services/Interfaces` or `Services/Implementation`.
- [ ] Namespace rule plus the preserved seam rules green. Context-owned `Models` types have moved.
- [ ] Moves separate from edits. ARCHITECTURE.md §4 updated.
- [ ] Behaviour-free, tests green, mutation score held.

## Dependencies

- Hard: S01, S02 gate. S12c before moving `WorkItemDto`.
- Soft: S07a, S03a.

## Notes for DESIGN

This is the busiest context. Domain events published from here (`WorkItemTransitioned` and the like) stay in the kernel's event namespace unless S01 says otherwise.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.

## Update after S01 (2026-11-01, ADR-270 accepted)

Retitled **Package Work Items**. ADR-270: this context owns the items mirrored from the trackers (one item model with a level, target), their transitions, blocked periods, parent links, roll-up and default-size guess; sync is its process. Ratchet candidates: Sync calls Flow metrics inline (`TeamDataService.cs:23` calls `UpdateTeamMetrics`); the Portfolio refresh triggers Promises and Forecasting directly (`PortfolioUpdater.cs:101,106,124`). Those crossings become reactions to an event; the steps inside Work Items may stay an ordered pipeline.
