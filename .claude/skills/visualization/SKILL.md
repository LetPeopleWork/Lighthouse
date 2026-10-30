---
name: visualization
description: How Lighthouse designs anything that shows data — a chart, dashboard widget, metric tile, forecast view, report page or its help text. Distilled from Storytelling with Data (Knaflic), The Big Book of Dashboards (Wexler, Shaffer, Cotgreave) and the #MakeoverMonday project, applied to flow metrics and Monte Carlo forecasts. Load it whenever a DISCUSS, DESIGN, DISTILL or DELIVER step (or a bug fix) adds or changes a chart, axis, legend, colour, widget, info popover or the docs that explain a chart, before sketching it.
---

# Visualization — designing Lighthouse's data views

Lighthouse's reader is a team lead, a coach or a manager who has to **explain the chart to someone else**
— a stakeholder, a client, a steering meeting. A chart that is correct but cannot be explained is a
hurdle to rolling Lighthouse out (that is what Story 6452 was about). So the bar is: *someone who has
never seen this chart can say what it shows, and whether it is good or bad news, within a minute, from
the screen alone.*

The rules below come from three sources. Where they disagree with a standing Lighthouse decision, the
Lighthouse decision wins and is called out. Provenance and how well each rule was verified are in
[references/sources.md](references/sources.md). Colour palettes and chart mechanics (contrast checks,
mark specs) are the global `dataviz` skill's job; this skill decides *what* to show and *how it reads*.

## 1. Before drawing: the question, the reader, the data

1. **Write the one question the view answers** in the story or `feature-delta.md` before sketching it
   ("Is this Delivery going to make its date?", not "Delivery predictability"). One chart, one question.
   If it answers two, it is two charts (or a toggle, as Predictability has).
2. **Name the decision it supports.** What will the reader do differently after reading it? A view that
   supports no decision is decoration — leave it out (YAGNI applies to charts too).
3. **Look at real data first, not the happy path.** Run it against the demo data *and* the awkward cases:
   a team with three items, a partial current period, one 300-day outlier, an empty history, a forecast
   that reads *More than 5 years*. Decide how each looks before the sketch is approved.
4. **Pick the simplest form that answers the question** (table §4). Familiar forms cost the reader
   nothing; a novel form must earn its learning cost, and almost never does on a dashboard.

## 2. Make it self-explaining

Every chart, widget and tile carries all of these:

- **A title that names the thing** in the user's Terminology words (`Feature`, `Work Item`, `Delivery`,
  `Cycle Time`… via `useTerminology()`), never a tracker's word. Product charts redraw on live data, so
  the title cannot be a takeaway sentence the way the books recommend for slides; the takeaway goes in a
  computed caption or BAN instead (§3) when there is one worth stating.
- **Every axis labelled, with its unit** — `Cycle Time (days)`, `Likelihood (%)`, `Date`,
  `Work Items`. *Lighthouse decision:* the books allow dropping an axis when every mark is directly
  labelled; we do not — every axis is labelled, always (maintainer, 2026-10-30).
