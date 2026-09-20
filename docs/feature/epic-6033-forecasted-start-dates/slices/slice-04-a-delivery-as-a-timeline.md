# Slice 04 — A Delivery shows its Features as a timeline

**Feature**: epic-6033-forecasted-start-dates · **ADO**: to create · **Story**: US-04
**Estimate**: ~2h evaluation + ~6h build
**Reference class**: none that fits. The existing charts are `@mui/x-charts` series over a numeric or
categorical axis; this is one horizontal bar per row against a date axis. The nearest thing in the
product is the process behaviour chart's axis handling, and the nearest thing in the ecosystem is a
component we do not own (S19).

## Goal

Open a Delivery, choose Timeline, and see its Features as bars against a date axis — one percentile
selector moving both ends of every bar together.

## IN scope

- A Timeline tab beside Features and Metrics on the Delivery. The tab structure exists (S20).
- One bar per Feature, in `WorkItemBase.Order` (S15) — the board's order, which is also the order the
  simulation itself works in.
- A P70 / P85 / P95 selector, defaulting to P70, moving the left and right edge of every bar together
  (D10). A bar is always one scenario, never a P70 start welded to a P85 finish.
- A started Feature's bar begins at its observed start date (D5).
- The Delivery's target date marked on the axis, where it has one.
- A Feature with no forecast listed with no bar and a stated reason, rather than dropped.
- Premium gate, using the Delivery surface's existing notice (D8).
- Light and dark, and the narrowest width the Delivery view supports.
- Docs saying plainly what an out-of-order board does to the picture (AC-4.9, D6). A Feature nobody has
  started, drawn as starting now while work sits lower in the order, is the timeline reporting the board
  rather than misreading it. Undocumented, that intended oddness arrives as a bug report.

## OUT of scope

- Dependencies drawn between bars. Slice 05, severable.
- Per-team sub-lanes under a multi-team Feature's bar. Slice 06, severable. One bar per Feature here.
- A timeline anywhere but on a Delivery.
- Dragging, editing, or writing anything back from the timeline. It is a read.
- Purchasing a licence. The evaluation may recommend one; the decision is not this slice's to take.

## Learning hypothesis

**Disproves, if it fails**: D11 — that a timeline can be built from what is installed, and therefore
that this Epic ends without a commercial dependency.

**Confirms, if it succeeds**: the Epic closes inside its own slices.

## Pre-slice SPIKE — build versus buy

**Yes, timeboxed to 2 hours, before any production code.** This is P8, the Epic's only open
pre-requisite.

The question is not "is a Gantt hard" in the abstract. It is: **can `@mui/x-charts` 9.0.1, already
installed, draw one horizontal bar per row against a date axis, themed like the rest of the product, in
light and dark, at the Delivery view's narrowest width?** What is being drawn is geometrically simple —
the hard parts of a commercial Gantt (drag-to-reschedule, editable durations, nesting, virtualised
scroll over thousands of rows) are all out of scope here.

Produce, within the timebox:

1. A throwaway render of five hardcoded bars on a date axis, in both themes.
2. The line count and the list of things that had to be hand-rolled.
3. A recommendation with a number attached — hours to finish, against the MUI X Premium licence cost.

Ask one question beyond the timebox's own: **can a bar carry sub-lanes?** Slice 06 needs a summary bar
with per-team lanes under it. It is severable, so it does not gate this decision — but a component that
forecloses it, or charges another tier for it, is worth knowing about while the choice is still open.

**Candidates, so the timebox is not spent finding them.** Two hours is not long enough to survey the
field and build a prototype, so the field is listed here and the hours go on the prototype:

| Candidate | What it is | Why it might not fit |
|---|---|---|
| `@mui/x-charts` 9.0.1 | Already installed, MIT, already themed to the product | Not a Gantt. A horizontal bar on a date axis has to be assembled from primitives |
| Plain SVG or CSS grid | No dependency at all, total control of theming | Everything is hand-rolled — axis ticks, zoom, tooltips, responsive width |
| MUI X Gantt | Purpose-built, matches the design system exactly | Premium licence, not owned. The thing the evaluation exists to price against |
| A third-party Gantt (`frappe-gantt`, `vis-timeline`, `gantt-task-react` and similar) | Purpose-built and free | A second charting idiom to theme in light and dark, a new dependency to keep current, and licence terms to read before anything else |

The shortlist is not a recommendation and is not exhaustive. It exists so the two hours produce a
rendered prototype rather than a browser history.

Two outcomes, both cheap. Either the build is a few hundred lines and slice 04 proceeds as written, or
it is not and the product owner gets a costed licence question before six hours go into the wrong
answer.

**Record the result here before writing the first test:**

> _(to be filled)_

## Acceptance criteria

AC-4.1 through AC-4.9 in `feature-delta.md`.

## Dependencies

Slice 01 (start distribution). Slice 02 is not required but will have surfaced any believability problem
first, which is why it is sequenced earlier.

## Effort

~2h evaluation + ~6h build. The build half is a genuine ≤1-day slice only if the evaluation confirms
the geometry is simple. If it does not, this brief is rewritten around whatever the evaluation
recommends rather than stretched.

## Dogfood moment

Same day: open a real Delivery on the dev instance and switch the percentile from 70 to 95. The demo is
not that bars appear — it is watching every bar slide right together and seeing whether the Delivery
still clears its target date. That is the moment the feature either earns the word "plan" or does not.

Expect bars to abut rather than gap where one team works Features in sequence (D7). That is the model
being honest and is not to be "fixed".
