# Options (raw) — Story 6502 "Hide Outliers"

No scoring or ranking in this file. Evaluation is in `taste-evaluation.md`.

## HMW

How might we keep the picture of normal work legible while every exceptional item stays known, reachable and
explainable, without any number on the chart changing?

Vocabulary used below: **cut** = the Cycle Time above which an item is an outlier (see `outlier-definition.md`: twice the
95th percentile); **off-scale** = an item above the cut.

## SCAMPER

| Lens | Idea |
|---|---|
| **S**ubstitute | Replace the linear Y axis by a logarithmic or symmetric-log one, or by an axis with a visible break between the dense region and the tail. Nothing is hidden. |
| **C**ombine | Put the outlier disclosure into the legend row that already carries the percentile chips and type chips: one more chip, "Outliers (2)". Clicking it opens the numbers. |
| **A**dapt | Break the frame (PolicyViz, Prison Policy Initiative charts): the axis ends at the cut, each off-scale item is drawn at the top edge with an arrow and its value, at its real date. |
| **M**agnify | Magnify the dense region: the main plot is zoomed to the cut, and a small full-range overview (inset, or a thin strip above) shows where the tail sits. |
| **P**ut to other use | Make View Data the home of the outlier facts: a "times the median" column and an "outlier" marker in the table the widget header already opens; the chart carries one line of text ("2 items above 60 days are not shown, see View Data"). |
| **E**liminate | Remove the indicator altogether: the axis simply ends at the cut and nothing marks what is beyond it. (Recorded as the baseline; the story explicitly asks for an indicator.) |
| **R**everse | Reverse who decides: the reader flags an item as "leave out of charts"; the system applies no rule. The flag is stored. |

## Crazy 8s supplements

- 8a. A "Show outliers" switch next to the chart (legend-chip style), default off for this chart and on for others, remembered per reader.
- 8b. A always-visible callout under the chart: "Longest: 312 days, 5.6 x the median".
- 8c. Cut at a multiple of the SLE instead of the percentile.
- 8d. Two stacked panels sharing the date axis: a thin one above for the tail, the main one below.
- 8e. Hover or focus on the indicator temporarily stretches the axis to show the tail, and snaps back.

## Curated six

### Option 1: Frame break (edge markers)

**Core idea**: The plot ends at the cut; each off-scale item shows as an arrow glyph pinned to the top edge at its own date, labelled with its value, and clicking it opens the item list the dots already open.
**Key mechanism**: The mark stays in the plot, positioned by date only; its height is replaced by the edge.
**Key assumption**: Readers understand "an arrow at the edge means further up than the axis goes" without being told.
**SCAMPER origin**: A (Adapt), with 8a-style labels.
**Closest competitor**: PolicyViz / Prison Policy Initiative frame-breaking; Grafana's clamp-to-edge.
**View Data**: unchanged (it is built from the full list).
**Percentile lines and statistics**: unchanged; lines drawn at their true values, all at or below the cut.
**PDF / report**: the glyphs and values print as drawn; nothing needs a click.

### Option 2: Indicator chip + popover (disclosure outside the plot)

**Core idea**: The plot ends at the cut and the off-scale dots are not drawn; a chip in the legend row says "2 outliers"; clicking it opens a popover listing each item with its days and "x the median", each row opening the item.
**Key mechanism**: The plot and the disclosure are separate surfaces; the chip is the only trace in the chart area.
**Key assumption**: A reader who sees a chip will click it, and a chart with a chip is understood as "not everything is plotted".
**SCAMPER origin**: C (Combine), with P (Put to other use) and 8b folded in as the caption variant.
**Closest competitor**: none found with a count and numbers on click; Datadog clips by threshold with no mark.
**View Data**: unchanged.
**Percentile lines and statistics**: unchanged.
**PDF / report**: a popover does not print; a printed chart would need a text line in place of the chip ("2 items above 60 days not shown: 312 d, 190 d").

### Option 3: Overview + detail (inset)

