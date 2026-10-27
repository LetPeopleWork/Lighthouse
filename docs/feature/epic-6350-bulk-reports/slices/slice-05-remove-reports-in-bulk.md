# Slice 05 — Remove reports in bulk

**Feature**: epic-6350-bulk-reports · **Epic**: #6350 · **Story**: US-05 (ADO: to be created) · **Estimate**: 1d ·
**Tier**: Community (LOCKED, maintainer, 2026-10-27; Q1) · **Ships alone**: yes (on 01) ·
`job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go`

## Goal

Remove the reports selected on the Reports page (slice 01's selection) in one go, only where the user is Admin,
knowing which Then windows can no longer be rebuilt, with a per-report outcome.

## IN

- Turns on 01's selection column and "N selected" count, unless 07 shipped first.
- **Remove** acts on 01's selection. Enabled only when every selected report is on an owner where the user is Admin;
  otherwise disabled with "{n} of the {m} selected reports are on a Team or Portfolio you are not Admin of" (D15,
  LOCKED, NMD-2). Auth off or RBAC off: every report removable.
- Confirm dialog (NMD-7): every selected report listed as "Name — Owner"; when it applies, "1 of them has a Then that
  can no longer be rebuilt from the kept history; removing it loses it for good." (the count follows); [Cancel]
  [Remove 3 reports] naming the exact count.
- Result per report (D8, NMD-7), lines as NMD-12's table: a report whose owner the caller no longer administers is
  "Not removed: you are no longer Admin of {owner}." (not-admin); one already gone, including with its deleted owner,
  is removed (already-gone, never counted as a deletion in usage data); never a whole-run failure.
- A lost answer: "could not confirm what was removed", link to Reports (D8, AC-5.7).
- Shares the PDF's ceiling N = 50 (maintainer, 2026-10-27): above it, "Pick at most 50 reports" (AC-5.9; D16).
- The server re-checks Admin per report. `ReportDeleted` per report this run removed (never already-gone); one `ReportBulkRemovalStarted` per sent
  removal, name-only (DEVOPS).

## OUT

Bulk editing · removing through the clients · undo.

## Learning hypothesis

**Disproves "bulk removal is needed once bulk creation exists"** if, a month after release, no dogfood or opted-in
run uses it.

## Data and dogfood moment

Dev instance: remove the trial reports left by slices 02a–04's dogfood runs.

## DESIGN must answer

Bulk endpoint vs one call per report; what the page shows while removal runs and if the answer never arrives; N.
*Answered*: one request of report ids (DESIGN DDD-9), N = 50.

## DISTILL must sketch first

Remove's disabled state and reason; the confirm; the result and the could-not-confirm look.

## Acceptance criteria

AC-5.1 … AC-5.9 in `feature-delta.md` (US-05).

## Dependencies

Slice 01 only.
