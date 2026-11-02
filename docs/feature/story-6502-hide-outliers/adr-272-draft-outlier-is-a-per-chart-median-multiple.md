# ADR-272 (draft): An outlier is a value above a fixed multiple of the median, computed in the frontend, per chart

Draft written in the DESIGN wave of Story 6502, revised after the first review round. It lives in the feature
folder; moving it to `docs/product/architecture/` and adding the ADR-270 glossary row and the `ARCHITECTURE.md`
section 4 line is a DELIVER step (the DESIGN run was not allowed to edit shared docs).

## Status

Proposed (maintainer reviews on return from AFK).

## Context

The Cycle Time scatterplot sets its axis from the largest value. One very long Work Item squeezes the other dots
into the bottom of the plot (computed example, Team Zenith's demo data over the last 30 days: the 50% line is 6 days,
so the cut is 30 days; TZ-301 at 58 days stretches the axis to 1.1 x 58 = 63.8 days, about 2.1 times the cut, while
the other items end at 20 days, where the axis would stop at 22 days; the figures shift by about a day with the
weekday the demo is loaded). The maintainer decided (Story 6502, after
DIVERGE): chip plus popover; an outlier is a value more than N times the median, N = 5 to start; no minimum sample;
the calculation is generic, in the frontend, and runs on each chart's own value, because an item can be normal on one
chart and an outlier on another. Percentiles, the SLE share, RAG, tiles, forecasts and View Data keep including every
item.

Quality drivers: changeability of N (one constant), reuse by a second chart without edits to the first, no number
anywhere silently changing, one meaning of "median" on the screen, testability without a browser (MUI X Charts is
mocked in unit tests). Constraints: frontend only, no API or DTO change, no Lighthouse-Clients change; the layer rules
in `layerBoundaries.enforcement.test.ts` (`utils` imports no services, hooks or pages; `components` imports no pages).

## Decision

1. The rule is a pure TypeScript module `src/utils/charts/outliers.ts`: one exported constant
   `OUTLIER_MEDIAN_MULTIPLE = 5`, a function that takes a list and a value accessor and returns the median, the cut
   (N x median), the outliers (largest first) and the typical items, and a pure `axisTop` function. It imports only
   the sibling `chartAxisUtils`. A chart passes its own accessor; nothing in the module knows a Work Item.
2. **"The median" is Lighthouse's own 50th percentile** (AFK-10), computed in the frontend by `fiftiethPercentile`
   with exactly the backend's convention (`PercentileCalculator.cs`: sort ascending, index = `floor(p/100 x N) - 1`,
   clamped). It is therefore always one of the items' values and always the number behind the chart's "50%" line, so
   the popover and the line cannot disagree. This is the one rule for "median" in the product; there is no second
   definition to explain. The backend is not changed. The convention sits one rank below the textbook median when
   `p x N` is not whole (for example N = 5 picks the 2nd value); changing it would move every percentile in the
   product, so whether it is intended is a separate question, flagged to the maintainer as an observation. A table
   test pins the frontend function to the backend's results for N = 1 to 6, 10 and 11.
3. An outlier is strictly more than the cut. Guard: no items, or a median that is not above zero, means no outliers.
4. A chart that hides outliers **filters them out of its series** (they are never handed to the chart library). Its
   axis top is `axisTop`: with outliers, `min(1.1 x max(typical values and the lines at or below the cut), cut)`;
   without outliers, exactly today's axis. So "not drawn" is exactly "above the axis". Stated properties: every
   typical value and every drawn line is at or below the top, every outlier is above it, and the top never exceeds
   the cut. The x-axis keeps its domain from all items (AFK-13).
5. Lines (percentiles, SLE) above the axis are drawn as labelled edge markers in a reserved top margin, by a small
   custom SVG layer built on `useDrawingArea` (already used in this repository at the installed range); they are not
   passed to `ChartsReferenceLine`. The marker group is `aria-hidden`; the popover names the same lines once.
6. The chip is a separate component (not a mode of `LegendChip`); the list behind it reuses `WorkItemsDialog`.
   The chip is disabled, and an open popover closes, whenever the widget status leaves "ready" (AFK-11). The page gives
   the chart a plain boolean, `frameIsReady`, taken from `frameStateOf` for the `cycleScatter` key and passed through
   the `buildWidgetNodes` context (AFK-16); it is not called `isCurrent`, which names an unrelated local in the same
   file. The `WidgetStatusReporterContext` channel was considered and rejected: it reports from a chart up to its
   frame, and this need runs from the frame down to the chart.
7. The statistics, the SLE share, RAG, tiles and View Data are not given the outlier partition. Only the chart's own
   drawing and its chip receive it. This is asserted at the wiring point, not by a pure-function property (a property
   that runs the same reduction on both sides cannot fail): a mounted chart (MUI mocked) is given the full population
   and must hand the percentile, SLE and View Data inputs on unchanged, and the lines it draws plus the markers it
   shows must equal the lines it received.
8. **Slices.** The cap at the cut and the edge markers ship in one slice (AFK-6). Shipping the cap without markers
   hides the 95% and SLE lines silently; shipping markers without the cap leaves "not drawn" outliers in plain
   sight. Neither half is honest alone.
