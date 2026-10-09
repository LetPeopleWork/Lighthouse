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
  "picker opened" flag resets on a window change (DDD-2, R10).
- `useMetricsData` exposes `fetchStates` per key; nothing draws them yet.
- Test wrappers gain a `QueryClientProvider`; a test reads back each metrics query's resolved options.

## OUT of scope
- Any loading or error look (slice 01b). A failed fetch still leaves the previous value, as today.
- The over-time charts (slice 01c) and first-load frames (slice 02).

## Learning hypothesis
- **Disproves** "the query key alone is a sufficient race guard" if, on the dev instance with throttling, any chart
  settles on the previous window after two quick window changes.
- **Confirms** it if every out-of-order arrival test settles on the last window picked, and the dev instance agrees.

## Acceptance criteria
AC-1.3, AC-1.5 (data half) in `../feature-delta.md` (US-01). Value on its own: a chart never settles on the wrong
window, which is the "inaccurate" half of the complaint.

## Dependencies
None.

## Effort
About a day. Most of it goes into moving about thirty effects to queries and re-wrapping the existing tests.
Reference class: story 5914 (metrics window presets and stepper, same files).

## Dogfood
Dev instance (`:5169`, real history), Team metrics, network throttled to "Slow 4G": click *Last 90 days*, then
*Last 7 days* immediately, with the Throughput filter on. Every chart ends on the 7-day data.
