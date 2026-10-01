# Feature Delta — story-6083-over-time-history-fill-on-by-default

> ADO User Story #6083 "Switch the over-time history fill on by default". Story #6053 shipped
> *Fill in past days on over-time charts* (`OverTimeHistoryFill`) as an opt-in Preview, seeded off. This
> story makes it on by default for new and existing instances. The trigger the story names, positive
> feedback on #6053, was confirmed by the maintainer on 2026-10-01.

Lean DISCUSS pass (2026-10-01). **The maintainer explicitly skipped DIVERGE, DESIGN and DEVOPS.** The one
mechanism decision DESIGN would have taken (D3) is recorded and locked here instead, so DISTILL has nothing
left to improvise.

---

## Wave: DISCUSS / [REF] Persona IDs

- **flow-coach**: reads Percentiles Over Time and wants a trend line their stored data already supports.
- **delivery-lead** (secondary): reads PBC Over Time to see whether the process is stable.
- **system-admin** (secondary): owns the switch under Settings → System. Before this change they had to
  know it existed and turn it on.

## Wave: DISCUSS / [REF] JTBD One-Liners

This refines `job-flow-coach-see-the-trend-my-data-already-supports` and
`job-delivery-lead-see-process-stability-trend` (`docs/product/jobs.yaml`). Story #6053 removed the push
force: on a young or intermittently-run instance, the trend was unreachable. What it left was a **habit**
force: an admin has to discover a Preview switch before anyone sees the benefit, and most never will.
Turning it on by default removes that step. The jobs themselves do not change, so `jobs.yaml` is not edited.

## Wave: DISCUSS / [REF] Pre-requisites

- Story #6053 is released (v26.9.24.6) and finalized (`docs/evolution/2026-09-24-story-6053-reconstruct-over-time-history.md`).
- Positive feedback on #6053 has come in. Confirmed by the maintainer on 2026-10-01.
- `OptionalFeatureSeeder` adds a row only when it is missing and never touches `Enabled` on an existing
  row. Every instance that has upgraded since v26.9.24.6 therefore has `OverTimeHistoryFill` with
  `Enabled = false`.
- **Nothing records why a row is off.** A seeded off and an admin's deliberate off look identical: there
  is no local audit of toggles, and `OptionalFeatureToggled` goes only to the opt-in usage-data pipe.

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Verdict |
|----|----------|---------|
| D1 | **Every existing instance gets it on, once.** A seeded off and a deliberate off can't be told apart, so both become on, a single time, on the first start-up of the release that ships this. An admin who turns it off again afterwards is honoured from then on. The release notes say plainly that a previous off has been switched on. This departs from the story text ("instances that explicitly turned it off should stay off"), which can't be done with the data on hand. | Locked by the maintainer, 2026-10-01 |
| D2 | **New instances are seeded on.** The seeder writes `Enabled = true` for `OverTimeHistoryFill`. | Locked |
| D3 | **"Once" is recorded by a marker the seeder writes, not by an EF migration.** When the marker is absent, `OptionalFeatureSeeder` sets the existing row's `Enabled` to true and writes the marker in the same `SaveChanges`. When it is present, `Enabled` is never touched again. The marker is a row in `AppSettings` under a new `AppSettingKeys` constant, not in the `AppSettingSeeder` defaults list. There is no schema change, so the expand-only migration rule doesn't apply and no `CreateMigration` run is needed. The FeatureOrdering carry-over set the precedent: the seeder is where an instance's earlier state turns into the switch's value. Taken here because DESIGN was skipped. | Locked |
| D4 | **It stays a Preview.** `IsPreview` stays `true`. Removing the switch and the badge is #6084. | Locked |
| D5 | **Demo data still doesn't touch the switch.** Loading demo data neither switches it on nor off. | Locked |
| D6 | **Terminology.** The only user-facing text that changes is docs and release-note copy. It uses the configurable terms (*Team*, *Portfolio*, *Work Item*) and names the charts as they appear. | Locked |

