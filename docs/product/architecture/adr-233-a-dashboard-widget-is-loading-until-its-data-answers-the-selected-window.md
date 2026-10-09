# ADR-233: A Dashboard Widget Is Loading Until Its Data Answers the Selected Window

**Status**: Proposed
**Date**: 2026-10-09
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
- Three widgets fetch for themselves: the two over-time charts (a hand-kept cache keyed by selection and window,
  guarded by a `cancelled` cleanup) and the Throughput run chart's filtered series (started from a click, never
  refetched on a window change). The Cumulative State Time scope, Work Item selection and candidate fetches are
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

- One query per service call, keyed by the call's name, the owner id and exactly the dates and choices it sends.
  The query returns only its current key's data, keeping the previous key's data as a placeholder to dim. A query
  is `loading` while it has no data for its current key, `error` when that fetch failed, `ready` otherwise. A fetch
  key's state is the most-behind of its queries, so one failing call marks only its own key. A key the service
  cannot answer for this owner is `ready`, by the same predicate that disables its query.
- Metrics queries do not cache across windows and do not retry: `staleTime: 0`, `gcTime: 0`, `retry: false`,
  `refetchOnWindowFocus: false`. Fetch timing stays what it is today, and a failure shows at once.
- A late answer for an old key lands in the old key's entry and is never read, so no separate race guard is
  written. A click-started choice that survives a window change (a filter, a Work Item selection) is part of its
  query's key; one that a window change resets keeps its existing generation counter where it already works.
- A widget's status is the most-behind of its inputs, with `loading` before `error` before `ready`: the page's
  commit-pending flag, each fetch key it needs, and whatever a self-fetching child reports.
- `WidgetShell` takes the status as one `'loading' | 'error' | 'ready'` prop and draws it as the maintainer
  decided: old content at 40 % opacity under a 24 px spinner with the header at full strength; a lone spinner and
  a title-and-info header when there is nothing to dim; on failure the chart removed, a warning icon and "This
  chart couldn't be loaded. Change the dates or reload to try again.", no Retry. While loading, the chart takes no
  pointer input and View Data is disabled. It exposes the status as `data-widget-status` for end-to-end waits.
  Self-fetching widgets report their status to the enclosing shell through context in a layout effect, before
  paint; this is the one place a status is set rather than derived, because a parent cannot read a child's
  queries during its own render.

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
defaults, so a reader has to look at them to know they do not cache. A request that never returns leaves its widget
loading indefinitely; no client timeout is added.

**Neutral.** The dimmed chart is not interactive while it loads and View Data is disabled, so nobody can open the
previous window's Work Items from a chart on its way to the new one.
