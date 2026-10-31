# Every report in one place, created and removed in bulk — Epic 6350

Delivered 2026-10-27 to 2026-10-31 across two code bases: the Lighthouse backend and frontend (slices 01, 02a, 02b, 03,
04, 05 and 07) and `lighthouse-clients` (slice 06). The Reports page, listing and removing in bulk, and the combined PDF
are Community; creating reports in bulk is Premium. No migration, nothing new stored, four new name-only usage events.

## Summary

A top-level **Reports** entry in the header (between Features and System Settings) opens one page listing every report
the reader can read, across Teams and Portfolios. Each row names its owner (a kind icon and the name, announced with
its kind), opens the report, and offers Edit and Delete where the reader is Admin of the owner and a PDF icon for every
reader. A filter matches name, owner and type; Portfolios list first, then Teams, until a column is sorted.

**Create reports** opens the single wizard's steps plus a final "Who gets this report?" step: Teams, then Portfolios,
each with Select all, only the owners the reader is Admin of. Owners whose kept history cannot hold the windows stay
listed, greyed and unpickable, with their history start and a link to their settings. One run carries one window set
(30 / 90 / 180 days or a custom length of at least 14; Then ends the day before Now starts). The run is one request
answered owner by owner; the result dialog says "2 reports created, 1 not created" and gives each owner its own line,
named as it is called, with an icon for Team or Portfolio, with **Open** (a new browser tab, the dialog stays open; in the Standalone app,
which has no tabs, the same window) or the plain reason it was not created.

Selecting rows shows **Remove (n)** and **Export PDF (n)**. Remove asks for confirmation, warns when a Then can no longer
be rebuilt, and answers per report; Remove is held back unless the reader is Admin of every picked owner, and while
permissions are loading or failed. Export PDF prints several reports into one document, in table order, one running
line per report and the footer once. Above 50 picked, both are disabled ("Pick at most 50 reports.").

In the clients, `lh report list` with no owner and the MCP tool `lighthouse_report_list` read the cross-owner list.

## Business context

A consultant and early adopter set up the same Then & Now report per Team by script; leaders want the big picture
across Teams and Portfolios. Job: `job-lead-compare-every-owner-against-its-own-past-in-one-go`. DISCOVER and DIVERGE
were skipped by the maintainer, so the problem rests on that one informant; each slice carried a learning hypothesis
dogfooded on the dev instance, and K6 asks the informant whether the script retired.

## Key decisions

### Scope and product (DISCUSS, 2026-10-27)

- **D1** the same windows for every owner; owners whose history cannot hold them are skipped with the reason and where
  to keep more history. **D13** such owners are shown but not pickable (Q4).
- **D2 / Q1** bulk creation Premium; the page, reading and bulk removal Community. **D3** Teams and Portfolios in one
  selection. **D4** type-agnostic flow, only Then & Now built. **D5** combined PDF in, as the last slice. **D6** clients
  read the cross-owner list; bulk stays web-only.
- **D7** RBAC per owner: Read to list and export, Admin to create and remove, the server re-checks. **D8** a per-owner
  outcome always; an owner gone or no longer administered mid-run is one "Not created / Not removed" row; a lost answer
  says it cannot confirm. **D11** nothing new stored. **D12** the combined PDF is not made if one report cannot be read;
  Retry or Leave it out. **D18** 02a offers only Teams the reader administers, so it releases alone.
- **Each slice ships alone**, in one direction of dependency; scope assessment PASS, 8 stories, ~7¼ days.

### Architecture (DESIGN, `docs/product/architecture/`)

- **ADR-265:** reports across owners are read from one endpoint (`GET api/latest/reports`), filtered on the server by
  Read, each row naming its owner. **ADR-266:** a bulk run is one request, answered owner by owner through the single
  report's path; each owner stands alone (a failed save is caught, its pending change discarded). **ADR-267:** several
  reports print as one document from the same hidden frame, ready only when every report is.
- **DDD-5** Admin is checked per owner when the run reaches it, through a principal-bound capability the controller
  hands in, so the Reports module still never sees a user. **DDD-12** usage events stay browser-sent; the domain-event
  bus was weighed and rejected (consent lives in the browser; no second consumer). **MQ-1 (a)** one bulk request, owners
  one by one. **MQ-2** ceiling N = 50, shared by selection and owners per run, dropping to 25 if a 50-report print fails
  in the Linux desktop app. **MQ-4** `useRbac()` reports a failed load. **MQ-3** slice 04's metric half dropped
  (capture is per owner kind).

