# Slice 02: Every chart's dialog brings the columns that explain its point

**Goal:** each chart click and *View Data* context opens with its locked defaults (D4), so the table explains why
its items are there.

## IN scope
- Defaults-map entries for every chart and *View Data* context in the D4 table except a Feature's child items and
  the Delivery timeline, plus `startedAndClosed` (M9); about 30 call sites move from `highlightColumn` to a context
  key, `BarRunChart` / `LineRunChart` take a required `dialogContext`.
- Feature Size (dot and *View Data*) sorts by Size; Aging, Stale and the in-progress *View Data* sort by Age (M8).
- Existing judgement cells (SLE colouring, Age band, SLE risk, Time in State) keep their look.
- Named cycle-time columns where the caller passes the definitions (M4); `ageOn` for past-day points (DDD-8).
- Cumulative State Time: Days Contributed only, no catalogue (DDD-11). Its bar now opens the dialog at once and
  reads the items through a TanStack query keyed by (owner, state, window, selected item ids), with the loading look
  (S6) and the could-not-load message (S5) (DDD-14, AC-2.6).

## OUT of scope
- A Feature's child items and the Delivery timeline (slice 03).

## Learning hypothesis
- **Disproves** "the locked table is right" if the dogfood shows a context whose defaults hide what the chart
  point is about; adjust the map entry, as D4 expects.
- **Confirms** it if each dialog answers "why is this item here?" without opening *Manage columns*.

## Acceptance criteria
AC-2.1 to AC-2.6 in `../feature-delta.md` (US-02).

## Dependencies
Slice 01b (catalogue, defaults map, per-context layout).

## Effort
About 1.5 to 2 days: many small call-site moves, plus the Cumulative State Time keyed query and its two looks.
`SleRiskColumnReachable.spec` is re-run; `workitemsdialog.png` changes (regenerated at finalize).

## Dogfood
Dev instance: Blocked over time bar shows Blocked since; Feature Size dot sorts by Size; Work Distribution slice
shows Parent. Cumulative State Time with the browser throttled to a slow connection: the dialog opens at once with
its headers and loading overlay, never "No items to display".
