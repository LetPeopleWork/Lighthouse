# Slice 02: Staleness Threshold + Visual Highlight + Portfolio Parity + Linear/CSV Coverage

**Feature**: time-in-state-and-staleness
**Stories shipped**: US-02, US-03, US-04
**Estimate**: ~2–3 crafter days
**Reference class**: similar to existing Team/Portfolio settings additions (RBAC enhancements pattern)

## Goal
Make stuck items visually obvious by adding a configurable threshold per Team and per Portfolio. Extend transition capture to Linear (source-of-truth) and CSV (sync-side fallback). Add the column to the Portfolio work-item view.

## IN scope
- Team entity: `StalenessThresholdDays: int` (default 7).
- Portfolio entity: `StalenessThresholdDays: int` (default 14).
- EF migration via the existing `CreateMigration` PowerShell script (CLAUDE.md mandates this).
- Settings forms — Team and Portfolio — integer input gated by `isTeamAdmin(teamId)` / `isPortfolioAdmin(portfolioId)` via `useRbac()`.
- Badge component: red colour treatment when `daysInState > threshold`, neutral otherwise.
- Portfolio work-item table: add the "Time in State" column (extending slice 01's column to a second surface).
- Linear connector: investigate `IssueHistory` GraphQL field; if it returns state transitions, use it (source-of-truth). If not, fall through to the sync-side fallback used by CSV.
- CSV connector: sync-side delta — on every file (re)load, compare current State to last-known State per work item; append a `WorkItemStateTransition` with `transitionedAt = syncTimestamp` if changed.

## OUT scope
- Per-state thresholds (locked D7 — one threshold per scope only)
- Changes to existing Cycle Time / Work Item Age semantics (locked D9)
- Backfill of historical transitions before this feature shipped (forward-only data)

## Learning hypothesis
**Confirms if it succeeds**: a single threshold per scope is enough; ≥20% of teams diverge from the default 7-day value within 4 weeks (signals: default isn't perfect, but tunability is used).
**Disproves if it fails**: customers ask for per-state thresholds within 4 weeks of release — would trigger a future "advanced staleness" feature.

## Acceptance criteria
See US-02, US-03, US-04 in `../feature-delta.md`.

Additional slice-level acceptance:
- Linear connector: transition data present for at least one real Linear team after first sync post-deploy.
- CSV connector: reloading a CSV with a changed `State` value generates a new `WorkItemStateTransition` row with `source = "csv-fallback"`.

## Dependencies
Slice 01 (this slice consumes the `currentStateEnteredAt` field and the `WorkItemStateTransition` table built there).

## Production data requirement
**Required.** Threshold setting persists and re-colours badges in the running app, observed during dogfood deploy.

## Dogfood moment
Set the Lighthouse team's threshold to 5 days; confirm a known item currently at `>5d in Active` flips colour without re-sync, and that the corresponding portfolio (if any) shows the same item correctly when its portfolio threshold is configured.

## Pre-slice spike candidates
- Linear: verify `IssueHistory` includes state transitions (15 min reading docs, 30 min one query call against a real workspace if access exists).
- Decide if Portfolio default threshold of 14 makes sense — could ask a customer or pick a defensible default and revisit via `OUT-staleness-threshold-tuning` KPI.
