# Slice 35 — FE3 Shrink `src/models`

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size M–L**

## Goal

`src/models` becomes the frontend's small shared core. Module-owned API types live with their module.

## IN

- Today `src/models` has 82 production files (verified 2026-10-22) and 317 importing files (survey). It mixes:
  - zod wire schemas (24)
  - view-logic classes (17)
  - request/response types
  - UI row types
  - `IDemoDataService`
- Move module-owned types to their module (FE2 folders, or interim per-module folders).
- Keep a small shared core: WorkItem, Feature, EntityReference, IFeatureOwner. DESIGN confirms the list.
- Add an FE1 rule that keeps `models` to the core list.

## OUT

- Changing zod schemas' behaviour. They stay at the trust boundary (ARCHITECTURE.md §12, schema-first).

## Hypothesis

Most of `models` is owned by one module. **Disproved if** many types are shared by two or more modules. That signals a contract, which goes to the S01 ADR.

## Acceptance criteria

- [ ] `src/models` holds only the confirmed core. `IDemoDataService` moved out.
- [ ] FE1 rule for `models` in the same PR. The `models → pages` allow-list entry gone.
- [ ] Behaviour-free, tests green, Stryker score held on touched files.

## Dependencies

- Hard: FE1.
- Soft: FE2 per module, S01.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (one model folder per slice).
