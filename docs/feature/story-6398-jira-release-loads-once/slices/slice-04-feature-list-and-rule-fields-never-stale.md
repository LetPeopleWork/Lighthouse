# Slice 04: The Manual Feature list and the rule fields never show a stale or empty answer (ADO #6398)

*(Added by MD-26 after the final-wave review, DESIGN review H-1.)*

**Goal:** the dialog's two other fetches, the Manual tab's Feature list and the Rule-Based schema, show a loading look
while they are on their way and a failure look when they fail, never an empty list that reads as "no Features" and
never an earlier opening's or another Portfolio's answer. Add and edit mode.

## IN scope
- Feature list (`featureService.getFeaturesByIds`, `DeliveryCreateModal.tsx:546-554`): the grid's loading overlay until
  this opening's answer lands; on failure the MD-29 notice in place of the grid, no Try again button (MD-23), no empty
  grid. Today a failure only logs, and `allFeatures` is not reset in `resetForm` (`:713-729`), so a reopen shows the
  previous opening's list until the new answer lands.
- Rule schema (`getRuleSchema`, `:569-592`): today's `LoadingSpinner` while it is on its way, never the failure look
  before the first answer (today `SchemaLoadError` shows whenever the schema is null and not loading, `:227-232`);
  today's `SchemaLoadError` on failure, asked once per opening (MD-30, answering N3: today's guard likely re-asks after
  each failure); reopening asks again. Still requested only once the licence is known premium.
- Both keyed per Portfolio and opening (keyed TanStack queries, the house pattern), so a late answer from a closed
  opening or another Portfolio is dropped.
- Edit mode: editing a Manual or Rule-Based Delivery shows the loading look while its list or schema loads, and Save
  stays disabled until it has answered (DDD-4).
- A Portfolio with no Features keeps its true empty grid at once (no request, as today).

## OUT of scope
- `components/Common/DataGrid/DataGridBase.tsx` and `hooks/useLicenseRestrictions.ts` (MD-12): the Manual grid renders
  through `FeatureSelector` → `FeatureGrid` → `DataGridBase`, which reads the licence hook; the two upper components
  only forward a `loading` prop, and these two files stay untouched.
- A Try again button (MD-23).
- Making the Feature list or the schema faster.
- The Release list, sources and licence looks (slices 01 and 02).

## Looks (decided, MD-29 and MD-30; no longer a sketch point)
- Feature list, add and edit (MD-29): loading is the grid's own existing loading overlay through `DataGridBase`'s
  `loading` prop. `FeatureSelector` and `FeatureGrid` gain an **additive, optional `loading` prop** that they pass
  straight down (they do not forward it today); `DataGridBase.tsx` is untouched. Failed: an amber `warning` `Alert` in
  place of the grid, no button: "Couldn't load this {Portfolio}'s {Features}. Close this dialog and open it again to
  retry."
- Rule-Based fields (MD-30): loading is today's `LoadingSpinner`; failed is today's `SchemaLoadError`, copy unchanged;
  asked once per opening, no loop of re-asks; reopening is the retry.

## Learning hypothesis
"An empty or stale Feature list in the dialog is a real way to build a wrong Delivery, and loading and failure looks
remove it."
- **Measure and window** (NF-11): K6 = 0 in the slice's Vitest scenarios (held, failed and late answers; reopen;
  Portfolio switch), each of which fails against today's code; and no reporter or user feedback about an empty, wrong
  or stale Feature list in the dialog within 2 weeks of the release.
- **Disproved** if a K6 scenario still fails at release, or such feedback arrives within those 2 weeks (the fix missed
  a path). **Not confirmed either** if the scenarios pass against today's code too (then today's bug was not where the
  slice looked).

## Acceptance criteria
US-04 AC-4.1 to AC-4.6 in `feature-delta.md` (copy of AC-4.3 and AC-4.4 from MD-29 and MD-30).

## Dependencies
Technically independent of slices 01–03; ordered third, after 02 and before 03, because it edits the same component as
01 and 02. **It shares the `resetForm` region of `DeliveryCreateModal.tsx` (`:713-729`) with slice 02, which lands
first** (NF-12): slice 04 rebases onto 02's version of that region rather than editing the old one. No file overlap with
Epic 6400 (AC-2.6's named list applies).

## Effort
About 1 day (NF-05). Reference class: two fetches moved to keyed queries with their looks, plus Vitest scenarios with
held and failed answers. **If it overruns, split it**: 04a the `FeatureSelector`/`FeatureGrid` `loading` prop and the
Feature list (loading, failure, keys, edit mode) first; 04b the rule schema (looks, once per opening, keys) second. Each
half ships alone.

## Dogfood
On the dev instance (`:5169`) throttled to "Slow 4G", open Add Delivery on a Portfolio with many Features and see the
loading look, then the grid. Block `features/ids` in DevTools and see the failure look, not an empty grid; unblock,
reopen, and the list loads. Close the dialog before the list lands and reopen: never the earlier list. On Rule-Based,
block the schema route: one failure look, no repeated requests in the waterfall.
