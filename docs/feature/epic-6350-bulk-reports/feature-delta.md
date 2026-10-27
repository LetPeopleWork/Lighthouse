# Feature Delta — epic-6350-bulk-reports

ADO Epic #6350 "Every report in one place, created and removed in bulk". DISCOVER and DIVERGE were skipped by the
maintainer; this file starts at DISCUSS.

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-10-27 · **Mode**: autonomous subagent. The maintainer's
decisions of 2026-10-27 are locked (D1–D6 below; Q1–Q4 answered the same day, so D9's one name, D12, D13 (history) and bulk removal's tier are LOCKED too; D9's
no-de-duplication clause and D10 stay PROPOSED); decisions this wave takes on its own carry **PROPOSED — maintainer
confirms** and are listed under "Questions for the maintainer". Config: user-facing, walking skeleton "depends"
(brownfield, evaluated below), UX research lightweight, JTBD yes (every story has a `job_id`), density lean.

| Read | Status |
|---|---|
| DISCOVER / DIVERGE artifacts | ⊘ none — skipped by the maintainer (risk noted in `discuss/wave-decisions.md`) |
| ADO Epic #6350 description (via the dispatch brief) | ✓ — the source of the problem and scope |
| Precedent: `docs/feature/epic-6298-report-pdf-export/feature-delta.md` DISCUSS sections, `slices/`, `discuss/wave-decisions.md` | ✓ — structure mirrored |
| `docs/evolution/2026-10-15-epic-5878-then-and-now.md`, `docs/evolution/2026-10-21-epic-6298-report-pdf-export.md` | ✓ (report concepts, cap, known gaps) |
| `docs/product/jobs.yaml` (tail; `job-share-numbers-outside-lighthouse`, `job-config-admin-know-what-a-setting-limits-before-it-bites`) | ✓ |
| `docs/product/personas/delivery-lead-rte.yaml`, `flow-coach` (by reference) | ✓ |
| Brownfield code (targeted, below) | ✓ |
| `lighthouse-clients` checkout | ⊘ not reachable from this session (outside the project root); tool names confirmed from `docs/aiintegration.md` and Epic 6298's feature delta |

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory (brownfield)

| # | What exists | Where | Consequence |
|---|---|---|---|
| S1 | Reports live per owner: the Reports tab of each Team and Portfolio lists that owner's reports in a `DataGridBase` table with Edit, Delete and PDF row icons (PDF disabled with a tooltip in the macOS desktop app, Bug 6425) | `Lighthouse.Frontend/src/pages/Common/Reports/ReportsTab.tsx`, `ReportList.tsx`, `useOwnerReports.ts` | The global page reuses the same table style and row actions; nothing per owner changes. |
| S2 | One report view for both owner kinds, reached at `/teams/:id/:tab/:reportId` and `/portfolios/:id/:tab/:reportId` | `ReportView.tsx`, `App.tsx` | The global page links to these addresses; no second view. |
| S3 | A top-level page that lists items across owners already exists: `/features` | `App.tsx` (`FeaturesView`) | Precedent for a top-level `/reports` page and its place in the navigation. |
| S4 | The create wizard: Type → Then → Now → Metrics & name, one owner, Type step backed by a type list; the window check mirrors the server's bounds; refusal copy per code | `wizard/ReportWizard.tsx`, `TypeStep.tsx`, `reportTypes.ts`, `thenAndNow/*`, `reportWindowCheck.ts`, `reportRefusalCopy.ts` | Bulk creation reuses the type and configuration steps and adds an owner step and a result; the type list already keeps the flow type-agnostic (D4). |
| S5 | Editing gates go through `useMayEditReports.ts`, built on `useRbac()` | `useMayEditReports.ts` | The owner picker and the bulk-removal selection derive "Admin of this owner" the same way, per owner. |
| S6 | Endpoints are per owner only: `api/latest/{teams,portfolios}/{id}/reports` — GET under `TeamRead`/`PortfolioRead`; POST, PUT, DELETE under `TeamWrite`/`PortfolioWrite` (Admin). No cross-owner list, no bulk create or delete | `TeamReportsController.cs`, `PortfolioReportsController.cs`, `ReportsControllerBase.cs` | Slice 01 needs a cross-owner read (DESIGN: new endpoint vs fan-out). Bulk create/remove: a bulk endpoint vs one call per owner is DESIGN's. |
| S7 | Read-scope filtering across owners exists (`GetReadableTeamIdsAsync`, `GetReadablePortfolioIdsAsync`), used by Features, Portfolios and Suggestions; **no** equivalent for "owners I may write" | `FeaturesController.cs`, `PortfoliosController.cs`, `SuggestionsController.cs` | The global list can filter like `/features`. The Admin set for the picker is new (DESIGN). |
| S8 | Refusals are ProblemDetails with a `code`; window refusals carry `earliestStart`; the cap answers 403. Codes: name-missing, name-too-long, template-not-supported, no-metric-shown, unknown-metric, duplicate-metric, metric-not-captured, then-too-short/-too-long/-ends-in-the-future/-before-cutoff, now-too-short/-too-long/-before-cutoff, report-cap-reached | `ReportsControllerBase.cs`, `Models/Reports/ReportRefusalCodes.cs`, `ReportWindowPolicy.cs` (min window 14 days) | Per-owner "not created" reasons come from these codes; `earliestStart` gives "its kept history starts {date}". |
| S9 | Each owner keeps its own history, "Closed Items Cutoff (days)" in its settings (`DoneItemsCutoffDays`, default 365), with a line saying it bounds how far back Reports reach (Story 6348) | `AdvancedInputs.tsx` | Same windows on many owners hit different cutoffs (D1). The settings line is where "keep more history" points. |
| S10 | Community: 1 report per type per owner; Premium unlimited; a lapsed licence takes nothing away | `ReportCapPolicy.cs` | Bulk create is Premium (D2), so the cap does not bite unless the licence lapses mid-run. |
| S11 | Then frozen at creation (`ReportCommands.Create` → `template.Freeze`), Now computed on read; creation logs `report-creation-slow` above 10 s (measured 0.23–0.60 s for one 365-day Then) | backend Reports module | Bulk over 20–30 owners takes several seconds or more: DESIGN performance question. |
| S12 | `NewReportRequest`: TemplateKey, Name, ShownMetricKeys, ThenAndNow{ThenEndDate, ThenLengthDays, NowLengthDays} | `API/DTO/Reports` | One configuration applies to many owners unchanged. |
| S13 | The print page per report `/print/{teams,portfolios}/:id/reports/:reportId`, mounted beside the app shell, with preparing/failed looks; a hidden print frame for exports | `print/reportPrintRoutes.tsx`, `ReportPrintPage.tsx`, `PrintFrameHost.tsx` | The combined PDF (slice 07) reuses its content per report; a page holding several reports is new. |
| S14 | Usage data: `ReportCreated = 16`, `ReportOpened = 17`, `ReportDeleted = 18`, `WorkTrackingSystemFieldsChanged = 19`, `ReportPdfExportStarted = 20` (last) | `Models/UsageData/UsageDataEventName.cs` | New events append from 21 on `main` (DEVOPS checks). |
| S15 | Clients: `lh report list --team-id/--portfolio-id`, `lh report get`, MCP `lighthouse_{team,portfolio}_report_{list,get}`; read-only, raw values | `docs/aiintegration.md` | The cross-owner list (D6) sits beside these. |
| S16 | The Overview shows each owner's reports as chips (one request per row) | Epic 5878 slice 12 | Unchanged; the global page is the full list. |
| S17 | Docs: `docs/reports/reports.md`, `docs/reports/thenandnow.md`, `docs/settings/rbac.md`, `docs/settings/usagedata.md`, `docs/aiintegration.md` | `docs/` | Finalize targets. |

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `delivery-lead-rte` | **Primary.** Martin Achterberg, Head of Delivery, reads every Team and Portfolio (Viewer); takes one PDF to the steering group each quarter. |
| `flow-coach` (consulting) | **Primary, creates.** Elena Kovacs, external flow coach, Admin of Teams Zenith, Gravity, Equinox, Voyager, Lightspeed and Portfolio Ocean Explorer; Viewer elsewhere. Sets up the comparisons, on a Premium instance. |
| `flow-coach` (internal) | **Secondary.** Priya Raman, Admin of Team Gravity only; a Community instance in one example. |
| `flow-coach` (internal) | Tomás Ruiz, a fellow Admin of Team Gravity on Elena's Premium instance (bulk-removal race). |
| *(no access)* | Jonas Weber, cannot read Team Pulsar — the RBAC boundary in examples. |

Example data is the demo set (Teams Zenith, Voyager, Pulsar, Gravity, Lightspeed, Meridian, Equinox; Portfolios
Apollo, Orion, Ocean Explorer, NeuroLink City, Altobelli). "Today" in examples is 27 Oct 2026; quarter against
quarter: Then 1 May – 29 Jul 2026, Now 30 Jul – 27 Oct 2026 (90 days each, back to back); month against month: Then
29 Aug – 27 Sep, Now 28 Sep – 27 Oct 2026 (30 days each, back to back). Now always ends today; Then is chosen to end the
day before Now starts. Team Equinox keeps 90 days (kept history starts 30 Jul 2026).