## Wave: DISCUSS / [REF] User Stories

### US-01: The over-time charts show the trend the data supports, without an admin having to know about a switch

As a **flow coach**, I want Percentiles Over Time and PBC Over Time to fill in the days Lighthouse missed
without anyone switching it on first, so that a young or intermittently-run instance shows me a trend
on the first open.

`job_id: job-flow-coach-see-the-trend-my-data-already-supports`
(also `job-delivery-lead-see-process-stability-trend`)

#### Elevator Pitch
Before: on a fresh or freshly upgraded instance, *Fill in past days on over-time charts* is off. Percentiles Over Time for a Team added last week shows a handful of points until a System Admin finds the switch.
After: upgrade, then open **Settings → System**. *Fill in past days on over-time charts (Preview)* is on. Open Percentiles Over Time for that Team, and a little later the chart shows a point for each day the stored history can fill.
Decision enabled: the coach reads the trend on its own instead of waiting for weeks of recorded days, and the admin decides whether to leave the fill on rather than whether to turn it on.

#### Acceptance Criteria
- **AC-1.1** On a fresh database, after start-up, `GET /api/latest/optionalfeatures/OverTimeHistoryFill` returns `enabled: true`, and opening Percentiles Over Time starts the fill without any other step.
- **AC-1.2** On an instance whose row is `enabled: false` from a release before this one, the first start-up of this release turns it to `enabled: true`. This holds whether the row was off because it was seeded that way or because an admin switched it off (D1).
- **AC-1.3** If a System Admin switches it off after that start-up, it stays off across any number of restarts. The seeder runs on each one and never turns it back on.
- **AC-1.4** A fresh instance that an admin switches off stays off across restarts, so the marker is written on fresh installs too.
- **AC-1.5** Name, description, `IsPreview = true` and `IsPremium = false` are unchanged, apart from description wording that no longer says "off by default", if any. Only a System Admin can switch it.
- **AC-1.6** `docs/settings/configuration.md` and `docs/metrics/predictability.md` describe the fill as on by default, keep the "take a backup before you switch it on" advice as "take a backup before you upgrade", and keep *Turning it back off*. The release notes tell admins who had it off that it is now on and how to switch it off.

## Wave: DISCUSS / [REF] Out of Scope

