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
