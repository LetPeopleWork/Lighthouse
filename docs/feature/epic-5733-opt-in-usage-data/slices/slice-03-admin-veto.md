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
- Verifying the premium gate refuses out loud — the fix itself is story #5876's (US-07 transferred
  there on 2026-08-31). This slice asserts the behaviour it depends on; it does not write it.

## OUT of scope

- The wider OptionalFeatures rework the Epic muses about — making the premium gate a first-class
  concept, migrating manual ordering out of `FeatureOrderingSettings`. That is story **#5876**, which
  as of 2026-08-31 also carries the S8 gate fix this slice used to own.
- **Writing** the S8 fix. Story #5876 does that. If it has not shipped when this slice starts, the fix
  comes back into scope here — it may not be skipped, because a privacy control cannot ship on a gate
  that drops writes.
- Any change to `DeltaSync`'s behaviour. It is not premium and must not become gated by the S8 fix
  (AC-07.3) — this Epic's invariant to check wherever the fix was written.
- A grace period or timed wipe on suspension. D6 is suspend-and-resume, full stop.

## Learning hypothesis

**Disproves "OptionalFeatures is the right home for a privacy control" if** an administrator looking
for it cannot find it — a feature-toggle list is where you look for feature toggles, not for a
consent governor. Watch for the first support question that asks "where do I turn off telemetry".

**Disproves "premium-gating the ask is commercially survivable" if** the first community reaction is
about the gate rather than about the feature. This is the slice that makes D5 visible, and D5 is the
Epic's most contestable decision.

## Acceptance criteria

Per US-06 (AC-06.1…6.7) in `feature-delta.md`, plus US-07 (AC-07.1…7.4) as an inherited precondition
now satisfied by story #5876. The three that carry it:

- **AC-06.3** — OFF stops emission from a browser that already consented, asserted at the emit path.
  A stale tab must not be able to keep sending.
- **AC-06.5** — ON resumes without re-asking.
- **AC-07.1** — a refused toggle is refused visibly. Written by story #5876; asserted here, because a
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

≤ 1 day, and smaller since the S8 fix moved to story #5876. The OptionalFeature scaffolding, the RBAC
guard and the premium field all exist (S8–S10). The new work is the emit-path consultation.

## Reference class

`DeltaSync` (Epic 5687) — the last new OptionalFeature. Seeding, toggling and RBAC were a
non-event; everything interesting was in how the flag reached the code that reads it. Expect the same
distribution here, with the added constraint that this flag must reach a background emitter without a
per-emit database read.

## Watch

The S8 fix changes the contract of an endpoint that already has callers — a controller that used to
always return 200 is a shared contract, whatever its behaviour was. Story #5876 carries that risk now.
Confirm it landed, and confirm `DeltaSync` stayed ungated, before assuming this slice inherited a
working gate.
