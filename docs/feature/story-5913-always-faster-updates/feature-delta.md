# Feature Delta — story-5913-always-faster-updates

> ADO User Story #5913 "Faster Updates :: Remove Toggle and always use it". Takes away the *Faster Updates*
> switch from Settings → System and makes the two-step refresh the only refresh wherever the connector
> supports it. This is the second half of a decision Epic #5687 already made: *"The flag has a defined end:
> once KPI-3 holds on real instances, it flips to on-by-default and is removed. A gate nobody removes
> becomes a permanent second code path."* (`docs/feature/epic-5687-faster-updates/feature-delta.md`, A1).
> The on-by-default half shipped with the epic. This story is the removal.

Combined DISCUSS + DESIGN pass (2026-09-24). The design lineage is already on file (Epic #5687 A1, ADR-138,
ADR-139), and the three decisions that were still open were taken by the maintainer before this pass began.

---

## Wave: DISCUSS / [REF] Persona IDs

- **platform-operator** — runs the instance and chooses the refresh interval. Before this change they had a
  switch that could only ever be set one way on purpose.
- **config-admin** (secondary) — reads the System settings list. After this change it has one row fewer.

## Wave: DISCUSS / [REF] JTBD One-Liners

Refines `job-operator-sync-without-hammering-the-tracker` (`docs/product/jobs.yaml`). Its **habit** force
reads: *"Operators today compensate by widening the refresh interval and accepting staler data. They must
learn they no longer need to."* A switch that has to be found, understood and left on is part of that habit.
Removing it means the saving applies without the operator doing anything. The job itself does not change,
so `jobs.yaml` is not edited.

## Wave: DISCUSS / [REF] Pre-requisites

- Epic #5687 is shipped and finalized (`docs/evolution/2026-08-14-epic-5687-faster-updates.md`). Delta
  refresh is live for Jira Cloud, Jira Data Center and Azure DevOps. ServiceNow, Linear and CSV answer
  `SupportsIncrementalSync = false` and always refresh in full.
- The seeder already writes `DeltaSync` with `Enabled = true` on a fresh install. Only an instance that turned
  it off, or upgraded while it was still opt-in and never turned it on, still runs full refreshes.

## Wave: DISCUSS / [REF] Locked Decisions

| ID | Decision | Verdict |
|----|----------|---------|
| D1 | **An instance that had Faster Updates off gets it on.** The `DeltaSync` row is removed, and every instance runs the two-step refresh wherever the connector supports it. A previous "off" is no longer honoured, and the release notes say so plainly. No startup warning. | Locked — maintainer, 2026-09-24 |
| D2 | **No kill switch.** Nothing replaces the toggle: no config key, no environment variable, no hidden setting. The only safety net is the one that already exists — a failed or refused identity scan downloads the whole query and logs a `WARNING`. A misbehaving connector is fixed with a release. | Locked — maintainer, 2026-09-24 |
| D3 | **KPI-3 is accepted on the evidence at hand.** The dev instance logged 118 delta refreshes from 2026-08-10 to 2026-09-24 (`Lighthouse.Backend/Lighthouse.Backend/logs`), none with `success=False`, and every `Identity scan failed` fell back to a full download. Together with the removal-correctness integration tests from the epic, this is enough to lift the gate. This is a maintainer acceptance, like AC-2.8 was: the log records failures, not drift. | Locked — maintainer, 2026-09-24 |
| D4 | **The removal is done by the seeder, not by a migration.** `DeltaSync` joins `RemoveDeprecatedFeatures` next to the four keys already retired that way. The constant stays in `OptionalFeatureKeys` because the deprecated list names it. No schema change, so the expand-only migration rule is not in play. | Locked |
| D5 | **The update log line does not change.** `Update completed \| … \| mode=Delta \| scanned=… \| fetched=…` is still how an operator sees what a refresh saved. Only the docs' advice to "turn the toggle off to compare" goes, because there is no toggle. | Locked |
| D6 | **Terminology.** User-facing text uses the configurable terms: *Work Item*, *Feature*. No new strings reach the UI, since the change removes a row and adds none. | Locked |

## Wave: DISCUSS / [REF] User Stories

### US-01 — Every refresh that can be cheap is cheap, without anyone having to ask for it

As a **platform operator**, I want Lighthouse to download only the Work Items and Features that changed on
every refresh it can, without a switch I have to find and leave on, so that the interval I choose is about
how fresh the data should be and never about what a refresh costs.

`job_id: job-operator-sync-without-hammering-the-tracker`

#### Elevator Pitch
Before: *Faster Updates* is a switch under Settings → System. An instance that turned it off, or never turned it on, re-downloads every Work Item on every refresh.
After: open **Settings → System** and there is no *Faster Updates* row. Refresh a Jira or Azure DevOps team twice, and the second refresh logs `Update completed | Team 'X' | mode=Delta | scanned=629 | fetched=9 | …`.
Decision enabled: the operator sets the refresh interval for freshness alone, knowing every capable connector already takes the cheap path.

#### Acceptance Criteria
- **AC-1.1** After upgrading, the System settings list carries no *Faster Updates* row, whether the instance had it on or off. `GET /api/latest/optionalfeatures` returns no `DeltaSync` entry.
- **AC-1.2** On an instance that had Faster Updates **off** before the upgrade, a Jira Cloud team with stored change stamps refreshes as `mode=Delta` on its next cycle. It scans the whole query and downloads only the Work Items whose stamp moved.
- **AC-1.3** The same holds for a portfolio's Features and for the parent Features it resolves. Each runs the two-step refresh without any opt-in.
- **AC-1.4** Everything that fell back to a full download before still does: a connector that cannot scan (ServiceNow, Linear, CSV), a failed scan (logged at `WARNING`), a changed fetch shape, a first refresh, or a stored record without a change stamp. Removal is still `stored − swept`.
- **AC-1.5** Upgrading again, which re-runs the seeder, never brings the row back.
- **AC-1.6** `docs/settings/configuration.md` describes Faster Updates as how every update works, with no toggle. The release notes tell operators who had switched it off that it is now on.

## Wave: DISCUSS / [REF] Out of Scope

