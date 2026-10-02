# Slice 01 — Reports tab: freeze a Then & Now report for a Team

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-01 · **Estimate**: ~1d (includes the migration) ·
**Tier**: Community · **Walking skeleton**: yes · `job_id: job-flow-coach-show-whether-flow-changed`

## Goal

A Team Admin creates a "Then & Now" report on a Team. Its Then window ends today and is frozen; the report shows
{Cycle Time} 50th / 70th / 85th percentile for Then (frozen) and Now (live). Readers see it, cannot create.

## IN

- **Reports** tab on Team detail after Metrics (D18), readable with Team read; role-specific empty state.
- **Create Report** (Team Write only, server-guarded; UI via `useRbac().isTeamAdmin`) → template card "Then & Now" →
  Then length 14 / 30 / 90 days ending today → defaulted name (D19) → Create → the report opens.
- Storage for reports: owner, template key, Then window, created date, name, selection, frozen claim payloads,
  settings snapshot (D26, D31). One expand-only migration via `CreateMigration`; cascades with the Team (Q5).
- The claim-definition seam with one registration, C1's values ({Cycle Time} percentiles); the Then series is frozen
  too, ready for slice 03.
- Report view: header "Then: 90 days to 2 Oct 2026 — frozen"; one row "{Cycle Time} 6 / 11 / 21 days → … days".
  Now = same length ending today (overlaps Then fully until slice 03 clips it).
- Usage data: route key `TeamDetail_Reports` on the existing `TeamTabOpened` (DEVOPS confirms); "report created"
  event if DEVOPS has designed it by then.

## OUT

Verdicts (03), past end dates (02), other claims (04, 05), Portfolio (06), Now choice (07), delete and cap (08),
settings notice (09; the snapshot is stored here).

## Learning hypothesis

**This disproves "freezing at creation is fast and simple enough"** if creating a 90-day report on the dev
instance's busiest Team takes more than 10 s. In that case freezing moves off the request before slice 04 multiplies
the claims frozen.

## Data and dogfood moment

- Demo: Team Lightspeed (≈100 days of history). E2E walking skeleton (Page Object, demo data): create a 30-day
  report, see the {Cycle Time} row.
- Dogfood the same day on the dev instance (`:5169`, real history): create a 90-day report on the busiest Team, time
  it, open it again the next day and check Then has not moved.

## Acceptance criteria

AC-1.1 … AC-1.6 in `feature-delta.md` (US-01).

## Dependencies

None.
