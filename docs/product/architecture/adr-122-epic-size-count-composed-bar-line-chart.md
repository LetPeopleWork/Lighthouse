# ADR-122: The epic size & count chart is one composed `ChartsContainer` (bar stack + line, dual y-axis), not two charts

- **Status**: Accepted (2026-07-31, DESIGN wave for ADO #5585 / Story #5614). Interaction mode = **propose**.
- **Date**: 2026-07-31
- **Feature**: `epic-size-and-count-over-time` (Epic 5585, slices 01-02)
- **Supersedes / extends**: nothing. Additive to the delivery-metrics application architecture (ADR-048/049/050).

## Context

ADO 5585 asks for one chart carrying two things: a daily **line** for how many epics are in the delivery,
and a daily **stacked bar** where each stack is one epic sized by its child item count. The two series
live on different scales — an epic count is single digits, a delivery's item total is hundreds — and the
whole value of the chart is reading one against the other on the same x-axis ("count flat while size
rose ⇒ an existing epic grew").

The existing three delivery charts are each a single MUI-X chart type: `LineChart` (burnup,
predictability) and `ScatterChart` (fever).

## Decision

Build the new chart as a **composed `ChartsContainer`** with `<BarPlot />` + `<LinePlot />` and **two
y-axes** — items on the left (bar stack), epic count on the right (line) — sharing one band x-axis of
recorded days.

This is not a new pattern in this codebase: `Lighthouse.Frontend/src/components/Common/Charts/RefreshHistoryChart.tsx:31-63`
already composes exactly this shape on the same MUI-X version (`@mui/x-charts` pinned at `9.0.1`) —
`ChartsContainer` with a `dataset`, `yAxis: [{ id: "items", position: "left" }, { id: "duration", position: "right" }]`,
a `type: "bar"` series and a `type: "line"` series, then `<BarPlot /> <LinePlot /> <MarkPlot /> <ChartsXAxis /> <ChartsYAxis />`.
The new chart follows that structure with epics in place of refresh logs.

## Alternatives considered

- **Two stacked charts sharing an x-axis** — rejected: two legends, two tooltips, two empty states, and
  the vertical gap between them defeats the point-in-time comparison the epic asks for. It also doubles
  the surface slice 04's legend filter has to reason about.
- **One y-axis for both series** — rejected: with a count of 7 against an item total of ~200 the count
  line renders as a flat trace glued to the axis. Dual axis is the only honest scaling.
- **A third-party composed-chart library** — rejected outright: no new frontend dependency for a shape
  MUI-X already supports and this repo already uses.

## Consequences

- The count line and the size bars can be read against each other and against the burnup directly above
  it in the 2×2 grid (`DeliverySection.tsx` Metrics tab).
- Two y-axes mean two axis labels; the right-hand axis must be explicitly labelled as the epic count or
  the chart is ambiguous.
- The composed form is `ChartsContainer`-based, so slot overrides (ADR-119) and legend handlers (ADR-120)
  are applied at plot level rather than through the single-chart convenience props the other delivery
  charts use.
- Slice 01 ships the container with only the line series and no bars; the bar series arrives in slice 02.
  The container shape does not change between the two — that is deliberate, so slice 02 is additive.