- A kill switch in any form (D2).
- A startup warning for instances that had it off (D1).
- Delta refresh for ServiceNow and Linear. Those slices stay deferred, as Epic #5687 left them.
- Deleting the `DeltaSyncKey` constant (D4).
- Any change to the update log line (D5) or to `SyncOutcome`.
- Any change to the removal rule, staleness evaluation or fetch fingerprint (ADR-138/140/141 stand as they are).

## Wave: DISCUSS / [REF] Cross-cutting Impact

- **RBAC** — **N/A, because** nothing about authorization changes. Writing an optional feature stays behind `RbacGuard(SystemAdmin)`; the list simply has one row fewer. No UI gating changes, and nothing new reads `/authorization/my-summary`.
- **Lighthouse-Clients (CLI + MCP)** — **N/A, because** no contract changes and the clients never reference the key. `grep -ri "deltasync\|faster.updates"` over `/storage/repos/lighthouse-clients` (excluding `node_modules`/`dist`) finds nothing. The optional-features endpoint keeps its shape and returns one element fewer.
- **Website** — **N/A, because** `/storage/repos/website` has no mention of Faster Updates, `DeltaSync` or `optionalfeatures.png`. The capability was never marketed as a switch.
- **Premium** — **N/A, because** Faster Updates was never premium. With it gone, every optional feature that ships is premium (Feature Order, Never send usage data). A non-premium instance therefore sees only locked rows in that list. That is acceptable: the table renders premium rows locked with the licence tooltip, which is the existing upsell behaviour.

## Wave: DISCUSS / [REF] WS Strategy

Strategy **B (extend existing)**. One thin vertical slice, no skeleton. The seeder, the resolver and the
refresh already exist end to end. The slice removes a branch and a row.

## Wave: DISCUSS / [REF] Driving Ports

- The scheduled refresh (team, portfolio): `TeamUpdater` / `PortfolioUpdater` → `WorkItemService`.
- Application start-up: `OptionalFeatureSeeder`.
- `GET /api/latest/optionalfeatures` → Settings → System list.

## Wave: DISCUSS / [REF] Scope Assessment: PASS

One story, one slice, two bounded contexts touched lightly (refresh and settings seeding). Well under a day
of production change; the bulk of the effort is the test rework listed under DESIGN. No oversized signals.

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|-----|--------|-------------|
| `OUT-5913-delta-without-asking` | On an upgraded instance, 100 % of refreshes of Jira Cloud / Data Center / Azure DevOps teams and portfolios that have stored stamps and an unchanged fetch shape log `mode=Delta` | The `Update completed` summary line (emitted at Information level by every build, on every instance) after the first post-upgrade cycle: count `mode=Delta` against `mode=Full` for the capable connectors. Any operator can read it off their own log; this story measures it on the dev instance (`:5169`), counted the same way D3 counted them |
| `OUT-5913-no-resurrection` | The `DeltaSync` row count stays 0 across repeated start-ups | `SELECT COUNT(*) FROM OptionalFeatures WHERE Key = 'DeltaSync'` after two restarts |

## Wave: DISCUSS / [REF] Definition of Done

1. The `DeltaSync` row is removed at start-up and never re-seeded.
2. No production code reads the flag; `SyncModeResolver` has no opt-in parameter.
3. Every Epic #5687 acceptance scenario that is still meaningful stays green without an opt-in step. The ones that tested the toggle itself are deleted, not skipped.
4. The behaviour-settings and usage-data fixtures that used `DeltaSync` as their shipped non-premium row have a subject that still exists.
5. `ARCHITECTURE.md` §Background refresh and `docs/settings/configuration.md` no longer describe an opt-in.
6. Release-notes line drafted on the story (it carries the `Release Notes` tag): a previous "off" is now on.
7. `docs/assets/settings/optionalfeatures.png` regenerated at finalization.
8. `dotnet build` (warnings as errors) + the filtered `dotnet test` are green; `pnpm test` + `pnpm build` are green.
9. No new SonarCloud issues; Stryker ≥ 80 % on changed lines.

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|----------|--------|----------|
| 1 | User value clear | ✅ | US-01 elevator pitch |
| 2 | Job traceability | ✅ | `job-operator-sync-without-hammering-the-tracker` (habit force) |
| 3 | ACs testable | ✅ | AC-1.1…1.5 observable at the refresh / seeder / HTTP ports; AC-1.6 is a docs check |
| 4 | Dependencies known | ✅ | Epic #5687 shipped; no open dependency |
| 5 | Scope bounded | ✅ | Out of Scope + D2/D4/D5 |
| 6 | KPIs defined | ✅ | Two KPIs with numeric targets |
| 7 | Cross-cutting addressed | ✅ | RBAC, Clients, Website, Premium answered with evidence |
| 8 | Sized ≤ 1 day | ✅ | Scope Assessment PASS |
| 9 | No blocking unknowns | ✅ | D1–D3 taken by the maintainer; every read site is located (see DESIGN) |

## Wave: DISCUSS / [REF] Wave Decisions Summary

- **Key decisions:** D1 (off becomes on), D2 (no kill switch), D3 (KPI-3 accepted), D4 (seeder removal, no migration), D5 (log line unchanged).
- **Feature type:** backend behaviour change with one user-visible effect: a settings row disappears.
- **Constraints:** removal semantics, staleness evaluation and the fetch fingerprint are untouched. No contract change.
- **Upstream changes:** Epic #5687 A1 said the flag ends "once KPI-3 holds on real instances". D3 records that the maintainer accepted a single long-running dev instance as that evidence.

---

## Wave: DESIGN / [REF] Component Decisions

