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
