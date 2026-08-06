# Slice 02 — Lighthouse takes ownership of the order (walking skeleton)

**Feature**: epic-5375-manual-sorting · **ADO**: Epic #5375 · **Stories**: US-02, US-05 · **Estimate**: ~6h
**Reference class**: the premium-gated instance settings already on `Settings → System`
(`pages/Settings/System/SystemSettingsTab.tsx`) + `LicenseGuardAttribute`.

## Goal

A config admin flips one switch, the Feature list does not move a pixel, and from then on a full refresh
no longer re-sequences the forecast. Flipping it back gives the tracker's order straight back.

## IN scope

- `Feature.ManualRank` — new **nullable int**, additive column, migration generated with the existing
  `CreateMigration` PowerShell script across all providers. Expand-only.
- Instance setting for manual sorting, `SystemAdmin`-guarded, with
  `[LicenseGuard(RequirePremium = true)]` on the enable path (S11). Storage choice is DESIGN's open
  question 1 — `AppSetting` proposed.
- **Seeding on enable** (D6): every Feature gets a rank from the current `FeatureComparer` result,
  1..N, in one transaction.
- **One** selection point for the ordering comparison, consumed by all five call sites (S4):
  `FeatureRepository.GetAll:18`, `GetAllByPredicate:23`, `PortfolioDto.cs:15`,
  `FeaturesController.cs:93`, `WorkItemService.cs:535`. Five independent `if` statements is the failure
  mode K4 exists to catch.
- **`WorkItemBase.Order` stays untouched** (D5) — it keeps taking the source value on every sync
  (`WorkItemBase.cs:142`). Only the *comparison* changes.
- New Features get an end-of-list rank on arrival, silently (D7).
- Off-switch (US-05): source order returns immediately, `ManualRank` values are **retained**, re-enabling
  restores them rather than re-seeding (D9).
- Slice 01's position column header switches to the manual label while on, on both surfaces.
- Tests: AC-2.1 … AC-2.7 and AC-5.1 … AC-5.5, with AC-2.1 parameterised over all five `Order` shapes.

## OUT of scope

- Any way to *change* the order. That is slice 03 — after this slice the order is frozen, not editable,
  and that is a coherent product on its own.
- All move actions and the reorder endpoint (slice 03), and the target picker (slice 04).
- Backfilling anything, and any change to `FeatureComparer`'s existing source-order rules.

## Learning hypothesis

**Disproves** "seeding from the current order is invisible to the user" **if** the seeded order differs
from what was on screen before the flip. `FeatureComparer` puts int-parseable values ahead of everything,
inverts doubles for Linear, and otherwise falls back to `string.Compare` — and it is applied at five
separate call sites, two of which (`PortfolioDto`, `FeaturesController`) sort a *subset*. A subset sort
with unstable tie-breaking can order ties differently from the full-set sort the seeding reads, so two
users could have been looking at two different orders all along.

If it fails, D6 cannot re-derive the order at enable time — it needs an explicit ordering snapshot
captured from the same query path the user was looking at, and the "nothing moves" promise in US-02's
pitch has to be re-scoped or dropped.

**Confirms**, if it holds, that D6 is safe and that slice 03 can assume one unambiguous global sequence.

## Verify the premise first (30 min, before the migration)

On the dev instance, dump the sequence produced by each of the five call sites for one Portfolio's
Features and diff them. Any pair that disagrees is the hypothesis failing, and it is cheaper to find here
than after a migration ships.

## Acceptance criteria

AC-2.1 … AC-2.7, AC-5.1 … AC-5.5 verbatim from `feature-delta.md`. The three that carry the slice:

- Enable on data whose `Order` values are LexoRank strings, inverted Linear doubles, ADO ints,
  ServiceNow record numbers and empty strings — the rendered list is identical before and after (AC-2.1).
- Five consecutive full refreshes change no rank, while `Order` is still updated from source (AC-2.2).
- Off → on returns the retained manual order, not a fresh seed (AC-5.3).

## Dependencies

Slice 01's `rank` field on `FeatureDto` and the Features view (reused; `rank` is now sourced from
`ManualRank` when the switch is on). Premium licence on the dev instance
(`reference_premium_license_dev_seed`).

## Dogfood moment

Same day: enable on the dogfood instance, screenshot the Features view before and after, then trigger a
full refresh and confirm nothing moved. Leave it on overnight — K2 wants five refreshes with zero churn,
and an overnight run is the cheapest way to get them.