| Component | Path | Action | Detail |
|-----------|------|--------|--------|
| `SyncModeResolver.Resolve` | `Services/Implementation/WorkItems/SyncModeResolver.cs` | **SHRINK** | Drop the `operatorAskedForTheCheaperRefresh` parameter and its leading `return SyncMode.Full` branch. The remaining five branches and their order stay. Rewrite the doc comment that explains the opt-in. |
| `WorkItemService` | `Services/Implementation/WorkItems/WorkItemService.cs` | **SHRINK** | Delete `TheOperatorAskedForTheCheaperRefresh()`. The three fetch deciders — `ResolveRemoteFetch` (team), `ResolveRemoteFeatureFetch` (portfolio), `ResolveRemoteParentFeatureFetch` (parents) — always call their `Scan…Identities`. Each scan already returns `TrackerCanBeScanned: false` for a connector that cannot scan, so the capability gate needs no replacement. Rewrite the three doc comments that say "the opt-in gates the scan". |
| `WorkItemService` constructor | same | **SHRINK** | `IRepository<OptionalFeature> optionalFeatureRepository` had exactly one reader, the method above. Remove the parameter. DI resolves the constructor, so `Program.cs` is untouched; test construction sites drop the argument. The `S107` pragma stays, because the parameter count is still above the rule's limit. |
| `OptionalFeatureSeeder` | `Services/Implementation/Seeding/OptionalFeatureSeeder.cs` | **EXTEND / SHRINK** | Add `OptionalFeatureKeys.DeltaSyncKey` to `RemoveDeprecatedFeatures`. Remove the `DeltaSync` entry from `GetOptionalFeatures()`. |
| `OptionalFeatureKeys.DeltaSyncKey` | `Models/OptionalFeatures/OptionalFeatureKeys.cs` | **KEEP** | Still needed by the deprecated list, like the four keys retired before it. |
| Frontend | `Lighthouse.Frontend/src` | **NO PRODUCTION CHANGE** | The settings table is data-driven (`BehaviourSettingsTable` maps whatever the endpoint returns). No production file names `DeltaSync`. |
| `ARCHITECTURE.md` | root | **UPDATE** | §Background refresh: drop "with the `DeltaSync` optional feature on" and "nobody opted in". |
| `docs/settings/configuration.md` | docs | **UPDATE** | Rewrite the "Faster Updates" section (lines 75–89) as how updates work, not as a toggle. |

No new ADR. This carries out the end state ADR-138 and Epic #5687 A1 already name. A short delta is appended to
`docs/product/architecture/brief.md`.

## Wave: DESIGN / [REF] Resulting Decision Table

What `SyncModeResolver.Resolve` answers after the change (first match wins):

| # | Condition | Mode |
|---|-----------|------|
| 1 | connector cannot scan this connection | Full |
| 2 | the scan failed | Full |
| 3 | the fetch shape changed since the last refresh | Full |
| 4 | nothing stored yet | Full |
| 5 | a stored record has no remote change stamp | Full |
| — | otherwise | **Delta** |

## Wave: DESIGN / [REF] Test Surface Handed to DISTILL

Found by `grep -rc DeltaSyncKey` and by reading every opt-in helper's callers. DISTILL decides the treatment
of each one; the list is here so nothing is missed.

| Where | What it does with the toggle | Likely treatment |
|-------|-----------------------------|------------------|
| `FasterUpdates/FasterUpdatesAcceptanceTest.cs` (base) | `TheCheaperRefreshOption`, `TheOperatorAsksForTheCheaperRefresh`, `TheOperatorTurnsOffTheCheaperRefresh`, `TheInstanceIsUpgradedAgain` | Remove the first three. Keep `TheInstanceIsUpgradedAgain`, since AC-1.5 needs it |
| `Slice02JiraCloudTeamDeltaScenarios.cs` | AC-2.10 (off ⇒ never scans), AC-2.11 (on takes effect next cycle), AC-2.12 (fresh install on, upgrade keeps the choice) | Delete 2.10/2.11; replace 2.12 with AC-1.1/1.5 (row gone, stays gone) |
| `Slice03JiraCloudPortfolioDeltaScenarios.cs` | two "switched off ⇒ never scans" scenarios (Features, parent Features) | Delete |
| `Slice02/03/04/05 …Specifications.cs` | `GivenTheOperatorAskedForTheCheaperRefresh()` steps | Remove the step and its callers, because delta is now the default |
| `SyncModeResolverTest.cs` | branch for `operatorAskedForTheCheaperRefresh = false` | Delete that case; the remaining cases drop the argument |
| `WorkItemServiceTest.cs` | constructs the service with an optional-feature repository | Drop the argument |
| `OptionalFeatureSeederTests.cs` | nine `DeltaSyncKey` references (seeded on, upgrade keeps choice, …) | Invert: seeded never, removed when present regardless of `Enabled` |
| `BehaviourSettings/Slice01PremiumRefusalSpecifications.cs:20`, `Slice02OneListOfSwitchesSpecifications.cs:19`, `UsageDataVeto/Slice03VetoSettingSpecifications.cs:35` | use `DeltaSync` as the **only shipped non-premium row** | After removal, no non-premium row ships. DISTILL decides: seed a test-only non-premium row in the fixture, or change the scenario's subject |
| `Lighthouse.Frontend/src/pages/Settings/System/SystemSettingsTab.behaviourSettings.test.tsx` | mocked row keyed `DeltaSync` | A mock needs no shipped key, but a fixture named after a removed setting misleads a reader; DISTILL decides |
| E2E | no spec references the key; `Screenshots.spec.ts` produces `optionalfeatures.png` | Regenerate at finalization only |

## Wave: DESIGN / [REF] DEVOPS

**N/A, because** nothing is deployed, configured or observed differently. No config key is added (D2), no
Helm value, no pipeline step. The observability signal the KPIs read, the `Update completed` summary line,
already exists and does not change (D5).

**Rollback.** Downgrading to a build from before this story brings the *Faster Updates* row back **switched on**. The older seeder re-adds a missing row with its fresh-install default, and the operator's earlier "off" was deleted with the row, so it cannot be restored. An operator who downgrades and wants full refreshes has to switch it off again. Upgrading again removes the row without asking.

## Wave: DESIGN / [REF] Handoff

DISTILL gets: US-01 with AC-1.1 … AC-1.6, the resolver's decision table and the test-surface list above.
The acceptance entry point stays the Epic #5687 harness (`FasterUpdatesAcceptanceTest`), driven through the
scheduled refresh and the seeder, with no new fixture.

---

## Wave: DISTILL / [REF] Reconciliation

