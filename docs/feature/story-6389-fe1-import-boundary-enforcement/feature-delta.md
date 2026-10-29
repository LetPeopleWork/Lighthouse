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

Items 1–8 are the nWave Definition of Ready; item 9 (outcome KPIs) is this project's addition. Result: 8/8, plus item 9.

The Story's acceptance criteria, restated in Given/When/Then with the trigger DESIGN chose (the brief's AC 1 said
`pnpm build`, its AC 2 had no When, and its AC 3 was a quality bar):

1. Given a production file in `components/` imports from `pages/` and is not on the list, when `pnpm test` runs, then
   it fails naming the importing file and the imported one (and likewise for the other four rules).
2. Given a listed file pair no longer leaks, when `pnpm test` runs, then it fails naming the stale entry until the
   entry is deleted.
3. Given the check is in place, when `pnpm test` and `pnpm build` run, then Biome reports zero errors and warnings
   and the test run is not slower beyond noise.

| DoR item | Status | Evidence for FE1 |
|---|---|---|
| 1 Problem statement, domain language | PASS | 16 production files leak across layers today, and nothing stops a 17th. |
| 2 Persona with specifics | PASS | `lighthouse-maintainer` and the CI build. |
| 3 ≥ 3 domain examples, real data | PASS | Re-counted list below, e.g. `ModifyTeamSettings` → `pages/Teams/Edit/ForecastSettingsComponent`, `useMetricsData` → `pages/Common/MetricsView/widgetStatus`, `deliveryArchivedRefusal` → `services/Api/ApiError`. |
| 4 UAT Given/When/Then (3–7) | PASS | Three acceptance criteria, each in Given/When/Then, restated above. The executable scenarios DISTILL derives from them are tests, not UAT, and are counted there. |
| 5 AC derived from UAT | PASS | AC map to the Elevator Pitch "After". |
| 6 Right-sized | PASS by waiver (D-6264-4) | S–M sizing; carpaccio into ≤ 1-day slices is deferred to this Story's DESIGN. |
| 7 Technical notes + cross-cutting | PASS | Brief's Notes for DESIGN (docs-symlink hazard, pnpm minimum release age, Fallow trial questions); cross-cutting answered below. |
| 8 Dependencies tracked | PASS | None, hard or soft. |
| 9 Outcome KPIs, measurable | PASS | KPI 8: 21 imports in 16 files → 0, measured by the allow-list length (one entry per importing file and imported file pair). |

## Wave: PICK-UP / [REF] Baseline re-count (2026-10-29)

Counted by hand, resolving relative imports in production `.ts`/`.tsx` files (tests excluded) under each layer.
Fallow, Biome and dependency-cruiser each reproduced the same 21 imports in 16 files at DESIGN (2026-10-29). This
supersedes the 16-violation baseline in the Epic FE1 AC and in the brief (components→pages 7, hooks→pages/components 5),
which counted files and miscounted hooks; the Epic's KPI 8 row says so.

| Measure | Brief (2026-10-22) | Today |
|---|---|---|
| KPI 8, the list length (file pairs) | 16 (counted as files) | **21** file pairs, in 16 files |
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
- [ ] Mutation last, on frozen code; touched area not lower. N/A, because FE1 adds no production code: the comparison helper lives in the test file on purpose (DEVOPS), and StrykerJS configs here mutate named production files only. Confirmed at the final gate.
- [ ] `ARCHITECTURE.md` §12 (frontend) updated in the same change; ADRs updated where a decision changes (DESIGN chose a new ADR, ADR-269, plus a status note on ADR-268).
- [ ] Payoff claim: test run within noise, `pnpm test` measured before FE1-1, after FE1-1 and after FE1-2 (the check is part of the Vitest suite, not a build step).
- [ ] No release notes (behaviour-free) unless the maintainer says otherwise.

Epic per-Story checklist (How to pick up a Story):

