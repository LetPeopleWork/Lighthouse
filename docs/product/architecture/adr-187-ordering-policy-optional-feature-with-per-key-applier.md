# ADR-187: The Ordering Policy moves to an OptionalFeature, and a per-key applier carries its consequence

- **Status**: **Proposed** (DESIGN, 2026-08-31)
- **Date**: 2026-08-31
- **Feature**: `story-5876-behaviour-settings` (ADO User Story #5876 "Move Feature Ordering to Optional Features")
- **Deciders**: Benjamin Huser-Berta (maintainer), Morgan (Solution Architect), interaction mode = PROPOSE
- **Amended**: 2026-09-01, guided amendment pass on evidence DISTILL surfaced — §6 records that the premium refusal is symmetric and why, §7 widens to the row's name, and a new §8 retires the numeric write route. Nothing decided on 2026-08-31 is reversed.
- **Supersedes**: [ADR-134](./adr-134-ordering-policy-appsetting-enum-single-selection-point.md) §1 (storage) and §A (the rejection of `OptionalFeature`). ADR-134 §2 (the single ordering seam), §3 (INV-A3, fill-nulls-appending in source order) and §4 (the sync path never writes `ManualRank`) are **retained in full**, as are its enforcement rules.

---

## Context

ADR-134 stored the Ordering Policy as an `AppSetting` row carrying an enum name, and considered
`OptionalFeature` as the alternative. It rejected it on three counts:

1. **"It is a `bool`."** ADR-132 named the policy an enum because "manual sorting on/off" is the UI's
   word for it, not the domain's.
2. **"Its premium path is a silent no-op, not a 403."** `OptionalFeaturesController.cs:41-44` returns
   the unchanged feature when the licence is missing; AC-2.5 requires 403.
3. **"The free UI cannot satisfy AC-5.5, and this is decisive."** The table renders a server-seeded
   `Description`; AC-5.5 requires the help text in the instance's own terminology, resolved
   client-side through `getTerm`. A seeded string would say "Feature" on an instance that says
   "Deliverables".

It also noted a fourth, softer point: at the time, `OptionalFeatureSeeder` returned an empty list and
`SystemSettingsTab.tsx:96` hid the section entirely, so adding a row would have resurrected a dormant
UI section as a side effect of a storage decision.

Two things have changed since, and one was never quite right.

**The dormant-section point has expired.** `DeltaSync` ("Faster Updates", Epic #5687) is a live
optional feature, so the section renders on every instance today. Adding a second row resurrects
nothing.

**The silent no-op has a mechanical cause, and it is fixable.** It is not a careless `return`.
`ApiHelpers.GetEntityByIdAnExecuteAction` (`APIHelpers.cs:30`) always wraps its lambda's return value
in `Ok(...)`, so an `ActionResult` cannot escape from inside it. The premium branch had no way to say
403 without leaving the helper.

**The `bool` point stands, and is narrowed rather than dismissed.** The storage becomes a boolean; the
domain type does not. That is a real reduction in room, and it is accepted below.

**The terminology point stands, and reveals a missing capability rather than a bad store.** ADR-134
was right that rendering one row specially would spend the entire reuse benefit. What it did not
consider is making the name and description cells terminology-aware for *every* row — which is a
smaller change than the special case it rejected, and one that pays out on every future setting.

Meanwhile the story that prompted this ADR is not about storage at all. It is about an administrator
finding every instance-wide switch in one place, and it is the point at which "which switches are in
the table" stops being a fact about release history.

## Decision

**The Ordering Policy is stored as an `OptionalFeature` row keyed `FeatureOrdering` with
`IsPremium = true`. A per-key applier carries the consequences of toggling it. The ordering seam,
the rank-seeding rule and the sync-path guarantee from ADR-134 are unchanged.**

### 1. Storage — `OptionalFeature`, migrated once by the seeder

| Concern | Resolution |
|---|---|
| Key | `FeatureOrdering` in `OptionalFeatureKeys` |
| Value | `Enabled` — `true` maps to `ManualOrder`, anything else to `SourceOrder` |
| Default | Seeded `Enabled = false`, i.e. `SourceOrder`, matching ADR-134's absent-row default |
| Migration | On the **first-add path only**, the seeder reads `AppSetting` `FeatureOrdering:Policy` and seeds `Enabled = true` where it reads `ManualOrder` |
| Old row | **Retained and unread.** Additive only; nothing deletes it |
| Read | `IFeatureOrderingPolicyProvider.GetPolicy()` — unchanged signature, unchanged callers |
| Write | `POST api/{v1,latest}/OptionalFeatures/{featureKey}`, `RbacGuard(SystemAdmin)` + per-row premium check. Addressed by key, not by `Id` — §8 |

`OptionalFeatureSeeder` never overwrites `Enabled` on an existing key, and a new key seeds with its
declared value. So the migration has **exactly one opportunity**, at first add, and a shipped release
with a wrong migration cannot be corrected by a later seed. That is the sharpest constraint in this
ADR.

### 2. The enum survives; only the storage is boolean

`FeatureOrderingPolicy` remains the type every consumer sees. `FeatureOrderingPolicyProvider` maps
between the boolean row and the enum, and is the only place that knows the mapping exists. ADR-132's
reasoning — that "on/off" is the UI's word and not the domain's — is preserved at the level it was
about.

Anyone reaching for `bool` in `FeatureOrdering`, `FeaturesController` or `AppSettingService` is
undoing ADR-132 and should be stopped in review.

`IFeatureOrderingPolicyProvider.SetPolicy` is **deleted, not made to delegate.** Its only caller moves
to the applier, and a `SetPolicy` that forwarded to the applier would be a second name for a write the
applier owns whole — which re-opens exactly the two-entry-point shape §3 exists to close. Deleting it
also leaves the port read-only, which is the honest shape: a port that only reads must not carry a
method that writes, or the next caller finds it and uses it.

The provider is **not** licence-aware, and must not become one. The row it reads is marked premium, so
checking the licence there is the tempting tidy-up; it would hand every lapsed customer's list back to
their tracker and reorder every Feature they had placed, silently, on their renewal date. The premium
gate belongs on the write, where a refusal is something a caller can see.

### 3. A per-key applier, because a generic toggle must carry a specific consequence

> **INV-A4.** Every optional-feature toggle is applied by exactly one `IOptionalFeatureApplier`,
> resolved by key, which owns the whole write: any pre-write work, the `Enabled` change, the save, and
> any post-write publication — in that order, in one method.

`DefaultOptionalFeatureApplier` sets and saves, which is what every row does today.
`FeatureOrderingApplier` seeds missing ranks, sets, saves, then publishes `FeatureOrderingPolicyChanged`.

The controller resolves an applier and calls it. It has no `switch` on key, because a key `switch` in
a controller is the five-`if` failure mode ADR-134 §2 exists to prevent, relocated one layer up.

**Why the applier owns the whole write rather than exposing before/after hooks:** a two-phase hook can
be called in the wrong order or half-called. One method cannot.

### 4. The rank seed reads source order explicitly

ADR-134 §3 already specifies the seed as "in current source order". `FeatureRankSeeder` obtains that
order through `IFeatureOrdering.Order(...)`, which consults the policy — so it produces source order
only because the seed happens to run before the flip. That temporal coupling is invisible at both
sites and is the single most dangerous thing about this move: seed after the flip and `Order` sorts by
an all-null `ManualRank`, renumbering every Feature in `Id` order. The user's list visibly scrambles at
the exact moment the product promises nothing will move.

`IFeatureOrdering` therefore gains a policy-independent source-order method, and the seeder uses it.
The seam stays single — `FeatureComparer` is still reachable from exactly one production type, so
`FeatureOrderingSingleSourceArchUnitTest` is unchanged — and the seed becomes correct regardless of
when it runs.

The applier still runs it first. This is belt and braces on purpose: §3 makes the sequence
unbreakable from outside, §4 makes it not matter.

### 5. The consequence is synchronous; the notification is an event

Seeding stays inside the request. `IDomainEventDispatcher` swallows handler exceptions by design — a
correct choice for metrics fan-out and the wrong one here, because a dropped seed leaves the instance
in `ManualOrder` with every rank null and no signal that anything failed.

`FeatureOrderingPolicyChanged` remains a published event. It genuinely belongs after the write, its
handler re-queues forecasts, and losing one costs a stale forecast rather than a scrambled order.

### 6. 403, on both doors

The premium check moves **out** of `GetEntityByIdAnExecuteAction`, which always wraps in `Ok(...)`,
and runs before any write. The helper is **not** widened: 83 call sites across 8 controllers, no other
beneficiary. This one action stops using it.

`LicenseGuardAttribute` is not reused on the new path — premium-ness here is per-row data and the
attribute cannot see which row is being written — but its response shape is matched, so the two doors
answer alike. The deprecated `AppSettings/FeatureOrdering` alias **keeps** its
`[LicenseGuard(RequirePremium = true)]` and delegates to the same applier.

**The refusal is symmetric, and that is the house pattern rather than an oversight.** The check is
`feature.IsPremium && !CanUsePremiumFeatures()`, so it refuses a disable exactly as it refuses an
enable: an instance whose licence lapsed keeps the setting it owns and cannot turn it off. Both wave
reviewers read that as a hole this story opens. It is not.
`BlackoutPeriodsController` carries `[LicenseGuard(RequirePremium = true)]` on `Delete` as well as on
`Create` and `Update` (`BlackoutPeriodsController.cs:61`), and `RecurringBlackoutRulesController` does
the same. Nowhere in Lighthouse can an instance undo premium configuration once its licence lapses — a
lapsed instance's blackout periods keep shrinking its forecasts and it cannot remove them. Gating only
the enable here would make the ordering policy the one downgradable premium setting in the product, and
the branch is generic rather than ordering-specific, so whatever it does is inherited by every premium
row added after it.

Allowing the disable would have been *data*-safe: disabling writes no places and keeps the ones already
chosen, so a renewal restores the exact list. The rejection is about product coherence, not risk. The
coherent version of "let people relinquish premium configuration" is a product-wide change across
ordering, blackout periods and recurring rules, and belongs on the board as its own item rather than in
this story.

### 7. Names and descriptions carry terminology tokens

Seeded names and descriptions may contain placeholder tokens, resolved through `getTerm` when the table
cell renders. This is what makes a server-seeded string satisfy AC-5.5, and it applies to every row and
to both cells rather than to this one.

**The name cell needs it as much as the description does.** `FeatureOrderingSettings.tsx:60` renders its
label as ``label={`Let Lighthouse own the order of your ${featuresTerm}`}`` — the control's name is
*already* terminology-aware in shipped code, while the table's name cell is a plain `{feature.name}`
over a server-seeded string (`SystemSettingsTab.tsx:121`). Resolving only the description would
therefore not be declining to widen a capability; it would regress one that ships today, which is
precisely what ADR-134 §A.3 rejected this store over. The resolver has to exist for the description in
any case, so the second call site costs a line. `DeltaSync`'s name carries no token and is unaffected.

Rewording the seeded name until it carries no configurable term was the alternative. It drops the label
users read today and leaves the description needing tokens regardless, so it removes nothing and loses
something.

Resolution is frontend-only. The seeder stores the token; the server never resolves it, because the
term map is a client-side concern and the seeding path has no principal.

### 8. The write is addressed by key, and the numeric route is retired

`POST api/{v1,latest}/OptionalFeatures/{featureKey}` resolves the row through
`GetByPredicate(f => f.Key == featureKey)`, exactly as the shipped `GET /OptionalFeatures/{featureKey}`
already does. `POST /OptionalFeatures/{id}` is removed outright. There is no compatibility alias.

The store has always been keyed by the string. `LighthouseAppContext.cs:102` reads
`modelBuilder.Entity<OptionalFeature>().HasKey(a => a.Key)`, so `Id` is an ordinary mapped column with
no value generation behind it, and `OptionalFeatureSeeder` writes every row `Id = 0`.
`RepositoryBase.GetById` is a `SingleOrDefault(t => t.Id == id)`, so it works only while exactly one row
exists — and §1 adds the second. From that moment the numeric route throws for **both** settings,
including `DeltaSync`, which this story promised not to touch.

The exception is not the decisive argument, though. Two entities in `LighthouseAppContext` are keyed by
something other than `Id`: `AppSetting` (line 101) and `OptionalFeature` (line 102). `AppSettingsController`
addresses every one of its settings by name in the route — `GET`/`PUT /AppSettings/FeatureOrdering`, with
no integer anywhere — and that is where this very setting lives today. Routing it onto
`OptionalFeatures/{id}` would take a setting correctly addressed by its name and re-address it by a
number that reads `0` for every row in the table.

`GetEntityByIdAnExecuteAction` has 83 call sites across 8 controllers, and every other user of it is a
genuinely `Id`-keyed entity. `OptionalFeature` was the one entity keyed by something else that still
went through the `Id` helper. The helper is not wrong; this one use of it was. §6 already says this
action stops using it, so the route table was the last place the retired numeric identity survived —
§8 finishes that sentence rather than reversing anything.

`Id` **stays** on the entity and on the wire. `OptionalFeature : IEntity` declares `required int Id` and
`RepositoryBase<T> where T : IEntity` is generic over it, so the column is part of a repository contract
rather than of this route. It simply stops naming a row. Nothing about the schema moves, and no
migration is generated.

The browser needs the same change, and does not inherit it. `SystemSettingsTab.tsx` keys its rows
`key={feature.id}` in two places — the `LicenseTooltip` wrapper and the `TableRow` inside it — and
matches its optimistic update on `feature.id === toggledFeature.id`, while `OptionalFeatureService.updateFeature`
posts to `/optionalfeatures/${feature.id}`. With both seeded rows carrying zero, one click flips both
switches and React sees duplicate keys. All four move to `feature.key`. Routing the server by key does
not fix the client; both halves have to be in place before the second row is seeded.

## Alternatives Considered

### A. Leave the policy in `AppSetting` and render it as a row in the table — rejected

The cheapest possible reading of the story: move the UI, keep the store. No migration, no ADR
amendment, no applier.

Rejected because it puts two stores behind one visual list. The table would render one row that is not
an `OptionalFeature`, fetched from a different endpoint, written through a different path, with its
own failure modes. The story exists to remove exactly that kind of "which one is this" question from
the settings page; solving it visually while preserving it structurally is the shape of a change that
looks done and is not.

### B. A post-write domain-event handler for the ordering consequence — rejected

The house preference is the domain-event bus, and this looked like its case: a setting changed, a
handler reacts.

Rejected on two independent grounds. The dispatcher swallows handler exceptions by design, so a failed
seed is silent and leaves the instance in a state nobody can distinguish from a deliberate ordering.
And a handler runs after the write, so without §4 it would read an all-null `ManualRank` and scramble
the list — the specific failure this design exists to avoid. §4 removes the second objection; the
first stands on its own.

### C. Widen `GetEntityByIdAnExecuteAction` to allow an `ActionResult` short-circuit — rejected

Arguably the more principled fix: the helper's shape is what made the silent no-op inevitable, so fix
the shape.

Rejected on blast radius. 83 call sites across 8 controllers, none of which needs the capability. A
shared-contract change wants more than one beneficiary. If a second controller ever needs a
short-circuit, this becomes the right change and this paragraph becomes its evidence.

### D. Render the `FeatureOrdering` row's description specially in the frontend — rejected

The direct answer to ADR-134 §A.3, and ADR-134 anticipated it: "rendering that one row specially to
fix it spends the entire reuse benefit."

Still true. It also repeats itself — the next setting whose copy names a configurable term adds a
second special case, and the mechanism that should have been general never gets written.

### E. Keep both stores in sync, writing to each — rejected without much thought

Two writes, one truth, and a divergence that surfaces only on instances that have flipped the switch.
Recorded only because it is the obvious thing to reach for when a deprecated endpoint has to keep
working, and the alias delegating to one store is the answer instead.

### F. Keep `POST {id:int}` as a compatibility alias alongside the key route — rejected

It routes: the `:int` constraint disambiguates the two, so both could sit on the controller at once.
The trouble is what the alias would be compatible with. It keeps `GetById(0)` alive — the one lookup
that throws the moment a second row exists — so what it offers a caller is a 500 on every instance this
ADR ships to.

And there is no caller to offer it to. The frontend ships in the same artifact, the backend tests move
with the route, and the full `lighthouse-clients` endpoint inventory contains no `OptionalFeatures` path
(checked during DISCUSS). An alias for nobody, answering 500.

### G. Give `Id` a generated identity — rejected

The other way to make the numeric route mean something: configure value generation and let the store
number the rows.

That is a migration across all four supported providers plus a backfill of the existing `DeltaSync` row,
for a column nothing reads, to preserve a route that names rows by a number the store does not key them
by. This feature generates no EF migration; this alternative is the only thing considered in it that
would have.

### H. Keep the `{id}` route and resolve the key from the request body — rejected

The smallest diff of the three and the worst outcome. The body already carries the whole row, so the
handler could read `Key` from it and ignore the route entirely. Then the route names something the
handler does not use, and a request whose route and body disagree is accepted silently — the same class
of failure as the premium branch §6 exists to remove, arriving through a different door.

## Consequences

**Positive**

- One list answers "what is turned on in this instance", and the answer stops depending on which
  release introduced a control.
- `IOptionalFeatureApplier` gives the next setting with a consequence somewhere to put it. Epic #5733's
  `UsageData` is the obvious next one.
- §4 deletes a temporal coupling rather than documenting it. The rank seed is now correct in isolation.
- The premium branch stops lying, on an endpoint whose contract said it could not.
- Terminology-aware names and descriptions are a capability every current and future row gets.
- One store behind two doors, so the deprecated alias cannot diverge.
- The read path and the write path name a row the same way, and it is the way the store keys it. A
  second, third and fourth row can now be added without anything else being true.
- The premium refusal is symmetric and generic, so every premium row added after this one inherits the
  same answer without anybody deciding it again.

**Negative / cost**

- **A boolean store closes the door on a third ordering policy.** ADR-134's first objection, narrowed
  but not eliminated: the enum survives as the domain type, but a third value would need a new store
  rather than a new enum member. Accepted. No third policy is on the board, and the migration back to
  a keyed setting is the same shape as the one this ADR performs.
- The migration has exactly one chance to run correctly, at first add, on every existing instance. It
  cannot be repaired by a later seed. This is the highest-consequence line of code in the story.
- `OptionalFeaturesController` gains a dependency it did not have and loses a helper it shared with
  seven other controllers, so it now looks slightly less like its neighbours.
- The `AppSetting` row and both alias endpoints survive as inert surface until a contract drop. Three
  things that exist only to not break something nobody has found.
- **`POST /OptionalFeatures/{id}` is removed without an alias, which is a breaking change to a public
  route.** It is safe here only because the inventory says nothing outside this artifact calls it — a
  fact that was checked rather than assumed, and that would need checking again if the endpoint ever
  grew a client.
- The acceptance harness posts by id today: `BehaviourSettingsAcceptanceTest.ToggleOptionalFeature`
  resolves the id from the key and then posts `/api/latest/optionalfeatures/{id}`. Under §8 the helper
  posts the key directly, `IdOfStoredSetting` and `CountOfSettingsCarryingIdentity` become dead
  scaffolding, and `ToggleASettingThatDoesNotExist` aims at a key nobody can name rather than at
  `987654`. **No scenario's Given/When/Then changes** — this is a driving adapter following a change in
  the shape of its port — but the file is DISTILL's, so the consequence is stated here rather than
  discovered in a DELIVER diff.
- An instance whose licence lapsed can neither use the setting it owns nor switch it off. Pre-existing,
  preserved deliberately, and now recorded as a decision rather than inherited as a default — §6.
- ADR-134 §1 and §A are superseded 25 days after they were accepted. The reasoning was sound on the
  evidence available; two of its three grounds have since been fixed or expired, which is the ordinary
  way of things and is recorded here rather than smoothed over.

**Quality attribute impact**

- **Findability**: the property the story is actually about. Unmeasurable directly; the proxy is
  whether the next support question about ordering asks how it behaves rather than where it is.
- **Correctness**: improved. Two silent failure modes — the dropped premium write and the
  order-dependent seed — become impossible rather than avoided.
- **Modifiability**: improved for settings-with-consequences, slightly reduced for ordering policies
  specifically.
- **Performance**: unchanged. One extra row read at startup, once.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Exactly one production type constructs `FeatureComparer` | `FeatureOrderingSingleSourceArchUnitTest` — unchanged, and the reason §4 goes through the seam rather than around it |
| An instance on `ManualOrder` before the upgrade is on it after | Integration test over a seeded `AppSetting` + absent `OptionalFeature`, asserting `Enabled = true` and every `ManualRank` byte-identical |
| First enable moves nothing | Integration test asserting the sequence from `GET /features` is identical before and after the flip, on an instance with every rank null |
| The seed is correct regardless of when it runs | Unit test calling `SeedMissingRanks` with the policy already `ManualOrder` and all ranks null, asserting source order |
| A premium toggle without a licence returns 403 and persists nothing, in **both** directions | Endpoint test, all four cases: premium × licensed / unlicensed, non-premium × licensed / unlicensed — with the premium/unlicensed case run against an already-enabled row, so the refused *disable* is asserted and not merely implied |
| `DeltaSync` never becomes gated | The non-premium cases above, asserted explicitly rather than implied |
| Both doors write the same store | Integration test: write through the alias, read through `GET /OptionalFeatures` |
| Every optional feature has exactly one applier | Startup test over the registered appliers asserting one per seeded key, no duplicates, no orphans |
| A terminology token renders the instance's word | Frontend test with a renamed term asserting the rendered **name and** description |
| No route addresses an `OptionalFeature` by `Id` | Endpoint test toggling by key; and the not-found case names a key nobody seeded, which is also what a leftover numeric request now resolves to |

## Cross-reference

- [ADR-134](./adr-134-ordering-policy-appsetting-enum-single-selection-point.md) — §1 and §A
  superseded; §2, §3, §4 retained. Its §A.3 objection is answered by §7 here rather than dismissed.
- [ADR-132](./adr-132-feature-ordering-derived-total-order-no-ordering-aggregate.md) — unchanged.
  INV-A3's "in current source order" is what §4 makes explicit.
- [ADR-133](./adr-133-feature-rank-change-publishes-domain-event.md) — unchanged. §5 keeps the
  policy-change event on the same footing.
- Epic #5375 AC-2.5 (403) and AC-5.5 (terminology) are the two shipped criteria this ADR must not
  regress; §6 and §7 exist for them.
- `docs/feature/story-5876-behaviour-settings/feature-delta.md` — decisions D1-D10, DDD-1 to DDD-12,
  and Changed Assumptions CA-1 / CA-2.