`[lang-mode] csharp` (NUnit 4, partial-class `…Scenarios.cs` + `…Specifications.cs`). `[policy-mode] inherit`:
every port in scope already has a row in `docs/architecture/atdd-infrastructure-policy.md` (HTTP API via
`WebApplicationFactory`, real EF over SQLite, `Mock<IWorkTrackingConnector>`, `Mock<ILicenseService>`,
`Mock<IForecastService>`), so nothing was appended. `[port-mode]` n/a: the project asserts through its own
harness observables, not a `state_delta` port.

Reconciliation passed — 0 contradictions. DISCUSS and DESIGN live in this file; DEVOPS is declared N/A by DESIGN
and nothing in D1–D6 needs an environment it does not already have.

## Wave: DISTILL / [REF] Scenario list with tags

File pair: `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/FasterUpdates/Story5913AlwaysFasterUpdates{Scenarios,Specifications}.cs`,
class `Story5913AlwaysFasterUpdatesTest : FasterUpdatesAcceptanceTest`. Every scenario is `[Ignore(PendingDeliver)]`
(`"Story #5913 — pending DELIVER"`). Seven test cases from six methods.

| # | Scenario | Tags | AC |
|---|----------|------|----|
| 1 | `A_fresh_install_offers_no_faster_updates_switch` | `@driving_port @real-io @contract-shape:unbounded-preservation` | AC-1.1 |
| 2 | `An_upgraded_instance_offers_no_faster_updates_switch_whichever_way_it_was_set(On)` / `(Off)` | `@driving_port @real-io @contract-shape:bounded-change` | AC-1.1 |
| 3 | `Upgrading_again_never_brings_the_faster_updates_switch_back` | `@driving_port @real-io @kpi @contract-shape:unbounded-preservation` | AC-1.5, `OUT-5913-no-resurrection` |
| 4 | `A_team_on_an_instance_that_had_faster_updates_off_downloads_only_the_issues_that_moved_after_the_upgrade` | `@driving_port @real-io @kpi @contract-shape:bounded-change` | AC-1.2, `OUT-5913-delta-without-asking` |
| 5 | `A_portfolio_on_an_instance_that_had_faster_updates_off_downloads_only_the_features_that_moved_after_the_upgrade` | `@driving_port @real-io @contract-shape:bounded-change` | AC-1.3 |
| 6 | `The_parent_features_on_an_instance_that_had_faster_updates_off_are_scanned_rather_than_downloaded_after_the_upgrade` | `@driving_port @real-io @contract-shape:bounded-change` | AC-1.3 |

Chaining: scenario 2's Given + When (had it set, upgraded) is scenario 3's Given (`GivenTheInstanceHasBeenUpgraded`
delegates to `WhenTheInstanceIsUpgraded`); scenarios 4–6 run a real refresh before the upgrade rather than seeding
its result. Scenario 4 runs on an instance **without a premium licence** on purpose — it carries forward the promise
the behaviour-settings fixtures used to hold ("Faster Updates is never gated by a licence") to the only place it is
still observable.

AC-1.4 (everything that fell back to Full still does) is **not** re-authored; see the coverage map below.
AC-1.6 is a docs check (`docs/settings/configuration.md` + release notes) and is not automatable as an acceptance
test; it is a DELIVER/finalize checklist item.

## Wave: DISTILL / [REF] WS strategy

No new walking skeleton, as DISCUSS decided (Strategy B, extend existing). The epic's skeletons stay
`Slice02JiraCloudTeamDeltaTest.A_later_refresh_downloads_only_the_issues_that_moved` and
`Slice03JiraCloudPortfolioDeltaTest.A_later_portfolio_refresh_downloads_only_the_features_that_moved`, which lose
only their opt-in step. Scenario 4 above is the demo for this story.

## Wave: DISTILL / [REF] Test placement

`Lighthouse.Backend.Tests/API/Integration/FasterUpdates/`, next to Slices 01–05, because the harness it needs
(`FasterUpdatesAcceptanceTest`: real update queue, real `WorkItemService`, real EF/SQLite, recorded tracker calls,
`TheInstanceIsUpgradedAgain`) already lives there. No new base class, no new fixture. Categories: `acceptance`,
`story-5913-always-faster-updates`.

## Wave: DISTILL / [REF] Driving-port coverage

| Driving port | How the scenario reaches it | Scenarios |
|--------------|-----------------------------|-----------|
| Application start-up (`OptionalFeatureSeeder`, all `ISeeder`s) | fixture `[SetUp]` seeds an empty database (the fresh install); `TheInstanceIsUpgradedAgain()` re-runs every seeder against the existing database (the upgrade) | 1, 2, 3, and the upgrade step of 4–6 |
| `GET /api/latest/optionalfeatures` (Settings → System list) | real HTTP through `Factory.CreateClient().AsSystemAdmin()`; keys read off the JSON body | 1, 2, 3 |
| Scheduled team refresh (`ITeamUpdater.TriggerUpdate` → update queue → `WorkItemService`) | `TheTeamRefreshRuns`, waits for the queue to go idle | 4 |
| Scheduled portfolio refresh (`IPortfolioUpdater.TriggerUpdate` → Features + parent Features) | `ThePortfolioRefreshRuns` | 5, 6 |

The store is also read directly once (scenario 3, `IRepository<OptionalFeature>.Exists`), because the KPI is a row
count and a row the endpoint filtered out would still be a resurrection.

## Wave: DISTILL / [REF] Adapter coverage

No new driven adapter — this story removes a read, it adds none.

| Adapter | Treatment | Covered by |
|---------|-----------|------------|
| EF `LighthouseAppContext` / `IRepository<OptionalFeature>` | real, SQLite | 1, 2, 3 (seeded and removed rows), and every refresh scenario |
| `IWorkItemRepository` (write-recording wrapper over the real repository) | real, SQLite | 4 |
| `IWorkTrackingConnector` | `Mock` programmed from one coherent tracker picture (existing harness) | 4, 5, 6 |
| `ILicenseService` | `Mock` | 4 (unlicensed) |

## Wave: DISTILL / [REF] Scaffolds