- [ ] Ratchet in the same PR; baselines are inline file lists; never loosened.
- [ ] Behaviour-free; API, E2E and response tests green unmodified.
- [ ] Mutation score in the touched area does not drop.
- [ ] Moves separate from edits.
- [ ] ARCHITECTURE.md updated in the same change (§12).
- [ ] One context at a time (N/A, because FE1 sets layer rules, not module boundaries).
- [x] Terminology follows the seeded defaults: N/A, because FE1 adds no user-visible or documented product term; the test and config use folder names only.
- [x] Cross-cutting re-check, answered 2026-10-29: RBAC N/A, because no endpoint, permission or `useRbac()` gating changes. Lighthouse-Clients N/A, because no route or payload changes. Website N/A, because nothing user-visible changes. Usage-data event provisionally N/A, because FE1 adds a build check, not a feature (DEVOPS confirms). UI sketch N/A, because no UI changes. Terminology N/A, because no user-visible or documented product term is added.

## Wave: DESIGN / [REF] Decisions (2026-10-29, propose mode, maintainer decided)

Paths are under `Lighthouse.Frontend/`. Scope: application (a build check on the frontend), no architect dispatch
for a Story this size.

| # | Decision | Verdict |
|---|---|---|
| D1 | Tool | **Fallow** (`fallow architecture --boundaries`), pinned exactly as a dev dependency. Trial below. |
| D2 | Where the named list lives | **Inline in one Vitest enforcement test**, `src/layerBoundaries.enforcement.test.ts`. It runs Fallow with `-f json` and compares the result with the list through `toEqual`, so Vitest's diff names a newcomer and a stale entry alike. Not Fallow's own baseline file: that would be a second mechanism over the same list, and it ignores stale entries. |
| D3 | Granularity | **One entry per leaking file pair**, `from → to` (21 today), not per importing file (16): a listed file must not gain a leak to a second target unnoticed. Fallow reports a pair once however many import statements connect the two files (probed at DISTILL), so a pair is the finest unit there is. |
| D4 | Where it runs | The Vitest suite: `pnpm test` locally; CI's frontend job runs the same suite as `pnpm run sonarreport` (Vitest with coverage). Not in `prebuild`: the test already gates, and a second run there adds time and nothing else. |
| D5 | Zones and rules | `.fallowrc.json` declares a zone per top-level `src/` layer folder, with root-anchored patterns (`src/components/**`, …): `components`, `context`, `hooks`, `models`, `pages`, `services`, `utils`. Fallow rules are allow-lists, so each constrained layer allows every other zone except the ones it must not reach (table below). Imports within a zone are always allowed. Imports into a file that belongs to no zone are not checked. `--production` leaves test files out. The rules check direct imports only; reaching a page through an unzoned file is out of scope. |
| D6 | How the test calls Fallow | `spawnSync(process.execPath, [launcher, …])`, with the launcher found through `createRequire(import.meta.url).resolve("fallow/bin/fallow")` (the frontend is an ES module, so there is no bare `require`). The launcher is a Node script, so this works on Windows too. Fallow exits **0** with no violation, **1** with violations and **2** on an error such as a missing config (checked 2026-10-29); the test parses stdout on 0 or 1 and throws, quoting stderr, on anything else. The JSON is typed by a local two-field interface (`from_path`, `to_path`); swapping in the package's shipped types is optional. |
| D7 | Renovate | Defaults: exact pin, the repo's 7-day minimum release age and automerge. No new rule. |
| D8 | What Fallow is used for | Boundaries only. Its dead-code, duplication and health reports are not enforced; the trial's dead-code findings are noted for a later item, not fixed here. |
| D9 | ADR | New **ADR-269** (frontend layer rules with Fallow, list in an enforcement test); status note on ADR-268 that the convention now covers the frontend. |

## Wave: DESIGN / [REF] Zone allow-lists

| Zone | May import | Must not import |
|---|---|---|
| `components` | `context`, `hooks`, `models`, `services`, `utils` | `pages` |
| `hooks` | `context`, `models`, `services`, `utils` | `pages`, `components` |
| `services` | `components`, `context`, `models`, `pages`, `utils` | `hooks` |
| `models` | `components`, `context`, `hooks`, `services`, `utils` | `pages` |
| `utils` | `components`, `context`, `hooks`, `models`, `pages` | `services` |
| `pages`, `context` | everything (no rule) | — |

