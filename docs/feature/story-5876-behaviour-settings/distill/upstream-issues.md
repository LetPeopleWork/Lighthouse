# Upstream issues found during DISTILL — story-5876-behaviour-settings

Written 2026-08-31. Findings that DESIGN did not have in front of it, discovered by running the
acceptance scenarios against the code on `main`. Each is stated with the evidence that produced it.

---

## UI-1 — A second row in the table cannot be switched. BLOCKING for slice 02

**Severity: blocking.** Slice 02 cannot ship without an answer. Slice 01 is unaffected once its
fixture carries an explicit identity, which it now does.

### What the scenarios found

`Lighthouse.Backend.Tests/API/Integration/BehaviourSettings/Slice01PremiumRefusalTest` seeds a second
optional feature so the premium branch can be exercised. Every toggle against it answered **500**:

```
System.InvalidOperationException : Sequence contains more than one element
  at Lighthouse.Backend.Services.Implementation.Repositories.RepositoryBase`1.GetById(Int32 id)
  at Lighthouse.Backend.API.ApiHelpers.GetEntityByIdAndExecuteActionInternal(...)
  at Lighthouse.Backend.API.OptionalFeaturesController.UpdateOptionalFeature(Int32 id, ...)
```

### Why

`LighthouseAppContext.cs:102` reads

```csharp
modelBuilder.Entity<OptionalFeature>().HasKey(a => a.Key);
```

The primary key is the string `Key`. `Id` is therefore an ordinary mapped column with no value
generation behind it, and `OptionalFeatureSeeder.cs` writes every row with `Id = 0` — the comment
there ("`Id` is required on the entity; 0 lets EF assign the key") describes a behaviour the model
configuration does not give it.

Today exactly one optional feature is seeded (`DeltaSync`, S7), so `GetById(0)` finds one row and the
toggle works. Nobody has hit this because nobody has ever had two rows.

**Slice 02 adds the second row.** From the moment `FeatureOrdering` is seeded, both shipped rows carry
`Id = 0`, `GetById(0)` matches two, and `POST /api/{v1,latest}/OptionalFeatures/{id}` throws for
*both* settings — including `DeltaSync`, which this story promised not to touch (D8, AC-01.9).

### What it means for the design

ADR-187 §3 routes the toggle through an applier resolved **by key**, and §1 records the store as keyed
by `Key`. The route still addresses a row by a number that names nothing. DESIGN has an open choice:

- Address the row by its key on the write path, as the read path already does
  (`GET /OptionalFeatures/{featureKey}` exists and works), and stop using `GetById` for this action —
  which this action was already going to do (DDD-7), so the cost is one route shape rather than a
  second change.
- Or make `Id` a generated identity. That is a schema change on four providers for a column nothing
  reads, to keep a route that names a row by a number the store does not key it by.

The first is recommended and is a DESIGN decision, not DISTILL's. Recorded here rather than settled.

### The test that holds it

`Slice02OneListOfSwitchesTest.Each_setting_in_the_list_is_switched_on_its_own` — RED, ignored,
asserting that turning the ordering setting on leaves the setting beside it exactly as it was.

---

## UI-2 — OQ-1 was left to DISTILL and is settled here

Not an issue, recorded for completeness: ADR-187's open question OQ-1 (token syntax for terminology in
seeded descriptions, and what an unknown token renders as) was deferred to DISTILL and is answered in
`feature-delta.md` under `## Wave: DISTILL / [REF] Closed Open Questions`.

---

## UI-3 — The seeded *name* carries the same configurable term as the description

DDD-8 makes the **description** cell terminology-aware. The row's name, as DISCUSS wrote it, is
*"Let Lighthouse own the order of your Features"* — which names the same configurable term and would
read "Features" on an instance that renamed it, the exact failure §A.3 of ADR-134 rejected the store
over.

The resolver is a one-line pure function over a string; running it on the name cell as well as the
description cell costs nothing and closes the hole. The acceptance scenarios assert both.

Recorded rather than assumed: it widens DDD-8 by one cell, and DESIGN should confirm it rather than
find it in a diff.

---

## UI-4 — A backend test-suite flake, widened but not introduced

`TeamInProject_WithExistingForecasts_DeleteTeam_SucceedsAsync` failed once during DISTILL with
`SQLite Error 1: 'no such table: WorkTrackingSystemConnections'`. It passes when run alone, the
baseline suite without these new fixtures is green, and the suite with them is green on a re-run.

