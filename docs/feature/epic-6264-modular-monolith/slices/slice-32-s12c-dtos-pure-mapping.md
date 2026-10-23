# Slice 32 — S12c DTOs become pure mapping

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M** · **Must precede any S03* that moves the affected DTOs**

## Goal

DTOs hold data only. Derived values (age, blackout-adjusted figures, blocked state) are computed on the application or read side and passed in as plain values.

## IN

- `FeatureDto` and `WorkItemDto` (both S107-suppressed today), plus any other DTO DESIGN finds that computes values.
- A small read model or factory per DTO family that computes the derived values. Unit-tested without HTTP.

## OUT

- Moving DTOs into context contracts (S03*).

## Hypothesis

Derivations are few and pure. **Disproved if** they need services the read side cannot reach cleanly. Then the read model becomes a query port (S12a pattern).

## Acceptance criteria

- [ ] Both S107 suppressions removed. DTO constructors take plain values only.
- [ ] Serialized JSON byte-identical (response snapshots), E2E green.
- [ ] Derivation logic unit-tested. Mutation score held.

## Dependencies

- None hard. It is a hard precondition for S03* DTO moves.

## Notes for DESIGN

Enums serialise as strings out and ints in. Keep that exactly. The instance calendar (Bug #5567) now arrives with the read model rather than the DTO.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (one DTO family per slice).
