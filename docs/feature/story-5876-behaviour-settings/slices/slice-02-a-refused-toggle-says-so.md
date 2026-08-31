# Slice 02 — A refused toggle says so

## Goal

A Community administrator who toggles a premium switch is told it was refused, instead of receiving a
success that carries the old value.

## IN scope

- `OptionalFeaturesController.cs:41`: the premium branch returns an explicit refusal instead of
  `return feature;` — a 200 carrying the unchanged entity.
- Extending the existing `OptionalFeaturesControllerTest` coverage across the four cases: premium
  feature × licensed / unlicensed, non-premium feature × licensed / unlicensed.
- Re-checking Epic #5733's documents still read true. The back-propagation itself landed in DISCUSS
  on 2026-08-31 — US-07 marked TRANSFERRED, slice 03 asserting rather than writing the refusal,
  AC-07.3 kept as #5733's invariant.

## OUT of scope

- Making the premium gate a first-class concept across other controllers. One gate, one controller.
- Any UI change. The control is already disabled for unlicensed instances, so no user-facing flow
  starts producing errors — the refusal is reachable only by calling the API directly.
- Any change to `DeltaSync`, which is not premium and must not become gated by this fix.

## Learning hypothesis

**Disproves "nothing depends on the 200-unchanged shape" if** any existing caller — the frontend's
optimistic update, a test, the Jira Forge app — treats a non-2xx from this endpoint as a failure it
cannot recover from. The frontend already rolls back on throw and re-fetches, so the expected answer
is that nothing depends on it; a surprise here means the response contract had a second consumer
nobody listed.

## Acceptance criteria

Per US-02 (AC-02.1…02.5) in `feature-delta.md`. The two that carry it:

- **AC-02.1** — a refused premium toggle returns an explicit refusal and persists nothing.
- **AC-02.3** — `DeltaSync` is unaffected on licensed and unlicensed instances alike.

## Production-data acceptance

Vendor instance with the premium licence removed: call the endpoint directly against the
`FeatureOrdering` row, confirm the refusal and confirm `GET /OptionalFeatures` still reports the old
value. Then call it against `DeltaSync` and confirm it still succeeds.

## Dogfood moment

Same day: with the licence removed, confirm the Settings page still renders both rows, the ordering
row still disabled with its tooltip, and that nothing in the UI has started showing an error.

## Dependencies

Slice 01 — before it there is no premium optional feature, so the branch this fixes is unreachable and
the fix would be untestable against anything real.

## Effort estimate

~2h of crafter dispatch. One branch, four test cases, two document edits in another feature's folder.

## Reference class

Any single-branch controller contract change with an existing test class. Half a day at the outside.

## Pre-slice SPIKE

Not needed.