This is the contention class `CLAUDE.md` already documents for `ReleaseServiceTest`, not a regression.
Two new integration fixtures widen the window it can occur in. Recorded so a DELIVER run that sees it
once does not spend time treating it as a new defect — and so that a *repeated* occurrence is
recognised as something to fix rather than something to re-run.

---

## UI-5 — UI-1 has a frontend twin that survives any backend fix. BLOCKING for slice 02

`SystemSettingsTab.tsx` keys its rows `key={feature.id}` and matches its optimistic update on
`feature.id === toggledFeature.id`; `OptionalFeatureService.updateFeature` posts to
`/optionalfeatures/${feature.id}`. With both seeded rows carrying zero, one click flips both switches
and React sees duplicate keys.

Reproduced, not inferred — `switches one setting without touching the other` in
`SystemSettingsTab.behaviourSettings.test.tsx` fails today with *"Received element is checked"* on the
row nobody touched.

Routing the backend write by key does not fix this. The frontend must key rows and match optimistic
state on `feature.key`, and post by key. Both changes belong in slice 02's component decomposition,
which currently lists neither `OptionalFeatureService.ts` nor this behaviour of `SystemSettingsTab.tsx`.

---

## UI-6 — `IFeatureOrderingPolicyProvider.SetPolicy` becomes a third door into the store

The design says the provider "keeps its interface; only its backing store changes". The interface
carries `SetPolicy` as well as `GetPolicy`. Swapping the store therefore leaves `SetPolicy` writing
`OptionalFeature.Enabled` directly — with no rank seed and no `FeatureOrderingPolicyChanged` — while
its one caller is being redirected to the applier.

That is the parallel write path the design's own invariant forbids, and the alternative it rejects by
name. Either delete `SetPolicy` (its sole caller is moving anyway) or have it delegate to the applier,
and say which in the ADR.

---

## UI-7 — The applier must not seed on the way out

`AppSettingService.SetFeatureOrderingPolicy` seeds only `if (policy == FeatureOrderingPolicy.ManualOrder)`.
Neither the applier decision, nor its invariant, nor the component diagram carries that condition — the
diagram shows an unconditional "seed missing ranks FIRST".

Seeding on a disable writes places nobody asked for and destroys the meaning of a null place, which is
how the product records that a Feature arrived while the switch was off. That is what makes taking the
order over again *append* rather than renumber.

Pinned by `Giving_the_order_back_writes_no_places`. It needs a Feature that arrives *after* the first
enable: once every row holds a place, a second seed is a no-op and proves nothing.

---

## UI-8 — An instance whose licence lapsed while it owned the order

Both the DISCUSS and DESIGN reviews raised this independently, and both read it as a new hole. It is
not: `FeatureOrderingSettings.tsx` already renders `checked={policy === "ManualOrder"}` with
`disabled={!isPremium}`, and no part of the ordering read path consults the licence. Such an instance
**already** sees an on-but-disabled switch it cannot turn off, and **already** keeps manual ordering.
The move preserves that exactly.

Two things follow, and they pull in opposite directions:

- **The preservation must be tested**, because the tempting tidy-up during the re-backing is to make
  the provider licence-aware. That would hand every lapsed customer's list back to their tracker and
  reorder every Feature they had placed, silently, on their renewal date.
  `An_instance_whose_licence_lapsed_keeps_the_order_it_already_owns` is green today and pins it.
- **Whether being stranded is acceptable is still an open product question.** Slice 01 is rewriting
  exactly the branch that refuses the write, so it is the cheap moment to decide whether the premium
  check gates only transitions to on. Not DISTILL's call; recorded so it is decided rather than
  inherited.

---

## UI-9 — Comments in the files this story edits break the project rule

Three, all in components the design marks EXTEND, so the standing "fix them as you go" applies:

- `AppSettingService.cs:101` opens *"D6 - seeding runs first on purpose"* — `D6` names a section of a
  feature document that gets archived.
- `IFeatureOrderingPolicyProvider`'s doc-comment cites `ADR-134`, which this story partially supersedes.
- `OptionalFeatureSeeder.cs`'s *"0 lets EF assign the key"* is **false**. The entity is keyed by `Key`,
  so nothing generates `Id`. That comment is why UI-1 went unnoticed for a release: it asserts the
  behaviour a reader would otherwise have checked.

DISTILL wrote no production code, so these are DELIVER's to fix rather than something to change here.
The third should be deleted outright rather than corrected — once the write path is addressed by key
there is no `Id` story left to tell.
