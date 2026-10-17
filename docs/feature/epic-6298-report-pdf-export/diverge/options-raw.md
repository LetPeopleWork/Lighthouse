# Options (raw) — epic-6298-report-pdf-export (DIVERGE, Phase 3)

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-17. Generation only: this file contains no scoring and no
ranking. Evaluation is in `taste-evaluation.md`.

**Fixed frame, shared by every option**: the existing Reports tab and Then & Now (Epic 5878). Values come from the
report's settled values, not read back off the screen (ADR-172, C10). The output names the Team or Portfolio, when
it was produced and the report's windows (C6), and uses the instance's configured terms (C7). Who may share is
decided through RBAC (C8).

**Deployment scope (maintainer input, 2026-10-17)**: the output does not have to be available in every deployment
model. Leaving out Standalone is acceptable if it makes the feature cheaper or simpler. Each option states its
coverage of server zips, Docker/Helm and Standalone, and what Standalone coverage costs. The rendering path named
for each option is the **cheapest viable** one, so that cost can be scored. The final mechanism is a DESIGN
decision.

## 1. HMW question

> How might we let a reader outside Lighthouse understand and act on numbers a Lighthouse user already trusts,
> without the sender rebuilding them and without the reader mistaking when they were taken?

No carrier named: the answer can be a file, an image, a link, text or data.

## 2. SCAMPER options

### S — Substitute (the carrier): a slide-ready image per panel or chart

**Core idea**: every report panel, and every metrics chart and forecast widget, gets "Copy as image" and
"Download PNG". The image includes a caption strip with the Team or Portfolio, the window and "taken {date}", ready
to paste into a slide.
**Key mechanism**: the browser turns the panel's own SVG and markup into a PNG on a canvas and puts it on the
clipboard. No server rendering.
**Cheapest rendering path**: in-browser (SVG serialised → canvas → PNG). Charts are the frontend's own components,
so they look the same as on screen (C4).
**Distributions**: server zips ✔, Docker/Helm ✔, Standalone ✔ (≈0 MB; clipboard image support in the WebKit
webviews on macOS and Linux needs checking, with download as the fallback).
**First payload**: what people capture today: metrics charts and forecast/Delivery widgets, plus Then & Now panels.
**Key assumption**: the reader's carrier is a slide deck the sender builds anyway; the sender wants pieces, not a
finished document.
**Closest competitor**: ActionableAgile's per-chart image export.

### C — Combine (with the adjacent AI-dashboard job): the report over MCP

**Core idea**: a report (its frozen Then values, Now values, windows, generated-at time and terms) becomes
readable through Lighthouse's MCP server and CLI. The user's assistant builds the slide, page or deck. Lighthouse
renders nothing.
**Key mechanism**: a report read endpoint plus an MCP tool or resource in Lighthouse-Clients. The payload carries
"as of" and windows as first-class fields, so any assistant output can quote them.
**Cheapest rendering path**: none in Lighthouse. The assistant renders.
**Distributions**: server zips ✔, Docker/Helm ✔, Standalone ✔ where an MCP client reaches the local backend
(desk assumption, not checked). Cost: the clients' versioning and release cycle.
**First payload**: Then & Now (numbers only, no charts).
**Key assumption**: senders already work in an AI assistant and would rather compose than export. Their
organisation allows sending report data to a model provider.
**Closest competitor**: Claude artifacts published from MCP data (`competitive-research.md` §2).
**Complement, out of scope**: the maintainer's Lighthouse dashboard skill (`story-6217-lighthouse-skills`) would
sit on top of this.

### A — Adapt (from BI "publish to web"): a read-only snapshot link

**Core idea**: "Share link" freezes the report as it is now and gives an unguessable URL on the instance. Anyone
holding the link sees a read-only, dated page with no sign-in until it expires or is revoked.
**Key mechanism**: a stored snapshot (values, windows, generated-at) with a link token, an expiry, a revoke list
and an unauthenticated read route. The existing SPA renders it.
**Cheapest rendering path**: the SPA itself. No PDF engine.
**Distributions**: server zips ✔ and Docker/Helm ✔, **only where the reader can reach the instance**. Standalone ✘
(no reachable address), at zero cost to leave out.
**First payload**: Then & Now snapshot.
**Key assumption**: outside readers can reach the instance over the network, and the operator accepts data shown
without sign-in.
**Closest competitor**: Power BI publish to web; Jira public dashboards; Appfire Dashboard Hub links.

### M — Magnify (the snapshot's honesty and readability): a print-designed report through the user's browser