Top-level `src/` entries with no zone, and why: `tests/` (test kits and helpers, test code), `assets/` (no TypeScript),
`App.tsx`, `main.tsx` and `App.css` (the composition root, which wires every layer), `vite-env.d.ts` (ambient types),
`layerBoundaries.enforcement.test.ts` (test code). `services` and `utils` keep the imports they have today toward
`components` and `pages`, and `models` toward `components`; the one `models → pages` import is a listed leak. Only the
five survey rules are in FE1's scope.

## Wave: DESIGN / [REF] Tool trial (2026-10-29)

Run read-only from a scratch folder; nothing was added to the repository. All three tools were given the same five rules.

| | Biome `noRestrictedImports` 2.5.15 | dependency-cruiser 18.7.0 | Fallow 3.36.0 |
|---|---|---|---|
| Same 16 files / 21 imports as the pick-up count | yes | yes, only with `typescript@6` added | yes |
| Works with the repo's TypeScript 7 | yes | **no**: scans 0 modules ("support for typescript@>=7 will follow") | yes, own parser |
| Time | +0 (already in `prebuild`) | seconds | 0.4 s for the boundary check |
| New dependency | none | yes, plus a second TypeScript | yes, native Rust binary, MIT, no install scripts |
| Unit of the allow-list | a file (`!path` in an override) | an import | a file pair (`from:to`) |
| New leak fails, named | yes | yes | yes, `file:line → target (components → pages)` |
| Fixed leak fails until delisted | no | no | no: a stale baseline entry passes |

The brief's six Fallow questions:

1. **Named list.** `--save-baseline` writes readable `boundary-violation:<from>:<to>` entries and `--baseline` fails a newcomer, but a stale entry passes silently. Hence D2: the test owns the list.
2. **Same 16?** Yes, 16 files and 21 imports, identical to the pick-up count and to Biome.
3. **Noise.** A first `dead-code --production` run reports 212 findings in 90 ms. Some are real: the dependencies `camelcase-keys`, `lodash.debounce` and `lorem-ipsum` are imported nowhere, and `ImageComponent` and `LicenseStatusDialog` are used only by their own tests. Some are false: test kits and test-only exports count as unused in production mode, and so does `public/sw.js`.
4. **Speed.** 0.4 s for the boundary check over the whole frontend.
5. **Maturity.** First release 2026-03-17, 236 releases since (about weekly or faster), 5.3k GitHub stars, 13 open issues. The cadence is handled by the 7-day minimum release age and automerge (D7).
6. **Overlap with Biome.** None for boundaries: Biome's `noRestrictedImports` matches import strings per file (seen in the trial), so it cannot say which zone a resolved import lands in. Fallow's `mutation` is not used (Stryker does that).

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
| `*.enforcement.test.ts` pattern | `ganttAdapterBoundary`, `reportPrint`, `bundledTypeface`, `oneStalenessRule`, `licenseStatusOneAsker`: an inline expectation compared through `toEqual` | CREATE NEW file, reusing the pattern: a new `*.enforcement.test.ts` with no shared code (the existing ones each guard one vendor or call) |
| Their private `sourceFilesUnder` scans | text search for one vendor or one call | not reused: an import graph with path resolution is what Fallow is for; a seventh hand-written resolver is what D1 avoids |
| Biome `prebuild` and its GritQL plugins | Sonar-mirroring lint, per-file | unchanged: no import graph, kept for what it does |
| Backend ratchets (ADR-268) | inline list, both directions | convention reused, code not shared (different stack) |

## Wave: DESIGN / [REF] Slices

Each slice is at most a day (estimates: FE1-1 half a day, FE1-2 half a day), leaves `pnpm test` and `pnpm build` green, and
changes no production file. The test's list holds all 21 entries from DISTILL on; in FE1-1, with only the components
rule configured, the test compares the `src/components/` entries both ways, so the two-way rule holds for every rule
that exists; FE1-2 switches the comparison to the whole list.