None. Every new scenario compiles against production types that already exist (`OptionalFeature`, `RefreshLog`,
`SyncMode`, the updaters, the seeders) and fails on an assertion, not on a missing symbol. The key the scenarios look
for is the literal `"DeltaSync"`, not `OptionalFeatureKeys.DeltaSyncKey`: an upgrading database carries that string
whatever the constant says, so a renamed constant cannot make "the row is gone" pass while the old row survives.

RED classification: `docs/feature/story-5913-always-faster-updates/red-classification.md` — 7/7 MISSING_FUNCTIONALITY.

## Wave: DISTILL / [REF] AC-1.4 coverage by existing scenarios

Once DELIVER removes their opt-in step, these existing scenarios carry AC-1.4. None is duplicated.

| Fallback to Full | Existing scenario (file : method) |
|------------------|-----------------------------------|
| Connector cannot scan (team) | `Slice02JiraCloudTeamDeltaScenarios.cs : A_tracker_that_says_it_cannot_be_swept_is_not_scanned_even_after_an_operator_asked` |
| Connector cannot scan (parents) | `Slice03JiraCloudPortfolioDeltaScenarios.cs : A_portfolio_whose_tracker_refuses_to_be_scanned_still_gets_every_parent_feature` |
| Failed scan, logged at WARNING (team) | `Slice02JiraCloudTeamDeltaScenarios.cs : A_refresh_whose_scan_fails_downloads_everything_rather_than_half` |
| Failed scan (Features) | `Slice03JiraCloudPortfolioDeltaScenarios.cs : A_portfolio_refresh_whose_scan_fails_downloads_every_feature_rather_than_half` |
| Failed scan (parents) | `Slice03JiraCloudPortfolioDeltaScenarios.cs : A_portfolio_refresh_whose_parent_scan_fails_downloads_every_parent_rather_than_half` |
| Changed fetch shape | `Slice05FetchFingerprintScenarios.cs : A_query_edit_makes_the_next_refresh_download_everything_again`, `An_edit_to_a_fetch_shaping_team_setting_makes_the_next_refresh_download_everything`, `An_edit_to_a_fetch_shaping_portfolio_setting_makes_the_next_refresh_download_every_feature` (and the other AC-5.1 cases) |
| No fingerprint recorded yet (upgrade into the fingerprint release) | `Slice05FetchFingerprintScenarios.cs : An_instance_that_upgraded_into_this_release_downloads_everything_on_its_first_refresh` |
| Stored record without a change stamp | `Slice02JiraCloudTeamDeltaScenarios.cs : The_first_refresh_after_an_upgrade_downloads_everything_and_remembers_when_each_issue_last_changed`; `Slice03JiraCloudPortfolioDeltaScenarios.cs : The_first_portfolio_refresh_downloads_every_feature_and_remembers_when_each_one_last_changed` |
| First refresh (nothing stored) | **Unit level only**: `SyncModeResolverTest : Resolve_NothingStoredYet_ResolvesToFull`, `Resolve_NoFeaturesStoredYet_ResolvesToFull`. At acceptance level it is exercised by every chained Given (the first cycle of scenarios 4–6 and of Slices 02/03) but no scenario asserts `mode=Full` for it — see Upstream findings. |
| Removal is still `stored − swept` | `Slice02 : An_issue_that_left_the_query_is_gone_from_the_team_on_the_very_next_cycle`; `Slice03 : A_feature_that_left_the_query_is_gone_from_the_portfolio_on_the_very_next_cycle` |

## Wave: DISTILL / [REF] Existing-test disposition (DELIVER's first step, same commit as the production change)

These stay green and untouched until DELIVER changes production. Each goes **in the same commit** as the change that
makes it wrong, so no commit is red. File paths are under `Lighthouse.Backend/Lighthouse.Backend.Tests/`.

