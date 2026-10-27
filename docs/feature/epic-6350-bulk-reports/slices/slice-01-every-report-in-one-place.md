# Slice 01 — Every report in one place (walking skeleton)

**Feature**: epic-6350-bulk-reports · **Epic**: #6350 · **Story**: US-01 (ADO: to be created) · **Estimate**: ~1.5d
(rev. 2026-10-27 after review: was 1d; row selection moved here) · **Tier**: Community · **Ships alone**: yes ·
`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go`

## Goal

A top-level Reports page lists every report the user can read across all Teams and Portfolios — name, Type, owner,
created day — opens each in the existing report view, and lets the user select rows, so slices 05 and 07 each depend
on this slice only.

## IN

- A Reports entry in the top navigation (beside the Features page precedent) opening a new page (address DESIGN's).
- A table in the `DataGridBase` / `ReportList` style: Name, Type, owner (kind in the instance's term + name), Created;
  the grid's own sorting and filtering.
- A row opens the existing report view at its owner address.
- Only reports on owners the user can read, filtered by the server (read scope as `/features` uses).
- Row selection (D15, D16 — PROPOSED, confirm at DISTILL sketch walk): every listed row selectable; the exact count
  shown; changing a filter clears the selection; sorting keeps it; select-all takes every row the filter shows, on all
  pages. Built and tested here, but **not shown**: no checkboxes and no "N selected" until the first slice with an
  action on the selection (05 or 07) ships and turns them on.
- Loading look; could-not-load look with Retry; empty look pointing to the owners' Reports tabs; never stale rows
  shown as current (keyed query).
- A report of a type this version cannot show is listed and opens to the existing look.
- Usage data: the page-opened event (D14; DEVOPS designs; listed in `docs/settings/usagedata.md` here).

## OUT

Create reports (02a), Remove (05), Export PDF of the selection (07) · row Edit/Delete/PDF icons on this page (the
DISTILL sketch decides whether to reuse them) · changes to per-owner tabs or Overview chips.

## Learning hypothesis

**Disproves "one list ends per-owner hunting"** if, on the dev instance (`:5169`), the maintainer still goes through a
Team's tab to find a known report, or the page is not ready within 2 s.

## Data and dogfood moment

Dev instance with every Team and Portfolio; reports created by hand beforehand on several owners. E2E on demo data
(created in the test).

## DESIGN must answer

Cross-owner read: new endpoint vs fan-out over per-owner endpoints; owner names in the payload; loading and failure
looks (`WidgetBody` states are the reuse candidate); selection across pages in `DataGridBase`.

## DISTILL must sketch first

Navigation entry and label; columns and their order; the three non-loaded looks. (The selection column and count are
sketched with 05 or 07, whichever ships first.) The sketch walk is a hard gate for the PROPOSED D15/D16.

## Acceptance criteria

AC-1.1 … AC-1.10 in `feature-delta.md` (US-01).

## Dependencies

None in this Epic. Reports (Epic 5878) on `main`.