| Slice | Change | Satisfies |
|---|---|---|
| FE1-1 | Add `fallow` (exact pin); `.fallowrc.json` with all zones and the **components ↛ pages** rule; unskip the components scenarios (its 12 entries compared both ways); ARCHITECTURE.md §12 bullet, since the rule lands here | AC: a new `components → pages` import fails naming file and import; a delisted fixed one fails until removed |
| FE1-2 | The other four rules (hooks, services, models, utils); unskip their scenarios and the whole-list comparison (all 21 entries); extend the §12 bullet to the five rules; time `pnpm test` before and after (ADR-269 and the ADR-268 note landed at DESIGN) | AC: the same for every rule; the build is not slower beyond noise |

## Wave: DESIGN / [REF] Before → after

Before: nothing relates the layers. `ModifyTeamSettings.tsx` importing `pages/Teams/Edit/ForecastSettingsComponent` compiles, lints and passes review, and so would a 22nd leak.

After: `pnpm test` fails on any import across the five rules that is not one of the 21 listed, naming `from → to`, and fails on a listed import that has gone, until the entry is deleted. FE2, FE3 and FE5 shrink the list; FE2 adds module zones to the same `.fallowrc.json`.

## Wave: DESIGN / [REF] Risks

- **Fallow changes its JSON shape.** The test parses `boundary_violations[].from_path/to_path`, typed by a local two-field interface (D6); a Renovate bump that breaks it goes red in CI, not silently green, because the test throws on an unusable run and an empty result cannot equal the listed entries under comparison.
- **A zone added later without a rule.** A new top-level `src/` folder is unconstrained until FE2 adds module zones. Accepted; Fallow's `boundaries.coverage.requireAllFiles` is the tool for that if it starts to matter.
- **Binary availability.** The platform binary comes as an optional dependency. Published for 3.36.0: `darwin-arm64`, `darwin-x64`, `linux-arm64-gnu`, `linux-arm64-musl`, `linux-x64-gnu`, `linux-x64-musl`, `win32-arm64-msvc`, `win32-x64-msvc`, which covers every CI runner and developer platform here. On a platform without one, pnpm skips the optional package and installs fine; only the test fails, loudly.
- **Config or rule semantics change between releases.** Fallow releases about weekly. The exact pin plus the enforcement test is the mitigation: a bump that changes the config schema or what a rule reports makes the test red, and Renovate's automerge waits for green.
- **Observed JSON (trial, 3.36.0):** `boundary_violations[]` with `from_path`, `to_path` (relative to the root, e.g. `src/components/Common/Team/ModifyTeamSettings.tsx`), `from_zone`, `to_zone`, `line`, `col`, `finding_id`.

## Wave: DESIGN / [REF] Open questions for DISTILL and DELIVER

- DISTILL: the scenarios are the enforcement test's own: a newcomer fails naming it, a stale entry fails naming it, today's list passes. Checking the newcomer case needs a throwaway fixture or a small `src/`-shaped temp dir; decide which at DISTILL.
- DELIVER: confirm `pnpm install` under pnpm 12's minimum release age with the pinned version, on CI too.
- Later item, not FE1: Fallow's dead-code findings (three unused dependencies, two test-only components). Propose as a catalogue item at close-out.

## Wave: DESIGN / [REF] Changed Assumptions

Quoted from `docs/feature/epic-6264-modular-monolith/feature-delta.md`, FE1 AC:

> Allow-list baseline of **16** violations (survey 2026-10-22, re-counted by the tool at DESIGN): components→pages 7, hooks→pages/components 5, services→hooks 2, models→pages 1, utils→services 1.

**Changed:** the list counts file pairs, not files (D3): **21** entries over 16 files (one import per pair today). By rule: components → pages 12 imports in 8 files (the new one is `FeatureListDataGrid` → `WidgetShell`), hooks → pages/components 5 imports in 4 files, services → hooks 2, models → pages 1, utils → services 1. KPI 8's baseline becomes 21 imports.

Quoted from the same AC:

> Tool chosen at DESIGN: Biome `noRestrictedImports` in the existing `biome.json`, dependency-cruiser, or Fallow … It runs in `pnpm build` or `pnpm test` so CI enforces it.

