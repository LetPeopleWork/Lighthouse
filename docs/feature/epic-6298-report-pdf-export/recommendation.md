# Recommendation — epic-6298-report-pdf-export (DIVERGE → DISCUSS)

**Agent**: Flux (`nw-diverger`) · **Date**: 2026-10-17 (revision 1, after peer review round 1) · Derived from
`diverge/taste-evaluation.md`. DISCOVER's decisions D1–D9 are taken as given. The maintainer's 2026-10-17 input on
deployment scope (Standalone may be left out) is applied as a decision-in-principle for DISCUSS to confirm (DV-2 in
`wave-decisions.md`).

## Decision for DISCUSS

> **Proceed with Option 4, "Browser-printed PDF" (4.40 / 5, tied with Option 1 and chosen by the tie-break rule in
> `diverge/taste-evaluation.md` §4: higher Desirability + Viability, 8 against 7).** Every report gets an **Export
> PDF** action that opens a print-designed version of the report. Its header names the Team or Portfolio, says
> "produced {date}", shows the Then and Now windows and gives a one-line "how to read this". The user saves it
> through the browser's own Save as PDF, or through the webview's print in Standalone. It covers server zips,
> Docker and Standalone at about 0 MB, with no server engine. **First payload: Then & Now.** **Licence (to confirm
> with the maintainer): the PDF is Community, within the existing 2-reports-per-owner cap, and carries a "Made with
> Lighthouse" footer; Premium removes the footer or adds the customer's own branding.**
> This assumes senders accept one OS print dialog, and that the outside readers worth reaching first are those
> of a Then & Now (risk R-A below).

