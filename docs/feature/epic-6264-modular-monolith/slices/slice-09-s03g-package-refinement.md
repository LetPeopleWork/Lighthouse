# Slice 09 — S03g Package Refinement

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S**

## Goal

Move Refinement, the context that already follows the target shape, into its own folder.

## IN

- `Services/*/Refinement/**`. *Amended at DESIGN (Story 6365):* `Models/Refinement` (EF-mapped or on the wire) stays in the kernel, and the sizing-log repository pair stays with the platform repositories until S05.
- The existing "depends down only, nothing but API depends on it" rules, restated by namespace.

## OUT

- Any Refinement behaviour (presenter mode and calibration are other Epics).

## Hypothesis

A context that is already clean packages with namespace changes only. **Disproved if** it needs edits. That would point at a flaw in the S01 layout.

## Acceptance criteria

- [x] No Refinement file remains in `Services/Interfaces` or `Services/Implementation` other than the sizing-log repository pair; `Models/Refinement` stays in the kernel (amended at DESIGN, Story 6365).
- [x] Down-only and only-API-depends rules green by namespace (`RefinementModuleBoundariesArchUnitTest`, R-R1 to R-R6).
- [x] ARCHITECTURE.md §4 Refinement row anchors updated. Behaviour-free, tests green.

## Dependencies

- Hard: S01, S02 gate.

## Notes for DESIGN

A good early follow-up to the pilot: low risk, and it confirms the layout on a context that has a write side (`SizingLogCommands`).

## Done

Story 6365, 2026-11-24: three nwdes features (rules first, the move, edges and rules). The hypothesis held: every moved file changed only in its namespace and `using` lines.

## Slicing

Carpaccio slicing happens at this Story's DESIGN. It is likely a single slice.