| File | Member | Action |
|------|--------|--------|
| `API/Integration/FasterUpdates/Slice02JiraCloudTeamDeltaScenarios.cs` | `A_refresh_never_scans_once_the_operator_switched_it_off` | **Delete** (the switch it tests no longer exists) |
| same | `Asking_for_the_cheaper_refresh_takes_effect_on_the_very_next_cycle` | **Delete** |
| same | `A_fresh_install_gets_the_cheaper_refresh_and_an_upgrade_leaves_a_choice_alone` | **Delete** — replaced by Story5913 scenarios 1–3 |
| same | every remaining `GivenTheOperatorAskedForTheCheaperRefresh();` line (8 scenarios once the three above are gone) | **Remove the line** |
| same | `A_tracker_that_says_it_cannot_be_swept_is_not_scanned_even_after_an_operator_asked` | **Keep**; rename to drop "even after an operator asked" and reword its header comment |
| `API/Integration/FasterUpdates/Slice02JiraCloudTeamDeltaSpecifications.cs` | `GivenTheOperatorAskedForTheCheaperRefresh`, `GivenTheOperatorTurnedTheCheaperRefreshOff`, `WhenTheInstanceIsUpgradedAgain`, `ThenTheTrackerWasNeverScanned`, `ThenTheCheaperRefreshIsOfferedAndOn`, `ThenTheCheaperRefreshIsStillOff`, the `// --- Then: the opt-in gate ---` block | **Delete** (unused privates fail the build) |
| same | `ThenTheTrackerWasNotScannedEvenThoughTheOperatorAsked`; class doc comment ("nobody opted in") | **Reword** without the opt-in |
| `API/Integration/FasterUpdates/Slice03JiraCloudPortfolioDeltaScenarios.cs` | `A_portfolio_refresh_never_scans_once_the_operator_switched_it_off` | **Delete** |
| same | `A_portfolio_refresh_switched_off_never_scans_the_parent_features_either` | **Delete** |
| same | every remaining `GivenTheOperatorAskedForTheCheaperRefresh();` line (16 scenarios) | **Remove the line** |
| same | `A_portfolio_whose_tracker_refuses_to_be_scanned_still_gets_every_parent_feature` | **Keep**; reword the comment that mentions the opt-in |
| `API/Integration/FasterUpdates/Slice03JiraCloudPortfolioDeltaSpecifications.cs` | `GivenTheOperatorAskedForTheCheaperRefresh`, `GivenTheOperatorTurnedTheCheaperRefreshOff`, `ThenTheTrackersFeaturesWereNeverScanned` | **Delete** |
| same | `ThenTheParentFeaturesWereNeverScanned` message; `GivenAPortfolioWhoseTrackerRefusesToBeScanned` doc comment | **Reword** without the opt-in |
| `API/Integration/FasterUpdates/Slice04JiraDataCenterDeltaScenarios.cs` / `…Specifications.cs` | 2 × `GivenTheOperatorAskedForTheCheaperRefresh();` + the step | **Remove** |
| `API/Integration/FasterUpdates/Slice05FetchFingerprintScenarios.cs` / `…Specifications.cs` | 17 × `GivenTheOperatorAskedForTheCheaperRefresh();` + the step | **Remove** |
| `API/Integration/FasterUpdates/FasterUpdatesAcceptanceTest.cs` | `TheCheaperRefreshOption`, `TheOperatorAsksForTheCheaperRefresh`, `TheOperatorTurnsOffTheCheaperRefresh`, the `// --- The opt-in gate ---` heading | **Delete**; keep `TheInstanceIsUpgradedAgain` (Story5913 scenarios use it). Drop `using …Models.OptionalFeatures` if nothing else needs it |
| `API/Integration/BehaviourSettings/Slice01PremiumRefusalScenarios.cs` | `The_setting_the_licence_has_nothing_to_say_about_is_still_not_premium` | **Delete** — it asserts the seeded Faster Updates row's premium flag and has no subject once the row goes. Its promise ("Faster Updates never needs a licence") lives on in Story5913 scenario 4 |
| `API/Integration/BehaviourSettings/Slice01PremiumRefusalSpecifications.cs` | `TheFasterUpdatesRow`, `GivenTheFasterUpdatesRowAsTheProductSeedsIt`, `ThenTheStoredSettingIsNotPremium` (last caller) | **Delete** with the scenario above |
| `Services/Implementation/WorkItems/SyncModeResolverTest.cs` | `Resolve_NobodyOptedIn_ResolvesToFull`; the `operatorAskedForTheCheaperRefresh` parameter of the private `Resolve` helper | **Delete** the test; drop the parameter |
| `Services/Implementation/WorkItems/WorkItemServiceTest.cs` | `optionalFeatureRepositoryMock` field, its `[SetUp]` construction, the `GetByPredicate` setup returning a `DeltaSync` row (~line 1560), `.WithOptionalFeatureRepository(...)` (~line 1639) | **Delete** |
| `TestHelpers/WorkItemServiceTestBuilder.cs` | `WithOptionalFeatureRepository` (its only caller is the line above) and the field it sets | **Delete** |
| `Services/Implementation/Seeding/OptionalFeatureSeederTests.cs` | `SeedAsync_AddsDeltaSync_EnabledAndNoLongerInPreview`, `SeedAsync_DeltaSyncLeftOffOnAnExistingInstance_StaysOff`, `SeedAsync_DeltaSyncEnabledByOperator_StaysEnabled`, `SeedAsync_DeltaSync_NamesTheThingItFetchesInTheInstancesOwnWord` | **Delete**, replaced by: the `DeltaSync` key added to the `SeedAsync_RemovesDeprecatedFeatures` cases (run with the row both `Enabled = true` and `false`) and a "never seeded on an empty database" case |
| same | `SeedAsync_CanBeCalledMultipleTimes_WithoutErrors` expected-key list (~line 59); `SeedAsync_FeatureWasRenamedOrRedescribed_RefreshesTheTextWithoutTouchingTheOperatorsChoice` (uses `DeltaSyncKey` as its subject) | **Edit**: drop `DeltaSync` from the list — and hoist that inline `new[] { … }` into a `static readonly` field while the line is being touched (CA1861 judges an edited line as new code); move the rename test onto `FeatureOrderingKey` or `UsageDataKey` |
| `Story5913AlwaysFasterUpdatesScenarios.cs` | all seven cases | **Un-ignore one at a time** as each goes green |

Production comment that also goes stale: `OptionalFeatureKeys.DeltaSyncKey`'s doc comment still says it "ships dark -
off by default and flagged as a preview". D4 keeps the constant; its comment should say it names a retired row kept only
so the seeder can remove it.

## Wave: DISTILL / [REF] Fixture decision — the non-premium row

After the change no non-premium optional feature ships, so the three fixtures that borrowed `DeltaSync` as "the shipped
non-premium row" now seed their own: `BehaviourSettingsAcceptanceTest.NonPremiumFixtureKey = "NonPremiumFixture"`
via `SeedTheNonPremiumFixture()` (next to the existing `PremiumFixtureKey`), written straight into the store switched
off with `IsPremium = false`. Applied now; green before and after the production change.

| Scenario | Now | Still proves |
|----------|-----|--------------|
| `Slice01PremiumRefusalTest.The_setting_the_licence_has_nothing_to_say_about_is_taken_either_way(true/false)` | toggles the fixture row | a non-premium setting is writable with and without a licence. Stronger than before: the row starts **off**, so "stored on" can now fail (the seeded `DeltaSync` was already on) |
| `Slice01PremiumRefusalTest.The_setting_the_licence_has_nothing_to_say_about_is_still_not_premium` | **unchanged, still on `DeltaSync`** | only something about the seeded Faster Updates row. On a fixture row it would assert what the fixture wrote. Deleted by DELIVER (table above) |
| `Slice02OneListOfSwitchesTest.The_setting_that_was_already_in_the_list_is_carried_across_untouched` | fixture row read before and after the upgrade | the ordering upgrade leaves every other setting exactly as it was |
| `Slice02OneListOfSwitchesTest.Each_setting_in_the_list_is_switched_on_its_own` | fixture row beside the ordering row, both `Id = 0` | toggling one row does not move another that shares its number |
| `Slice02OneListOfSwitchesTest.The_setting_a_caller_names_is_the_setting_it_gets_back` | reads the fixture row by key | reading by name returns that row |
| `Slice03VetoSettingTest.A_setting_that_is_not_premium_still_toggles_on_an_instance_with_no_premium_licence` (renamed from `Faster_updates_still_toggles_…`) | toggles the fixture row unlicensed | the veto's premium branch does not spread to a non-premium row |

