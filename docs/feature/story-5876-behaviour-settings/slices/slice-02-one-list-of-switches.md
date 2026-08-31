# Slice 02 — One list of switches

## Goal

A system administrator finds every instance-wide switch in one table, including the one that decides
who owns the forecast order, and turning it on still moves nothing.

## IN scope

- `OptionalFeatureKeys.FeatureOrderingKey`, seeded `IsPremium = true`, `Enabled = false`, named for
  what it does rather than for the mechanism.
- **One-time value migration inside the seeder**, on the first-add path only: `AppSetting`
  `FeatureOrdering:Policy == ManualOrder` → `Enabled = true`. The `AppSetting` row is left in place.
- `FeatureOrderingPolicyProvider` re-backed onto `IRepository<OptionalFeature>`, mapping
  `Enabled` → `ManualOrder` / `SourceOrder`. Its interface, and every consumer, unchanged.
- A pre-write apply path on the toggle so `SeedMissingRanks()` still runs **before** the stored value
  changes, and `FeatureOrderingPolicyChanged` is still published after it.
- `GET`/`PUT /AppSettings/FeatureOrdering` kept as delegating aliases over the same path.
- Frontend: the `FeatureOrdering` row appears in the table; `FeatureOrderingSettings.tsx` and its
  `InputGroup` are deleted; `useFeatureOrdering` reads the policy from the optional feature; the group
  heading becomes **Behaviour Settings**.
- **Terminology token substitution in the description cell.** Seeded descriptions may carry
  placeholder tokens, resolved through `getTerm` when the cell renders. This is what makes the seeded
  string satisfy Epic #5375 AC-5.5, and it is a general capability of the table — every row gets it,
  including `DeltaSync` and every future one. Without it the row says "Feature" on an instance that
  renamed it, which is the objection ADR-134 §A.3 rejected this whole store on.
- Docs: `docs/settings/configuration.md` heading rename, *Feature Order (Premium)* folded in as the
  row's description, `settings/optionalfeatures.png` regenerated (delete the PNG first).
- ADR-134 amended; ADR-132's enum reasoning explicitly preserved.

## OUT of scope

- The premium gate silent-drop. Slice 01, which runs first precisely so this slice inherits a gate
  that already answers 403.
- Deleting the `AppSetting` row or the alias endpoints.
- Any change to what manual ordering does — move actions, the position column, shared Features,
  ADO #5691.
- Any change to `DeltaSync` beyond the heading above it.
- Renaming the `OptionalFeature` entity, table or route.

## Learning hypothesis

**Disproves "an optional-feature toggle can carry a side-effecting setting" if** turning the switch on
for the first time moves anything in the Features list. `SeedMissingRanks` reads the order through
`IFeatureOrdering`, which reads the policy; seed after the flip and it sorts by an all-null
`ManualRank`, so the list is renumbered in Id order. The whole promise of this control is "nothing
moves when you turn it on" — if that breaks, the toggle table is the wrong home and the setting stays
where it is.

**Confirms, if it succeeds,** that the table can host settings that are more than a stored boolean,
which is what makes it a place to consolidate rather than a place for flags.

## Acceptance criteria

Per US-01 (AC-01.1…01.10) in `feature-delta.md`. The three that carry it:

- **AC-01.5** — first enable moves nothing. The regression test for the ordering constraint.
- **AC-01.3** — an instance already on `ManualOrder` stays on it across the upgrade, with every place
  intact. The seeder gets exactly one chance at this.
- **AC-01.11** — an instance that renamed *Feature* reads its own word in the row's description. The
  criterion ADR-134 said this store could not meet.
- **AC-01.6** — both transitions still re-queue the forecasts.

## Production-data acceptance

Run against the Postgres dev restore, which carries real Features with real `ManualRank` values, and
against the vendor instance with a real premium licence. Compare the `Features.ManualRank` column
before and after the upgrade: it must be byte-identical. Then remove the licence and confirm the row
renders disabled with the premium tooltip.

## Dogfood moment

Same day: upgrade the vendor instance, confirm the row reads on and the Features list still shows
*Manual* with the same sequence, flip it off and on again, confirm the places come back and the
forecasts re-queue.

## Dependencies

Slice 01 — the gate must already answer 403 before a setting that promises 403 moves onto it
(Epic #5375 AC-2.5).

Epic #5375 shipped; this moves its switch.

## Effort estimate

~7h of crafter dispatch. Backend ~3h (key, seeder migration, provider re-backing, apply path, aliases,
tests), frontend ~3h (row, token substitution, component deletion, hook rewire, tests), docs and
screenshot ~1h. Over the ideal one-day line by an hour; splitting it further would produce a slice
with no user-visible value, which the composition gate forbids.

## Reference class

Epic #5375 slice 02 — same ordering seam, same seeder, same test suite. Estimated 1 day, took 1 day.

## Pre-slice SPIKE

Not needed. The one uncertainty is D4's mechanism, which DESIGN settles from the evidence already in
`feature-delta.md` (S4) — no code needs to be written to learn it.
