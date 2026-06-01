# Mutation Testing Report — lighthouse-user-survey in-app nudge (slices 04-05)

Date: 2026-06-01. Strategy: `per-feature` (CLAUDE.md), gate ≥ 80% on the feature's logic.
Scope: the nudge surface delivered in steps 04-01 / 04-02 / 05-01.

## Configs

- Backend: `Lighthouse.Backend.Tests/stryker-config.survey-nudge.json` (Stryker.NET 4.14) — run from `Lighthouse.Backend/`: `dotnet stryker -f Lighthouse.Backend.Tests/stryker-config.survey-nudge.json`.
- Frontend: `stryker.config.survey-nudge.mjs` + `vitest.stryker.survey-nudge.config.ts` (StrykerJS 9) — `pnpm exec stryker run stryker.config.survey-nudge.mjs`.

## Backend — 83.33% (45 killed / 50, 0 timeout) — PASS

Mutated: `AppSettingService.cs`, `Seeding/InstallTimestampSeeder.cs`, `API/SurveyNudgeController.cs`.
Tests: `SurveyNudgeSettingsTest`, `SurveyNudgeControllerTest`, `AppSettingServiceTest`, `SystemInfoServiceTest`, `SystemInfoControllerTest`.

Two genuine feature survivors from the first run (81.48%) were killed by added tests:
- `GetInstallTimestamp` absent-returns-null (its null-guard was masked by `SystemInfoService`'s degrade-to-null catch) → `GetInstallTimestamp_WhenAbsent_ReturnsNull`.
- the upsert value-update path → `RecordSurveyNudgeAction_RemindLaterThenNoInterest_UpdatesPersistedNextEligibleInstant`; plus `..._SecondRemindLater_StillQuietsForAboutOneWeek` pins the pre-backoff boundary.

Remaining 5 survivors — accepted:
- `AppSettingService.cs` `await repository.Save()` removals (×2) — equivalent under the unit test double (the in-memory store persists via the Add/Update callbacks regardless of `Save`). Persistence-across-restart is asserted behaviourally by `..._PersistsChoiceSoCadenceSurvivesRestart`.
- 2 survivors in pre-existing non-feature methods (`UpdateRefreshSettingsAsync` `Save`, `GetSettingByKey` exception-message string) — only mutated because the whole file is in scope; outside this feature.

## Frontend — logic 91.11% (PASS), component presentational

| File | Score | Killed/Total | Note |
|---|---|---|---|
| `nudgeEligibility.ts` (pure logic) | **91.11%** | 41/45 | PASS — the eligibility/cadence/fail-closed logic. 4 survivors at the `!value` guard / `!== null` cadence check are equivalent-mutant territory (e.g. `!= null` vs `!== null`); the show/no-show behaviour is asserted across the boundary by the eligibility + cadence tests. |
| `SurveyNudge.tsx` (MUI component) | 53.06% | 26/49 | Presentational. Survivors are `sx` style props (position/zIndex/gap/margins, L92-119) and defensive async-effect optional-chains / cancelled-guards (L43-62) whose null branch the deterministic mocks don't exercise. |
| **Blended** | 71.28% | 67/94 | Below the 80% high mark because of the presentational component. |

Decision: not chased. Per CLAUDE.md (assert business behaviour, skip framework/presentational) and the project's established FE-mutation norm (cf. the state-time-cumulative baseline), brittle assertions on exact `sx` pixel/zIndex values would be testing theater. The component's BUSINESS behaviour — premium-never-renders (KPI-5 guardrail), the three choices recording the right action, the `/survey` link-out, dismiss-as-remind-later — is covered by deterministic RTL assertions (confirmed in adversarial review) and is not among the survivors.

## Verdict

Feature logic clears the ≥80% gate on both stacks (backend 83.33%, FE eligibility 91.11%). Remaining survivors are test-double-equivalent, pre-existing-non-feature, or presentational, each justified above.
