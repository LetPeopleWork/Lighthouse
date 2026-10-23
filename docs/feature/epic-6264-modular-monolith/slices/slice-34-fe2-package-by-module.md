# Slice 34 — FE2 Package the frontend by module (pilot: Delivery)

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L** (pilot); further modules become their own Stories, split at this DESIGN

## Goal

The frontend mirrors the backend's bounded contexts. Delivery is the pilot, then one Story per further module.

## IN

- Delivery's ~40 files, today spread over `models/Delivery*`, `services/Api/DeliveryService.ts`, `pages/Portfolios/Detail/Components/DeliveryGrid` (31), `components/Common/DeliveryRuleBuilder`, `utils/deliveries`. They move into the S01 frontend layout (e.g. `src/modules/delivery/`) with one public entry.
- Extend FE1: from outside, only a module's public entry is importable. Same PR.
- List the follow-up module Stories.
- Context: the frontend is layer-first today. `components/Common` holds 308 of 375 component files, and `pages/Common` holds 166.

## OUT

- Splitting large units (FE5). Per-module API ports (FE4). Both are soft precursors.

## Hypothesis

The S01 layout fits the frontend. **Disproved if** shared UI pieces (`DataGridBase`, `ItemListManager`) resist a home. Then S01 needs a "shared UI" band, recorded as an ADR amendment.

## Acceptance criteria

- [ ] Delivery files under the module folder. Move commits separate from edits.
- [ ] FE1 public-entry rule green for Delivery.
- [ ] Pixel- and behaviour-preserving: Vitest, Playwright and `@screenshot` unchanged and green.
- [ ] ARCHITECTURE.md §12 describes the layout.

## Dependencies

- Hard: S01 (layout covers frontend), FE1.
- Soft: FE4, FE3.

## Notes for DESIGN

Run Playwright locally before committing any moved page-object locators. E2E goes through page objects.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN.
