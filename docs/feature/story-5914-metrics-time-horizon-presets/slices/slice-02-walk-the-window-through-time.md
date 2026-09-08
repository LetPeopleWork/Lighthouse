# Slice 02 — Walk the window through time

**Feature**: `story-5914-metrics-time-horizon-presets` | **Stories**: US-02 | **Estimate**: ~5h

## Goal

A Delivery Lead who has just read this period's numbers clicks the back stepper beside the date
label, the same-length window moves to the period immediately before, and every widget redraws over
it — and clicking it four times in a row costs one round of requests, not four.

## Learning hypothesis

**Debouncing the commit is enough to stop the refetch storm without the header lying about what is
on screen.**

Disproves if it fails: that a pending window and a committed window can share one label. The header
has to update on every click or the control feels broken, but the widgets below it cannot follow
until the commit lands — so for up to 500 ms the label names a window the dashboard is not showing.
If no visual treatment makes that honest, the debounce has to go and the storm has to be solved
another way (a request-level cancel, or committing on pointer-up of the last click), and this slice's
shape is wrong.

Confirms if it succeeds: that the 500 ms quiet period is the right seam, and that
`useVisitedCategories`' date-keyed reset token (`useCategorySelection.ts:85-104`) can stay exactly as
it is — the storm is prevented upstream of it rather than by weakening the token that Bug #5571
exists to enforce.

## Production data

Accepted against the **dev instance restored from a production backup**, on a Team with enough
history to step back three or four periods and still see populated widgets. The acceptance bar is
the request count: with the browser devtools network panel filtered to `/metrics/`, four rapid back
clicks produce **one** round of requests, and the window those requests carry is the fourth step
back — not the first, and not an average of them. Seeded demo data would prove the arithmetic and
miss the thing that breaks: on real data the fetch round is slow enough that a torn or intermediate
commit is visible on screen, which is precisely what the debounce exists to prevent.

## Dogfood moment

Same day: on the dogfood instance's own Team metrics, walk back four weeks one click at a time and
read the Throughput and Work Item Age widgets against what the same window shows when typed by hand
into the two pickers. Then step forward until the forward button disables, and confirm the window
ends exactly on today with its length unchanged.

## IN scope

- Backward and forward icon buttons flanking the date label in `DashboardHeader`, with accessible
  labels naming the step ("Previous 7 days", "Next 7 days" on a Team; 28 on a Portfolio).
- Owner-aware step size, from the same `ownerType` slice 01 already threads through: Team 7 days,
  Portfolio 28 days.
- A pending window held separately from the committed one; every click moves the pending window and
  re-renders the label immediately.
- A 500 ms quiet-period commit that calls slice 01's `applyDateRange` once with the settled window.
  The timer is cleared on unmount, so navigating away mid-debounce sets no state (AC-02.9).
- A not-yet-applied visual treatment on the header label while pending differs from committed, so
  the label never silently claims the widgets have caught up.
- The forward clamp: `end` never passes today, the window keeps its length when clamped (start moves
  with it), and the forward button renders disabled once the window already ends today.
- Vitest coverage: one Team click, one Portfolio click, four rapid clicks producing one commit,
  pending label ahead of commit, the clamp landing exactly on today with the length preserved, the
  disabled forward button, and unmount-mid-debounce firing nothing.
- Playwright: one spec asserting the request count across a burst — intercept `/metrics/` requests,
  click back four times inside the quiet period, assert one round and the fourth-step-back window.

## OUT scope

- The preset chips — slice 01.
- Any change to `useVisitedCategories` or its reset token. The storm is prevented before the commit,
  not by weakening the token (Bug #5571).
- Persistence (D10), opening-range changes (D1, D2), end-picker clamping (D16).
- A previous-period overlay or per-widget delta. This slice moves the window; it does not draw two.
- Any backend or client change (D14, D15).

## Acceptance criteria

AC-02.1 through AC-02.9 in `feature-delta.md`.

## Dependencies

**Slice 01** — the combined `applyDateRange` setter. This slice commits a two-ended window on every
step, so it cannot start before that setter exists.

## Reference class

Comparable to the debounced autosave on the settings surfaces — same shape (accumulate locally, commit
after a quiet period), same known hazard: a navigation that lands while a commit is still pending
drops the last edit. That is recorded as an E2E trap in this repo, and AC-02.9 plus the Playwright
`waitForResponse` on the committed window are what keep this slice out of it.
