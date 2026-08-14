# Slice 02 — What exactly, and what Lighthouse cannot act on (free)

**Feature**: epic-4365-dependencies · **ADO**: Epic #4365 · **Stories**: US-02, US-03 ·
**Estimate**: ~5h
**Reference class**: the existing work-items dialog opened from a Feature row, plus
`WarningsIndicator.tsx`, which is additive by construction — it already composes two warning kinds.

## Goal

The count from slice 01 becomes actionable: click it and see which Features, in what state, and read a
warning on every dependency Lighthouse will not be able to honour.

## IN scope

- `GET /api/latest/features/{id}/dependencies` — free, read, RBAC-filtered result set.
- A dialog off the Depends On cell listing each Feature waited on: name, state, its Portfolios, and a
  link to the tracker record.
- **The honour-ability verdict per edge**, computed and exposed here even though nothing consumes it
  for forecasting yet: `outside this Portfolio` (D6), `part of a loop` (D7), `cannot be forecast` (D8).
  This is where cycle detection actually lands — one slice before the forecast needs it, which is the
  cheapest place to get it wrong.
- Three new warnings in `WarningsIndicator`: cross-Portfolio, blocker-ranked-below (read from
  `IFeatureOrdering`, never written), and loop-member. Existing warnings unchanged (AC-3.5).
- A dependency the forecast *would* honour produces **no** warning (AC-3.4) — the presence of a
  dependency is not a problem, only an unhonourable one is.
- A Feature the user cannot read renders as a redacted row with the reason, never omitted (AC-2.5).

## OUT of scope

- Adding or removing anything. Lighthouse never authors an edge (D4), so no write path exists in this
  epic at all — this slice is read-only because the whole feature is.
- Any forecast change. The honour-ability verdict is computed and displayed but drives nothing yet.
- The premium hint — there is no premium behaviour to hint at until slice 03.

## Learning hypothesis

**Disproves** "the honour-ability verdict is a pure function of stored edges plus Portfolio membership
plus rank" **if** computing it per row on a Portfolio's Feature list needs a query per Feature, or if
loop detection over the dogfood edge set is not instant. Both the many-to-many Portfolio join and the
transitive loop walk are the kind of thing that reads as free at 94 Features and is not at 10,000.

If it fails, the verdict must be precomputed at ingestion and stored on the edge, which changes slice
01's storage shape retroactively and makes OQ-5 urgent rather than open.

**Confirms**, if it holds, that slices 03 and 04 can ask "is this edge honoured?" cheaply, inside the
forecast run, without a cache.

## Acceptance criteria

AC-2.1 … AC-2.5 and AC-3.1 … AC-3.6 verbatim from `feature-delta.md`. The three that carry the slice:

- A cross-Portfolio dependency warns and names the Feature it points at (AC-3.1).
- A dependency loop warns on every member and names the others (AC-3.3).
- A healthy dependency produces no warning at all (AC-3.4).

## Dependencies

Slice 01's stored edges and `dependsOnCount`. `IFeatureOrdering` for the ranked-below comparison —
read only. The loop and throughput-less-blocker shapes are created as **real Predecessor links in the
dogfood ADO project**; if they are not in place when this slice runs, its loop AC falls back to
fixtures and the real-data confirmation moves to slice 03's dogfood moment. Creating them first is
cheap and strictly better — say which happened in the verdict rather than leaving it implied.

## Dogfood moment

Same day: open `/features` on `:5169` and screenshot the dialog and each warning kind against real ADO
data. Cross-Portfolio should appear naturally; the loop appears only if the deliberate ADO links were
created first.

## Commit gate

**No commit without the maintainer's explicit approval.**

## Learning hypothesis verdict

_Not yet run._
