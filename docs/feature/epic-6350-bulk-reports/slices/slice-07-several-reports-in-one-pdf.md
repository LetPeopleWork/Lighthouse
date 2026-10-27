# Slice 07 — Several reports in one PDF

**Feature**: epic-6350-bulk-reports · **Epic**: #6350 · **Story**: US-07 (ADO: to be created) · **Estimate**: 1d ·
**Tier**: Community · **Ships alone**: yes (on 01) · `job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go`
(carries `job-share-numbers-outside-lighthouse`)

## Goal

Select reports on the Reports page and export them as one PDF, each report on its own page(s) exactly as its own
Epic 6298 PDF shows it — the leadership "big picture" in one file.

## IN

- Turns on 01's selection column and "N selected" count, unless 05 shipped first.
- Export PDF acts on slice 01's selection, for any reader, including reports on owners the user only reads (D15).
- One print document; each report starts on a new page with Epic 6298's header, body and footer, its values read for
  this export (reuse `ReportPrintContent`).
- Order: the table's current order (D12, LOCKED, maintainer, 2026-10-27).
- Any report that cannot be read → no PDF; the page names it, with Retry and "Leave it out" (D12, LOCKED, maintainer,
  2026-10-27; Q3).
- The action names the exact count; above the ceiling N = 50 (maintainer, 2026-10-27; drops to 25 if a 50-report print fails in the Linux desktop app): "Pick at most 50 reports" (D16; removal shares N — confirm
  at DISTILL sketch walk).
- Preparing and failed looks as the single print page.
- macOS desktop app: disabled with Bug 6425's reason (on `main`); this slice adds no macOS handling of its own.
- Usage data (DEVOPS): `ReportCombinedPdfExportStarted`, name-only, once per click with two or more selected;
  exactly one selected counts `ReportPdfExportStarted` with its type; Retry and Leave it out count nothing.

## OUT

Server-side rendering, scheduled sending (#5882) · a cover page or table of contents (DISTILL may propose) · export
from the clients.

## Learning hypothesis

**Disproves "one combined PDF replaces the script for leadership"** if the maintainer, given the dev instance's 90-day
reports, would rather send separate PDFs or a spreadsheet.

## Data and dogfood moment

Dev instance: every Team's quarter report in one PDF from Chrome and the Linux desktop app.

## DESIGN must answer

A print page holding several reports (address, page breaks, fonts loaded once); the ceiling N (answered: 50); reads in parallel or in
turn; preparing and failure looks.

## DISTILL must sketch first

The selection's Export PDF placement and count; the ceiling message; the failure page (Retry / Leave it out).

## Acceptance criteria

AC-7.1 … AC-7.8 in `feature-delta.md` (US-07).

## Dependencies

Slice 01 only; Epic 6298's print route (on `main`); Bug 6425's macOS disabling (on `main`).
