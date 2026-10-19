# Slice 02 — The report PDF reads the same on every machine

**Feature**: epic-6298-report-pdf-export · **Epic**: #6298 · **Story**: US-02 (ADO: to be created) · **Estimate**: ~1d ·
**Tier**: Community · **Ships alone**: yes (improves 01's PDF) · `job_id: job-share-numbers-outside-lighthouse`

## Goal

The same report exported on any machine — Chrome on Windows, Firefox on Linux, the Lighthouse app on Linux, an
instance with no internet — gives a PDF with the same marks, the Lighthouse typeface, every panel whole on its page
and no page holding only the footer.

## IN

- ✔ ✘ ± ▲ and the no-change mark in the print do not depend on fonts installed on the machine: they are drawn icons
  (D17; maintainer chose drawn icons on screen too, 2026-10-17).
- The Lighthouse typeface ships with the whole app and reaches the PDF without internet (D22; Q4 answered: whole app,
  with `OFL.txt`).
- No panel or summary split across pages; no trailing page with only the footer; A4 and US Letter portrait.
- On screen the marks become the same drawn icons as in the PDF; their meaning, colours and words are unchanged
  (AC-2.4, U-13).

## OUT

"How to read this" (03) · page numbers (not in v1) · landscape · charts (#6299).

## Learning hypothesis

**Disproves "own marks and shipped fonts make PDFs alike"** if the same report printed from Chrome on Windows,
Firefox on Linux and the Linux Standalone app differs in more than spacing (a missing or different mark, another
typeface, an extra page). Then small cross-browser differences are not small, and D16's tolerance needs revisiting
before #6299 adds charts.

## Data and dogfood moment

- Dev instance (`:5169`): the busiest Team's report with all four panels, printed on A4 and Letter from the three
  environments above; the PDFs compared side by side and attached to the evidence.
- Offline check: the frontend served with no internet access (or the Standalone app with networking off); the PDF's
  embedded fonts listed.
- E2E: no new walking skeleton; 01's E2E gains one check — the Chromium PDF of the print view embeds no font but the
  Lighthouse typeface (U-4).
- Not automatable (DoD 1): cross-machine marks and the page-break scenarios are manual evidence under DoD 7; the
  automated proxies are that embedded-font check and no mark characters left in the rendered output.

## Guardrails

G3 (identical marks and typeface across environments) · G4 (Standalone installer and Docker image grow ≤ 1 MB).

## Acceptance criteria

AC-2.1 … AC-2.4 in `feature-delta.md` (US-02).

## Dependencies

Slice 01 (the print layout). Q4 (fonts scope) answered 2026-10-17: the whole app.
