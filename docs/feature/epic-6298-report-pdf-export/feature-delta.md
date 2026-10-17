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
