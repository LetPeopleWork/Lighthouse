# ADR-134: The Ordering Policy is an AppSetting enum, read at exactly one ordering port

**Status**: **PARTIALLY SUPERSEDED (2026-08-31) by [ADR-187](./adr-187-ordering-policy-optional-feature-with-per-key-applier.md)** — §1 (storage) and §A (the rejection of `OptionalFeature`) no longer hold. §2 (the single ordering seam), §3 (INV-A3) and §4 (the sync path never writes `ManualRank`) are **retained in full**, as is every enforcement rule. Originally Accepted 2026-08-06 (Morgan, interaction mode PROPOSE)
**Date**: 2026-08-06
**Feature**: `epic-5375-manual-sorting` (ADO Epic #5375 "Manual Sorting")
**Decider**: Morgan (Solution Architect), DESIGN application layer, interaction mode = PROPOSE

---

## Context

[ADR-132](./adr-132-feature-ordering-derived-total-order-no-ordering-aggregate.md) fixed the domain
shape — `Feature.ManualRank` is a scalar, the sequence is derived, and the instance owns a **policy**
(`SourceOrder` | `ManualOrder`), explicitly "an enum, not a boolean". It deliberately left two
application-layer questions open: where that policy is persisted, and where the choice between the
two comparisons is made.

The second question is the load-bearing one. Five production sites construct `FeatureComparer` today:

| Site | Shape |
|---|---|
| `FeatureRepository.cs:18` (`GetAll`) | sorts the whole materialised table |
| `FeatureRepository.cs:23` (`GetAllByPredicate`) | sorts a **subset**, after a DB-level predicate |
| `PortfolioDto.cs:15` | sorts `portfolio.Features`, a navigation collection, inside a DTO constructor |
| `FeaturesController.cs:93` | sorts the result of `GetAllByPredicate` — i.e. **re-sorts an already-sorted sequence** |
| `WorkItemService.cs:535` | sorts an in-memory list during sync, before `Save` |

KPI K4 requires all of them to return the same sequence in every policy state. Five independent `if
(policy == ManualOrder)` statements is the named failure mode, and it is a bad one: a site that
diverges is wrong only for instances that have flipped the switch, which is the smallest and most
motivated population.

`Feature` is also read on paths that have no `ClaimsPrincipal` and no HTTP context at all
(`ForecastService.cs:72`, `WorkItemService`, background refresh), so whatever reads the policy must be
callable from a plain service.

## Decision

**The Ordering Policy is stored as a single `AppSetting` row carrying the enum name. Exactly one
production type — `FeatureOrdering` — reads it and selects a comparison. Every ordering site calls
that type; none constructs a comparer.**

### 1. Storage — `AppSetting`, not `OptionalFeature`

| Concern | Resolution |
|---|---|
| Key | `FeatureOrdering:Policy` in `AppSettingKeys` (`Models/AppSettings/AppSettingKeys.cs`) |
| Value | `"SourceOrder"` \| `"ManualOrder"` — the enum name, round-tripped |
| Default | Absent row reads as `SourceOrder`. No seeder entry is required, and an unseeded instance is a correct instance |
| Read | `IFeatureOrderingPolicyProvider.Current` — one method, no principal, no HTTP |
| Write | `PUT api/v1\|latest/appsettings/FeatureOrdering`, `[LicenseGuard(RequirePremium = true)]` on the controller action; `AppSettingsController`'s class-level `[RbacGuard]` already defaults to `SystemAdmin` (`RbacGuardAttribute.cs:22`), which is AC-2.7 for free |

### 2. The seam — one ordering port, five sites become four

```
IFeatureOrdering.Order(IEnumerable<Feature>) -> IEnumerable<Feature>
```

One implementation. It reads the policy once and applies either `FeatureComparer` (unchanged, source
semantics preserved verbatim per the DISCUSS out-of-scope list) or `ManualRankComparer`
(`ManualRank` ASC, **nulls last**, `Feature.Id` ASC — INV-O1's full key).

| Site | Disposition |
|---|---|
| `FeatureRepository.cs:18` / `:23` | EXTEND — `featureOrdering.Order(...)` |
| `WorkItemService.cs:535` | EXTEND — inject `IFeatureOrdering` |
| `PortfolioDto.cs:15` | EXTEND — the constructor takes `IFeatureOrdering`. Precedent: `FeatureDto` already takes `ILighthouseClock` (`FeatureDto.cs:16`). Two construction sites: `PortfolioController.cs:47`, `PortfoliosController.cs:50` |
| `FeaturesController.cs:93` | **DELETED.** `GetAllByPredicate` already ordered; the re-sort is redundant today and would be a divergence risk tomorrow |

Five sites become four, and `FeatureComparer` becomes reachable from exactly one production type.

### 3. Seeding on enable — fill nulls only, appending

> **INV-A3.** Flipping to `ManualOrder` assigns a rank **only** to Features whose `ManualRank` is
> null, in current source order, starting at `max(existing rank) + 1`.

One rule, three ACs. On a first enable every rank is null, so this is `1..N` in exactly the pre-flip
order — D6 and AC-2.1. On a re-enable (D9) only Features that arrived while the policy was off are
null, so they append rather than interleaving into the retained manual order — AC-5.3. And a Feature
arriving *while* the policy is on is never seeded at all, because a null rank already sorts last by
INV-O1 — AC-2.6, which ADR-132 explicitly permits to be satisfied this cheaply.

The seed is **synchronous**, inside the policy-flip request: one ordered projection read plus one
batched write. ADR-132's INV-O1 is what makes this safe — a partially-seeded instance is still
totally ordered — so there is nothing to make atomic and nothing to show a progress bar for.

### 4. The sync path never writes `ManualRank`

ADR-132's INV-O4 says the sync *should* assign `max + 1` on arrival but that correctness does not
depend on it. **We decline to write it.** A null rank sorts last, which is D7's behaviour exactly, and
declining makes "the sync path never touches `ManualRank`" an absolute rather than a qualified
statement — which is what K2 and AC-2.2 assert. Move-to-Bottom materialises ranks lazily for the
null-ranked rows it must jump (INV-O4), bounded work on the one gesture that cares.

This is reinforced structurally rather than by a guard: `Feature.Update(Feature)` (`Feature.cs:172-178`)
copies fields by explicit enumeration, and `ManualRank` is simply absent from that list, while
`base.Update` continues to copy `Order` (`WorkItemBase.cs:142`). D5's two-field independence is a
property of what the copy list contains, so "the sync clobbered a rank" is not a state the code can
reach without someone adding a line.

## Alternatives Considered

### A. Store the policy as an `OptionalFeature` row with `IsPremium = true` — rejected

> **Overtaken by [ADR-187](./adr-187-ordering-policy-optional-feature-with-per-key-applier.md)
> (2026-08-31).** Two of the three grounds below have since been fixed or expired: the silent no-op is
> a consequence of `ApiHelpers.GetEntityByIdAnExecuteAction` always wrapping in `Ok(...)` and is
> repaired, and the dormant-section point died when `DeltaSync` shipped. The terminology objection
> (§A.3) was correct and is answered by resolving terminology tokens in a row's **name as well as its
> description**, for every row rather than for one. The `bool` objection survives, narrowed: the enum
> stays the domain type and only the storage is boolean, and `FeatureOrderingPolicyProvider` is the
> single place that knows the two are the same choice said two ways. Kept unedited below as the
> reasoning of its time.

Genuinely attractive, and the option DISCUSS's framing pointed away from for the wrong reason. DISCUSS
called `OptionalFeature` "preview capability, not licensed setting"; the entity carries `IsPremium`
and `IsPreview` as **separate orthogonal flags** (`OptionalFeature.cs:17-19`), so that objection does
not hold. What the option really offers is a lot of shipped machinery: a `SystemAdmin` guard on the
update action, a premium check, a Settings UI row with a premium-disabled `Switch` and a
`LicenseTooltip` (`SystemSettingsTab.tsx:108-152`), a seeder, a frontend service and model.

Rejected on three counts, in ascending order of weight:

1. **It is a `bool`.** ADR-132 named the policy an enum precisely because "manual sorting on/off" is
   the UI's word for it, not the domain's. Encoding a growing enum in a boolean is the tri-state trap,
   and the sparse-key option ADR-132 §D keeps deliberately open would be the first thing to hit it.
2. **Its premium path is a silent no-op, not a 403.** `OptionalFeaturesController.cs:41-44` returns
   the unchanged feature when the licence is missing. AC-2.5 requires 403. Getting there means
   changing shared semantics for a mechanism whose whole value here was that it was already correct.
3. **The free UI cannot satisfy AC-5.5, and this is decisive.** The Optional Features section renders
   a generic Name / Description / Enabled table over a `Description` string persisted by a server-side
   seeder. AC-5.5 requires the switch's help text to state, *in the instance's own terminology*, what
   turning it off does and that the manual order is retained. Terminology is resolved client-side
   through `getTerm` (D16); a seeded database string cannot be. So the row would ship copy saying
   "Feature" to an instance that calls them "Deliverables" — a direct D16 violation. Rendering that one
   row specially to fix it spends the entire reuse benefit.

There is a fourth, softer point worth recording: `OptionalFeatureSeeder.GetOptionalFeatures()`
currently returns an empty list and every historical key is in the deprecated-removal set, so a fresh
instance has **zero** optional features and `SystemSettingsTab.tsx:96` hides the section entirely.
Adding this row would resurrect a dormant UI section — a visible product change smuggled in as a
storage decision.

### B. A mode parameter on `FeatureComparer` (`new FeatureComparer(policy)`) — rejected

The narrowest possible change, and it keeps the comparison in one class.

Rejected because it leaves five construction sites and hands each of them the obligation to obtain the
policy. It relocates the five-`if` failure mode into five constructor arguments without removing it —
and two of the five (`PortfolioDto`, `WorkItemService`) have no natural access to the policy today, so
it would be threaded through their callers as well.

### C. Push ordering entirely into the repository so callers cannot choose — rejected as insufficient

The right instinct, and it covers three sites. It cannot reach `PortfolioDto.cs:15`, which sorts an EF
navigation collection rather than a repository result, nor `WorkItemService.cs:535`, which sorts a list
it built during sync before `Save`. A partial solution here is worse than an explicit one, because it
would read as complete.

### D. A dedicated `FeatureOrderingSettings` entity and table — rejected

One value, one row, one column, plus a migration, a repository, a controller and a frontend model,
when two general instance-settings mechanisms already exist. No property of the value justifies it.

## Consequences

**Positive**

- One production type constructs a comparer, and an ArchUnitNET rule can say so in one line.
- The redundant sort at `FeaturesController.cs:93` is deleted rather than converted, so the surface
  this feature must keep consistent gets smaller, not larger.
- The policy read needs no principal and no HTTP context, so `ForecastService` and the background
  refresh path see the same order as the API without special-casing.
- An unseeded or downgraded instance reads `SourceOrder` and behaves exactly as it does today.

**Negative / cost**

- Five new small backend pieces the `OptionalFeature` route would have given for free: the setting key,
  two `IAppSettingService` methods, two `AppSettingsController` actions, a frontend service method and
  a Settings panel component. This is the price of AC-5.5 and it is paid knowingly.
- `PortfolioDto`'s constructor gains a service parameter, touching two call sites and their tests. It
  matches `FeatureDto`'s existing shape, but it does make a DTO less of a plain projection.
- **`WorkItemService.cs:535` orders Features whose `Id` is still 0** — they are not persisted until the
  `Save` two lines later, which the file's own comment at `:539-540` already documents for a different
  reason. Under `ManualOrder` the `Id` tie-break is therefore degenerate for newly-arrived Features at
  that one site. INV-O1 keeps the result deterministic and the collection is re-sorted on every read
  path anyway, so nothing observable depends on it — but it is a real edge that neither DISCUSS nor
  the domain layer caught, and it is recorded rather than left to be rediscovered.

**Quality attribute impact**

- Correctness / consistency: K4 becomes a structural property enforced by a compile-time-visible rule,
  rather than a convention re-established at each call site.
- Modifiability: adding a third policy is a new enum member and one `switch` arm in one type.
- Performance: unchanged. The same comparisons run over the same materialised collections.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| Exactly one production type constructs `FeatureComparer` | `FeatureOrderingSingleSourceArchUnitTest` — `Classes().That().AreNot(typeof(FeatureOrdering)).Should().NotDependOnAny(typeof(FeatureComparer))`, mirroring `LicenseGateSingleSourceArchUnitTest.cs` verbatim |
| The four remaining ordering sites agree in every policy state | Integration test over a deliberately gapped + duplicated + partially-null rank set, asserting identical sequences from `FeatureRepository.GetAll`, `GetAllByPredicate`, `PortfolioDto.Features` and `GET /features` (ADR-132's enforcement row, one site lighter) |
| A full refresh changes no existing rank | Integration test: seed, refresh, assert every `ManualRank` byte-identical and every `Order` updated (K2 / AC-2.2) |
| `Feature.Update` does not copy `ManualRank` | Unit test on `Update` asserting the target's rank survives and its `Order` does not |
| Re-enabling does not re-seed | Integration test: enable, move, disable, add a Feature, re-enable — assert the manual order is intact and the new arrival is last (AC-5.3) |
| The enable endpoint is premium-gated and `SystemAdmin`-gated | Endpoint tests for 403 on each (AC-2.5, AC-2.7) |

## Cross-reference

- [ADR-132](./adr-132-feature-ordering-derived-total-order-no-ordering-aggregate.md) — answers its
  "Deliberately left open" rows 1 and 2. INV-O1's full sort key is what `ManualRankComparer`
  implements; INV-O4 is what §4 declines to use.
- [ADR-135](./adr-135-feature-position-computed-global-ordinal.md) — the read side reuses the
  comparison selected here, which is why the position map cannot be a SQL window function.
- Extends **ADR-027** — no clause superseded; no new aggregate, no new token.
- DISCUSS decisions D2, D5, D6, D7, D9 and open questions 1, 2, 6 in
  `docs/feature/epic-5375-manual-sorting/feature-delta.md`.
