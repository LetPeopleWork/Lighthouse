# Slice 04 — Move above/below a named Feature

**Feature**: epic-5375-manual-sorting · **ADO**: Epic #5375 · **Story**: US-04 · **Estimate**: ~4h
**Reference class**: `WorkItemsDialog` (`components/Common/WorkItemsDialog/`) — same modal-over-the-grid
shape, opened from a row.

**This slice is designed to be cancellable.** It ships only if two weeks of living with slice 03 show
that relative moves are not enough (K7). If people only ever reach for Move to Top, it is deleted, not
deferred.

## Goal

A product owner reprioritises across a long backlog in one step — "this belongs ahead of the thing we
start in Q3" — instead of clicking Move Up two hundred times.

## IN scope

- **Move above…** / **Move below…** entries in the row-action menu shipped by slice 03.
- Searchable picker dialog: Features from Portfolios the user can read, searchable by name and reference
  id, excluding Done Features (D15) and the Feature being moved.
- Both entries call slice 03's existing endpoint with `beforeFeatureId` / `afterFeatureId` — **no new
  endpoint, no new rank logic**. That is the whole reason this slice is 4h and not 8.
- Available regardless of the grid's current column sort (D14), unlike the relative moves, because the
  target is named explicitly rather than implied by screen position.
- Available on both surfaces (D10) from the one shared menu.
- Vitest for AC-4.1 … AC-4.6, including the permission asymmetry in AC-4.5.

## OUT of scope

- Drag-and-drop (D18).
- Multi-select or bulk "move all of these above X".
- Any change to the rank service or the endpoint contract. If this slice needs either, that is a signal
  slice 03 got the primitive wrong, and it is worth stopping to say so.

## Learning hypothesis

**Disproves "relative moves are enough for the beginning"** — the user's own framing when choosing
buttons over drag (D18). The measurement is K7: over two weeks of dogfooding slice 03, what share of
moves are a single Move to Top versus a run of Move Ups climbing toward a specific target?

The reading rule is pinned in `feature-delta.md` → "K7 reading rule", and is a **run** signal rather than
a percentage band — two weeks of single-operator dogfooding yields tens of moves, and a band over that
sample is noise dressed as a threshold:

- **Any** run of ≥3 consecutive Move-Ups on one Feature → **build it**. That run is someone hand-climbing
  toward a target the picker collapses into one action. One clear instance is existence proof.
- Zero such runs **and** Move-to-Top ≥ ~75% of moves → **drop it**. The real decision is binary and
  long-range placement was imagined rather than needed.
- Zero such runs, Move-to-Top below that → **re-time, do not decide**. Neither signal fired and the
  sample is too thin to build or delete on.

**Confirms**, if it holds, that a Feature list of real size needs an absolute gesture and not only
relative ones — which is also the thing that would eventually justify revisiting drag.

## Verify the premise first

There is no code probe here — the premise check *is* K7, and it runs during the two weeks after slice 03
ships. Do not start this slice before that data exists; starting early is how a cancellable slice stops
being cancellable.

## Acceptance criteria

AC-4.1 … AC-4.6 verbatim from `feature-delta.md`. The two that carry the slice:

- The same intended outcome, produced once through a relative move and once through a targeted move,
  yields the identical global order (AC-4.2) — this is what keeps D4 a single rule across two gestures.
- Choosing a target the user may not move *against* is **allowed**: the permission that matters is write
  on the **moved** Feature's Portfolios, not on the target's (AC-4.5). Pinned by a test because it is
  exactly the case a reviewer will otherwise assume was overlooked.

## Dependencies

Slice 03 (menu, endpoint, rank service, RBAC). Slice 01 (the searchable Feature set and its RBAC filter).
Gated on K7 data.

## Dogfood moment

Same day: use it once for a real decision that spans the length of the backlog. If the picker's search
does not find the target in a couple of keystrokes, the dialog needs work before the docs do.