**Core idea**: The main plot is zoomed to the cut; a small second chart shows the whole range with the zoomed region marked, so the tail is visible there at its true scale.
**Key mechanism**: Two coordinated views of the same data.
**Key assumption**: There is room for a second view and readers relate the inset to the main plot.
**SCAMPER origin**: M (Magnify), with 8d.
**Closest competitor**: Desbarats's inset charts; map insets; map minimaps.
**View Data**: unchanged.
**Percentile lines and statistics**: unchanged; lines drawn on both views.
**PDF / report**: both views print; the inset takes space on a page.

### Option 4: "Show outliers" switch (reader-held view state)

**Core idea**: A chip-style switch in the legend row, "Outliers (2)", off by default on the scatterplot: off = clipped at the cut, on = full axis; the choice is remembered per reader and each chart has its own default.
**Key mechanism**: Two complete renderings of one chart; the reader picks.
**Key assumption**: Readers want to flip between the two and a per-reader remembered choice is acceptable to them.
**SCAMPER origin**: Crazy 8s 8a (with 8e as a temporary variant).
**Closest competitor**: ActionableAgile's zoom (but there the numbers follow the view), Easy Agile's legend filtering.
**View Data**: unchanged.
**Percentile lines and statistics**: unchanged in both states.
**PDF / report**: prints in whichever state the screen was in, so the state has to be named in the printed chart.

### Option 5: Re-scale the axis (log, symlog or broken axis)

**Core idea**: The Y axis is non-linear (logarithmic, symmetric-log) or has a visible break; every item stays plotted.
**Key mechanism**: A change of encoding; nothing is hidden or clipped.
**Key assumption**: The reader can read a non-linear axis, or a break, correctly.
**SCAMPER origin**: S (Substitute), with 8d's stacked-panel variant merged here as a break.
**Closest competitor**: ggbreak, Highcharts breaks, log axes in most plotting tools.
**View Data**: unchanged.
**Percentile lines and statistics**: lines are drawn at their true values on a non-linear axis, so their spacing changes.
**PDF / report**: prints as drawn.

### Option 6: Reader-flagged outliers

**Core idea**: Each Work Item has an "leave out of charts" flag the reader sets; the system applies no rule; flagged items are not plotted.
**Key mechanism**: Human judgement stored against an item.
**Key assumption**: Someone will flag, keeps it up to date, and the flag is the same for every reader and every chart.
**SCAMPER origin**: R (Reverse).
**Closest competitor**: spreadsheets' manual "exclude" rows; Great Gadgets' per-widget threshold settings (a setting, not a per-item flag).
**View Data**: flagged items need a column to remain visible there.
**Percentile lines and statistics**: depends on whether the flag also feeds the statistics; the story says it must not.
**PDF / report**: prints as the flags stood at the time.

## Diversity test (mechanism / assumption about the reader / cost profile)

| Option | Mechanism | Assumption | Cost profile |
|---|---|---|---|
| 1 Frame break | mark stays in plot, pinned to the edge | the glyph explains itself | chart-only code; new mark type in an MUI X Charts scatter with a custom marker |
| 2 Chip + popover | disclosure outside the plot | the reader opens the chip | chart-axis cap plus a small shared component |
| 3 Overview + detail | two coordinated views | the reader relates inset to main | layout and responsive work; second chart |
| 4 Switch | two renderings, reader chooses | the reader wants to flip, and a stored preference is fine | option 2's cost plus stored per-reader state and per-chart defaults |
| 5 Re-scale | encoding change | the reader reads non-linear axes | an axis scale setting plus help text that explains the scale |
| 6 Reader flag | stored human judgement | someone curates | backend, RBAC, migration, sync, plus UI |

All six differ on all three.

## Merged or removed

- Silent clip (E) removed as an option: it is option 2 without the chip, and the story requires an indicator. It remains
  the reference point ("today's behaviour minus the indicator") when comparing.
- 8b (callout under the chart) merged into option 2 as the print variant of its disclosure.
- 8c (cut at an SLE multiple) is a variation of the definition, handled in `outlier-definition.md`, not a separate direction.
- 8e (hover stretches the axis) merged into option 4.
- P (View Data as home) merged into option 2: View Data is unchanged in every option, so it is a shared extension, not a direction.

## Gate G3

Six options, SCAMPER (7 lenses) and Crazy 8s documented, diversity test passed, no evaluation language in this file.