- Telling a deliberate off apart from a seeded off, retroactively or by recording toggles from now on. D1 rejected this.
- Removing the switch or the Preview badge (#6084).
- Any change to how the fill works: its bounds, budget, maintenance gate or the filler itself.
- A start-up warning or in-app notice that the switch flipped. The release notes carry that message.

## Wave: DISCUSS / [REF] Cross-cutting Impact

- **RBAC**: **N/A, because** writing an optional feature stays behind `RbacGuard(SystemAdmin)`, and nothing new reads `/authorization/my-summary`.
- **Lighthouse-Clients (CLI + MCP)**: **affected, copy only.** `packages/client/src/index.ts:1036-1039` ("By default it never fills in a day … a Preview, off by default"), `packages/mcp-core/src/index.ts:863,878` ("By default Lighthouse only returns days it recorded … where a System Admin has switched on …") and `skill/SKILL.md:354` describe the off default. Reword them to "on by default; a System Admin can switch it off". No contract change, so this is a patch release of the clients, cut alongside the backend release.
- **Website**: **N/A, because** `/storage/repos/website` doesn't mention the fill or `OverTimeHistoryFill`.
- **Premium**: **N/A, because** it stays free (`IsPremium = false`).
- **E2E**: `PbcOverTime`, `PercentilesOverTime`, `PredictabilityOverTime` and `Screenshots` specs call `switchHistoryFill` explicitly, so the default doesn't affect them. Any spec that relied on the fill being *off* without saying so is a DISTILL check.
- **Usage data (DEVOPS skipped)**: **N/A, because** `OptionalFeatureToggled` already exists and fires on every admin toggle. A rising off-count after this release is the signal that the default is wrong, and nothing new is needed to see it.

## Wave: DISCUSS / [REF] WS Strategy

Strategy **B (extend existing)**: one thin slice and no skeleton. The seeder, the switch and the fill exist
end to end. This slice changes one seeded value and adds a once-only flip.

## Wave: DISCUSS / [REF] Driving Ports

- Application start-up: `OptionalFeatureSeeder.Seed()`.
- `GET` / `POST /api/latest/optionalfeatures/{key}`: the Settings → System row.
- Reading Percentiles Over Time or PBC Over Time: the fill trigger `#6053` already covers.

## Wave: DISCUSS / [REF] Scope Assessment: PASS

One story and one slice. It touches one bounded context lightly (settings seeding), plus docs and client
copy. Well under a day. No oversized signals.

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|-----|--------|-------------|
| `OUT-6083-on-after-upgrade` | 100 % of upgraded instances have `OverTimeHistoryFill` enabled after the first start-up of the release | `SELECT Enabled FROM OptionalFeatures WHERE Key = 'OverTimeHistoryFill'` on the dev instance (`:5169`) after upgrading it |
| `OUT-6083-default-holds` | Fewer than 10 % of instances that send usage data emit an `OptionalFeatureToggled` that switches `OverTimeHistoryFill` off within 30 days of the release | PostHog, existing `OptionalFeatureToggled` event |
| `OUT-6083-off-sticks` | 0 instances where an admin's post-upgrade off gets turned back on | Integration test across two seeder runs (AC-1.3), plus the dev instance after two restarts |

## Wave: DISCUSS / [REF] Definition of Done

1. A fresh instance is seeded with the fill on and the marker written.
2. An instance upgraded from an earlier release has the fill on after the first start-up, exactly once.
3. An admin's off after that start-up survives restarts.
4. `docs/settings/configuration.md` and `docs/metrics/predictability.md` say "on by default", with the backup advice moved to before upgrading.
5. `ARCHITECTURE.md` no longer describes the fill as opt-in, if it says so.
6. Lighthouse-Clients copy is updated in the three places listed, and a patch version is cut.
7. A release-notes line is drafted on #6083 (`Release Notes` tag): previous offs are now on, and here is how to switch it back off.
8. `dotnet build` (warnings as errors) and the filtered `dotnet test` are green, and so are `pnpm test` and `pnpm build`.
9. No new SonarCloud issues. Stryker ≥ 80 % on changed lines.

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|----------|--------|----------|
| 1 | User value clear | ✅ | US-01 elevator pitch |
| 2 | Job traceability | ✅ | `job-flow-coach-see-the-trend-my-data-already-supports`, `job-delivery-lead-see-process-stability-trend` (habit force) |
| 3 | ACs testable | ✅ | AC-1.1…1.5 are observable at the seeder and HTTP ports. AC-1.6 is a docs check |
| 4 | Dependencies known | ✅ | #6053 released, feedback gate confirmed |
| 5 | Scope bounded | ✅ | Out of Scope, plus D1/D4 |
| 6 | KPIs defined | ✅ | Three KPIs with numeric targets |
| 7 | Cross-cutting addressed | ✅ | RBAC, Clients, Website, Premium, E2E and usage data each answered |
| 8 | Sized ≤ 1 day | ✅ | Scope Assessment PASS |
| 9 | No blocking unknowns | ✅ | D1 was taken by the maintainer, and D3 fixes the mechanism in place of the skipped DESIGN |

## Wave: DISCUSS / [REF] Wave Decisions Summary

- **Key decisions:** D1 (one-time on for every existing instance, deliberate offs included), D2 (seeded on), D3 (seeder marker in `AppSettings`, no migration), D4 (stays a Preview).
- **Feature type:** a backend default change, whose user-visible effect is that a settings row starts on.
- **Constraints:** the filler's behaviour is untouched. No contract or schema change.
- **Upstream changes:** the story asked that deliberate offs stay off. D1 overrides that because the data can't distinguish them. #6053's docs warned that filled days can't be undone without a backup, so that advice moves to "before you upgrade".
- **Waves skipped by the maintainer:** DIVERGE, DESIGN, DEVOPS.

## Wave: DISCUSS / [REF] Open checks for DISTILL

- `AppSettingSeeder.RemoveObsoleteSettings` deletes rows by **Id** (9–42). Confirm that a marker row added
  at runtime can never be given one of those Ids on any provider, SQLite in particular. Otherwise the
  marker could be deleted, and the flip would run again on the next start-up.
- `OptionalFeatureSeederTests` currently asserts the fill is seeded off. That assertion inverts in the
  same commit as the production change.

---

## Wave: DISTILL / [REF] Reconciliation

`[lang-mode] csharp` (NUnit 4, partial-class `…Scenarios.cs` + `…Specifications.cs`). `[policy-mode] inherit`:
every port in scope already has a row in `docs/architecture/atdd-infrastructure-policy.md` (HTTP API via
`TestWebApplicationFactory<Program>` + `WithTestAuthentication`, real EF over SQLite, `Mock<ILicenseService>`), so
nothing was appended. `[port-mode]` n/a: the project asserts through its own harness observables, not a
`state_delta` port.

Reconciliation passed — 0 contradictions. DESIGN and DEVOPS were skipped by the maintainer, so there is nothing to
contradict DISCUSS. D3 stands in for DESIGN, and the driving ports are the three DISCUSS names. DEVOPS missing → the
project default infrastructure from the policy file.

## Wave: DISTILL / [REF] Scenario list with tags

File pair: `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/BehaviourSettings/Story6083HistoryFillOnByDefault{Scenarios,Specifications}.cs`,
class `Story6083HistoryFillOnByDefaultTest : BehaviourSettingsAcceptanceTest`. Eight methods, ten cases, every one
`[Ignore(PendingDeliver)]` (`"Story #6083 — pending DELIVER"`).

| # | Scenario | Tags | AC / KPI |
|---|----------|------|----------|
| 1 | `A_new_instance_has_the_history_fill_switched_on` | `@walking_skeleton @driving_port @real-io @contract-shape:pure-function` | AC-1.1 |
| 2 | `An_upgraded_instance_has_the_history_fill_switched_on_whichever_way_the_earlier_release_left_it(SwitchedOff / SwitchedOn / NotYetOffered)` | `@driving_port @real-io @kpi @contract-shape:bounded-change` | AC-1.2, `OUT-6083-on-after-upgrade` |
| 3 | `The_upgrade_switches_the_history_fill_on_and_changes_nothing_else_in_behaviour_settings` | `@driving_port @real-io @contract-shape:bounded-change` | AC-1.5 |
| 4 | `An_admin_who_switches_the_history_fill_off_after_the_upgrade_keeps_it_off_across_every_restart` (3 restarts) | `@driving_port @real-io @kpi @contract-shape:unbounded-preservation` | AC-1.3, `OUT-6083-off-sticks` |
| 5 | `A_new_instance_whose_admin_switches_the_history_fill_off_keeps_it_off_across_restarts` (2 restarts) | `@driving_port @real-io @contract-shape:unbounded-preservation` | AC-1.4 |
| 6 | `A_history_fill_setting_removed_by_hand_comes_back_on_at_the_next_restart` | `@driving_port @real-io @error @contract-shape:bounded-change` | edge, D7 |
| 10 | `Restoring_a_backup_from_before_this_release_brings_the_history_fill_back_off_and_a_restart_keeps_it_off` | `@driving_port @real-io @error @contract-shape:unbounded-preservation` | D8 |
| 11 | `Clearing_the_database_leaves_the_history_fill_switched_on_like_a_new_instance` | `@driving_port @real-io @contract-shape:bounded-change` | D8, D2 |

#10 and #11 drive the real `DatabaseManagementService.RestoreBackup` / `ClearDatabase` (resolved gate, tracker and
service provider from the test host) with only `IDatabaseManagementProvider` mocked, as `DatabaseManagementServiceTest`
does. The restore stand-in puts the backup's database in place (empty, migrated, holding the fill off and no record).
The clear stand-in deletes the database. Migration and every seeder afterwards are the product's own, and each `When`
checks that they ran, because a seeding failure there is logged, not reported. A real restore of a backup file was
not used, because the shipped fixtures need a password from the environment and the provider's database tooling.
The backup-file builder moved from `DatabaseManagementServiceTest` to `TestHelpers/EncryptedBackupStream.cs`, so
both use one copy.

