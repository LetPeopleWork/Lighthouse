# Feature Delta — story-5876-behaviour-settings

ADO **User Story #5876** — *Move "Feature Ordering" to "Optional Features"*. Active, Priority 2,
unparented, created 2026-08-31.

The ADO description is three lines and one of them is already true. This delta is the result of the
DISCUSS dive-in on 2026-08-31, grounded in a pre-DISCUSS code reality check of
`OptionalFeature.cs`, `OptionalFeatureKeys.cs`, `OptionalFeatureSeeder.cs`, `OptionalFeaturesController.cs`,
`FeatureOrderingPolicy.cs`, `FeatureOrderingPolicyProvider.cs`, `FeatureOrderingDto.cs`,
`AppSettingKeys.cs`, `AppSettingService.cs`, `AppSettingsController.cs`, `FeatureRankSeeder.cs`,
`FeatureOrdering.cs`, `RbacGuardAttribute.cs`, `SystemSettingsTab.tsx`, `FeatureOrderingSettings.tsx`,
`useFeatureOrdering.ts`, `SettingsService.ts`, and the `lighthouse-clients` endpoint inventory.

---

## Wave: DISCUSS / [REF] Persona IDs

| Persona | File | Role here |
|---|---|---|
| `config-admin` | `docs/product/personas/config-admin.yaml` | The only actor. Holds `SystemAdmin`, is the one who flips instance-wide switches, and is the one who cannot currently find them all in one place. |

