# Slice 02: A dashboard opens with every chart's frame in place

**Goal:** opening a metrics dashboard, or switching to a category not visited yet, renders every widget's
frame at its final position with a spinner, and the charts fill in where they stand.

## IN scope
- Widget nodes no longer render `null` while their data is missing. They render the shell's loading state from
  slice 01 (D2, AC-2.1, AC-2.2).
- `FlowEfficiencyOverviewWidget`, `PredictabilityScoreOverviewWidget` and `TotalWorkItemAgeWidget` drop their
  own `CircularProgress` in favour of the shared state (AC-2.3).
- E2E: the `MetricsPage` POM gets a "widget loaded" wait (no spinner). Specs that waited on `widget-shell-*`
  visibility go through it. `@screenshot` output is unchanged.

## OUT of scope
- Widgets whose presence depends on their data, if DESIGN finds any (AC-2.2's exception).
- Page-level `LoadingAnimation` on Team and Portfolio detail pages.

## Learning hypothesis
- **Disproves** "showing every frame up front feels faster than pop-in" if a category with many widgets reads
  as a wall of spinners on the dev instance. That would argue for a skeleton shape instead of a spinner.
- **Confirms** it if the dashboard keeps its layout from first paint to fully loaded on demo data and on the
  dev instance.

## Acceptance criteria
AC-2.1 … AC-2.4 in `../feature-delta.md` (US-02).

## Dependencies
Slice 01 (the loading state in `WidgetShell`). It ships on its own after that.

## Effort
About a day. The production change is small. Most of the time goes into the E2E POM wait and re-pointing
roughly ten specs, then a screenshot run.
Reference class: Bug #6112 (E2E lazy-route stale-page race, same "wait for target-only content" fix).

## Dogfood
Open Team metrics on the dev instance with network throttled. Every frame appears at once, nothing
reshuffles, and Screenshots.spec regenerates byte-identical PNGs.