**Settled:** Fallow, in `pnpm test` (D1, D4). dependency-cruiser is out because it does not support TypeScript 7.

## Wave: DEVOPS / [REF] Platform decisions (2026-10-29, AFK, recommended options taken)

Every platform decision is the project's existing one. FE1 adds a dev dependency, a config file and a test;
nothing is deployed, built or served differently.

| Decision | Answer |
|---|---|
| Deployment target, orchestration, strategy | The shipped artifacts are unchanged: `fallow` is a dev dependency and Vite does not bundle it. The build steps do change slightly: every path that installs the frontend now installs `fallow`, and every `tsc -b` (CI build, the Docker `node-builder` stage, the Standalone builds) type-checks the new test. FE1-1 confirms `pnpm exec tsc -b` passes. |
| Security | Audit scope unchanged: `pnpm audit --prod` in the Sonar gates leaves dev dependencies out, as for every other one. Controls for `fallow`: exact pin, Renovate's 7-day minimum release age, automerge only on green CI, MIT licence, and no install script (checked in the trial). Engines `node >=22`; the repo pins Node 24 (`.nvmrc`), so `engineStrict` is satisfied. |
| CI/CD platform | GitHub Actions, existing workflows. No new job, no new workflow step. |
| Branching | Trunk-based on `main`. |
| Observability | N/A, because no runtime behaviour changes. |
| Continuous learning / flags | N/A, because there is nothing a user can see to roll out. |
| Mutation testing | **N/A, because FE1 adds no production code.** Frontend StrykerJS configs are per feature (`stryker.config.story-*.mjs`) and list the production files they mutate; FE1 adds only a test and a config. The comparison helper stays inside the test file on purpose: it exists only to serve this one check. Recorded at the final gate rather than run on an empty mutate set. |
| Usage-data event | **N/A, because FE1 is a build check**: nothing a user does changes, so there is no use to count. |

## Wave: DEVOPS / [REF] CI pipeline outline

- **Where the check runs:** `ci_frontend.yml` → `.github/actions/build-frontend` with `run-tests: 'true'`, which runs
  `pnpm run sonarreport` (the same Vitest suite as `pnpm test`, with coverage), and so the new enforcement test. `ci_changes.yml` sets `frontend=true` for any path under
  `Lighthouse.Frontend/`, which covers `.fallowrc.json`, `package.json` and the test. No change to either workflow.
- **Install:** `fallow` brings its native binary as a per-platform optional dependency (`@fallow-cli/<platform>`) and
  runs no install script, so `onlyBuiltDependencies` in `pnpm-workspace.yaml` stays as it is. Every place that installs
  the frontend's dependencies has a published binary: `build-frontend` (frontend CI and app packaging), `ci_sbom.yml`, the
  Linux, macOS and Windows Standalone packaging jobs, the E2E and verify workflows (`ci_e2e`, `ci_verifyauth`,
  `ci_verifypostgres`, `ci_verifysqlite`), and the Dockerfile's `node-builder` stage (Debian, glibc). A platform without a
  binary would still install; only the test would fail there.
- **Minimum release age:** pnpm 12 checks every lockfile entry against its default 24 h (`pnpm-workspace.yaml` sets no
  value of its own), and Renovate waits 7 days. Pin a version at
  least a day old when adding it (3.36.0 was used in the trial).
- **Time:** the boundary check took 0.4 s in the trial. DELIVER FE1-2 times `pnpm test` before and after; a change beyond
  noise is a finding.
- **Sonar:** the new test is test code. The Sonar TS profile is mirrored by Biome, so `pnpm build` must stay clean.

## Wave: DEVOPS / [REF] Risks for DISTILL and DELIVER

- **Generated folders.** Fallow must not scan `.stryker-tmp/`, `dist/`, `coverage/` or `reports/` copies of `src/`. A
  mutation run leaves sandboxes under `.stryker-tmp/`, and a scan that included them would report each leak several
  times. Settled by the root-anchored zones (DISTILL pre-check): a copy under `.stryker-tmp/` or `dist/` matches no zone, so
  no `ignorePatterns` is needed. DELIVER FE1-1 confirms that a stray sandbox copy is not reported.
