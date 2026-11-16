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
- [ ] Namespace rule plus the preserved seam rules green. ~~Context-owned `Models` types have moved.~~
  *Rewritten at close-out (maintainer, 2026-11-15, DESIGN hold 1):* no `Models` type moves; the list of Work
  Items-owned `Models` types is handed to S04b. They cannot move before S12c and S04b, because kernel types and the EF
  model snapshots name them.
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

## Close-out (Story 6360, 2026-11-16)

Delivered in three nwdes features; see `docs/evolution/2026-11-16-story-6360-s03b-package-work-items/evolution.md`.

- **No file of this context remains in `Services`**, with stated exceptions. The parent and dependency source
  selectors (`ParentSourceSelector`, `DependencySourceSelector`, `DependencyFieldReferences`) stay, because the
  connectors call them and a connector may not reach Work Items; they go with Tracker integration (S03a). The
  orphan clean-up pair stays until it reads Features through a repository port.
- **The refresh pipelines** (`TeamUpdater`, `PortfolioUpdater`) stay in the update code; Epic 6474 slice 12 replaces
  them with one `UnitUpdater` in `Modules/WorkItems`.
- **Hypothesis partly disproved:** no Forecasting or Portfolio service edge, but Flow metrics (now 2 query edges, the
  metrics update became an event), Access, Licensing, Organisation set-up and the update queue, each listed.
