# Slice 01: Outlier chip, axis capped at the cut, edge markers and popover list (walking skeleton)

Stories: US-6502-01 (first half) and US-6502-02. Job: `job-flow-coach-read-typical-work-know-the-exceptions`
(proposed, see feature-delta). The first two DISCUSS slices (chip and clipped axis; lines above the axis) are merged
here by AFK-6: the cap without markers hides the 95% and SLE lines silently, and markers without the cap leave
"not drawn" outliers in plain sight, so neither half is honest alone. The other slices are renumbered: 02 is the
former 03, 03 is the former 04.

## Goal

A reader of the Cycle Time scatterplot sees a plot scaled to normal work, every percentile / SLE line still visible
(in place, or as a labelled marker at the top edge), plus an "N outliers" chip that opens a popover listing each
outlier with its days and how many times the median it is.

## IN scope

- The generic outlier calculation in `utils/charts/outliers.ts`: `value > OUTLIER_MEDIAN_MULTIPLE x median`,
  `OUTLIER_MEDIAN_MULTIPLE = 5` in one place, a pure function over any list plus a value selector, reusable by
  another chart.
- "The median" is Lighthouse's own 50th percentile (AFK-10), the same number as the "50%" line: sort ascending,
  index = `floor(0.5 x N) - 1`, clamped, as in `PercentileCalculator.cs`. Population: every closed Work Item of the
  date range for the shown Cycle Time. Median <= 0 or no items: no outliers.
- A pure `axisTop`: with outliers `min(1.1 x max(typical dots and lines at or below the cut), cut)`; without outliers
  exactly today's axis. Properties: every typical value and drawn line at or below the top, every outlier above it,
  the top at or below the cut. The x-axis domain stays from all items (AFK-13).
- Scatterplot: outliers are not drawn (filtered from the series; one filtered array feeds series, marker colours and
  tooltip). A line above the axis top is drawn as an edge marker (triangle + text, line colour, `aria-hidden` group;
  the popover names the same lines once). Percentile / SLE chip toggles show and hide the markers.
- Chip (outlined, neutral, icon + "N outliers") in the legend row; absent when N = 0; disabled, and an open popover
  closed, whenever the widget status leaves "ready" (AFK-11). The page passes the chart a boolean `frameIsReady`, from
  `frameStateOf` for the `cycleScatter` key through the `buildWidgetNodes` ctx (AFK-16).
- Popover: count, "not drawn" line with the cut and the median in days, the one-sentence rule, "Above the axis"
  sentence only when lines are above it, compact list (Work Item ID + name linking to the tracker, closed date, days,
  "N x the median"), sorted by days descending, first 10 rows then "and k more" (chip count = rows + k).
- Keyboard and screen-reader behaviour of chip and popover, with the testable criteria listed in the feature delta
  (button name, dialog name, table headers, "9.7 times the median" text for TZ-301 against the 50% line of 6 (computed), marker group `aria-hidden`).
- Named percentiles tagged with the definition id and the fetch callback, so a late answer for an old date range
  never lands (needed because the markers and the axis read them).
- Demo data: `TZ-301` added to Team Zenith (no Parent), with the accepted shift in Zenith's statistics handled in the
  affected tests (AFK-14; details in the feature delta).
- Where each thing is proven (AFK-15): on **Team Zenith** (computed, last 30 days: 50% line 6, 95% line 15, SLE 85%
  within 7 days, cut 30, TZ-301 at 58 days = 9.7x, axis top 22) the demo E2E and the `@screenshot` show the chip and
  the popover only, because **no line lies above the cut there, so no edge marker appears**. Edge markers, their text
  and order, and the `axisTop` cap with a line above it are proven by RTL and unit tests on an illustrative fixture
  (median 6, cut 30, 95% line 33, SLE 35); there is no marker or geometry assertion in E2E.
- Tests that guard the statistics (K4): a mounted chart (MUI mocked) given the full population hands the page's
  percentile, SLE and View Data inputs on unchanged, and the lines it draws plus its markers equal the lines it
  received; and the chart's 50% line equals `fiftiethPercentile(effectiveDataPoints)` for the default and for a
  named Cycle Time. The factor text rounds half-up from 10 up (63 against 6 reads "11x", AFK-17).
- Effect shape of the new components: `OutlierChip` is bounded-change (its own open flag; effect ports `onOpenInList`,
  `onClose` and the usage-event callback; no service access); `OutlierPopoverContent` is a pure view of props whose
  callbacks are effect ports; `WorkItemsDialog` is pure presentation and unchanged.
- Copy built from the reader's Terminology words; Teams and Portfolios (one shared component; the Portfolio path is
  covered by unit tests only, no seeded Portfolio has an outlier).

## OUT of scope

- Type-chip / named-cycle-time / empty-state work (slice 02), "Open in a list" and the ⓘ sentence (slice 03).
- Any other chart. Any change to percentile lines, SLE share, tiles, View Data, forecasts.

## Learning hypothesis

Disproves: "a small chip in the legend row is noticed and understood without explanation." Test with 3 readers who
have not seen the story: they open the demo Team's scatterplot and say what is not drawn and where the 95% line is (on Zenith it is drawn in
place at 15 days; the edge marker cannot be shown on the demo and is shown on the throwaway fixture instead).
If fewer than 2 of 3 find and open the chip unprompted, direction 2 needs a stronger affordance (this is where the
dissenting switch returns). If they read an edge marker as a data point, rework the marker (the spike fallback moves
it into the legend row). Confirms: the one-sentence rule is enough to explain why an item is missing.

## Pre-slice spike (DELIVER step 0)

Proves in a real browser, at 1280 px and 375 px, light and dark, that a custom SVG text row fits in a reserved top
margin: (1) its box lies inside the SVG; (2) the plot height does not change with the number of marker rows apart
from the margin, and ticking a percentile chip moves nothing; (3) no marker box intersects the plot or the legend
row; (4) the longest label stays inside the SVG at 375 px or is shortened by the phone fallback. Fail on any: the
marker row moves into the legend row (non-interactive, `aria-hidden`, wraps with the row). Hook names relied on
(`useDrawingArea` and friends from `@mui/x-charts/hooks`) are already used in this repository
(`BlackoutOverlay.tsx:2,13`, `PbcBlackoutOverlay.tsx:2`, `TimeBlackoutOverlay.tsx:2`). The demo has no line above the
cut (AFK-15), so the spike runs on a throwaway fixture page and its result goes in the delivery log; no E2E keeps it.

## Acceptance criteria (summary; full text in the stories)

- Outliers absent from the plot, present in the popover, the lines and View Data; the axis top is below the cut.
- A line above the axis top has a marker with text; one at or below does not; toggling a line off removes its marker.
- Chip count equals the rows when n is at most 10 (rows + k above 10); "1 outlier" singular.
- Chip and popover reachable and operable by keyboard; opening, closing and focus return verified.
- Statistics and View Data identical with and without the partition (the wiring-point test on a mounted chart; the
  pure-function "same reduction" property is not used as evidence, it cannot fail).

## Dependencies

None upstream. DELIVER ownership: the spike, the demo-data item, and the phone-width, dark-theme and PDF checks.

## Effort and reference class

About 2 days (the former slices 01 and 02, about 1 day each, merged). Reference class: the added legend chips and info
popovers of earlier chart stories, plus the existing custom overlays on the same container (`BlackoutOverlay`,
`PbcBlackoutOverlay`). Production data: the demo Team Zenith and the dev instance restored from
`Restore-DbBackup.ps1` (real history). Dogfood: same day on the dev instance.
