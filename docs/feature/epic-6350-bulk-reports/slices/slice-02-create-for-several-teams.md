# Slice 02a — Create a Then & Now for several Teams at once (the write path)

**Feature**: epic-6350-bulk-reports · **Epic**: #6350 · **Story**: US-02a (ADO: to be created) · **Estimate**: ~1d
(rev. after re-review 2026-10-27: Admin-set filter moved in from 02b, so 02a is releasable alone) ·
**Tier**: Premium · **Ships alone**: yes (on 01) · `job_id: job-lead-compare-every-owner-against-its-own-past-in-one-go`

> Split after peer review 2026-10-27: the old slice 02 is now 02a (this brief; the file keeps its name) and 02b
> (`slice-02b-owner-step-offers-admin-teams.md`).

## Goal

From the Reports page, a user creates the same Then & Now — same windows, metrics and name — for each picked Team in
one run, and sees per Team whether it was created or why not, including every refusal the server can give mid-run.

## IN

- **Create reports** on the Reports page; Premium. On Community shown disabled with the existing Premium-licence
  tooltip (`LicenseToolTip`; D17, LOCKED, NMD-4). From this slice the empty page shows NMD-1's full sentence,
  unchanged on Community (AC-1.5).
- Type-agnostic flow (D4): Type (Then & Now only) → the single wizard's Then, Now, Metrics & name steps, once → owner
  step → result. Default "Then ends on" per NMD-14: Now ends today, Then ends the day before Now starts, following the
  lengths until the user edits the date.
- Owner step: only the Teams the user administers (`TeamWrite`, via `useRbac()`); every Team with RBAC or auth off
  (D7, D18). Count on the button ("Create 3 reports").
- One name for every Team (D9, LOCKED); running again adds second copies, no de-duplication (D9's clause, LOCKED,
  NMD-10; Q11).
- The server checks each Team when it reaches it: not (or no longer) Admin, Team deleted, history too short, not saved,
  licence lapsed past the Community cap → one "Not created" row with its reason, never a whole-run failure. A
  run from a Community instance is refused as a whole with the licence reason.
- Result: one row per picked Team — Created (opens it) or Not created with the line NMD-12's table gives; cutoff
  refusals name the kept-history start and point to Closed Items Cutoff (D1).
- At most 50 owners per run (the shared ceiling N = 50, maintainer 2026-10-27): picking stays possible above it and
  the button names the count, but Create is disabled with "Pick at most 50 Teams and Portfolios." (NMD-13); a group's
  Select all is not capped; the server refuses the request (AC-2.12).
- A run where every picked Team is refused: headline "0 reports created, {N} not created", every row with its reason.
- A lost answer: "could not confirm which reports were created", link to Reports (D8).
- Usage data: `ReportBulkCreationStarted`, name-only, once per sent run, also for a run that created nothing and with
  no outcome property (D19, decided by DEVOPS), and `ReportCreated` per report created, as today.

## OUT

The no-Admin state and the owner step's loading and could-not-load looks (02b) · Portfolios (03) · telling before Create which Teams cannot take the report (04) ·
per-owner names · several windows in one run · bulk editing.

## Learning hypothesis

**Disproves "same windows for many Teams is what leads want"** if, after a run over the dev instance's Teams, the
maintainer edits most created reports, or more than half the Teams are skipped.

## Data and dogfood moment

Dev instance (`:5169`): a quarter run (Then 90 days ending the day before Now starts, Now 90 days) and a month run over
every Team; time it (K3). E2E: two demo Teams with the Premium licence fixture.

## DESIGN must answer

The Admin set per user (corrected after DESIGN: `useRbac()` already exposes it) and how `useRbac()` exposes it; bulk endpoint with per-owner
outcomes vs one call per Team; per-owner checks when each Team is reached; Premium and a
mid-run lapse on the server; time budget and progress for 20–30 Teams (10 s slow log); the result's loading and
could-not-confirm looks.

## DISTILL must sketch first

Where Create reports sits and its Community state; the owner step; result wording for each refusal and the
all-refused headline. The sketch walk settled D9's clause, D10 and D17 (NMD-10, NMD-9, NMD-4, LOCKED 2026-10-27).

## Acceptance criteria

AC-2.1 … AC-2.12 in `feature-delta.md` (US-02a).

## Dependencies

Slice 01 (the page it starts from).
