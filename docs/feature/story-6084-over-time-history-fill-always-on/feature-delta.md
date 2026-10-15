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
| D2 | **The row leaves the database in this release.** `OptionalFeatureKeys.OverTimeHistoryFillKey` moves to `OptionalFeatureSeeder.RemoveDeprecatedFeatures`, as `DeltaSyncKey` and `McpServerKey` did, and leaves `GetOptionalFeatures()`. During a mixed-version rolling deploy, an old replica finds no row and reads the fill as off, which only delays filling until every replica runs the new version. A downgrade re-seeds the row on (the #6083 seeder: missing row → seeded on, no marker → writes it). Seeder data, not schema, so no EF migration and no `CreateMigration` run. | Locked by the maintainer, 2026-10-15 |
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
After: upgrade, then open Percentiles Over Time for that Team. A little later the chart shows a point for each day the stored history can fill. **Settings → System** no longer lists the row, and `GET /api/latest/optionalfeatures` no longer returns `OverTimeHistoryFill`.
Decision enabled: the coach reads the trend without first asking whether the fill is on on this instance, and the admin has nothing to weigh up.

#### Acceptance Criteria
- **AC-1.1** On a fresh database, after start-up, `GET /api/latest/optionalfeatures` holds no `OverTimeHistoryFill`, and opening Percentiles Over Time or PBC Over Time for a range with missing days starts the fill.
- **AC-1.2** On an instance upgraded from a release that had the row, **whether it was on or off**, the first start-up of this release removes the row, and opening either chart starts the fill (D1, D2).
- **AC-1.3** That start-up also removes the `OptionalFeatures:HistoryFillSwitchedOnByDefault` app setting if it is present, and leaves every other app setting and every other optional feature exactly as they were (D4).
- **AC-1.4** Restoring a backup taken before #6053 (no row, no marker) or one that held the fill switched off ends with no `OverTimeHistoryFill` row and no marker, and the next chart open fills (D3).
- **AC-1.5** A waiting fill pass is never dropped because of a setting. The only things that stop or defer it are the ones that did before: the maintenance gate, the per-visit budget and the stored-history bounds (D5).
- **AC-1.6** `docs/settings/configuration.md` no longer describes the switch. `docs/metrics/predictability.md` describes the fill as always on and no longer a Preview, and keeps the caveats about today's configuration, filled days staying, and taking a backup before upgrading. The release notes tell admins that the setting is gone and the fill always runs. Links into the removed or renamed anchors resolve.

## Wave: DISCUSS / [REF] Out of Scope

- Any change to how the fill works: bounds, the 90-day per-visit budget, the memo, the maintenance gate or the filler's queueing.
- A way to opt out, whether by configuration, environment variable or API. D1 rejected it.
- Telling filled days apart from recorded ones, or undoing a fill.
- The Preview mechanism itself (`IsPreview` on other rows, and the Settings page's Preview handling).
- Frontend UI changes. There is no switch-dependent copy to change. The `overTimeEmptyState.ts` doc comment loses its "switched off" paragraph, which is a comment-only edit.

## Wave: DISCUSS / [REF] Cross-cutting Impact

- **RBAC**: **N/A, because** a key is being removed and no permission is added. With nothing left to switch, the `RbacGuard(SystemAdmin)` on writing this row goes away along with the row. Nothing new reads `/authorization/my-summary`.
- **Lighthouse-Clients (CLI + MCP)**: **affected, copy only, patch release.** These still say the fill is a Preview a System Admin switches on, which was already stale after #6083: `packages/client/src/index.ts:1091`, `packages/cli/src/index.ts:1592` (comment), `packages/mcp-core/src/index.ts:1081,1096,1111,1126` (tool descriptions), and `skills/lighthouse/references/lighthouse-mechanics.md:239`. Reword them as "Lighthouse also fills in missing past days in the background after a read, from the history it already stores". No contract change: no client calls `/optionalfeatures/OverTimeHistoryFill`.
- **Website**: **N/A, because** `/storage/repos/website` does not mention the fill or `OverTimeHistoryFill`.
- **Premium**: **N/A, because** the fill was free and stays free.
- **Docs**: `configuration.md` §"Fill in past days on over-time charts (Preview)" is removed. `predictability.md` §"Filling in past days (Preview)" becomes "Filling in past days", and its three `#filling-in-past-days-preview` links are repointed. `releasenotes.md:152` links to the removed configuration anchor, so that link is repointed to `predictability.html#filling-in-past-days`. Only the link target changes, and the past release-note text stays as written. `ARCHITECTURE.md:189,387` and ADR-207 are updated to say the fill always runs, with no behaviour setting. For ADR-207, the status note is amended rather than the decision rewritten.
- **E2E**: `tests/helpers/api/optionalFeatures.ts` (`switchHistoryFill`) and the `PbcOverTime`, `PercentilesOverTime`, `PredictabilityOverTime` and `Screenshots` specs that call it lose those calls. Removing them is a DISTILL item, and Playwright runs locally before the commit.
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
3. `OverTimeReconstructionSeamArchUnitTest`'s confinement of the key is updated to the deprecated-keys list only.
4. #6083's acceptance suite and #6053's opt-in suite (`Slice09TheFillShipsOptIn…`) are retired or rewritten to the always-on behaviour, and the switch's unit tests are deleted with the switch.
5. Docs per AC-1.6, plus `ARCHITECTURE.md` and the ADR-207 status note.
6. The Lighthouse-Clients copy is updated in the places listed, and a patch version is cut.
7. A release-notes line is drafted on #6084 (`Release Notes` tag): the setting is gone, the fill always runs, and take a backup before upgrading if you want the unfilled history kept.
8. `dotnet build` (warnings as errors) and the filtered `dotnet test` are green, plus the full Integration suite because `Program.cs` changes. `pnpm test` and `pnpm build` are green, and so is the E2E for the four touched specs.
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

- **The two gates in the 6053 harness.** `ReconstructOverTimeHistoryAcceptanceTest`'s `[SetUp]` switches the fill on through the endpoint. That call must go, and it would now 404. Check that no scenario in the 6053 suite depends on the fill being off for its "Given".
- **The filler test's "a pass that got past this point finishes even if the switch goes off" contract.** It loses its subject. Confirm that no maintenance-gate scenario relied on a dropped pass never counting as in flight.
- **Downgrade path.** A downgrade to v26.10.3.6 / v26.10.10.1 re-seeds the row on and writes the marker. Confirm this by reading #6083's seeder; it needs no test.
- **`RemoveObsoleteSettings` by key and by Id together.** The new key list must not delete a row the Id list would keep, or the other way round. Pin with a unit test over the real seeder on SQLite (`IntegrationTestBase`).