9. **Named percentiles** are stored with the definition id **and** the fetch callback's identity. A keyed TanStack
   query would be complete but needs the fetch to move out of the chart; the callback already closes over the owner
   id and the date range (`BaseMetricsView.tsx:1464-1473`), so its identity changes exactly when a key would. This is
   a deviation from the project's keyed-query norm, accepted because scoped percentiles already live in component
   state elsewhere on the page and moving the fetch is outside this Story.

## Spike and fallback

MUI X behaviour for out-of-domain values and for a reserved top margin is unverified (the DESIGN worktree has no
`node_modules`). DELIVER step 0 is a browser spike with these criteria, at 1280 px and 375 px, light and dark:
(1) a marker `<text>` above the drawing area is fully inside the SVG; (2) the plot height does not change with the
number of marker rows apart from the reserved margin, and no dot or line moves when a percentile chip is ticked; (3)
no marker box intersects the plot or the legend row; (4) the longest label stays inside the SVG at 375 px or is
shortened by the phone fallback. Any failure triggers the **fallback: the marker row moves into the legend row** as
non-interactive, `aria-hidden` text after the percentile chips, wrapping on a phone. Only the marker component's home
changes; the pure modules, the cap and their tests stay. Keeping the axis open for every line is not a fallback.

## Alternatives considered

- **Backend flag on the DTO** (an `isOutlier` per item or a cut in the metrics response). Rejected: the maintainer
  wants it per chart on that chart's value; it would change API contracts and force a Lighthouse-Clients bump for a
  render-time concern; the frontend already holds the numbers.
- **A textbook median in the frontend.** Rejected (AFK-10): two different "medians" on one screen, the chart's "50%"
  line and the popover's, would confuse every reader and need an explanatory sentence.
- **Changing the backend percentile convention.** Rejected: it moves every percentile in the product.
- **A `useOutliers` React hook as the home of the rule.** Rejected: the rule has no state or effect; a hook would
  tie reuse and property tests to React for nothing. A hook can wrap the function later if a second chart wants it.
- **A strategy class or interface per chart** (`IOutlierPolicy`). Rejected: there is one rule; the accessor
  function already is the strategy. Add a seam when a second rule exists.
- **Clip the points instead of filtering them.** Rejected: MUI X scatter markers are not clipped by the axis, and
  each marker is also a focusable button and a tooltip target; an off-screen marker would be reachable by keyboard.
- **Pass out-of-domain lines to `ChartsReferenceLine`.** Rejected: drawn above the plot, unclipped, over the legend.
  To be proven in the DELIVER spike.
- **Keep the axis open for every line** (the DISCUSS slice-01 rule). Rejected as the permanent rule: for a team
  whose 95% line or SLE lies above the cut, keeping the axis open for it undoes the point of the story for exactly
  that team. Such a line is rare on real-looking data (computed on Team Zenith, the 95% line is 15 days against a
  50% line of 6, 2.5 times, well under the cut of 30), so the markers are an honesty guard more than the usual look;
  an illustrative right-skewed lognormal (95% line near 5.2 times the median, not a measurement) is where they would
  show.
- **`WidgetStatusReporterContext` as the source of the chip's "ready" flag.** Rejected (AFK-16): it carries status
  from a chart to its frame, the opposite direction; a prop from `frameStateOf` is the smaller seam.
- **Two slices (cap first, markers later).** Rejected (AFK-6), see decision 8.
- **2 x P95 (DIVERGE) or Tukey fences.** Superseded by the maintainer's decision.

## Consequences

- Positive: changing N is a one-line change; copy that quotes N follows it; a second chart adopts the rule by
  passing an accessor; the module is trivially property-testable; the popover's median always equals the "50%" line;
  no API, DTO, RBAC, migration or client change.
- Negative: the lower-index convention means the "median" can sit one rank below the textbook value at small N (an
  existing product behaviour, now visible in one more place). A dot exactly at the cut touches the top edge when the
  largest typical value is above `cut / 1.1`. The named-percentile tag deviates from the keyed-query norm.
- Risk, with its probe: the spike above (manual, on a throwaway fixture, because the demo has no line above the cut
  and so shows no marker) and RTL and unit tests of the marker specs on a fixture; no E2E geometry check is kept.
  Demo data carries one outlier on Team Zenith (`TZ-301`, no Parent) so the chip is visible; its accepted effect on Zenith's
  statistics is listed in the feature delta.
- Glossary: "Outlier" enters the Flow metrics section of ADR-270 (row text in `feature-delta.md`, DISCUSS context
  map). "Pace outlier" (Work Item Aging) and a Process Behaviour Chart point beyond its limits remain different
  things; every use is qualified.

## Enforcement

`utils/charts/outliers.ts` imports only the pure `chartAxisUtils`, so the existing fallow layer check already keeps
it free of services, hooks and pages. A unit test asserts that the help sentence and the chip copy contain the value
of `OUTLIER_MEDIAN_MULTIPLE`, so the constant cannot drift from the words. A table test pins `fiftiethPercentile` to
the backend convention. Property tests (fast-check, a frontend dev dependency per AFK-7) cover the partition, the
`axisTop` bounds, and the half-up rounding of the factor from 10 up (AFK-17). The wiring-point test above covers the
statistics, and a second chart test pins the chart's 50% line to `fiftiethPercentile` of the same population, for the
default and for a named Cycle Time, so the popover's median and the line cannot drift apart. Mutation testing (Stryker, 80%) covers the boundary (`>` against
`>=` at the cut) through a test at exactly N x median.