Frontend: `SystemSettingsTab.behaviourSettings.test.tsx` renamed its mocked row from `DeltaSync` / "Faster Updates" to
`NonPremiumExample` / "An example setting" (help text still carries `{{workItems}}` for the terminology test). A mock
needs no shipped key, and a fixture named after a setting that no longer exists would mislead the next reader. 16/16
green.

## Wave: DISTILL / [REF] Pre-requisites

- DESIGN component decisions (seeder EXTEND/SHRINK, resolver SHRINK, `WorkItemService` SHRINK) — the scenarios need
  **all three**: removing the row alone makes the refresh read "no row" as off (`TheOperatorAskedForTheCheaperRefresh`
  answers false on a missing row), so scenarios 4–6 stay red until the read is gone too.
- No DEVOPS dependency (N/A per DESIGN). Filtered backend run:
  `dotnet test --filter "FullyQualifiedName~FasterUpdates|FullyQualifiedName~BehaviourSettings|FullyQualifiedName~UsageDataVeto|FullyQualifiedName~OptionalFeatureSeeder"`
  → 150 passed, 7 skipped (the new scenarios), 0 failed on 2026-09-24.

## Wave: DISTILL / [REF] Upstream findings

1. **"A first refresh falls back to Full" (AC-1.4) has no acceptance scenario of its own.** DESIGN's hand-off lists it as
   covered by Slices 02–05; it is covered by `SyncModeResolverTest` and exercised as a Given everywhere, but never
   asserted at the refresh port. Not added here, per "do not duplicate"; worth one scenario if DELIVER wants AC-1.4
   fully port-level.
2. **The removal needs the read to go as well as the row** (Pre-requisites above). DESIGN says so implicitly; the
   scenarios make it explicit.
3. **DoD 3 wording.** "The ones that tested the toggle itself are deleted, not skipped" also applies to one
   behaviour-settings scenario (`…is_still_not_premium`) that DESIGN's test-surface list did not name.
4. AC-1.6 is not an acceptance-test criterion (docs + release notes); it stays a finalize checklist item.

Mandate compliance, briefly: tests enter through driving ports only (HTTP list, seeder re-run, updater trigger);
scenario and step names are domain language; production composition root (`TestWebApplicationFactory<Program>`) with
only the connector, licence and forecast faked; layer 3+ so example-only, no PBT (no unbounded input domain in a
removal story; the one finite axis, on/off, is a `[TestCase]`). The formal 15-item completeness checklist was not
scored; the only gap found is finding 1.

## Wave: DISTILL / [REF] Review Gate (2026-09-24)

| Reviewer | Scope | Verdict | Disposition |
|----------|-------|---------|-------------|
| Product owner | DISCUSS | approved (0 blocker, 2 low) | Both lows are format preferences (labelled examples, Given/When/Then wording); no change |
| Solution architect | DESIGN | approved (0 findings) | Single reader, scan guards on all three paths and the seeder pattern verified in code |
| Platform architect | DEVOPS N/A | conditionally approved (2 medium) | Applied: KPI measurement now says any instance can read it off its own log; rollback behaviour recorded under DESIGN / DEVOPS |
| Acceptance designer | DISTILL | needs revision (1 blocker) | **Overruled.** The "blocker" asks to delete `The_setting_the_licence_has_nothing_to_say_about_is_still_not_premium` now. It is already in the disposition table for deletion in DELIVER's first step, together with the production change, and stays green until then. All structural checks passed (driving ports, tags, ignores, deletion-table completeness) |

---

## Wave: DELIVER / [REF] Implementation summary

Three roadmap steps in one phase, in the order the DISTILL pre-requisites forced: the refresh stopped reading the
switch before the switch was removed, because a missing row read as "off" would have turned every instance back to
full refreshes. Step 01-01 (`1c7e88b71`) dropped the opt-in parameter and its leading `Full` branch from
`SyncModeResolver.Resolve`, deleted `WorkItemService.TheOperatorAskedForTheCheaperRefresh()` together with the
`IRepository<OptionalFeature>` it was the only reader of, and made the team, portfolio and parent-Feature fetch
deciders always call their identity scan; a connector that cannot scan still answers `TrackerCanBeScanned: false`, so
the capability gate needed no replacement. Step 01-02 (`1bd0e78c6`) moved `DeltaSyncKey` into the seeder's retired
keys and stopped seeding it, so the row disappears at start-up whichever way it was set and never comes back. Step
01-03 (`a226c4e96`) rewrote the user docs to describe how every update works rather than a toggle. A comments-only
refactor (`7fe595517`) followed. Every existing test the change made wrong was deleted or edited in the same commit
as the production change, per the DISTILL disposition table, so no commit is red. No migration, no contract change,
no frontend production change.

## Wave: DELIVER / [REF] Files modified

From `git diff --name-only 716b68e8c..HEAD`, excluding `a7ee9f29b` (another feature's DISCUSS+DESIGN commit that
landed in between). Backend paths are under `Lighthouse.Backend/`.

**Production (4)**
- `Lighthouse.Backend/Services/Implementation/WorkItems/SyncModeResolver.cs`
- `Lighthouse.Backend/Services/Implementation/WorkItems/WorkItemService.cs`
- `Lighthouse.Backend/Services/Implementation/Seeding/OptionalFeatureSeeder.cs`
- `Lighthouse.Backend/Models/OptionalFeatures/OptionalFeatureKeys.cs`

