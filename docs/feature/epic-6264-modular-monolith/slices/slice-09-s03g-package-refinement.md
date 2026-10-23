# Slice 09 — S03g Package Refinement

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S**

## Goal

Move Refinement, the context that already follows the target shape, into its own folder.

## IN

- `Services/*/Refinement/**`, `Models/Refinement/**`, the sizing-log repository if S01 assigns it to the owning context.
- The existing "depends down only, nothing but API depends on it" rules, restated by namespace.

## OUT

- Any Refinement behaviour (presenter mode and calibration are other Epics).

## Hypothesis

A context that is already clean packages with namespace changes only. **Disproved if** it needs edits. That would point at a flaw in the S01 layout.

## Acceptance criteria

- [ ] No Refinement file remains in `Services/Interfaces` or `Services/Implementation`.
- [ ] Down-only and only-API-depends rules green by namespace.
- [ ] ARCHITECTURE.md §4 Refinement row anchors updated. Behaviour-free, tests green.

## Dependencies

- Hard: S01, S02 gate.

## Notes for DESIGN

A good early follow-up to the pilot: low risk, and it confirms the layout on a context that has a write side (`SizingLogCommands`).

## Slicing

Carpaccio slicing happens at this Story's DESIGN. It is likely a single slice.
