# Slice 03 — S03a Package WorkTracking-Integration

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L**

## Goal

Move the anti-corruption layer in front of Jira / ADO / Linear / CSV / ServiceNow into its own context folder.

## IN

- The `Services/*/WorkTrackingConnectors/**` folders: connectors, auth strategies, `WorkItemCategoryCrossing`, mappers, `WorkTrackingSystems` if S04a has not already moved it to the kernel.
- `Factories`, plus the context's slice of `OAuth` if S01 assigns it here.
- The context-owned DTOs and types in `Models`, e.g. `ConnectionValidationResult`, connection option types. Move them only after S12c if any DTO computes values.
- The "WorkTracking-Integration is a leaf input adapter" ArchUnit rule, restated by namespace.

## OUT

- Capability interfaces (S06). Connection ports (S05e).

## Hypothesis

The ACL can live in one folder with no inward dependency. **Disproved if** the move reveals dependencies on Forecasting, Portfolio/Delivery or Sync that the existing rule missed.

## Acceptance criteria

- [ ] No file of this context remains in `Services/Interfaces` or `Services/Implementation`. Interfaces are co-located with their implementations.
- [ ] The leaf-adapter rule holds by namespace. Context-owned `Models` types have moved.
- [ ] Moves separate from edits. ARCHITECTURE.md §4 row, anchors and diagram updated.
- [ ] Behaviour-free, connector tests green (run the live-connector categories deliberately, since connectors are moved), mutation score held.

## Dependencies

- Hard: S01, S02 gate.
- Soft: S06 (doing S06 first means fewer, cleaner interfaces to move), S04a.

## Notes for DESIGN

- Moving connectors triggers CI's path-based connector test selection, so expect the live suites to run.
- Watch the Linear rate limit (its key is shared with CI).

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (e.g. one connector folder per slice).