## Wave: DISCUSS / [REF] JTBD One-Liner

- `job-lead-compare-every-owner-against-its-own-past-in-one-go` (**new**, `docs/product/jobs.yaml`): when leadership
  asks whether flow got better across several Teams and Portfolios, get the same before-and-after comparison for each,
  set up once and read side by side, without configuring each owner or running an own script. Every story carries it.
- Carried, not served anew: `job-flow-coach-show-whether-flow-changed` (each report created) and
  `job-share-numbers-outside-lighthouse` (slice 07's PDF).

---

## Wave: DISCUSS / [REF] Changed Assumptions

| Was (source) | Now | Why |
|---|---|---|
| Reports are found per owner, on its Reports tab or the Overview chips (Epic 5878) | Also on one top-level Reports page (slice 01) | ADO #6350 scope. |
| Creation is one owner at a time, all-or-nothing (Epic 5878) | Bulk creation is per owner: each owner is created or not, with a reason; never a silent all-or-nothing over the set (D1, D8) | Same windows hit different kept histories. Each owner's own creation stays all-or-nothing. |
| "Several reports at once" out of scope (Epic 6298 Out of Scope) | In, as the last slice (D5) | Maintainer 2026-10-27. |
| Clients read reports per owner only (Epic 6298 D23) | Plus a read-only cross-owner list (D6) | Maintainer 2026-10-27. |
| Story 6347 would keep the single wizard's windows inside the kept history and say which length to pick | **Removed in ADO on 2026-10-27; not depended on.** Slice 04 solves it for the bulk owner step only; the single wizard keeps today's behaviour (window check + refusal) | Maintainer 2026-10-27. |

---

## Wave: DISCUSS / [REF] Locked Decisions

> **Revised after peer review 2026-10-27** (`nw-product-owner-reviewer`, rejected pending revisions): selection rules
> unified and moved into slice 01 (B1); D8 carried into bulk removal (B2); per-owner refusals mid-run and RBAC/auth off
> made explicit (B3); metrics an owner does not capture (H4); licence lapse and button states (H5); slice 02 split into
> 02a and 02b, slice 06 re-estimated at ~1d (H6); the owner step's loading and failure looks (H7); selection under
> filter, select-all and a ceiling (H8); nits 9–16. New behaviour choices carry **PROPOSED — confirm at DISTILL sketch
> walk** and are listed as Q6–Q10.

D1–D6 restate the maintainer's decisions of 2026-10-27 (locked). D7 onward are this wave's.

- [D1] **Same windows for every selected owner**, so Teams compare like with like. Owners whose kept history cannot
  hold the windows are skipped; the result says why per owner ("Not created: its kept history starts 30 Jul 2026")
  and where to keep more history (Closed Items Cutoff in that owner's settings). The wizard may limit picks to what the
  selection can hold where it can (slice 04). Story 6347 is Removed and not depended on. (Locked.)
- [D2] **Licence**: bulk creation is **Premium**; the Reports page and opening reports are **Community**. (Locked.)
  Bulk removal: **Community — LOCKED (maintainer, 2026-10-27; Q1 (a))**: removing is harmless and a Community owner
  holds at most one Then & Now.
- [D3] **Teams and Portfolios in one selection.** (Locked.) Delivered as slices 02a/02b (Teams) and 03 (Portfolios
  join the same list), so each slice stays ≤ 1 day; the selection is one list once 03 ships.
- [D4] **Type-agnostic flow**: choose report type → configure once → pick owners → per-owner result. Only Then & Now
  exists and only it is built; the type step shows even with one type, as the single wizard does. (Locked.)
- [D5] **Bulk PDF is in, as the last slice (07)**: select reports on the Reports page → one PDF, each report on its own
  page(s), reusing Epic 6298's print content. (Locked.)
- [D6] **Clients**: a read-only cross-owner list beside the per-owner reads; bulk create and remove stay web-only.
  Needs a clients release (changesets, manual `pnpm release:version`, the `lighthouse` skill names the new tool and
  command — drift test). (Locked.)
- [D7] **Who may do what**: the Reports page lists reports on owners the user can read; the owner picker offers only
  owners where the user is Admin (`TeamWrite` / `PortfolioWrite`, from slice 02a); bulk removal acts only on reports
  on such owners (D15); the server re-checks each owner. Auth off or RBAC off: everyone is Admin everywhere, so every
  owner is offered and every report is removable. UI gating via `useRbac()` only.
- [D8] **Per-owner outcome, always**, for creation and removal: every selected owner (or report) appears in the result
  as created/removed or not, with a plain reason. An owner that was deleted, or that the caller no longer administers,
  when the run reaches it, is one "Not created" / "Not removed" row, never a whole-run failure. If the request as a
  whole gets no answer, the result says it cannot confirm what was created (or removed) and sends the user to the
  Reports page — never an implied "nothing happened".
- [D9] **One name for every owner — LOCKED (maintainer, 2026-10-27; Q2 (a))**: the user gives one name (default
  follows Then, as today: "Then & Now — 90 days to 29 Jul 2026"); each report carries it, and the owner is shown beside
  it everywhere it is listed. **No de-duplication — PROPOSED — confirm at DISTILL sketch walk (Q11)**, never asked of
  the maintainer: running the same bulk creation again creates second copies.
- [D10] **Several windows = several runs — PROPOSED — confirm at DISTILL sketch walk (Q5)**: defaulted by the main
  session; the maintainer did not answer it. A 90-day and a 30-day copy are
  two bulk runs; the Reports page shows both.
- [D11] **Nothing new is stored**: bulk creation writes the same Reports rows as the single wizard; no "batch" record.
  DESIGN confirms (no EF migration expected).
- [D12] **Combined PDF order and failure — LOCKED (maintainer, 2026-10-27; Q3 (a))**: reports print in the Reports
  page's current order; if any selected report cannot be read, no PDF is made; the page names the report(s) and offers
  Retry or "Leave it out".
- [D13] **Owners that cannot take the report are shown but not pickable — LOCKED for history (maintainer, 2026-10-27;
  Q4 (a))** in slice 04, with the owner's history start and a link to its settings; going back to shorter windows
  makes them pickable. **Extended to metrics — PROPOSED — confirm at DISTILL sketch walk (Q7)**: the Metrics step offers
  every metric; an owner that does not capture a chosen metric is shown but not pickable in the owner step, with the
  reason. The result still words `metric-not-captured` as a safety net.
- [D14] **Usage data is wanted** (DEVOPS designs): Reports page opened; a bulk creation run; a bulk removal run; a
  PDF holding several reports. Name-only preferred; at most a closed enum; never counts, names, ids or values.
- [D15] **Selection rules — PROPOSED — confirm at DISTILL sketch walk (Q6)**: on the Reports page every listed row is
  selectable (slice 01). Export PDF is enabled for any selection (read is enough). Remove is enabled only when every
  selected report is on an owner the user is Admin of; otherwise it stays disabled with a reason naming how many
  selected reports cannot be removed ("2 of the 5 selected reports are on a Team or Portfolio you are not Admin of").
- [D16] **Selection under filter, sort and pages — PROPOSED — confirm at DISTILL sketch walk (Q8)**: changing a filter
  clears the selection; sorting keeps it; select-all selects every row the current filter shows, on all pages; the
  remove confirm and the PDF name the exact count. A selection larger than the ceiling DESIGN sets (N) is refused with
  "Pick at most N reports".
- [D17] **"Create reports" states — PROPOSED — confirm at DISTILL sketch walk (Q9)**: always shown on the Reports page.
  On Community: disabled with a Premium hint (the existing licence-tooltip pattern, consistent with D2). On Premium for
  a user who administers no owner: disabled with "You are not Admin of any Team or Portfolio" (slice 02b). While the
  Admin set loads: disabled; if it cannot be loaded: the owner step shows could-not-load with Retry.
- [D18] **Slice 02a's owner step offers only the Teams the user administers** (every Team with RBAC or auth off), so
  02a is releasable on its own (re-review, 2026-10-27; settles Q10). 02b adds only the no-Admin state (D17) and the
  owner step's loading and could-not-load looks. A Team that stops being administered mid-run is still one "Not
  created" row (D8).
- [D19] **The bulk-run usage event fires for every run, including one that created nothing, and carries no outcome
  property — PROPOSED for DEVOPS**: K2 counts users who try bulk creation, not runs that succeed.

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — 8 stories (01, 02a, 02b, 03–07), 2 code bases (Lighthouse frontend + backend Reports module;
`lighthouse-clients`), ~7¼ days** (rev. 2026-10-27 after review and re-review: was ~6; 01 +½d for row selection, 02
split into 02a ~1d and 02b ¼d, 06 ~1d).
No new bounded context. The walking skeleton touches 3 integration points (cross-owner read, new page + navigation,
existing report view). Under 2 weeks. Four user outcomes (find, create, remove, one PDF) ship as slices of one Epic
because the maintainer framed them as one; each slice adds value alone and the Epic is releasable after any finished
slice.

---

## Wave: DISCUSS / [REF] Journey

SSOT: `docs/product/journeys/epic-6350-bulk-reports.yaml` (two journeys; shared artifacts and their sources there).

```
[Reports page + selection] ──> [Create reports: type → Then → Now → metrics+name → owners] ──> [Per-owner result] ──> [Remove selected] ──> [Export selected as one PDF]
 Burdened→Oriented              Hopeful→Focused                                                Anxious→Confident     Cautious→Relieved    Ready→Confident
 readable_owners, report_name   report_windows, admin_owners, licence_tier, owner_history_start refusal_reason        admin_owners         report_values, generated_at
```

Emotional arc **burdened → focused → confident**. The peak tension is the result: "which ones did it make?" — D8
answers it per owner. Integration checkpoints: the page lists what the per-owner tabs list; every created report
carries the same windows; each report in the combined PDF equals its single-report PDF read at the same moment.

---

## Wave: DISCUSS / [REF] Story Map & Slices

**Backbone**: *find and select reports* → *create for many* → *understand each owner's outcome* → *tidy up* → *share
the big picture*; beside it, *an assistant lists every report*.

| Find & select | Create for many | Each owner's outcome | Tidy up | Share | Assistant |
|---|---|---|---|---|---|
| **01 Reports page + selection** | 02a write path + Admin owners · 02b no-Admin and loading looks · 03 + Portfolios | 02a result · 04 owners that cannot take the report | 05 bulk removal | 07 one PDF | 06 cross-owner list |

**Walking skeleton = slice 01** (brownfield): a top-level Reports page listing every readable report across owners,
with row selection, opening the existing view. Read-only, Community, no new write path.

**How the consultant's "big picture" is reached**: 01 shows every report in one place → 02a–03 create the quarter's
and the month's comparison for every Team and Portfolio in two runs → 07 puts the chosen ones into one PDF for
leadership. K4 (several reports in one PDF) shows it working; K6 asks whether the script retired.

| # | Slice | Tier | Est. | Depends on | Learning hypothesis — disproves … if it fails |
|---|---|---|---|---|---|
| 01 | Every report in one place: top-level Reports page, all readable reports across Teams and Portfolios, opens each; row selection (filter clears it, select-all = filtered rows, exact count); loading, failure, empty looks | Community | ~1.5d | — | "One list ends per-owner hunting" — if on the dev instance (`:5169`) the maintainer still goes through a Team's tab to find a known report, or the page takes > 2 s |
| 02a | Create a Then & Now for several Teams at once — the write path: type → windows + metrics + name once → pick from the Teams the user administers (every Team with RBAC/auth off) → per-Team result, incl. refusals mid-run, licence lapse, an all-refused run and a lost answer; server Premium and Admin checks | Premium | ~1d | 01 | "Same windows for many Teams is what leads want" — if after a run over the dev instance's Teams the maintainer edits most created reports, or more than half are skipped |
| 02b | The owner step says why Create is unavailable: the no-Admin state and the owner step's loading and could-not-load looks | Premium | ¼d | 02a | "Saying why beats an empty step" — if a dogfood user without Admin rights, or on a throttled connection, cannot tell why there is nothing to pick |
| 03 | Portfolios join the same selection | Premium | ½d | 02b | "Leads compare Teams and Portfolios on one setup" — if no dogfood run picks a Portfolio together with Teams |
| 04 | The owner step shows owners that cannot take the report (history too short; a chosen metric not captured), with the reason and where to fix it | Premium | 1d | 02b, 03 | "Saying it before Create beats reasons after" — if dogfood runs still end with skipped owners the user did not expect |
| 05 | Remove the selected reports (Admin owners only), with confirm and per-report result, incl. a lost answer | Community | 1d | 01 | "Bulk removal is needed once bulk creation exists" — if after a month no dogfood or opted-in run uses it |
| 06 | Every report through the clients: `lh report list` with no owner and an MCP tool (stdio and HTTP), skill drift test, changesets, clients release | Community | ~1d | 01 (released) | "Assistants want the whole list, not per owner" — if in the `lighthouse` skill eval the assistant still lists owner by owner |
| 07 | Several reports in one PDF: export the selection, each report on its own page(s) | Community | 1d | 01 | "One combined PDF replaces the script for leadership" — if the maintainer, given the dev instance's 90-day reports, would rather send separate PDFs or a spreadsheet |

Briefs: `slices/slice-NN-*.md` (02a's brief is `slice-02-create-for-several-teams.md`).

### Priority Rationale

- **01** first: walking skeleton, Community, every later slice builds on its page and selection; no write risk.
- **02a** next: the Epic's core pain (setup per Team) and the riskiest assumption (same windows across owners, D1); it
  ships the write path with every per-owner refusal so the result is honest from day one.
- **02b** adds the no-Admin state and the owner step's loading and could-not-load looks; small, and 02a is already
  releasable without it.
- **03** completes D3 cheaply on the same flow.
- **04** reduces skipped owners; after 03 so it covers both kinds at once.
- **05** tidies what 02a–04 made; needs only 01's selection.
- **06** is in another repo and needs 01's cross-owner read released; before 07 because D5 fixes the PDF as last.
- **07** last (D5): the payoff for leadership, built on a page full of comparable reports.

### Slice taste tests

| Test | Verdict |
|---|---|
| 4+ new components in one slice? | Pass after the split: 02a has the bulk write path and the result (the type and configuration steps are reused, S4); 02a also the Admin-set filter; 02b the no-Admin state and owner-step looks. 01 carries list + selection (~1.5d, the only slice above 1d — the selection is the grid's own checkbox column). |
| Every slice depends on a new abstraction? | Pass — the cross-owner read and the selection are born in 01 and reused by 05, 06, 07; the type-agnostic bulk flow (D4) is born in 02a. |
| Disproves a pre-commitment? | Pass — 01 tests "one list", 02a tests D1, 04 tests D13, 05 tests D2's removal half, 07 tests D5. |
| Synthetic-data-only slices? | Pass — every slice is dogfooded on the dev instance (`:5169`, real history, many owners); demo data covers E2E and screenshots. |
| Two slices alike except scale? | Pass. |

---

## Wave: DISCUSS / [REF] User Stories

<!-- markdownlint-disable MD024 -->

System constraints (all stories): RBAC per D7, UI gating via `useRbac()` only, never fetching
`/api/latest/authorization/my-summary` · works with auth off and RBAC off (everyone is Admin everywhere) · every label
through Terminology (seeded {Team}, {Portfolio}, {Cycle Time}, …) · no new stored data (D11) · each created report is
exactly what the single wizard would create with the same inputs · per-owner outcome always (D8) · every view that
fetches has a loading look and a failure look, never stale rows or an empty look passed off as "none" · meaning never
by colour alone.

### US-01 — Every report in one place (slice 01, walking skeleton)

`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go` · persona `delivery-lead-rte` (Martin)

**Problem**: Martin reads seven Teams and five Portfolios. To find this quarter's Then & Now for Team Gravity he must
remember it lives on Gravity's Reports tab; to see which Teams have one at all he opens twelve tabs.

#### Elevator Pitch

Before: reports are found only on each {Team}'s or {Portfolio}'s Reports tab.
After: Martin chooses **Reports** in the navigation → sees one table: "Then & Now — 90 days to 29 Jul 2026 · Then &
Now · Team Gravity · 27 Oct 2026" and the rest, across every owner he can read → clicks a row → the report opens; he
can also filter to one name and tick "select all" → "9 selected".
Decision enabled: which Teams and Portfolios still lack a comparison, and which report to open, without visiting each.

**Examples**: (1) Martin reads 7 Teams and 5 Portfolios holding 9 reports; all 9 are listed with their owner and he
opens Team Gravity's. (2) Jonas cannot read Team Pulsar; Pulsar's report is not in his list. (3) A fresh instance has
no reports; the page says reports are created on a {Team}'s or {Portfolio}'s Reports tab. (4) The list cannot be
loaded on a dropped connection; the page says so with Retry, not "no reports". (5) The instance calls Teams "Squads";
the owner column reads "Squad Gravity". (6) Martin filters to "90 days to 29 Jul 2026" (9 of 14 rows, over two pages),
selects all: "9 selected"; he changes the filter and the selection clears.

```gherkin
Scenario: A lead sees every report he can read and opens one
  Given Martin can read 7 Teams and 5 Portfolios that hold 9 reports between them
  When Martin opens Reports from the navigation
  Then he sees all 9 reports with their name, type, owner and created day
  And choosing "Then & Now — 90 days to 29 Jul 2026" of Team Gravity opens that report

Scenario: Reports on owners he cannot read stay hidden
  Given Team Pulsar has a report and Jonas cannot read Team Pulsar
  When Jonas opens Reports
  Then Team Pulsar's report is not listed

Scenario: An instance without reports says where they are made
  Given no Team or Portfolio has a report
  When Martin opens Reports
  Then he is told that reports are created on a Team's or Portfolio's Reports tab

Scenario: A list that could not be loaded is not mistaken for no reports
  Given the reports cannot be loaded for Martin
  When Martin opens Reports
  Then he sees that the reports couldn't be loaded, with Retry
  And he is not told there are no reports

Scenario: Owners carry the instance's terms
  Given the instance calls a Team a "Squad"
  When Martin opens Reports
  Then Team Gravity's report is listed under "Squad Gravity"

# D16 — PROPOSED, confirm at DISTILL sketch walk; checkboxes and count visible once slice 05 or 07 ships
Scenario: Select-all takes exactly the reports the filter shows
  Given 14 reports are listed and 9 of them are named "Then & Now — 90 days to 29 Jul 2026", over two pages
  When Martin filters to that name and selects all
  Then the page says "9 selected"
  And when he changes the filter, nothing is selected
```

**AC**: AC-1.1 A top-level Reports entry in the navigation opens the page (address DESIGN's, e.g. `/reports`) for
every signed-in user. AC-1.2 The table lists every report on every owner the user can read — name, Type, owner (kind
in the instance's term + name), created day — in the existing `DataGridBase` style with its sorting and filtering.
AC-1.3 Choosing a row opens the existing report view at its owner address. AC-1.4 Reports on owners the user cannot
read are absent; the server filters, not the browser. AC-1.5 While loading: a loading look; on failure: could-not-load
with Retry; with no reports: an empty look pointing to the owners' Reports tabs; never stale rows shown as current.
AC-1.6 A report of a type this version cannot show is listed and opens to the existing "cannot be shown" look.
AC-1.7 Community; auth off lists all. AC-1.8 The usage event of D14 for opening the page, listed in
`docs/settings/usagedata.md`. AC-1.9 Every listed row is selectable (D15) and the page knows the exact count selected;
slice 01 builds the selection but shows no checkboxes and no "N selected" until the first slice with an action on it
(05 or 07) ships. AC-1.10 Changing a filter clears the selection; sorting keeps it; select-all selects every row the
current filter shows, on all pages (D16, PROPOSED); visible, like AC-1.9, from slice 05 or 07.
**KPI**: K1, G2. **Tech**: cross-owner read (S6, S7) — new endpoint or fan-out is DESIGN's; owner names in the payload.

### US-02a — Create a Then & Now for several Teams at once (slice 02a)

`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go` · persona `flow-coach` (Elena)

**Problem**: Elena coaches four Teams at a client. For the quarterly review she runs the Then & Now wizard four times
with the same dates, retyping them each time; once she mistyped one Team's end date and the comparison was not like
with like.

#### Elevator Pitch

Before: one wizard per Team, the same dates typed again each time.
After: Reports → **Create reports** → Type: Then & Now → Then ends 29 Jul 2026, 90 days; Now 90 days; all metrics;
name "Then & Now — 90 days to 29 Jul 2026" → picks from her Admin Teams Zenith, Gravity, Equinox → **Create 3
reports** → sees "2 reports created, 1 not created": Zenith Created, Gravity Created, Equinox "Not created: its kept
history starts 30 Jul 2026. Keep more history under Closed Items Cutoff in its settings."
Decision enabled: which Teams can join the comparison as set, and whether to keep more history for Equinox.

**Examples**: (1) Elena creates the quarter's comparison for Zenith, Gravity and Equinox; two are created, Equinox is
skipped with its history start. (2) Elena is Viewer on Team Pulsar; Pulsar is not offered. With RBAC or auth off
every Team is offered. (3) Priya's instance is Community; Create reports is shown disabled with the Premium hint; the
Team's own Create Report still works. (4) The connection drops while creating; Elena is told it could not be confirmed
which reports were made and to check Reports. (5) During a run over Zenith, Gravity, Voyager and Lightspeed, Voyager is
deleted, Elena loses Admin of Gravity and the licence lapses while Lightspeed already holds a Then & Now: Zenith is
created, the other three are "Not created" with their reasons. (6) Elena picks Equinox and Voyager, whose kept
histories both start after 1 May 2026: "0 reports created, 2 not created". (7) The same afternoon she runs it with
30-day windows ending 27 Sep 2026; each Team holds both. Running the quarter again would add second copies (D9,
PROPOSED).

```gherkin
Scenario: A coach creates the same comparison for several Teams in one go
  Given Elena is Admin of Teams Zenith and Gravity on a Premium instance
  When Elena creates "Then & Now — 90 days to 29 Jul 2026" with Then 1 May – 29 Jul 2026 and Now 90 days for both
  Then each Team holds that report with the same windows and metrics
  And the result says "2 reports created"

Scenario: A Team whose kept history is too short is skipped with the reason
  Given Team Equinox keeps finished work only since 30 Jul 2026
  When Elena creates the quarter's comparison for Zenith, Gravity and Equinox
  Then Zenith and Gravity are created
  And Equinox reads "Not created: its kept history starts 30 Jul 2026" with where to keep more history

Scenario Outline: The owner step offers the Teams she may create on
  Given the instance runs with RBAC <rbac>
  And Elena is Admin of Team Gravity and Viewer on Team Pulsar
  When Elena reaches the owner step
  Then Team Gravity is offered and Team Pulsar is <pulsar>
  And creating for every offered Team passes the server's check
  Examples:
    | rbac | pulsar      |
    | on   | not offered |
    | off  | offered     |

# D17 — PROPOSED, confirm at DISTILL sketch walk
Scenario: Community shows bulk creation as a Premium capability
  Given Priya's instance has a Community licence
  When Priya opens Reports
  Then Create reports is shown disabled with a Premium hint

Scenario: A lost connection never reads as nothing created
  Given Elena chose Create 3 reports
  When the answer does not arrive
  Then she is told it could not be confirmed which reports were created and to check Reports

Scenario: Changes during the run are refused owner by owner
  Given Elena picked Teams Zenith, Gravity, Voyager and Lightspeed, and Lightspeed already holds a Then & Now
  And during the run Team Voyager is deleted, Elena stops being Admin of Team Gravity and the Premium licence lapses
  When the run finishes
  Then Zenith is created
  And Gravity, Voyager and Lightspeed are each listed as not created, with the Admin, deleted and licence reasons

Scenario: A run where every owner is refused says so plainly
  Given Teams Equinox and Voyager both keep finished work only since 30 Jul 2026
  When Elena creates the quarter's comparison for both
  Then the result reads "0 reports created, 2 not created"
  And each row gives its reason and where to keep more history
```

**AC**: AC-2.1 Create reports on the Reports page, Premium only; on Community shown disabled with a Premium hint (D2,
D17 PROPOSED). AC-2.2 Steps: Type → Then → Now → Metrics & name (the single wizard's steps and rules, once) → owners →
result (D4). AC-2.3 The owner step offers only the Teams the user administers, derived via `useRbac()`; with RBAC or
auth off, every Team (D7, D18). AC-2.4 Every created report equals what the single wizard creates with the same
inputs; one name for all (D9). AC-2.5 The result lists every picked owner: Created (opens it) or Not created with a
plain reason per refusal code (S8): cutoff reasons name the history start and point to Closed Items Cutoff (D1);
`metric-not-captured` reads "Not created: {Team} Zenith does not capture {Work Item Age}"; not Admin; owner deleted;
licence (cap). AC-2.6 A whole-request failure says it cannot confirm what was created and links to Reports (D8).
AC-2.7 The server checks each owner when it reaches it: an owner the caller does not (or no longer) administer, or one
deleted mid-run, becomes one "Not created" row with its reason, never a whole-run failure; a run from a non-Premium
instance is refused as a whole with the licence reason. AC-2.8 A licence that lapses mid-run: owners past the
Community cap are "Not created" with the licence reason, the rest are created. AC-2.9 Duplicates are allowed: no
de-duplication by name or windows (D9's clause, PROPOSED — confirm at DISTILL sketch walk). AC-2.10 Usage: the D14
bulk-run event once per run, and `ReportCreated` once per report created, as today; the run event fires also for a run
that created nothing and carries no outcome property (D19, PROPOSED for DEVOPS). AC-2.11 When every picked owner is
refused, the headline reads "0 reports created, {N} not created" and every row carries its reason; never an empty or
success-looking result.
**KPI**: K2, K3, G1, G4. **Tech**: bulk endpoint vs one call per owner, progress and the 10 s slow log (S11) are
DESIGN's; the Admin set per user is new (S7).

### US-02b — The owner step says why Create is unavailable (slice 02b)

`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go` · persona `delivery-lead-rte` (Martin) and
`flow-coach` (Elena)

**Problem**: after 02a, Martin, who administers nothing, finds Create reports leads to an owner step with nothing in
it; and on a slow connection Elena's owner step looks empty while the Admin set is still on its way.

#### Elevator Pitch

Before: an empty owner step reads as "no Teams", whether the user administers none or the list is still loading.
After: Martin, Admin of nothing, sees Create reports disabled with "You are not Admin of any Team or Portfolio"; Elena's
owner step shows a loading look and, when the owners cannot be loaded, "couldn't be loaded" with Retry, Create
unavailable meanwhile.
Decision enabled: whether to ask for Admin rights, wait, or retry, instead of guessing.

**Examples**: (1) Martin, Viewer everywhere on a Premium instance, sees Create reports disabled with the reason.
(2) The Admin set is still loading for Elena: the step shows it is loading and Create is not available. (3) The Admin
set cannot be loaded: could-not-load with Retry; Retry succeeds and her Admin Teams appear.

```gherkin
# D17 — PROPOSED, confirm at DISTILL sketch walk
Scenario: A Viewer on a Premium instance is told why he cannot create
  Given Martin administers no Team or Portfolio on a Premium instance
  When Martin opens Reports
  Then Create reports is shown disabled with "You are not Admin of any Team or Portfolio"

Scenario: The owner step shows it is still loading
  Given the Teams Elena administers are still on their way
  When Elena reaches the owner step
  Then she sees they are loading
  And Create is not available

Scenario: The owner step does not offer Create when it could not load the owners
  Given the Teams Elena administers cannot be loaded
  When Elena reaches the owner step
  Then she sees that they couldn't be loaded, with Retry
  And Create is not available
```

**AC**: AC-2b.1 No Admin owner on Premium: Create reports disabled with "You are not Admin of any Team or Portfolio"
(D17, PROPOSED). AC-2b.2 While the Admin set loads, the owner step shows a loading look and Create is not available.
AC-2b.3 If it cannot be loaded, could-not-load with Retry; never an empty list read as "no Teams".
**KPI**: G1. **Tech**: the looks reuse the existing loading and could-not-load patterns (DESIGN).

### US-03 — Portfolios in the same selection (slice 03)

`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go` · persona `flow-coach` (Elena)

**Problem**: Elena also reports on Portfolio Ocean Explorer; after 02b she still runs the single wizard for it, with
the same dates.

#### Elevator Pitch

Before: bulk creation covers Teams; the Portfolio needs its own wizard run.
After: Create reports → … → the owner step lists {Teams} and {Portfolios} → picks Zenith, Gravity and Portfolio Ocean
Explorer → "3 reports created", the Portfolio's row reading "Portfolio Ocean Explorer · Created".
Decision enabled: show Teams and their Portfolio on one set of windows.

**Examples**: (1) Zenith, Gravity and Ocean Explorer in one run, all created. (2) Elena is Viewer on Portfolio
Apollo; it is not offered. (3) Only Portfolios picked (Ocean Explorer, NeuroLink City); both created. (4) The window
presets offer 30, 90 and 180 days whatever the mix (PROPOSED, settled in the DISTILL sketch walk).

```gherkin
Scenario: Teams and a Portfolio get the same comparison in one run
  Given Elena is Admin of Teams Zenith and Gravity and Portfolio Ocean Explorer
  When Elena creates the quarter's comparison for all three
  Then all three hold it and the result names "Portfolio Ocean Explorer" as created

Scenario: Only Portfolios she administers can be picked
  Given Elena is Viewer on Portfolio Apollo
  When Elena picks owners for new reports
  Then Portfolio Apollo is not offered

Scenario: A run of Portfolios only
  Given Elena is Admin of Portfolios Ocean Explorer and NeuroLink City
  When Elena creates the quarter's comparison for both
  Then both hold it
```

**AC**: AC-3.1 The owner step lists Teams and Portfolios in one selection, grouped by kind, in the instance's terms
(D3). AC-3.2 Portfolio admin rights as D7, AC-2.3 and AC-2b.1–2b.3 apply. AC-3.3 Presets cover the mix (PROPOSED: 30 / 90 / 180
and any number of days ≥ 14). AC-3.4 Result rows name the owner kind; every AC-2.5 reason applies to Portfolios,
including "Not created: {Portfolio} Ocean Explorer does not capture {Work Item Age}". **KPI**: K2. **Tech**: same path
as 02a.

### US-04 — The owner step says which owners cannot take the report (slice 04)

`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go` · persona `flow-coach` (Elena)

**Problem**: in 02a Elena learns only after Create that Equinox cannot hold the quarter; she would rather see it while
picking and decide then (shorter windows, or keep more history first).

#### Elevator Pitch

Before: owners that cannot hold the windows, or do not capture a chosen metric, are found out in the result.
After: on the owner step Team Equinox shows unpickable with "Kept history starts 30 Jul 2026 — Then starts 1 May 2026"
and a link to its settings; the button reads "Create 2 reports".
Decision enabled: shorten the windows, keep more history first, or go ahead without Equinox.

**Examples**: (1) Equinox shown unpickable with its history start. (2) Elena goes back and picks 30-day windows
ending 27 Sep 2026; Equinox becomes pickable. (3) Equinox's history start cannot be read; it stays pickable and 02a's
result covers it. (4) Elena opens Equinox's settings from the link and raises Closed Items Cutoff; Equinox becomes
pickable only after its next update has brought the older Work Items back. (5) A Portfolio that does not capture a
chosen metric shows "Does not capture {Work Item Age}" and cannot be picked (PROPOSED, Q7).

```gherkin
Scenario: An owner that cannot hold the windows is shown but cannot be picked
  Given Team Equinox keeps finished work only since 30 Jul 2026
  When Elena reaches the owner step with Then 1 May – 29 Jul 2026
  Then Team Equinox cannot be picked and shows "Kept history starts 30 Jul 2026"
  And it links to where Equinox keeps more history

Scenario: Shorter windows make it pickable
  Given Team Equinox cannot be picked for the quarter's comparison
  When Elena goes back and chooses Then 29 Aug – 27 Sep 2026
  Then Team Equinox can be picked

Scenario: An unknown history start falls back to the result
  Given Team Equinox's kept history cannot be read
  When Elena reaches the owner step
  Then Team Equinox can be picked and its outcome shows in the result

# D13 extension — PROPOSED, confirm at DISTILL sketch walk
Scenario: An owner that does not capture a chosen metric cannot be picked
  Given Portfolio Ocean Explorer does not capture Work Item Age
  When Elena chose Work Item Age and reaches the owner step
  Then Portfolio Ocean Explorer cannot be picked and says it does not capture Work Item Age

Scenario: Create waits for the owners' limits
  Given the owners' kept-history starts are still loading
  When Elena reaches the owner step
  Then Create is not available until they have loaded or failed

Scenario: Limits that could not be loaded leave the owners pickable and say so
  Given the owners' kept-history starts and captured metrics cannot be loaded
  When Elena reaches the owner step
  Then every Team she administers can be picked
  And the step says their limits could not be checked
  And any owner that cannot take the report is listed as not created in the result
```

**AC**: AC-4.1 On the owner step each owner whose kept history cannot hold Then or Now is shown but not pickable, with
its history start and a link to its settings (D13, LOCKED). AC-4.2 Each owner that does not capture a chosen metric
is shown but not pickable, with the reason (D13 extension, PROPOSED). AC-4.3 The Create count excludes them.
AC-4.4 Going back and changing windows or metrics re-evaluates. AC-4.5 Unknown start → pickable; the server still
decides (02a). AC-4.6 Uses the same bounds as the server (as `reportWindowCheck.ts` does today). AC-4.7 While the
owners' limits load, Create is not available; if they cannot be loaded, the owners stay pickable and the step says
their limits could not be checked. **KPI**: K3. **Tech**: each owner's earliest kept day and captured metrics must
reach the browser (DESIGN).

### US-05 — Remove reports in bulk (slice 05)

`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go` · persona `flow-coach` (Elena)

**Problem**: after a trial run Elena holds a dozen reports she does not need, spread over five owners; deleting them
means opening each owner's tab and confirming one at a time.

#### Elevator Pitch

Before: one Delete and one confirm per report, per owner tab.
After: Reports → selects three "Then & Now — 30 days to 27 Sep 2026" rows → **Remove** → "Remove 3 reports from Team
Zenith, Team Gravity and Portfolio Ocean Explorer? Then on 2 of them can no longer be rebuilt." → Remove → "3 reports
removed".
Decision enabled: clear out trial or outdated comparisons in one go, knowing which cannot be recreated.

**Examples**: (1) Elena removes three trial reports in one go. (2) Elena also selects Team Pulsar's report (Viewer):
Remove stays disabled with "1 of the 4 selected reports is on a Team or Portfolio you are not Admin of" (PROPOSED,
Q6). (3) The confirm warns that 2 Then windows can no longer be rebuilt. (4) Tomás Ruiz, a fellow Admin of Team
Gravity, removed one of them a minute earlier; it counts as removed. (5) Elena lost Admin of Gravity meanwhile;
Gravity's report is "Not removed: you are no longer Admin of Team Gravity". (6) The answer never arrives: "could not
confirm what was removed". (7) RBAC off: every report is removable.

```gherkin
Scenario: A coach removes several reports in one go
  Given Elena is Admin of Teams Zenith and Gravity and Portfolio Ocean Explorer, each holding a trial report
  When Elena selects the three trial reports, chooses Remove and confirms
  Then the three reports are gone from Reports and from each owner's Reports tab

# D15 — PROPOSED, confirm at DISTILL sketch walk
Scenario: A mixed selection cannot be removed, and says why
  Given Elena selected 3 reports on Teams she administers and 1 on Team Pulsar, where she is Viewer
  When Elena looks at Remove
  Then it is disabled with "1 of the 4 selected reports is on a Team or Portfolio you are not Admin of"

Scenario: The confirmation warns which Then windows cannot be rebuilt
  Given 2 of the 3 selected reports have a Then that starts before their owner's kept history
  When Elena chooses Remove
  Then the confirmation names 3 reports and says Then on 2 of them can no longer be rebuilt

Scenario: A report already removed by someone else counts as removed
  Given Tomás removed Team Gravity's trial report after Elena selected it
  When Elena confirms the removal
  Then the result says 3 reports removed

Scenario: Losing Admin meanwhile is said per report
  Given Elena lost Admin of Team Gravity after selecting its report
  When Elena confirms the removal
  Then Gravity's report is listed as not removed with that reason

Scenario: A lost answer never reads as nothing removed
  Given Elena confirmed removing 3 reports
  When the answer does not arrive
  Then she is told it could not be confirmed what was removed and to check Reports

Scenario: With RBAC off every report can be removed
  Given the instance runs with RBAC off
  When Elena selects any reports
  Then Remove is enabled and the server's check passes
```

**AC**: AC-5.1 Remove acts on 01's selection; it is enabled only when every selected report is on an owner where the
user is Admin; otherwise disabled with a reason naming how many cannot be removed (D15, PROPOSED). A user who
administers nothing therefore never has Remove enabled. AC-5.2 Remove asks to confirm, naming the exact count and
which owners, and, when it applies, the existing "can no longer be rebuilt" warning with a count. AC-5.3 Result per
report (D8); already gone counts as removed. AC-5.4 The server re-checks Admin per report; a report whose owner the
caller no longer administers, or that was deleted with its owner, is one "Not removed" / "removed" row, never a
whole-run failure. AC-5.5 Licence: Community (D2, LOCKED). AC-5.6 `ReportDeleted` keeps firing per report; D14's run
event once. AC-5.7 A whole-request failure says it cannot confirm what was removed and links to Reports (D8, as
AC-2.6). AC-5.8 Auth off or RBAC off: every report removable. **KPI**: G1. **Tech**: bulk endpoint or per-report
calls (DESIGN). AC-5.9 Removal shares the PDF's ceiling N (DESIGN sets N): a selection above it is refused with "Pick
at most N reports" (D16, PROPOSED).

### US-06 — Every report through the clients (slice 06)

`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go` · persona `delivery-lead-rte` (Martin)

**Problem**: Martin asks his assistant "which of my Teams have a Then & Now?"; today it must list every Team and
Portfolio, then each one's reports — a dozen calls, and it often stops early.

#### Elevator Pitch

Before: `lh report list --team-id 1`, once per owner.
After: `lh report list` with no owner → one table of every report Martin can read, e.g. a row "ID 9 · Then & Now — 90
days to 29 Jul 2026 · Then & Now · Team Gravity · 2026-10-27"; `--json` gives report id, owner kind, owner id and name
per report, ready for `lh report get`. The MCP tool (name DESIGN's) returns the same.
Decision enabled: the assistant answers across all owners and reads the right report next.

**Examples**: (1) Martin's assistant lists 9 reports in one call. (2) Jonas's list omits Team Pulsar's. (3) Against a
Lighthouse older than this Epic: "upgrade Lighthouse". (4) The per-owner `list` and `get` keep working unchanged.

```gherkin
Scenario: The command line lists every report without naming an owner
  Given Martin can read owners holding 9 reports
  When Martin runs lh report list with no owner
  Then lh prints the 9 reports, each with its id and owner

Scenario: An assistant lists every readable report in one call
  Given Martin can read owners holding 9 reports
  When his assistant lists every report through MCP
  Then it receives 9 reports, each with its owner kind, owner id and owner name

Scenario: An older Lighthouse is named as the reason
  Given Martin's Lighthouse predates the cross-owner list
  When Martin runs lh report list with no owner
  Then he is told to upgrade Lighthouse
```

**AC**: AC-6.1 `lh report list` with no `--team-id` / `--portfolio-id` lists every readable report (the contract); an
MCP tool returns the same over stdio and HTTP, name DESIGN's (D6). AC-6.2 Uses slice 01's read with the caller's
credential; filtering is the server's. AC-6.3 Relays as the existing report tools (no access, upgrade). AC-6.4 The
`lighthouse` skill names the new tool and command (drift test). AC-6.5 Changesets; manual `pnpm release:version`,
commit and push before the clients release. AC-6.6 Usage data per the clients' consent-gated practice (DEVOPS; a list
reports nothing today). **KPI**: K5.

### US-07 — Several reports in one PDF (slice 07)

`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go` · persona `delivery-lead-rte` (Martin)

**Problem**: for the steering group Martin wants every Team's quarter comparison in one document. With Epic 6298 he
exports nine PDFs one by one and merges them in another tool.

#### Elevator Pitch

Before: one Export PDF per report, then merging files by hand.
After: Reports → filters to "Then & Now — 90 days to 29 Jul 2026" → selects all ("9 selected") → **Export PDF (9)** →
Save as PDF → one file: Team Gravity's report on its own page(s) with its own header ("Team Gravity", windows,
"Produced 27 Oct 2026, 14:05"), then Team Zenith's, … each ending with the Lighthouse footer.
Decision enabled: hand leadership the whole group's comparison as one file.

**Examples**: (1) Martin exports 9 reports into one PDF in the table's order. (2) Martin is Viewer on every owner; he
can still export all 9. (3) Team Voyager's report cannot be read; no PDF is made; the page names Voyager's report with
Retry and "Leave it out". (4) In the macOS desktop app the action is disabled with the same reason as single export
(Bug 6425). (5) Martin selects more reports than the ceiling N (for example 50): "Pick at most N reports" (PROPOSED). (6) One
report selected: the same PDF the single export makes.

```gherkin
Scenario: A lead hands the steering group every Team's comparison in one PDF
  Given Martin selected 9 reports on Reports
  When Martin chooses Export PDF and saves the file
  Then the PDF holds the 9 reports in the table's order, each starting on a new page with its own header

Scenario: A Viewer exports reports on owners he only reads
  Given Martin is Viewer on Teams Gravity and Zenith and selected their reports
  When Martin chooses Export PDF
  Then the PDF holds both reports

Scenario: Each report reads as its own PDF would
  Given Team Gravity's report is among the selected
  When Martin exports the selection
  Then Gravity's pages show the same values as Gravity's own PDF read at the same moment

Scenario: No PDF passes a missing report off as complete
  Given Team Voyager's report cannot be read
  When Martin exports a selection including it
  Then no PDF is made and the page names Voyager's report with Retry and Leave it out

Scenario: The macOS desktop app explains why it cannot print
  Given Martin uses the Lighthouse desktop app on macOS
  When Martin selects reports
  Then Export PDF is disabled with the reason given for a single report

# D16 — PROPOSED, confirm at DISTILL sketch walk
Scenario: Too many reports are refused before preparing
  Given the ceiling is N reports and Martin selected more
  When Martin looks at Export PDF
  Then he is told "Pick at most N reports"
```

**AC**: AC-7.1 Export PDF acts on 01's selection, for any reader, whatever the user administers (D15; Community).
AC-7.2 One print document; each report starts on a new page with Epic 6298's header, body and footer, its values read
for this export (D5). AC-7.3 Order = the table's current order (D12, LOCKED). AC-7.4 Any unreadable report → no PDF;
named, with Retry and Leave it out (D12, LOCKED). AC-7.5 A selection above the ceiling DESIGN sets is refused with
"Pick at most N reports"; the action names the exact count (D16, PROPOSED). AC-7.6 macOS desktop: disabled with Bug
6425's reason (on `main`); slice 07 adds no macOS handling of its own. AC-7.7 Preparing and failed looks as the single print page.
AC-7.8 Usage per D14. **KPI**: K4, K6. **Tech**: a print page holding several reports reusing `ReportPrintContent`
(S13); N is DESIGN's.

---

## Wave: DISCUSS / [REF] Outcome KPIs

Objective: leads see and share every Team's and Portfolio's comparison without setting each up by hand. **North
star: K2.** Targets are first guesses; DEVOPS turns "Measured by" into usage-data design (per-browser proxies, as in
Epics 5878 and 6298).

| # | Who | Does what | Target | Baseline | Measured by | Type |
|---|---|---|---|---|---|---|
| K1 | Opted-in browsers that open ≥ 1 report in 90 days | open the Reports page | ≥ 40% | 0 | page-opened event ÷ browsers with `ReportOpened` | Leading |
| K2 | Opted-in Premium browsers that create reports in 90 days | create through bulk at least once | ≥ 30% | 0 | bulk-run event ÷ browsers with `ReportCreated` (Premium) | Leading (north star) |
| K3 | The maintainer, dogfooding on the dev instance | sets up a Then & Now for 10 owners | ≤ 2 min | ~15 min (10 wizard runs) | timed dogfood, slices 02a–04 | Outcome |
| K4 | Bulk creators | export a PDF holding several reports within 90 days | ≥ 20% | 0 | multi-report PDF event ÷ bulk-run browsers | Leading |
| K5 | Instances using the clients | list every report through MCP or `lh` | ≥ 3 client ids in 90 days | 0 | clients' practice (DEVOPS) | Leading |
| K6 | The informant who prompted the Epic | stops running his own script for leaders | yes | script in use | maintainer asks after slice 07 ships | Qualitative |

Guardrails: G1 no report is created or removed on an owner where the caller is not Admin (acceptance test); G2 the
Reports page is ready ≤ 2 s on the dev instance; G3 the single wizard and the per-owner Reports tab behave as before
(existing tests); G4 every picked owner appears in the result (acceptance test).

Hypothesis: we believe creating the same comparison for many owners at once, and reading them in one place and one
PDF, will replace per-Team setup and own scripts. We will know when ≥ 30% of Premium report creators use bulk (K2)
and ≥ 20% of those export several reports in one PDF (K4).

---

## Wave: DISCUSS / [REF] Definition of Done

1. Every UAT scenario passes as an automated acceptance test (the combined PDF via print-media emulation; the dialog
   is checked by hand).
2. Unit and component tests green; `pnpm test`; `dotnet test` (filtered per CLAUDE.md); clients `pnpm ci` for 06.
3. `pnpm build` and `dotnet build` with zero warnings; Biome clean; no new SonarCloud issues.
4. RBAC per D7; the server re-checks every owner; UI gating via `useRbac()` only.
5. Copy through Terminology; never "Epic", "Story", "Initiative" for product concepts.
6. One E2E walking skeleton (Page Object, demo data, Premium licence fixture): bulk-create on two demo Teams, both
   listed on Reports, one opens. No more E2E than that.
7. Dogfood evidence per slice on the dev instance (`:5169`): Reports page timing (G2); a bulk run over its Teams with
   the result; for 07 a combined PDF printed from Chrome and the Linux desktop app.
8. Mutation testing ≥ 80% on the feature's code, run last on frozen code (both stacks).
9. At feature finalization: docs, screenshots, website, usage-data entries and the clients release per the Checklist.

---

## Wave: DISCUSS / [REF] Out of Scope

Bulk editing of existing reports · one report spanning several Teams · new report types (Signals) · scheduled
delivery (#5882) · several windows in one bulk run (D10, PROPOSED) · per-owner names in one run (D9) · de-duplicating reports
(D9) · bulk create or remove through the clients (D6) · changing the single wizard's window limits (Story 6347,
Removed) · a summary endpoint for the Overview chips · creating reports for owners the user does not administer.

---

## Wave: DISCUSS / [REF] WS Strategy, Driving Ports, Pre-requisites

**Walking skeleton**: brownfield, slice 01 — navigation → Reports page → cross-owner read → open the existing report
view, with row selection; demo data for E2E, dev instance for dogfood.

**Driving ports**: the Reports navigation entry and page with its selection; Create reports (owner step, result); the
selection actions Remove and Export PDF; the cross-owner read and the bulk create/remove entry points DESIGN names;
`lh report list` and the MCP tool (06).

**DESIGN must answer**:

- Slow connection (project rule): the Reports page's loading and could-not-load looks; **the owner step's loading and
  could-not-load looks** (Admin set, kept-history starts, captured metrics — Create unavailable meanwhile, AC-2b.3,
  AC-4.7); the bulk result while creation or removal runs (progress, or a lone spinner) and when the answer never
  arrives (D8); the combined print page's preparing and failed looks (reuse Epic 6298's). Keyed queries so a late
  answer never lands on a newer view.
- The cross-owner read: one endpoint (filtered with `GetReadable*IdsAsync`) or fan-out; payload with owner names.
- The Admin set per user (no `GetWritable*` exists, S7) for the picker and the Remove rule, and how `useRbac()`
  exposes it.
- Bulk create/remove: one endpoint per action with per-owner outcomes vs one call per owner; per-owner checks at the
  moment each owner is reached (AC-2.7); Premium enforced on the server; licence lapse mid-run (AC-2.8); time budget
  for 20–30 owners against the 10 s slow log (S11).
- Each owner's earliest kept day and captured metrics for slice 04.
- Type-agnostic flow (D4): how a report type plugs its configuration into the bulk flow (`reportTypes.ts`).
- Combined PDF: a print page for several reports, page breaks between them, the ceiling N, which bulk removal shares
  (AC-5.9).
- Clients: the MCP tool name; `lh report list` with no owner is the contract.

**DISTILL must sketch with the maintainer first** (reuse `DataGridBase`, the `ReportList` columns and row icons): the
Reports page (navigation entry, columns, selection column and count — shown from slice 05 or 07, empty / loading /
failure looks, where Create reports, Remove and Export PDF sit, their disabled states and reasons — D15, D16, D17);
the owner step (grouping by kind, unpickable owners with history start or metric reason and link, the no-Admin state,
loading and could-not-load, the Create count); the presets for mixed selections; the result list wording for each
refusal, including the all-refused headline; the bulk-removal confirm and its could-not-confirm look; the combined-PDF
failure page (Retry / Leave it out) and the shared ceiling message; and the two open product choices D10 (several
windows, several runs) and D9's no-de-duplication clause.

**The sketch walk is a hard gate for every PROPOSED item.** DISTILL writes no scenario that pins a PROPOSED behaviour
(D9's clause, D10, D13's metric extension, D15–D17, the presets) until the maintainer has confirmed or changed it in
the walk; the outcome is recorded as a maintainer-decision block in this file.

**Pre-requisites**: Reports and Then & Now (Epic 5878), the print route (Epic 6298) on `main`; Bug 6425's macOS
disabling (on `main`, AC-7.6). Story 6347 is Removed and not needed. None
block slice 01.

**NFRs**: Reports page ready ≤ 2 s on the dev instance; keyboard-operable selection and steps; never colour alone;
the bulk result readable by a screen reader as a list; no new stored data.

---

## Wave: DISCUSS / [REF] Project DISCUSS Checklist

No silent N/A — every item answered.

| Item | Answer |
|---|---|
| **RBAC impact** | **Applies.** Reports page: reports on owners the user can read (`TeamRead`/`PortfolioRead`), filtered on the server; every listed row selectable. Bulk create: only owners with `TeamWrite`/`PortfolioWrite` offered (02a), checked per owner on the server when reached. Bulk removal: enabled only when every selected report is on such an owner, checked per report. Combined PDF and clients list: read only. No new permission. UI gating via `useRbac()`; an "Admin set" helper is new (DESIGN). Auth/RBAC off: everyone may do everything. `docs/settings/rbac.md` gains rows for the page, bulk create, bulk remove and the combined PDF at finalize. |
| **Lighthouse-Clients CLI/MCP** | **Slice 06 applies** (D6): `lh report list` with no owner and an MCP tool (stdio and HTTP); changesets for `lighthouse-client`, `lighthouse-mcp-core`, `lighthouse-cli` (minor); adapters by dependency bump; manual `pnpm release:version`, commit and push before the clients release; the `lighthouse` skill names the new tool and command (drift test); the `lighthouse-reports` skill may mention the list (skill-only, no changeset). **Slices 01–05 and 07: N/A, because** bulk create/remove stay web-only and the PDF is a browser print. |
| **Website / marketing surface** | **Owed at finalize**: features list "Every report in one place; create a Then & Now for many Teams and Portfolios at once (Premium); several reports in one PDF"; the Premium list on the pricing page gains bulk creation. The website hot-links `docs/assets` from `@main`. Copy confirmed with the maintainer first. |
| **Docs + screenshots** | **Owed at finalize**: `docs/reports/reports.md` (Reports page and selection, bulk create with per-owner outcomes and the history-start reason, bulk remove, combined PDF), `docs/reports/thenandnow.md` (same windows for many owners, Closed Items Cutoff), `docs/settings/rbac.md`, `docs/settings/usagedata.md` (each new event in the slice that emits it), `docs/aiintegration.md` (06). `@screenshot` one per theme: the Reports page, the owner step, the result with a skipped owner. Docs wait for the maintainer's confirmation. |
| **Demo data** | **No change planned.** Reports are not seeded; the E2E bulk-creates on two demo Teams with the Premium licence fixture (gitignored, copy into the worktree). Whether a demo Team's history gives a natural skipped owner for the screenshot is checked at finalize. |
| **Terminology** | **No new key.** "Reports", "Create reports", "Remove", "Export PDF" are product words. The owner column, the owner step's groups, the disabled reasons and the result rows use {Team}/{Teams}, {Portfolio}/{Portfolios}; metric reasons use the metric's term. |
| **Usage-data event** | **Wanted (D14), designed in DEVOPS**, appended after `ReportPdfExportStarted = 20`: Reports page opened (name-only, slice 01); bulk creation run (name-only, slice 02a; fires for every run, including one that created nothing, with no outcome property — D19, PROPOSED for DEVOPS, since K2 counts users who try bulk creation; `ReportCreated` keeps counting each report — DEVOPS decides whether it gains a closed `origin` {Wizard, Bulk}); bulk removal run (name-only, slice 05); a PDF of several reports (a new name-only event or a closed `scope` {One, Several} on `ReportPdfExportStarted`, slice 07). A count bucket is not proposed: it carries a count. Clients list: per the clients' practice (06). Never names, ids, counts or values. |
| **EF migrations** | **N/A, because** nothing new is stored (D11); DESIGN confirms. |
| **Premium gating** | **Applies**: bulk creation is Premium (D2), refused on the server too; on Community the button is shown disabled with a Premium hint (D17, PROPOSED). A licence that lapses mid-run: owners past the Community cap are "Not created" with the licence reason (AC-2.8). Reading, the Reports page, selection, the combined PDF, bulk removal (LOCKED) and the clients list are Community. |
| **ADO** | Epic #6350 exists. Proposed, one Story per slice: 01 "Every report in one place", 02a "Create a Then & Now for several Teams at once", 02b "Bulk creation says why Create is unavailable", 03 "Portfolios join bulk report creation", 04 "Bulk creation says which owners cannot take the report", 05 "Remove reports in bulk", 06 "List every report through the Lighthouse clients", 07 "Several reports in one PDF". Story 6347 stays Removed. Creating Stories is the maintainer's call; the Epic's state is the maintainer's. Not touched by this wave. |
| **Release notes** | The maintainer's. Proposed lead: the pain (one wizard per Team, reports scattered over tabs, own scripts for the big picture), then the win (one page, one setup for every Team, one PDF for leadership). Clients CHANGELOG through changesets for 06. |

---

## Wave: DISCUSS / [REF] DoR Validation

(rev. 2026-10-27 after peer review)

| # | DoR item | Status | Evidence |
|---|---|---|---|
| 1 | Problem clear, domain language | PASS — clear, not validated | Each story opens with Martin's or Elena's situation; the source is one informant (DISCOVER skipped by the maintainer). |
| 2 | Persona specific | PASS | Martin (Head of Delivery, Viewer on 12 owners), Elena (coach, Admin of 4 Teams + 1 Portfolio, Premium), Priya (Admin of one Team, Community), Tomás (fellow Admin of Gravity), Jonas (no read on Pulsar). |
| 3 | 3+ domain examples, real data | PASS | 4–7 per story with demo owners, contiguous windows (Then 1 May – 29 Jul, Now 30 Jul – 27 Oct 2026; Then 29 Aug – 27 Sep, Now 28 Sep – 27 Oct 2026), history start 30 Jul 2026. |
| 4 | UAT G/W/T, 3–7 | PASS | US-01 6, US-02a 7 (one an outline, RBAC on/off), US-02b 3, US-03 3, US-04 6, US-05 7, US-06 3, US-07 6; each has a boundary or failure path. |
| 5 | AC from UAT | PASS | AC-n.m per story trace to scenarios and D-decisions. |
| 6 | Right-sized | PASS, one at the edge | 01 ~1.5d (list + the grid's selection column, shown from 05/07; demonstrable in one session), 02a ~1d (write path + Admin-set filter), 02b ¼d, 03 ½d, 04 1d, 05 1d, 06 ~1d, 07 1d; ~7¼d total. US-02a and US-05 at the 7-scenario cap. |
| 7 | Technical notes | PASS | Inventory S1–S17; NFRs; "DESIGN must answer" incl. the owner step's looks. |
| 8 | Dependencies tracked | PASS | 02a on 01; 02b on 02a; 03 on 02b; 04 on 02b and 03; 05, 06, 07 on 01 only; one direction. Q1–Q4 answered 2026-10-27; Q10 settled by the re-review; Q5–Q9 and Q11 open with defaults, gated by the DISTILL sketch walk. |
| 9 | Outcome KPIs | PASS | K1–K6 with targets, baselines and measurement; G1–G4. |

Elevator Pitch check: all eight stories carry Before / After / Decision enabled; no `@infrastructure` story; every
story has `job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go`.

**DoR status: 9 of 9 PASS** (item 1 "clear, not validated" on one informant; item 6 with 01 at ~1.5d). D2's removal
half, D9's one name, D12 and D13 (history) are LOCKED by the maintainer (2026-10-27); D18 is settled by the re-review.
PROPOSED, gated by the DISTILL sketch walk: D9's no-de-duplication clause, D10, D13's metric extension, D15–D17 and
the presets for mixed selections; D19 is PROPOSED for DEVOPS. None blocks DESIGN.

---

## Wave: DISCUSS / [REF] Questions for the maintainer

*Q1–Q4 answered on 2026-10-27 — see "Maintainer answers" below; the options are kept for the record. Q5 and Q11 are
open (never answered); Q6–Q9 were added after the peer review; Q10 was settled by the re-review. Each open question
carries a default marked **PROPOSED — confirm at DISTILL sketch walk**.*

1. **Q1 — Licence for bulk removal.** (a) Community; (b) Premium, like bulk creation. **Recommendation: (a)** —
   removing is harmless, and a Community owner holds at most one Then & Now, so gating it protects nothing.
2. **Q2 — Names across owners.** (a) One name for every owner, the owner shown beside it everywhere (default follows
   Then); (b) a name with the owner put in, e.g. "Then & Now — 90 days to 29 Jul 2026 — Team Gravity"; (c) edit each
   owner's name in the owner step. **Recommendation: (a)** — every list and the PDF header already show the owner;
   (b) duplicates it and gets long; (c) undoes the "set up once" win.
3. **Q3 — Combined PDF when one report cannot be read.** (a) No PDF; name the report(s), Retry or Leave it out;
   (b) print the rest with a page listing what is missing. **Recommendation: (a)** — a file that reads complete but
   is not is what Epic 6298 refused to make.
4. **Q4 — Owners that cannot hold the windows (slice 04).** (a) Shown, not pickable, with history start and settings
   link; (b) pickable with a warning, then skipped in the result; (c) drop slice 04 and rely on 02a's result.
   **Recommendation: (a)** — the user decides before Create; D1 still holds on the server.
5. **Q5 — Several windows in one run** (the 90-day and 30-day copies). *Open — defaulted by the main session, not
   answered by the maintainer.* (a) Out: two runs (D10); (b) in, as a further slice after 07. **Default: (a)** — a
   second run costs a minute, and the flow stays type-agnostic.
6. **Q6 — Selection rules (D15).** (a) Every row selectable; Export PDF for any selection; Remove only when every
   selected report is removable, else disabled with "{n} of the {m} selected reports are on a Team or Portfolio you are
   not Admin of"; (b) Remove acts on the removable ones and lists the rest as not removed; (c) non-removable rows
   cannot be selected (breaks the PDF for Viewers). **Default: (a).**
7. **Q7 — Metrics an owner does not capture (D13 extension).** (a) Metrics step offers every metric; such owners shown
   but not pickable with the reason; (b) metrics limited to what every picked owner captures; (c) create without that
   metric for those owners. **Default: (a)** — same pattern as Q4, and (c) breaks "same comparison for everyone".
8. **Q8 — Selection under filter and the ceiling (D16).** (a) Changing a filter clears the selection; select-all
   takes the filtered rows on all pages; above N, "Pick at most N reports"; (b) the selection survives filter changes
   (hidden selected rows). **Default: (a)** — nothing selected is ever out of sight.
9. **Q9 — "Create reports" states (D17).** (a) Always shown; Community: disabled with a Premium hint; Premium without
   any Admin owner: disabled with "You are not Admin of any Team or Portfolio"; (b) hidden in both cases. **Default:
   (a)** — it matches the existing licence tooltip on Create Report and tells people the capability exists.
10. **Q10 — Slice 02a's owner list (D18).** *Settled by the re-review (2026-10-27)*: 02a offers only the Teams the
    user administers, so it is releasable alone; the former default (every readable Team until 02b) is withdrawn.
11. **Q11 — Running the same bulk creation twice (D9's clause).** (a) No de-duplication: second copies are created;
    (b) skip owners that already hold a report with the same name and windows, saying so per owner; (c) ask before
    creating duplicates. **Default: (a)** — simplest, and the single wizard allows duplicates today.

Non-blocking: the preset choice for mixed selections (30 / 90 / 180) is settled in the DISTILL sketch walk.

**The DISTILL sketch walk is a hard gate for every PROPOSED item** (Q5–Q9, Q11, the presets): no scenario that pins
one of them is written until the maintainer has confirmed or changed it there. D19 is PROPOSED for DEVOPS instead.

## Wave: DISCUSS / [REF] Maintainer answers (2026-10-27)

These settle Q1–Q4; every PROPOSED default they touch is now LOCKED.

| Q | Answer |
|---|---|
| Q1 Bulk removal licence | **(a) Community** (D2's removal half). |
| Q2 Names across owners | **(a) One name for every owner**, the owner shown beside it everywhere (D9). |
| Q3 Combined PDF when a report cannot be read | **(a) No PDF**; the page names that report and offers Retry or "Leave it out" (D12). |
| Q4 Owners that cannot hold the windows | **(a) Shown but not pickable**, with their history start and a settings link (D13). |

Not answered, so still **PROPOSED — confirm at DISTILL sketch walk** (corrected after the re-review, 2026-10-27): Q5
(several windows in one run, D10) was defaulted by the main session, not answered by the maintainer; D9's
no-de-duplication clause (Q11) was never asked.

---

## Wave: DISCUSS / [REF] Handoff

To DESIGN (`nw-solution-architect`): this file, `slices/`, `discuss/wave-decisions.md`, the journey SSOT
`docs/product/journeys/epic-6350-bulk-reports.yaml`, the job in `docs/product/jobs.yaml`. Open for DESIGN: the list
under "DESIGN must answer"; the usage events (and D19) to DEVOPS; the sketches and every PROPOSED item (Q5–Q9, Q11,
the presets) to the DISTILL sketch walk, which is a **hard gate**: DISTILL pins none of them in a scenario until the
maintainer has confirmed or changed it there. Re-review (2026-10-27): conditionally approved; the conditions are
applied in this file.
