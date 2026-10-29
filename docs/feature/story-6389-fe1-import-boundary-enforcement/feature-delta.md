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

## Wave: DESIGN / [REF] Decisions (2026-10-29, propose mode, maintainer decided)

Paths are under `Lighthouse.Frontend/`. Scope: application (a build check on the frontend), no architect dispatch
for a Story this size.

| # | Decision | Verdict |
|---|---|---|
| D1 | Tool | **Fallow** (`fallow architecture --boundaries`), pinned exactly as a dev dependency. Trial below. |
| D2 | Where the named list lives | **Inline in one Vitest enforcement test**, `src/layerBoundaries.enforcement.test.ts`. It runs Fallow with `-f json` and compares the result with the list through `toEqual`, so Vitest's diff names a newcomer and a stale entry alike. Not Fallow's own baseline file: that would be a second mechanism over the same list, and it ignores stale entries. |
| D3 | Granularity | **One entry per leaking import**, `from → to` (21 today), not per file (16): a listed file must not gain a second leak unnoticed. |
| D4 | Where it runs | `pnpm test` (the frontend CI job runs it). Not in `prebuild`: the test already gates, and a second run there adds time and nothing else. |
| D5 | Zones and rules | `.fallowrc.json` declares a zone per top-level `src/` layer (`components`, `context`, `hooks`, `models`, `pages`, `services`, `utils`). Fallow rules are allow-lists, so each constrained layer allows every other zone except the ones it must not reach: components ↛ pages, hooks ↛ pages/components, services ↛ hooks, models ↛ pages, utils ↛ services. `--production` leaves test files out. |
| D6 | How the test calls Fallow | `execFileSync(process.execPath, [require.resolve("fallow/bin/fallow"), …])` from the frontend root: the launcher is a Node script, so this works on Windows too, and the parsed JSON is typed by `fallow/types`. |
| D7 | Renovate | Defaults: exact pin, the repo's 7-day minimum release age and automerge. No new rule. |
| D8 | What Fallow is used for | Boundaries only. Its dead-code, duplication and health reports are not enforced; the trial's dead-code findings are noted for a later item, not fixed here. |
| D9 | ADR | New **ADR-269** (frontend layer rules with Fallow, list in an enforcement test); status note on ADR-268 that the convention now covers the frontend. |

## Wave: DESIGN / [REF] Tool trial (2026-10-29)

Run read-only from a scratch folder; nothing was added to the repository. All three tools were given the same five rules.

| | Biome `noRestrictedImports` 2.5.15 | dependency-cruiser 18.7.0 | Fallow 3.36.0 |
|---|---|---|---|
| Same 16 files / 21 imports as the pick-up count | yes | yes, only with `typescript@6` added | yes |
| Works with the repo's TypeScript 7 | yes | **no**: scans 0 modules ("support for typescript@>=7 will follow") | yes, own parser |
| Time | +0 (already in `prebuild`) | seconds | 0.4 s for the boundary check |
| New dependency | none | yes, plus a second TypeScript | yes, native Rust binary, MIT, no install scripts |
| Unit of the allow-list | a file (`!path` in an override) | an import | an import (`from:to`) |
| New leak fails, named | yes | yes | yes, `file:line → target (components → pages)` |
| Fixed leak fails until delisted | no | no | no: a stale baseline entry passes |

The brief's six Fallow questions:

1. **Named list.** `--save-baseline` writes readable `boundary-violation:<from>:<to>` entries and `--baseline` fails a newcomer, but a stale entry passes silently. Hence D2: the test owns the list.
2. **Same 16?** Yes, 16 files and 21 imports, identical to the pick-up count and to Biome.
3. **Noise.** A first `dead-code --production` run reports 212 findings in 90 ms. Some are real: the dependencies `camelcase-keys`, `lodash.debounce` and `lorem-ipsum` are imported nowhere, and `ImageComponent` and `LicenseStatusDialog` are used only by their own tests. Some are false: test kits and test-only exports count as unused in production mode, and so does `public/sw.js`.
4. **Speed.** 0.4 s for the boundary check over the whole frontend.
5. **Maturity.** First release 2026-03-17, 236 releases since (about weekly or faster), 5.3k GitHub stars, 13 open issues. The cadence is handled by the 7-day minimum release age and automerge (D7).
6. **Overlap with Biome.** None for boundaries: Biome does not build an import graph. Fallow's `mutation` is not used (Stryker does that).

## Wave: DESIGN / [REF] Component decomposition

