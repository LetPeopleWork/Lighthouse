# Slice 01b: A chart the page feeds says when it is behind the window

**Goal:** after any window change, every chart **the page feeds** that is not yet showing the selected window is
dimmed with a spinner over it, and comes back only with that window's data, or with the could-not-load message.
The over-time and self-fetching charts are outside this slice's acceptance and arrive in 01c; 01a + 01b + 01c
together close the complaint (E10, review round 2).

## IN scope
- `WidgetShell` status (S1-S3): content at 40 % under a 24 px spinner, the header at full strength; on failure the
  chart removed, a warning icon and the copy from S3 ("This chart couldn't be loaded. Change the dates or reload to
  try again.", confirmed by M4), no Retry, under a title + info header with no rating chip, trend or View Data (M2).
  The header is refined by S2/S4 (D1): View Data disabled while loading.
- `data-widget-status` and `aria-busy` on the frame (AC-1.1, formerly AC-1.8).
- ~~The chart surface takes no pointer input while loading; the controls inside the body stay usable (S2, R2).~~
  **The whole body takes no pointer input while loading (M1, Maintainer 2026-10-09):** `pointer-events: none` on the
  body box `widget-shell-body-<key>` (`WidgetShell.tsx:385`, which gains that test id), with no child opting back
  in. The chart and every control inside it (filter switches, toggles, legend chips, scope selectors, the Cumulative
  Time per State picker) are blocked; the info button still works.
- The widget status rule (DDD-5, restated E3): while the stepper debounce is pending the page's keys read loading;
  then any input failed → could-not-load at once; anything in flight → loading; else ready.
- 01a's last-success reference (E2) goes: a failure now shows the could-not-load frame.
- The self-fetching charts read `ready` in this slice: no child reports yet, and a shell with no reporter
  contributes nothing (E1). That is honest about what 01b covers.
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
  instance with real history, the page-fed charts flicker so much on a stepper burst that the dashboard reads as
  broken. That would point to a dashboard-level indicator instead.
- **Confirms** it if a preset change on the dev instance reads as "the page-fed charts dim → each returns".

## Acceptance criteria
AC-1.1 (which now holds the former AC-1.7 and AC-1.8, E9), AC-1.2, AC-1.6 in `../feature-delta.md` (US-01), for the
charts the page feeds.
Acceptance check: the walking skeleton, the 17 E2E specs and `Screenshots.spec` pass through the waits on both CI
databases, and a screenshot run regenerates spinner-free PNGs identical in content.

## Dependencies
Slice 01a (the queries and `fetchStates`).

## Effort
About a day: the shell is small; the time goes into the E2E re-pointing and a screenshot run.
Reference class: Bug #6112 (E2E lazy-route stale-page race, same "wait for target-only content" fix).

## Dogfood
Dev instance, Team metrics, throttled: click *Last 90 days*. Each chart the page feeds dims at once and returns on
its own; hovering a dimmed chart shows nothing, and the Throughput filter switch does not respond until its chart is
back (M1). The over-time charts and the filtered views are 01c's.