**Tests (17)**
- `Lighthouse.Backend.Tests/API/Integration/FasterUpdates/Story5913AlwaysFasterUpdatesScenarios.cs`, `…Specifications.cs` (un-ignored)
- `Lighthouse.Backend.Tests/API/Integration/FasterUpdates/FasterUpdatesAcceptanceTest.cs`
- `Lighthouse.Backend.Tests/API/Integration/FasterUpdates/Slice02JiraCloudTeamDelta{Scenarios,Specifications}.cs`
- `Lighthouse.Backend.Tests/API/Integration/FasterUpdates/Slice03JiraCloudPortfolioDelta{Scenarios,Specifications}.cs`
- `Lighthouse.Backend.Tests/API/Integration/FasterUpdates/Slice04JiraDataCenterDelta{Scenarios,Specifications}.cs`
- `Lighthouse.Backend.Tests/API/Integration/FasterUpdates/Slice05FetchFingerprint{Scenarios,Specifications}.cs`
- `Lighthouse.Backend.Tests/API/Integration/BehaviourSettings/Slice01PremiumRefusal{Scenarios,Specifications}.cs`
- `Lighthouse.Backend.Tests/Services/Implementation/Seeding/OptionalFeatureSeederTests.cs`
- `Lighthouse.Backend.Tests/Services/Implementation/WorkItems/SyncModeResolverTest.cs`
- `Lighthouse.Backend.Tests/Services/Implementation/WorkItems/WorkItemServiceTest.cs`
- `Lighthouse.Backend.Tests/TestHelpers/WorkItemServiceTestBuilder.cs`

**Docs (3)**
- `ARCHITECTURE.md` (§Background refresh)
- `docs/settings/configuration.md` (Faster Updates section)
- `docs/settings/systeminfo.md`

## Wave: DELIVER / [REF] Scenarios green

**7 of 7** `Story5913AlwaysFasterUpdatesTest` cases green, none left `[Ignore]`d: scenarios 4–6 un-ignored in 01-01,
scenarios 1–3 (four cases, scenario 2 running once with the switch left on and once off) in 01-02. Full filtered backend suite 7243 passed / 0 failed;
`pnpm test` 5698 passed. DES integrity: `des-verify-integrity` reports all 3 steps with complete traces (01-03's RED
phase recorded as not applicable, a docs-only step).

## Wave: DELIVER / [REF] Definition of Done check

| # | DoD item | Verdict | Evidence |
|---|----------|---------|----------|
| 1 | `DeltaSync` row removed at start-up, never re-seeded | **Pass** | Story5913 scenarios 1–3; `OptionalFeatureSeederTests` removal cases (row `Enabled` true and false) and the never-seeded case |
| 2 | No production code reads the flag; resolver has no opt-in parameter | **Pass** | `grep -rn "operatorAskedForTheCheaperRefresh\|TheOperatorAskedForTheCheaperRefresh" Lighthouse.Backend` → 0 matches; `SyncModeResolver` mutated whole at 100 % |
| 3 | Meaningful Epic #5687 scenarios stay green without an opt-in; toggle tests deleted, not skipped | **Pass** | Slices 02–05 lost their opt-in step and stay green; the five toggle scenarios and `…is_still_not_premium` were deleted |
| 4 | Behaviour-settings and usage-data fixtures have a subject that still exists | **Pass** | `NonPremiumFixture` row seeded by the fixture (DISTILL); green before and after |
| 5 | `ARCHITECTURE.md` and `configuration.md` no longer describe an opt-in | **Pass** | Commits `1c7e88b71` (ARCHITECTURE.md) and `a226c4e96` (configuration.md, systeminfo.md) |
| 6 | Release-notes line drafted | **Pass — drafted here, not posted.** Posting it to the ADO item is the maintainer's | Draft below |
| 7 | `optionalfeatures.png` regenerated | **Deferred** to the `/release` update-docs pass | Regenerating needs a full frontend build served on `:5169` plus a premium licence, and `:5169` is the maintainer's dev instance. The list is one row shorter; nothing else on the screen changes |
| 8 | `dotnet build` zero warnings + filtered `dotnet test` green; `pnpm test` + `pnpm build` green | **Pass** | 7243 / 0 backend; 5698 frontend |
| 9 | No new SonarCloud issues; Stryker ≥ 80 % on changed lines | **Stryker pass; SonarCloud deferred** | Stryker 100 % on changed lines (`mutation/results.md`). SonarCloud is verified only by CI after push, and the push is held |

**Draft release-notes line (DoD 6):**

> Refreshing a large Jira or Azure DevOps board used to mean downloading every Work Item again on every refresh,
> unless you had found the Faster Updates switch and left it on. Now every refresh of a Jira Cloud, Jira Data Center or
> Azure DevOps connection downloads only the Work Items and Features that changed, with nothing to switch on, so you
> can pick a refresh interval for how fresh you want your data rather than for what it costs. The switch is gone from
> Settings → System: if you had turned Faster Updates off, your instance now uses it too.

## Wave: DELIVER / [REF] Demo evidence

The Elevator Pitch is exercised end to end by the Story5913 acceptance scenarios, through the real
`GET /api/latest/optionalfeatures` endpoint (no *Faster Updates* row, fresh or upgraded, on or off), the real seeder
re-run as an upgrade (the row never comes back), and the scheduled team and portfolio refresh on an instance that had
it off (the next cycle scans and downloads only what moved, logging `mode=Delta`). Scenario 4 runs unlicensed. The
live check on a real instance is the maintainer's and is **pending**.

## Wave: DELIVER / [REF] Quality gates

| Gate | Result |
|------|--------|
| Roadmap review | Approved (nw-acceptance-designer-reviewer, 0 blocker / 0 high / 0 low) |
| Refactor pass | Comments only (`7fe595517`) |
| Adversarial review | Approved (nw-software-crafter-reviewer), 0 findings |
| Mutation | 100 % on changed lines; `SyncModeResolver` 100 % whole-file; the 17 `OptionalFeatureSeeder` survivors all pre-existing (`mutation/results.md`) |
| DES integrity | Pass — all 3 steps have complete traces |

## Wave: DELIVER / [REF] KPI measurement

Both OUT-5913 KPIs are measured **after release**, on real logs and a real database. `OUT-5913-delta-without-asking`
counts `mode=Delta` against `mode=Full` on the `Update completed` line for capable connectors after the first
post-upgrade cycle; `OUT-5913-no-resurrection` counts `DeltaSync` rows after repeated start-ups. The acceptance
scenarios prove the mechanism; they are not the measurement. No baseline exists before release.