The print-designed report is the seam. When Scheduled Report Delivery (#5882) needs unattended rendering, a
server-side renderer can load **the same view**, on server and Docker only. That is a later step, not an
alternative. The rendering mechanism, in both steps, is DESIGN's decision.

The tie-break rule was set in revision 1, after the tie appeared. It reads the Viability score already in the
matrix and changes no score or weight.

**Entry condition for DESIGN**: the Standalone "≈0 MB" rests on a desk check in the spike (Tauri and WebKit
print-to-PDF were not run). A short probe must print one Then & Now from the Standalone app on macOS and Linux
before DESIGN starts. It is also the one piece of evidence that could turn the tie into a lead (S3: 4.50 vs 4.40).

## 1. Top 3

### Option 4 — Browser-printed PDF — 4.40 (wins the tie)

- **Why it scores well**: the highest DVF (4.00). A file reaches every reader, including those who cannot open
  Lighthouse. It is dated and self-explaining (OS3, OS6), and it is the cheapest PDF path in every distribution.
  It scores 5 on Subtraction and 5 on Concept count: one button, one universally known document. Charts from
  Custom Metric Reports (#6299) print the same way with no new mechanism, because the browser draws them (C4).
- **Core trade-off**: one OS print dialog stands between the click and the file (T3 4, T4 4). No unattended
  rendering, so #5882 still needs a server renderer later. A PDF going into a slide is one more step than an image.
- **Key risk (R-A)**: the first payload, Then & Now, is not what people capture today (X2). The PDF may sit
  unused until chart-bearing reports exist.
- **Who it is for**: a coach or team lead who hands a sponsor or client one document that must read on its own,
  weeks later, with no Lighthouse access.

### Option 1 — Slide image (with copy as text) — 4.40 (loses the tie)

- **Why it scores well**: the observed workaround made honest. Copy a panel or a chart as an image with a dated
  caption, and paste it into the slide. It scores 5 on Concept count, Progressive disclosure and Speed, all
  front-end, and works in Standalone.
- **Core trade-off**: per-panel pieces lose the report's story (OS6). It does not build the report-as-share
  capability (D6) or anything #5882 can reuse (V 3).
- **Key risk**: it puts images on interactive chart widgets, which pulls sharing back towards the exploring
  surface. The maintainer's direction is for reports to be the surface you share.
- **Who it is for**: a team lead assembling a recurring steering deck out of single charts.

### Option 5 — Server-rendered chart report (server and Docker only) — 4.00

- **Why it scores well**: one click, no dialog (T3 5). Its first payload is a chart-bearing report, closest to
  what people capture. It is the engine #5882 needs, and Premium scheduled delivery could ride on it. Standalone
  is left out (maintainer input); covering it would cost +99–120 MB per OS plus notarising.
- **Core trade-off**: +95–110 MB Docker pull, the Helm memory limit raised to about 1.5 GiB, about 120 MB
  first-use download on server zips, 18 Linux libraries, and Chromium without a sandbox restricted to
  Lighthouse's own origin (spike #6052). It waits for #6299 and is slow cold (T4 3).
- **Key risk**: paying the whole runtime cost before #5882 exists, and making this Epic follow #6299.
- **Who it is for**: an operator who wants PDFs of chart reports produced by the server, uniformly, with no
  browser in the loop.

## 2. Dissenting case

The matrix **ties** Options 4 and 1 at 4.40. The single-point tests split three to three:
- **Option 1 leads** when speed is weighted as for a developer tool (S1: 4.52 vs 4.35), when nobody shares a
  Then & Now (S2), and when an image counts as "the report capability" (S4).
- **Option 4 leads** when its print route proves as small as desk-checked (S3), when self-explanation weighs more
  (S6), and when clipboard images fail in the WebKit webviews (S7).

Option 1's case: people already paste screenshots of metrics charts into slides (#1822, maintainer 2026-10-17). A
dated caption on that same image serves OS1 and OS3 today, without waiting for a chart-bearing report.

**Why it loses the tie**: it scores lower on Viability (3 vs 4). It serves the *observed* workflow but not the
job's under-served self-explanation (OS6). And it grows sharing from the interactive charts, against the stated
product direction that reports are the surface for sharing (D6). Option 4 builds the capability once for every
report type (C1). This is a direction call made by a stated rule, not a margin in the scores.

**Pre-committed trigger**: if the maintainer's 3–5 user question ("the last time you showed Lighthouse numbers to
someone else, what did you send?") shows that most artefacts are **single charts pasted into slides**, do not swap
directions. Add "Copy as image" with the same dated caption to **report panels** as a follow-up slice or Epic. It
is front-end only, it reuses the print header's caption data, and it keeps sharing on reports.

## 3. Answers to the open questions (for DISCUSS to confirm)

| DISCOVER Q | Answer from this wave |
|---|---|
| Q1 Mechanism | Rendering path, not mechanism: **the user's browser (or the Standalone webview) prints a print-designed view of the report**. No server renderer until #5882; when it comes, it is server and Docker only. Route, stylesheet, engine and fonts are **DESIGN's decision**. |
| Q2 PDF vs other forms | PDF first (Option 4). Slide image ties and is the dissent with a trigger. Slide deck (3.70), MCP report access (3.30) and the snapshot link (3.05) trail. The AI path is a complement (the out-of-scope Lighthouse dashboard skill). A no-sign-in link has no precedent here and fails readers outside the instance's network. |
| Q3 First payload | **Then & Now** (pool F1 3.95). The runner-up, "wait for #6299" (3.60), would make this Epic follow another. The chart payload (Option 5) and the Delivery payload (Option 6) lose on cost and on direction. |
| Q4 Licence | **Community PDF with a "Made with Lighthouse" footer; Premium removes it or brands it**, on top of the existing 2-reports-per-owner cap. L3 and L4 tie at 3.90 on base weights; L3 leads under the reversibility-heavy weights that reflect the maintainer's caution, and the two combine. This replaces DISCOVER D4's "Premium until extras exist", because the footer is a day-one extra. **The maintainer decides.** |
| Q5 Downloaded vs stored | Downloaded, nothing stored. Stored PDFs (retention, RBAC on files, migration) arrive with #5882. |
| Q6 Snapshot honesty | Header: Team or Portfolio, "produced {date and time}", the Then and Now windows. Now as computed for the report at export (C10). Wording is DISCUSS's. |
| Q7 Anonymising | Out of v1; a candidate Premium extra. Then & Now carries aggregates only. |
| Q8 Who exports | Anyone who may read the report (exporting is reading what is already shown); confirm through RBAC (C8). |
| Q9 What leaves | Then & Now carries aggregates plus the Team or Portfolio name, and no Work Item titles. An optional confidentiality line in the header is a DISCUSS item. The question becomes live with chart reports (#6299) that list Work Items. |

**Deployment scope (maintainer input 2026-10-17)**: the recommended option covers Standalone at about 0 MB (desk
check, probe before DESIGN), so it does not need the relaxation. The relaxation applies when a server renderer
arrives with #5882: Standalone keeps browser print and does not get the server renderer. If DISCUSS ever leaves out
Standalone, how a Standalone user learns the export is unavailable (hidden, or disabled with a reason) is DISCUSS's
or DESIGN's call.
