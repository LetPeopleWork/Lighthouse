# Slice 01: A chart that is behind the window says so

**Goal:** after any window change on a Team or Portfolio metrics dashboard, every chart not yet showing the
selected window is dimmed with a spinner over it. It comes back only with that window's data, or with a
could-not-load message.

## IN scope
- A loading state in `WidgetShell`: dimmed content, a spinner overlay, the header untouched (D1).
- Every dashboard fetch source tells its widget when it hasn't answered the selected window: `useMetricsData`
  keys, `ThroughputRunChartCard`'s own fetch, `usePbcOverTime` and `usePercentilesOverTime` (D5, AC-1.5).
- Superseded responses discarded in `useMetricsData` and `ThroughputRunChartCard` (D3).
- Over-time charts show the spinner in an empty frame instead of a blank area (AC-1.4).
- Fetch failure ends in the could-not-load message (D6, copy from the DISTILL sketch).
- Release-notes line on ADO #6249.

## OUT of scope
- First-load frames and the overview widgets' own spinners (slice 02).
- Forecast and backtest results (D4).

## Learning hypothesis
- **Disproves** "a per-widget loading state is enough to make a window change read as trustworthy" if, on
  the dev instance with real history, the dashboard flickers so much on a stepper burst that it reads as
  broken. That would point to a dashboard-level indicator instead.
- **Confirms** it if a preset change on the dev instance reads as "dim → each chart returns", with no chart
  ever settling on the previous window.

## Acceptance criteria
AC-1.1 … AC-1.7 in `../feature-delta.md` (US-01).

## Dependencies
None.

## Effort
About a day. Most of it goes into threading per-widget loading through `useMetricsData` and the two
over-time hooks. The shell overlay itself is small.
Reference class: story 5914 (metrics window presets and stepper, same files).

## Dogfood
Dev instance (`:5169`, real history), Team metrics, network throttled to "Slow 4G": click *Last 90 days*,
then *Last 7 days* immediately. Every chart dims, and each returns with 7-day data.
