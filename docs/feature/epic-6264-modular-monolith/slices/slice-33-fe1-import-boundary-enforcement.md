# Slice 33 — FE1 Import-boundary enforcement + ratcheting allow-list

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S–M** · Pick-up group G0 (should go first on the frontend)

## Goal

Layer leaks in `Lighthouse.Frontend/src` fail the build and name the file. Today's leaks sit on an allow-list that only shrinks.

## IN

- Tool, decided at DESIGN: Biome `noRestrictedImports` via a new `biome.json`, or dependency-cruiser. There is no `biome.json` and no ESLint today (verified 2026-10-22). It must run inside `pnpm build` (the `prebuild` hook) or `pnpm test`, so CI enforces it.
- Rules: components ↛ pages, hooks ↛ pages/components, services ↛ hooks, models ↛ pages, utils ↛ services.
- Allow-list baseline of **16** (survey 2026-10-22, re-counted by the tool at DESIGN):
  - components → pages (7): ModifyConnectionSettings → pages/Settings/Connections/*; ModifyTeamSettings → pages/Teams/Edit/ForecastSettingsComponent; RefinementBandSettings → pages/Teams/Detail/Refinement/*; ModifyProjectSettings → pages/Common/AdvancedInputs; DateRangeSelector and DateRangePresets → pages/Common/MetricsView/dateWindow; ReportsChips → pages/Common/Reports/*.
  - hooks → pages/components (5): useMetricsData, useCreateWizard, useRuleRowDraft, useUsageDataConsent.
  - services → hooks (2): services/UsageData/* → hooks/useUsageDataConsent.
  - models → pages (1): models/Metrics/InfoWidgetData.ts → pages/Common/MetricsView/trendTypes.
  - utils → services (1): deliveryArchivedRefusal → ApiError.

## OUT

- Fixing the 16 (FE2/FE3/FE5 shrink the list). Module boundaries (added by FE2).

## Hypothesis

A file-naming allow-list stops new leaks without slowing work. **Disproved if** contributors keep adding entries. That means a missing shared place, which FE2/FE3 then create.

## Acceptance criteria

- [ ] Given a new file in `components/` imports from `pages/`, when `pnpm build` runs, then it fails naming the file and the import.
- [ ] Given a listed file stops leaking, then the check fails until it is removed from the allow-list.
- [ ] Biome stays at zero errors and warnings. Build time within noise.

## Dependencies

None.

## Notes for DESIGN

- `prebuild` runs `biome check --write`, not read-only, so mind the docs-symlink hazard.
- A new dev dependency (dependency-cruiser) must respect pnpm's minimum release age, and overrides live in `pnpm-workspace.yaml`.

## Slicing

Carpaccio slicing happens at this Story's DESIGN (tool + one rule first, then the rest).