- **Running inside StrykerJS.** A mutation run of another feature runs this test in a sandbox copy. The test resolves
  paths from its own location, so it scans the sandbox, which holds the same imports. Accepted.

## Wave: DEVOPS / [REF] Environment matrix

CI runners `ubuntu-latest`, `macos-latest`, `windows-latest` and the Docker `node-builder` stage, plus the developer
machines (Linux, macOS, Windows). Nothing installs or runs differently between them, apart from which `@fallow-cli/*`
binary pnpm picks. The test itself runs only where Vitest runs: the frontend CI job and developer machines.

## Wave: DISTILL / [REF] Pre-check (2026-10-29, before dispatch)

- Fallow run with `-r <fixture root>` and the same zone config reports a `components → pages` import in a two-file fixture tree with no `package.json`. So the enforcement test can prove the newcomer and stale-entry behaviours against a fixture, not only against the real tree.
- Zones are anchored at the project root (`src/components/**`), so copies under `.stryker-tmp/`, `dist/` or `coverage/` cannot match a zone. That settles the DEVOPS risk about generated folders; no `ignorePatterns` needed.

## Wave: DELIVER / [REF] Rigor profile (maintainer decision 2026-10-29)

| Slices | Profile | Crafter and acceptance designer | Reviewer |
|---|---|---|---|
| FE1-1, FE1-2 | standard | sonnet | haiku |

A clean Haiku review is not proof on its own: the orchestrator spot-checks the cited lines and untested edges.

## Wave: DISTILL / [REF] Scenario list

