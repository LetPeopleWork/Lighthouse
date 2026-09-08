# ADR-189: The Metrics Window Is Owned by a Hook, and Its Debounce Is the Shipped Revision-keyed Effect

**Status**: Accepted
**Date**: 2026-09-08
**Feature**: story-5914-metrics-time-horizon-presets (ADO User Story #5914)
**Decider**: Morgan (Solution Architect), interaction mode = PROPOSE

---

## Context

Story #5914 adds two controls to the metrics dashboard's time window: a row of named preset windows
in the date popover, and a back/forward stepper pair that moves the same-length window through time.
Frontend only — no endpoint, DTO, migration or client change.

Three grounding facts from the code decided this, none of which are visible from the story text.

**The two existing date setters cannot be composed.** `BaseMetricsView.tsx:1232-1252` holds
`handleStartDateChange`, which writes `updateDateParams(date, endDate)`, and `handleEndDateChange`,
which writes `updateDateParams(startDate, date)`. Both close over the current render's values, and
`updateDateParams` rebuilds the params from a `searchParams` that is stale by the same mechanism.
Calling them in sequence to move both ends of a window therefore makes the second call rewrite the
first call's start back to its old value — a torn window, written to the URL and then fetched. Every
control this story adds moves both ends at once, so all of them sit on top of this trap.

**Every committed window change refetches the whole current category.** `useVisitedCategories`
(`useCategorySelection.ts:68-104`) builds its reset token from the formatted start and end dates
(`BaseMetricsView.tsx:1267-1270`), so a new window collapses the visited set to the current category
and re-fires every fetch for it. That gate is deliberate — it is Bug #5571's fix, and it is
monotonic precisely so a return visit does not refetch. A stepper the user clicks four times in a row
would drive four full refetch rounds through it, three of them for windows nobody wants to see.

**The codebase already ships the debounce this needs.** `useDebouncedRevisionRun`
(`TeamForecastView.tsx:30-39`) is an effect keyed on a monotonic revision counter with a `setTimeout`
inside and a `clearTimeout` in its cleanup. It is module-private to `TeamForecastView`.
`DEBOUNCE_MS = 300` appears twice — there (L28) and in `useModifySettings.ts:50`.

ADR-029 answered the structurally identical question for `remove-action-buttons`: where does a
debounced, multi-field commit mechanism live? It put the mechanism and its state machine **in a
hook** (`useModifySettings`) and reduced the presentational surface to a status indicator, driven by
correctness of the commit machine, one identical mechanism across consuming surfaces, and
testability of the debounce in isolation.

Quality attributes here, in priority order: **correctness of the two-ended write** (no torn window
ever reaches the URL or a request), then **request economy** (a burst of clicks costs one round),
then **testability** (the debounce and the clamp unit-testable without mounting a 1938-line
dashboard), then **no-regression** (Bug #5571's gate and Bug #5566's local-date encoding both
survive untouched).

---

## Decision

**Extract the metrics window into a `useDateRange` hook that owns the committed window, the pending
window, the forward clamp and the URL round-trip, and expose exactly one write path,
`applyDateRange(start, end)`. Debounce the commit with the shipped revision-keyed effect, promoted
out of `TeamForecastView` into a shared hook.**

Concretely:

- `useDateRange` owns `startDate` / `endDate` (committed), the pending window, `applyDateRange`,
  `stepWindow(direction)` and the clamp. `handleStartDateChange` and `handleEndDateChange` survive as
  thin delegates to `applyDateRange`, so there is one write path rather than two that disagree. The
  stale-closure torn write becomes unreachable by construction, not by care.
- `useDebouncedRevisionRun` moves to `src/hooks/useDebouncedRevisionRun.ts` and takes its delay as a
  parameter, defaulting to the 300 ms its two existing callers use. `TeamForecastView` imports it
  instead of declaring it; its behaviour does not change.
- The metrics window passes **500 ms**, not 300. The existing constant is tuned for keystroke-driven
  work — autosave as the user types, a forecast recomputed as a number is edited. A stepper is
  clicked deliberately, and the gap between two intended clicks is longer than the gap between two
  keystrokes; firing in that gap is the precise failure the debounce exists to prevent. The two
  values are different because the inputs are different, and both stay named where they are used.
- The pending window lives in the hook and never reaches the URL. Only a committed window writes
  `?startDate` / `?endDate`, still with `replace: true`, still in local Y/M/D — so a link copied
  mid-burst names a window that was really fetched, and browser history gains one entry rather than
  four.
- `useVisitedCategories` and its reset token are **not touched**. The refetch storm is stopped
  upstream of the commit. Weakening the token to absorb it would reintroduce the refetch-on-return
  that Bug #5571 exists to prevent.
- Two new presentational components, `DateRangePresets` (the chip row, inside the popover) and
  `DateWindowStepper` (the back/forward pair, on the header bar), are props-only: they compute no
  dates and know no owner type. The owner-varying facts — which presets, how big a step — live
  together in one pure module keyed on the `ownerType` that `BaseMetricsView.tsx:1254-1255` already
  derives.

---

## Alternatives considered

**Leave the window in `BaseMetricsView` and add the pending state beside it.** The window state does
already live there, which is the whole of the argument for it. Against: the component is 1938 lines
and this would add a pending window, a clamp and a debounce to it, leaving all three testable only by
mounting a dashboard — against the third quality attribute, and against the precedent ADR-029 set for
the same shape of problem.

**A bespoke `useDebouncedValue` hook, or a ref-held timer inside the stepper.** Both were the
candidates carried out of DISCUSS, and both are wrong in the same way: they write a second debounce
idiom into a codebase that already ships one and tests it. The ref-held timer additionally makes
unmount safety something a reviewer has to notice rather than something the idiom provides.

**Reuse `useModifySettings`' autoSave machine.** It is also a debounced multi-field commit, but it
persists a form to the server behind an RBAC `canSave` input, with `saving | saved | error` states
and a stale-response sequence guard. Nothing here is persisted and nothing can fail — the commit is a
URL write. Sharing it would mean carrying a save-state machine that can never leave `idle`. Its
*precedent* is reused; its code is not.

**Absorb the click burst by relaxing the visited-category reset token.** Rejected outright: the token
is Bug #5571's fix and its monotonicity is what makes a return visit free. Trading that for a
debounce this feature needs anyway would pay for a burst of clicks with every category navigation.

---

## Consequences

**Positive.** The torn two-ended write is structurally impossible rather than avoided. A burst of
stepper clicks costs one round of requests regardless of length. The window's arithmetic — clamp,
step, preset — is unit-testable without a dashboard. `BaseMetricsView` gets smaller. One debounce
idiom exists in the frontend instead of two, and `TeamForecastView` picks up the shared version for
free.

**Negative.** Two debounce *values* now exist (300 ms and 500 ms) with no shared constant, so a reader
must look at the call site to know which applies — accepted deliberately, since a single shared value
would be wrong for one of the two input styles. Promoting `useDebouncedRevisionRun` touches
`TeamForecastView`, a file this story otherwise has no business in; the move is behaviour-preserving
and its existing tests are the guard.

**Neutral.** The header label can name a window the widgets have not caught up to for up to 500 ms.
That is inherent to any debounce and is handled by rendering the pending window as visibly
not-yet-applied — the one thing in this design a user could otherwise catch us lying about.