Unit level, real seeder over real EF/SQLite (`IntegrationTestBase`), also `[Ignore]`d:

| # | Test | Covers |
|---|------|--------|
| 7 | `OptionalFeatureSeederTests.SeedAsync_AddsTheOverTimeHistoryFill_OnInPreviewAndFree_AndRecordsThatItSwitchedItOn` | D2, AC-1.4's record on a fresh install; replaces the `…_OffInPreviewAndFree` test |
| 8 | `OptionalFeatureSeederTests.SeedAsync_RunAgainInTheSameProcessAfterTheAdministratorSwitchedTheFillOff_LeavesItOff` | the seeder run twice in one process, same instance and same context: one record, no second flip |
| 9 | `AppSettingSeederTests.SeedAsync_KeepsTheRecordThatTheHistoryFillWasSwitchedOn` | the record survives `RemoveObsoleteSettings`. Written by the real `OptionalFeatureSeeder`, so the `Id` under test is whatever DELIVER gives it (Upstream findings 2) |

Chaining: #4's Given is #2's Given + When (an earlier release left it off, then the upgrade), checked rather than
assumed. #5 and #6 build on #1 (a new instance whose admin finds it on). Error and edge share is 5 of 11 (#3, #6,
#8, #9, #10). AC-1.5's "only a System Admin can switch it" is not re-authored, because 6053's
`Slice09TheFillShipsOptInTest.Someone_who_is_not_a_system_admin_cannot_switch_the_fill_and_their_charts_carry_on_as_before`
already holds it. AC-1.6 is a docs and release-notes check, so it is a DELIVER/finalize checklist item.

