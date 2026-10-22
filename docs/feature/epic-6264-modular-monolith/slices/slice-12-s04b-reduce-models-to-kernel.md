# Slice 12 — S04b Reduce `Models` to the shared kernel + kernel-size rule

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M** · Pick-up group G4 (end state)

## Goal

`Models` holds only what S01's ADR allows in the shared kernel (shared IDs, value objects, domain-event base types), and a rule keeps it that way.

## IN

- Move every remaining context-specific type out of `Models` (242 files on 2026-10-22) to its owning context.
- Add an ArchUnit kernel rule, e.g. an allow-list, or "no type in `Models` is used by only one context".

## OUT

- Contracts namespaces (S09a).

## Hypothesis

After S03*, the residue in `Models` is small and clearly shared. **Disproved if** many types are shared by exactly two contexts. That would signal a missing context or a contract, which goes back to S01 and S09a.

## Acceptance criteria

- [ ] Every type in `Models` is on the ADR's allowed list.
- [ ] Kernel rule green, and it fails naming any context-specific type added to `Models`.
- [ ] ARCHITECTURE.md §4 "Models — the shared kernel" text updated. Behaviour-free.

## Dependencies

- Hard: all S03*, S04a.

## Notes for DESIGN

Decide whether domain-event records stay in the kernel or move to each publisher's `Contracts`. Coordinate with S09a.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.
