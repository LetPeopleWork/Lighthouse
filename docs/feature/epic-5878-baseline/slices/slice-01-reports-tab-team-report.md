# Slice 01 — Reports tab: freeze a Then & Now report for a Team

**Feature**: epic-5878-baseline · **Epic**: #5878 · **Story**: US-01 (ADO #6159) · **Estimate**: ~1d (includes the migration) ·
**Tier**: Community · **Walking skeleton**: yes · `job_id: job-flow-coach-show-whether-flow-changed`

## Goal

A Team Admin creates a "Then & Now" report on a Team. Its Then window ends today and is frozen; the report shows a
"{Cycle Time}: Then & Now" panel — Then's 85th percentile (frozen) on the left, Now's 85th / 70th / 50th percentile
(live) on the right with the change and % change, coloured by whether it is better or worse. Readers see it, cannot
create.

> Revised 2026-10-03 for the maintainer's decisions: a panel, not a row with a verdict to come (D39); Now saved on the
> report and allowed to overlap Then (D43, D38); Team presets 30 / 90 days (D37).

## IN

- **Reports** tab on Team detail after Metrics (D18), readable with Team read; role-specific empty state.
- **Create Report** (Team Write only, server-guarded; UI via `useRbac().isTeamAdmin`) → template card "Then & Now" →
  Then length 30 / 90 days ending today → defaulted name (D19) → Create → the report opens.
- Storage for reports: owner, template key, name, Then window, Now length (= Then length for now, D43), created
  date, selection, frozen per-metric values with their sample sizes (D26, D45); nothing about settings (D46). One expand-only migration via
  `CreateMigration`; cascades with the Team (D42).
- The panel registry with one registration, the {Cycle Time} panel (D39); the direction of good (down) lives only in
  the frontend panel.
- Report view: header "Then: 90 days to 2 Oct 2026 — frozen"; the panel: Then 85th 21 days │ Now 85th / 70th / 50th,
  change and % change on the 85th; lower = green, higher = red, equal = neutral, the sign always shown. Now = the
  saved length ending today, may overlap Then (D38).
- Every percentile with its sample size ("· 3 Work Items"); "—" with a reason only when no {Work Item} finished in
  the window; change and % then show "—" (D45).
- Usage data: route key `TeamDetail_Reports` on the existing `TeamTabOpened` (DEVOPS confirms); "report created"
  event if DEVOPS has designed it by then.

## OUT

Points beyond Then's limits (03), past end dates and custom lengths (02), other panels (04, 05), Portfolio (06),
choosing Now's length (07), delete and cap (08), Edit report (10). (Slice 09 is dropped, D46.)
No verdicts, text or charts at all (D39).

## Learning hypothesis

**This disproves "freezing at creation is fast and simple enough"** if creating a 90-day report on the dev
instance's busiest Team takes more than 10 s. In that case freezing moves off the request before slice 04 multiplies
the metrics frozen.

## Data and dogfood moment

- Demo: Team Lightspeed (≈100 days of history). E2E walking skeleton (Page Object, demo data): create a 30-day
  report, see the {Cycle Time} panel.
- Dogfood the same day on the dev instance (`:5169`, real history): create a 90-day report on the busiest Team, time
  it, open it again the next day and check Then has not moved.

## Acceptance criteria

AC-1.1 … AC-1.7 in `feature-delta.md` (US-01).

## Dependencies

None.