## Wave: DISTILL / [REF] WS strategy

Strategy B (extend existing), as DISCUSS decided. #1 is tagged as the story's demo: a fresh instance, read the way an
admin reads it. AC-1.1's second half ("opening Percentiles Over Time starts the fill without any other step") is
carried by the 6053 suite once DELIVER drops its set-up switch-on (see disposition). Every fill scenario then runs
on the shipped default.

## Wave: DISTILL / [REF] Test placement

`Lighthouse.Backend.Tests/API/Integration/BehaviourSettings/`, next to Story 5876's slices, because the harness it
needs already lives there: `BehaviourSettingsAcceptanceTest` (real host, real SQLite per test, `RunEverySeeder()` as
start-up/upgrade, `GetOptionalFeature`/`GetOptionalFeatures`/`ToggleOptionalFeature`, `ReadStoredAppSetting`). That
mirrors 5913 placing its scenarios beside the harness they needed. The 6053 harness was not used: its `[SetUp]`
switches the fill on through the endpoint, which hides the very default this story changes. Categories: `acceptance`,
`story-6083-over-time-history-fill-on-by-default`. No new base class and no new fixture.

## Wave: DISTILL / [REF] Driving-port coverage

| Driving port | How the scenario reaches it | Scenarios |
|--------------|-----------------------------|-----------|
| Application start-up (`OptionalFeatureSeeder.Seed()` and every other `ISeeder`) | fixture `[SetUp]` seeds an empty database (the new instance); `WhenTheInstanceIsUpgraded` / `WhenTheInstanceRestarts(n)` re-run every seeder against it | all; #7–#9 call the seeders directly |
| `GET /api/latest/optionalfeatures/{key}` and `GET /api/latest/optionalfeatures` | real HTTP as a System Admin (`TheCallerAdministersTheWholeInstance`), JSON read off the body | 1–6 |
| `POST /api/latest/optionalfeatures/{key}` (System Admin) | `ToggleOptionalFeature`, asserted 200 | 4, 5 |
| Restore and clear (`DatabaseManagementService.RestoreBackup` / `ClearDatabase`, which re-run migrations and every seeder) | real service, provider mocked | 10, 11 |
| Over-time chart read that triggers the fill | 6053 suite, unchanged, once its set-up switch-on goes | — |

