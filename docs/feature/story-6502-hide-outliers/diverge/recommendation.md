# Recommendation — Story 6502 "Hide Outliers"

Derivation: job (`job-analysis.md`) -> chart inventory (`chart-inventory.md`) -> rule (`outlier-definition.md`) ->
research (`competitive-research.md`) -> six options (`options-raw.md`) -> scores (`taste-evaluation.md`).

## Decision for DISCUSS

**Proceed with Option 2, the indicator chip with a popover, on the Cycle Time scatterplot only, with the rule "an outlier
is a closed Work Item whose Cycle Time is more than twice the 95th percentile", assuming that readers will notice and
open a small chip in the legend row, and that no way to see the outliers in place on that chart is needed.** Build the
cut and the chip as shared pieces so Estimation vs Cycle Time can adopt them later; leave Work Item Age, the six Process
Behaviour Charts, Feature Size and the count charts as they are.

## Top 3

### 1. Option 2, chip + popover (4.35)

**Why it scores well**: it is the maintainer's own idea, it reuses the legend row and `LegendChip`, and it is the only
option with a simple first look (a readable plot and one small chip) and depth only on request. Concept cost is one
word, "outlier", with the rule in one sentence inside the popover.
**Core trade-off**: the outliers are not in the plot. A reader who wants to see them next to the other dots must read a
list, not look at a picture.
**Key risk**: the chip goes unnoticed, so someone reads the clipped plot as the whole picture. Mitigation to design:
the chip carries an icon and the words ("2 outliers"), it sits with the percentile chips, and the axis end is stated in it.
**Hire criteria**: choose this when the common job is reading normal work and the outliers are worked through from a
list, which is how a flow review uses them.

### 2. Option 4, "Show outliers" switch (3.55)

