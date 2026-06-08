# Slice 03 - Invalid-on-removal: a definition survives a boundary-state change gracefully (D5)

**Type:** vertical | **Est:** ~0.5-1 day | **Stories:** US-03

## Learning hypothesis

A saved definition whose `startState`/`endState` is later removed or renamed from the team config can
be detected and surfaced as INVALID consistently across the config list AND both chart selectors -
never a silent break, a wrong computation, or a crash - by validating each definition's boundaries
against the current `AllStates` at read time.

## What ships

- Backend: a per-definition validity check (both boundary states still present in current `AllStates`);
  the per-definition read endpoint refuses to compute an invalid definition and reports it as invalid
  rather than erroring.
- Frontend: invalid definitions render DISABLED with a warning in the Cycle Times config list and in
  the scatterplot selector (and the cumulative scope switch once Slice 04 lands); selecting one is
  prevented; the chart stays on the last valid selection. The admin can edit the definition to pick a
  valid state, or delete it.

## IN scope

- Detection + surfacing of invalid definitions everywhere they appear; safe edit/delete recovery.

## OUT of scope

- Auto-correction of ordering or boundaries (explicitly the user's responsibility per D5).
- Cumulative scope switch (Slice 04), Portfolio (Slice 05).

## Production-data AC

- Given a saved "Concept to Cash" (Planned->Done) and Carlos removes "Planned" from the team's states,
  when Carlos reopens the Cycle Times config, then "Concept to Cash" shows a warning and is disabled.
- Given the same removal, when Priya opens the scatterplot selector, then "Concept to Cash" appears
  disabled with a warning and the chart does not crash or plot a wrong series.
- Given an invalid definition, when Carlos edits it to pick a still-present start state and saves, then
  it becomes valid and selectable again.

## Taste tests

- Sad-path coverage (DoR happy-path-bias guard): turns a crash-class into a first-class error path. PASS.
- Right-sized: one cross-cutting error behavior, 3 scenarios. PASS.
