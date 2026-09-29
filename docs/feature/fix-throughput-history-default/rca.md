# RCA — Bug #6071: Lighthouse disagrees with itself about the default throughput history window

Investigated on `main` @ fde113def (2026-09-29). Read-only; no code changed.

## 1. Problem statement (scoped)

A Team's `ThroughputHistory` (rolling throughput window, days) gets a different starting value
depending on which path creates the Team:

| Creation route | Value that lands on the entity | Evidence |
|---|---|---|
| UI wizard (`/teams/new`, no `cloneFrom`) | **90** | `Lighthouse.Frontend/src/components/Common/CreateWizards/CreateTeamWizard.tsx:35`; wizard chosen at `pages/Teams/Edit/EditTeam.tsx:26` (`useWizard = isNewTeam && !hasCloneFrom`); pinned by `CreateTeamWizard.test.tsx:693` |
| UI clone (`/teams/new?cloneFrom=N`) | source Team's value | `EditTeam.tsx:61-70` spreads `sourceSettings` |
| `EditTeam` fallback defaults | **90** | `EditTeam.tsx:77-108`; reached only when `cloneFrom` does not parse to a number — near-dead code |
| Demo data | **30** (entity initializer; not set) | `Factories/DemoDataFactory.cs:39-60` sets no `ThroughputHistory` |
| Direct API `POST /teams` / `PUT /teams/{id}` / `POST /teams/validate` | whatever the caller sends; omitted → **HTTP 400** | `TeamSettingDto.cs:34-35` `[JsonRequired]`; `TeamsController.cs:22` `[ApiController]`; STJ in `Program.cs:320-324`; documented CI incident `docs/ci-learnings.md:639-640` |
| Lighthouse-Clients CLI/MCP (`../lighthouse-clients` @ 3533ad6) | pass-through payload, no default | `packages/client/src/index.ts:2045` `createTeam(payload)`; no `throughputHistory` literal anywhere in that repo |
| Configuration import/export | route no longer exists | removed in 9b3822f42 "Remove configuration export and import"; `src/models/Configuration/ConfigurationExport.ts` is unused residue |
| Backend `new Team` elsewhere | transient only | `API/Helpers/CycleTimeDefinitionValidator.cs`, `TeamsController.cs:148` (validate) |

Reported facts re-verified at fde113def: `Team.cs:17` = 30; `CreateTeamWizard.tsx:35` = 90;
`EditTeam.tsx:80` = 90; `TeamExtensions.cs:39` copies unconditionally — all still true.

**Disproven:** "a create request that omits `throughputHistory` may bind to 0". The DTO property carries
`[JsonRequired]` since 84f5a430d (2025-02-14); System.Text.Json refuses the body and `[ApiController]`
turns that into a 400 (`ci-learnings.md:639-640` records exactly this for another `[JsonRequired]`
field of the same DTO). Not re-executed as a live request in this investigation.

**Still real:** an *explicit* `0` (or negative) is accepted. No server-side range check exists
(`TeamsController.cs:86-135` validates only baseline and state mappings). The frontend is split:
the full settings form blocks `<= 0` (`ModifyTeamSettings.tsx:56-60`), while the Team-detail quick
setting deliberately allows `0` "to unset" (`ThroughputQuickSetting.tsx:83,169-173`, since dcf2db300).

Downstream of 0 (partially verified):
- `Team.GetThroughputSettings` → start = today+1, `numberOfDays` = 0 (`Team.cs:36-38`).
- `TeamMetricsService.cs:890` → empty range → `HasSufficientData` false → "Cannot forecast"
  (release note `docs/releasenotes/releasenotes.md:418`). No crash found.
- Reality check treats it explicitly: `RealityCheckVerdictPolicy.cs:64` `NotAPositiveLength`.
- `ProcessBehaviorSnapshotWriter.cs:153` returns lookback `-1` — behaviour with a negative span not verified.

## 2. Five Whys

### Branch A — two literals disagree (30 vs 90)

- **WHY 1A** New UI Teams get 90, demo/entity-default Teams get 30.
  [`CreateTeamWizard.tsx:35`, `EditTeam.tsx:80`, `Team.cs:17`, `DemoDataFactory.cs:39-60`]
