# Slice 08 — Linear requests history only for issues that moved

**Feature**: epic-5687-faster-updates · **ADO**: Epic #5687 → Story #5731 · **Story**: US-07 · **Estimate**: ~4h
**Reference class**: slice 02's contract; `LinearWorkTrackingConnector` already builds its queries from
templates with an optional history fragment (`HistoryConnectionFragment`,
`ProjectHistoryConnectionFragment`), so "with history" and "without history" is a parameter that already
exists.

**Subject to the D7 checkpoint. Lowest-value slice in the epic — Linear's API is already the fastest of
the four connectors.**

## Goal

A Linear refresh requests the history connection only for issues and projects whose `updatedAt` moved.

## IN scope

- Identity sweep = the existing paginated issue/project query with `includeHistory: false` and the field
  selection narrowed to id + `updatedAt`.
- `LastChangedRemote` populated from `updatedAt`.
- The history-bearing query issued only for the changed set.
- Removal semantics unchanged (D2).
- The existing `DowngradeHistorySupport()` fallback still works — a workspace whose plan does not expose
  history must keep degrading exactly as it does now, and must not be mistaken for "nothing changed".

## OUT of scope

- Any change to the GraphQL pagination helper (`GetWithPagination`).
- Initiative / team resolution paths.
- Any UI.

## Learning hypothesis

**Disproves "the history fragment is separable from the issue query."** The connector composes one
GraphQL document per page; if issues and their history cannot be requested in two passes without the
second pass costing as much as the combined one — GraphQL charges by complexity, not by round trip —
then delta buys nothing on Linear and the slice should close with that recorded.

Secondary: **disproves "a degraded workspace is distinguishable from an unchanged one."** If
`DowngradeHistorySupport()` fires during the sweep rather than during the history pass, a plan limitation
could look like "no items changed", which is silent staleness rather than the visible degradation the
connector currently produces.

## Acceptance criteria

AC-7.3, AC-7.4 from `feature-delta.md` (US-07), plus: a workspace that triggers
`DowngradeHistorySupport()` degrades identically under `mode=delta` and `mode=full`.

## Dependencies

- Slices 02 and 05.
- The D7 checkpoint verdict.
- A Linear workspace with enough issues that the two-pass cost is measurable.

## Effort

~4h. Sweep query ~1h, restricting the history pass ~1h, downgrade-path tests ~2h.

## Production data / dogfood moment

A real Linear workspace, one full cycle and one delta cycle, summary lines compared. If the delta cycle
is not meaningfully cheaper, that is the hypothesis disproved and the slice closes with the number rather
than the code.

## Pre-slice SPIKE

Not needed.

## Verdict

_(recorded at slice close)_
