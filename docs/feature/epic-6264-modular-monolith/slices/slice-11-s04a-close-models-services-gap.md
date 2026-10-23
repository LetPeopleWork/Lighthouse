# Slice 11 — S04a Close the `Models ↛ Services` gap

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S** · Pick-up group G0 (independent, can go first)

## Goal

Make the shared kernel independent of the application core, and enforce it.

## IN

- Relocate `IEntity` (`Services/Interfaces/IEntity.cs`) and `WorkTrackingSystems` (`Services/Implementation/WorkTrackingConnectors/WorkTrackingSystems.cs`) into `Models`.
- Add the ArchUnit rule `Models ↛ Services` in the same PR. Fix any other violation it finds, or baseline it by inline file list.
- Delete the "known, documented gap" note in ARCHITECTURE.md §4.

## OUT

- Shrinking `Models` (S04b).

## Hypothesis

Only these two types keep the gap open. **Disproved if** the new rule finds more violations. Then baseline them by file list and record each one.

## Acceptance criteria

- [ ] Given the two types are in the kernel, when `dotnet test --filter FullyQualifiedName~ArchitectureTests` runs, then `Models ↛ Services` is green.
- [ ] Given a kernel type later references `Services.*`, then the rule fails naming it.
- [ ] Behaviour-free. `WorkTrackingSystems` keeps its integer values, since it is persisted and serialised (strings out, ints in).

## Dependencies

None.

## Notes for DESIGN

The move touches many `using` lines. Keep it a pure move commit, then the rule commit.

## Slicing

Carpaccio slicing happens at this Story's DESIGN. It is likely one or two slices.
