# Slice 03 — Estimated sizes render hatched

**Feature**: epic-size-and-count-over-time · **ADO**: Epic #5585 · **Story**: US-03 · **Estimate**: ~4h
(+1h spike if MUI-X fights the pattern fill)
**Reference class**: none internal — no existing Lighthouse chart uses a pattern fill. Highest-uncertainty
slice, which is why it precedes the cheaper slice 04.

## Goal

A segment sized by the portfolio default renders hatched, so the day an epic flips from guess to real
breakdown is visible without hovering anything.

## IN scope

- SVG `<pattern>` def scoped to the chart instance, with a per-instance unique id (AC-3.6 — two
  deliveries can be expanded at once on the same page).
- Per-segment fill selection driven by `isUsingDefaultSize` (recorded in slice 02).
- Tooltip line for hatched segments: size is the portfolio default (not broken down).
- `null`/absent flag ⇒ solid, never hatched (AC-3.5) — absence is not truth.
- `DemoDataService` seed adjusted so at least one demo epic **flips** `isUsingDefaultSize` mid-window;
  without it the hatch has nothing to show in the demo instance or in the DELIVER screenshot.
- Vitest AC-3.1 … AC-3.6, asserting the fill reference and tooltip text — not a pixel snapshot.

## OUT of scope

- Any recorder or DTO change — the flag already flows from slice 02.
- Legend filtering (slice 04). Any change to the burnup's aggregate estimated line (slice 05).
- Colourblind-palette rework of the whole chart set.

## Pre-slice SPIKE (timeboxed 1h, only if needed)

Can a MUI-X `<BarChart>` bar rect take a `fill="url(#pattern-id)"` per data item? If not, options in
order: (a) a custom `slots.bar` renderer, (b) an overlay `<rect>` layer, (c) fall back to D6's rejected
alternative — lighter shade + tooltip — and record the fallback as a Changed Assumption in this file
rather than silently downgrading.

## Learning hypothesis

**Disproves** "MUI-X bar segments can carry a per-item pattern fill without leaving the library's
rendering model" **if** the spike exhausts (a) and (b). The consequence is a documented downgrade of D6,
not a dropped slice — the estimate/actual distinction still ships, in a weaker encoding.
**Confirms**, if it holds, that the "pinpoint the flip day" value the epic asks for is delivered.

## Acceptance criteria

AC-3.1 … AC-3.6 verbatim from `feature-delta.md`. Load-bearing pair:

- An epic hatched on days 1-2 and solid from day 3 renders exactly that transition (AC-3.4).
- Entries with no flag render solid (AC-3.5).

## Dependencies

Slice 02 (the flag must be recorded and parsed).

## Dogfood moment

Same day: on the demo instance, the seeded flip epic shows hatched-then-solid across the window —
the screenshot for `docs/portfolios/detail.md` is taken from exactly this state.
