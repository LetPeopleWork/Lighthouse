# Slice 03 — One switch stops the asking and the sending, and a refused toggle says so

## Goal

A system administrator can enforce an organisational policy on usage data with one control, and can
trust that the control on screen is the control in force.

## IN scope

- `UsageData` OptionalFeature: `IsPremium = true`, seeded `Enabled = true`, `RbacGuard(SystemAdmin)`
  (US-06).
- OFF suppresses the dialog everywhere **and** stops emission for browsers already holding consent,
  enforced at the emit path.
- ON resumes emission for those browsers silently — consent was suspended, never revoked (D6).
- The footer indicator distinguishes "the administrator disabled this" from "you declined"
  (AC-06.7).
- Settings copy stating the suspend/resume behaviour in plain words (AC-06.6).
- **Fix `OptionalFeaturesController:41`** (US-07): a premium toggle without a premium licence returns
  an explicit refusal, not a 200 carrying the unchanged entity.

## OUT of scope

- The wider OptionalFeatures rework the Epic muses about — making the premium gate a first-class
  concept, migrating manual ordering out of `FeatureOrderingSettings`. Board item, not this Epic.
- Any change to `DeltaSync`'s behaviour. It is not premium and must not become gated by the S8 fix
  (AC-07.3).
- A grace period or timed wipe on suspension. D6 is suspend-and-resume, full stop.

## Learning hypothesis

**Disproves "OptionalFeatures is the right home for a privacy control" if** an administrator looking
for it cannot find it — a feature-toggle list is where you look for feature toggles, not for a
consent governor. Watch for the first support question that asks "where do I turn off telemetry".

**Disproves "premium-gating the ask is commercially survivable" if** the first community reaction is
about the gate rather than about the feature. This is the slice that makes D5 visible, and D5 is the
Epic's most contestable decision.

## Acceptance criteria

Per US-06 (AC-06.1…6.7) and US-07 (AC-07.1…7.4) in `feature-delta.md`. The three that carry it:

- **AC-06.3** — OFF stops emission from a browser that already consented, asserted at the emit path.
  A stale tab must not be able to keep sending.
- **AC-06.5** — ON resumes without re-asking.
- **AC-07.1** — a refused toggle is refused visibly. This is the precondition for the whole slice: a
  privacy control whose write can be silently dropped is worse than no control.

## Production-data acceptance

Run against the vendor's own instance with a real premium licence, then again with the licence
removed, confirming the Community path shows the switch, shows it is on, and refuses the change
visibly (AC-07.4).

## Dogfood moment

Same day: switch it off on the vendor instance, confirm the next heartbeat does not arrive at the
collector, switch it back on, confirm the following one does — without anyone re-consenting.

## Dependencies

- Slice 02 shipped (there is a prompt worth vetoing and consenting browsers worth suspending).
- Premium licence fixture, which is gitignored and absent in a fresh worktree — import it from the
  main checkout before running these tests or any `@screenshot` pass.

## Effort

≤ 1 day. The OptionalFeature scaffolding, the RBAC guard and the premium field all exist (S8–S10).
The new work is the emit-path consultation and the refusal semantics.

## Reference class

`DeltaSync` (Epic 5687) — the last new OptionalFeature. Seeding, toggling and RBAC were a
non-event; everything interesting was in how the flag reached the code that reads it. Expect the same
distribution here, with the added constraint that this flag must reach a background emitter without a
per-emit database read.

## Watch

The S8 fix changes the contract of an endpoint that already has callers. Grep for them and extend
the test factory before touching it — a controller that used to always return 200 is a shared
contract, whatever its behaviour was.
