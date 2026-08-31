# Slice 01 — A refused toggle says so

## Goal

A Community administrator who toggles a premium switch is told it was refused, instead of receiving a
success that carries the old value.

## IN scope

- `OptionalFeaturesController.cs:41`: the premium branch returns **403**, not `return feature;` — a
  200 carrying the unchanged entity. 403 specifically, because Epic #5375 AC-2.5 already promises it
  on the endpoint this setting is about to move onto, and `[LicenseGuard(RequirePremium = true)]`
  delivers it today.
- Extending the existing `OptionalFeaturesControllerTest` coverage across the four cases: premium
  feature × licensed / unlicensed, non-premium feature × licensed / unlicensed. The premium fixture is
  a test-only `OptionalFeature` with `IsPremium = true`; no live premium feature is needed, and none
  exists until slice 02.
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

This slice runs **first** for a reason that is not learning leverage: slice 02 moves a setting whose
403 is a shipped promise (Epic #5375 AC-2.5) onto this endpoint. Fixing the gate afterwards would mean
shipping a slice that regresses it.

## Acceptance criteria

Per US-02 (AC-02.1…02.5) in `feature-delta.md`. The two that carry it:

- **AC-02.1** — a refused premium toggle returns **403** and persists nothing.
- **AC-02.3** — `DeltaSync` is unaffected on licensed and unlicensed instances alike.

## Production-data acceptance

Vendor instance with the premium licence removed: call the endpoint directly against a premium
optional feature, confirm 403 and confirm `GET /OptionalFeatures` still reports the old value. Then
call it against `DeltaSync` and confirm it still succeeds. On this slice the premium row is a test
fixture; on the vendor instance the check repeats against the real `FeatureOrdering` row once slice 02
has landed.

## Dogfood moment

Same day: with the licence removed, confirm the Settings page still renders `DeltaSync` and that
nothing in the UI has started showing an error — no user-facing flow reaches the refused branch,
because the control is already disabled for unlicensed instances.

## Dependencies

None. It runs first so that slice 02 inherits a gate that already refuses correctly — moving a setting
that promises 403 onto an endpoint that answers 200 would regress Epic #5375 AC-2.5.

Also satisfies the precondition Epic #5733 slice 03 depends on, earlier than that Epic could have.

## Effort estimate

~2h of crafter dispatch. One branch, four test cases, two document edits in another feature's folder.

## Reference class

Any single-branch controller contract change with an existing test class. Half a day at the outside.

## Pre-slice SPIKE

Not needed.