- **WHY 2A** The frontend hard-codes its own create defaults instead of receiving them from the backend.
  [`CreateTeamWizard.tsx:26-55` builds the full DTO literally; no API call for defaults exists]
- **WHY 3A** The backend endpoint that used to supply defaults was removed and the values were
  re-typed by hand into the frontend. [babe81f99 2026-01-23 "Remove Default Team and Portfolio Settings
  from AppSettings" — deletes `getDefaultTeamSettings`, adds `throughputHistory: 90` to `EditTeam.tsx`;
  c599b6978 2026-04-05 copied it into the wizard]
- **WHY 4A** The re-typed value did not match the product's seeded default. The seed was **30**
  (`AppSettingRepository.cs:39` at babe81f99^, `Key = TeamSettingHistory, Value = "30"`, since faff052c4
  2024-07-27; `AppSettingRepositoryTest` pinned `"30"`). The only `"90"` for this key lived in a unit
  test's mocked settings (`AppSettingServiceTest.cs:88` at babe81f99^). *Hypothesis — requires
  verification:* 90 was transcribed from that test fixture rather than from the seed.
- **WHY 5A** **ROOT CAUSE A:** the default has no single named owner. It exists as three independent
  literals in two stacks, and nothing (test, contract, constant) ties them together, so a refactor that
  moves the default across the stack boundary can change it silently.

### Branch B — explicit 0 is storable

- **WHY 1B** A Team can hold `ThroughputHistory = 0` → no forecast. [`ThroughputQuickSetting.tsx:169-173,205`]
- **WHY 2B** The API copies the value with no range check. [`TeamExtensions.cs:39`, `TeamsController.cs:86-135`]
- **WHY 3B** The rule "at least one day" lives only in one frontend form. [`ModifyTeamSettings.tsx:56-60`]
- **WHY 4B** A second UI deliberately treats 0 as "unset", and downstream code grew to tolerate it
  (`RealityCheckVerdictPolicy.cs:64`, "Not set" label `ThroughputQuickSetting.tsx:84`), so the
  invariant was never decided.
- **WHY 5B** **ROOT CAUSE B:** whether 0 is a legal value ("unset") or invalid is an undecided domain
  rule; the Team entity does not own it, so each UI surface decides for itself.

### Branch C — same pattern on Portfolio (scope decision)

- `Portfolio.PercentileHistoryInDays` entity default **90** (`Models/Portfolio.cs:29`, `int?`) vs UI
  create default **0** (`CreatePortfolioWizard.tsx:39`, `EditPortfolio.tsx:64`; was 180 at babe81f99,
  set to 0 in ad5eeef3e/c599b6978). `WorkItemService.cs:718` falls back to 90 only on `null`, so 0 →
  a zero-day window → no percentile sample → falls back to `DefaultAmountOfWorkItemsPerFeature`
  (inferred from `WorkItemService.cs:727`, not run). Same root cause A.
- Also: `Team.FeatureWIP` entity 1 (`Team.cs:7`) vs UI 0 (`CreateTeamWizard.tsx:39`).

## 3. Validation

- A forward: three unlinked literals + a cross-stack refactor → divergence. Yes; commit history shows it.
- B forward: no server rule + a UI that emits 0 → stored 0 → "Cannot forecast". Yes.
- A and B are independent and consistent. The omitted-field symptom in the bug report is not produced
  by either (it does not occur).

## 4. Evidence for which value is intended

For **30**: the seeded product default for ~18 months (faff052c4 → babe81f99); the entity initializer
(since 84f5a430d); docs that describe the default Team range as "last 30 days … (or whatever range that
Team is configured with)" (`docs/metrics/predictability.md:57,117`, ADR-108 line 164 "30 days team / 90
portfolio"); every demo Team.
For **90**: two frontend literals introduced 2026-01-23/2026-04-05 with no recorded rationale; the upper
end of the docs recommendation "between 30 and 90 days" (`docs/teams/edit.md:73-74`); every Team
created through the UI in the last ~8 months.
`docs/teams/edit.md` states no default value.

## 5. Proposed fix (minimal)

