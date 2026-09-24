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
| `OUT-5913-delta-without-asking` | On an upgraded instance, 100 % of refreshes of Jira Cloud / Data Center / Azure DevOps teams and portfolios that have stored stamps and an unchanged fetch shape log `mode=Delta` | The `Update completed` summary lines in the instance log after the first post-upgrade cycle. On the dev instance (`:5169`), counted the same way D3 counted them |
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

## Wave: DESIGN / [REF] Handoff

DISTILL gets: US-01 with AC-1.1 … AC-1.6, the resolver's decision table and the test-surface list above.
The acceptance entry point stays the Epic #5687 harness (`FasterUpdatesAcceptanceTest`), driven through the
scheduled refresh and the seeder, with no new fixture.
