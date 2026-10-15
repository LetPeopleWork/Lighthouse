# Feature Delta — story-6084-over-time-history-fill-always-on

> ADO User Story #6084 "Remove the over-time history fill switch - always on". Story #6053 shipped
> *Fill in past days on over-time charts* (`OverTimeHistoryFill`) as an opt-in Preview. Story #6083 switched
> it on by default, once, for every instance (released in v26.10.3.6). This story removes the switch: the
> fill always runs, and it stops being a Preview.

Lean DISCUSS pass (2026-10-15). **The maintainer explicitly skipped DIVERGE, DESIGN and DEVOPS.** The
mechanism decisions DESIGN would have taken (D2–D4) are taken and locked here, so DISTILL has nothing
left to improvise.

---

## Wave: DISCUSS / [REF] Persona IDs

- **flow-coach**: reads Percentiles Over Time and wants the trend that the stored data already supports.
- **delivery-lead** (secondary): reads PBC Over Time to see whether the process is stable.
- **system-admin** (secondary): today sees a Preview row under Settings → System and has to decide whether to
  keep it. After this story there is nothing to decide.

## Wave: DISCUSS / [REF] JTBD One-Liners

This refines `job-flow-coach-see-the-trend-my-data-already-supports` and
`job-delivery-lead-see-process-stability-trend` (`docs/product/jobs.yaml`). #6053 removed the push force,
and #6083 removed the habit force of having to find the switch. What is left is an **anxiety** force: a
setting marked Preview tells the admin that the fill might be withdrawn, or might be wrong, so it looks
like something to keep an eye on. A fill that is simply part of how the over-time charts work answers that.
The jobs themselves do not change, so `jobs.yaml` is not edited.

## Wave: DISCUSS / [REF] Pre-requisites

- #6083 is released in v26.10.3.6 (flip commit `e551cab5f`), and is also in v26.10.10.1.
- The story's own trigger ("after the default has been switched on and stayed on without issue") was
  confirmed by the maintainer on 2026-10-15. It had been on by default for about 12 days, with no issue
  reported.
- The frontend never reads the switch. The over-time widgets' empty-state sentence
  (`overTimeEmptyState.ts`) is the same whether the fill is on or off. Only its doc comment mentions the
  switch. **The ADO description's "flag-dependent empty-state wording in both over-time widgets" does not
  exist**, so no UI copy changes.
- `DatabaseMaintenanceGate` does not read the switch. It asks `IOverTimeHistoryFillActivity` whether a pass
  is in flight, and that stays as it is.
