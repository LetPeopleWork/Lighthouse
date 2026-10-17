# Feature Delta — epic-6298-report-pdf-export

**ADO**: Epic #6298 *Report PDF Export* (tag `Premium`, state Planned). Predecessors on the board: #1822 *Project
Reports* (2025-03-02, Removed) and #4753 *Export to PDF* (2026-04-08, Premium, Removed 2026-08-21 as superseded by
Reports). Related: Epic #5878 *Then & Now* (Released), #6299 Custom Metric Reports, #5935 Signals, #5882 Scheduled
Report Delivery, Spike #6052 (Closed).

**Waves**: DISCOVER (2026-10-17). A deliberately light round, at the maintainer's request: one maintainer interview
about the past behaviour of their users, two removed backlog items the maintainer wrote (#1822, #4753), the Epic 5878 discovery and the #6052 spike.
Lighthouse ran no direct customer interviews this round.

**One line**: any report can leave Lighthouse as a PDF, so the numbers reach people who will not or cannot open
Lighthouse. The PDF is a capability of a report, not a report type. The first payload is Then & Now.

**Density**: `lean`, Tier-1 `[REF]` only. `evidence_standard: past_behavior`. Each claim is graded as
**observed behaviour**, **second-hand report** (of past behaviour), **stated intent**, or **desk/codebase fact**.

---

## Wave: DISCOVER / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `flow-coach` | **Primary, per engagement.** Captures numbers for a client hand-over or sponsor update. The same persona that froze a Then & Now (Epic 5878). |
| `delivery-lead-rte`, `product-owner` | **Primary, recurring.** Team lead or PM who takes numbers out on a regular cadence (status update, steering slide). |
| *(any of the above)* | **Ad hoc.** One-off captures. The maintainer reported all three cadences on 2026-10-17: coach/consultant per engagement, team lead/PM on a recurring cadence, and ad-hoc one-offs. |
| *(candidate)* `report-reader` | **Not created.** The person the PDF is for: either *could* open Lighthouse but will not (does not know the tool, unsure how to use it), or *cannot* (no connection; the data lives in a Standalone install). Split out in DISCUSS only if the reader's needs shape the PDF beyond "readable without Lighthouse". |

---

## Wave: DISCOVER / [REF] Opportunity statement

The outcome: the numbers someone already reads in Lighthouse reach a reader outside it, in a form that reads well
without the tool and says when it was taken, with less effort than a screenshot or a rebuilt spreadsheet.

| # | Opportunity (customer-outcome form) | Source | Grade |
|---|---|---|---|
| O1 | Minimise the time it takes to get Lighthouse numbers into a slide or document for someone who will not open Lighthouse | #1822 ("reference customers ... manually export screenshots"); maintainer 2026-10-17 (screenshots into slides) | second-hand report of past behaviour; one informant (the maintainer) across a backlog record and the interview |
| O2 | Minimise the likelihood that a reader without access (Standalone, no connection) sees no numbers at all | maintainer 2026-10-17 | second-hand report |
| O3 | Minimise the effort of rebuilding the same numbers elsewhere (CSV into a spreadsheet, MCP into an AI-made dashboard) | #1822 (CSV of forecasts); maintainer 2026-10-17 | second-hand report of past behaviour |
| O4 | Minimise the likelihood that a shared number is read as current when it is not | project rule (no value the reader cannot tell is stale) | desk |
| O5 | Minimise the effort of handing data over for a Flow Analysis, optionally without names | #4753 | stated intent |

The opportunity algorithm cannot be applied: there are no importance or satisfaction ratings (see G2).

**Key gap, not resolved here.** What people capture today is **metrics charts and forecasts/Deliveries**. The
first planned payload, **Then & Now**, has **no charts** and is not captured by anyone yet (it is new). So the
first slice may not serve the job that was observed. DIVERGE decides (Open question 3).

**Competing alternative.** Workaround (3), the MCP connection plus an AI that generates a dashboard, is a live
substitute for a PDF, not just a workaround to replace. It already lets a user compose what they want to show.

---

## Wave: DISCOVER / [REF] Validated assumptions

| # | Assumption | Evidence | Confidence |
|---|---|---|---|
| V1 | Numbers leave Lighthouse today through workarounds that cost effort. | One informant, the maintainer: two dated backlog records the maintainer wrote and the interview. #1822 (2025-03) relays reference customers exporting screenshots by hand, the only customer-originated signal. #4753 (2026-04) names people sharing charts in reports or slides. Interview 2026-10-17: last real cases were screenshots into slides, CSV export rebuilt elsewhere, MCP plus AI dashboard. | **Low-Medium**. Past behaviour, but one informant; frequency and effort not measured. |
| V2 | Many readers are outside Lighthouse, by choice or by necessity. | Maintainer 2026-10-17: mixed. Some could open it but will not; others have no connection (Standalone). Epic 5878 discovery: the audience is management sponsors. | **Medium** |
| V3 | Capture happens on three cadences (per engagement, recurring, ad hoc). | Maintainer 2026-10-17. | **Low-Medium**. No split by frequency. |
| V4 | A PDF can be produced in every distribution. | `docs/feature/story-6052-server-side-chart-rendering/spike/findings.md`: headless Chromium works on server and Docker with caveats; Tauri print-to-PDF costs ≈0 in Standalone. Probe 2 in `.../spike/wave-decisions.md` supersedes the findings table's row C "DOESN'T WORK": in-process Jint + HarfBuzz rendering came out near-identical. | **Medium-High** for feasibility. **None** for which mechanism (not decided). |
| V5 | Then & Now is the cheapest first payload. | Its values are frozen and it carries no charts (Epic 5878 journey: "no charts"), so no chart rendering is needed. | **High** for cost. Says nothing about value (see X2). |

---

## Wave: DISCOVER / [REF] Invalidated assumptions

| # | Assumption (as stated or implied) | Evidence against |
|---|---|---|
| X1 | "A PDF is what people want when numbers leave Lighthouse." | **Challenged, not disproven.** Only one of three observed workarounds (screenshots) is a static picture. CSV means the user wants the data to rebuild; MCP plus AI means they want to compose their own view. A PDF serves the first, not obviously the other two. |
| X2 | "The first payload (Then & Now) serves the observed job." | **Challenged.** Captured today: metrics charts and forecasts/Deliveries (maintainer 2026-10-17). Then & Now is new and nobody captures it yet. |
| X3 | "Print the whole page so all metrics export cleanly" (#4753 shape). | The spike found the interactive page was not designed for print and splits across pages (`docs/feature/story-6052-server-side-chart-rendering/spike/findings.md`). Dropping the shape is a decision (D9), not evidence. |
| X4 | "Rendering charts identically outside the browser is cheap." | Spike #6052: five chart components derive their data in `useEffect` and render empty server-side; a bundled browser adds 99–120 MB to Standalone and ≈480 MB on disk to Docker; Helm's 1 GiB limit has no headroom for Chromium. Irrelevant for Then & Now, decisive once charts arrive. |
| X5 | "PDF can start Premium and become free later at no cost." | **Challenged, on an assumption.** We have no record of moving a capability from Community to Premium; we assume it would read as a take-away. If so, starting Premium keeps the option, but the later flip is effectively one-way: once free, it stays free. Also, the Epic 5878 DIVERGE dissent: gating the management-facing PDF works against land-and-expand, since the PDF is what reaches the sponsor. |

---

## Wave: DISCOVER / [REF] Dropped options

| Option | Why dropped |
|---|---|
| Print the interactive page as is (#4753) | A decision the maintainer confirmed on 2026-10-17: #4753 was removed as "superseded by Reports" (D9). The spike also found the interactive page splits badly across pages. |
| PDF as its own report type | A PDF is a capability of any report, built once (Epic #6298 description, 2026-10-14). |
| Bundling a headless browser in Standalone | +99–120 MB per OS plus notarising a second engine (spike #6052). |
| Redrawing charts in .NET (ScottPlot / SkiaSharp) | Only "visually equivalent"; every chart drawn twice (spike #6052). |
| browserless sidecar | SSPL / commercial licence (spike #6052). |

Not dropped, only out of this Epic: CSV export of forecasts (#1822), and anonymising for sharing (#4753, see Open
questions 7 and 9).

---

## Wave: DISCOVER / [REF] Decision gate

| Gate | Status | Why |
|---|---|---|
| G1 Problem | **FAIL on threshold** | No direct customer interviews (threshold: 5, >60% confirming). The evidence is one informant, the maintainer: two dated backlog records the maintainer wrote (#1822, #4753) and the 2026-10-17 interview. The only customer-originated signal is the reference-customer observation #1822 relays second-hand (screenshots exported by hand). |
| G2 Opportunity | **FAIL** | Five opportunities (O1–O5), no importance or satisfaction ratings, so no score above 8 can be shown. The gap between captured content and first payload (X2) is open. |
| G3 Solution | **FAIL** | Nothing tested with users. The spike tested feasibility, not value or usability. |
| G4 Viability | **NOT EVALUATED** | Light DISCOVER; no Lean Canvas exists. Partial signals, as notes only: feasibility amber-to-green (V4) with the mechanism open; licence a decision-in-principle with a planned later flip (D4); a free substitute exists (MCP plus AI); no usage event yet (DEVOPS). |

**Proceeding is a maintainer-accepted risk**, as with Epic 5878. The maintainer asked for a light DISCOVER and
answered the interview on 2026-10-17. This wave is a learning investment, not a validated build case. The
cheapest evidence to gather alongside DIVERGE: ask 3–5 users "the last time you showed Lighthouse numbers to
someone else, what did you send?" and ask to see the artefact. Then count how many were charts, forecasts, tables
or Then & Now.

---

## Wave: DISCOVER / [REF] Constraints established

| # | Constraint | Evidence source |
|---|---|---|
| C1 | One delivery mechanism for every report type (Then & Now, Custom Metric Reports, Signals). | Epic #6298 description |
| C2 | Server zips and Docker are hard targets (scheduled jobs, no UI). Standalone needs in-app view plus PDF export at near-zero size cost; Tauri kills the backend when the app exits. | `docs/feature/story-6052-server-side-chart-rendering/spike/findings.md` |
| C3 | Helm's 1 GiB memory limit has no headroom for an in-process browser; headless Chromium as non-root needs `--no-sandbox` with requests restricted to Lighthouse's own origin. | `docs/feature/story-6052-server-side-chart-rendering/spike/findings.md` |
| C4 | Once charts are in a PDF, they look like the frontend's charts. | `docs/feature/story-6052-server-side-chart-rendering/spike/findings.md` (maintainer narrowing, 2026-09-24) |
| C5 | Fonts must ship with the app: Quicksand loads from Google Fonts at runtime today, so offline and air-gapped PDFs would fall back. | `docs/feature/story-6052-server-side-chart-rendering/spike/wave-decisions.md` |
| C6 | A PDF is a snapshot. It must say when it was produced and which windows it covers, so nobody reads it as current. | Project rule (no value the reader cannot tell is stale) |
| C7 | The PDF renders the instance's configured terms (Feature, Work Item, Team, Portfolio, Delivery, …). | `TerminologySeeder.cs`; project rule |
| C8 | Who may export is decided through RBAC (`useRbac()`), not ad hoc. | Project architecture rule |
| C9 | DEVOPS designs the usage event; it never carries report content or names. | Project rules; usage data never personal |
| C10 | The report must show the same values in the PDF as on screen (Then & Now's Then is frozen; Now is computed at export time). | Epic 5878 journey |

---

## Wave: DISCOVER / [REF] Key decisions

- [D1] One workspace, `epic-6298-report-pdf-export` (see: maintainer dispatch 2026-10-17).
- [D2] Light DISCOVER: the maintainer interview stands in for customer interviews; G1–G3 recorded as failing, G4 not evaluated (see: maintainer request 2026-10-17; Decision gate).
- [D3] The PDF is a capability of a report, built once; Then & Now is the planned first payload, charts follow with Custom Metric Reports (see: Epic #6298 description 2026-10-14). The fit of that first payload is an open question (X2).
- [D4] Licence, decision-in-principle: Premium while there are no extras; free PDF with Premium extras later. The later flip is assumed one-way: we have no record of moving a capability from Community to Premium, and we assume it would read as a take-away (X5) (see: maintainer 2026-10-17).
- [D5] MCP plus AI dashboards recorded as a competing alternative, not a workaround to displace (see: maintainer 2026-10-17).
- [D6] Product direction, graded **stated intent**: reports are for sharing, the interactive charts for exploring. The maintainer verbatim: "the idea of reports is exactly to make this 'shareable', so later on we want reporting for different types like metrics charts optimized for sharing and not for 'playing around'" (see: maintainer 2026-10-17).
- [D7] Mechanism not chosen here; DIVERGE owns it (see: `docs/feature/story-6052-server-side-chart-rendering/spike/wave-decisions.md`, maintainer 2026-09-24).
- [D8] No ICE change: no new direct evidence; the interview consolidates what the maintainer's own #1822 and #4753 already said (see: wave-decisions.md D8).
- [D9] #4753's "print the page" shape is dropped. A decision the maintainer confirmed on 2026-10-17 (#4753 removed as "superseded by Reports"), not evidence (see: maintainer 2026-10-17).

---

## Wave: DISCOVER / [REF] Pre-requisites

- Reports tab and the Then & Now type (Epic 5878, Released).
- A way to render a report statically (print route or equivalent) without the interactive controls.
- For chart-bearing payloads: Custom Metric Reports (#6299), and for non-browser mechanisms the five effect-driven chart components refactored (spike #6052).
- Fonts shipped with the app (C5) if the mechanism renders outside the user's browser.

---

## Wave: DISCOVER / [REF] Open questions for DIVERGE

1. **Mechanism**: browser print with a print stylesheet, headless Chromium (PuppeteerSharp), in-process Jint + HarfBuzz + Skia, or a Gotenberg sidecar, given C2–C5. Does the first slice need a server-side mechanism at all, or is a browser/Tauri print enough until #5882 needs unattended rendering?
2. **PDF vs other share forms**: a slide-ready image per panel, a shareable read-only link, CSV, or making reports reachable through MCP so AI dashboards use them. Which form serves O1–O3 best, given the observed workarounds (X1, D5)?
3. **First payload**: Then & Now (cheapest, not captured today) or a chart-bearing report closer to what people capture (metrics charts, forecasts/Deliveries) (X2).
4. **Licence gate timing**: Premium now with a planned flip, or free from the start (5878 dissent: gating hurts land-and-expand). What are the "extras" that would justify Premium later (branding, scheduled email, anonymising)?
5. **Downloaded vs stored**: generate on demand and download, or store server-side (the spike assumed stored, for #5882 email). Storage brings retention, RBAC on stored files and a migration.
6. **Snapshot honesty**: how a PDF shows its generation time and windows (C6), and whether Now is recomputed or taken as on screen.
7. **Anonymising** (#4753): in or out of scope, and which names it would cover (Teams, Work Items, people).
8. **Who exports**: any reader of the report, or editors only (C8).
9. **What leaves Lighthouse**: what may a PDF carry out of Lighthouse (Work Item titles, Team and person names, or aggregates only), and how is a file outside the access model marked as confidential? Linked to #4753's "Option: Anonymise so it can be shared?" and question 7.

---

## Wave: DIVERGE / [REF] Recommendation

Full text: `recommendation.md` · artifacts: `diverge/` · decisions DV-1..DV-9: `wave-decisions.md`.

- **Job**: `job-share-numbers-outside-lighthouse` (added to `docs/product/jobs.yaml`). Under-served outcomes, as
  desk estimates: a shared number read as current (OS3), time into the reader's slide or email (OS1), and a figure
  misread because its meaning is explained only inside Lighthouse (OS6).
- **Direction**: Option 4, **"Browser-printed PDF"** (4.40 of 5, tied with the dissent "Slide image" and chosen
  by the stated tie-break, higher Desirability + Viability; third "Server-rendered chart report" 4.00). **Export
  PDF** opens a print-designed report with a header carrying the Team or
  Portfolio, "produced {date}", the Then and Now windows and a "how to read this" line. The file is saved through
  the browser's or the Standalone webview's print. This costs about 0 MB in every distribution (Standalone
  desk-checked; probe before DESIGN), with no server renderer; a server renderer loads the same view later for
  #5882. The mechanism is DESIGN's.
- **Dissent trigger**: if users mostly share single charts pasted into slides, add "Copy as image" with the same
  dated caption to report panels as a follow-up. Do not swap directions.
- **Q3 first payload**: Then & Now (the X2 gap is accepted knowingly; #6299's charts print through the same route
  later).
- **Q4 licence (maintainer decides)**: Community PDF with a "Made with Lighthouse" footer; Premium removes it or
  adds own branding, on top of the existing 2-reports-per-owner cap. This would replace D4's "Premium until extras
  exist".
- **Deployment scope (maintainer input 2026-10-17, DISCUSS confirms)**: Standalone may be left out if that makes
  the feature cheaper. The recommended option does not need to leave it out. If a later step does, hidden vs
  disabled-with-reason is DISCUSS's or DESIGN's call.
- **Q5–Q9**: downloaded, not stored; Now as computed at export; anonymising out of v1; exporters are report
  readers via RBAC; Then & Now carries aggregates and the Team or Portfolio name only.

---

## Wave: DISCUSS / [REF] Prior-Wave Reading Confirmation

**Agent**: Luna (`nw-product-owner`) · **Date**: 2026-10-17 · **Mode**: autonomous subagent; the maintainer was not
reachable from the session, so decisions that are the maintainer's carry a proposed default marked **PROPOSED —
maintainer confirms** and are listed under "Questions for the maintainer". Config: user-facing, walking skeleton
"depends" (brownfield, evaluated below), research depth lightweight, JTBD reused from DIVERGE (refined, not re-run),
density lean. Per-wave peer review skipped by instruction (the consolidated review runs at the end of DISTILL).

| Read | Status |
|---|---|
| `feature-delta.md` DISCOVER + DIVERGE sections | ✓ |
| `wave-decisions.md` (DV-1..DV-9, maintainer decisions on the recommendation, Linux print probe) | ✓ — locked inputs |
| `recommendation.md` | ✓ |
| `discover/wave-decisions.md` (D1–D9, "Adjacent opportunity" now moved into scope) | ✓ |
| `diverge/job-analysis.md` | ✓ (job reused) |
| `diverge/competitive-research.md`, `options-raw.md`, `taste-evaluation.md`, `review.yaml` | ⊘ not read; the recommendation and the decision log were enough |
| `docs/product/jobs.yaml` (`job-share-numbers-outside-lighthouse`, tail of file) | ✓ |
| `docs/product/journeys/epic-6298-report-pdf-export.yaml` | ✓ (refined this wave) |
| Precedent: `docs/feature/epic-5878-baseline/feature-delta.md` DISCUSS sections, `slices/slice-01-*.md`, `discuss/wave-decisions.md` | ✓ |
| Brownfield code: frontend `pages/Common/Reports/*`, backend `TeamReportsController`, `ReportReadModels`, `UsageDataEventName`, `index.html` | ✓ (targeted, below) |
| `lighthouse-clients`: `skills/README.md`, `skills/lighthouse/SKILL.md`, MCP tool list, `docs/release-model.md` | ✓ |
| `docs/feature/story-6217-lighthouse-skills/feature-delta.md` (how skills are built and shipped) | ✓ (targeted) |

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory (brownfield)

| # | What exists | Where | Consequence |
|---|---|---|---|
| S1 | One report view serves Team and Portfolio reports (owner passed in); breadcrumb "Reports › {name}" with Edit report and Delete for editors; body in `WidgetBody` with loading, could-not-load (Retry) and "no longer exists" looks; data from a keyed TanStack query | `Lighthouse.Frontend/src/pages/Common/Reports/ReportView.tsx`, `ThenAndNowHeader.tsx` | Export PDF lands once and serves both owners: no separate Portfolio slice. The existing loading and failure looks already decide when an export is possible. |
| S2 | Summary marks ✔ Improved, ✘ Got worse, ± Mixed and the no-change mark are **text characters**, coloured; the probe also saw ▲ in the panels | `ThenAndNowSummary.tsx`, `panels/*` | These fell back to system fonts in the probe; they must not depend on the machine (slice 02). |
| S3 | Report reads: `GET …/teams/{id}/reports`, `…/reports/{reportId}` (and Portfolio twins) under `TeamRead` / `PortfolioRead`; the read carries name, created date, both windows and every metric's Then and Now values, Now computed on read | `TeamReportsController.cs`, `PortfolioReportsController.cs`, `ReportReadModels.cs` | The PDF needs no new endpoint (DESIGN confirms). The clients slice can read the same endpoints. |
| S4 | No print stylesheet and no `window.print` anywhere in the frontend | search of `Lighthouse.Frontend/src` | The print layout is new; nothing to unpick. |
| S5 | The Quicksand typeface loads from `fonts.googleapis.com` at runtime | `Lighthouse.Frontend/index.html`, theme in `main.tsx` | The probe embedded Quicksand only because the machine was online; an offline or air-gapped instance prints in a fallback face (DISCOVER C5). |
| S6 | Usage data: `ReportCreated = 16`, `ReportOpened = 17`, `ReportDeleted = 18`; last on this checkout `WorkTrackingSystemFieldsChanged = 19` | `UsageDataEventName.cs` | A new export event appends after the last integer on `main` (DEVOPS checks). |
| S7 | Clients: 49 MCP tools and the `lh` commands; **none reads reports**; the client package already reads the instance's terminology | `lighthouse-clients/packages/mcp-core/src/index.ts`, `packages/client/src` | Epic 5878 excluded reports from CLI/MCP (its D15). An assistant cannot fetch a report today (Q1). |
| S8 | Skills: one folder per skill, one zip per skill packed by `scripts/pack-skills.sh` in CI; a drift test fails when the `lighthouse` skill does not name every MCP tool and `lh` command; skill-only changes need no changeset; package changes need one and a manual `pnpm release:version` before the release | `lighthouse-clients/skills/README.md`, `docs/release-model.md` | A new skill is cheap to ship; new client tools also touch the `lighthouse` skill text. |
| S9 | Standalone release builds wire no print shortcut or menu; `window.print()` from the frontend prints in Tauri 2 on Linux (probe) | `wave-decisions.md`, probe 2026-10-17 | The Export action calls print itself. macOS unprobed. |
| S10 | Docs: `docs/reports/reports.md`, `docs/reports/thenandnow.md`; skill zips listed in `docs/aiintegration.md` | `docs/` | Finalize targets. |

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | Role here |
|---|---|
| `flow-coach` (consulting) | **Primary.** Elena Kovacs, external flow coach with Team Lightspeed since 1 Aug 2026, Team Admin. Exports the Then & Now report for the client's sponsor. |
| `delivery-lead-rte` | **Primary, recurring.** Martin Achterberg, Head of Delivery, Viewer on Team Lightspeed and on Portfolio Ocean Explorer. Exports a report every month for the steering group. |
| `flow-coach` (internal, Standalone) | **Primary, cannot-share case.** Priya Raman, coach of Team Gravity, runs the Lighthouse app (Standalone) on a Linux laptop; nobody else can reach her data. |
| *(candidate)* `report-reader` | **Not created.** Sabine Roth, the client's sponsor: reads the PDF weeks later without Lighthouse. Her needs (dated, self-explaining, readable in black and white) are outcomes of the sender's job; no separate entry. |

## Wave: DISCUSS / [REF] JTBD One-Liner

- `job-share-numbers-outside-lighthouse` (DIVERGE, reused): when someone who will not or cannot open Lighthouse needs
  to know where flow stands, hand them the numbers already read in Lighthouse in a form that explains itself and says
  when it was taken. Every story carries this `job_id`. The skill slices serve the same job through another carrier
  (the assistant's dashboard, the third observed workaround).
- `job-flow-coach-show-whether-flow-changed` is carried, not served anew: the PDF and the dashboard take a Then & Now
  result to its audience.

---

## Wave: DISCUSS / [REF] Changed Assumptions

DISCOVER and DIVERGE documents are not edited; these supersede them for DESIGN onward.

| Was (source) | Now | Why |
|---|---|---|
| Licence: Premium until extras exist (DISCOVER D4); Community with a footer that Premium removes (DV-6) | **Community, branded with LetPeopleWork and Lighthouse; no removal or own branding in v1** (D10) | Maintainer 2026-10-17. The ADO Epic still carries the `Premium` tag. |
| "Export unavailable on this licence" and "rendering unavailable on this host" failure modes (DISCOVER journey) | Removed | Community (D10); no server rendering in this Epic (D16). |
| Standalone needs PDF export (DISCOVER C2), relaxed by DV-2 | Covered anyway by browser print; Linux proven by probe; macOS checked in slice 01 with a fallback (D12, Q2) | Probe 2026-10-17. |
| AI-dashboard skill out of scope (DISCOVER "Adjacent opportunity", DV-9) | **In scope**, as slices 04–05, each shippable on its own (D18) | Maintainer 2026-10-17. |
| Reports are not reachable through CLI/MCP (Epic 5878 D15) | Reading reports through the clients is proposed (slice 04, Q1); creating, editing and deleting stay web-only | Without it the skill has nothing to read (S7). |
| The PDF embeds the Lighthouse typeface (probe result) | Only when the machine is online (S5) | The probe ran online. |

---

## Wave: DISCUSS / [REF] Locked Decisions

Numbering continues after DISCOVER's D1–D9 (DIVERGE used DV-1..DV-9). D10–D18 restate the maintainer's decisions of
2026-10-17 (locked). D19 onward are this wave's; those marked **PROPOSED — maintainer confirms** wait for an answer
under "Questions for the maintainer".

- [D10] **Licence**: the PDF is Community and carries LetPeopleWork and Lighthouse branding. Removing or replacing the
  branding is a later Premium extra, not in v1. Supersedes D4 and the Premium half of DV-6. (Maintainer, locked.)
- [D11] **First payload: Then & Now**, knowing it is not what people capture today (X2). (Locked.)
- [D12] **Deployment scope**: Standalone may be left out where that saves cost. Browser print covers it at no size
  cost; Linux works with no Rust code (probe). macOS is checked in slice 01; if it fails, see D28. (Locked + D28.)
- [D13] **Content**: aggregates plus the {Team} or {Portfolio} name and the report's name; no {Work Item} titles,
  no person names. The header as recommended (owner, report, Then and Now windows, produced, "how to read this").
  No confidentiality line in v1 — the recommendation left it optional and the maintainer found the header "good
  enough". (Locked; the confidentiality part PROPOSED.)
- [D14] **Who may export**: anyone who can read the report. Exporting is reading what is already shown; no new RBAC
  requirement. (Locked.)
- [D15] **Delivery**: made on demand, downloaded through the print dialog, never stored. Anonymising deferred.
  Nothing is persisted, so no migration. (Locked.)
- [D16] **Fidelity**: our own print layout; small differences between browsers are acceptable. **Requirement
  recorded for Scheduled Report Delivery (#5882), not built here**: a later server render of a report must be
  pixel-comparable to the Chromium print of the same view. So the print-designed report must be a view a renderer
  can load on its own, not something assembled only inside the click. (Locked; the "own view" consequence for DESIGN.)
- [D17] **Print layout** (from the probe): the light theme always; no app chrome (navigation, tabs, Edit report,
  Delete, app footer); our own header and a branding footer; ✔ ✘ ▲ ± and the no-change mark must not depend on system
  fonts (DESIGN decides how); no stray near-empty last page. The Export action calls print itself, because release
  Standalone builds wire no print shortcut. (Locked.)
- [D18] **The report skill is in scope** as its own slice, shippable alone; it describes how to show each report
  type as a dashboard or document, starting with Then & Now. (Locked.) This wave splits it into the client read
  (slice 04) and the skill (slice 05), so each ships alone (Q1).
- [D19] **Produced = when the printed values were read.** Export reads the report afresh before printing, so Now
  ends on the export day and the screen shows the same values as the PDF. The header shows date and time in the
  viewer's local time. (PROPOSED — DISCUSS default; DESIGN chooses the mechanism.)
- [D20] **Export PDF sits on the report view only**, for every reader, beside Edit report and Delete for editors;
  not on the Reports list in v1. Exact place and label are sketched at the start of DISTILL. (PROPOSED.)
- [D21] **"How to read this"** (part of the recommended header) ships as its own thin slice (03), so slice 01
  stays a one-day walking skeleton.
- [D22] **Fonts ship with the app** instead of loading from Google Fonts at runtime, so an offline instance prints
  in the Lighthouse typeface. (PROPOSED — maintainer confirms, Q4.)
- [D23] **Reports become readable through the clients**: list an owner's reports and read one, for Teams and
  Portfolios, in MCP (all transports) and `lh`; read-only. (PROPOSED — maintainer confirms, Q1.)
- [D24] **Skill name `lighthouse-reports`**, release asset `lighthouse-reports-skill.zip`. (PROPOSED, Q6.)
- [D25] **Skill rules**: quote the report's values exactly; never rebuild Then from today's metrics; a dash stays a
  dash with its reason; the instance's terms; a dated header with both windows and the day the report was read; an
  attribution line naming Lighthouse by LetPeopleWork; any comment of the assistant's own kept visibly apart from
  the report's numbers and labelled as such. (PROPOSED, Q3.)
- [D26] **Order**: PDF slices 01 → 02 → 03, then 04 → 05. (PROPOSED, Q5.)
- [D27] **Usage data is wanted**: one event when Export PDF is chosen (DEVOPS designs; it counts the intent, since
  nothing tells us whether the dialog saved a file). Client reads may count through the clients' existing practice
  of reporting the web's event for a mapped command; DEVOPS decides. Never names or values.
- [D28] **If printing does not work in the macOS Standalone app**, Export PDF stays visible but disabled there, with
  a one-line reason. (PROPOSED, Q2.)

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — 5 stories, 2 code bases (Lighthouse frontend; `lighthouse-clients`), ~4 days.** No oversize signal: under
10 stories, no new bounded context (export is a capability of the existing Reports module; the client read and the
skill sit in the existing clients and skills), the walking skeleton touches 2 integration points (report view →
system print), under 2 weeks. Two user outcomes (PDF; assistant dashboard) that could ship separately — the
maintainer chose to keep both in this Epic (D18); they are ordered, not interleaved, and the skill pair builds only on
finished slices.

---

## Wave: DISCUSS / [REF] Journey

SSOT: `docs/product/journeys/epic-6298-report-pdf-export.yaml` (refined this wave). Two journeys.

```
PDF:  [Open report] ──> [Export PDF] ──> [print dialog: Save as PDF] ──> [attach / hand over] ──> [Sabine reads it]
      Burdened           Ready            Relieved                        Relieved                  Sceptical → Oriented
      report_values      + generated_at   (nothing stored)                generated_at              header: produced, windows
      owner_name                          light theme, no chrome                                    "how to read this", branding

Skill: [Ask: which reports?] ──> [Assistant reads the report] ──> [Dashboard / document per the skill]
       Wary                       Engaged                           Confident
       owner_name, report_name    report_values, read_at            same values, dated, own comments apart
```

Emotional arc: **burdened → relieved → confident** (sender); **sceptical → oriented** (reader); **wary → confident**
(assistant user). Shared artifacts and their single sources are in the YAML (`report_values`, `report_windows`,
`owner_name`, `report_name`, `generated_at`, `read_at`, `branding`). Integration checkpoint: the PDF and the
assistant show the values the report view shows, and the PDF's produced date is the day its Now window ends.

---

## Wave: DISCUSS / [REF] Story Map & Slices

**Backbone**: *open a report* → *export it* → *it reads the same anywhere* → *it explains itself* → *an assistant
reads it* → *an assistant shows it*.

| Open report | Export | Reads the same anywhere | Explains itself | Assistant reads | Assistant shows |
|---|---|---|---|---|---|
| (exists, Epic 5878) | **01 Export PDF, print layout, header, footer** | 02 own marks, shipped fonts, page breaks | 03 "how to read this" | 04 list + read reports via MCP and `lh` | 05 `lighthouse-reports` skill |

**Walking skeleton = slice 01** (brownfield): the existing report view gains Export PDF; the print dialog produces a
branded, dated PDF in a browser and in the Linux Standalone app. It touches every PDF activity thinly.

| # | Slice | Est. | Ships alone | Learning hypothesis — disproves … if it fails |
|---|---|---|---|---|
| 01 | Export PDF on a Then & Now report (Team and Portfolio): fresh read, print layout (light, no chrome), header with owner, report, windows, produced; branding footer; usage event | 1d | Yes | "Browser print of a print-designed view is good enough to hand over" — if the maintainer would not send the PDF of the dev instance's busiest Team's report to a sponsor without editing it (cut panels, leftover chrome, wrong date), the route needs more than a print layout |
| 02 | The PDF reads the same everywhere: marks drawn, not borrowed from system fonts; the Lighthouse typeface shipped with the app; no panel split across pages, no near-empty last page, on A4 and Letter | 1d | Yes | "Own marks and shipped fonts make PDFs alike" — if the same report printed from Chrome on Windows, Firefox on Linux and the Linux Standalone app differs in more than spacing |
| 03 | "How to read this": one line saying which side is frozen and since when, what Now covers, what the colours and "—" mean, in the instance's terms | ½d | Yes | "One line explains the report to a cold reader" — if someone who has never opened Lighthouse, handed the PDF, cannot say which side is frozen and whether {Cycle Time} got better |
| 04 | Reports readable through the clients: list an owner's reports and read one, MCP and `lh`, Team and Portfolio; the `lighthouse` skill names the new tools | 1d | Yes | "Reading the report is enough for an assistant to answer about it" — if, in the eval, the assistant still calls the metric tools to rebuild Then |
| 05 | `lighthouse-reports` skill: how to show a Then & Now as a dashboard or document; evals; its own zip | ½d | Yes (builds on finished 04) | "A skill makes assistant dashboards faithful" — if in 3 eval runs a dashboard misquotes a value, drops the dates, or mixes the assistant's comment into the numbers |

Briefs: `slices/slice-NN-*.md`.

### Priority Rationale

- **01** is the walking skeleton and the Epic's core value (value 5, urgency 4, effort 2).
- **02** next: riskiest assumption — the probe showed font-dependent marks and a fallback typeface offline; a PDF
  that looks broken on the sponsor's machine undoes 01's value.
- **03** completes the recommended header (OS6, misread without explanation); cheapest slice.
- **04 → 05** last, by D26: independent of the PDF, the maintainer's "low effort, give it a try"; 05 needs 04's read
  (one-direction dependency on a finished slice). If the maintainer picks option (c) in Q1, 04 drops and 05 stands
  alone.

### Slice taste tests

| Test | Verdict |
|---|---|
| 4+ new components in one slice? | Borderline in 01 (Export action, print layout, print header, branding footer, event). Each is thin; the header and footer are one print frame. Kept as the walking skeleton. |
| Every slice depends on a new abstraction? | Pass — the print frame is born in 01 and is the seam #6299's chart reports and #5882's server render reuse (D16). |
| Disproves a pre-commitment? | Pass — 01 tests DV-3/DV-7 (browser print is enough), 02 tests D17/D22, 03 tests OS6, 04 tests D23, 05 tests D18/D25. |
| Synthetic-data-only slices? | Pass — 01–03 are dogfooded on the dev instance (`:5169`, real history) and the Linux Standalone app with the maintainer's data copy; 04–05 against the dev instance. Demo data covers E2E and screenshots only. |
| Two slices alike except scale? | Pass. |

---

## Wave: DISCUSS / [REF] User Stories

<!-- markdownlint-disable MD024 -->

System constraints (all stories): Community, no licence check (D10) · RBAC: exporting and reading through the clients
need only the owner's Read; UI gating via `useRbac()` only, never fetching `/api/latest/authorization/my-summary` ·
works with auth off · nothing stored, no migration (D15) · the PDF shows exactly the values the report view shows,
read at export (D19) · every label through Terminology (seeded defaults {Team}, {Portfolio}, {Work Item},
{Cycle Time}, …) · meaning never by colour alone · no {Work Item} titles or person names leave Lighthouse (D13) · no
server-side rendering (D16) · no verdicts or generated text in the report itself (Epic 5878 D39).

### US-01 — Export a Then & Now report as a PDF (slice 01, walking skeleton)

`job_id: job-share-numbers-outside-lighthouse` · persona `flow-coach` (Elena Kovacs) and `delivery-lead-rte`
(Martin Achterberg)

**Problem**: Elena wants the client's sponsor, Sabine Roth, to see the Then & Now report for Team Lightspeed. Sabine
will not open Lighthouse. Today Elena screenshots the report, crops it into a slide and types the date beside it;
the screenshot carries the app's navigation and nothing says when it was taken. *(The workaround is second-hand,
from the maintainer; DISCOVER G1 failed, maintainer-accepted risk D2.)*

#### Elevator Pitch

Before: a report can only be read inside Lighthouse; getting it to Sabine means screenshots and a hand-typed date.
After: Team Lightspeed → Reports → "Then & Now — 90 days to 2 Oct 2026" → **Export PDF** → Save as PDF → sees a light
page headed "Team Lightspeed · Then & Now — 90 days to 2 Oct 2026", "Then 5 Jul – 2 Oct 2026 · Now 1 Aug – 30 Oct
2026", "Produced 30 Oct 2026, 09:15", the summary and the {Cycle Time} panel (85th 21 → 12 days, −9 days, −43%),
and a Lighthouse · LetPeopleWork footer — no navigation, tabs or buttons.
Decision enabled: whether to send this file to the sponsor as it is, instead of building a slide.

**Examples**: (1) Elena exports on 30 Oct 2026 in Chrome; the PDF shows the values on her screen and "Produced 30 Oct
2026, 09:15". (2) Martin, a Viewer, uses the dark theme; he exports the Portfolio Ocean Explorer report on 2 Nov
2026 for the steering group; the PDF is light and headed with "Portfolio Ocean Explorer". (3) Elena opened the report
on 29 Oct and left the tab open; she exports on 30 Oct; the PDF shows Now ending 30 Oct, not 29 Oct. (4) Priya Raman
exports from the Lighthouse app on her Linux laptop; the system print dialog opens. (5) The report cannot be loaded
for Martin on 2 Nov; no Export PDF is offered.

```gherkin
Scenario: A coach hands a sponsor a dated Then & Now report as a PDF
  Given Team Lightspeed has the report "Then & Now — 90 days to 2 Oct 2026"
  And on 30 Oct 2026 its Cycle Time panel shows 85th percentile 21 days Then and 12 days Now
  When Elena chooses Export PDF and saves the file from the print dialog
  Then the PDF is headed "Team Lightspeed" and "Then & Now — 90 days to 2 Oct 2026"
  And it shows "Then 5 Jul – 2 Oct 2026", "Now 1 Aug – 30 Oct 2026" and "Produced 30 Oct 2026"
  And its Cycle Time panel shows 21 days, 12 days, "−9 days" and "−43%"
  And it ends with the Lighthouse and LetPeopleWork branding
  And it shows no navigation, tabs, Edit report or Delete

Scenario: A Viewer in the dark theme gets a light PDF of a Portfolio report
  Given Martin is a Viewer on Portfolio Ocean Explorer and uses the dark theme
  When Martin exports the Portfolio's Then & Now report on 2 Nov 2026
  Then the PDF has a light page headed "Portfolio Ocean Explorer"

Scenario: A report left open overnight is exported with today's Now
  Given Elena opened the report on 29 Oct 2026 and left it open
  When Elena chooses Export PDF on 30 Oct 2026
  Then the PDF shows "Now 1 Aug – 30 Oct 2026" and "Produced 30 Oct 2026"
  And the report on screen shows the same values as the PDF

Scenario: No PDF of a report that could not be loaded
  Given the report cannot be loaded for Martin on 2 Nov 2026
  When Martin looks at the report page
  Then he sees "This report couldn't be loaded. Reload to try again." with Retry
  And Export PDF is not offered

Scenario: The Lighthouse app on Linux opens the print dialog
  Given Priya runs the Lighthouse app on Linux with Team Gravity's report open
  When Priya chooses Export PDF
  Then the system print dialog opens on the print-designed report

Scenario: Configured terms head the PDF
  Given the instance calls a Team a "Squad" and Cycle Time "Lead Time"
  When Elena exports Team Lightspeed's report
  Then the PDF is headed "Squad Lightspeed" and its panel reads "Lead Time: Then & Now"
```

**AC**: AC-1.1 Export PDF on the report view for anyone who can read the report (D14, D20), for Team and Portfolio
reports. AC-1.2 Export reads the report afresh, then asks the browser or the Standalone webview to print (D17, D19).
AC-1.3 The printed page is light whatever the theme, and shows none of the app's navigation, tabs, Edit report,
Delete or app footer (D17). AC-1.4 Header: owner kind and name in the instance's terms, report name, Then window,
Now window, "Produced {date}, {time}" in the viewer's local time (D13, D19). AC-1.5 Body: the summary and every
shown panel with exactly the values on screen. AC-1.6 Footer: Lighthouse and LetPeopleWork branding (D10; copy and
logo sketched at DISTILL). AC-1.7 While the report loads, Export PDF is unavailable; when it could not be loaded or
no longer exists, Export PDF is not offered and no PDF is produced. AC-1.8 Cancelling the dialog leaves the page as
it was; nothing is stored (D15). AC-1.9 The usage event of D27 fires when Export PDF is chosen and is listed in
`docs/settings/usagedata.md`. AC-1.10 macOS Standalone checked during DELIVER; if print does nothing there, D28
applies (Q2).
**KPI**: K1, K2. **Tech**: reuse `ReportView`'s query and `WidgetBody` states; no new endpoint expected; the print
view must be loadable on its own (D16); DESIGN answers the export's own loading and failure look (the fresh read).

### US-02 — The PDF reads the same on every machine (slice 02)

`job_id: job-share-numbers-outside-lighthouse` · persona `flow-coach` (Elena; Priya)

**Problem**: in the Linux probe the change marks came out in LiberationSans and DejaVuSans, picked by the machine; on
another OS, or a server with no such fonts, they differ or vanish. The probe's PDF also had a near-empty second page.
An instance with no internet prints without the Lighthouse typeface at all (S5). Sabine sees a page that looks
broken, and Elena looks careless.

#### Elevator Pitch

Before: the same report printed on two machines shows different or missing ✔ ✘ marks, a different typeface offline,
and a near-empty last page.
After: Export PDF on "Then & Now — 90 days to 2 Oct 2026" from Chrome on Windows and from the Lighthouse app on Linux
→ both PDFs show the same drawn ✔ Improved / ✘ Got worse marks, the Lighthouse typeface, every panel whole on its page
and no page holding only the footer.
Decision enabled: whether the PDF can go to any reader without checking it first on their kind of machine.

**Examples**: (1) Elena prints from Chrome on Windows, Priya from the Lighthouse app on Linux; the marks look the
same. (2) An air-gapped instance with no internet: the PDF still uses the Lighthouse typeface. (3) A report with all
four panels on A4: no panel is cut between pages, no near-empty last page. (4) The same report on US Letter: same.

```gherkin
Scenario: Change marks look the same whichever machine printed them
  Given the summary of Team Lightspeed's report shows "✔ Improved: Cycle Time" and "✘ Got worse: WIP"
  When Elena exports it from Chrome on Windows and Priya from the Lighthouse app on Linux
  Then both PDFs show the same improved and got-worse marks

Scenario: An instance without internet prints in the Lighthouse typeface
  Given a Lighthouse instance that cannot reach the internet
  When Elena exports Team Lightspeed's report
  Then the PDF uses the Lighthouse typeface

Scenario: No panel is cut between pages
  Given Team Lightspeed's report shows all four panels
  When Elena exports it on A4 and on US Letter
  Then every panel sits whole on one page

Scenario: No page carries only the footer
  Given Team Lightspeed's report shows all four panels
  When Elena exports it on A4
  Then no page holds only the branding footer
```

**AC**: AC-2.1 Marks (✔ ✘ ±, the no-change mark, ▲) in the print do not depend on fonts installed on the machine
(D17; DESIGN decides how). AC-2.2 The Lighthouse typeface reaches the PDF without internet (D22, Q4). AC-2.3 No
panel or summary splits across pages; no trailing page holds only the footer; A4 and US Letter portrait. AC-2.4 The
screen keeps looking as it does today (the marks' meaning, colours and words unchanged).
**KPI**: guardrail G3. **Tech**: if D22 bundles the typeface for the whole app, the Standalone and Docker sizes grow
by the font files only (≤ 1 MB, G4); DESIGN confirms.

### US-03 — The PDF explains itself to someone who never opened Lighthouse (slice 03)

`job_id: job-share-numbers-outside-lighthouse` · persona `flow-coach` (Elena), reader Sabine Roth

**Problem**: Sabine receives the PDF three weeks later. She does not know that Then is frozen and Now is the latest
window, or what green, red and "—" mean; inside Lighthouse that is explained by the tool around the report.

#### Elevator Pitch

Before: the PDF shows Then, Now and coloured changes, but only a Lighthouse user knows how to read them.
After: Export PDF on Team Lightspeed's report → sees under the header one line in the instance's terms, e.g. "How
to read this: Then is frozen — the 90 days to 2 Oct 2026, as captured then. Now is the 90 days up to the produced
date. Green is better, red is worse; — means no data in that window." (copy sketched at DISTILL).
Decision enabled: whether the PDF can go out without a covering note explaining it.

**Examples**: (1) Sabine reads the line and says Cycle Time got better. (2) An instance that calls Cycle Time "Lead
Time" and Work Items "Tickets" gets those words in the line. (3) Martin prints the PDF in black and white for the
steering meeting; the line refers to the signs and words, not only to colours.

```gherkin
Scenario: A sponsor learns which side is frozen
  Given Elena exported Team Lightspeed's report on 30 Oct 2026
  When Sabine reads the PDF on 20 Nov 2026
  Then a line under the header says Then is frozen as the 90 days to 2 Oct 2026
  And that Now is the 90 days up to the produced date

Scenario: The explanation uses the instance's terms
  Given the instance calls Cycle Time "Lead Time" and Work Items "Tickets"
  When Elena exports the report
  Then the explanation line says "Lead Time" and "Tickets"

Scenario: A black-and-white print still reads
  Given Martin prints the PDF in black and white
  When the steering group reads it
  Then every change shows its sign and its word, and the explanation names both
```

**AC**: AC-3.1 One "how to read this" line under the header (D13, D21): which side is frozen and its window, what
Now covers, what the colours, signs and "—" mean. AC-3.2 Instance terms throughout. AC-3.3 Shown in the PDF only, not
on the screen (screen unchanged). AC-3.4 Copy confirmed with the maintainer at the start of DISTILL.
**KPI**: K3 (the cold-reader check). **Tech**: none beyond the print frame of 01.

### US-04 — An assistant can read a report through the Lighthouse clients (slice 04)

`job_id: job-share-numbers-outside-lighthouse` · persona `flow-coach` (Elena, Claude Desktop with MCP stdio) and
`delivery-lead-rte` (Martin, `lh` in a terminal)

**Problem**: Elena already asks her assistant, over the Lighthouse MCP connection, to build dashboards. It cannot
see her Then & Now report — no tool reads reports (S7) — so it rebuilds numbers from today's metrics, and Then comes
out different from what was frozen.

#### Elevator Pitch

Before: the MCP tools and `lh` read metrics and forecasts, but no report; an assistant asked about a Then & Now report
has nothing to read.
After: with the Lighthouse MCP connected, Elena asks "What does Team Lightspeed's Then & Now report say?" → the
assistant reads the report and answers "Then 5 Jul – 2 Oct 2026, Now 1 Aug – 30 Oct 2026; Cycle Time 85th 21 → 12
days (−43%)"; Martin runs the `lh` read command DESIGN names (e.g. `lh report get`) and sees the same values.
Decision enabled: whether to let the assistant build the sponsor's dashboard from the report itself.

**Examples**: (1) Elena asks which reports Team Lightspeed has; the assistant names "Then & Now — 90 days to 2 Oct
2026" and "Then & Now — WIP limit 14 Sep", with their created dates. (2) Martin reads Portfolio Ocean Explorer's
report with `lh` and sees the same numbers as the Reports tab. (3) Jonas Weber's API key has no read on Team
Lightspeed; the assistant says he has no access to that Team's reports. (4) A Lighthouse older than Reports: the
assistant says to upgrade Lighthouse.

```gherkin
Scenario: An assistant lists a Team's reports
  Given Team Lightspeed has the reports "Then & Now — 90 days to 2 Oct 2026" and "Then & Now — WIP limit 14 Sep"
  When Elena asks her assistant which reports Team Lightspeed has
  Then it names both reports with the dates they were created

Scenario: An assistant reads a report with the values the Reports tab shows
  Given the Reports tab shows Cycle Time 85th percentile 21 days Then and 12 days Now on 30 Oct 2026
  When Elena's assistant reads "Then & Now — 90 days to 2 Oct 2026"
  Then it reports 21 days Then and 12 days Now, with both windows

Scenario: The command line reads a Portfolio report
  Given Portfolio Ocean Explorer has a Then & Now report
  When Martin reads it with lh
  Then he sees its windows and every shown metric's Then and Now values

Scenario: No access is said plainly
  Given Jonas's key has no read on Team Lightspeed
  When Jonas asks his assistant for Team Lightspeed's reports
  Then he is told he has no access to that Team's reports

Scenario: An older Lighthouse is named as the reason
  Given a Lighthouse instance released before Reports existed
  When Elena's assistant asks for Team Lightspeed's reports
  Then she is told to upgrade Lighthouse
```

**AC**: AC-4.1 List an owner's reports and read one, for Teams and Portfolios, through MCP (stdio and HTTP) and `lh`
(D23); read-only. AC-4.2 Values, windows and dashes exactly as the backend answers them; no recomputation. AC-4.3 A
refused read is relayed as no access; a missing endpoint as "upgrade Lighthouse"; a report made by a newer version as
"can't be shown here". AC-4.4 The `lighthouse` skill names the new tools and command (drift test green). AC-4.5
Changesets for the touched packages; released with the next clients release.
**KPI**: K4. **Tech**: reuses the existing GET endpoints (S3); tool and command names are DESIGN's, following the
existing `lighthouse_{owner}_{noun}_{verb}` pattern; terminology from the existing client read.

### US-05 — An assistant shows a Then & Now report as a faithful dashboard (slice 05)

`job_id: job-share-numbers-outside-lighthouse` · persona `flow-coach` (Elena)

**Problem**: when Elena asks her assistant for a one-page dashboard for Sabine, each run lays it out differently,
sometimes rounds a value, drops the windows, turns a "—" into 0 or adds "great improvement!" as if the report said
so. Elena has to check every number by hand.

#### Elevator Pitch

Before: an assistant can read the report (slice 04) but improvises how to show it.
After: with `lighthouse-reports-skill.zip` installed, Elena asks "Make a one-page dashboard of Team Lightspeed's Then &
Now report for our sponsor" → gets a dashboard headed with Team Lightspeed, both windows and the read date, the summary
groups, one block per metric with Then, Now, change and % change exactly as in Lighthouse, any comment of the
assistant's set apart and labelled, and "Data from Lighthouse by LetPeopleWork".
Decision enabled: whether to send the assistant's dashboard as it is, without re-checking each number.

**Examples**: (1) Elena's dashboard shows Cycle Time 21 → 12 days, −43%, as in Lighthouse. (2) Team Nova's report
shows "—" with "No finished Work Items in Then"; the dashboard shows the same dash and reason. (3) Elena asks "and add
your take"; the assistant's view appears under its own heading, apart from the numbers. (4) The instance calls Work
Items "Tickets"; the dashboard says Tickets.

```gherkin
Scenario: The dashboard carries the report's own numbers and dates
  Given Team Lightspeed's report reads Cycle Time 85th 21 days Then and 12 days Now on 30 Oct 2026
  When Elena asks for a one-page dashboard of the report
  Then the dashboard names Team Lightspeed, both windows and 30 Oct 2026 as the day it was read
  And it shows 21 days, 12 days and −43%

Scenario: Then is never rebuilt from today's metrics
  Given Elena's assistant has the report skill and the Lighthouse connection
  When Elena asks for the dashboard
  Then the assistant reads the report and does not call the metric tools for Then

Scenario: A dash stays a dash
  Given Team Nova's report shows "—" with "No finished Work Items in Then"
  When Priya asks for a dashboard of it
  Then the dashboard shows "—" with that reason and never 0

Scenario: The assistant's own view is kept apart
  Given Elena asks for the dashboard "with your take on it"
  When the assistant answers
  Then its comment sits under its own heading, labelled as the assistant's, apart from the report's numbers
```

**AC**: AC-5.1 The skill describes the Then & Now layout: dated header, summary, one block per shown metric (D25).
AC-5.2 Guardrails: exact values, dashes with reasons, instance terms, no rebuild of Then, attribution line, own
comments apart (Q3). AC-5.3 Eval cases (3 runs each; guardrail cases 3 of 3) recorded under
`docs/feature/story-6217-lighthouse-skills/evals/`. AC-5.4 Its own zip in the clients release; listed in
`docs/aiintegration.md`. AC-5.5 The skill is written so a later report type adds a section, not a new skill.
**KPI**: K4, K5. **Tech**: skill-only change, no changeset (S8); the direction of good per metric is written in the
skill — a second copy of a fact that lives in the frontend panels, so the skill's eval checks it (shared artifact
risk, MEDIUM).

---

## Wave: DISCUSS / [REF] Outcome KPIs

Objective: people who read a report in Lighthouse send it to readers outside the tool as a dated file instead of a
screenshot. **North star: K1.** Targets are first guesses without a baseline (nothing to measure before release);
DEVOPS turns each "Measured by" into a usage-data design.

| # | Who | Does what | Target | Baseline | Measured by | Type |
|---|---|---|---|---|---|---|
| K1 | Opted-in instances that opened ≥ 1 report in the 90 days after release | use Export PDF at least once | ≥ 20% | 0 | export event ÷ instances with `ReportOpened` | Leading (north star) |
| K2 | Instances that exported | export in ≥ 2 different calendar weeks within 90 days | ≥ 30% | 0 | export event per instance (per browser, as Epic 5878's proxies) | Leading (recurring cadence) |
| K3 | People who never opened Lighthouse, handed a PDF by the maintainer | say which side is frozen and whether {Cycle Time} improved | ≥ 2 of 3 | — | maintainer's conversation log (slice 03 dogfood) | Qualitative |
| K4 | Instances using the clients | read a report through MCP or `lh` within 90 days of the clients release | ≥ 5 instances | 0 | the web's `ReportOpened` reported by the clients with their source, if DEVOPS maps it (D27); else K5 only | Leading |
| K5 | Skill eval runs | produce a dashboard with no misquoted value, dates present, comments apart | 3 of 3 per guardrail case, ≥ 90% others | — | eval record (AC-5.3) | Quality gate |

Guardrails: G1 no PDF value differs from the screen at export (acceptance test); G2 opening a report stays ≤ 2 s and
Export opens the dialog ≤ 2 s after the click on a loaded report; G3 marks and typeface identical across Chrome,
Firefox and Linux Standalone (slice 02 check); G4 Standalone installer and Docker image grow by ≤ 1 MB.

Measurement caveat: the export event counts the choice, not a saved file — a cancelled dialog counts too.
Learning to feed the dissent trigger (DV-4): the maintainer's 3–5 user question ("the last time you showed
Lighthouse numbers to someone else, what did you send?") — ≥ 3 answers before Custom Metric Reports (#6299) starts
DISCUSS.

Hypothesis: we believe a one-click, dated, branded PDF of a report will replace screenshots for readers outside
Lighthouse. We will know when ≥ 20% of instances that open reports export one (K1) and ≥ 30% of those do it again in
another week (K2).

---

## Wave: DISCUSS / [REF] Definition of Done

1. Every UAT scenario of the story passes as an automated acceptance test (the print layout via print-media
   emulation; the dialog itself is not automatable — checked by hand, item 7).
2. Unit and component tests green; frontend `pnpm test`, backend `dotnet test` (filtered per CLAUDE.md) if touched;
   clients `pnpm ci` for 04–05.
3. `pnpm build` and, if touched, `dotnet build` with zero warnings; Biome clean; no new SonarCloud issues.
4. RBAC: Export and client reads need only the owner's Read; UI gating via `useRbac()` only.
5. Copy through Terminology; no "Epic", "Story", "Initiative" for product concepts; no verdicts in the report.
6. One E2E walking skeleton (Page Object, demo Team Lightspeed): create a report, Export PDF, the print view shows
   the header and no app chrome. No more E2E than that.
7. A PDF of the dev instance's busiest Team's report printed from Chrome, Firefox and the Linux Standalone app is
   attached to the slice's evidence (slices 01–03); macOS Standalone checked by the maintainer (Q2).
8. Mutation testing ≥ 80% on the feature's code, run last on frozen code (Stryker, both stacks if the backend is
   touched).
9. At feature finalization: docs, screenshots, website, usage-data entry and clients release per the Checklist.

---

## Wave: DISCUSS / [REF] Out of Scope (v1)

Server-side rendering and unattended PDFs (#5882; D16 records its fidelity requirement) · stored PDFs, PDF history,
email or schedule (D15) · removing or replacing the branding, own logo (later Premium extra, D10) · anonymising (D15)
· a confidentiality line (D13) · charts in the PDF (Custom Metric Reports #6299 prints through the same route later)
· "Copy as image" (the DV-4 trigger) · export from the Reports list, several reports at once, CSV · printing the
interactive Metrics page (#4753 shape, D9) · creating, editing or deleting reports through the clients · skill
sections for report types other than Then & Now (none exist yet) · a native print call in the Standalone app (only if
Q2 says so, as a follow-up).

---

## Wave: DISCUSS / [REF] WS Strategy, Driving Ports, Pre-requisites

**Walking skeleton**: brownfield, slice 01. Existing report view → Export PDF → fresh read → print dialog → PDF, on
demo Team Lightspeed (E2E) and the dev instance's busiest Team (dogfood), in Chrome and the Linux Standalone app.

**Driving ports (user-invocable entry points)**: Export PDF on the report view (Team and Portfolio); the browser's or
Standalone webview's print dialog; the existing report read endpoints (S3), unchanged; for slice 04 the MCP tools and
the `lh` command DESIGN names; for slice 05 the skill an assistant loads.

**DESIGN must answer** (project rule, slow connection): Export PDF's look while the fresh read is on its way, and
when it fails (no print, no stale numbers, a could-not-load message); the existing `WidgetBody` states are the
reuse candidate. Also: how the print view is reachable on its own (D16), how marks are drawn (D17), how fonts ship
(D22), the Standalone macOS check (D28).

**DISTILL must sketch with the maintainer first**: Export PDF's place and label on the report view (with and without
Edit report / Delete); the printed page (header lines, produced format, summary, panels, footer copy and logo);
the "how to read this" copy; the macOS disabled-with-reason copy if Q2 needs it; the skill's reference dashboard
layout.

**Pre-requisites**: Reports and Then & Now (Epic 5878, released). For slice 04: the report read endpoints (exist).
For slice 05: slice 04 (or Q1 option c). A macOS machine with the Standalone app for the D28 check (maintainer).
None block slice 01.

**NFRs**: Export opens the dialog ≤ 2 s after the click on a loaded report; keyboard-operable action; PDF text stays
selectable (vector, as the probe showed); meaning never by colour alone (black-and-white print); A4 and US Letter
portrait; Standalone and Docker growth ≤ 1 MB.

---

## Wave: DISCUSS / [REF] Project DISCUSS Checklist

No silent N/A — every item answered.

| Item | Answer |
|---|---|
| **RBAC impact** | **No new requirement, no new guard.** Export is reading: anyone with `TeamRead` / `PortfolioRead` sees Export PDF, Viewers included (D14). No new endpoint, so nothing new server-side. Client reads (slice 04) call the existing GET endpoints under the same guards with the caller's own credential; a refusal is relayed as no access. UI gating via `useRbac()` only. Auth off or RBAC off: everyone may export. `docs/settings/rbac.md` gains "Export PDF: anyone who can read the report" at finalize. |
| **Lighthouse-Clients CLI/MCP** | **Slices 01–03: N/A, because** no API changes. **Slice 04 (if Q1 = a): applies** — new read-only tools and an `lh` read command; changesets for `lighthouse-client`, `lighthouse-mcp-core` and `lighthouse-cli` (minor: new capability), the stdio and HTTP adapters pick up the new core by their dependency bump; manual `pnpm release:version`, commit and push before the release; the `lighthouse` skill names the new tools (drift test). This reopens Epic 5878's D15 for reading only. **Slice 05: skill-only, no changeset**; new release asset `lighthouse-reports-skill.zip` packed by the existing script. If Q1 = b or c: no package change, skill zip only. |
| **Website / marketing surface** | **Owed at finalize**: features list entry "Export any report as a branded PDF (Community)" and the reports skill on the AI integration page. The website hot-links `docs/assets` from `@main`, so new screenshots go live on push. Copy confirmed with the maintainer before editing. |
| **Docs + screenshots** | **Owed at finalize, not batched**: `docs/reports/reports.md` (Export PDF: how, what the PDF holds, produced date, nothing stored, Standalone incl. macOS status, a tip to turn off the browser's own headers and footers) and `docs/reports/thenandnow.md` (reading the PDF); `docs/aiintegration.md` (report tools, the new zip); `docs/settings/rbac.md`; `docs/settings/usagedata.md` (event, in slice 01). `@screenshot`: one per theme of the report view showing Export PDF, and one image of the printed page (always light). Docs wait for the maintainer's confirmation. |
| **Demo data** | **No change planned.** No pre-seeded report (as in Epic 5878); the E2E creates one on Team Lightspeed (≈100 days) and exports it. Checked at finalize that the printed page's screenshot shows an Improved and a Got worse group. |
| **Terminology** | **No new key.** "Export PDF", "Produced", "How to read this" are product words. Header, panels and the explanation use {Team}, {Portfolio}, {Work Item(s)}, {Cycle Time}, {Throughput}, {WIP}, {Work Item Age}. The skill tells the assistant to use the instance's terms, which the clients already read. |
| **Usage-data event** | **Wanted (D27), designed in DEVOPS.** One event when Export PDF is chosen; proposed shape like `ReportCreated` / `ReportOpened` (closed enum `report_template` {ThenAndNow}), appended after the last integer on `main` (19 on this checkout), never renumbered; emitted in slice 01 and listed in `docs/settings/usagedata.md` there. It counts the choice, not a saved file. Client reads: DEVOPS decides whether the clients report the web's `ReportOpened` with their source (K4). The skill itself: N/A, because it is text an assistant reads; nothing of ours runs to emit. Never report names or values. |
| **EF migrations** | **N/A, because** nothing is stored (D15) and no endpoint changes; DESIGN confirms. |
| **Premium gating** | **None.** Community (D10); export does not count against the 2-reports cap. Removing or replacing the branding is a later Premium extra, not built. The ADO Epic's `Premium` tag no longer matches — the maintainer's call. |
| **ADO** | Epic #6298 is Active with no child Stories. Proposed, one Story per slice: 01 "Export a Then & Now report as a PDF", 02 "The report PDF reads the same on every machine", 03 "The report PDF explains itself", 04 "Read reports through the Lighthouse clients", 05 "Lighthouse reports skill". Creating them and retagging the Epic is the maintainer's call; not touched by this wave. |
| **Release notes** | The maintainer's. Proposed lead: the pain (screenshots with a hand-typed date) then the win (one dated, branded PDF anyone can read). Clients CHANGELOG through changesets for slice 04. |

---

## Wave: DISCUSS / [REF] DoR Validation

| # | DoR item | Status | Evidence |
|---|---|---|---|
| 1 | Problem clear, domain language | WAIVED — clear, not validated | Each story opens with Elena's, Martin's, Priya's or Sabine's situation. DISCOVER G1 failed (one informant); the maintainer accepted the risk (D2). |
| 2 | Persona specific | PASS | Elena (consulting coach, Team Admin, Lightspeed), Martin (Viewer, Head of Delivery, monthly steering), Priya (Standalone on Linux), Sabine (sponsor, never opens Lighthouse), Jonas (no read). |
| 3 | 3+ domain examples, real data | PASS | Each story lists 3–5 examples with owner names, dates (29–30 Oct, 2 Nov, 20 Nov 2026) and values (21 → 12 days, −43%). |
| 4 | UAT G/W/T, 3–7 | PASS | US-01: 6, US-02: 4, US-03: 3, US-04: 5, US-05: 4. Each has an error or boundary path (could not load, stale tab, offline, no access, older Lighthouse, dash). |
| 5 | AC from UAT | PASS | AC-n.m per story trace to scenarios and D-decisions. |
| 6 | Right-sized | PASS, one at the edge | 01: 1d with 5 thin parts (taste test borderline); others ½–1d. |
| 7 | Technical notes | PASS | Inventory S1–S10; NFRs; DESIGN questions (export loading/failure look, own view, marks, fonts, macOS). |
| 8 | Dependencies tracked | PASS | 02, 03 on 01; 05 on 04 (or Q1 c); 04 independent of 01–03. Six PROPOSED decisions tracked as Q1–Q6; none blocks slice 01 except the label/place sketch at DISTILL. |
| 9 | Outcome KPIs | PASS | K1–K5 with targets, baselines, measurement; guardrails G1–G4; measurement caveat stated. |

Elevator Pitch check: all five stories carry Before / After (a UI action, an MCP question or an `lh` command, with the
observed output) / Decision enabled; no `@infrastructure` story. Every story has a `job_id`.

**DoR status: PASSED, with item 1 waived** (maintainer-accepted risk D2). PROPOSED decisions D13 (confidentiality
part), D19, D20, D22–D26, D28 wait for the maintainer; slice 01 can start DESIGN on the defaults.

---

## Wave: DISCUSS / [REF] Questions for the maintainer

1. **Q1 — How does the skill get a report's data?** Today no client reads reports (Epic 5878 excluded them).
   (a) Read-only tools in MCP and `lh` — list an owner's reports, read one — as slice 04 (~1 day, a clients release).
   (b) The skill tells the assistant to call the REST API directly — no client change, but it fails in web assistants
   and over MCP HTTP, bypasses the clients' sign-in and version handling, and the drift test cannot guard it.
   (c) The skill works from what the user supplies (the PDF from slice 01, or pasted values) — no client change, no
   live read. **Recommendation: (a)**, with the skill also accepting (c) as a fallback.
2. **Q2 — If printing does nothing in the macOS Standalone app** (unprobed): (a) Export PDF visible but disabled there
   with a one-line reason; (b) hidden there; (c) add a small native print call to the Standalone app (a further
   slice). **Recommendation: check in slice 01; if it fails, (a) now and (c) only when a macOS Standalone user asks.**
3. **Q3 — May the assistant add its own view to a report dashboard?** (a) No, the dashboard reproduces the report
   only; (b) yes, under its own labelled heading, apart from the numbers; (c) no rule. **Recommendation: (b)** — the
   report itself stays verdict-free (Epic 5878), and people ask assistants for a take anyway.
4. **Q4 — Ship the Lighthouse typeface with the app?** (a) For the whole app (also fixes the offline screen);
   (b) only for printing; (c) accept a fallback typeface offline. **Recommendation: (a)** in slice 02 (≤ 1 MB).
5. **Q5 — Slice order.** (a) PDF 01 → 02 → 03, then 04 → 05; (b) 04 → 05 first, as the cheap experiment; (c) 01, then
   04 → 05, then 02 → 03. **Recommendation: (a)** — the walking skeleton and the riskiest PDF assumption first.
6. **Q6 — Skill name.** `lighthouse-reports` (zip `lighthouse-reports-skill.zip`) or `lighthouse-report-dashboards`.
   Zip names become permanent once docs and the website link them. **Recommendation: `lighthouse-reports`** — it
   grows with each report type.

Non-blocking: the 3–5 user question from DISCOVER still stands (it feeds the DV-4 trigger and adds informants).

---

## Wave: DISCUSS / [REF] Handoff

To DESIGN (`nw-solution-architect`): this section, `slices/`, `discuss/wave-decisions.md`, the journey SSOT. Open
for DESIGN: the print view as a view of its own (D16) and how Export reaches it; the fresh read and its loading and
failure look (D19); forcing the light theme and hiding chrome (D17); marks independent of system fonts (D17); fonts
shipped (D22, after Q4); page-break rules for A4 and Letter; the Standalone macOS check and fallback (D28); for slice
04 the tool and command names and the clients' error relays; the usage event to DEVOPS.

## Wave: DISCUSS / [REF] Maintainer answers (2026-10-17)

These settle the questions above. Every PROPOSED default they touch is now decided.

| Q | Answer |
|---|---|
| Q1 How the skill gets a report's data | **(a) Read-only client tools in slice 04** (MCP and `lh`, Teams and Portfolios). The skill falls back to values the user pastes in. This reopens Epic 5878's "no reports in the clients" decision, for reading only. |
| Q2 macOS Standalone | **Assume printing works there and confirm it in production.** Only released macOS builds exist, with no devtools (a release Tauri webview is not inspectable), so there is no way to test before slice 01 ships the button. If it fails, the button is disabled or changed in a follow-up. The maintainer checks it once 01 is released. |
| Q3 The assistant's own reading | **Yes, kept apart.** It goes under its own labelled heading; the report's numbers stay exactly as Lighthouse shows them. |
| Q4 Lighthouse font | **Bundle Quicksand for the whole app** (slice 02, under 1 MB), with this condition: Quicksand is under the SIL Open Font License 1.1 (Google Fonts keeps it under `ofl/quicksand`). The licence allows bundling and redistributing it with software, commercial use included, as long as the licence text (`OFL.txt`) ships with the font, the font is not sold on its own, and it is not modified under its reserved name. DESIGN confirms the licence against the actual font files, and slice 02 ships `OFL.txt`. |
| Q5 Slice order | **01 → 02 → 03 → 04 → 05.** |
| Q6 Skill name | **`lighthouse-reports`**, so the zip is `lighthouse-reports-skill.zip`. |
| ADO `Premium` tag on Epic #6298 | **The maintainer changes it.** Epic fields and states are never touched from a session. |
