# ADR-233: A Dashboard Widget Is Loading Until Its Data Answers the Selected Window

**Status**: Proposed
**Date**: 2026-10-09 (amended the same day after review round 1: error precedence, over-time caching, shared query
options, placeholders, nullish answers, key format, in-chart controls, reporter default)
**Feature**: story-6249-chart-loading-indicators (ADO User Story #6249)
**Decider**: Morgan (Solution Architect), interaction mode = PROPOSE (maintainer AFK; for confirmation). The
visual treatment was decided by the maintainer in the sketch review of the same day.

---

## Context

A user changed the metrics window and the charts kept showing the previous window, looking final. The story
asks for a loading state in the frame every metrics widget shares, and DISCUSS locked what "loading" means: a
widget is loading from the moment the window changes until its own data for that window arrives, including the
stepper's 500 ms debounce, and a failure must end in a message, never a spinner or an undimmed old chart.

Facts from the code that shape the decision:

- `useMetricsData` holds one value per metric and one effect per fetch key. It has no pending flag, no guard
  against a superseded response, and its failures only reach `console.error`. Its effects depend on different
  inputs: most on the whole window, three on the window end only, SLE risk on the Team alone, blackout periods on
  nothing. Several effects batch several calls under one `Promise.all` with one `catch`.
- Four widgets fetch for themselves: the two over-time charts (a hand-kept cache keyed by selection and window,
  guarded by a `cancelled` cleanup), and the filtered views of the Throughput run chart and of the Predictability
  Score details (started from a click, never refetched on a window change). The Cumulative State Time scope, Work Item selection and candidate fetches are
  click-started and unguarded.
- No metrics service method accepts an `AbortSignal`.
- TanStack Query is installed, its `QueryClient` provided at the app root, and two features already use it.
- `WidgetShell` wraps every dashboard item in one place, and `getFetchRequirementsForWidget` already lists every
  fetch key a widget needs to render completely.

Quality attributes, in order: **correctness** (never present one window's numbers as another's), **immediacy**
(the dim appears on the first frame after the change), **one look** across all widgets, **testability** without
a running backend.

---

## Decision

**A widget's loading state is derived during render from whether it holds data for the request the current
inputs would send. Every dashboard fetch is a TanStack Query whose key is that request.**

- One query per service call, keyed by the call's name, the owner id and exactly the dates and choices it sends,
  dates as local-day strings (`formatLocalDate`, as the over-time charts already key their cache). Each query
  function turns an `undefined` answer into `null`, so "answered with nothing" never reads as "not answered". The
  query returns only its current key's data. The page-fed queries keep the previous key's data as a placeholder to
  dim; the over-time queries keep none, so they show a lone spinner. A query is `loading` while it has no data for
  its current key or shows a placeholder, `error` when that fetch failed, `ready` otherwise. One failing call marks
  only its own fetch key. A key the service cannot answer for this owner is `ready`, by the same predicate that
  disables its query.
- One shared set of metrics query options, which every metrics query spreads: no cache across windows
  (`staleTime: 0`, `gcTime: 0`), `retry: false`, `refetchOnWindowFocus: false`. The over-time queries use a
  documented variant with a long `staleTime` and `gcTime`, because they already ship a per-selection cache within a
  window: toggling back to a selection costs no request, as today, while a window change still moves to a new key.
  So fetch timing stays what it is today for every chart, and a failure shows at once. A test reads back the options
  each metrics query resolved to.
- A late answer for an old key lands in the old key's entry and is never read, so no separate race guard is
  written. A click-started choice that survives a window change (a filter, a Work Item selection) is part of its
  query's key; one that a window change resets keeps its existing generation counter where it already works.
- A widget's inputs are the page's commit-pending flag, each fetch key it needs, and whatever a self-fetching child
  reports. While the stepper's debounce is pending, every widget is `loading`. Once the window is committed, a widget
  with any input failed for that window is `error` at once, without waiting for its in-flight siblings; otherwise it
  is `loading` while any input is in flight; otherwise `ready`. A failure for a window already left is never read,
  because it sits under the old key.
- `WidgetShell` takes the status as one `'loading' | 'error' | 'ready'` prop and draws it as the maintainer
  decided: old content at 40 % opacity under a 24 px spinner with the header at full strength; a lone spinner and
  a title-and-info header when there is nothing to dim; on failure the chart removed, a warning icon and "This
  chart couldn't be loaded. Change the dates or reload to try again.", no Retry. While loading, the chart surface
  takes no pointer input and View Data is disabled, while the controls drawn inside the chart's body (filters,
  metric and percentile toggles, scope selectors, the Work Item picker, legend toggles) stay usable: using one only
  starts another fetch or redraws the dimmed chart. It exposes the status as `data-widget-status` for end-to-end
  waits, and marks a loading frame `aria-busy`. Self-fetching widgets report their status to the enclosing shell
  through context in a layout effect, before paint; this is the one place a status is set rather than derived,
  because a parent cannot read a child's queries during its own render. The reported status starts at `loading`,
  so such a widget is never briefly `ready` before its child has spoken.