- The switch is read in exactly two places: `OverTimeGapReconciler` (when a fill is asked for) and
  `OverTimeHistoryFiller` (when a waiting pass is about to start).

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Verdict |
|----|----------|---------|
| D1 | **Every instance fills, with no way to switch it off.** That includes instances whose System Admin switched it off after the #6083 upgrade. The release notes say plainly that the setting is gone, that the fill now always runs, and that anyone who wants the unfilled history kept should take a backup before upgrading. | Locked by the maintainer, 2026-10-15 |
| D2 | **The row leaves the database in this release.** `OptionalFeatureKeys.OverTimeHistoryFillKey` moves to `OptionalFeatureSeeder.RemoveDeprecatedFeatures`, as `DeltaSyncKey` and `McpServerKey` did, and leaves `GetOptionalFeatures()`. During a mixed-version rolling deploy, an old replica finds no row and reads the fill as off, which only delays filling until every replica runs the new version. A downgrade re-seeds the row on (the #6083 seeder: missing row → seeded on, no marker → writes it). Seeder data, not schema, so no EF migration and no `CreateMigration` run. *(amended at DISTILL review, 2026-10-15)* Reading or switching the setting by name (`GET`/`POST /api/latest/optionalfeatures/OverTimeHistoryFill`) answers 404 and stores nothing. *(amended at DISTILL review, 2026-10-15)* **Restart during a rolling deploy:** an old replica that restarts re-adds the row (on) with the marker. New-version code never reads it, so the fill is unaffected, but the row can show in Settings → System until the next new-version start-up deletes it, and toggling it there has no effect on new replicas. Accepted risk, no test: it clears itself at the next start-up of the new version. | Locked by the maintainer, 2026-10-15 |
| D3 | **#6083's leftovers go entirely.** `OptionalFeatureSeeder.SettleTheHistoryFillAfterARestore` and its call in `DatabaseManagementService.RestoreBackup` are removed, so a restored backup fills like any other instance, whatever the backup held. The marker-writing code and `AppSettingKeys.HistoryFillSwitchedOnByDefault` are removed too. | Locked by the maintainer, 2026-10-15 |
| D4 | **The marker row is deleted by key.** `AppSettingSeeder.RemoveObsoleteSettings` deletes by **Id**, and the marker's Id was assigned at runtime, so the Id list can't reach it. `AppSettingSeeder` gains a key-based obsolete list holding the literal `"OptionalFeatures:HistoryFillSwitchedOnByDefault"`. The literal stays because the constant is removed. The existing Id list is left as it is. | Locked by the maintainer, 2026-10-15 |
| D5 | **The switch abstraction is deleted, not stubbed.** `IOverTimeHistoryFillSwitch`, `OverTimeHistoryFillSwitch` and their DI registration in `Program.cs` go. The reconciler asks the filler whenever days are missing, and the filler no longer drops a waiting pass with the "switched off" log line. Nothing else about the fill changes: bounds, the per-visit budget, the memo and the maintenance gate stay as they are. | Locked |
| D6 | **No longer a Preview anywhere.** Docs headings lose "(Preview)", and the docs describe the fill as part of how both charts work rather than as a setting. Old anchors that release notes link to are repointed (see Cross-cutting Impact, Docs). | Locked |
| D7 | **Terminology.** Docs and release-note copy use the configurable terms (*Team*, *Portfolio*, *Work Item*) and name the charts as they appear in the UI. | Locked |

## Wave: DISCUSS / [REF] User Stories

### US-01: The over-time charts always fill in the days the stored history supports

As a **flow coach**, I want Percentiles Over Time and PBC Over Time always to fill in the days Lighthouse
missed, so that the trend I read is never cut short by a setting somebody switched off, or one nobody
trusts because it says Preview.

`job_id: job-flow-coach-see-the-trend-my-data-already-supports`
(also `job-delivery-lead-see-process-stability-trend`)

#### Elevator Pitch
Before: Settings → System lists *Fill in past days on over-time charts (Preview)*. If a System Admin has switched it off, Percentiles Over Time for a Team added last week shows only the handful of days Lighthouse recorded, and nothing on the chart says why.
After: upgrade, then open Percentiles Over Time for that Team. The missing days are filled in the background after that first open, and the next open shows a point for each day the stored history can fill *(amended at DISTILL review, 2026-10-15)*. **Settings → System** no longer lists the row, and `GET /api/latest/optionalfeatures` no longer returns `OverTimeHistoryFill`.
Decision enabled: the coach reads the trend without first asking whether the fill is on on this instance, and the admin has nothing to weigh up.

#### Acceptance Criteria
- **AC-1.1** On a fresh database, after start-up, `GET /api/latest/optionalfeatures` holds no `OverTimeHistoryFill`, and opening Percentiles Over Time or PBC Over Time for a range with missing days starts the fill.
- **AC-1.2** On an instance upgraded from a release that had the row, **whether it was on or off**, the first start-up of this release removes the row, and opening either chart starts the fill (D1, D2).
- **AC-1.3** That start-up also removes the `OptionalFeatures:HistoryFillSwitchedOnByDefault` app setting if it is present, and leaves every other app setting and every other optional feature exactly as they were (D4).
- **AC-1.4** Restoring a backup taken before #6053 (no row, no marker), one that held the fill switched off, or *(amended at DISTILL review, 2026-10-15)* one that held the fill switched **on** with the marker (the common case after #6083) ends with no `OverTimeHistoryFill` row and no marker, and the next chart open fills (D3).
- **AC-1.5** A waiting fill pass is never dropped because of a setting. The only things that stop or defer it are the ones that did before: the maintenance gate, the per-visit budget and the stored-history bounds (D5).
- **AC-1.6** `docs/settings/configuration.md` no longer describes the switch. `docs/metrics/predictability.md` describes the fill as always on and no longer a Preview, and keeps the caveats about today's configuration, filled days staying, and taking a backup before upgrading. The release notes tell admins that the setting is gone and the fill always runs. Links into the removed or renamed anchors resolve.

#### Examples (Given/When/Then) *(amended at DISTILL review, 2026-10-15)*

- **An admin had switched the fill off** (DISTILL #8, and #2 `SwitchedOffByTheAdmin`). Given the System Admin switched *Fill in past days on over-time charts* off after the last upgrade, and Percentiles Over Time for Team Voyager shows only the 5 days Lighthouse recorded in the last 30. When the instance is upgraded to this release and a flow coach opens Percentiles Over Time for Team Voyager for the last 30 days, then the missing 25 days are filled in the background, and the next open shows a point for each of the 30 days. The same holds for PBC Over Time on the Portfolio Project Apollo.
- **An untouched instance** (DISTILL #2 `SwitchedOnByDefault`). Given an instance where nobody touched the setting, so it is on by default and the record of switching it on is stored. When the instance is upgraded, then Settings → System no longer lists the setting, the record is gone, and every other setting, such as *Feature Ordering*, reads exactly as before.
- **A backup from before the fill existed** (DISTILL #6 `NotYetOffered`, with #9 for the fill on the next open). Given a backup taken before the fill was offered, with neither the setting nor the record. When the System Admin restores it, then Settings → System lists no fill setting, nothing about the fill is stored, and that stays so after the next restart, so Team Voyager's charts fill on the next open like any other instance's.
- **A chart waiting behind a running fill** (DISTILL #9). Given Percentiles Over Time for Team Voyager is filling in its last 30 days, and the same chart for Team Zenith is waiting its turn. When a switched-off fill setting turns up meanwhile, left behind by an older copy of Lighthouse, then Team Zenith's 30 days are still filled once Team Voyager's are done.

## Wave: DISCUSS / [REF] Out of Scope

- Any change to how the fill works: bounds, the 90-day per-visit budget, the memo, the maintenance gate or the filler's queueing.
- A way to opt out, whether by configuration, environment variable or API. D1 rejected it.
- Telling filled days apart from recorded ones, or undoing a fill.
- The Preview mechanism itself (`IsPreview` on other rows, and the Settings page's Preview handling).
- Frontend UI changes. There is no switch-dependent copy to change. The `overTimeEmptyState.ts` doc comment loses its "switched off" paragraph, which is a comment-only edit. *(amended at DISTILL review, 2026-10-15)* So does `Lighthouse.Frontend/src/pages/Common/MetricsView/overTimeEmptyState.test.ts`, whose comment says the fill reaches "every instance until an administrator opts in".

## Wave: DISCUSS / [REF] Cross-cutting Impact

- **RBAC**: **N/A, because** a key is being removed and no permission is added. With nothing left to switch, the `RbacGuard(SystemAdmin)` on writing this row goes away along with the row. Nothing new reads `/authorization/my-summary`.
- **Lighthouse-Clients (CLI + MCP)**: **affected, copy only, patch release.** These still say the fill is a Preview a System Admin switches on, which was already stale after #6083: `packages/client/src/index.ts:1091`, `packages/cli/src/index.ts:1592` (comment), `packages/mcp-core/src/index.ts:1081,1096,1111,1126` (tool descriptions), and `skills/lighthouse/references/lighthouse-mechanics.md:239`. Reword them as "Lighthouse also fills in missing past days in the background after a read, from the history it already stores". No contract change: no client calls `/optionalfeatures/OverTimeHistoryFill`.
- **Website**: **N/A, because** `/storage/repos/website` does not mention the fill or `OverTimeHistoryFill`.
- **Premium**: **N/A, because** the fill was free and stays free.
- **Docs**: `configuration.md` §"Fill in past days on over-time charts (Preview)" is removed. `predictability.md` §"Filling in past days (Preview)" becomes "Filling in past days", and its three `#filling-in-past-days-preview` links are repointed. `releasenotes.md:152` links to the removed configuration anchor, so that link is repointed to `predictability.html#filling-in-past-days`. Only the link target changes, and the past release-note text stays as written. `ARCHITECTURE.md:189,387` and ADR-207 are updated to say the fill always runs, with no behaviour setting. For ADR-207, the status note is amended rather than the decision rewritten.
- **E2E**: `tests/helpers/api/optionalFeatures.ts` (`switchHistoryFill`) and the `PbcOverTime`, `PercentilesOverTime`, `PredictabilityOverTime` and `Screenshots` specs that call it lose those calls. Removing them is a DISTILL item, and Playwright runs locally before the commit. *(amended at DISTILL review, 2026-10-15)* Those calls were also the isolation: each spec switched the fill **off** again with `switchHistoryFill(false)`, so later specs met no background fill. That isolation is lost, so every spec that opens Predictability now starts a fill in the background on demo data. Hence the full suite in DoD 8.
- **Screenshots** *(amended at DISTILL review, 2026-10-15)*: `docs/assets/settings/configuration.png` shows the row today and is regenerated at finalize. `rm` the old PNG first, because the regenerator keeps the old file when the pixel difference is under 0.5 %.
- **Usage data (DEVOPS skipped)**: **N/A, because** removing a setting gives nothing new to count. `OptionalFeatureToggled` simply stops firing for this key. The fill's own use is not an event, and #6053 decided that.
- **Demo data**: **N/A, because** the demo never touched the switch (#6083 D5).

## Wave: DISCUSS / [REF] WS Strategy

Strategy **B (extend existing)**: one thin slice and no skeleton. The fill exists end to end, and this
slice removes its gate, the setting and #6083's one-time machinery.

## Wave: DISCUSS / [REF] Driving Ports

- Application start-up: `OptionalFeatureSeeder.Seed()` and `AppSettingSeeder.Seed()`.
- `GET /api/latest/optionalfeatures`: the Settings → System list, which no longer holds the row.
- Restore and clear: `DatabaseManagementService.RestoreBackup` / `ClearDatabase`, which re-run migrations and every seeder.
- Reading Percentiles Over Time or PBC Over Time: the fill trigger that #6053 already covers.

## Wave: DISCUSS / [REF] Scope Assessment: PASS

One story and one slice, and mostly deletion. It touches the fill's two gate points, two seeders, one restore
hook, docs and client copy. Under a day. No oversized signals.

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|-----|--------|-------------|
| `OUT-6084-fills-everywhere` | 100 % of instances fill missing days on a chart open after upgrading, including ones that had it off | Acceptance scenario that upgrades from a switched-off row; dev instance (`:5169`) after the upgrade: `OverTimeHistoryFill` row and marker both gone, and a chart open adds days |
| `OUT-6084-no-residue` | 0 rows left for `OverTimeHistoryFill` or `OptionalFeatures:HistoryFillSwitchedOnByDefault` after the first start-up | `SELECT … FROM OptionalFeatures / AppSettings` on the dev instance, plus the AC-1.3 scenario |
| `OUT-6084-no-new-reports` | No issue reported against the fill within 30 days of the release that can be traced to its being always on | GitHub issues, Slack and support. The maintainer judges this. |

## Wave: DISCUSS / [REF] Definition of Done

1. No production code references `OverTimeHistoryFillKey` except the deprecated-keys list. `IOverTimeHistoryFillSwitch` and `HistoryFillSwitchedOnByDefault` are gone.
2. A fresh, an upgraded-on, an upgraded-off and a restored instance all end with no row and no marker, and all fill on a chart open.
3. `OverTimeReconstructionSeamArchUnitTest`'s confinement of the key is updated so the key may be named only in `OptionalFeatureKeys` and `OptionalFeatureSeeder` (its deprecated-keys list) *(amended at DISTILL review, 2026-10-15)*.
4. #6083's acceptance suite and #6053's opt-in suite (`Slice09TheFillShipsOptIn…`) are retired or rewritten to the always-on behaviour, and the switch's unit tests are deleted with the switch.
5. Docs per AC-1.6, plus `ARCHITECTURE.md` and the ADR-207 status note, and `docs/assets/settings/configuration.png` regenerated without the row (old PNG removed first).
6. The Lighthouse-Clients copy is updated in the places listed, and a patch version is cut.
7. A release-notes line is drafted on #6084 (`Release Notes` tag): the setting is gone, the fill always runs, and take a backup before upgrading if you want the unfilled history kept.
8. `dotnet build` (warnings as errors) and the filtered `dotnet test` are green, plus the full Integration suite because `Program.cs` changes. `pnpm test` and `pnpm build` are green, and *(amended at DISTILL review, 2026-10-15)* so is the **full** Playwright suite on SQLite and on Postgres, `DatabaseManagement.spec.ts` included, not just the four touched specs.
9. No new SonarCloud issues. Stryker ≥ 80 % on changed lines (the seeders and the reconciler).

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|----------|--------|----------|
| 1 | User value clear | ✅ | US-01 elevator pitch |
| 2 | Job traceability | ✅ | `job-flow-coach-see-the-trend-my-data-already-supports`, `job-delivery-lead-see-process-stability-trend` (anxiety force) |
| 3 | ACs testable | ✅ | AC-1.1…1.5 are observable at the seeder, HTTP, restore and chart-read ports. AC-1.6 is a docs check |
| 4 | Dependencies known | ✅ | #6083 released, and the trigger was confirmed by the maintainer |
| 5 | Scope bounded | ✅ | Out of Scope, plus D1/D5 |
| 6 | KPIs defined | ✅ | Three KPIs with targets |
| 7 | Cross-cutting addressed | ✅ | RBAC, Clients, Website, Premium, Docs, E2E, usage data and demo data each answered |
| 8 | Sized ≤ 1 day | ✅ | Scope Assessment PASS |
| 9 | No blocking unknowns | ✅ | D1–D4 were taken by the maintainer, and D5 fixes the mechanism in place of the skipped DESIGN |

## Wave: DISCUSS / [REF] Wave Decisions Summary

- **Key decisions:** D1 (always on, deliberate offs included, no opt-out), D2 (row deprecated in this release), D3 (restore rule and marker code removed), D4 (marker row deleted by key), D5 (switch abstraction deleted).
- **Feature type:** a backend change. Its user-visible effects are that a settings row disappears, and that instances which had the fill off now fill.
- **Constraints:** the filler's behaviour is otherwise untouched. No contract or schema change.
- **Upstream changes:** the ADO description's "flag-dependent empty-state wording" does not exist, because the frontend never read the switch (#6053 slice-05). #6083's D8 restore rule ("a backup from before the setting brings the fill back off") is withdrawn by D3.
- **Waves skipped by the maintainer:** DIVERGE, DESIGN, DEVOPS.

## Wave: DISCUSS / [REF] Open checks for DISTILL

- **The two gates in the 6053 harness.** *(amended at DISTILL review, 2026-10-15)* `ReconstructOverTimeHistoryAcceptanceTest`'s `[SetUp]` no longer switches the fill on: #6083's DELIVER already removed that call, so nothing there will 404 (DISTILL finding). Check that no scenario in the 6053 suite depends on the fill being off for its "Given".
- **The filler test's "a pass that got past this point finishes even if the switch goes off" contract.** It loses its subject. Confirm that no maintenance-gate scenario relied on a dropped pass never counting as in flight.
- **Downgrade path.** A downgrade to v26.10.3.6 / v26.10.10.1 re-seeds the row on and writes the marker. Confirm this by reading #6083's seeder; it needs no test.
- **`RemoveObsoleteSettings` by key and by Id together.** The new key list must not delete a row the Id list would keep, or the other way round. Pin with a unit test over the real seeder on SQLite (`IntegrationTestBase`).

---

## Wave: DISTILL / [REF] Reconciliation

`[lang-mode] csharp` (NUnit 4, partial-class `…Scenarios.cs` + `…Specifications.cs`). `[policy-mode] inherit`:
every port in scope already has a row in `docs/architecture/atdd-infrastructure-policy.md` (HTTP API via
`TestWebApplicationFactory<Program>` + `WithTestAuthentication`, real EF over SQLite, `Mock<ILicenseService>`,
`FakeLighthouseClock`), so nothing was appended. `[port-mode]` n/a: the project asserts through its own harness
observables, not a `state_delta` port.

Reconciliation passed — 0 contradictions. DIVERGE, DESIGN and DEVOPS were skipped by the maintainer, so nothing
can contradict DISCUSS. D2–D5 stand in for DESIGN and the driving ports are the four DISCUSS names. DEVOPS
missing → the project default infrastructure from the policy file. One DISCUSS statement is out of date (the 6053
harness no longer switches the fill on in its set-up); see Upstream findings 1. It changes no decision.

## Wave: DISTILL / [REF] Maintainer decision: the only visible change (2026-10-15)

> **Reviewed with the maintainer at the start of DISTILL, as a sketch of Settings → System before and after.**
> The only thing a user sees change is that the row *Fill in past days on over-time charts (Preview)*
> disappears from Settings → System. No notice replaces it, no new copy is added anywhere in the UI, and the
> over-time widgets' empty-state sentence stays as it is. Nothing else in the UI is sketched for this story,
> so DELIVER has no UI step to re-sketch. The one consequence outside the backend is that the
> `settings/configuration.png` screenshot, which shows the row today, is regenerated at finalize (see E2E impact).

## Wave: DISTILL / [REF] Scenario list with tags

Two partial-class pairs, both categories `acceptance` and `story-6084-over-time-history-fill-always-on`, every
test `[Ignore(PendingDeliver)]` (`"Story #6084 — pending DELIVER"`). No new base class.

**A. `API/Integration/BehaviourSettings/Story6084HistoryFillAlwaysOn{Scenarios,Specifications}.cs`**, class
`Story6084HistoryFillAlwaysOnTest : BehaviourSettingsAcceptanceTest`. Seven methods, twelve cases.

| # | Scenario | Tags | AC / KPI |
|---|----------|------|----------|
| 1 | `A_new_instance_offers_no_history_fill_setting` | `@walking_skeleton @driving_port @real-io @contract-shape:pure-function` | AC-1.1 (first half) |
| 2 | `An_upgraded_instance_no_longer_offers_the_history_fill_and_keeps_everything_else_as_it_was(SwitchedOnByDefault / SwitchedOffByTheAdmin / SwitchedOffBeforeItWasSwitchedOnByDefault)` | `@driving_port @real-io @kpi @contract-shape:bounded-change` | AC-1.2 (row half), AC-1.3, `OUT-6084-no-residue` |
| 3 | `An_instance_whose_fill_setting_was_already_removed_keeps_nothing_of_it_after_the_upgrade` | `@driving_port @real-io @error @kpi @contract-shape:bounded-change` | AC-1.3, D4, `OUT-6084-no-residue` |
| 4 | `Restarting_again_and_again_never_brings_the_history_fill_setting_back` (3 restarts) | `@driving_port @real-io @kpi @contract-shape:unbounded-preservation` | D2 (idempotent across restarts; Behaviour Settings and every stored app setting read as straight after the upgrade), `OUT-6084-no-residue` |
| 5 | `A_script_that_still_switches_the_history_fill_is_told_there_is_no_such_setting` | `@driving_port @real-io @error @contract-shape:bounded-change` | D2 (read and write by name answer 404, nothing stored) |
| 6 | `Restoring_a_backup_leaves_no_history_fill_setting_whatever_the_backup_held(SwitchedOnByDefault / SwitchedOffByTheAdmin / SwitchedOffBeforeItWasSwitchedOnByDefault / NotYetOffered)`, plus one restart | `@driving_port @real-io @error @kpi @contract-shape:bounded-change` | AC-1.4 (restore half; every backup state, the on-with-record backup included), D3 |
| 7 | `Clearing_the_database_leaves_no_history_fill_setting_like_a_new_instance` | `@driving_port @real-io @contract-shape:bounded-change` | D3, AC-1.1 |

**B. `API/Integration/PercentilesOverTime/Story6084TheFillRunsEverywhere{Scenarios,Specifications}.cs`**, class
`Story6084TheFillRunsEverywhereTest : ReconstructOverTimeHistoryAcceptanceTest`. Two methods.

| # | Scenario | Tags | AC / KPI |
|---|----------|------|----------|
| 8 | `After_the_upgrade_an_instance_whose_admin_had_switched_the_fill_off_fills_in_both_over_time_charts` | `@driving_port @real-io @kpi @contract-shape:bounded-change` | AC-1.2 (fill half), AC-1.1 (second half), `OUT-6084-fills-everywhere` |
| 9 | `A_chart_waiting_its_turn_still_fills_in_whatever_an_old_fill_setting_says` | `@driving_port @real-io @error @concurrency @contract-shape:bounded-change` | AC-1.5, D5, AC-1.4 (fill half, see below) |

AC-1.4's "the next chart open fills" half is covered by #9 (no stored setting is read, so nothing a backup held can stop a fill) together with #6 (the restore leaves no row and no record behind).

**C. Unit level, real seeder over real EF/SQLite (`IntegrationTestBase`), also `[Ignore]`d:**

| # | Test | Covers |
|---|------|--------|
| 10 | `AppSettingSeederTests.SeedAsync_RemovesTheRecordThatTheHistoryFillWasSwitchedOn_AndKeepsEverythingTheClearOutByNumberKeeps` | D4, Open check 4: the record (number 0) goes; `Install:Timestamp` (also number 0), the seven defaults (0–6) and rows on the skipped numbers 18/26/37 stay; the row on 9 still goes; two runs in a row |
| 11 | `OptionalFeatureSeederTests.SeedAsync_OnAnEmptyDatabase_NeverAddsTheHistoryFillNorARecordAboutIt` | D2, D3 on a new instance |

How #8 observes the fill: exactly as the 6053 suite does. The chart is opened through the real series endpoints
(`ReadTeamPercentileTrend` = Percentiles Over Time, `ReadPortfolioLimitTrend` = PBC Over Time), the harness drains
the real filler (`TheReconstructionPassRunsToCompletion`), and the days each chart is drawn from are read back:
Percentiles Over Time from the cycle-time percentile days (`PercentileDaysHeldFor`), PBC Over Time from the
Portfolio's throughput limit days (`LimitDaysHeldFor(portfolioId, OwnerType.Portfolio, ProcessBehaviorMetricType.Throughput)`),
so neither chart stands in for the other. #9 reuses the harness's `AReconstructionPassHeldInFlight` latch to keep a second chart
waiting while a switched-off row is written into the store. That is the only arrangement under which today's
filler drops a waiting pass, so it pins "a waiting pass is never dropped because of a setting" rather than
merely "a chart fills".

**Why #8 and #9 are not in the behaviour-settings pair.** Watching a chart fill needs the pinned clock, the
seeded owner histories, the latchable writer and the drainable filler that only `ReconstructOverTimeHistoryAcceptanceTest`
has. Copying them into `BehaviourSettingsAcceptanceTest` would duplicate a 1,200-line harness; a C# partial
class cannot have two bases. So the chart half lives on the 6053 harness, with a Given that writes the
last release's state (row off plus record) straight into the store, because the endpoint that once wrote it answers
404 after D2.

Chaining: #4's Given is #2's Given + When (switched off by the admin, then the upgrade), checked rather than
assumed. Error and edge share: 6 of 11 methods (#3, #4, #5, #6, #9, #10) — **55 %**; 11 of 16 cases
(**69 %**) counting #2's two switched-off cases. Layer 3+ throughout, so example-only and no PBT: the
input space is the finite set of states an earlier release can leave, which is a `[TestCase]` over an enum.
AC-1.6 is a docs and release-notes check, so it stays a DELIVER/finalize checklist item, not a test.

## Wave: DISTILL / [REF] WS strategy

Strategy B (extend existing), as DISCUSS decided. #1 is the story's demo: a fresh instance, read the way a System
Admin reads Settings → System. The fill half of AC-1.1 needs no new skeleton: once DELIVER retires the 6053 opt-in
scenarios, every remaining 6053 scenario already opens charts on a fresh instance and fills with no switch touched.

## Wave: DISTILL / [REF] Test placement

`Lighthouse.Backend.Tests/API/Integration/BehaviourSettings/` for A, beside 6083's pair, because the harness it
needs lives there: `BehaviourSettingsAcceptanceTest` (real host, real SQLite per test, `RunEverySeeder()` as
start-up/upgrade, `GetOptionalFeature(s)`, `ReadStoredOptionalFeature`, `ReadStoredAppSetting`), plus the real
`DatabaseManagementService.RestoreBackup`/`ClearDatabase` with only `IDatabaseManagementProvider` mocked, as 6083
drove them. `…/PercentilesOverTime/` for B, beside the fill's own scenarios (reason above). C goes into the two
existing seeder test files, as 6083 did. The restore/clear step definitions are a copy of 6083's: 6083's suite is
retired in DELIVER, and this copy is the one that survives.

## Wave: DISTILL / [REF] Driving-port coverage

| Driving port | How the scenario reaches it | Scenarios |
|--------------|-----------------------------|-----------|
| Application start-up (`OptionalFeatureSeeder.Seed()`, `AppSettingSeeder.Seed()` and every other `ISeeder`) | fixture `[SetUp]` seeds an empty database (the new instance); `WhenTheInstanceIsUpgraded` / `WhenTheInstanceRestarts(n)` re-run every seeder | all; #10, #11 call the seeder directly |
| `GET /api/latest/optionalfeatures` (Settings → System) | real HTTP as a System Admin, JSON read off the body | 1–7 |
| `GET` and `POST /api/latest/optionalfeatures/OverTimeHistoryFill` | real HTTP as a System Admin, raw body as the last release served it | 5 |
| Restore and clear (`DatabaseManagementService.RestoreBackup` / `ClearDatabase`) | real service, provider mocked; each `When` checks the operation completed and the seeders ran | 6, 7 |
| Opening Percentiles Over Time / PBC Over Time (the fill trigger) | real series endpoints, real filler drained | 8, 9 |

The store is read directly in two places, both with no port to read through: the record (`ReadStoredAppSetting`)
and the full app-settings list in #2 and #4 (the "nothing else changed" half of AC-1.3, and of every restart).

## Wave: DISTILL / [REF] Adapter coverage

No new driven adapter.

| Adapter | Treatment | Covered by |
|---------|-----------|------------|
| EF `LighthouseAppContext` (`OptionalFeatures`, `AppSettings`, over-time snapshot tables) | real, SQLite file | every scenario |
| `IDatabaseManagementProvider` | `Mock`, standing in for the database tooling | 6, 7 |
| `ILighthouseClock` | `FakeLighthouseClock` (6053 harness) | 8, 9 |
| `ILicenseService` | `Mock` (both harnesses), licensed | all |

Postgres is not run separately: seeder data only, no schema change, and the E2E runs on Postgres in CI
(`ci_verifypostgres`).

## Wave: DISTILL / [REF] Scaffolds

None. No test references a production symbol that does not exist yet: the setting and the record are addressed by
their literals `"OverTimeHistoryFill"` and `"OptionalFeatures:HistoryFillSwitchedOnByDefault"`, because both
constants (`OptionalFeatureKeys.OverTimeHistoryFillKey` stays, `AppSettingKeys.HistoryFillSwitchedOnByDefault`
goes) are DELIVER's to change.

RED classification: `docs/feature/story-6084-over-time-history-fill-always-on/red-classification.md` — 16/16
MISSING_FUNCTIONALITY.

## Wave: DISTILL / [REF] Existing-test disposition (executed by DELIVER)

Untouched in DISTILL: the production code they guard still exists. Suggested DELIVER steps, each one commit and
green on its own:

- **Step 01 — the fill stops reading a setting (D5).** Delete `IOverTimeHistoryFillSwitch`, `OverTimeHistoryFillSwitch`,
  the `Program.cs` registration and both checks. Un-ignore #8 and #9. Nothing about the row changes yet, so
  6083's suite stays green.
- **Step 02 — the setting and its leftovers go (D2, D3, D4).** Deprecated-keys list, key-based obsolete list in
  `AppSettingSeeder`, marker code and `SettleTheHistoryFillAfterARestore` and its call deleted. Un-ignore #1–#7,
  #10, #11. The E2E edits go in this commit, because this is the commit that makes `switchHistoryFill` 404.
- **Finalize** — docs, Lighthouse-Clients copy, screenshot.

Paths under `Lighthouse.Backend/Lighthouse.Backend.Tests/` unless stated.

| File | Member | Action | Step |
|------|--------|--------|------|
| `Services/Implementation/OverTimeHistoryFillSwitchTest.cs` | whole file (3 tests) | **Delete** with the switch | 01 |
| `Services/Implementation/OverTimeGapReconcilerTest.cs` | `MissingDaysWhileTheFillIsSwitchedOff_AreNotAskedFor` | **Delete** | 01 |
| same | `APeriodReachingPastToday_AsksForNoDayAfterToday`, `ReconcilerOver(…, bool switchedOn)` | **Rewrite**: drop the switch mock and the `switchedOn` parameter from the constructor calls; the other four tests keep their assertions | 01 |
| `Services/Implementation/BackgroundServices/OverTimeHistoryFillerTest.cs` | `AWaitingPass_WhileFillingInPastDaysIsSwitchedOff_LoadsNoOwner` | **Delete** (its contract is the one D5 removes; #9 pins the opposite at the chart) | 01 |
| same | `fillSwitchMock` field, its set-up and DI line | **Delete**; every other test is unchanged | 01 |
| `Services/Implementation/DatabaseMaintenanceGateTest.cs` | all | **Keep**: it mocks `IOverTimeHistoryFillActivity` and never names the switch (Open check 2) | — |
| `Architecture/OverTimeReconstructionSeamArchUnitTest.cs` | `TheFillSwitchKey_IsNamedOnlyByTheKeyList_TheSeeder_AndTheSwitch` | **Rewrite** to "named only by the key list and `OptionalFeatureSeeder`" (its deprecated-keys list after step 02); drop `TheFillSwitchPort` and the exactly-one-implementation assertion (DoD 3) | 01 |
| same | `WhatTheFillSwitchDoesNotGovern_NeverAsksIt`, `WhatTheFillSwitchDoesNotGovern` | **Delete**: once the key is confined to the key list and the seeder, nothing else can name it, so this list adds nothing | 01 |
| same | `OnlyTheReconciler_AsksTheFillerForDays` | **Keep, reword** its summary and message: still the one door (memo, budget, bounds), no longer "where the switch is read" | 01 |
| same | `TheFrontend_NeverNamesTheFillSwitch` | **Keep** (rename optional): the frontend still must not name the key | — |
| `API/Integration/PercentilesOverTime/Slice09TheFillShipsOptInScenarios.cs` | `With_the_fill_switched_off_opening_a_chart_queues_nothing_and_switching_it_on_needs_no_restart` | **Delete**: there is no off | 01 |
| same | `A_chart_already_filling_in_when_the_fill_is_switched_off_finishes_and_one_still_waiting_never_starts` | **Delete**; replaced by #9 | 01 |
| same | `Switching_the_fill_off_keeps_every_day_it_filled_and_switching_it_back_on_fills_only_what_is_still_missing` | **Delete**; "rewrites nothing it already wrote" stays pinned by Slice05 (`A_day_that_was_actually_watched_keeps_the_value_it_was_watched_at`, `Looking_at_the_same_period_twice_costs_nothing_the_second_time`) | 01 |
| same | `An_instance_that_stores_no_fill_switch_fills_nothing_until_the_upgrade_switches_it_on` | **Delete**; no row is now the normal state and it fills (#8, #9) | 01 |
| same | `Someone_who_is_not_a_system_admin_cannot_switch_the_fill_and_their_charts_carry_on_as_before` | **Delete**; the endpoint's System-Admin guard is pinned by `OptionalFeaturesControllerTest.UpdateOptionalFeature_HasSystemAdminRbacGuardAttribute` | 01 |
| same | `The_daily_recording_still_writes_today_with_the_fill_switched_off`, `A_quiet_day_is_still_left_blank_by_the_daily_recording_with_the_fill_switched_off` | **Rewrite**: drop the `GivenTheFillIsSwitchedOff()` line and the "with the fill switched off" suffix. "A refresh writes nothing but today" is still worth holding now that the fill always runs | 01 |
| same | `Switching_the_fill_leaves_a_line_in_the_log_saying_which_way_it_went` | **Delete**, in the same commit that adds its replacement on the BehaviourSettings harness (next row) | 01 |
| `API/Integration/BehaviourSettings/` (a scenario on `BehaviourSettingsAcceptanceTest`) | the controller's generic `"Optional feature {FeatureKey} switched from {WasEnabled} to {Enabled}"` log line | **Move** the assertion here, switching `FeatureOrdering` instead of the fill, so the log line keeps a test once the Slice09 scenario goes. Bring the log capture over from `Slice09TheFillShipsOptInSpecifications` if the harness lacks one | 01 |
| same | `Opening_a_chart_that_is_missing_nothing_pays_nothing_for_the_switch_and_one_missing_days_pays_one_lookup` | **Rewrite** to "opening a chart never looks up a behaviour setting": both reads send no command naming `OptionalFeatures`. Cheap, and it fails if a gate comes back | 01 |
| `…/Slice09TheFillShipsOptInSpecifications.cs` + class summary | the switch step definitions, log capture if unused | **Delete** what the deletions orphan; reword the class summary. Keep the class name; renaming is churn | 01 |
| `API/Integration/PercentilesOverTime/ReconstructOverTimeHistoryAcceptanceTest.cs` | `TheFillSwitchKey`, `TheStoredFillSwitch`, `TheFillIsSwitched`, `SomeoneAsksToSwitchTheFill`, `TheFillSwitchIsNotStoredAtAll` | **Delete** once Slice09's callers are gone (#8/#9 carry their own store writes). Keep `TheInstanceIsUpgraded`. Its `[SetUp]` needs no change (Open check 1) | 01 |
| `API/Integration/BehaviourSettings/Story6083HistoryFillOnByDefault{Scenarios,Specifications}.cs` | whole pair (9 methods, 11 cases) | **Delete**: every scenario asserts the row on, the record written, or a restore keeping the row off, all of which D2/D3 reverse. #1–#7 replace it | 02 |
| `Services/Implementation/Seeding/OptionalFeatureSeederTests.cs` | `SeedAsync_AddsTheOverTimeHistoryFill_OnInPreviewAndFree_AndRecordsThatItSwitchedItOn`, `SeedAsync_RunAgainInTheSameProcessAfterTheAdministratorSwitchedTheFillOff_LeavesItOff`, `SeedAsync_OverTimeHistoryFillSwitchedOnBeforeTheUpgrade_StaysOnAndIsRedescribed`, `SeedAsync_OverTimeHistoryFill_ReadsTheWayAnAdministratorSeesIt`, both `SettleTheHistoryFillAfterARestore_*` | **Delete** | 02 |
| same | `TheSettingsTheProductSeeds` | **Edit**: drop `OptionalFeatureKeys.OverTimeHistoryFillKey` | 02 |
| same | `SeedAsync_RemovesDeprecatedFeatures` | **Edit**: add `[TestCase(OptionalFeatureKeys.OverTimeHistoryFillKey, true)]` and `(…, false)`, as DeltaSync has | 02 |
| same | #11 | **Un-ignore** | 02 |
| same | summary of the `OverTimeHistoryFillKey` constant ("The switch that decides whether this instance fills in past days … a script switching it from outside uses") | **Reword**: it now names a setting that no longer exists, kept as a literal because an upgrading instance still carries that key. Left as is in DISTILL because production still has the switch | 02 |
| `Services/Implementation/Seeding/AppSettingSeederTests.cs` | `SeedAsync_KeepsTheRecordThatTheHistoryFillWasSwitchedOn` | **Delete**: it asserts the opposite of #10 | 02 |
| same | #10; `SeedAsync_CanBeCalledMultipleTimes_WithoutErrors` (count 7) | **Un-ignore** #10; keep the other unchanged | 02 |
| `TestHelpers/EncryptedBackupStream.cs` | — | **Keep**: used by `DatabaseManagementServiceTest` and #6 | — |
| `Lighthouse.EndToEndTests/tests/helpers/api/optionalFeatures.ts` | `switchHistoryFill`, `OVER_TIME_HISTORY_FILL_KEY` | **Delete** | 02 |
| `…/tests/specs/flow/PercentilesOverTime.spec.ts`, `PbcOverTime.spec.ts`, `PredictabilityOverTime.spec.ts` | `beforeEach`/`afterEach` calling `switchHistoryFill`, and the import | **Delete** | 02 |
| `…/tests/specs/screenshots/Screenshots.spec.ts` | `switchHistoryFill(request, true)` in "Take @screenshot of the system configuration page"; the `testWithDemoHistoryFill` fixture | **Delete** the call; the over-time shot uses `testWithDemo` directly and its comment loses the switch. Keep both over-time widgets in the dashboard loop's `coveredElsewhere` | 02 |

No other test names the key, the record, the switch port or `"Fill in past days"`: checked by grepping
`IOverTimeHistoryFillSwitch|OverTimeHistoryFillSwitch|IsSwitchedOn|HistoryFillSwitchedOnByDefault|OverTimeHistoryFillKey|"OverTimeHistoryFill"`
across `Lighthouse.Backend` and `HistoryFill` across `Lighthouse.EndToEndTests/tests`.

## Wave: DISTILL / [REF] E2E impact

No E2E added; the backend scenarios carry the story (one walking skeleton per flow already exists and keeps running).
Two consequences DELIVER must run, not reason about:

1. **The fill now runs in every E2E spec that opens Predictability.** The three flow specs and the screenshot fixture
   switched it **off** afterwards so later specs met no background fill. That off no longer exists. Any spec that
   opens an over-time widget (or the dashboard loop) on demo data now starts a fill, possibly mid-screenshot or
   mid-assertion. Run the full E2E suite once, SQLite and Postgres, including `DatabaseManagement.spec.ts` (a restore
   while a fill may be in flight meets the maintenance gate), and fix any spec that leaned on off.
2. **`settings/configuration.png` changes.** It shows the row today. Regenerate it at finalize (`rm` the PNG first,
   because the regenerator keeps the old file under a 0.5 % pixel difference), and check whether docs embed it.

## Wave: DISTILL / [REF] Open checks for DISTILL — answers

1. **The two gates in the 6053 harness.** `ReconstructOverTimeHistoryAcceptanceTest`'s `[SetUp]` no longer switches
   the fill on: #6083's DELIVER removed `SwitchTheFillOnTheWayAnAdministratorWould()`, and `Init()` only seeds. So
   nothing there will 404. The only callers of the endpoint are Slice09's switch steps, disposed above. **No 6053
   scenario outside Slice09 depends on the fill being off for its Given** (grep of `TheFillIsSwitched`,
   `SomeoneAsksToSwitchTheFill`, `TheStoredFillSwitch`, `TheFillSwitchIsNotStoredAtAll`: Slice09 only). Within
   Slice09, the five off-dependent scenarios are deleted and three rewritten.
2. **"A pass that got past this point finishes even if the switch goes off."** No maintenance-gate scenario relied
   on a dropped pass never counting as in flight. `DatabaseMaintenanceGateTest` mocks `IOverTimeHistoryFillActivity`
   only, and the 6053 gate scenarios (`An_operator_cannot_start_a_database_restore_while_the_chart_is_filling_in`,
   `The_chart_stops_filling_itself_in_while_the_operator_is_restoring_the_database`) never switch the fill. After
   D5 every waiting pass counts as running from the moment it starts, which is what the gate already assumes. The
   filler's comment about "settled before the pass counts as running" goes with the check.
3. **Downgrade path.** Confirmed by reading `OptionalFeatureSeeder` at the flip commit `e551cab5f` (in both
   v26.10.3.6 and v26.10.10.1) and at HEAD: a missing row is added from `GetOptionalFeatures()` with `Enabled = true`,
   and a missing record is written. So a downgrade re-seeds the row **on** and writes the record, and a re-upgrade
   removes both again. The restore-off rule (`6e1ab3dff`) is in both releases too, but a downgrade is a start-up,
   not a restore, so it does not apply. No test, as DISCUSS asked.
4. **`RemoveObsoleteSettings` by key and by number together.** Pinned by #10 over the real seeder on SQLite. The
   record is stored with number 0 (`RecordThatTheHistoryFillWasSwitchedOn` writes `Id = 0`), which
   `TeamDataRefreshInterval` and the settings written at runtime through `AppSettingService` (`Install:Timestamp`,
   for one) also carry, because `AppSettings` is keyed by `Key` and nothing generates `Id`. So the record cannot be reached by
   number without deleting those, which is why D4 is right. #10 holds both directions: the key list takes only the
   record (number 0 neighbours kept), and the number list still takes 9 and keeps the numbers it skips (18, 26, 37).

## Wave: DISTILL / [REF] Upstream findings

1. **DISCUSS "Open checks" item 1 is out of date, and so is the brief for this wave.** Both say
   `ReconstructOverTimeHistoryAcceptanceTest`'s `[SetUp]` switches the fill on through the endpoint and will 404.
   It does not: #6083 DELIVER deleted that call (its disposition table recommended it). No decision changes; recorded
   so DELIVER does not hunt for it.
2. **E2E loses its "fill off between specs" isolation (new risk, not in DISCUSS).** DISCUSS lists dropping the
   `switchHistoryFill` calls as a mechanical edit. They were also the thing that kept later specs free of background
   fills on demo data. After D1 there is no way to keep that isolation. See E2E impact 1; worth the full E2E run.
3. **`settings/configuration.png` must be regenerated.** Not in DISCUSS's E2E or Docs bullets. Finalize item.
4. **Low: the behaviour-settings log line loses its only test.** `OptionalFeaturesController` logs "Optional feature
   {Key} switched from {WasEnabled} to {Enabled}" for every key. Its only pin is Slice09's
   `Switching_the_fill_leaves_a_line_in_the_log_saying_which_way_it_went`. Resolved at review: DELIVER moves it to the
   BehaviourSettings harness on `FeatureOrdering` in step 01 (disposition table).
5. **D2's rolling-deploy note holds.** During a mixed-version deploy an old replica reads a missing row as off
   (`OverTimeHistoryFillSwitch.IsSwitchedOn`: "A row nobody has stored reads as off"), so it neither fills nor drops
   anything it had not already. An old replica restarting mid-deploy re-adds the row **on** with the record (Open
   check 3), which the next new start-up removes; now recorded in D2 as an accepted risk. Neither is scenario'd: it
   needs the multi-host harness and DEVOPS was skipped.

Mandate compliance, briefly: tests enter through driving ports only (HTTP list/read/write, the series endpoints,
seeder re-runs, the restore and clear service). Scenario and step names are domain language; "store" appears only
in step names that read what no port shows. The SUT is the production composition root
(`TestWebApplicationFactory<Program>`) with only the licence, the clock and the database tooling faked. The
15-item completeness checklist was not scored; the open gap is finding 5.

## Wave: DISTILL / [REF] Pre-requisites

- Step 01 (D5) turns #8 and #9 green on its own. Step 02 (D2 + D3 + D4 together) turns #1–#7, #10 and #11 green:
  deprecating the row without the key-based removal leaves #2/#3/#4 red on the record, and removing the restore hook
  without deprecating the row leaves #6 red.
- Filter for the new tests: `TestCategory=story-6084-over-time-history-fill-always-on|FullyQualifiedName~AppSettingSeederTests|FullyQualifiedName~OptionalFeatureSeederTests`
  (with the CLAUDE.md connector exclusions): 38 passed, 16 skipped, 0 failed on 2026-10-15 (after the review revision). Step 01 touches
  `Program.cs`, so the full Integration suite runs (DoD 8), and the whole `PercentilesOverTime` directory must
  run after the Slice09 edits.

## Wave: DISTILL / [REF] Review gate

- **Sentinel** (acceptance designer reviewer): approved, 0 blockers / 0 high / 7 lows, all addressed in the revision pass (on-by-default restore case, PBC checked on its own limit days, no story numbers in test comments, seeder-test summary and log-line rows in the disposition table, the AC-1.4 coverage note, the set-up step justified, #4 compares every stored value).
- **Eclipse** (product owner reviewer): rejected_pending_revisions on B1/B2, fixed by the US-01 examples block; H1, M1–M3 and L1–L10 addressed as amendments marked "(amended at DISTILL review, 2026-10-15)", scenario titles reworded to outcomes, or recorded in the disposition table.
- **Architect** (solution architect reviewer): N/A, because the maintainer skipped DESIGN.
- **Forge** (platform architect reviewer): N/A, because the maintainer skipped DEVOPS.
- **Eclipse re-review** (revision cycle 1): approved, 0 blockers / 0 high / 3 lows. The examples block now cites #9 for the fill after a restore, names Project Apollo as a Portfolio, and DoD 5 lists the regenerated `configuration.png`.
