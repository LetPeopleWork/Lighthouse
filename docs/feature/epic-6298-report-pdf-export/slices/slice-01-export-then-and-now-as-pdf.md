# Slice 01 — Export a Then & Now report as a PDF

**Feature**: epic-6298-report-pdf-export · **Epic**: #6298 · **Story**: US-01 (ADO: to be created) · **Estimate**: ~1d ·
**Tier**: Community (D10) · **Walking skeleton**: yes · **Ships alone**: yes ·
`job_id: job-share-numbers-outside-lighthouse`

## Goal

Anyone who can read a Then & Now report (Team or Portfolio) chooses **Export PDF** and saves, through the browser's
or the Lighthouse app's own print dialog, a light, branded, dated document that shows exactly the values on screen
and nothing of the app around them.

## IN

- **Export PDF** on the report view for every reader (D14, D20); unavailable while the report loads; not offered when
  it could not be loaded or no longer exists.
- Export reads the report afresh, then calls print itself (release Standalone builds wire no print shortcut, D17).
  The screen shows the same fresh values (D19).
- Print layout (D17): light theme whatever the user's theme; no navigation, tabs, Edit report, Delete or app footer.
- Header: owner kind and name in the instance's terms, report name, Then window, Now window, "Produced {date},
  {time}" in the viewer's local time (D13, D19).
- Body: the summary and every shown panel as on screen.
- Footer: Lighthouse and LetPeopleWork branding (D10; copy and logo sketched at DISTILL).
- Usage event chosen by DEVOPS (D27), fired when Export PDF is chosen; listed in `docs/settings/usagedata.md` in the
  same step.
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
- macOS Standalone: the maintainer tries Export PDF once; if nothing happens, D28 / Q2 applies.

## DESIGN must answer

Export's look while the fresh read is on its way and when it fails (no print, no stale numbers; reuse `WidgetBody`
states); how the print view is reached; how the light theme and hidden chrome are enforced.

## DISTILL must sketch first

Export PDF's place and label (reader alone; editor with Edit report and Delete); the printed page (header lines,
produced format, footer).

## Acceptance criteria

AC-1.1 … AC-1.10 in `feature-delta.md` (US-01).

## Dependencies

None (Reports and Then & Now shipped with Epic 5878).
