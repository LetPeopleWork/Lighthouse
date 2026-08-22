# Slice 01b — Create a Delivery from a Jira Release

**Goal**: save the previewed Release as a Delivery, bound to it, with name, date and Features owned by
Jira.

**Story**: US-02.

## IN scope

- `DeliverySelectionMode` gains one **appended** member (S3 — the enum is int-persisted with no
  conversion; inserting anywhere but the end silently repoints every stored Delivery).
- Two additive columns on `Delivery`: the handler key and the source reference — the Version **id**, never
  its name (D3.3).
- Extend the create and update endpoints to accept the source-bound payload. Manual and rule payloads
  unchanged.
- Read-only rendering with provenance on a bound Delivery: name, date and Feature list each say where
  they came from.
- Unbind: back to Manual, retaining the last synced name, date and Features, all editable again.
- EF migration via the existing `CreateMigration` PowerShell script (all providers), expand-only.

## OUT of scope

- Re-syncing. Values are captured at bind time and do not yet follow Jira — that is slice 02. Say so in
  the UI if it is not obvious within one slice's gap.
- Bulk import of Releases as Deliveries.
- Anything outbound.

## Learning hypothesis

**Disproves D4's read-only stance** if the first person to bind a Delivery immediately wants to add a
Feature the Release does not carry. If that happens, D4 needs revisiting before slice 02 builds on it —
cheaper now than after re-sync exists.

**Confirms** that a Delivery identified by a remote object is a coherent thing to have in the grid
alongside hand-made ones.

## Acceptance criteria

AC-02.1 through AC-02.5 in `feature-delta.md`. The two that carry the slice:

- The stored reference is the Version **id**, not the name, so a Jira-side rename leaves the binding
  resolvable. What a rename does to the displayed name is AC-03.8 and needs slice 02's refresh path.
- Editing an existing Manual or Rule-based Delivery is byte-identical to before the Epic.

## Dependencies

Slice 01a.

## Effort

~5 hours, of which the migration and its all-provider generation is the fixed cost.

## Watch

The migration DLLs are HintPath references — build them before `dotnet ef`, or the tooling cannot load
them and reports pending model changes that are not real.