No second persona. Everyone else *reads* the consequence of the setting (the `#`/*Manual* column on
the Features list) and never touches the control.

---

## Wave: DISCUSS / [REF] JTBD One-Liners

- `job-admin-find-every-instance-switch-in-one-place` — **When** I want to change how my Lighthouse
  instance behaves, **I want** every instance-wide switch in the same list, **so I can** find the one
  I need without already knowing which release introduced it.

---

## Wave: DISCUSS / [REF] Current-State Surface Inventory

The story's premise is partly stale. What is actually on `main` at 2026-08-31:

| S | Fact | Evidence |
|---|---|---|
| S1 | Ordering ownership is an **enum**, not a boolean: `SourceOrder` / `ManualOrder`, stored in `AppSetting` key `FeatureOrdering:Policy`. | `FeatureOrderingPolicy.cs`, `AppSettingKeys.cs:26` |
| S2 | It is read through one seam, `IFeatureOrderingPolicyProvider`, which ADR-134 names the single selection point. Three production consumers: `AppSettingService.cs:97`, `FeaturesController.cs:154`, `FeatureOrdering.cs:33`. An ArchUnit test guards it. | `FeatureOrderingSingleSourceArchUnitTest.cs` |
| S3 | **Flipping it on has two side-effects a plain toggle does not have.** `AppSettingService.SetFeatureOrderingPolicy` runs `featureRankSeeder.SeedMissingRanks()` *before* persisting, then publishes `FeatureOrderingPolicyChanged` → forecast re-queue. | `AppSettingService.cs:99-110` |
| S4 | The pre-write ordering is load-bearing, not stylistic. `SeedMissingRanks` calls `featureOrdering.Order(...)`, which itself reads the policy. Seed *after* the flip and it sorts by an all-null `ManualRank`, so the "order you were looking at" is replaced by Id order. | `FeatureRankSeeder.cs:19-24`, `FeatureOrdering.cs:33` |
| S5 | `OptionalFeature` **already** carries `IsPremium` and `IsPreview`. The seeder refreshes both on every upgrade and never touches `Enabled`. | `OptionalFeature.cs`, `OptionalFeatureSeeder.cs:80-88` |
| S6 | The frontend **already** renders premium optional features correctly: `LicenseTooltip` wrapper plus a disabled `Switch` when `isPremium && !canUsePremiumFeatures`. | `SystemSettingsTab.tsx` |
| S7 | Exactly **one** live optional feature: `DeltaSync` ("Faster Updates") — preview, off, **not** premium. No premium optional feature exists yet, so the premium branch has never run in production. | `OptionalFeatureSeeder.cs:54-64` |
| S8 | The premium gate **silently drops the write**: `return feature;` — HTTP 200 carrying the unchanged entity, no error, no signal. | `OptionalFeaturesController.cs:41-44` |
| S9 | A **new** key seeds with its declared `Enabled`; an **existing** key never has `Enabled` overwritten. So a new key gets exactly one chance to carry a migrated value, at first add. | `OptionalFeatureSeeder.cs:71-88` |
| S10 | Write guards already match: `RbacGuardAttribute.Requirement` defaults to `SystemAdmin`, which is what both `POST /OptionalFeatures/{id}` and `PUT /AppSettings/FeatureOrdering` require today. | `RbacGuardAttribute.cs:22` |
| S11 | `GET /AppSettings/FeatureOrdering` is **deliberately unguarded** — every feature list reads it to label its position column. `GET /OptionalFeatures` is unguarded too, so the read path survives the move unchanged. | `AppSettingsController.cs:38-45`, `OptionalFeaturesController.cs:16-34` |
| S12 | **No client calls either endpoint.** The full `lighthouse-clients` endpoint inventory contains no `AppSettings` and no `OptionalFeatures` path. | `lighthouse-clients` grep, 2026-08-31 |
| S13 | Epic #5733 slice 03 already claims the S8 fix (US-07) and already declares this migration out of its own scope — "board item, not this Epic". #5733 is blocked on legal DoR-9. | `docs/feature/epic-5733-opt-in-usage-data/slices/slice-03-admin-veto.md:18-24` |

**What this means for the story text.** "The optional features must support *Premium only* flags" is
S5 + S6: already built, never exercised. The real work is S3/S4 (a toggle that must carry side-effects
in a specific order), S9 (one shot at migrating the existing value) and S8 (a gate that lies).

---

## Wave: DISCUSS / [REF] Locked Decisions

### D1 — The value moves into `OptionalFeature`. The AppSetting row is left where it is

A new key `FeatureOrdering` is seeded with `IsPremium = true`, `Enabled = false`. On its **first add**
only, the seeder reads `AppSetting` `FeatureOrdering:Policy` and seeds `Enabled = true` where it read
`ManualOrder` (S9 gives exactly one shot at this). The `AppSetting` row is **not** deleted — additive
only, per the expand-only migration rule — it simply stops being read.

Rejected: an EF migration. This is a data move between two tables that both already exist, it must run
on four providers, and the seeder already runs at startup with the right idempotence. No schema change
is needed and none should be invented.

### D2 — `IFeatureOrderingPolicyProvider` keeps its shape; only its backing store changes

The provider swaps `IRepository<AppSetting>` for `IRepository<OptionalFeature>` and maps
`Enabled == true` → `ManualOrder`, everything else → `SourceOrder`. `AppSettingService`,
`FeaturesController` and `FeatureOrdering` are untouched, and the ArchUnit guard still holds.

This is what keeps the blast radius at one class. ADR-134 is amended to say the selection point is now
backed by an optional feature; it is **not** replaced, because the seam it names is exactly what makes
the move cheap.

### D3 — The enum survives. Only the storage becomes a boolean

ADR-132 chose an enum because *"manual sorting on/off names a switch in the UI, not the thing being
decided"*. That reasoning is about the domain vocabulary and is still right. Storing it as
`OptionalFeature.Enabled` does not make the domain a boolean — the enum stays as the type every
consumer sees. Anyone reaching for `bool` in `FeatureOrdering` or `FeaturesController` is undoing
ADR-132 and should be stopped in review.

### D4 — The side-effects run **before** the value flips, so a post-commit domain event is not enough

`SeedMissingRanks` must run before the stored value changes (S4). A handler on a
"the toggle was saved" event runs after, reads an all-null `ManualRank`, and renumbers the whole list
in Id order — the user's list visibly scrambles the first time they turn it on, which is
indistinguishable from a bug and is the exact failure ADR-132's `SeedMissingRanks`-first ordering
exists to prevent.

The standing preference for the domain-event bus does not apply to the *seeding* half. The
`FeatureOrderingPolicyChanged` publication stays an event, because it genuinely is one and its handler
genuinely does belong after the write. DESIGN chooses the mechanism for the pre-write half — a
per-key apply seam on the toggle path, or making `SeedMissingRanks` read source order explicitly
rather than through the policy — but it may not choose "publish and hope".

### D5 — The group is renamed **Behaviour Settings**

"Optional Features" reads as opt-in extras; owning your forecast order is not an extra. "Feature
Toggles" was rejected outright: *Feature* is a Terminology-configurable term, so an instance that
renamed it reads "Epic Toggles" or "Initiative Toggles". "Behaviour Settings" names what the rows
decide, covers preview flags and premium flags alike, and collides with nothing renameable.

The stored key stays `OptionalFeature`. This is a display rename, not an entity rename.

### D6 — The premium gate stops lying, and this story owns the fix

`OptionalFeaturesController.cs:41` returns 200 with the unchanged entity. This story creates the
**first** premium optional feature (S7), so it is the first change that makes the broken branch
reachable at all.

Epic #5733 slice 03 owned this fix as US-07 (S13) and is blocked on legal. The fix moved here on
2026-08-31, and #5733 was updated in the same DISCUSS pass rather than at implementation time — its
US-07 is marked TRANSFERRED and kept in full, its slice 03 asserts the behaviour instead of writing
it, and AC-07.3 (`DeltaSync` must not become gated) stays #5733's inherited invariant to check.

The transfer is reversible on purpose: if #5733 unblocks and reaches slice 03 before this story
ships, the fix returns there. It may not be skipped in either document — a privacy control cannot
ship on a gate that drops writes.

### D7 — Both `AppSettings/FeatureOrdering` endpoints stay, as deprecated aliases

`GET` and `PUT` both remain and both delegate to the same provider, so there is one store behind two
doors and no possible divergence. No client calls them (S12), so the alias exists for a caller nobody
has found rather than for a known one; it costs two delegating methods and is removed at the next
contract drop.

The user's instruction to point the clients at the new endpoint is **N/A — there is no client caller
to point.** Recorded rather than silently skipped.

### D8 — `DeltaSync` is not touched

It is not premium and must not become premium, gated, renamed or reordered by any of this. The only
thing that changes for it is the heading above the table.

---

## Wave: DISCUSS / [REF] Scope Assessment

**PASS — right-sized.** Two user stories, two slices, one bounded context (instance configuration)
touching one adjacent one (feature ordering) through a seam that already exists. No new abstraction,
no new component, no schema change, no new external integration. Under every oversized signal.

---

## Wave: DISCUSS / [REF] WS Strategy

**C — no walking skeleton.** Brownfield. Every part of the path is already running in production:
the entity, the seeder, the toggle endpoint, the premium rendering, the ordering seam. Nothing here is
a mechanism nobody has run.

---

## Wave: DISCUSS / [REF] Driving Ports

| Surface | Change |
|---|---|
| `POST /api/{v1,latest}/OptionalFeatures/{id}` | Gains a per-key apply path (D4) and an honest refusal on the premium branch (D6). |
| `GET /api/{v1,latest}/OptionalFeatures` | Unchanged shape. Returns one more row. Still unguarded (S11). |
| `PUT /api/{v1,latest}/AppSettings/FeatureOrdering` | Deprecated alias, delegates (D7). |
| `GET /api/{v1,latest}/AppSettings/FeatureOrdering` | Deprecated alias, delegates. Still unguarded. |
| Settings → System → *Behaviour Settings* | The renamed group. Gains the ordering row, loses the standalone *Feature Order* section. |

---

## Wave: DISCUSS / [REF] Pre-requisites

- None blocking. Epic #5375 (Manual Sorting) shipped and is the thing being moved.
- Coordination done: the back-propagation into Epic #5733 (D6) landed in this DISCUSS pass, so the
  two documents never both claim the fix. Nothing is left waiting on slice 02.

---

## Wave: DISCUSS / [REF] Out of Scope

- Making the premium gate a first-class concept across every controller. This story fixes one gate on
  one controller.
- Any change to what manual ordering *does* — the move actions, the `#`/*Manual* column, the shared-
  Feature behaviour, ADO #5691 slice 04. Moving the switch is not touching the feature.
- Renaming the `OptionalFeature` entity, table or route (D5 is a display rename).
- Deleting the `AppSetting` row or the alias endpoints (D1, D7).
- Any change to `DeltaSync` (D8).
- Per-key configuration richer than a boolean. If a future setting needs three states, it does not
  belong in this table and this story does not make room for it.

---

## Wave: DISCUSS / [REF] User Stories

### US-01 — Find every instance switch in one list

`job_id: job-admin-find-every-instance-switch-in-one-place` · Slice 01

As a system administrator, I want the setting that decides who owns my forecast order to sit with the
other instance-wide switches, so that finding it does not depend on remembering that it shipped as its
own section.

#### Elevator Pitch
Before: Settings → System shows *Optional Features* (one row) and, three groups further down, a
separate *Feature Order* section with a lone switch. Which switches live where is a fact about release
history, not about what they do.
After: open **Settings → System → Behaviour Settings** → sees one table listing *Faster Updates* and
*Let Lighthouse own the order of your Features*, the second carrying the premium affordance, and no
separate *Feature Order* section anywhere on the page.
Decision enabled: which instance behaviours are currently on — answerable by reading one table instead
of scanning a page for sections that happen to contain a switch.

#### Acceptance Criteria
- **AC-01.1** Settings → System renders a group headed **Behaviour Settings** containing both
  `DeltaSync` and `FeatureOrdering`; no group headed *Optional Features* and no group headed
  *&lt;Feature&gt; Order* remains on the page.
- **AC-01.2** The `FeatureOrdering` row shows the premium affordance — `LicenseTooltip` plus a
  disabled switch — on an instance without a premium licence, exactly as `SystemSettingsTab` already
  renders premium rows (S6).
- **AC-01.3** On an instance that had `FeatureOrdering:Policy = ManualOrder` before the upgrade, the
  row reads **on** after the upgrade, and the Features list still shows the *Manual* column heading
  with every place unchanged.
- **AC-01.4** On an instance that had no row, or `SourceOrder`, the row reads **off** and the Features
  list still shows the `#` heading.
- **AC-01.5** Turning the row on for the first time leaves the visible order of the Features list
  **identical** — the seeded places are the ones the administrator was looking at, not Id order
  (S4). This is the regression test for D4.
- **AC-01.6** Turning the row on or off re-queues the forecasts, i.e. `FeatureOrderingPolicyChanged`
  is still published on both transitions.
- **AC-01.7** Turning the row off and on again restores the previously chosen places rather than
  re-reading them from the tracker — the existing revert guarantee is unchanged.
- **AC-01.8** `PUT /api/latest/AppSettings/FeatureOrdering` still succeeds, still applies the same
  side-effects in the same order, and the change is visible in `GET /api/latest/OptionalFeatures` —
  one store, two doors (D7).
- **AC-01.9** `DeltaSync` is unchanged in name, description, preview badge, premium status and stored
  `Enabled` value (D8).
- **AC-01.10** The upgrade does not delete the `AppSetting` row (D1).

### US-02 — Be refused out loud, not in silence

`job_id: job-admin-find-every-instance-switch-in-one-place` · Slice 02

As a system administrator on a Community instance, I want a toggle my licence does not cover to say so,
so that I do not believe I have changed something that is still running as before.

#### Elevator Pitch
Before: `POST /api/latest/OptionalFeatures/{id}` on a premium feature without a premium licence returns
**200 OK carrying the unchanged entity** (S8). The write vanished and the response says it worked.
After: `POST /api/latest/OptionalFeatures/{id}` for a premium feature without a premium licence → sees
an explicit refusal status, not a success carrying stale state.
Decision enabled: whether the setting shown on screen is the setting actually in force.

#### Acceptance Criteria
- **AC-02.1** A premium optional feature toggled without a premium licence returns an explicit refusal
  and does not persist the change.
- **AC-02.2** A premium optional feature toggled **with** a premium licence persists and returns the
  updated entity.
- **AC-02.3** A non-premium optional feature — `DeltaSync` — is unaffected on licensed and unlicensed
  instances alike (D8, and inherited from Epic #5733 AC-07.3).
- **AC-02.4** The refusal is reachable only by calling the API directly; the UI continues to disable
  the control, so no user-facing flow starts producing errors (S6).
- **AC-02.5** Epic #5733's documents already record the transfer (done in DISCUSS, 2026-08-31). This
  AC is a check, not work: neither document claims the fix, and #5733 slice 03 still names AC-07.3 as
  its own invariant.

---

## Wave: DISCUSS / [REF] Story Map

**Backbone:** Configure my instance → find the switch → understand what it costs → flip it → trust it flipped.

| Slice | Stories | Ships |
|---|---|---|
| 01 — *One list of switches* | US-01 | The whole move: key, seeded migration, provider re-backing, pre-write apply path, alias delegation, table row, section removal, rename, docs. |
| 02 — *A refused toggle says so* | US-02 | The premium gate returns a refusal; #5733 back-propagation. |

Slice 01 is the story. Slice 02 is independent of it in code and dependent on it in meaning — before
slice 01 no premium optional feature exists, so slice 02 alone would fix a branch nothing reaches.

---

## Wave: DISCUSS / [REF] Slice Taste Tests

| Test | Verdict |
|---|---|
| Ships 4+ new components? | No. Slice 01 ships **zero** new components — it moves one and deletes one. |
| Every slice depends on a new abstraction? | No new abstraction. D2 deliberately reuses the existing seam. |
| Does any slice disprove a pre-commitment? | Yes, both. Slice 01 disproves "an optional-feature toggle can carry a side-effecting setting". Slice 02 disproves "no caller depends on the 200-unchanged shape". |
| Synthetic data only? | No. Both slices are accepted on the vendor's own instance with real Features and a real licence, then again with the licence removed. |
| Two slices identical but for scale? | No. Different surfaces, different failure modes. |

All pass.

---

## Wave: DISCUSS / [REF] Prioritization

1. **Slice 01 first — highest learning leverage.** D4 is the one decision that can be wrong in a way
   users see. If the pre-write apply path is wrong, the first administrator to flip the switch watches
   their list scramble, and that is cheapest to discover before anything else is built on it.
2. **Slice 02 second — no urgency, low uncertainty.** The broken branch is unreachable through the UI
   (S6), so shipping slice 01 without it exposes only direct API callers. It is also the slice that
   touches another feature's documents, which is better done once slice 01's shape is settled.

---

## Wave: DISCUSS / [REF] Outcome KPIs

| KPI | Target | Measurement |
|---|---|---|
| KPI-1 — Migration fidelity | **100%** of instances that read `ManualOrder` before the upgrade read the row as on after it, with every `ManualRank` unchanged | Vendor instance plus the Postgres dev restore, before/after comparison of the `Features.ManualRank` column |
| KPI-2 — Nothing moves on first enable | **0** Features change visible position when the switch is first turned on | AC-01.5 acceptance test, plus a manual dogfood check on the vendor instance |
| KPI-3 — Forecast re-queue survives the move | `FeatureOrderingPolicyChanged` published on **both** transitions, 100% of flips | Acceptance test asserting the published event; the existing `FeatureOrderingPolicyChangedForecastTriggerHandler` test stays green |
| KPI-4 — No silent drops | **0** responses that return 200 with an unchanged entity on a refused premium toggle | AC-02.1 acceptance test |
| KPI-5 — Findability | The next support question about ordering asks how it behaves, not where the switch is | Qualitative, watched for one release |

---

## Wave: DISCUSS / [REF] Definition of Done

1. Both slices' acceptance criteria pass as automated tests.
2. `dotnet build` zero warnings; `dotnet test` green on the non-connector filter.
3. `pnpm test`, `pnpm build` and Biome clean on `./src`.
4. SonarQube Cloud introduces no new issue of any severity.
5. Mutation testing run per-feature on both stacks, ≥80% kill rate, recorded under `docs/feature/story-5876-behaviour-settings/mutation/`.
6. `docs/settings/configuration.md` updated: the *Optional Features* heading becomes *Behaviour Settings*, and the *Feature Order (Premium)* section folds into it as a row description.
7. The `settings/optionalfeatures.png` screenshot regenerated against the renamed group showing both rows (delete the PNG first — the regen keeps the old file when the diff is under 0.5%).
8. ADR-134 amended to record the store change (D1, D2); ADR-132's enum reasoning explicitly preserved (D3).
9. Epic #5733's delta and slice 03 record that US-07 landed here — done in DISCUSS on 2026-08-31; re-check it still reads true at finalization (AC-02.5).

---

## Wave: DISCUSS / [REF] DoR Validation

| # | Item | Evidence |
|---|---|---|
| 1 | Business value stated | US-01 and US-02 elevator pitches; `job-admin-find-every-instance-switch-in-one-place`. |
| 2 | Job traceability | Both stories carry a real `job_id`; no `infrastructure-only` escape used. |
| 3 | Acceptance criteria testable | 15 ACs, each asserting an observable response, rendered element or stored value. |
| 4 | Dependencies known | None blocking. One coordination item with Epic #5733 (D6), which is itself blocked and therefore cannot race this. |
| 5 | Sized | Two slices, ~6h and ~2h of crafter dispatch. |
| 6 | Technical feasibility | Every mechanism already runs in production (S2, S5, S6, S10). The one novel decision is D4, and S4 shows exactly what it must avoid. |
| 7 | Non-functional constraints | No new endpoint, no new query, no schema change. The seeder gains one predicate at startup. |
| 8 | UX defined | The target is the existing `SystemSettingsTab` table plus a heading string; the section being removed is a whole component. |
| 9 | Testable in isolation | Backend via `OptionalFeaturesControllerTest`, `OptionalFeatureSeederTests` and the `ManualSorting` integration suite; frontend via `SystemSettingsTab.test.tsx`. |

**Requirements completeness: 0.97.** The one open item is D4's mechanism, which is deliberately left
to DESIGN — the constraint is fixed, the implementation is not.

**Per-wave peer review: skipped.** No trigger fired — one persona, no contested DoR item, no
vendor-neutrality surface, no regulatory language. The consolidated review fires at end of DISTILL.

**Expansion catalog: no trigger fired.** AC ambiguity no; cross-context complexity no (two contexts);
multi-stakeholder no (one persona); compliance no; WS strategy is C not D. Strict lean output.

---

## Wave: DISCUSS / [REF] Wave Decisions Summary

### Key Decisions
- **[D1]** Value moves into `OptionalFeature`, migrated once by the seeder, `AppSetting` row left in place — expand-only, no EF migration for a data move between existing tables.
- **[D2]** `IFeatureOrderingPolicyProvider` keeps its shape, swaps its backing store. Blast radius: one class.
- **[D3]** The enum stays the domain vocabulary; only storage becomes boolean. ADR-132 preserved.
- **[D4]** Side-effects run before the flip. A post-commit domain event is insufficient and would visibly scramble the list.
- **[D5]** Group renamed *Behaviour Settings*. *Feature Toggles* rejected — collides with the configurable term *Feature*.
- **[D6]** The premium silent-drop fix moves here from Epic #5733 slice 03.
- **[D7]** Both `AppSettings/FeatureOrdering` endpoints stay as delegating aliases. No client caller exists to repoint.
- **[D8]** `DeltaSync` untouched.

### Requirements Summary
- Primary need: instance-wide switches live in one list, so finding one does not depend on knowing which release introduced it. The secondary need is that a switch the licence refuses says so.
- Walking skeleton: N/A (strategy C, brownfield).
- Feature type: **cross-cutting** — a configuration surface, a licensing gate and a forecasting side-effect meeting on one toggle.

### Constraints Established
- `SeedMissingRanks` must observe the pre-flip order. Non-negotiable; it is the difference between "nothing moves" and "everything moves".
- The ordering seam stays single. Any new read path takes `IFeatureOrderingPolicyProvider`, never a comparer — the ArchUnit guard stays green.
- `DeltaSync` must not become premium or gated.
- The seeder has exactly one opportunity to carry the old value across (S9); a shipped release with a wrong migration cannot be corrected by a later seed.

### Upstream Changes
- Epic #5733's slice 03 loses US-07 to this story (D6). Its US-07 is marked TRANSFERRED and kept in full, its slice 03 asserts the refusal rather than writing it, and its DESIGN reuse table now reads UNCHANGED HERE for `OptionalFeaturesController`. AC-07.3 stays #5733's invariant. Applied 2026-08-31; #5733 is not otherwise modified, and the transfer reverses if #5733 reaches slice 03 first.

---

## Wave: DISCUSS / [REF] SSOT Updates

- `docs/product/jobs.yaml` — adds `job-admin-find-every-instance-switch-in-one-place`.
- `docs/product/journeys/story-5876-behaviour-settings.yaml` — new journey.
- `docs/product/personas/config-admin.yaml` — reused unchanged; no new persona.

---

## Wave: DISCUSS / [REF] Handoff

**To:** `nw-solution-architect` (DESIGN) — full artifact set.
**Also to:** `nw-platform-architect` (DEVOPS) — outcome KPIs only.

The one question DESIGN must answer: **the mechanism for D4.** A per-key apply seam on the toggle
path, or `SeedMissingRanks` reading source order explicitly instead of through the policy. Both
satisfy the constraint; they differ in whether `OptionalFeaturesController` learns that one key is
special, which is the kind of `if` ADR-134 exists to prevent.
