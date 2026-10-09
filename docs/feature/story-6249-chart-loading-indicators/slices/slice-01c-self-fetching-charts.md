# Slice 01c: Charts that fetch for themselves say so too

**Goal:** the charts that fetch their own data (Percentiles Over Time, PBC Over Time, the Throughput run chart's and
the Predictability Score details' filtered views) show the same loading and could-not-load states as every other
chart, and an over-time chart never shows an empty area while its series loads. With 01a and 01b, this closes the
complaint (E10, review round 2).

## IN scope
- ~~`usePbcOverTime` / `usePercentilesOverTime`: the hand-kept per-selection cache becomes a query keyed (selection,
  owner, window) on the over-time variant of the options: the cache within a window is kept, so toggling back costs
  no request, as today (R5).~~ *Superseded by E5 (review round 2):* `usePbcOverTime` / `usePercentilesOverTime` are
  **not** moved to TanStack Query. Their `cancelled` cleanup and per-mount cache stay exactly as today, so toggling
  back within a window still costs no request. They only return a `status`: `loading` while the series is null for
  the current key, `error` when that fetch failed, `ready` otherwise. A window change is a new key with a null
  series, so a reload is S4's lone spinner, never an empty area (AC-1.4).
- Those widgets, `ThroughputRunChartCard` and `PredictabilityScoreDetailsWidget` report their status to the frame;
  ~~the reported status starts at `loading` (R11)~~ their hook reports `loading` from its first render, in a layout
  effect before paint, so a fresh frame is never painted ready (DDD-4, E1). Until this slice they read `ready`.
- A self-fetching chart stays mounted while the frame shows the spinner or the message, so its report is not lost.
- Over-time failures end in the S3 message (AC-1.6). A child's own failure shows even during a stepper debounce
  (DDD-5, E3).
- While loading, the body takes no pointer input, the in-chart toggles and filter switches included (M1).

## OUT of scope
- First-load frames (slice 02).

## Learning hypothesis
- **Disproves** "one reporter hook is enough for self-fetching charts" if any of them flips between loading and
  ready on its own, or never leaves loading, on the dev instance.
- **Confirms** it if a window change on Flow Overview dims the run chart with its filter on, and the over-time
  charts show a centred spinner, then their series.

## Acceptance criteria
AC-1.4, AC-1.5 (look), AC-1.6 for these charts, in `../feature-delta.md` (US-01).

## Dependencies
Slice 01b (the frame's status and the reporter context).

## Effort
Under a day. Four widgets, one hook; the over-time hooks only gain a returned status (E5), so most of the time goes
into the reporter and the filtered views' tests.
Reference class: the over-time charts' own cache work in Epic 5427 (same hooks).

## Dogfood
Dev instance, throttled: open Percentiles Over Time, switch selection, change the window, switch back. A spinner
in the frame each time the window changes; switching back within a window shows the series at once.
