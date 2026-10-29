# Story 6389 — FE1 Import-boundary enforcement + ratcheting allow-list

Epic #6264 (path to a modular monolith) · workspace `docs/feature/epic-6264-modular-monolith/` · brief
`docs/feature/epic-6264-modular-monolith/slices/slice-33-fe1-import-boundary-enforcement.md` · ADO #6389 ·
picked up 2026-10-29 via `/continuous-improvement`.

DISCUSS: covered at Epic level (see the Epic's Wave Decisions, D-6264-3); waves start at DESIGN.

## Wave: PICK-UP / [REF] Inherited

Read these in the Epic's `feature-delta.md`; they are not copied here.

- `[REF] Story Map`: pick-up group G0; the dependency graph (`FE1 --> FE3, FE4, FE5`; `FE1 ==> FE2`).
- `[REF] Stories` → `#### FE1 — Import-boundary enforcement + ratcheting allow-list` (the AC).
- `[REF] How to pick up a Story`: the per-Story checklist.
- `[REF] Cross-Cutting Impact Checklist`: RBAC, Lighthouse-Clients, Website, usage data, UI sketches, Terminology.
- `[REF] Outcome KPIs + guardrails`: KPI 8 (frontend import-boundary violations), KPI 12 (mutation).
- `[REF] Wave Decisions`: D-6264-3/4/5 (per-Story waves, carpaccio at DESIGN, slice-composition gate waived), D-6264-8 (frontend track in).
- `[REF] Risks / Open questions`: item 10 (Biome `noRestrictedImports` vs dependency-cruiser; Fallow added by the brief since).
- `[REF] Improvement log`: the S04a/S05a entries. ADR-268's ratchet convention applies: offenders listed by name,
  failing in both directions, only ever shrinking.
- ADR: `docs/product/architecture/adr-268-a-ratchet-guard-lists-its-offenders-by-name-in-both-directions-and-only-ever-shrinks.md`.
  S01's bounded-context ADR does not exist yet; FE1 sets no module boundaries (that is FE2), so it does not need it.

## Wave: PICK-UP / [REF] DoR re-run (2026-10-29)

| DoR item | Status | Evidence for FE1 |
|---|---|---|
| 1 Problem statement, domain language | PASS | 16 production files leak across layers today, and nothing stops a 17th. |
| 2 Persona with specifics | PASS | `lighthouse-maintainer` and the CI build. |
| 3 ≥ 3 domain examples, real data | PASS | Re-counted list below, e.g. `ModifyTeamSettings` → `pages/Teams/Edit/ForecastSettingsComponent`, `useMetricsData` → `pages/Common/MetricsView/widgetStatus`, `deliveryArchivedRefusal` → `services/Api/ApiError`. |
| 4 UAT Given/When/Then | PASS | The brief's three AC are Given/When/Then; DISTILL writes the scenarios. |
| 5 AC derived from UAT | PASS | AC map to the Elevator Pitch "After". |
| 6 Right-sized | PASS | S–M; DESIGN cuts it to ≤ 1-day slices (tool + one rule first, then the rest). |
| 7 Technical notes + cross-cutting | PASS | Brief's Notes for DESIGN (docs-symlink hazard, pnpm minimum release age, Fallow trial questions); cross-cutting answered below. |
| 8 Dependencies tracked | PASS | None, hard or soft. |
| 9 Outcome KPIs, measurable | PASS | KPI 8: 16 → 0, measured by the allow-list length. |

## Wave: PICK-UP / [REF] Baseline re-count (2026-10-29)

Counted by resolving relative imports in production `.ts`/`.tsx` files (tests excluded) under each layer. The
DESIGN tool re-counts; a difference there is a finding, not an error.

| Measure | Brief (2026-10-22) | Today |
|---|---|---|
| Leaking files, total (KPI 8) | 16 | **16** files, 21 import edges |
| components → pages | 7 | **8**: new `FeatureListDataGrid` → `pages/Common/MetricsView/WidgetShell` |
| hooks → pages/components | 5 | **4**: the brief names four hooks (`useMetricsData`, `useCreateWizard`, `useRuleRowDraft`, `useUsageDataConsent`); its 5 was a miscount |
| services → hooks | 2 | 2 |
| models → pages | 1 | 1 |
| utils → services | 1 | 1 |

Today's list:

```text
components->pages  components/Common/Connection/ModifyConnectionSettings.tsx        -> pages/Settings/Connections/AdditionalFieldsEditor, WriteBackMappingsEditor
components->pages  components/Common/DataOverviewTable/ReportsChips.tsx             -> pages/Common/Reports/reportOwner, useOwnerReports
components->pages  components/Common/DateRangeSelector/DateRangePresets.tsx         -> pages/Common/MetricsView/dateWindow
components->pages  components/Common/DateRangeSelector/DateRangeSelector.tsx        -> pages/Common/MetricsView/dateWindow
components->pages  components/Common/FeatureListDataGrid/FeatureListDataGrid.tsx    -> pages/Common/MetricsView/WidgetShell
components->pages  components/Common/ProjectSettings/ModifyProjectSettings.tsx      -> pages/Common/AdvancedInputs/AdvancedInputs
components->pages  components/Common/Team/ModifyTeamSettings.tsx                    -> pages/Common/AdvancedInputs/AdvancedInputs, pages/Teams/Edit/ForecastSettingsComponent
components->pages  components/Common/Team/RefinementBandSettings.tsx                -> pages/Teams/Detail/Refinement/InfoTooltip, needWording
hooks->components  hooks/useCreateWizard.ts                                          -> components/DataRetrievalWizards
hooks->components  hooks/useRuleRowDraft.ts                                          -> components/Common/DeliveryRuleBuilder/types
hooks->components  hooks/useUsageDataConsent.tsx                                     -> components/UsageData/UsageDataIndicator
hooks->pages       hooks/useMetricsData.ts                                           -> pages/Common/MetricsView/categoryMetadata, widgetStatus
models->pages      models/Metrics/InfoWidgetData.ts                                  -> pages/Common/MetricsView/trendTypes
services->hooks    services/UsageData/usageDataEvents.ts                             -> hooks/useUsageDataConsent
services->hooks    services/UsageData/usageDataReporter.ts                           -> hooks/useUsageDataConsent
utils->services    utils/deliveries/deliveryArchivedRefusal.ts                       -> services/Api/ApiError
```

## Wave: PICK-UP / [REF] Checklist (tick through DELIVER)

Continuous-improvement rules:

- [ ] Behaviour-free: no route, DTO, serialized response or pixel changes; existing tests green unmodified. FE1 adds a check and moves no code.
- [ ] Ratchet in the same change: the boundary rule lands with its allow-list in the commit range that makes it pass. Inline file list, never a count; fails in both directions (ADR-268). Nothing loosened.
- [ ] Moves separate from edits (N/A unless DESIGN decides to move code; FE1's OUT says fixing the 16 belongs to FE2/FE3/FE5).
- [ ] Baselines re-counted (above) and re-checked by the chosen tool at DESIGN.
- [ ] Mutation last, on frozen code; touched area not lower (StrykerJS, if FE1 adds testable code such as a list-comparison helper).
- [ ] `ARCHITECTURE.md` §12 (frontend) updated in the same change; ADRs updated where a decision changes (tool choice: new ADR or ADR-268 amendment, decided at DESIGN).
- [ ] Payoff claim: build time within noise, measured before and after (`pnpm build`, and the new step on its own).
- [ ] No release notes (behaviour-free) unless the maintainer says otherwise.

Epic per-Story checklist (How to pick up a Story):

- [ ] Ratchet in the same PR; baselines are inline file lists; never loosened.
- [ ] Behaviour-free; API, E2E and response tests green unmodified.
- [ ] Mutation score in the touched area does not drop.
- [ ] Moves separate from edits.
- [ ] ARCHITECTURE.md updated in the same change (§12).
- [ ] One context at a time (N/A, because FE1 sets layer rules, not module boundaries).
- [ ] Terminology follows the seeded defaults (no S01 glossary yet).
- [x] Cross-cutting re-check, answered 2026-10-29: RBAC N/A, because no endpoint, permission or `useRbac()` gating changes. Lighthouse-Clients N/A, because no route or payload changes. Website N/A, because nothing user-visible changes. Usage-data event provisionally N/A, because FE1 adds a build check, not a feature (DEVOPS confirms). UI sketch N/A, because no UI changes.
