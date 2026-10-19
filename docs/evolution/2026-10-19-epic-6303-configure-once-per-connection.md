# Configure once per Work Tracking Connection — Epic 6303

Delivered 2026-10-16 to 2026-10-19 in twelve slices of 48 steps (slice 07 dropped), across four ADO Stories that
each ship on their own: #6300, #6312, #6304 and #6313. Free, on every licence, no setting. Reported by community
member [Steve Pereira](https://www.linkedin.com/in/devopsto/) as a follow-up to Epic #6028, and credited in the
release notes.

Every Team on a Jira or Azure DevOps connection that reads its parent from the same link needed its own Parent
Override Field. Forget it on a new Team and its Work Items sat unparented, with nothing pointing at the cause. A
Portfolio repeated the same choices for its Dependency, Size Estimate and Feature Owner Fields and its Estimation,
and changing the field meant finding and changing every one of them by hand. Separately, a Portfolio whose waits
were recorded with any link type other than the default (`is blocked by` on Jira, Predecessor on Azure DevOps)
showed no dependencies, and a link-type Additional Field picked as the Dependency Field silently read nothing.

Those settings now live on the connection, once, under **Defaults for Teams and Portfolios**. Every Team and
Portfolio follows the default unless it owns its own choice, a deliberate None included. A Portfolio's Dependency
Field may name a Jira link phrase or an Azure DevOps relation type with its end, and a name that reads nothing says
so on Refresh History.

## What shipped

### Story #6300 — Jira dependencies from a named link (slices 01, 02)

| Step | What it changed |
| --- | --- |
| 01-01 … 01-02 | A Feature waits on what its named one-way link points at. The named end sets the direction, in any case and under a renamed phrase, and replaces `is blocked by`; a name that matches a field stays a field. No extra Jira request. |
| 01-03 … 01-05 | A name both ends share reads nothing and is recorded as one finding on the Portfolio's refresh row; Refresh History's read carries the findings and shows them under the Portfolio's figures. |
| 02-01 … 02-03 | A named link nothing carries gives one finding per full download, listing the links seen. A cheap refresh keeps showing the last full download's finding and its time. |

### Story #6312 — Azure DevOps dependencies from a named relation type (slices 03, 04)

| Step | What it changed |
| --- | --- |
| 03-01 … 03-02 | After a spike, Validate accepts a relation type reference name with its end, in any case, listing the organisation's relation types at most once; a typo is refused with the list, and a missing listing permission says so. |
| 03-03 … 03-04 | A refresh resolves the reference as a link type, with no missing-field warning. A relation type named in any other setting reads as nothing named, and Refresh History says only the Dependency Field reads one. |
| 04-01 … 04-03 | A Feature waits on the work item at the other end of the named relation, replacing Predecessor. Relations are read even with a Parent Override set, at one listing per connection per refresh across a shared connector. No end, no direction (`System.LinkTypes.Related`) or nothing carrying the end each say so once. |

### Story #6304 — Connection defaults, with the Teams' Parent Override Field (slices 05a, 05b, 06)

| Step | What it changed |
| --- | --- |
| 05-01 … 05-02 | An owner setting is its own value plus a source (*Connection default* or *Own*), and every reader, both connectors and the fetch fingerprint included, gets the effective value. The upgrade adds the source and the default, turns an empty value into *Connection default*, repairs a value pointing at a deleted field the same way, and moves no other fingerprint. |
| 05-03 … 05-06 | The connection save moves into `IConnectionSaveService`. One save sets the default for every Team that follows it and writes one Information log entry naming the followers. A connection offers only the defaults its tracker honours; the nested `defaults` member is authoritative when present and keeps every stored default when absent; a stale save is refused; a field added in the same save can be the default. |
| 05-07 … 05-11 | A Team's settings say what it follows and from where, and a new Team follows from the start. The connection page shows the defaults section in this instance's words, with the page's loading and could-not-load looks. The Team's dropdown opens on `Connection default (…)` with "Set on the <tracker> connection."; any other entry makes the value the Team's own. |
| 06-01 … 06-04 | Removing a field a default or an owner uses is refused, naming them, and nothing is saved, also when a Team starts using it while the removal is being saved. A Team cannot name a field its connection lacks. A PUT without the source keeps it unless it sends a different, non-empty value, so a script saving back what it read changes nothing. |
| 07-01 … 07-04 | Story 6307's update question is reused: a changed default with followers asks `Save and update` or `Save only`, with the follower counts and no names, and queues exactly the Teams whose fetch moved. Nobody following means no question. Answering it for a default alone does not report an Additional Fields change in usage data. |

### Story #6313 — Four more settings and Estimation (slices 08 to 12)

| Step | What it changed |
| --- | --- |
| 08-01 … 08-03 | The Portfolios' Parent Override Field, with its own default separate from the Teams' (they name different links); the two never mix. |
| 09-01 … 09-02 | The Portfolios' Dependency Field. Ignore Dependencies is never inherited. |
| 10-01 … 10-02 | The Portfolios' Size Estimate Field; an empty value falls back to the Portfolio's default size. |
| 11-01 … 11-02 | The Portfolios' Feature Owner Field, on CSV too; an empty value shares the work, and the Owning Team is never taken from the connection. |
| 12-01 … 12-04 | Estimation as one bundle (field, unit, non-numeric flag, categories) per grain, read at page load, so a change queues nothing. Edited on the connection with the owner's own Estimation editor, collapsed until a field is chosen, and saved through `Change the Estimation default?`. A script saving back a follower's estimation leaves it following. |
| 12-05 | E2E walking skeleton on the CSV demo connection: an admin sets the Teams' Estimation default and a following Team shows it. At the maintainer's request a second scenario covers the Portfolio side with the Feature Owner Field default. |

Each slice added its own expand-only migration through `CreateMigration`, and `HistoricalSchemaPatch` gained the
new columns. No new permission, no new controller, no licence gate, no client release, and the connection PUT's
response is unchanged.

## Decisions

### DISCUSS (maintainer, 2026-10-16)

- **One setting for dependencies** (D1): the Dependency Field accepts any Additional Field, resolved per tracker as
  Parent Override is on Jira; field lookup runs first and wins on a collision. No new control, no migration.
- **One-way only, one link type, replacing the default** (D2, D3); **Portfolio only** (D4); **no match warns and
  never falls back** (D5), on Refresh History (D5a).
- **Five settings follow a connection default** (D7), with **separate Parent Override defaults for Teams and for
  Portfolios** (D8), stored as **a source beside the value** (D9) so `null` keeps meaning None.
- **At the upgrade, empty becomes *Connection default***, and a value naming a deleted field is repaired the same way
  (D10, P-12). No default exists on release day, so those owners read the tracker's own value again; the release
  note says so. **New owners start on *Connection default*** (D11).
- **A PUT without the source keeps the stored source** unless it sends a present, non-empty, different value (D18,
  gate G-R1 option b). Option a (absent means *Own*) was rejected: a client that drops unknown properties would
  silently turn followers into a deliberate None.
- **A referenced field cannot be deleted** (D17); **Estimation follows as one bundle** (D16); **the defaults group
  offers only what the tracker honours** (D21): five on Jira and Azure DevOps, Feature Owner and Estimation on CSV,
  hidden on Linear and ServiceNow.
- **Four Stories, in order #6300 → #6312 → #6304 → #6313, each shipping alone** (D19).

### DESIGN (maintainer, 2026-10-16)

- **ADR-241**: an inherited setting is its own value plus a source, and the owner answers with the effective value,
  so the read and the fingerprint cannot drift.
- **ADR-242**: a connection save queues exactly the owners whose own fetch fingerprint moved, within one unit of work
  under the connection's existing concurrency token, and logs who follows. Defaults never enter
  `FetchFingerprint.ForConnection`.
- **ADR-243**: a Dependency Field may name a link type, read as a third source of the dependency selector.
- **ADR-244**: refresh findings ride on the records and land on the refresh-log row, showing the latest full run's.
- **P-5**: no on-screen notice after a save. The reused update question carries the follower counts, the log entry
  names the followers, and their refreshes show in Task Manager → Activity.
- **CA-6**: a non-directional Azure DevOps relation type, or a reference name without its end, reads nothing and warns.

### UI sketches (maintainer, before DISTILL)

- **UI-1**: its own section below Additional Fields, sub-headings *Teams* and *Portfolios*, no follower count on the
  page. **UI-2**: `Connection default (<value>)` as the first dropdown entry. **UI-3**: Story 6307's dialog reused;
  an Estimation-only change gets a plain Save. **UI-5**: the owner's Estimation editor reused inline.
  **UI-6, UI-7**: the Refresh History and refusal copy, the Azure DevOps wording approved during DELIVER.
- **UI-4: slice 07 dropped.** No "who follows / who differs" view and no bulk "make these follow" action. An owner
  hand-set to the default's value stays *Own*. US-06 and KPI `OUT-6303-followers-share` went with it; the job is
  `status: deferred` in `docs/product/jobs.yaml`.

### DEVOPS

- **Usage-data event: none.** N/A, because a connection default is set once at setup; an event would count
  configuration saves, not whether the feature helps anyone. `UsageDataEventName` is unchanged.
  `WorkTrackingSystemFieldsChanged` is now sent only when Additional Fields changed, and
  `docs/settings/usagedata.md` says a defaults-only change is never reported.
- **Rollback is the previous release.** Every migration is expand-only; a follower reads empty on an older build,
  and the value it held before is not recovered by upgrading again. The release note asks for a backup first.
- **Mutation runs once per Story, on frozen code**, judged on the Story's own lines.

## Quality gates

- **Mutation** (`mutation/results.md`, gate 80 %, the Story's changed lines, after one kill round each):

  | Story | Backend | Frontend |
  | --- | --- | --- |
  | #6300 | 77.88 % → **95.19 %** (99 / 104) | 67.39 % → **94.57 %** (87 / 92) |
  | #6312 | 48.63 % → **93.99 %** (172 / 183), the connector run with two acceptance suites | **100 %** (17 / 17), no kill round needed |
  | #6304 | 68.78 % → **95.77 %** (181 / 189) | 83.17 % → **93.53 %** (289 / 309) |
  | #6313 | 84.18 % → **95.41 %** (187 / 196) | 73.54 % → **95.08 %** (309 / 325) |

  Every remaining survivor is argued in the results file as equivalent or unreachable. Whole-file backend scores
  stay between 53 % and 73 %, the gap being older lines these Stories did not change.
- **DES integrity.** `des-verify-integrity` reports all 48 steps with complete RED, GREEN and COMMIT traces.
- **Reviews.** Every slice had its own review; the live review of slice 01 on the demo Jira Cloud site read 3 of 3
  named links as dependencies, against 0 before.

## Issues encountered

- **Step 12-03's RED is recorded as FAIL honestly.** Its acceptance test passed as soon as it was un-skipped:
  an estimation bundle equal to the one the owner already reads changes nothing, so the existing rule already held.
  The step shipped as a test-only commit (`7071e36b2`) with no production change. It is complete, not skipped.
- **Step 12-05's RED is recorded as FAIL for a setup reason.** The only Team the CSV demo scenario seeds owns its
  Estimation, so nothing followed the default and the save never asked. The walking skeleton now makes Team Zenith
  follow through the API first, and the page object opens the Estimation section once instead of clicking its
  heading on every poll, which could close it again (`88c0cc168`).
- **A workaround hid behind a structural test.** Handing the Azure DevOps relation-type listing from the Portfolio
  fetch to the parent fetch kept it on the shared connector, wrapped in a record so the test that forbids a kept
  listing no longer saw it. It was reverted (`97db5e03a`): one extra metadata request beats a cached connector
  holding an organisation's listing. The test was then hardened to see through the wrapper (`669ab59a5`).
- **Test hosts opened the product database.** `TestWebApplicationFactory` left the product's own `AddDbContext`
  configuration registered, so every test scope opened `LighthouseAppContext.db` in WAL mode and never closed it.
  Fixed along the way (`48d58955b`).
- **A field still in use was reported as a stale save.** A save refused because a removed field was still referenced
  first told the admin someone else had saved; it now says who uses the field, and a stale copy is refused as stale before its
  removals are judged (`a9566596c`, `f46440f08`).
- **Slice 06's update question had two latent issues**, harmless while only one default with followers existed: the
  count summed followers per changed default, so an owner following two was counted twice, and its body sentence
  fitted only Parent Override. The slice 06 review carried both into slices 08 to 11 (`f843bb7c7`), where the
  question now counts each follower once (`15dc1311e`) and each new default's sentence was sketched first.

## Lessons learned

- **Stryker's incremental mode keeps the first verdict on static mutants** (module-level constants, Zod schemas).
  A spec that kills one does not get it re-tested, so the score after a kill round must come from `--force` or a
  narrowed non-incremental run. Both 6300 and 6313 hit this.
- **Score mutation on the Story's lines, not whole files.** Whole-file scores mixed in older survivors and would
  have hidden or overstated this Epic's own coverage.
- **Demo data decides whether an E2E walking skeleton can see the feature.** Check what the seeded owners already
  own before writing the scenario.
- **A structural invariant earns its keep when it blocks a clever shortcut.** When a test forbids a shape, a
  wrapper that slips past it is a sign to revert, not to loosen the test.
- **Dropping a slice at the sketch review was cheap because it happened before DISTILL.** UI-4 removed slice 07,
  US-06 and one KPI before a scenario was written.

## Finalize checklist

| Item | Answer |
| --- | --- |
| User docs | Done. The connection page, the Team and Portfolio pages, and the Jira and Azure DevOps concept pages (`b5f29576a`). |
| Release notes | Drafted in `release-notes-draft.md` in the workspace, with Steve Pereira credited, to paste in at the next release (`84fc9dcd8`). |
| Usage-data page | One sentence added: a change to the connection's defaults alone is never reported. No new event. |
| Screenshots | Two `@screenshot` tests produce `docs/assets/settings/worktrackingsystems/connection-defaults.png` (the connection's defaults section) and `docs/assets/features/team_estimation_connection_default.png` (a Team following the Estimation default), on the CSV demo connection, the only one the demo data seeds. |
| `ARCHITECTURE.md` | Updated: one default per setting for Teams and for Portfolios, and a saved default queues only the owners whose download moved (`84fc9dcd8`). |
| RBAC | N/A, because nothing new is gated: defaults stay `SystemAdmin`, overrides stay `TeamWrite` / `PortfolioWrite`. |
| Lighthouse-Clients | N/A, because a PUT without the source keeps it (D18), so existing CLI and MCP saves change nothing. |
| Migrations | Expand-only, one per slice from 05a on, through `CreateMigration`. |

## Migrated out of the workspace

Nothing was moved. This project keeps its lasting artifacts under `docs/product/` from DESIGN on, and they are
already there:

- ADR-241 to ADR-244 in `docs/product/architecture/`, all Accepted (2026-10-16), matching the shipped code.
- The Epic's section in `docs/product/architecture/brief.md` ("Application Architecture —
  epic-6303-configure-once-per-connection").
- The journey `docs/product/journeys/epic-6303-configure-once-per-connection.yaml`, the jobs in
  `docs/product/jobs.yaml` and `docs/product/personas/config-admin.yaml`.

The workspace `docs/feature/epic-6303-configure-once-per-connection/` stays in place as delivery history: the
feature delta, the maintainer decisions, the slice briefs, the roadmap and execution log, and the mutation results
and configs. Nothing was deleted.

## ADO

Stories #6300, #6312, #6304 and #6313 under Epic #6303. The Epic's state is the maintainer's to move.

Commits: `9d7086d77..84fc9dcd8`, plus this record.
