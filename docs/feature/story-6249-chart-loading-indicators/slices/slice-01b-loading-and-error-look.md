# Slice 01b: A chart that is behind the window says so

**Goal:** after any window change, every chart the page feeds that is not yet showing the selected window is dimmed
with a spinner over it, and comes back only with that window's data, or with the could-not-load message.

## IN scope
- `WidgetShell` status (S1-S3): content at 40 % under a 24 px spinner, the header at full strength; on failure the
  chart removed, a warning icon and the copy from S3 ("This chart couldn't be loaded. Change the dates or reload to
  try again."), no Retry. The header is refined by S2/S4 (D1): View Data disabled while loading.
- `data-widget-status` and `aria-busy` on the frame (AC-1.8).
- The chart surface takes no pointer input while loading; the controls inside the body stay usable (S2, R2).
- The widget status rule: pending window → loading; any input failed for the committed window → could-not-load at
  once; anything in flight → loading; else ready (DDD-5, R4).
- E2E: the `MetricsPage` POM waits `MetricsWidget.waitUntilLoaded()` and `MetricsPage.waitUntilEveryChartHasLoaded()`
  (both exist since DISTILL) are wired in, and every spec that reads values after a window change goes through them,
  in this slice. Specs locate widgets by `dashboard-item-<key>` and the chart POMs. **`Screenshots.spec` calls
  `waitUntilEveryChartHasLoaded()` before every metrics capture.**
- E2E: the walking skeleton (`ChartLoadingIndicators.spec.ts`) is un-skipped here, because this is the slice where a
  frame first carries `data-widget-status`.
- Release-notes line on ADO #6249.

## OUT of scope
- The over-time charts and the self-fetching filtered views reporting through the frame (slice 01c).
- First-load frames and the overview widgets' own spinners (slice 02).

## Learning hypothesis
- **Disproves** "a per-widget loading state is enough to make a window change read as trustworthy" if, on the dev
  instance with real history, the dashboard flickers so much on a stepper burst that it reads as broken. That would
  point to a dashboard-level indicator instead.
- **Confirms** it if a preset change on the dev instance reads as "dim → each chart returns".

## Acceptance criteria
AC-1.1, AC-1.2, AC-1.6, AC-1.7, AC-1.8 in `../feature-delta.md` (US-01).
Acceptance check: the walking skeleton, the 17 E2E specs and `Screenshots.spec` pass through the waits on both CI
databases, and a screenshot run regenerates spinner-free PNGs identical in content.

## Dependencies
Slice 01a (the queries and `fetchStates`).

## Effort
About a day: the shell is small; the time goes into the E2E re-pointing and a screenshot run.
Reference class: Bug #6112 (E2E lazy-route stale-page race, same "wait for target-only content" fix).

## Dogfood
Dev instance, Team metrics, throttled: click *Last 90 days*. Every chart dims at once and each returns on its own;
hovering a dimmed chart shows nothing; the Throughput filter switch still works.