The store is read directly in one place, the switch-on record (`ReadStoredAppSetting`). No port shows it, and none
should. Without it, "an admin's off holds" could only be proven by the restart, never pinned on the upgrade itself.

## Wave: DISTILL / [REF] Adapter coverage

No new driven adapter.

| Adapter | Treatment | Covered by |
|---------|-----------|------------|
| EF `LighthouseAppContext` (`OptionalFeatures`, `AppSettings`) | real, SQLite | every scenario |
| `ILicenseService` | `Mock` (existing harness), licensed | 3 (lists the premium rows) |

Postgres is not run separately. `AppSettings.Id` has the same shape on both providers (Upstream findings 2), and the
E2E runs on Postgres in CI (`ci_verifypostgres`).

## Wave: DISTILL / [REF] Scaffolds

One production line: `AppSettingKeys.HistoryFillSwitchedOnByDefault = "OptionalFeatures:HistoryFillSwitchedOnByDefault"`
(`Lighthouse.Backend/Models/AppSettings/AppSettingKeys.cs`). It is the final constant, not a stub. A `const` cannot
throw, so there is no scaffold marker, and the RED comes from the seeder not writing it yet. The name and value
deliberately avoid `OverTimeHistoryFillKey` and `"OverTimeHistoryFill"`, the two spellings
`OverTimeReconstructionSeamArchUnitTest` confines to the key list, the seeder and the switch. The tests address the
setting by the literal `"OverTimeHistoryFill"`, like 6053's, because that string is its wire identity.

RED classification: `docs/feature/story-6083-over-time-history-fill-on-by-default/red-classification.md` — 11/11
MISSING_FUNCTIONALITY.

## Wave: DISTILL / [REF] Existing-test disposition (same commit as the production change)

These stay green and untouched until DELIVER changes production. Each goes **in the same commit** as the seeder
change, so no commit is red. Paths are under `Lighthouse.Backend/Lighthouse.Backend.Tests/` unless stated.

