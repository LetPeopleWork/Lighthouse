# Slice 01 — Export a Then & Now report as a PDF

**Feature**: epic-6298-report-pdf-export · **Epic**: #6298 · **Story**: US-01 (ADO: to be created) · **Estimate**: ~1.5d (rev. 2026-10-19, was ~1d: the Reports list icon,
the print page's failed look with Retry and the Back destinations) ·
**Tier**: Community (D10) · **Walking skeleton**: yes · **Ships alone**: yes ·
`job_id: job-share-numbers-outside-lighthouse`

## Goal

Anyone who can read a Then & Now report (Team or Portfolio) chooses **Export PDF** on the report view, or the PDF
icon on its row in the Reports list, and saves, through the browser's
or the Lighthouse app's own print dialog, a light, branded, dated document that shows exactly the values on screen
and nothing of the app around them.

## IN

- **Export PDF** on the report view for every reader, an outlined button first in the action row (D14, NMD-1); shown
  disabled while the report loads; not offered when it could not be loaded, no longer exists, was made by a newer
  version, or has a type this version cannot show (PD-3, **PROVISIONAL — maintainer confirms**).
- **PDF icon in the Reports list** (NMD-2, the maintainer's explicit choice for this slice): first in the Actions
  column, tooltip "Export PDF", aria-label "Export {report name} as PDF", for every reader; Viewers get an Actions
  column holding only this icon; a row whose type this version cannot show gets no icon. It depends on the row's type
  only (PD-4, **PROVISIONAL — maintainer confirms**) and goes to the same print page.
- The print page's own states (NMD-4): "Preparing the PDF…"; a retryable failure "This report couldn't be loaded, so
  no PDF was made." with Retry, where a successful Retry prints once (PD-1, **PROVISIONAL — maintainer confirms**); a
  final failure with the report view's own sentence and no Retry.
- Back and the return after the dialog lead to where Export started: "‹ Back to report" or "‹ Back to Reports"; a
  final failure, and a print page opened from its address (PD-5, **PROVISIONAL — maintainer confirms**), lead to the
  Reports list.
- Export reads the report afresh, then calls print itself (release Standalone builds wire no print shortcut, D17).
  The screen shows the same fresh values (D19).
- Print layout (D17): light theme whatever the user's theme; no navigation, tabs, Edit report, Delete or app footer.
- Header in two lines (NMD-3): owner kind and name in the instance's terms above the report name; then "Then 5 Jul –
  2 Oct 2026 · Now 2 Aug – 30 Oct 2026"; then "Produced 30 Oct 2026, 09:15" in the viewer's local time (D13, D19).
- Body: the summary and every shown panel as on screen (a renamed panel reads the term alone, e.g. "Lead Time";
  PD-2, **PROVISIONAL — maintainer confirms**).
- Footer: the Lighthouse logo and "Made with Lighthouse by LetPeopleWork · letpeople.work" (D10, NMD-3); default file
  name "{report name} — {owner kind} {owner name}".
- Usage data: `ReportPdfExportStarted` with the report's type (DEVOPS), fired once per choice from **both entry
  points** — the report view's Export PDF button and the Reports list's PDF icon — before navigating; the print page
  itself reports nothing; a row with no icon reports nothing. Listed in `docs/settings/usagedata.md` in the same step.
- The print-designed report is a view a renderer could load on its own (D16 — the seam for #5882).

## OUT

Marks drawn independently of system fonts, shipped fonts, page-break rules (02) · "how to read this" (03) · client
reads and the skill (04, 05) · storing anything (D15) · branding removal (later Premium) · confidentiality line.

## Learning hypothesis

**Disproves "browser print of a print-designed view is good enough to hand over"** if the maintainer would not send
the PDF of the dev instance's busiest Team's report to a sponsor without editing it (cut panels, leftover chrome,
wrong date). Then the route needs more than a print layout before 02 polishes it.

## Data and dogfood moment

- Demo: Team Lightspeed (≈100 days). E2E walking skeleton (Page Object, demo data): create a 30-day report, choose
  Export PDF, the print view shows the header and no app chrome (print-media emulation; the dialog is not automated).
- Dogfood the same day on the dev instance (`:5169`, real history): export the busiest Team's report from Chrome and
  Firefox, and from the Linux Standalone app with a copy of the maintainer's data; attach the PDFs to the evidence.
- macOS Standalone: the maintainer tries Export PDF once slice 01 is released (Q2 answer). Slice 01 has no disabled
  state; if nothing happens there, a follow-up sketches and builds the fallback (NMD-7).

## DESIGN answered / DISTILL sketched (2026-10-17, 2026-10-19)

The print page of its own, with `WidgetBody`'s preparing and failed looks (NMD-4); light theme and hidden chrome on
that page. Place, label, list icon, printed page and states were sketched and locked with the maintainer (NMD-1..4).

## Acceptance criteria

AC-1.1 … AC-1.12 in `feature-delta.md` (US-01).

## Dependencies

None (Reports and Then & Now shipped with Epic 5878).
