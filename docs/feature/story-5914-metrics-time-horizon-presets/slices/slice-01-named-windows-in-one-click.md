# Slice 01 — Named windows in one click

**Feature**: `story-5914-metrics-time-horizon-presets` | **Stories**: US-01 | **Estimate**: ~4h

## Goal

A Delivery Lead opening the date popover on a Team's or Portfolio's metrics clicks one named window
— `Last 30 days`, `Last 90 days` — and the whole dashboard redraws over it, with both ends of the
window landing in the URL together.

## Learning hypothesis

**A two-ended window change cannot reuse the existing per-end setters.**

Disproves if it fails: the assumption that a preset is "just two date changes". `BaseMetricsView.tsx:1232-1252`
holds two setters that each write the URL from the *other* end's current-render value, plus a
`updateDateParams` that rebuilds from a stale `searchParams`. Calling them in sequence writes a
window that is half old and half new, and fires a fetch for it. If AC-01.4 fails on the first
implementation, that is the confirmation — and it is far cheaper to find it here, with four chips on
screen and a visible URL, than underneath slice 02's debounced stepper where the torn window flashes
past between commits.

Confirms if it succeeds: that a single `applyDateRange(start, end)` setter is a sufficient
foundation, and slice 02 can be built entirely on top of it with no further change to how the window
is committed.

## Production data

Accepted against the **dev instance restored from a production backup** (`Restore-DbBackup.ps1`), not
the checked-in `.db` and not synthetic fixtures. The acceptance bar: on a real Team with real
history, clicking `Last 90 days` produces widgets whose values are consistent with the same window
typed by hand into the two pickers. A 90- or 180-day preset over seeded demo data proves the
arithmetic and misses what actually breaks — real instances have Teams whose history is shallower
than the preset asks for, which is where an empty or partial window has to render honestly rather
than as a crash or a silently-clamped range.

## Dogfood moment

Same day: open the Lighthouse dogfood instance's own Team metrics, click each of the four chips in
turn, and check the header label and the URL after each. Then hard-reload on the resulting URL and
confirm the same window comes back (AC-01.8).

## IN scope

- A combined `applyDateRange(start: Date, end: Date)` setter that writes both ends of the window and
  both URL params in one navigation, built from the *arguments* rather than from closed-over state.
  This is a precursor commit inside this slice, not a slice of its own.
- The two existing per-end handlers delegate to it, so there is one write path and not two.
- A presentational preset chip row rendered above the two pickers inside `DateRangeSelector`,
  receiving its preset list and its click handler as props — it computes no dates and knows nothing
  about owner type.
- The owner-aware preset lists, derived from the `ownerType` that `BaseMetricsView.tsx:1254-1255`
  already computes: Team `[7, 14, 30, 90]`, Portfolio `[30, 90, 180]`.
- Selected-state rendering: the chip whose window matches the current one is visually selected; none
  is selected after a manual picker edit.
- `date-fns` arithmetic only. `dayjs` does not enter this surface.
- Vitest coverage: the Team chip set, the Portfolio chip set, both URL params written from one click,
  a preset clicked from a past-anchored window returning to today, selected-state on match and
  absent after a manual edit, and an existing `?startDate`/`?endDate` pair surviving mount.
- Playwright: one walking-skeleton spec through an extended `MetricsDateRange` POM — click a chip,
  wait on the request carrying the new `endDate`, assert the header label.

## OUT scope

- The steppers, the debounce, the pending-window label and the forward clamp — all slice 02.
- Any change to the Team's or Portfolio's opening range (D1, D2). `TeamMetricsView.tsx:117-128` and
  `PortfolioMetricsView.tsx:68` are not edited.
- Persistence of any kind (D10).
- Constraining the manual end picker to today (D16).
- Any backend or client change (D14, D15).
- Docs prose and screenshot regeneration — those land at feature finalization, after slice 02.

## Acceptance criteria

AC-01.1 through AC-01.8 in `feature-delta.md`.

## Dependencies

None. Every surface it touches is shipped and stable.

## Reference class

Comparable to the `ThroughputChartFilterToggle` and `CumulativeStateTimeScopeControl` additions — a
presentational control added to an existing metrics surface, with the state kept in
`BaseMetricsView`. Those landed inside a day each.