| File | Member | Action |
|------|--------|--------|
| `Services/Implementation/Seeding/OptionalFeatureSeederTests.cs` | `SeedAsync_AddsTheOverTimeHistoryFill_OffInPreviewAndFree` | **Delete**: `Enabled` flips from `False` to `True`. Replaced by #7, un-ignored in the same commit |
| same | `SeedAsync_OverTimeHistoryFillSwitchedOnBeforeTheUpgrade_StaysOnAndIsRedescribed`, `SeedAsync_OverTimeHistoryFill_ReadsTheWayAnAdministratorSeesIt`, `SeedAsync_CanBeCalledMultipleTimes_WithoutErrors` | **Keep**: on stays on, the description has no "off by default" wording to change, and the key list counts optional features only |
| `API/Integration/PercentilesOverTime/Slice09TheFillShipsOptInScenarios.cs` | `An_instance_that_stores_no_fill_switch_fills_nothing_until_an_admin_switches_it_on_after_the_upgrade` | **Edit (D7).** Its upgrade runs with the record present and the row missing, which is exactly #6. `ThenTheFillIsStoredSwitched(on: false)` becomes `on: true`, the following `WhenTheAdminSwitchesTheFillOn()` goes, and the name and summary drop "until an admin switches it on". Its first half (no row stored ⇒ fills nothing) is unchanged |
| same | class summary ("The switch ships off on a fresh instance and an upgraded one alike") | **Reword**. The class name stays; renaming it is churn |
| `API/Integration/PercentilesOverTime/ReconstructOverTimeHistoryAcceptanceTest.cs` | `SwitchTheFillOnTheWayAnAdministratorWould()` and its call in `[SetUp]` | **Delete** (recommended). Every fill scenario then runs on the shipped default, which is AC-1.1's second half at the chart port. Slice09 still exercises the endpoint explicitly in five scenarios. Keeping it instead means rewording its "the fill ships switched off" summary and leaving AC-1.1's second half unasserted |
| `Story6083HistoryFillOnByDefaultScenarios.cs` (#1–#6, #10, #11) and the unit tests #7–#9 | all | **Un-ignore one at a time** |
| `Services/Implementation/DatabaseManagementServiceTest.cs` | `CreateValidBackupStream` | **Done in DISTILL**: moved to `TestHelpers/EncryptedBackupStream.Create`, with the six callers repointed. Its tests stay green (inside the 107-pass run below) |
| `Lighthouse.EndToEndTests/tests/helpers/api/optionalFeatures.ts` | `switchHistoryFill` doc comment ("ships switched off … so the next spec meets the shipped default") | **Reword**; see E2E impact |

No other backend test asserts the default. Checked by grepping `OverTimeHistoryFill`, `TheStoredFillSwitch`,
`ThenTheFillIsStoredSwitched` and `"Fill in past days"` across the test project. `OverTimeHistoryFillSwitchTest`,
`OverTimeGapReconcilerTest` and the ArchUnit seam test do not depend on it.

## Wave: DISTILL / [REF] E2E impact

No E2E added; the backend scenarios carry the story. No spec asserts the fill is off. Two places silently assumed it:

1. **`Screenshots.spec.ts` › "Take @screenshot of the team metrics dashboard widgets"** loops over every
   Predictability widget without switching the fill, and writes `features/metrics/<widgetId>.png`. The ids of the two
   over-time widgets are `percentilesOverTime` and `pbcOverTime` (`MetricsPage.ts:848-849`), the same files the
   dedicated fill-on shot (`testWithDemoHistoryFill`) writes. That collision is pre-existing: whichever test runs last
   wins. With the default on, the loop's version is a chart that may be **mid-fill** when captured, so the image is
   no longer deterministic. Recommended DELIVER fix: add both widgets to the loop's `coveredElsewhere` list.
2. **The cleanup hooks** in `PbcOverTime`, `PercentilesOverTime`, `PredictabilityOverTime` and `Screenshots` switch
   the fill back **off** "so the next spec meets the shipped default". After this story that leaves the instance
   in the non-default position. It is harmless, since later specs met off before too, but the comment is now false. Recommended:
   keep the hooks restoring **off** (later specs keep the deterministic state they have today) and reword the comments
   to say so. Restoring on would let `ForecastFilter.spec.ts` and the dashboard loop start background fills on demo
   data. On a fresh E2E database the first spec to open Predictability already meets the fill on, so DELIVER should
   run the full E2E suite once (SQLite and Postgres), with a backup restore in it (`DatabaseManagement.spec.ts`), to
   confirm nothing else leans on off.

## Wave: DISTILL / [REF] Upstream findings

1. **D7 — a setting removed by hand comes back on.** *Taken autonomously while the maintainer was AFK, to
   revisit.* With the record present and the `OverTimeHistoryFill` row missing, the seeder re-adds the row with the
   shipped default, **on**, as for a new instance. The add branch seeds the default, and D3 only governs an existing
   row's `Enabled`. The rejected alternative was off, on the grounds that "the admin already had their chance". The
   row goes missing only by hand-editing the database, or in 6053's "no switch stored" scenario. Pinned by #6, and
   the Slice09 disposition row follows it.
2. **`AppSettingSeeder.RemoveObsoleteSettings` vs the record — no risk unless DELIVER sets an `Id`.**
   `AppSetting.Id` is not generated on either provider. The key is `Key`
   (`modelBuilder.Entity<AppSetting>().HasKey(a => a.Key)`), and `Id` is a plain `INTEGER NOT NULL` (SQLite) or
   `integer` (Postgres) column with no `ValueGeneratedOnAdd`, no identity and no autoincrement (both
   `LighthouseAppContextModelSnapshot.cs`, and the `AddAppSettingToDb` and `InitialCreate` migrations). EF writes
   whatever the entity holds, so `new AppSetting { Key, Value }` stores `0`, as `AppSettingService.EnsureInstallTimestamp`
   and `UpsertSetting` already do. The record can only land on 9–42 if code sets `Id` explicitly, for example by
   copying `AppSettingSeeder`'s numbered list. **Mitigation: write the record with `Id = 0`** (or leave it unset), and
   never add it to `AppSettingSeeder`'s defaults (D3 already says so). #9 pins this by behaviour, and so does #4: a
   deleted record would flip the fill back on at the second restart.
3. **D8 — a restore never flips.** *Taken autonomously while the maintainer was AFK, to revisit.*
   `DatabaseManagementService` runs migrations and every seeder after a **restore** and after **clear database**
   (`MigrateAndSeedDatabase`, lines 115 and 173), not only at start-up. Without D8, restoring a backup from before this
   release (no record) would switch the fill on, and the next chart open would fill again. That would undo "restore
   the backup you took before upgrading", the recovery path AC-1.6 keeps. Decision: **a restore restores exactly what
   the backup held.** The fill comes back the way the backup stored it. **Constraint on DELIVER: after a restore, the
   seeder writes the record without flipping `Enabled`**, so a later restart does not flip it either. **Clear
   database is a new instance**: seeded on, record written. How the seeder knows it is running after a restore is
   left to DELIVER, for example a parameter or context the restore path passes, as opposed to start-up. Whatever the
   mechanism, start-up must still flip. Pinned by #10 (restore of a backup holding off: off after the restore, off
   after a restart, record present) and #11 (clear: on, record present). AC-1.6's docs should say a restored backup
   keeps the fill as the backup held it.