- **An ⓘ `InfoPopoverButton`** with a 2–4 sentence *how to read it*: what one mark is ("each dot is one
  closed Work Item"), what the colours/lines mean, **which direction is good** ("a gap that narrows is
  closing in"), and a **Learn More** link to the chart's section of the docs. Dashboard widgets get this
  through `widgetInfoMetadata.ts`; status guidance (sustain/observe/act) only where the widget has a RAG
  status.
- **Docs that teach interpretation**, not just describe the UI: a "how to read it" passage with the good
  and bad shapes ("widening is bad, narrowing is good"), linked from the popover anchor.
- **Thresholds named, not just coloured.** If a colour change means something (50/70/85% → Risky,
  Realistic, Confident, Certain), the meaning is written down somewhere the reader can see without
  guessing — popover, label or legend.

## 3. Focus attention

- **Grey by default, one accent with one meaning.** Context (all items, past days, other series) recedes in
  a muted tone; the thing the question is about gets the accent. The same colour means the same thing
  across a page — if orange is "SLE breach" on one widget it is not "Feature count" on the next.
- **Never colour alone.** ~8% of men cannot tell red from green. Pair every colour signal with a second
  channel: a label, an icon (the forecast-level icons), a shape, a pattern (hatched = estimated), or
  position. Fever zones and RAG chips need a word, not just a hue.
- **Label directly where it is cheap** — a percentile line labelled at its right end ("85%: 14 days"), the
  last point of a line, a target line — and drop the legend when every series is labelled. Keep a legend
  (the existing `ChartLegend` with click-to-filter) when series are many or identified by name.
- **Big numbers need context.** A BAN / tile shows the number, what it is compared with (limit, target,
  last period) and the direction that is good. A bare "14" is not information.
- **Show uncertainty honestly.** Forecasts are ranges: show percentiles or a band, keep forecast visually
  distinct from actuals (dashed vs solid), and never let a single date look like a promise.
- **Show the individuals when they matter.** A cycle time scatterplot keeps one dot per item, because the
  outliers are the conversation; an average alone hides them.

## 4. Choose the form

| Question shape | Use | Avoid |
|---|---|---|
| One or two numbers now (WIP, blocked, likelihood) | BAN / tile with comparison and good-direction | Gauge, speedometer, colour-only status |
| Change over time (throughput, WIP, likelihood per day) | Line or run chart; columns for counts per period | Area fills that imply volume where none exists |
| Many individuals over time (cycle time, age) | Scatter / strip, percentile lines labelled directly | Averages without spread |
| Distribution (cycle time, forecast outcomes) | Histogram or percentiles; highlight the chosen percentile, grey the rest | Mean alone |
| Progress vs scope (Delivery, Feature) | Burnup: scope line + done area, estimated scope visibly different | Burndown that hides scope growth |
| Actual vs forecast / target | Solid actual, dashed or banded forecast, labelled target line | A forecast styled like data |
| Part of a whole | Sorted bars, 100% bar for 2–3 parts, or a sentence | Pie/donut with more than 2–3 slices, 3D anything |
| Comparing teams / portfolios | Small multiples on **shared** axes | One tangled multi-colour chart; rankings that invite blame |
| Status against thresholds (fever, SLE risk) | Zones with named labels plus a second channel | Red/amber/green as the only cue |

**Bars start at zero**, since length is the value. Lines and dots may start elsewhere when the change is the
point, but say so on the axis.

**Dual axes** suggest a relationship through an arbitrary choice of scale. Prefer two panels that share an
x-axis, or a common unit. When a dual axis stays, each axis is labelled with the series it belongs to, and
the series are told apart by more than colour. *Existing exception:* Features over Time pairs Work Items
(bars, left axis) with the Feature count (line, right axis). It is kept because the two answer different
questions, and both axes carry the Terminology word.

## 5. Declutter (the makeover pass)

Run this pass over every sketch before showing it, and again on the built screen:

1. Remove borders, heavy gridlines (or drop them to a light tone), redundant ticks, and markers on every
   point.
2. Mute everything to grey, then put back the one accent.
3. Label directly where you can, and drop the legend if every series is labelled.
4. Keep axis text horizontal. Round numbers to the precision the decision needs (days, not 13.84 days),
   and format them the same way everywhere.
5. Add one annotation where it changes the reading: a moved target, a scope jump, today's line.
6. Check alignment and white space, and that the view still reads at phone width and in dark mode.
7. Ask whether everything left on the chart earns its place. Delete whatever doesn't.

## 6. States and size: what Lighthouse adds on top of the books

- **Loading and failure look different from "no data".** Use `WidgetShell` status for widgets, keyed
  TanStack queries, and never show the previous answer as current. This is a standing rule from DESIGN in
  `CLAUDE.md`.
- **Too little data is its own state.** If a chart needs N points, say so ("needs at least 3 daily
  records, have 1") rather than drawing a misleading line.
- **Phone width and print/PDF.** The bulk-report PDF and `EnlargeableChart` both render these charts, so
  check labels at the small size and when enlarged.

## 7. Where it plugs into the waves

- **DISCUSS:** each story that adds a view states its one question and the decision it supports (§1).
- **DESIGN:** the sketch shows the loaded, loading and failure looks, labelled axes and units, the ⓘ
  wording, and the colour/accent choice. Name the existing control it reuses.
- **DISTILL:** pin the axis labels, the ⓘ text and Learn More target, the Terminology words, and the
  too-little-data state. Don't pin colours or pixel layout.
- **DELIVER / finalize:** run the §5 pass on the real screen with real data, write the docs' "how to read
  it" passage, and regenerate screenshots.

## 8. Review checklist

Answer each item explicitly. "N/A, because …" is a valid answer.

- [ ] The view's one question and its decision are written down.
- [ ] Checked against awkward data: tiny, partial, outlier, empty, beyond-the-horizon.
- [ ] The simplest form that answers it is used. No pie, gauge or 3D.
- [ ] The title uses Terminology words. Every axis is labelled, with its unit.
- [ ] ⓘ popover: what a mark is, what the colours mean, which direction is good, and Learn More.
- [ ] Docs carry a "how to read it" passage at the linked anchor.
- [ ] Grey plus one accent. The same colour means the same thing across the page.
- [ ] No meaning carried by colour alone.
- [ ] Bars start at zero. Any dual axis is justified and both its axes are labelled.
- [ ] Uncertainty is shown as a range, and forecast looks different from actuals.
- [ ] Loading, failure and too-little-data states are distinct from "no data".
- [ ] Reads at phone width, in dark mode, enlarged and in the PDF export.