**Core idea**: "Export PDF" opens a print-designed version of the report, with a header carrying the Team or
Portfolio, "produced {date}", the Then and Now windows, a one-line "how to read this" and an optional
confidentiality line. It then opens the browser's (or Tauri's) Save-as-PDF.
**Key mechanism**: a dedicated print route with a print stylesheet (page breaks per panel, no interactive
controls, fonts shipped with the app), then `window.print()`. Mechanism detail is a DESIGN decision; this names
the cheapest path only.
**Cheapest rendering path**: browser print-to-PDF; in Standalone, the Tauri webview's print. No server engine.
**Distributions**: server zips ✔, Docker/Helm ✔, Standalone ✔ (≈0 MB; WebKit text rendering differs slightly on
macOS and Linux). No unattended rendering: Scheduled Report Delivery (#5882) needs a server engine later.
**First payload**: Then & Now. The same print route carries charts later, because the browser draws them.
**Key assumption**: the sender is at a browser when sharing, and one OS print dialog is an acceptable step.
**Closest competitor**: none of those researched offers this exact form. Power BI's PDF export is the nearest
document (server-rendered).

### P — Put to other use (serve the unattended recipient too): a server-rendered chart report, server and Docker only

**Core idea**: "Export PDF" asks the server to render a chart-bearing report and returns the file. The same engine
later serves Scheduled Report Delivery (#5882). Standalone does not offer the export.
**Key mechanism**: a backend render service that loads a print route in a headless browser and returns
`page.pdf`, authenticated by a short-lived render token restricted to Lighthouse's own origin. Mechanism detail is a
DESIGN decision; this names the cheapest path only.
**Cheapest rendering path**: headless Chromium (PuppeteerSharp + chrome-headless-shell). Docker gets
+95–110 MB pull and a higher Helm memory limit. Server zips download about 120 MB on first use (or use Edge on
Windows), and minimal Linux hosts install 18 system libraries. Gotenberg reaches Docker and Helm only and so misses
the server zips. In-process Jint + HarfBuzz + Svg.Skia renders charts but has no HTML layout for the report body.
**Distributions**: server zips ✔ (with the Linux libraries caveat), Docker/Helm ✔, **Standalone ✘** under the
maintainer's 2026-10-17 input. Covering Standalone would cost +99–120 MB per OS plus notarising a second engine.
**First payload**: a chart-bearing report, the first Custom Metric Report (#6299). This Epic follows #6299.
Then & Now prints on the same engine.
**Key assumption**: what people capture today (metrics charts) is what they want to share, and unattended delivery
(#5882) follows soon enough to carry a server engine. Standalone users keep using screenshots.
**Closest competitor**: Power BI export to PDF (server-rendered).

### E — Eliminate (the document): copy the report as text

**Core idea**: "Copy summary" puts a short, dated, plain-language text of the report on the clipboard, for an
email, a chat message or a document: "{Team}, Then {window} → Now {window}, produced {date}: {Cycle Time} 85th
12 → 7 days (−42%) …".
**Key mechanism**: a text formatter over the settled report values, using Terminology. No rendering.
**Cheapest rendering path**: none (string formatting in the browser).
**Distributions**: all ✔, including Standalone, at 0 MB.
**First payload**: Then & Now.
**Key assumption**: the reader reads numbers in an email or chat, and the visuals add little.
**Closest competitor**: none researched; the sender types it today.

### R — Reverse (Lighthouse goes to the reader's carrier): a slide deck (PPTX) export

**Core idea**: "Export slides" produces a PowerPoint file: a title slide with the Team or Portfolio, "produced
{date}" and the windows, then one editable slide per report panel with native tables and text. The sender edits it
and drops it into their own deck.
**Key mechanism**: a slide writer over the settled report values. Charts later go in as images drawn by the
browser.
**Cheapest rendering path**: in the browser with an MIT slide library (for example PptxGenJS), or in the backend
with the Open XML SDK (MIT). No PDF engine.
**Distributions**: all ✔, including Standalone (a few hundred KB of script, or a managed library in the backend).
**First payload**: a Delivery and its Features with their forecasts (what is captured today besides charts),
from the same settled table the Delivery CSV export already builds (ADR-172). Then & Now follows.
**Key assumption**: the outside reader's carrier is the sender's slide deck, senders want to edit before sending,
and forecasts are the numbers most often taken to a steering meeting.
**Closest competitor**: Power BI export to PowerPoint; Claude Slides export.

## 3. Crazy 8s supplements

| # | Option | Note |
|---|---|---|
| X1 | **Presenter view**: a full-screen, chrome-free report view for screen sharing in a steering meeting | The reader sees it live; no artefact |
| X2 | **Data pack**: report values as CSV or XLSX, with a metadata sheet (date, windows, terms) | CSV export of forecasts is out of this Epic (DISCOVER); the Delivery CSV exists (ADR-172) |
| X3 | **Anonymised Flow Analysis hand-over**: a file of aggregates with Team and Work Item names replaced | #4753 "Option: Anonymise"; DISCOVER Q7 |
| X4 | **Email it from Lighthouse**: send the report to an address from the report page | Needs an email channel; overlaps #5882 |

## 4. Curated 6

| # | Option | From | Share form | First payload | Rendering path (cheapest viable) | Standalone |
|---|---|---|---|---|---|---|
| 1 | **Slide image** (with copy-as-text) | S + E merged | PNG and clipboard text per panel or chart | Metrics charts, forecast widgets, Then & Now panels | In-browser SVG → canvas | ✔ ≈0 MB |
| 2 | **Report over MCP** | C | Data to the user's assistant | Then & Now | None in Lighthouse | ✔ desk |
| 3 | **Snapshot link** | A | Read-only, no-sign-in URL | Then & Now | The SPA | ✘ (no address), 0 to leave out |
| 4 | **Browser-printed PDF** | M | PDF file | Then & Now (charts later by the same route) | Print route + browser or Tauri print | ✔ ≈0 MB |
| 5 | **Server-rendered chart report** | P | PDF file | A chart-bearing Custom Metric Report (#6299) | Headless Chromium against a print route | ✘ (covering it: +99–120 MB per OS) |
| 6 | **Slide deck** | R | PPTX file | A Delivery with its Features' forecasts | In-browser PptxGenJS or backend Open XML SDK | ✔ small |

First payloads across the 6: Then & Now (2, 3, 4), existing metrics chart widgets (1), a chart-bearing report (5),
forecasts and Deliveries (6).

### Diversity test (mechanism · assumption about the user · cost profile)

| Pair | Mechanism | Assumption | Cost |
|---|---|---|---|
| 1 vs 4 | canvas image of one panel vs whole-document print route | sender assembles pieces vs sender sends one finished document | front-end only, per panel vs print route + stylesheet |
| 1 vs 6 | raster image vs native editable slide objects | pastes into an existing deck vs takes a generated deck | canvas helper vs slide writer + layout per panel |
| 2 vs all | data over a protocol; no Lighthouse rendering | the sender works in an assistant and composes | endpoint + clients release; no renderer |
| 3 vs all | live hosted read-only view behind a token | reader can reach the instance; operator allows no-sign-in | storage, token lifecycle, unauthenticated route, security review |
| 4 vs 5 | user's browser prints vs server-side headless browser | sender is at a browser vs unattended rendering is near | ≈0 vs +95–110 MB image, memory limit, Linux libraries, render token |
| 5 vs 6 | server PDF engine vs slide writer | unattended or recurring use vs edit-before-send | heavy runtime vs light library |

Every pair differs on all three counts. **All 6 pass.** Options 4 and 5 share the *print route* and differ in
*who prints*. That is the mechanism question DISCOVER left open (Q1), so the two are kept apart on purpose.

## 5. Eliminated or merged

| Option | Outcome | Reason (structural, not evaluative) |
|---|---|---|
| E Copy as text | **Merged into 1** | Same mechanism (browser-side, clipboard), same assumption (the sender pastes into their own carrier), same cost profile (front-end only). It becomes a second clipboard format of Option 1. |
| X1 Presenter view | Set aside | This is today's behaviour (open the report and share the screen). It produces nothing that reaches a reader without access, so it is the baseline rather than an option. |
| X2 Data pack | Set aside | Out of this Epic by DISCOVER (CSV of forecasts deferred). Same "data, not picture" assumption as Option 2 with a file carrier. |
| X3 Anonymised hand-over | Set aside as a modifier | It is a property any option can carry (DISCOVER Q7, Q9), not a share form. Recorded for DISCUSS. |
| X4 Email from Lighthouse | Set aside | It is the scope of Scheduled Report Delivery (#5882), which depends on an email channel. |

## 6. Side pools (generated here, scored in `taste-evaluation.md`)

DISCOVER left two decisions open that cut across every option. They are listed here as raw candidates.

**First payload (Q3)**. The curated options already carry four different payloads. This pool cross-checks the
payload on its own, so that DISCUSS can pair any payload with the chosen share form.

| # | Candidate |
|---|---|
| F1 | Then & Now only (frozen, no charts) |
| F2 | Then & Now plus the existing metrics chart widgets (the observed capture) |
| F3 | Wait for Custom Metric Reports (#6299) and launch with a chart-bearing report |
| F4 | Forecasts and Deliveries first |

**Licence line (Q4)**

| # | Candidate |
|---|---|
| L1 | Premium now; flip to Community when Premium extras exist (DISCOVER D4 as stated) |
| L2 | Community from the start; Premium extras later (scheduled delivery, branding, anonymising, stored history) |
| L3 | Community with a "Made with Lighthouse" footer; Premium removes it or adds own branding |
| L4 | Community per report up to the existing report cap; Premium unlimited (rides the 2-reports-per-owner cap) |

## DIVERGE gate G3

- [x] HMW without an embedded solution.
- [x] All 7 SCAMPER lenses applied; 4 Crazy 8s supplements.
- [x] 6 curated options; every pair passes the 3-point diversity test.
- [x] Options differ on share form (6 forms) and on first payload (4 payloads).
- [x] No evaluative language in the options.

**DIVERGE-G3: PASS.**