| Component | Path | Change |
|---|---|---|
| Fallow config | `Lighthouse.Frontend/.fallowrc.json` | NEW: zones and the five layer rules |
| Layer-boundary ratchet | `Lighthouse.Frontend/src/layerBoundaries.enforcement.test.ts` | NEW: runs Fallow, compares with the inline list |
| Dev dependency | `Lighthouse.Frontend/package.json`, `pnpm-lock.yaml` | MODIFIED: `fallow` exact pin |
| ADR-269 | `docs/product/architecture/adr-269-…md` | NEW |
| ADR-268 | status note | MODIFIED |
| ARCHITECTURE.md | §12 frontend bullet | MODIFIED |
| Production code | `src/**` | UNCHANGED (fixing the 21 belongs to FE2, FE3, FE5) |

## Wave: DESIGN / [REF] Reuse analysis

| Existing component | Evidence | Decision |
|---|---|---|
| `*.enforcement.test.ts` pattern | `ganttAdapterBoundary`, `reportPrint`, `bundledTypeface`, `oneStalenessRule`, `licenseStatusOneAsker`: an inline expectation compared through `toEqual` | EXTEND the pattern: one more enforcement test |
| Their private `sourceFilesUnder` scans | text search for one vendor or one call | not reused: an import graph with path resolution is what Fallow is for; a seventh hand-written resolver is what D1 avoids |
| Biome `prebuild` and its GritQL plugins | Sonar-mirroring lint, per-file | unchanged: no import graph, kept for what it does |
| Backend ratchets (ADR-268) | inline list, both directions | convention reused, code not shared (different stack) |

## Wave: DESIGN / [REF] Slices

Each slice is at most a day, leaves `pnpm test` and `pnpm build` green, and changes no production file.

| Slice | Change | Satisfies |
|---|---|---|
| FE1-1 | Add `fallow` (exact pin); `.fallowrc.json` with all zones and the **components ↛ pages** rule; the enforcement test with that rule's 12 entries | AC: a new `components → pages` import fails naming file and import; a delisted fixed one fails until removed |
| FE1-2 | The other four rules (hooks, services, models, utils) and their 9 entries; ADR-269, the ADR-268 note, ARCHITECTURE.md §12; time `pnpm test` before and after | AC: the same for every rule; the build is not slower beyond noise |

## Wave: DESIGN / [REF] Before → after

Before: nothing relates the layers. `ModifyTeamSettings.tsx` importing `pages/Teams/Edit/ForecastSettingsComponent` compiles, lints and passes review, and so would a 22nd leak.

After: `pnpm test` fails on any import across the five rules that is not one of the 21 listed, naming `from → to`, and fails on a listed import that has gone, until the entry is deleted. FE2, FE3 and FE5 shrink the list; FE2 adds module zones to the same `.fallowrc.json`.

## Wave: DESIGN / [REF] Risks

- **Fallow changes its JSON shape.** The test parses `boundary_violations[].from_path/to_path`, typed by `fallow/types`; a Renovate bump that breaks it goes red in CI, not silently green, because an empty or failed parse cannot equal a 21-entry list.
- **A zone added later without a rule.** A new top-level `src/` folder is unconstrained until FE2 adds module zones. Accepted; Fallow's `coverage.requireAllFiles` is the tool for that if it starts to matter.
- **Binary availability.** The platform binary comes as an optional dependency; a platform without one fails the test loudly. CI is Linux x64, and developers use Linux, macOS and Windows, all published.

## Wave: DESIGN / [REF] Open questions for DISTILL and DELIVER

- DISTILL: the scenarios are the enforcement test's own: a newcomer fails naming it, a stale entry fails naming it, today's list passes. Checking the newcomer case needs a throwaway fixture or a small `src/`-shaped temp dir; decide which at DISTILL.
- DELIVER: confirm `pnpm install` under pnpm 12's minimum release age with the pinned version, on CI too.
- Later item, not FE1: Fallow's dead-code findings (three unused dependencies, two test-only components). Propose as a catalogue item at close-out.

## Wave: DESIGN / [REF] Changed Assumptions

Quoted from `docs/feature/epic-6264-modular-monolith/feature-delta.md`, FE1 AC:

> Allow-list baseline of **16** violations (survey 2026-10-22, re-counted by the tool at DESIGN): components→pages 7, hooks→pages/components 5, services→hooks 2, models→pages 1, utils→services 1.

**Changed:** the list counts imports, not files (D3): **21** entries over 16 files. By rule: components → pages 12 imports in 8 files (the new one is `FeatureListDataGrid` → `WidgetShell`), hooks → pages/components 5 imports in 4 files, services → hooks 2, models → pages 1, utils → services 1. KPI 8's baseline becomes 21 imports.

Quoted from the same AC:

> Tool chosen at DESIGN: Biome `noRestrictedImports` in the existing `biome.json`, dependency-cruiser, or Fallow … It runs in `pnpm build` or `pnpm test` so CI enforces it.

**Settled:** Fallow, in `pnpm test` (D1, D4). dependency-cruiser is out because it does not support TypeScript 7.
