# Slice 01a: A chart never settles on the wrong window

**Goal:** after any window change on a Team or Portfolio metrics dashboard, every chart ends up showing the
selected window's data. A slow answer for a window already left never lands. No visual change yet.

## IN scope
- The `useMetricsData` fetches move to TanStack queries keyed by the request (owner, the dates as local-day strings,
  the choices the call sends), one query per service call, all spreading one shared `metricsQueryOptions` object
  (no retry, no cache across windows), with `keepPreviousData` (DDD-1, R6, R9).
- The race guard is the query key: superseded answers never land (D3, AC-1.3).
- Every query function returns `null` where the service answered `undefined` (R8).
- The Throughput run chart's and the Predictability Score details' filtered views follow the window: a query keyed
  (view, owner, window) while the filter is on (AC-1.5, DDD-2).
- The Throughput PBC filter becomes its own fetch key (DDD-10).
- Cumulative Time per State: the scope, the chosen Work Items and the picker candidates follow the window; the
  "picker opened" flag resets on a window change (DDD-2, R10). The selection and scope queries are fetch keys of
  `stateTimeCumulative`, each `ready` while it does not apply (no Work Items chosen, no scope chosen) (DDD-11, E4).
- **Picker candidates failure (E6, review round 2):** a failed candidates request leaves the picker's list empty and
  the chart's numbers in place, as today: a named DoD-4 exception. The request has no `catch` today
  (`BaseMetricsView.tsx:1550`); the `catch` is added in the same commit that un-skips its spec.
- **A failed fetch keeps the last success (E2, review round 2):** when a fetch for the selected window fails,
  `useMetricsData` keeps that metric's last successful value, exactly as today, through a per-metric last-success
  reference that is reset when the owner (Team or Portfolio, id or type) changes. So "no visual change" is true.
- `useMetricsData` exposes `fetchStates` per key; nothing draws them yet.
- Test wrappers gain a `QueryClientProvider`; a test reads back each metrics query's resolved options.

## OUT of scope
- Any loading or error look (slice 01b). A failed fetch still leaves the previous value, as today (E2); 01b
  replaces that with the could-not-load frame.
- The over-time charts (slice 01c; they are not moved to TanStack Query, E5) and first-load frames (slice 02).

## Learning hypothesis
- **Disproves** "the query key alone is a sufficient race guard" if, on the dev instance with throttling, any chart
  settles on the previous window after two quick window changes.
- **Confirms** it if every out-of-order arrival test settles on the last window picked, and the dev instance agrees.

## Acceptance criteria
AC-1.3, AC-1.5 (data half) in `../feature-delta.md` (US-01), and DoD-4's picker exception (E6). Value on its own: a chart never settles on the wrong
window, which is the "inaccurate" half of the complaint.

## Dependencies
None.

## Effort
About a day. Most of it goes into moving about thirty effects to queries and re-wrapping the existing tests.
Reference class: story 5914 (metrics window presets and stepper, same files).

## Dogfood
Dev instance (`:5169`, real history), Team metrics, network throttled to "Slow 4G": click *Last 90 days*, then
*Last 7 days* immediately, with the Throughput filter on. Every chart ends on the 7-day data.