### DEVOPS

- **Four name-only events** (DVO-1): `AllReportsListed` 21 (browser; clients later), `ReportBulkCreationStarted` 22,
  `ReportBulkRemovalStarted` 23, `ReportCombinedPdfExportStarted` 24. **D19** the bulk-creation event fires per sent run,
  also when nothing is created. Existing `ReportCreated` / `ReportDeleted` fire per outcome, never for already-gone.
- One `report-bulk-run` log line per run without names; Warning above 20 s create / 10 s remove. No CI change.
  Rollback is the previous release (DVO-9). Clients release follows a Lighthouse release carrying slice 01 (DVO-10).

### Maintainer decisions during DISTILL (sketch walk, NMD-1 to NMD-16)

- **NMD-1 to NMD-4:** the page layout; every row selectable, Export PDF for any selection, Remove only when all are
  removable; filter change clears the selection, sorting keeps it, select-all means every filtered row on every page;
  Create reports always shown (Community disabled with the licence tooltip, Premium without Admin disabled with a reason).
- **NMD-5, NMD-6, NMD-13, NMD-14:** the owner step and presets; "Pick at most 50 Teams and Portfolios."; Now ends today.
- **NMD-7, NMD-8, NMD-12:** result and removal dialogs, the "No PDF was made" dialog, reasons worded word for word.
- **NMD-9, NMD-10:** several windows are several runs; no de-duplication, a second identical report per owner.
- **NMD-11:** the Owner cell shows the name alone (demo Teams are called "Team …"). **NMD-15:** Create reports loads the
  creation options before the wizard opens. **NMD-16:** row icons ship in slice 01.

### Maintainer decisions during DELIVER

- **2026-10-30 (after slice 03):** the could-not-load title, the ceiling placement, the generic duplicate-metric line,
  the owner icons, "Select at least one report." and the removal could-not-confirm sentence stay as built; the page
  lists Portfolios first.
- **Slice 05 review H2:** while permissions are loading or failed, Remove is disabled with "Your permissions couldn't be
  loaded. Reload the page to try again."; Remove does not fall through to the permissive Admin fallback.
