# ADR-119: An estimated epic size is hatched via a `slots.bar` renderer over a per-epic actual/estimated series split

- **Status**: Accepted (2026-07-31), **revised 2026-08-02** — the series split is withdrawn, the bar slot
  stands. See *Revision* at the end. The filename keeps its original slug so existing links resolve.
- **Interaction mode**: propose (DESIGN wave for ADO #5585 / Story #5616)
- **Date**: 2026-07-31
- **Feature**: `epic-size-and-count-over-time` (Epic 5585, slice 03)

## Context

DISCUSS D6 requires that an epic sized by the portfolio default (`Feature.IsUsingDefaultFeatureSize`,
set by `WorkItemService.ExtrapolateNotBrokenDownFeatures`) render **hatched** rather than solid, so the
day an epic flips estimate → actual is locatable at a glance rather than by hovering every bar. DISCUSS
recorded this as the feature's only genuine rendering unknown and budgeted a 1h spike, because MUI-X bar
charts expose no per-item pattern fill.

The burnup solves its analogous problem with a CSS selector on a DOM attribute —
`& .MuiLineChart-line[data-series="estimated"] { strokeDasharray: "2 4" }` (`DeliveryBurnupChart.tsx:20-27`).
The obvious move was to copy that for bars.

## Decision

**Two parts.** Part 1 was withdrawn on 2026-08-02 — see *Revision*. Part 2 shipped as written.

1. ~~**Series split.**~~ **(WITHDRAWN)** Each epic contributes **two** bar series to the same stack, same colour:
   `size::{referenceId}::actual` and `size::{referenceId}::estimated`. On any given day exactly one of
   the pair carries that epic's `totalItems` and the other is `null`, chosen by the day's
   `isUsingDefaultSize` flag. A `null`/absent flag routes to the **actual** series — absence is never
   read as "estimated" (DISCUSS AC-3.5).

2. **Bar slot.** Pass a custom `slots.bar` renderer that reads `ownerState.seriesId` and fills with
   `url(#hatch-{instanceId})` for the `::estimated` half, solid `color` otherwise. The `<pattern>` def is
   emitted once per chart instance with an id derived from React's `useId()`, so two simultaneously
   expanded deliveries cannot collide (DISCUSS AC-3.6).

Verified against the installed package (`@mui/x-charts@9.0.1`): `BarElement.d.ts` declares
`BarElementSlots { bar?: React.JSXElementConstructor<BarProps> }` and `BarElementOwnerState { seriesId, dataIndex, color, isFaded, isHighlighted, isFocused }`.
Custom chart slots are already an established pattern in this codebase —
`ProcessBehaviourChart.tsx:508` (`slots={{ mark: SpecialCauseMark }}`), `CumulativeStateTimeChart.tsx:488`
(`slots={{ tooltip: BarTooltipSlot }}`), plus `CycleTimeScatterPlotChart`, `EstimationVsCycleTimeChart`,
`FeatureSizeScatterPlotChart` and `WorkItemAgingChart`.

**The DISCUSS spike is therefore answered before the slice starts**: the mechanism exists, and the
documented fallback (lighter shade + tooltip only) is not needed. It stays on the record only in case
the hatch reads badly against the palette in practice.

## Alternatives considered

- **CSS selector on a `data-series` attribute, mirroring the burnup** — rejected because it does not
  exist for bars. `BarElement` renders no `data-series` attribute; the bar DOM carries only the
  `barClasses` set (`root`, `series`, `element`, `label`, ...). Verified in
  `node_modules/@mui/x-charts/BarChart/barClasses.d.ts`. Copying the burnup's trick would have failed at
  implementation time — this is exactly what the ADR spike was for.
- **One series per epic + a `dataIndex` lookup inside the slot** — workable, but the renderer would close
  over the day array and become a function of chart state rather than of its own props, which is harder
  to test and re-renders more. The split keeps the renderer a pure function of `seriesId`.
- **Pre-rendered hatched PNG fills** — rejected: does not scale with the theme or with per-epic colour.

## Consequences

- Series count is 2n for n epics (30 series for a 15-epic delivery). Stacking must be pinned with an
  explicit `stack` id and explicit per-series `color`, or MUI-X's implicit ordering will shuffle colours
  between days.
- The legend must show **one** entry per epic, not two. The `::estimated` twin is left unlabelled so
  MUI-X omits it from the legend; this interacts with ADR-120's filter and is called out as an open
  question for DISTILL to assert.
- Tooltips must not show the `null` twin for a day. MUI-X's null handling is assumed to skip it; DISTILL
  asserts it rather than trusting it.
- Hatch state is assertable in tests by series id — no pixel snapshots (DISCUSS AC-3.2).

---

## Revision (2026-08-02, during DELIVER of slice 02)

**The series split (Decision part 1) is withdrawn. One bar series per epic, keyed on `referenceId`.**
The `slots.bar` renderer (part 2), the `useId()`-scoped `<pattern>`, and the AC-3.5 rule that an absent
flag means *actual* are all unchanged.

The renderer now reads **`ownerState.dataIndex` as well as `ownerState.seriesId`**, looking the day's
flag up in a per-epic-per-day map the component already builds:

```ts
slots.bar = ({ seriesId, dataIndex, color }) =>
  estimatedByEpicDay[seriesId]?.[dataIndex]
    ? <rect fill={`url(#hatch-${patternId})`} />
    : <rect fill={color} />
```

This is precisely the option the *Alternatives considered* section rejected. It is reopened because
shipping slices 01 and 02 produced evidence the DESIGN wave did not have:

1. **The null-twin tooltip problem stopped being hypothetical.** This ADR listed it as a consequence to
   "assert rather than trust". Slice 02 hit the same class of bug without any split at all — every epic
   drew a blank tooltip row on days before it joined the delivery, which Benjamin caught on review. The
   fix was a `valueFormatter` returning `null` (MUI's `ChartsAxisTooltipContent` drops such a row). Under
   the split, **every** epic would carry a permanently-null twin on **every** day, making that the
   steady state rather than an edge case.
2. **Slice 02 shipped `series.id === referenceId`** with green acceptance tests asserting it
   (`barSeries().map(e => e.id)` ordering, `valuesFor(referenceId)`). The split renames every series and
   rewrites working assertions for no user-visible gain.
3. **Slice 04's legend filter reasons over series.** 2n series doubles what it must de-duplicate — and
   the "leave the `::estimated` twin unlabelled" trick this ADR proposed is itself an open question it
   raised against itself.
4. **The original objection weakened.** "The renderer closes over the day array and becomes a function of
   chart state" — the component already builds that map to produce the dataset, and the renderer stays a
   pure function of `(seriesId, dataIndex)` given it. DISTILL's scenarios call the slot directly with a
   synthetic `ownerState`, so it is no harder to test than the `seriesId`-only form.

**What this costs**: the renderer needs a second lookup key, and it is no longer decidable from
`seriesId` alone. Accepted as the smaller price.

**Unchanged consequences**: stacking still needs an explicit `stack` id and explicit per-series `color`;
hatch state is still assertable without pixel snapshots. **Withdrawn consequences**: the legend
de-duplication requirement and the null-twin tooltip requirement disappear with the split that caused
them.