---

## Alternatives considered

**A hand-rolled request identity per fetch key, recorded by each effect, with a `cancelled` cleanup** (the first
draft of this ADR). It gives the same render-time derivation with no library. Rejected after review: each
identity has to list exactly the inputs its effect sends, and a mismatch either spins a widget for ever or shows
an old window as current; only tests would hold that rule. A query key cannot disagree with its call, because the
call reads its arguments from the key. It remains the fallback if adopting TanStack Query here is declined.

**A pending flag set when each fetch starts**, the shape `LoadingAnimation` callers use. Rejected: an effect runs
after the render that changed the window is committed, and when the change comes from the stepper's timer the
browser may paint that render first, showing the old chart undimmed for a frame. Writing the flag in a layout
effect or under `flushSync` closes that gap but adds a state write per key per window change across about thirty
keys.

**Clear every value to `null` on a window change.** Rejected: it throws away the old chart the maintainer wants
shown dimmed, and it cannot tell loading from failed.

**`AbortController` as the race guard.** Rejected for now: no service method takes a signal, so it means changing
about forty methods and every test double, and it buys bandwidth, not correctness.

**Errors wait until nothing is in flight** (`loading` before `error`, this ADR's first precedence). Rejected in
review: a widget whose first input failed keeps a spinner up, promising data that cannot come, until its slowest
sibling answers.

**`gcTime: 0` for every metrics query, the over-time charts included.** Rejected in review: it would remove the
per-selection cache those charts already ship, so toggling back to a selection would fetch again.

**`inert` on the whole widget body while loading.** Rejected in review: a child cannot opt out of `inert`, so it
would disable the filters and selectors inside the chart along with the chart.

**Each self-fetching widget draws the shared overlay itself.** Rejected: two places would draw the state, and the
shell's `data-widget-status` would be wrong for exactly the widgets that fetch on their own.

---

## Consequences

**Positive.** "Is this widget behind?" has one answer, right on the very render that changed the window. A
superseded response can no longer overwrite a newer one, including in the click-started fetches that had no guard.
Failures become visible, per call. End-to-end tests get a "loaded" signal that cannot be briefly `ready` before a
fetch starts. Any later dashboard surface that fetches by window can use the same shape.

**Negative.** `useMetricsData` is rewritten internally (about thirty queries), and every test that renders it, the
dashboard or the self-fetching widgets needs a `QueryClientProvider`. Metrics queries override the app-wide query
defaults, so a reader has to look at the one shared options object to know they do not cache. A request that never returns leaves its widget
loading indefinitely; no client timeout is added.

**Neutral.** The dimmed chart surface is not interactive while it loads and View Data is disabled, so nobody can
open the previous window's Work Items from a chart on its way to the new one. Its in-chart controls stay usable.
On failure the message replaces the body, controls included; changing the dates or reloading is the way back.