4. **The flip must stay in `OptionalFeatureSeeder.cs`.** `OverTimeReconstructionSeamArchUnitTest` allows the fill's
   key to be named only in the key list, the seeder and the switch. Putting the flip in a new seeder class, as the
   `FeatureOrdering` precedent might suggest, reds that test unless its allow-list grows too.
5. **Low, pre-existing: two replicas upgrading at once.** Both could see no record and both insert it. The second
   `SaveChanges` would then hit the `AppSettings` key and fail its start-up seed. Adding a missing optional feature
   or a default app setting has the same race today, so this story adds no new class of risk. Not scenario'd: it
   needs the multi-host Testcontainers harness, and DEVOPS was skipped.

Mandate compliance, briefly: tests enter through driving ports only (HTTP read and toggle, seeder re-runs). Scenario
and step names are domain language. The SUT is the production composition root (`TestWebApplicationFactory<Program>`)
with only the licence faked. These are layer 3+ tests, so example-only, no PBT: there is no unbounded input domain,
and the one finite axis, the earlier state, is a `[TestCase]` over an enum. The 15-item completeness checklist was not
scored; the open gap is finding 5.

## Wave: DISTILL / [REF] Pre-requisites

- D2 + D3 together. Seeding on alone fails #2's record assertion and #4/#5. The record alone fails #1.
- D8 needs the restore path to tell the seeder it is not a start-up. A flip that runs on every seed reds #10.
- Run the new scenarios with the filter `Story6083HistoryFillOnByDefaultTest|OptionalFeatureSeederTests|AppSettingSeederTests|BehaviourSettings|OverTimeReconstructionSeamArchUnitTest|Slice09TheFillShipsOptInTest|DatabaseManagementServiceTest`
  (with the CLAUDE.md connector exclusions): 107 passed, 13 skipped, 0 failed on 2026-10-01. Removing the 6053
  set-up switch-on means the whole `PercentilesOverTime` directory must run in DELIVER.
- D7 and D8 were taken autonomously; the maintainer should confirm them before finalize.
