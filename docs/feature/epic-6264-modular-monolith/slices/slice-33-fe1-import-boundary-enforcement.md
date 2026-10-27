# Slice 33 — FE1 Import-boundary enforcement + ratcheting allow-list

Epic #6264 · `@infrastructure` · job_id: infrastructure-only · **Size S–M** · Pick-up group G0 (should go first on the frontend)

## Goal

Layer leaks in `Lighthouse.Frontend/src` fail the build and name the file. Today's leaks sit on an allow-list that only shrinks.

## IN

- Tool, decided at DESIGN: Biome `noRestrictedImports` in the existing `Lighthouse.Frontend/biome.json`, dependency-cruiser, or [Fallow](https://fallow.tools/) (see Notes for DESIGN). Biome already runs in `prebuild` with the Sonar-mirroring rules; there is still no ESLint. The check must run inside `pnpm build` (the `prebuild` hook) or `pnpm test`, so CI enforces it.
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

- **Fallow is a third candidate** (maintainer, 2026-10-27). It is an MIT-licensed analyser for TypeScript and JavaScript that builds one module graph and reports boundary violations, import cycles, unused files, exports and dependencies, duplication and complexity. It runs as `npx fallow` with an optional `.fallowrc.json`, and has plugins for Vite, Vitest, Playwright, pnpm, Biome and TypeScript path aliases. Boundaries are zones made of globs, with rules for which zone may import which; it has presets (`layered`, `hexagonal`, `feature-sliced`) and `autoDiscover`, which makes every child folder its own zone. That last one is a fit for FE2's module public entries. Its dead-code, cycle and duplication reports are something neither Biome nor dependency-cruiser gives us.
- Settle these for Fallow with a short read-only trial on `Lighthouse.Frontend` before choosing it:
  1. **The named list.** Its docs show boundary exceptions only as inline `fallow-ignore` comments, not as a central list. FE1 needs one list in one place that fails in both directions and only shrinks, like the backend ratchets. Committed baselines exist for dead code, duplication and health; check whether they cover boundary violations, and whether a fixed entry fails until it is pruned.
  2. **Does it find the same 16 violations** as the survey above?
  3. **Noise.** How many dead-code findings does a first run give, and how many are false?
  4. **Speed.** Can it run inside `prebuild` without slowing the build beyond noise?
  5. **Maturity.** It comes from a small company founded in 2026, so check the version, release cadence and adoption on its GitHub repo (`fallow-rs/fallow`) and changelog. It would be a new dev dependency in every CI run, under pnpm's minimum release age.
  6. **Overlap with Biome.** If it is chosen, it gets a clear job next to Biome rather than reporting the same things twice. Its `fallow mutation` is not needed; Stryker already does that.
- If Fallow passes for boundaries but not for the named list, a thin test can still own the list. It reads Fallow's machine-readable output and compares it with an inline list, the way the backend ratchets compare a scan with theirs.

## Slicing

Carpaccio slicing happens at this Story's DESIGN (tool + one rule first, then the rest).