Executable spec: `Lighthouse.Frontend/src/layerBoundaries.enforcement.test.ts` (TypeScript, Vitest). All 17 are `it.skip`; DELIVER unskips one at a time. Error/edge scenarios: 15 of 17 (everything except the two walking-skeleton stages #1 and #2).

| # | Title | Slice | Tags |
|---|---|---|---|
| 1 | lets components reach into pages only in the twelve listed places, and in all of them | FE1-1 | @walking_skeleton @real-io |
| 2 | lets the frontend cross its layers only in the twenty-one listed places, and in all of them | FE1-2 | @walking_skeleton @real-io |
| 3 | a component reaching into a page is refused as a new leak, naming the component and the page | FE1-1 | @error @real-io |
| 4 | a component reaching into a page is not reported when it is only a `.test.ts` file that does so | FE1-1 | @edge @real-io |
| 5 | the same, for a `.test.tsx` file | FE1-1 | @edge @real-io |
| 6 | a component reaching into a page is reported once, however many times it imports that page | FE1-1 | @edge @real-io |
| 7 | a leak listed once is refused as a new leak when the same entry turns up a second time | FE1-1 | @error |
| 8 | a leak listed twice but found only once is refused as stale | FE1-1 | @error |
| 9 | a listed leak that has been fixed is refused as stale until its entry is deleted, naming the entry | FE1-1 | @error @real-io |
| 10 | directions the layers allow report nothing: a component using a hook | FE1-1 | @edge @real-io |
| 11 | directions the layers allow report nothing: a page using a component | FE1-1 | @edge @real-io |
| 12 | refuses a hook reaching into a page, naming both ends | FE1-2 | @error @real-io |
| 13 | refuses a hook reaching into a component, naming both ends | FE1-2 | @error @real-io |
| 14 | refuses a service reaching into a hook, naming both ends | FE1-2 | @error @real-io |
| 15 | refuses a model reaching into a page, naming both ends | FE1-2 | @error @real-io |
| 16 | refuses a utility reaching into a service, naming both ends | FE1-2 | @error @real-io |
| 17 | a boundary check that cannot run (missing config) fails the test, quoting what it printed, instead of reporting no leaks | FE1-1 | @error @real-io |

Slice split by list: FE1-1 enables #1 against the 12 `components → pages` entries; FE1-2 enables #2 against all 21 (the other 9: hooks 5, services 2, models 1, utils 1). The inline `ALLOWED_LEAKS` holds all 21 from the start; `.fallowrc.json` decides which rules exist, so in FE1-1 the comparison covers the `src/components/` entries only.

Positive control: every scenario that expects no new leak (#4, #5, #9, #10, #11) puts one real `components → pages` import in the same fixture and expects exactly that entry, so a run that applied no rule fails. #6 carries the same control next to its own file.

Counted entries: `compareWithAllowList` subtracts as a multiset, so a second identical entry is a newcomer and a surplus listed entry is stale (#7, #8). Fallow itself cannot report two edges for one file pair: with three statements importing the same page (two `import`s and an `export … from`), the 3.36.0 trial binary printed one violation (the first line). So an entry is one per file pair, not per import statement, #6 pins that, and #7/#8 are written at the comparison level with duplicated lists, not with a fixture.

Acceptance criterion 3 is not a scenario. Its Biome part is the `pnpm build` gate every slice ends with; its timing part
is a DELIVER measurement of `pnpm test`, taken before FE1-1 adds Fallow and again after FE1-1 and FE1-2 (DEVOPS).

Environments: N/A as scenarios, because the test has no install or runtime precondition of its own; the DEVOPS matrix
varies only which Fallow binary pnpm installs. Paths are normalised to forward slashes, so a Windows run compares the
same entries.

## Wave: DISTILL / [REF] Walking skeleton

The maintainer runs `pnpm test` and learns exactly which layer leaks exist, with nothing hidden and nothing stale. Two stages of one skeleton: #1 (components, FE1-1) and #2 (whole frontend, FE1-2). Both drive the real Fallow CLI over the real `src/` tree with the real `.fallowrc.json`.

## Wave: DISTILL / [REF] Adapter coverage

| Driven adapter | @real-io scenario | Covered by |
|---|---|---|
| Fallow CLI (`fallow architecture --boundaries`, spawned as a child process) | YES | #1 and #2 on the real tree; #3 to #6, #9 to #17 on throwaway project trees in the OS temp folder |

Fixtures are built on the fly with `mkdtemp` rather than committed under `src/`: no deliberate leaks sit in the repository, nothing needs to type-check or pass Biome, and the real check can never see them.

Exit status: `findLeaks` throws unless Fallow exits 0 (no violations) or 1 (violations), and checks that the JSON holds a `boundary_violations` array. Fallow exits 2 on an error such as a missing config and still prints JSON to stdout (`{"error":true,…,"exit_code":2}`), so without the check an error could read as "no leaks". #17 covers it.

## Wave: DISTILL / [REF] Scaffolds

None. There is no production module: the comparison helper (`compareWithAllowList`, `without`, `describeDrift`, `findLeaks`) lives inside the test file. Nothing carries a scaffold marker, and no `src/**` production file changes.

## Wave: DISTILL / [REF] Test placement

`Lighthouse.Frontend/src/layerBoundaries.enforcement.test.ts`, at the `src/` root because it spans every layer. Precedent: `ganttAdapterBoundary.enforcement.test.ts` and the other `*.enforcement.test.ts` files (inline expectation compared through `toEqual`). It is picked up by the existing `pnpm test`.

## Wave: DISTILL / [REF] UI sketches

N/A, because nothing a user sees changes; FE1 adds a build check.

## Wave: DISTILL / [REF] Reconciliation

Reconciliation found one contradiction at review, resolved in commit f4027b594. An earlier version of this section said "0 contradictions"; that was wrong. The FE1-1 slice row said the list holds "12 entries", while the test holds all 21. DESIGN now says: the list holds all 21 entries from DISTILL on; in FE1-1 (only the components rule configured) the test compares the `src/components/` entries both ways; FE1-2 switches to the whole list. The scenarios follow that (#1 then #2).

D6 was also brought in line with the test: `spawnSync`, `createRequire`, the exit codes 0, 1 and 2, and a local two-field type for the JSON. DISCUSS is the Epic's; DESIGN and DEVOPS otherwise agree: Fallow, `pnpm test`, no production change, mutation N/A. No contradiction is open.