1. Backend: `public const int DefaultThroughputHistoryDays = 30;` on `Team`; `Team.cs:17` initialises
   from it. No DB default exists (`LighthouseAppContextModelSnapshot.cs` Sqlite:1320 / Postgres:1382 have
   no `HasDefaultValue`) → **no migration**; persisted rows untouched.
2. Frontend: one exported constant `DEFAULT_THROUGHPUT_HISTORY_DAYS` beside `ITeamSettings`
   (`src/models/Team/TeamSettings.ts`), used at `CreateTeamWizard.tsx:35` and `EditTeam.tsx:80`.
   A backend-served default is not recommended: that endpoint was deliberately removed in babe81f99.
3. Drift guard: a backend unit test pinning `new Team().ThroughputHistory == Team.DefaultThroughputHistoryDays`
   and a Vitest pinning the wizard's saved value to the frontend constant (update `CreateTeamWizard.test.tsx:693`).
   Optional stronger guard: a backend test that reads the TS constant file — judge whether worth it.
4. Omitted field: already a 400. Add one integration test pinning it so a future removal of
   `[JsonRequired]` cannot silently re-open the bind-to-0 path.
5. Explicit 0: depends on decision D2 below.

## 6. Risk

- Persisted Teams: unaffected (initializer only applies to new CLR objects; EF overwrites on load).
- Demo data: unchanged if 30 is chosen; changes demo forecasts/screenshots if 90 is chosen.
- Backend tests: 194 test files construct `new Team` and many rely on the implicit 30 — choosing 90
  on the backend could shift many assertions; choosing 30 changes only frontend tests.
- Frontend: `CreateTeamWizard.test.tsx:693` asserts 90 — must change if 30 is chosen.
- E2E: `Lighthouse.EndToEndTests/tests/helpers/api/teams.ts` sends its own value; not affected.
- Choosing 30 changes behaviour for newly UI-created Teams (shorter window than the last 8 months'
  UI Teams) — user-visible, needs a release note line.

## 7. Open decisions

- **D1 Value.** Recommend **30**: longest-standing seeded default, matches entity, demo, docs wording
  and all backend fixtures; 90 has no recorded rationale. Choose 90 only if the product owner prefers
  it on forecasting grounds — then change the backend, demo data and docs to match.
- **D2 Is 0 legal?** Either (a) reject `ThroughputHistory <= 0` when `!UseFixedDatesForThroughput`
  with 400 and remove the quick setting's "0 to unset", or (b) keep 0 as "unset" and document it.
  Recommend (a) — the full settings form already blocks it and "unset" only produces "Cannot forecast".
- **D3 Portfolio.** Recommend a separate bug for `PercentileHistoryInDays` (90 vs 0) and `FeatureWIP`
  (1 vs 0) so this fix stays narrow.
- **D4** Delete the dead `ConfigurationExport.ts` and the unreachable `EditTeam` fallback? Optional cleanup.

## Maintainer decisions (2026-09-29)

- **D1 — throughput history default: 90 days.** One named constant per stack (`Team.DefaultThroughputHistoryDays`, `DEFAULT_THROUGHPUT_HISTORY_DAYS`); the entity default moves from 30 to 90, so demo data and API-created Teams follow.
- **D2 — throughput history ≤ 0 is rejected server-side** when the Team does not use fixed dates (create and update). The Team quick setting drops "0 to unset". Missing `throughputHistory` keeps its 400, now pinned by a test.
- **D3 — Portfolio folded in.** `PercentileHistoryInDays` default 90 in one named place per stack; the UI stops creating Portfolios with 0, and the backend fallback treats a stored 0 (or less) like null, so Portfolios already saved with 0 get the 90-day window without a migration. `FeatureWIP` default is **0** ("Set Feature WIP" unticked): forecasts treat 0 as 1, so this changes no forecast, only the entity default (1 → 0) to match what the UI has always created.
- **D4 — cleanup included**, as a separate refactor commit: delete the unused `ConfigurationExport.ts`; a malformed `cloneFrom` goes to the wizard, so the Edit pages' hand-typed New-Team/New-Portfolio defaults can be deleted.

Correction to the RCA: the `EditTeam` defaults are reached only for `/teams/new?cloneFrom=<non-number>` (`EditTeam.tsx:26` routes a plain `/teams/new` to the wizard), so "practically unreachable" holds.
