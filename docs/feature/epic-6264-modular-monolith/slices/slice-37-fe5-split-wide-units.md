# Slice 37 — FE5 Split wide units

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size L** (split per unit at DESIGN)

## Goal

No unjustified production file over 1000 lines, and the duplicated RBAC handlers become one shared hook.

## IN

- `BaseMetricsView.tsx` (2034 lines, 87 imports) and `hooks/useMetricsData.ts` (970).
- `ModifyConnectionSettings.tsx` (1201).
- `DeliverySection` (993) and `DeliveryCreateModal` (914).
- The RBAC member and group-mapping handlers duplicated in `TeamDetail.tsx` (≈ l.144–211) and `PortfolioDetail.tsx` (≈ l.114–185) become one shared hook, still going through `useRbac` / `RbacService`.
- DESIGN counts the exact > 1000-line baseline.

## OUT

- Behaviour or layout changes. No TanStack Query migration.

## Hypothesis

Each unit splits by responsibility without visible change. **Disproved if** a split needs a visible change. Then sketch it and ask the maintainer before building (CLAUDE.md).

## Acceptance criteria

- [ ] Production files > 1000 lines: 0, or each justified in writing.
- [ ] Shared RBAC-handler hook. All gating via `useRbac()`, and no direct `/api/latest/authorization/my-summary` fetch.
- [ ] `@screenshot`, Playwright and Vitest unchanged and green. Stryker score held.

## Dependencies

- Soft: FE1, FE4.

## Slicing

Carpaccio slicing into ≤ 1-day slices happens at this Story's DESIGN (one unit per slice).
