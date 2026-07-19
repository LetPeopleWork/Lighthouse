# Slice 01 — Stop demo blocked history corrupting team blocked reads

**Story**: US-01 | **ADO**: #5524 | **Effort**: ~0.4 crafter-day

## Goal

Withdraw the demo backfill's `Feature.Id` writes into `WorkItemBlockedTransition` so a team work item's historic blocked answer comes only from its own history.

## Why first

Live correctness defect on a shipped story (#5508), and the cheapest possible disproof of the shared-table option that D3 already rejects. If it turns out the collision is unreachable, DESIGN needs to know that before slice 02 builds a whole keyspace on the premise.

## IN scope

- Remove the transition writes from `DemoBlockedHistoryBackfillHandler`'s portfolio path (`BackfillAsync` → `UpsertBackdatedTransition`) when the owner is a Portfolio.
- **Retain** the portfolio `BlockedCountSnapshot` backfill — the demo trend chart must keep working (US-01 AC4).
- Repository-level invariant test: no `WorkItemBlockedTransition.WorkItemId` without a corresponding `WorkItem`.
- Integration test reproducing the collision: Feature id N + never-blocked WorkItem id N, past range, assert `isBlocked = false`.

## OUT of scope

- Creating the feature keyspace — slice 02.
- Re-adding demo feature history — slice 05 (deliberately after the keyspace exists).
- Cleaning up already-written bad rows in existing demo databases. Demo data is reloaded, not migrated; if DESIGN disagrees, this becomes an expand-only data migration in slice 02.
- Any change to team capture, the team read path, or `IBlockedItemService`.

## Amendment (DESIGN, 2026-07-19) — this slice is strictly better than written

`WorkItemBlockedTransition` has an **enforced FK to `WorkItems`** (cascade delete, both providers). So when a
`Feature.Id` has no matching `WorkItem`, the write raises `DbUpdateException`, the dispatcher swallows it, and
`BackfillAsync` aborts **before** `snapshotRepository.Save()` — meaning the backdated portfolio *snapshots*
never land either.

Removing the transition writes therefore does not merely stop corruption: it lets the portfolio snapshot
backfill complete for the first time. AC4 changes from "still renders" to "renders backdated history it
previously could not". Verify that stronger claim.

The reason this shipped green: `DemoBlockedHistoryBackfillHandlerTests` uses `UseInMemoryDatabase`, which
**does not enforce foreign keys**. Any test asserting this fix must run against SQLite, not InMemory, or it
will pass for both the broken and the fixed code. This is the single most important thing to get right in
this slice.

## Learning hypothesis

**Hypothesis**: `Feature.Id` / `WorkItem.Id` collision is reachable in practice and does corrupt the #5508 historic team read.

- **Confirmed if** the collision integration test fails before the change and passes after.
- **Disproved if** the test cannot be made to fail — meaning some scoping exists that this analysis missed. Then D3's premise is weaker than stated and DESIGN must be told before slice 02 commits to a separate keyspace.

Either outcome is worth the half day; the disproof is the more valuable one.

## Acceptance criteria

See US-01 AC1–AC4 in `../feature-delta.md`. Summary: colliding id absent from the team historic read (AC1) and from `blockedItemsAtDate` (AC2); repository invariant holds (AC3); demo portfolio trend chart still renders (AC4).

## Dependencies

None. Can start immediately.

## Reference class

`DemoBlockedHistoryBackfillHandler` was itself built in epic-5074 slice-07 in well under a day. This is a subtraction from it plus two tests.

## Risks

- **The demo portfolio experience regresses** between this slice and slice 05: the trend renders but bars drill into an empty dialog. Acceptable and time-boxed — an empty dialog is honest, a phantom blocked item is not. Do not close out the feature with slice 05 unshipped.
- The dispatcher swallows handler errors, so a broken backfill fails silently. Assert the *outcome* (rows present/absent), never that no exception was thrown.

## Notes for the crafter

- The portfolio branch is `DemoBlockedHistoryBackfillHandler.cs:79`; the shared `BackfillAsync` is `:106`. The split is by `OwnerType`, already a parameter — no new plumbing needed.
- `RepositoryBase.GetByPredicate` and `Exists` use `SingleOrDefault` and will throw on multiple matches. Use `GetAllByPredicate(...).Any()` for any invariant scan.