**Why it scores well**: it gives both views and it generalises the per-chart default (scatterplot off, Work Item Age
on) as a remembered setting; a reader can put the outlier back in the picture to explain it.
**Core trade-off**: a stored per-reader preference and a different default per chart ("why does this chart remember
differently?"), and the axis jumps when it is flipped.
**Key risk**: the stored state and per-chart defaults become configuration surface for a problem one rule solves.
**Hire criteria**: choose this if the maintainer wants outliers visible in context on demand, or if more charts adopt
clipping and readers disagree on the default.

### 3. Option 1, frame break with edge markers (3.40)

**Why it scores well**: nothing is out of sight; each outlier stays in the plot at its own date with its value written
on it, and it is the treatment the dataviz literature (PolicyViz) recommends over a slashed bar.
**Core trade-off**: arrows stack up at the edge when several items are outliers on nearby days, and a second custom mark
type goes into a scatter whose marker is already custom.
**Key risk**: the glyph is not self-explanatory at phone width and in dark mode.
**Hire criteria**: choose this if the maintainer wants no hidden-until-clicked state at all.

## Recommended option

Option 2 leads under the locked weights (4.35 vs 3.55 and 3.40) and under both alternative weightings, so the choice does
not depend on the weight adjustment. It also matches the product's own rule that a mark never disappears silently: the
chip is the trace, and View Data, the percentile lines, the SLE share and every tile keep including the outliers. The
2nd and 3rd places are close to a tie.

## Dissenting case

Option 4 almost won on intent, not on score. Its argument: a reader presenting the chart often needs to show the
outlier in context ("this one is 52 times the median") and a popover list cannot do that; a stored per-chart default is
also the clean way to express "scatterplot hides, Work Item Age shows". It loses on concept count and added state.
It becomes the better pick if the maintainer's answer to open question 5 is "yes, readers need to see them on the chart".
A cheap bridge exists: put a "Show on chart" action in Option 2's popover that stretches the axis until the reader
leaves (the 8e variant), with no stored state. That is a DESIGN choice, not part of this recommendation.

## What the chart looks like (rough, for DISCUSS; DESIGN owns the sketch)

```
 Cycle Time   [Default v]
 [50%] [70%] [85%] [95%] [SLE]                    [Feature] [Bug]  [^ 2 outliers]
 Cycle Time (days)
  60 |                                              .
     |      .  .                .
  30 |  . .:..:.. .  .   . ..
     | .:::::::::::.....:.:
   1 +---------------------------------------------------- Date
        Jan      Feb      Mar      Apr
                                  
 Popover on the chip:
 +---------------------------------------------------------------+
 | 2 outliers. The axis ends at 60 days.                         |
 | An outlier took more than twice as long as 95% of the items.  |
 | The lines and numbers include them.                           |
 |                                                               |
 |  Work Item            Closed     Days   Compared with median  |
 |  OE-123 Migrate DB    12 Mar      312   52 x  (median 6 days) |
 |  OE-091 Legacy export 02 Feb      190   32 x                  |
 |  [Open in a list]                                             |
 +---------------------------------------------------------------+
```

The chip is absent when nothing exceeds the cut. "Open in a list" reuses `WorkItemsDialog`, the same dialog a dot opens.
Reuse: `LegendChip`-style chip, the existing info-popover styling, `WorkItemsDialog`. Nothing new in the data layer.

## Visualization review checklist (recommended direction, scatterplot)

- [x] **One question and decision.** "Where do my closed Work Items land against the percentile lines and the SLE, and
  which ones are far beyond?" Decision: is typical delivery within the SLE, and which exceptional items to investigate.
  To be copied into `feature-delta.md` at DISCUSS.
- [x] **Awkward data.** Checked on paper, to repeat on demo data in DESIGN: fewer than 10 items (rule off, chart as
  today); one 300-day item (the case: axis ends at the cut); a cluster of outliers above 5% of items (P95 rises with them,
  so fewer are flagged; the chip still tells the truth about what is hidden); an item exactly at the cut (not an outlier:
  "more than"); several items on one day (grouped dots have one cycle time, so a group is in or out whole); a named cycle
  time selected (cut recomputed from the named percentiles); types hidden by chips (open question 4); SLE above the cut
  (axis keeps the SLE line); empty (the existing "No data available"); partial period (unchanged); beyond-the-horizon:
  N/A, not a forecast chart.
- [x] **Simplest form.** The scatter stays; the addition is one chip and one popover. No pie, gauge or 3D.
- [x] **Title and axes.** Title and axis label already use Terminology (`Cycle Time (days)`, `Date`). "Outlier" is a
  fixed UI word, not one of the renameable terms; confirm (open question 8). The axis label does not change; the chip
  states where the axis ends.
- [x] **ⓘ popover.** The `cycleScatter` entry in `widgetInfoMetadata.ts` gets one added sentence naming the rule and
  where to see them; Learn More stays `#cycle-time-scatterplot`.
- [x] **Docs.** The scatterplot section of `metrics/flow-metrics` gets a "how to read it" passage: what the chip is, the
  rule in one sentence, that nothing is removed from the numbers or from View Data.
- [x] **Grey plus one accent; colour not alone.** The chip is neutral (outlined grey), not the SLE or percentile hues; its
  meaning is carried by an icon and the word "outliers", not a colour.
- [x] **Bars from zero; dual axis.** N/A, because a scatter with a single value axis.
- [x] **Uncertainty.** N/A, because the chart shows actuals; the P95 line stays a drawn line.
- [x] **Loading, failure, too little data.** Loading and failure are `WidgetShell` status as for every widget; the chip
  never shows a count from a previous answer. Too little data (fewer than 10 closed items) is its own state: the rule
  is off and the chart behaves as today, rather than flagging from a P95 of four items.
- [ ] **Phone width, dark mode, enlarged, PDF.** To verify in DESIGN/DELIVER: the legend row already wraps (`flexWrap`);
  the popover must fit a phone (width-limited, scrolls); contrast of the chip in dark mode. The scatterplot is not in
  `EnlargeableChart` (only the Delivery metrics tab uses it), and no report PDF contains a chart today, so enlarged and
  PDF are N/A, because those paths do not render this chart. Rule for the future: any chart added to a report carries a
  printed one-line stand-in for the chip ("2 items above 60 days are not shown: 312, 190").

## Next-wave pointers (answers belong to DISCUSS / DESIGN)

- **Context map (ADR-270):** touches the metrics / flow-analytics area only; no new domain event; "Outlier" would be a
  new glossary term. To be answered explicitly at DISCUSS.
- **UI surface (ADR-271):** the Metrics tab of a Team's and a Portfolio's page; no move of surfaces in this story.
- **Sketch approval:** needed before DELIVER (project rule); the sketch above is a placeholder.
- **Usage data (DEVOPS):** candidate name-only event "outlier popover opened". Not decided.
- **Website / clients:** the CLI and MCP return raw values and are unaffected; the marketing surface is a DISCUSS checklist item.
