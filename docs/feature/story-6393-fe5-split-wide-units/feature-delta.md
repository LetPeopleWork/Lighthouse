# Story 6393 — FE5 Split wide units

Epic #6509 (frontend packaged by module; catalogue in Epic #6264's workspace) · workspace
`docs/feature/epic-6264-modular-monolith/` · brief
`docs/feature/epic-6264-modular-monolith/slices/slice-37-fe5-split-wide-units.md` · ADO #6393 · picked up 2026-11-12
via `/continuous-improvement before 6473`.

DISCUSS: covered at Epic level (see the Epic's Wave Decisions, D-6264-3); waves start at DESIGN.

**Why now.** The maintainer chose to run FE5 alongside Epic #6473 (settings split). Slice 05 of #6473 changes the
links and return path in `ModifyConnectionSettings.tsx`, one of FE5's units. The split of that unit should land on
`main` before #6473 slice 05 starts, so the two do not edit the same 1000-line file at once. DESIGN orders the units
with that in mind.

## Wave: PICK-UP / [REF] Inherited

Read these in the Epic's `feature-delta.md`; they are not copied here.

- `[REF] Story Map`: pick-up group G2 (can run before or alongside packaging); the dependency graph
  (`FE1 --> FE5`, done; `FE4 --> FE5` soft, not done; `FE5 <-> FE7` soft, either order).
- `[REF] Stories` → `#### FE5 — Split wide units` (the AC).
- `[REF] How to pick up a Story`: the per-Story checklist.
- `[REF] Cross-Cutting Impact Checklist`: RBAC (the shared RBAC-handler hook goes through `useRbac()`), Lighthouse-Clients,
  Website, usage data, UI sketches, Terminology.
- `[REF] Outcome KPIs + guardrails`: KPI 11 (frontend production files > 1000 lines), KPI 12 (mutation).
- `[REF] Wave Decisions`: D-6264-3/4/5, D-6264-8 (frontend track in, TanStack Query migration out).
- `[REF] Out of scope`: Team/Portfolio metrics view duplication is already generic via `BaseMetricsView`; splitting
  that file is FE5.
- `[REF] Improvement log`: the FE1 entry (Fallow, ADR-269: later frontend ratchets use Fallow rather than a
  hand-written scan; the list unit is the file pair).
- ADRs: ADR-268 (ratchet lists offenders by name, both directions, only shrinks), ADR-269 (Fallow), ADR-270 (contexts
  and layout).

## Wave: PICK-UP / [REF] DoR re-run (2026-11-12)

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement, domain language | PASS | Named files and line counts below; the duplicated RBAC handlers named by function. |
| 2 Persona | PASS | `lighthouse-maintainer`, `@infrastructure`. |
| 3 ≥ 3 real examples | PASS | `BaseMetricsView.tsx` 2074, `ragRules.ts` 1063, `ModifyConnectionSettings.tsx` 1062 lines. |
| 4 UAT Given/When/Then | PENDING DESIGN | The brief's 3 AC; restated as G/W/T with the trigger DESIGN chooses for the size ratchet. |
| 5 AC from UAT | PASS | AC map to the Story's "After". |
| 6 Right-sized | PASS by deferral | Size L; DESIGN cuts it one unit per slice, at most 5 slices per nwdes feature, so more than one nwdes feature. |
| 7 Technical notes + cross-cutting | PASS | Epic's Cross-Cutting Impact Checklist; re-answered per Story in the checklist below. |
| 8 Dependencies | PASS | Soft only: FE1 done, FE4 not started (accepted: FE5 does not wait for it), FE7 either order. |
| 9 Outcome KPIs | PASS | KPI 11 baseline below, target 0 or each justified. |

## Wave: PICK-UP / [REF] Baseline re-count (2026-11-12, old → new)

Production files under `Lighthouse.Frontend/src`, tests excluded.

| Unit | Brief (2026-10-22) | Now |
|---|---|---|
| `pages/Common/MetricsView/BaseMetricsView.tsx` | 2034 | **2074** |
| `pages/Common/MetricsView/ragRules.ts` | not listed | **1063** (new over the line) |
| `components/Common/Connection/ModifyConnectionSettings.tsx` | 1201 | **1062** |
| `pages/Portfolios/Detail/Components/DeliveryGrid/DeliverySection.tsx` | 993 | 992 (under the line) |
| `pages/Portfolios/Detail/Components/DeliveryGrid/DeliveryCreateModal.tsx` | 914 | 989 (under the line) |
| `hooks/useMetricsData.ts` | 970 | 970 (under the line) |
| Files > 1000 lines (KPI 11) | ≥ 2 known | **3** |
| RBAC handlers, `pages/Teams/Detail/TeamDetail.tsx` | ≈ l.144–211 | l.169–228 (`handleAssignTeamRole`, `handleRemoveTeamMember`, `handleCreateTeamGroupMapping`, `handleRemoveTeamGroupMapping`) |
| RBAC handlers, `pages/Portfolios/Detail/PortfolioDetail.tsx` | ≈ l.114–185 | l.142–205 (the four `…Portfolio…` equivalents) |

Next in line, under 1000: `services/Api/MetricsService.ts` 957 (FE4's), `FeatureSizeScatterPlotChart.tsx` 866. DESIGN
decides whether the units just under the line (992, 989, 970) are in scope or only guarded by the ratchet.

## Wave: PICK-UP / [REF] Checklist (ticked through DELIVER)

- [ ] **Behaviour-free**: routes, DTO shapes, serialized responses, UI pixels unchanged. Vitest, Playwright and
      `@screenshot` green unmodified, apart from tests that move with the code they cover.
- [ ] **Ratchet in the same change**: a size rule (production files > 1000 lines, listed by name, ADR-268 style) added
      in the commit range that brings the first unit under it, and shrunk with each later unit. Never loosened.
- [ ] **Moves separate from edits**: `refactor(<area>): move …` commits apart from edit commits.
- [ ] **Re-count before designing**: done above; DESIGN re-checks if code moved since.
- [ ] **Mutation last, on frozen code**: StrykerJS score on the touched files does not drop.
- [ ] **Architecture docs in the same change**: `ARCHITECTURE.md` §12 where the frontend layout or rule changes.
- [ ] **One context at a time**.
- [ ] **Terminology**: new names follow ADR-270's glossary and the seeded default terms.
- [ ] **Cross-cutting re-check**: RBAC (shared hook through `useRbac()` / `RbacService`, no direct
      `/api/latest/authorization/my-summary` fetch), Lighthouse-Clients N/A (no wire change), Website N/A (nothing
      visible), usage data N/A (behaviour-free), UI sketch N/A unless a split needs a visible change (then stop and
      sketch).
- [ ] **No release notes** unless the maintainer says so.
- [ ] **Coordination with #6473**: the `ModifyConnectionSettings` split is on `main` before #6473 slice 05 starts.

## Wave: DESIGN / [REF] Scope, route and measured facts (2026-11-12)

Route: spine wave DESIGN, then host-led `nwdes` DELIVER (`/nw-deliver`), no lane. Size L, cut into 6 slices in 2 nwdes features. Paradigm: preserved as the repo has it (React function components and hooks, project rule object-oriented): no class is added; the cohesive owner of each decision is a hook or a module, never the page. Read first, as asked: `test-writing/SKILL.md` ends "End of SKILL.md"; `references/frontend.md` ends "End of frontend.md".

Re-count (HEAD `a50825717`, the Epic's own `find`): 3 production files over 1000 lines (2074, 1063, 1062); next 992, 989, 970, 957 (`MetricsService.ts`), 940 (`src/tests/ReportPrintTestKit.tsx`, test support, not production).

Facts read from the code (not inferred):
- `BaseMetricsView.tsx` is two things: l.1-1346 are pure builders (`buildWorkItemLookup`, `buildWidgetFooters`, `buildViewData`, `buildPbcNodes`, `buildWidgetNodes`, four frame helpers) and l.1347-2074 is one 730-line component. Only `buildViewData` and `buildWorkItemLookup` are exported for tests; the component has 4 importers (`TeamMetricsView`, `PortfolioMetricsView`, two tests). `BaseMetricsView.test.tsx` l.6970-7358 (two `describe`s, no `render(` or `screen.`) test those two functions.
- `ragRules.ts` is 31 independent pure functions, one per widget, no shared table; importers: `BaseMetricsView.tsx`, `blockedMaxAgeRag.ts` and `.test.ts` (type `RagTerms` only), `ragRules.test.ts`. It grew past 1000 since the brief.
- `ModifyConnectionSettings.tsx`: the component is l.302-1062 (760 lines). Helpers and copy l.80-152, `ConnectionDefaultsSection` l.153-286, load effect l.408-514, save flow l.629-730, JSX l.779-1059. Its only `/settings/license` link is l.878, inside the OAuth upgrade notice. **The return path of Save and Cancel is not in this file**: `EditConnection.tsx` l.137 holds `navigate("/")`. `layerBoundaries.enforcement.test.ts` lists two leaks from this file (`AdditionalFieldsEditor`, `WriteBackMappingsEditor`); the split keeps both imports in the same file, so that list does not change.
- RBAC: `rbacService` member and group-mapping methods are called only from `TeamDetail.tsx` and `PortfolioDetail.tsx` (non-test grep). `my-summary` appears only in `RbacService.ts` and its test. `TeamDetailAccessTab.test.tsx` and `PortfolioDetailAccessTab.test.tsx` have 2 rows each, none about assign, remove or the "Failed to ..." messages; `TeamDetail.test.tsx` l.917 and `PortfolioDetail.test.tsx` l.687 pin `createGroupMapping`. So the handlers' error paths are **untested today**.
- Probe (Fallow 3.39.0): `fallow dupes --mode semantic --min-lines 8 --min-tokens 50 -f json src/pages/Teams/Detail/TeamDetail.tsx` reports the 86-line clone `PortfolioDetail.tsx` l.122-207 and also two unrelated clones between the same two files (JSX l.549-567/620-636 and l.48-61/57-71). So a dupes check can see the RBAC copy, but a pair-level dupes ratchet would also list the other clones. Not used for the guard.

## Wave: DESIGN / [REF] Decisions

| Id | Decision | One-line why |
|---|---|---|
| D-6393-1 | Order: S1 `ModifyConnectionSettings`, then ragRules, then BaseMetricsView, then RBAC; S6 optional | #6473 slice 05 must find the file already split; the rest has no ordering constraint |
| D-6393-2 | `ModifyConnectionSettings` splits into 5 files: `ConnectionDefaultsSection.tsx`, `connectionFormRules.ts` (copy and pure helpers), `OAuthPremiumNotice.tsx`, `ConnectionAuthSection.tsx`, `useConnectionSave.ts` (validate, 409, update question) | One reason to change each; result about 600 lines; the two layer-leak imports stay in the parent |
| D-6393-3 | The `/settings/license` link moves into `OAuthPremiumNotice.tsx` (about 25 lines) | #6473's one link in this file then lands in a file that does nothing else; return path is in `EditConnection.tsx`, which is not split |
| D-6393-4 | `ragRules.ts` is split by metric family into `ragRulesOverview.ts` (WIP, Blocked, Stale, FeaturesWorkedOn, Predictability, FlowEfficiency, about 175 lines), `ragRulesTimeAndAge.ts` (SleRisk, CycleTimePercentiles, TotalWorkItemAge, CycleTimeScatterplot, WorkItemAgeChart, TotalWorkItemAgeOverTime, WorkItemAgePercentiles, CumulativeStateTime, about 420) and `ragRulesVolumeAndFlow.ts` (StartedVsClosed, Throughput, WipOverTime, SimplifiedCfd, WorkDistribution, FeatureSize, EstimationVsCycleTime, LoadBalance, Arrivals, Pbc, about 450), with `RagResult` and `RagTerms` in `ragTypes.ts`; no barrel | Pure and independent functions, fast tests; a written justification would still leave a file that grows by one function per widget |
| D-6393-5 | `BaseMetricsView.tsx` gives away `workItemLookup.ts`, `widgetFooters.ts`, `widgetViewData.ts`, `widgetNodes.tsx`, `dashboardFrame.ts` (S3, S4) and, in S6, `useNamedPercentilesScope.ts` and `useStateDrillDown.ts` | Pure builders first (no effect can be lost); the two effectful hooks last and alone |
| D-6393-6 | The 390 DOM-free test lines of `BaseMetricsView.test.tsx` move to `workItemLookup.test.ts` and `widgetViewData.test.ts` with their code | Rule 1 and 8 of the frontend half: a rule on a function, not on a render; moves to the fast tier |
| D-6393-7 | No re-export from `BaseMetricsView.tsx` for moved functions; importers (one test) change their import | A re-export keeps the dependency alive and hides the move |
| D-6393-8 | `DeliverySection` (992), `DeliveryCreateModal` (989), `useMetricsData` (970) are **guarded only**, not split | Under the line, already partly decomposed (`useDeliveryFormQueries`, `DeliverySourceTab`, ...); splitting them is a second decision, see Q1 |
| D-6393-9 | The size ratchet is one enforcement test `src/fileSize.enforcement.test.ts`, a source scan with a list of offenders by name (ADR-268) | Fallow has no file-size rule (`fallow --help`); a scan with a name list is the pattern ADR-268 names for "which file" questions |
| D-6393-10 | One hook `hooks/useScopedAccess.ts` for members and group mappings of a Team or a Portfolio, taking the scope kind and id, using `rbacService` from `ApiServiceContext` and `useRbac()` untouched; messages stay word for word | Both pages already call `rbacService`; same behaviour, one owner |
| D-6393-11 | RBAC guard: a second enforcement test `src/rbacAccess.enforcement.test.ts`: the member and group-mapping methods of `rbacService` are called only from `hooks/useScopedAccess.ts`, and `my-summary` appears only in `RbacService.ts`; empty allow-lists | A caller question, so a source scan; a dupes ratchet would list unrelated clones (probe above) |
| D-6393-12 | Characterization rows for the untested handlers are written **before** the extraction and observed GREEN on the old code | A behaviour-free move needs an oracle that exists on both sides |
| D-6393-13 | Mutation: new Stryker config listing the new files by file, not line ranges; baseline = old files measured once at the start on frozen code; `vitest.stryker.baseview.ts` `include` gets the moved `.test.ts` files | Moving tests out of the included spec would silently lower the score otherwise |
| D-6393-14 | Not an observable change: the "Failed to ... team members." texts say "team" and "portfolio" in lower case, not the configured term | Kept as is (behaviour-free); noted for a later fix, see Q3 |

## Wave: DESIGN / [REF] Reuse Analysis

| Candidate | Where | Decision | Why |
|---|---|---|---|
| `layerBoundaries.enforcement.test.ts` | `src/` | **REUSE its shape** (list by name, `compareWithAllowList`, planted-project scenarios); no shared import | Fallow-specific; the size scan needs no tool. Copying the 8-line `without` and drift helpers is accepted; extract on a third user |
| `oneStalenessRule.enforcement.test.ts` | `src/utils/` | **REUSE its walk** (`readdirSync` over `src`) | Closest source-scan precedent |
| Fallow `dupes`, `health` | `.fallowrc.json` | **REJECT for the guards** | No size rule; dupes would list unrelated clones |
| `useRbac`, `useRbacGate` | `hooks/` | **REUSE unchanged** | Gating stays there; `useScopedAccess` does not decide permission |
| `RbacService` | `services/Api/` | **REUSE unchanged** | Only owner of `my-summary` |
| `whatASaveAsks.ts`, `defaultsKeepingOnlyFields.ts`, `UpdateQuestion.tsx` | `components/Common/Connection/` | **REUSE unchanged** | Already the split of the decision rules |
| `useDateRange`, `useCumulativeStateTimeChoices`, `useCategorySelection` | `pages/Common/MetricsView/` | **REUSE as the style** for S6 hooks | Same folder, same shape |
| `blockedMaxAgeRag.ts` | same | **EXTEND**: import of `RagTerms` points at `ragTypes.ts` | Only edit it needs |
| A generic "wide file" splitter | none | **CREATE_NEW: none** | Nothing owns file size; the one new test is the owner |

## Wave: DESIGN / [REF] Ratchet design

`src/fileSize.enforcement.test.ts`, in the default `pnpm test` and `pnpm test:fast` (it is a `.test.ts`).
- Production file: `.ts` or `.tsx` under `src`, not `*.test.ts(x)`, not under `src/tests/`, name without `TestKit` or `Fixture`, not `*.d.ts`. The scan counts line terminators like `wc -l`; the limit is 1000, a file of more than 1000 offends.
- `TOO_WIDE_TODAY`: a list of the offenders by path, each with a plain-words reason. It starts, in the S1 range, with `pages/Common/MetricsView/BaseMetricsView.tsx` and `pages/Common/MetricsView/ragRules.ts` (not `ModifyConnectionSettings.tsx`: it is under the line when the test lands, as the last commit of S1). It fails both ways: a new file over the line fails naming path and length ("split it by responsibility, do not list it"); a listed file at or under the line fails until its entry is deleted. The entry goes in the same commit that brings the file under (S2 for `ragRules.ts`, S4 for `BaseMetricsView.tsx`). The list ends empty and the rule stays.
- Its own scenarios in the same file, on planted temp projects as `layerBoundaries` does: 1001 lines is reported, 1000 is not, a `.test.tsx` of 1500 lines is not, `src/tests/Kit.tsx` is not, a listed file that is now short is stale, a scan that cannot read the folder fails loudly instead of returning "none".
- The 3 near files (992, 989, 970) are not listed; crossing the line makes the next change fail and split in its own change. Limit: the check sees length only, not cohesion.
- Time: one directory walk of about 700 files, expected well under 1 s; per-test budget from `vitest.config.ts` applies; no derogation needed.

## Wave: DESIGN / [REF] Slices (superseded by "Slices (current)" below)

Two nwdes features. **Feature A `story-6393-wide-units-a`** (S1-S5), **Feature B `story-6393-wide-units-b`** (S6, optional, see Q1). One oracle file per slice (lesson from #6602).

| Slice | Delivers, in plain words | Commits (move then edit) | Oracle | Size |
|---|---|---|---|---|
| S1 | The connection form lives in five small files; the size rule exists and names the two files still too wide | `refactor(connections): move ConnectionDefaultsSection`; `move form copy and rules to connectionFormRules`; `move OAuthPremiumNotice`; `move ConnectionAuthSection`; `extract useConnectionSave` (edit); `test(frontend): files over 1000 lines are listed by name` | `ModifyConnectionSettings.test.tsx`, `.parentSources.test.tsx`, `.directionTips.test.tsx`, `whatASaveAsks.test.ts`, `EditConnection*.test.tsx`, `CreateConnectionWizard.test.tsx`; new `fileSize.enforcement.test.ts`; Playwright `ConnectionDefaults.spec.ts` and the connection-creation `@screenshot` (Screenshots.spec.ts l.1344, `--grep` that one) | 1 day |
| S2 | `ragRules.ts` becomes three family files plus a types file | `move` x3 (one per family, with their `describe` blocks from `ragRules.test.ts` to matching `.test.ts` files); `edit: point the importers at the new files and delete the entry` | all moved `.test.ts`, `blockedMaxAgeRag.test.ts`, `BaseMetricsView.test.tsx` | 0.5 day |
| S3 | The pure builders of the metrics view leave `BaseMetricsView.tsx` | `move workItemLookup`; `move widgetFooters`; `move widgetViewData`; `move the 390 DOM-free test lines to .test.ts files` | `workItemLookup.test.ts`, `widgetViewData.test.ts`, `BaseMetricsView.test.tsx`, `.loading.test.tsx`, `.cumulativeStateTimeDialog.test.tsx`, Playwright `@screenshot` team metrics widgets (l.516) | 1 day |
| S4 | The widget nodes and the frame helpers leave it too; the file is under 1000 and its list entry is deleted | `move widgetNodes`; `move dashboardFrame`; `edit: delete the BaseMetricsView entry` | as S3 plus `@screenshot` portfolio metrics widgets (l.1225) and date range selection (l.494) | 0.75 day |
| S5 | Team and Portfolio share one hook for members and group mappings; a test keeps the copies from coming back | `test: characterize assign, remove, failed load and failed assign on both Access tabs` (GREEN on old code); `refactor: extract useScopedAccess from TeamDetail`; `refactor: use it in PortfolioDetail`; `test: hook rows (function layer) and shrink the page rows to one wiring row each`; `test: rbacAccess enforcement` | characterization rows, then `useScopedAccess.test.ts`, `TeamDetail*.test.tsx`, `PortfolioDetail*.test.tsx`; Playwright `RoleBasedAccessControl.spec.ts` (Access tab cases, run as one `--grep`) | 0.75 day |
| S6 (B) | The metrics view's two effectful parts become hooks | `move useNamedPercentilesScope`; `move useStateDrillDown` | `BaseMetricsView.loading.test.tsx`, `.cumulativeStateTimeDialog.test.tsx`, `BaseMetricsView.test.tsx` | 0.5 day |

Fast loop in every slice: `cd Lighthouse.Frontend && pnpm test:fast --changed`, after a component edit `pnpm test --changed`, plus the named oracle file, for example `pnpm test src/components/Common/Connection`. Full gates once at the end of a slice and before each push: `pnpm test`, `pnpm build` (Biome and `tsc`, check `git status` after; mind the docs-symlink hazard in memory). Playwright and `@screenshot` run locally by me, one test per `--grep`, with a premium licence. `ctx_patch` can write `[REDACTED]` into token-like lines (the `type={option.isSecret ? "password" : "text"}` lines in S1): `git grep -n REDACTED` before each commit.

Verification argv per slice: `pnpm test <oracle files>` exits 0 and the `Time budget:` line ends with 0 offenders; before and after each move the sorted list of test ids for the touched files is equal (`pnpm exec vitest list --filesOnly` plus `vitest run --reporter=json`, ids compared), except for rows deliberately moved to a new file, whose id list is mapped one by one.

## Wave: DESIGN / [REF] Deliberate-break table (each planted change must turn the named test red; then reverted)

| Slice | Planted change | Must fail |
|---|---|---|
| S1 | In `OAuthPremiumNotice.tsx` remove the link text | `ModifyConnectionSettings.test.tsx` "renders Upgrade affordance and not OAuthAuthForm when .oauth is selected and user lacks premium" |
| S1 | In `useConnectionSave.ts` return `CONCURRENCY_CONFLICT_COPY` for every error | "does not blame someone else when the save fails for another reason" |
| S1 | In `ConnectionAuthSection.tsx` drop `readOnly` on the OAuth locked fields | "locks Client ID / Client Secret / Jira URL as read-only in edit mode for OAuth methods" |
| S1 | `fileSize` limit 1000 to 1100 | the ratchet's 1001-line scenario |
| S2 | Swap two bands in `computeCycleTimePercentilesRag` after the move | its `describe` in the moved `.test.ts` |
| S2 | Make `ragTypes.ts` miss `blocked` in `RagTerms` | `tsc -b` in `pnpm build` (not a test) |
| S3 | In `widgetViewData.ts` drop `sleRiskColumn` from `inFlight` | "which lists carry the risk column" |
| S3 | In `workItemLookup.ts` let a Feature overwrite a Team Work Item | `buildWorkItemLookup` row on `ownerKind === "team"` |
| S3 | In `widgetFooters.ts` swap amber and red thresholds | `BaseMetricsView.test.tsx` row "widget-rag-aging" amber |
| S4 | In `dashboardFrame.ts` make `isPlaced` always true | the `.loading.test.tsx` row for estimation vs cycle time |
| S4 | Re-add a 1001-line file | `fileSize.enforcement.test.ts` names it |
| S5 | In the hook, call `upsert` without `loadMembers` after | hook row "assign reloads the members" and the page wiring row |
| S5 | Call `rbacService.removeGroupMapping` from a page again | `rbacAccess.enforcement.test.ts` names the file |
| S5 | Change "Failed to update team member role." to "portfolio" | hook row for scope Team |
| S6 | Drop the generation bump in the reset effect | `.loading.test.tsx` row for a scope chosen on another team (if none exists, the acceptance designer adds it before S6) |
| S6 | Remove `selectedItemIds` from the drill-down query key | `.cumulativeStateTimeDialog.test.tsx` |

## Wave: DESIGN / [REF] Formal verification and algebra

Not applicable, with reasons. A file split and one hook extraction change no state, transition, protocol, ordering or concurrency: the sets of states and effects are the same before and after, only their file differs. The laws that matter are guarantee-style and are checked by running code: (1) test ids and outcomes equal before and after, (2) rendered output equal (`@screenshot`, Vitest), (3) effect dependency arrays and hook call order equal, read in the diff, (4) the two new scans refuse a planted case. No Quint, TLA+ or Agda model is written and no checker was run; none is claimed. Construction closure: `input -> useScopedAccess(scope) -> TeamDetail/PortfolioDetail Access tab -> ScopedMembershipManager` is acyclic; runtime `DESIGNED_NOT_BUILT`. Property tests: none broad; the rag rules keep their finite example rows (a finite domain is a table, not a property).

## Wave: DESIGN / [REF] Self-falsification

| Counterexample | What it would show | Answer |
|---|---|---|
| A moved effect loses an item in its dependency array, or two effects swap order | A refetch loop or a stale scope | Pure builders move first (S3, S4); effects only in S6, alone, diff read line by line, loading tests named |
| A test mocks the moved module and stays green regardless | A check that cannot fail | `vi.mock` ids are module paths, so chart mocks keep working after the move; each slice has a plant that turns a named test red |
| `ctx_patch` writes `[REDACTED]` into the moved `type=` lines | Password fields become text or the build breaks | `git grep REDACTED` gate; `ConnectionDefaults.spec.ts` |
| Stryker score drops because the moved tests are no longer in the run | False "held" | D-6393-13: configs list files, `include` extended, baseline on the old files first |
| Stryker line ranges in old configs point at lines that moved | Mutants skipped | New config by file; old per-feature configs are history |
| The RBAC characterization rows pass because both pages share one mock | Proves nothing | Rows run on both pages and have the plants above |
| The ratchet counts `src/tests/*TestKit.tsx` or generated files | Wrong offender | Scenario for each exclusion; a TestKit at 940 would otherwise be next |
| A hook extraction changes the `useCallback` identity and re-renders children | Visible flicker | Dependencies copied as they are; `@screenshot` and loading rows; open check in review |
| `fileSize` passes because the walk missed a folder | False green | Scenario that plants a deep file; failure on unreadable folder |
| #6473 slice 05 edits `ModifyConnectionSettings.tsx` anyway (stepper links, banner) | Merge conflict | Those edits are in other files; the one link is in `OAuthPremiumNotice.tsx`; tell #6473 when S1 is pushed |

Second stressor set (walked against the finished design, not executed): a fourth file crosses 1000 lines during S3 (fails the new test, names it, fix by split, not by list); #6473 lands first (S1 rebases onto a one-line link change, trivial, but the maintainer decides, never an autonomous rebase); a screenshot differs by under the pixel threshold (the old PNG is kept silently: compare element text and box in the Vitest rows, not only the PNG).

## Wave: DESIGN / [REF] Cross-cutting answered

| Item | Answer |
|---|---|
| RBAC | The shared hook keeps `rbacService` and `useRbac()`; gating unchanged; `rbacAccess.enforcement.test.ts` keeps `my-summary` in `RbacService.ts` only. Backend not touched |
| Lighthouse-Clients | N/A, no wire change |
| Website | N/A, nothing visible |
| Usage data | N/A, behaviour-free; no event |
| UI sketch | N/A: no visible change in any split. A split that needs one stops and returns here (open question) |
| Loading / failure looks | Unchanged; the loading tests are in the oracle |
| Terminology | New names are code names only; user texts stay word for word (D-6393-14 notes the lower-case team and portfolio texts) |
| Context map (ADR-270) | No new concept, context or edge; the Delivery and Metrics code stays where it is until FE2 |
| ARCHITECTURE.md §12 | One bullet in S1 for the size rule, one in S5 for the hook and its guard, same change; `brief.md` l.801 names the drill-down in `BaseMetricsView`: re-read after S6 |
| Release notes | None |
| Mutation | Last, on frozen code, per D-6393-13 |

## Wave: DESIGN / [REF] Open questions for the maintainer (answered, see Maintainer decisions)

1. **Do the 3 near files and S6 get done?** Recommendation: S6 yes (the component stays a 730-line function after S4, where "split by responsibility" bites); the 3 near files no, guarded only, because they sit under the line and are already partly decomposed. Option B: split them as a third feature (about 2 days, not designed). Changes: slice count and Feature B.
2. **S1 first, in its own push, before #6473 slice 05?** Recommendation: yes, push S1 alone and tell #6473. Cost: the Story stays open while S2-S5 follow.
3. **The "Failed to update team member role." texts say "team" and "portfolio", not the configured terms.** Recommendation: keep them in this Story, file a separate bug. Changes: a visible text change, so it needs a sketch.
4. **#6473's slice 05 text says the return path changes in `ModifyConnectionSettings.tsx`; the code has it in `EditConnection.tsx` l.137.** Recommendation: tell #6473's owner; no design change here.

## Wave: DESIGN / [REF] Architecture summary for the maintainer

**1. Shape, in hexagonal terms (frontend).**

```
Before                                    After
pages/Common/MetricsView                  pages/Common/MetricsView
  BaseMetricsView.tsx (2074)                BaseMetricsView.tsx  (component, ~850; ~600 after S6)
    builders + component in one file          workItemLookup.ts, widgetFooters.ts, widgetViewData.ts,
  ragRules.ts (1063)                          widgetNodes.tsx, dashboardFrame.ts   (pure builders)
                                              ragTypes.ts + 3 ragRules* family files
components/Common/Connection              components/Common/Connection
  ModifyConnectionSettings.tsx (1062)       ModifyConnectionSettings.tsx (~600) -> ConnectionDefaultsSection,
                                              ConnectionAuthSection, OAuthPremiumNotice,
                                              connectionFormRules, useConnectionSave
pages/Teams|Portfolios Detail             hooks/useScopedAccess.ts  <- TeamDetail, PortfolioDetail
  4 duplicated handlers each                -> rbacService (ApiServiceContext); gating via useRbac()
src/                                      src/fileSize.enforcement.test.ts, src/rbacAccess.enforcement.test.ts
```

New: the files above, the hook, two enforcement tests. Modified: the three wide files, both detail pages, `blockedMaxAgeRag.ts` (one import), two docs bullets. Unchanged: routes, DTOs, backend, `RbacService`, `useRbac`, trackers, all driven adapters. No new external call.

**2. Patterns weighed.**

| Pattern | Chosen / rejected | Why |
|---|---|---|
| Single Responsibility: split by reason to change | Chosen | Each new file has one owner and a test beside it |
| Extract hook (cohesive owner of state plus handlers) | Chosen for RBAC and S6 | The data and its decisions live together, not in the page |
| Pure builder module over a long component | Chosen | Tested as a function, fast tier |
| Ratchet by name, both ways (ADR-268) | Chosen | Shows which file was fixed; never loosened |
| Source scan for "who calls this" | Chosen for the RBAC guard | Names file and rule; Fallow has no such rule |
| Fallow `dupes` ratchet | Rejected | Lists unrelated clones between the same files |
| Barrel / re-export from the old file | Rejected | Keeps the old dependency and hides the move |
| One generic `useScopedAccess` vs two hooks | Chosen one | Same behaviour, two scope names; a mode flag would be rejected only if behaviour diverged |
| Strategy per tracker for the connection form | Rejected | No behaviour differs; it would be a new abstraction |
| Count as a baseline | Rejected | Hides which file was fixed (ADR-268) |
| Composition over inheritance | Chosen | Hooks and components compose; no base class |

## Wave: DESIGN / [REF] Maintainer decisions (2026-11-12)

These answer the four open questions above and **supersede** the slice table, the deliberate-break table, the self-falsification table, the open questions and the architecture summary above where they differ; the sections below are the current ones.

| Id | Decision |
|---|---|
| M-6393-1 | S6 (BaseMetricsView hooks) is in. |
| M-6393-2 | The three near-line files are **split** in FE5, not guarded only: `DeliverySection.tsx` (992), `DeliveryCreateModal.tsx` (989), `hooks/useMetricsData.ts` (970). D-6393-8 is replaced by this. |
| M-6393-3 | S1 is pushed on its own, before #6473 slice 05 starts. |
| M-6393-4 | The six "Failed to ... team/portfolio member(s)..." texts change in S5, in their own `fix(access): ...` commit apart from the moves, pinned by tests. Approved copy: `Failed to load {team} members.`, `Failed to update {team} member role.`, `Failed to remove {team} member.` and the same with {portfolio}; `{team}` and `{portfolio}` are `getTerm(TERMINOLOGY_KEYS.TEAM)` and `getTerm(TERMINOLOGY_KEYS.PORTFOLIO)` lower-cased. With the default terms the text is unchanged. D-6393-14 is replaced by this. |
| M-6393-5 | Rigor profile `standard` for every slice (sonnet crafter and acceptance designer, haiku reviewers). Per slice: refactor and review; push per slice; mutation once at the end. |
| M-6393-6 | FE2 will later move the Delivery pair into a Delivery module: the new Delivery files stay in the one folder `pages/Portfolios/Detail/Components/DeliveryGrid/`, no barrel (the existing `index.ts` is not touched). |

## Wave: DESIGN / [REF] The three near-line splits (read from the code, 2026-11-12)

**Ratchet: nothing more is needed.** Each file is under the 1000 line threshold, so none is ever listed; the threshold stays 1000. The slices still lower their line counts so a later change does not cross the line.

**`useMetricsData.ts` (970), S7.** l.49-341 are types, the owner key and a query helper; l.342-970 are the hook, a flat list of about 40 `useMetricsQuery` calls plus one effect. Moves: `metricsApplicability.ts` (the `is...MetricsService` guards, `applicabilityFor`, `narrowsAChartAlreadyDrawn`, l.132-225, pure), `metricsOwnerKey.ts` (`OwnerType`, `MetricsOwnerKey`, `metricsOwnerKeyOf`, the request types, l.241-277), `useMetricsQuery.ts` (`useMetricsQuery`, `LastDrawn`, `ownerOf`, `listOf`, `dayOf`, l.270-341). Result about 750 lines. The hook's query calls are **not** split into sub-hooks: React runs hooks in call order and the order the queries start is observable (requests, tests), so a family split is a behaviour risk this Story does not take. Eight files import `MetricsOwnerKey` / `MetricsOwnerRequest` from `hooks/useMetricsData`: their import lines change (edit commit, not part of a move). **`layerBoundaries.enforcement.test.ts` lists `useMetricsData.ts -> pages/Common/MetricsView/categoryMetadata.ts` and `-> widgetStatus.ts`**: the file that holds those imports after the move gives the pair a new importing name, so the list entries are renamed in the same commit (ADR-268: the list is part of the test; the number of leaks does not grow). Oracle: `useMetricsData.test.ts`, `usePbcOverTime`, `useFilteredView` and `useCumulativeStateTimeChoices` tests, `BaseMetricsView*.test.tsx`, `layerBoundaries.enforcement.test.ts`, `@screenshot` team metrics (Screenshots.spec.ts l.516).

**`DeliverySection.tsx` (992), S8.** The component is l.450-868 (JSX l.613-868); before it sit helper components and copy (l.80-449), after it two components (l.869-992). Moves: `UnbindConfirmationDialog.tsx` (l.869-937), `WorkItemsTab.tsx` (l.938-992), `deliveryFeatureRow.tsx` (`DeliveryRowActions`, `SelectionModeMarker`, `OwningTeams`, `featureColumns`, l.164-403, about 240 lines), `deliveryHeaderCopy.tsx` (`howTheFeaturesWereChosen`, `WhatIsWrongWithTheSource`, `whatTheHeaderChipSays`, l.94-163 and 404-432, about 100 lines). Result about 480 lines. Oracle: the ten `DeliverySection.*.test.tsx` files (archive, beyondTheHorizon, likelihoodCopy, metrics, notes, overdue, provenance, publishRefused, sourceUnavailable, plain), `DeliveryDialogTestKit.tsx` as support, Playwright `DeliveryMetrics.spec.ts` and the `@screenshot` of retiring a Delivery (Screenshots.spec.ts l.449).

**`DeliveryCreateModal.tsx` (989), S9.** l.114-470 are presentational pieces and the selection tab content, l.472-580 are pure form rules, l.582-989 is the dialog (state, handlers, two effects, JSX). Moves: `DeliverySelectionContent.tsx` (`ManualSelectionContent`, `RuleBasedContent`, `SelectionModeContent`, their prop types, `selectionTabContent`, l.173-374), `DeliverySelectionTabRow.tsx` (`SelectionTabButton`, `BusySelectionTab`, `SelectionTabRow`, l.375-470), `deliveryFormRules.ts` (`dateInputValue`, `offeredTabs`, `boundSourceSelection`, `isValidFutureDate`, `nameErrorOf`, `dateErrorOf`, `getFirstBlockingError`, l.472-580, pure, new `.test.ts` rows move down from the render tests where a row only proves one of these rules), `DeliveryDialogParts.tsx` (`LoadingSpinner`, `SchemaLoadError`, `PremiumFeatureNotice`, `ValidationButton`, `MatchedFeaturesAlert`, `MatchedFeaturesGrid`, l.114-171). Result about 470 lines. **Semantic-duplicate check before CREATE_NEW:** `deliverySelectionTabs.ts` already owns the tab types and keys, and `deliverySelectionBody.ts` the body; the crafter reads both first and puts `offeredTabs` there if it is the same knowledge (EXTEND), else `deliveryFormRules.ts`. Oracle: the nine `DeliveryCreateModal.*.test.tsx` files (edit, featureListAndRuleFields, licenceNeverKnown, loadsOnce, neverAGuess, rulebased, sourceBinding, sourceValidation, plain), `deliverySelectionTabs.test.ts`, Playwright `DeliverySourcePicker.spec.ts`.

## Wave: DESIGN / [REF] Slices (current; replaces the earlier table)

Two nwdes features, at most 5 slices each. **Feature A `story-6393-wide-units-a`**: S1-S5. **Feature B `story-6393-wide-units-b`**: S6-S9. Profile `standard` throughout. One oracle file per slice. Push per slice; S1 first and on its own.

| Slice | Delivers, in plain words | Commits (move, then edit) | Oracle | Size |
|---|---|---|---|---|
| S1 | The connection form lives in five small files; the size rule exists and names the two files still too wide | as in the earlier table | as in the earlier table | 1 day |
| S2 | `ragRules.ts` becomes three family files plus a types file | as before | as before | 0.5 day |
| S3 | The pure builders of the metrics view leave `BaseMetricsView.tsx` | as before | as before | 1 day |
| S4 | Widget nodes and frame helpers leave it; under 1000; list entry deleted | as before | as before | 0.75 day |
| S5 | Team and Portfolio share one hook for members and group mappings; a test keeps the copies from coming back; the six failure texts follow the renamed Team/Portfolio term | (1) `test: characterize assign, remove, failed load, failed assign on both Access tabs` with the **default** texts, GREEN on the old code; (2) `refactor: extract useScopedAccess from TeamDetail`; (3) `refactor: use it in PortfolioDetail`; (4) `test: hook rows at the function layer; page rows shrink to one wiring row each`; (5) `test: rbacAccess enforcement`; (6) `fix(access): failure texts use the configured Team and Portfolio term` (the only commit that changes behaviour; its renamed-term rows are RED before it, GREEN after) | Characterization rows (1) stay GREEN through (2)-(5) unmodified. Fix oracle in `useScopedAccess.test.ts` (hook, provider wrapper): for each of the three actions (load fails, assign fails, remove fails) and each scope, **with Team renamed to "Squad" and Portfolio to "Program"** the message is `Failed to load squad members.`, `Failed to update squad member role.`, `Failed to remove squad member.` and `... program ...`; **with the default terms** the six texts equal today's (`team`, `portfolio`). One wiring row per page (`TeamDetailAccessTab.test.tsx`, `PortfolioDetailAccessTab.test.tsx`) shows one renamed text on screen. Playwright `RoleBasedAccessControl.spec.ts` Access tab cases (`--grep` one). Sketch: none needed, the copy is approved text | 0.75 day |
| S6 | The metrics view's two effectful parts become hooks | `move useNamedPercentilesScope`; `move useStateDrillDown` | `BaseMetricsView.loading.test.tsx`, `.cumulativeStateTimeDialog.test.tsx`, `BaseMetricsView.test.tsx` | 0.5 day |
| S7 | `useMetricsData.ts` gives away its owner key, applicability rules and query helper | `move metricsApplicability`; `move metricsOwnerKey`; `move useMetricsQuery`; `edit: eight importers and two layer-list entries` | as in the `useMetricsData` paragraph above | 0.5 day |
| S8 | `DeliverySection.tsx` gives away its dialogs, row parts and header copy | `move UnbindConfirmationDialog`; `move WorkItemsTab`; `move deliveryFeatureRow`; `move deliveryHeaderCopy` | as in its paragraph | 0.75 day |
| S9 | `DeliveryCreateModal.tsx` gives away its selection content, tab row, form rules and small parts | `move deliveryFormRules (+ tests down)`; `move DeliverySelectionContent`; `move DeliverySelectionTabRow`; `move DeliveryDialogParts` | as in its paragraph | 0.75 day |

Fast loop and full gates as in the earlier Slices section. Mutation: once, at the end, on frozen code, config by file (D-6393-13), covering the files of all slices.

## Wave: DESIGN / [REF] Deliberate-break rows added (S5 to S9)

| Slice | Planted change | Must fail |
|---|---|---|
| S5 | In the hook, drop `.toLowerCase()` of the term | the renamed-term row ("Squad" gives `Failed to load Squad members.`) |
| S5 | Hard-code "team" in the portfolio scope text | the Portfolio renamed-term row |
| S5 | Revert only the `fix(access)` commit | the six renamed-term rows RED, the characterization rows with default texts stay GREEN |
| S7 | In `metricsApplicability.ts` make `providesSleRisk` always true | `useMetricsData.test.ts` "asks for the team alone, with no window to disagree about" or the risk rows |
| S7 | In `useMetricsQuery.ts` drop the `lastDrawn` update | the `.loading.test.tsx` row that keeps the previous answer dimmed |
| S7 | Leave the two moved layer imports unrenamed in the list | `layerBoundaries.enforcement.test.ts` names stale and new pairs |
| S8 | In `UnbindConfirmationDialog.tsx` make the confirm button always enabled | `DeliverySection.archive.test.tsx` or the unbind row in `DeliverySection.test.tsx` (the acceptance designer names the exact row before S8) |
| S8 | In `deliveryHeaderCopy.tsx` change "Overdue" wording | `DeliverySection.overdue.test.tsx` |
| S9 | In `deliveryFormRules.ts` make `isValidFutureDate` accept yesterday | the moved `deliveryFormRules.test.ts` row and the matching `DeliveryCreateModal.test.tsx` row |
| S9 | In `DeliverySelectionContent.tsx` drop the premium notice | `DeliveryCreateModal.licenceNeverKnown.test.tsx` |

## Wave: DESIGN / [REF] Self-falsification rows added

| Counterexample | What it would show | Answer |
|---|---|---|
| `useMetricsData` is split into per-family sub-hooks and the queries start in another order | Request order changes, tests flake or a spinner shows later | Not split (above); only pure and helper code moves |
| A moved layer import keeps its old name in `ALLOWED_LEAKS` | The ratchet reports a stale entry and a new leak | The rename is in the same commit as the move; the planted break above |
| The renamed term is read before the terminology loads | The text shows a default word under a renamed instance | The hook reads `getTerm` at the time of the failure, not at mount; the renamed-term row uses a provider that is ready, plus one row where the term arrives after mount |
| Lower-casing breaks a term that is an acronym or has an uppercase second word | "SLE team" becomes "sle team" | The approved copy says lower-cased; stays as approved, listed in the new open question below |
| `DeliveryCreateModal` pieces move into a folder FE2 will also touch | Double move | Same folder, no barrel (M-6393-6); FE2 moves the whole folder |
| A moved Delivery row test mocks a moved module by path and stays green | A check that cannot fail | `vi.mock` ids are module paths, kept; each slice has a plant |
| `offeredTabs` is copied next to `deliverySelectionTabs.ts` | A semantic duplicate | Read both before CREATE_NEW; extend the existing owner |

## Wave: DESIGN / [REF] Open questions (current)

The four earlier questions are closed by the maintainer's answers above. One new question:
1. **Lower-casing a renamed term.** The approved copy lower-cases the Terminology word. A term such as "SLE group" would read "sle group". Recommendation: keep the approved rule (it matches the default texts "team"/"portfolio"); change it only if a real instance shows a bad case. Changes: the S5 fix commit and its renamed-term rows.

## Wave: DESIGN / [REF] Architecture summary (amendments)

```
hooks/useMetricsData.ts (970)        hooks/useMetricsData.ts (~750)
                                       metricsApplicability.ts, metricsOwnerKey.ts, useMetricsQuery.ts
DeliveryGrid/DeliverySection.tsx     DeliveryGrid/DeliverySection.tsx (~480)
 (992)                                 UnbindConfirmationDialog, WorkItemsTab,
                                       deliveryFeatureRow, deliveryHeaderCopy
DeliveryGrid/DeliveryCreateModal.tsx DeliveryGrid/DeliveryCreateModal.tsx (~470)
 (989)                                 DeliverySelectionContent, DeliverySelectionTabRow,
                                       deliveryFormRules, DeliveryDialogParts
useScopedAccess (S5)                 reads getTerm for the six failure texts
```

Patterns: same as before, plus: **extract pure rules to a function module** (chosen for `metricsApplicability`, `deliveryFormRules`: tested without a render); **keep hook call order** (chosen: no sub-hook split of `useMetricsData`); **one folder, no barrel for the Delivery pair** (chosen: FE2 moves a folder, not a facade); **a family split of the 40 queries** (rejected: observable order); **a new Delivery module now** (rejected: FE2's job).

## Wave: DESIGN / [REF] Open question closed in AFK (2026-11-12)

- Lower-casing a renamed term ("SLE group" reads "sle group"): kept the approved rule, as recommended. Revisit only if
  a real instance shows a bad case. Taken in AFK mode; the maintainer may revisit.

## Wave: DEVOPS / [REF] Platform and usage data (2026-11-12)

- **Platform risk: none.** Frontend-only file splits plus one copy change; no route, data, migration, config,
  workflow or deployment change. Existing CI (`ci.yml`, Vitest, Playwright via the verify workflows, Sonar) covers it
  unchanged; the two new enforcement tests run inside `pnpm test`.
- **Usage-data event: N/A.** The Story is behaviour-free apart from six failure texts that now use the configured
  Team and Portfolio words; nothing new is there to be used, so no event would show adoption. `UsageDataEventName`
  and `docs/settings/usagedata.md` are unchanged.
- **Rollout and rollback:** per-slice push to `main` (S1 first, before #6473 slice 05); rollback is a revert of the
  slice's commits. No release notes (behaviour-free; the copy change matches today's text under default terms).
- **Mutation:** StrykerJS once at the end of each nwdes feature, on frozen code, ≥ 80 % on touched files, baseline
  the old files (see DESIGN, Stryker notes).

### Part A EXAMINE outcome (2026-11-12) — gap accepted by the maintainer

Four source-blind EXAMINE runs (haiku) closed unmet, every verdict `observation_gap`; none found a product fault.
Causes: the examiner's own name filter hid rows (run 1); a host `nwdes examine abort` deleted the workspace
(run 2; never abort before an examination — prompt generation creates the workspace); probes spent on `--help`
(run 3); a truncated capture (run 4). Run 4 still observed 892 passing tests, all six default failure texts and
the renamed "Failed to load squad/program members." texts. Not observed: the size-rule scan, the access guard,
the renamed update and remove texts. `frontend-tests` was improved on the way (comma-separated path fragments,
`--help`, message rows quote their text). Maintainer's choice: one more haiku run, then accept the gap; taken.
Other evidence for the four promises: each slice's observed RED, the accepted whole-candidate review, the full
suite (9736 pass), mutation 97.06 %.
