# Slice 02: A dashboard opens with every chart's frame in place

**Goal:** opening a metrics dashboard, or switching to a category not visited yet, renders every widget's
frame at its final position with a spinner, and the charts fill in where they stand.

## IN scope
- Widget nodes no longer render `null` while their data is missing. They render the shell's loading state from
  slice 01b: a centred spinner under a title + info header (D2, S4, AC-2.1, AC-2.2).
- A first-load failure ends in the S3 message (AC-2.4), under a title + info header (M2).
- `FlowEfficiencyOverviewWidget`, `PredictabilityScoreOverviewWidget` and `TotalWorkItemAgeWidget` drop their
  own `CircularProgress` in favour of the shared state (AC-2.3).
- E2E: the walking skeleton (`ChartLoadingIndicators.spec.ts`), the POM waits and the re-pointed specs landed in
  slice 01b; this slice re-checks that they hold on first load. `@screenshot` output is unchanged.

## OUT of scope
- Estimation vs. Cycle Time and Feature Size keep today's presence rule: no frame and no spinner on first load; they
  appear when their data says so (R1, AC-2.2's named exceptions). *But when their own fetch fails, on first load or
  later, they show the could-not-load frame, with no spinner (M3, Maintainer 2026-10-09); that part is in scope.*
- Page-level `LoadingAnimation` on Team and Portfolio detail pages.

## Learning hypothesis
- **Disproves** "showing every frame up front feels faster than pop-in" if a category with many widgets reads
  as a wall of spinners on the dev instance. That would argue for a skeleton shape instead of a spinner.
- **Confirms** it if the dashboard keeps its layout from first paint to fully loaded on demo data and on the
  dev instance (the two named exceptions aside).

## Acceptance criteria
AC-2.1 … AC-2.4 in `../feature-delta.md` (US-02), and AC-1.1's `aria-busy` clause (formerly AC-1.8, E9) for the
first-load frames.

## Dependencies
~~Slice 01b (the loading state in `WidgetShell`).~~ Slice 01c (corrected in review round 2, E8): it needs 01b's
loading state in `WidgetShell` and 01c's reporters, so a self-fetching widget's first-load frame spins instead of
reading `ready`. It ships on its own after that.

## Effort
Under a day. The production change is small; the E2E re-pointing of the 17 specs moved to slice 01b, so what is
left is a first-load check of the skeleton and the waits, and a screenshot run.
Reference class: Bug #6112 (E2E lazy-route stale-page race, same "wait for target-only content" fix).

## Dogfood
Open Team metrics on the dev instance with network throttled. Every frame appears at once, nothing
reshuffles, and Screenshots.spec regenerates byte-identical PNGs.