- **No nWave scenario tags in code:** nothing reads them; swept from ~266 files and ruled out in `CLAUDE.md`.
- **Slice 07 follow-ups after hands-on review (2026-10-31):** result rows name the owner (first as "Team Zenith"; at finalize the screenshot of demo names showed "Team Team Gravity", so the maintainer chose the bare name with the Owner column's kind icon); Open goes to a
  new tab with the dialog staying open, in place in the Standalone app; the server leaves not-Admin rows unnamed (no
  name leak), so the browser falls back to the name it picked.
- **Clients release deferred (2026-10-31):** `lh report list` / `lighthouse_report_list` ship with the next `/release`.

## Work completed

Rigor per slice: 01, 02b, 03, 04, 06 standard (Sonnet crafter, Haiku reviewer); 02a, 05, 07 thorough (Opus crafter,
Sonnet reviewer). Roadmap: 8 phases, 47 planned steps; phases 01 to 08 are slices 01, 02a, 02b, 03, 04, 05, 06, 07. Each
slice ran refactor, review, mutation, then push with CI watched.

| Slice (ADO Story) | Phase | What it delivered | Pushed |
| --- | --- | --- | --- |
| 01 Every report in one place (#6428) | 01 | Cross-owner list endpoint and the read-only Reports page; Reports nav; owner cell; filter; row icons by `useRbac()`; `AllReportsListed` (21); selection built but hidden | `d28037402..60433ebe8` |
| 02a Create for several Teams (#6429) | 02 | Bulk creation request, Admin-owner set, per-owner result, FK-race handling (`ReportOwnerGoneException`), ceiling of 50, event 22, wizard owner step, E2E walking skeleton | `e0e1dc5ac..59ce96f43`, fixes to `7ed1ce7c7` |
| 02b Owner step says why Create is unavailable (#6430) | 03 | No-Admin state, permissions-failed Retry, owner step looks | `..f9521a42f` |
| 03 Portfolios join the selection (#6431) | 04 | Portfolios in the owner step, presets for mixed selections | `a13fdd6d2..5714fb7a4` |
| 04 Owners that cannot hold the windows (#6432) | 05 | Greyed owners with history start and settings link; Now-only sentence | `..7ff899d92`, then `f9f265600` |
| 05 Remove reports in bulk (#6433) | 06 | Bulk removal (own class), selection, confirm, per-report result, ceiling guard, event 23 | `2af912460..9385b8f85` |
| 06 Every report through the clients (#6434) | 07 | `lh report list`, `lighthouse_report_list`, `AllReportsListed` from CLI and MCP; Stryker added to the clients | clients `e621811..56158e5` |
| 07 Several reports in one PDF (#6435) | 08 | `/print/reports`, combined bundle, "No PDF was made" dialog, one exporter per page, footer once, event 24 | `..dda234574` |

All seven Lighthouse Stories and the clients Story are Closed; the Epic's state is the maintainer's to move.

**Final gates after slice 07, pushed `c0a323b20..c479934d5`:** a feature-wide refactor (`ReportBulkRun.MostPerRun`,
`ownerPathOf`, `UnderWay`); an adversarial review whose fixes landed (owner name sent by the server, bulk removal drops
the single-report cache, duplicate-metric copy, slice categories removed from test names); and the maintainer follow-ups
from hands-on review (owner names with a kind icon in result rows; Open in a new tab).

## Quality gates

| Slice | Mutation, changed lines | Record |
| --- | --- | --- |
| 01 | BE 16 / 16 (100 %); FE 165 / 197 (83.76 %) | `mutation/results-6428.md` |
| 02a | BE 54 / 57 (94.74 %, from 84.21 %); FE 358 / 403 (88.83 %, from 80.65 %) | `results-6429.md` |
| 02b | FE 70 / 75 (93.33 %) | `results-6430.md` |
| 03 | FE 104 / 115 (90.43 %) | `results-6431.md` |
| 04 | FE 113 / 128 (88.28 %) | `results-6432.md` |
| 05 | BE 100 %; FE 87.92 % | `results-6433.md` |
| 06 (clients) | 94.83 % to 97.41 % after two kills | `results-6434.md` |
| 07 | FE 85.28 % to 89.06 % after nine kills; BE enum-only, N/A | `results-6435.md` |
| Final | BE 8 / 8 (100 %); FE 60 / 67 (**89.55 %**, 88.06 % before one kill) | `results-final.md` |

- Every slice's CI ended green through the release gates, Sonar included; the only waiting job is Standalone packaging,
  which is the maintainer's deploy approval. Backend filtered suite 11 759 passed at the last run; frontend 8 858.
- Final run notes: Stryker rolled back every mutant inside `CreateForEach`, `RemoveEach` and the per-owner create method
  as `CompileError`, so those conditions were probed by hand; one (`Count > MostPerRun` to `>=` on removal) survived and
  is now killed. Six frontend survivors left, judged equivalent; none revealed a bug.
- Chromium print check of the combined PDF (throwaway instance): per-report headers and the footer at the page bottom
  of every sheet. The maintainer then verified behaviour by hand.

## Lessons learned

- **A hands-on pass finds what scenarios cannot.** The maintainer's first look at `/reports` found a crash (the page
  lacked its snackbar handler; the test kit wrapped it and hid that); the last pass found that the server never sent
  `ownerName`, so result rows said "Team" alone. Both had green tests.
- **A shared fixture can hide the contract.** The DISTILL kit defaulted most Teams to Admin, so tests did not notice
  owners that were not Admin; per-scenario grants and a wire-shape contract fixed it.
- **Fail closed on a failed permission load.** `useRbac` answers a failed load as Admin everywhere; gating Remove on it
  would have produced an untrue "no longer Admin of" result. The page-load variant is still open (below).
- **Sonar finds what local gates miss, and the local gate should learn it.** S4144, S4043, S6639 and S2004 each got a
  Biome plugin or ledger rule instead of a one-off fix.
- **A green-base rule needs the skipped jobs.** A frontend-only push skipped the backend after a red run because a failed
  run with skipped Sonar counted as green; `.github/scripts/green-base-run.mjs` now requires BE, FE and E2E verify
  success or skipped and Sonar held.

## Issues encountered

- Slice 01: Sonar S4144 (identical scaffold bodies); `useErrorSnackbar` crash on the page.
- Slice 02a: the FK race lands in Freeze, not the save, so a typed `ReportOwnerGoneException` was added; a vacuous
  Escape test and a teardown unhandled rejection (CI 40926989922) were fixed; `ForecastLaneTest` flaked, not ours.
- Slice 03/04: two unreachable scenarios retired after NMD-15; a 04-04 acceptance test lacked a Next press.
- Slice 04/05: Sonar S4043 (sort in an expression) and S6639 (unbounded ids sizing a list) took four CI rounds; the
  final fix was pushed alone from a worktree on `origin/main` and one rebase conflict was resolved by hand.
- Slice 06: a flag without a value (`--team-id`) listed everything; found in review, fixed with a regression test. The
  clients trunk moved mid-push (overlapping slice files and lockfile) and was rebased.
- Slice 07: fixed-position running line and footer stacked across reports on paper, so they moved into repeated table
  groups, then the footer printed once; CI failed an E2E "fits A4" check (header nowrap widened the auto-layout table,
  865 px against 703) and S2004, fixed with a fixed table layout and a flatten.
- Another worktree's vitest load made several frontend runs time out; each passed alone.

## Known gaps and deferred

- The disabled Export / Remove tooltip is not reachable by keyboard (the Remove pattern predates this Epic).
- No Retry after a final refusal; removal reveals whether an id exists (low).
- Clients Epic 6298 scenario tags remain; about 250 backend test files still carry slice ids in their names.
- `useRbac` falls back to permissive when the permission load fails at page load: the page then shows Create and row
  Edit / Delete (the server enforces). Worth its own Bug.
- A 50-report combined PDF on the Linux desktop app is checked by hand only; the ceiling falls to 25 if it fails.
- The `usageDataAsk` E2E is flaky. Standalone packaging for CI 41200239868 awaits the maintainer's approval.
- Decisions to revisit: the combined PDF title "Lighthouse reports"; a failure with one report picked uses the
  single-report snackbar, not the dialog; the footer prints once as a fixed element; rows the server leaves unnamed
  (not-Admin) are named from the picks.
- **Clients release deferred** to the next `/release`; `docs/settings/usagedata.md`'s "Every report was listed" row gains
  its clients part then.

## Finalize checklist

| Item | Answer |
| --- | --- |
| User docs | `docs/reports/reports.md`, `docs/reports/thenandnow.md`, `docs/settings/rbac.md`, `docs/aiintegration.md`; `docs/settings/usagedata.md` rows landed with events 21 to 24 |
| Screenshots | `reports_page.png`, `report_bulk_owners.png`, `report_bulk_result.png` |
| Website | Features table gained "Every report in one place" (Community) and "Create reports in bulk" (Premium); committed in the website repo, held for the maintainer to push |
| Lighthouse-Clients | Slice 06 pushed and CI green; release deferred to `/release` (2026-10-31) |
| RBAC | No new permission: Read to list and export, Admin to create and remove, server re-checks |
| Migrations | None |
| Terminology | Docs and copy use the configured terms (seeded defaults Team, Portfolio) |

## Migrated out of the workspace

Nothing was moved. The workspace has no `design/architecture-design.md`, `component-boundaries.md`,
`technology-stack.md` or `data-models.md`, no `design/adrs/`, no `distill/walking-skeleton.md` and no `discuss/journey-*`
files. The lasting artifacts were written to permanent places from DESIGN on:

- ADR-265, ADR-266 and ADR-267 in `docs/product/architecture/`, and the Epic's section in
  `docs/product/architecture/brief.md` ("Application Architecture — epic-6350-bulk-reports").
- The journey `docs/product/journeys/epic-6350-bulk-reports.yaml`, the job in `docs/product/jobs.yaml`, and the KPI
  contracts `OUT-6350-*` in `docs/product/kpi-contracts.yaml`.

The workspace `docs/feature/epic-6350-bulk-reports/` stays in place as delivery history. Nothing was deleted.

## Links

- Workspace: `docs/feature/epic-6350-bulk-reports/` (`feature-delta.md`, `slices/`, `deliver/roadmap.json`, `mutation/`)
- Precedent: `docs/evolution/2026-10-21-epic-6298-report-pdf-export.md`
- User docs: `docs/reports/reports.md`, `docs/reports/thenandnow.md`

## ADO

Epic #6350. Stories: 01 #6428, 02a #6429, 02b #6430, 03 #6431, 04 #6432, 05 #6433, 06 #6434 (lighthouse-clients),
07 #6435, all Closed. Story #6347 was Removed at DISCUSS and is not depended on.
