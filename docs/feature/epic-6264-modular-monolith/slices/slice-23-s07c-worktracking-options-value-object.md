# Slice 23 — S07c Evaluate `WorkTrackingSystemOptionsOwner` → `WorkTrackingOptions` value object

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S** (evaluation) / **M** (if adopted)

## Goal

Decide whether Team and Portfolio should hold a `WorkTrackingOptions` value object instead of inheriting from `WorkTrackingSystemOptionsOwner`, and do it if the answer is yes. **"No" is a valid outcome.**

## IN

- Weigh the EF mapping (owned type vs current columns), the migration cost (expand-only, via `CreateMigration`), and the callers that treat Team and Portfolio polymorphically (e.g. `WorkTrackingSystemConnectionUsage`).
- Record the decision with reasons, as an ADR or in S08's catalogue.

## OUT

- Other aggregate reshaping (S08 follow-ups).

## Hypothesis

A value object models "tracker options of an owner" better than inheritance. **Disproved if** the EF and migration cost outweighs the benefit, or polymorphic callers rely on the base type.

## Acceptance criteria

- [ ] A written decision with reasons.
- [ ] If adopted: no schema change unless it is expand-only, behaviour-free, and tests and mutation score held.

## Dependencies

- Soft: S08 catalogue, S05c/S05d.

## Slicing

Carpaccio slicing happens at this Story's DESIGN (evaluation first).
