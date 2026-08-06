# Slice 01 — A Features view, and the forecast order becomes visible

**Feature**: epic-5375-manual-sorting · **ADO**: Epic #5375 · **Story**: US-01 · **Estimate**: ~6h
**Reference class**: `PortfolioFeatureList.tsx` (same grid, same column factory, same terminology
resolution) — this slice is largely that component pointed at a wider, RBAC-filtered set.

## Goal

Anyone can open one place listing every Feature they have rights to, across all Portfolios, in the order
Lighthouse actually simulates — and read one line saying that this order is what moves the dates.

## IN scope

- Third top-level nav entry beside Overview and System Settings (`App/Header/Header.tsx:59-60`),
  labelled via `getTerm(TERMINOLOGY_KEYS.FEATURES)` (D16), routing to `/features` (`App.tsx`).
- New page reusing `FeatureListDataGrid` (S16) — not a new grid.
- `GET api/v1|latest/features` — ranked list filtered to Features in Portfolios the caller holds
  `PortfolioRead` on (D11). **Not** premium-gated (D12). A Feature in several such Portfolios appears
  once, showing all of them. DESIGN found the filter already exists: this is
  `FeaturesController.GetFeaturesByPredicate` (`:100`) with a true predicate, not new machinery.
- **`GetWritablePortfolioIdsAsync` on `IRbacAdministrationService`** (closes OQ-1, user decision
  2026-08-06). Mirrors `GetReadablePortfolioIdsAsync` (`RbacAdministrationService.cs:122-155`) with
  `HasPortfolioWritePermission` (`:1205`) swapped in. Resolves the writable set **once per request** so
  the per-row move verdict is not ~1000 permission checks at 500 rows. All four early-return branches
  carry over unchanged and each needs its own test — getting one wrong is silent over-permission on a
  write path, not a visible error. Widening `IRbacAdministrationService` is a shared-contract change:
  extend the test doubles first.
- Position column added to the shared `columns.tsx` factory, so it lands on the Features view **and** the
  existing Portfolio Feature list in one change (D10). Value is the Feature's global rank, supplied by
  the backend — an additive `rank` integer on `FeatureDto` — not computed from the row index.
- Help text: "Lighthouse forecasts Features in this order — the top of the list gets your teams'
  throughput first."
- Done Features hidden by default here (D15), via the existing `hideCompleted` mechanism.
- Demo data: enough Features across enough Portfolios that the view is not trivially short, plus one
  Feature shared between two Portfolios (needed by slice 03's AC-3.8).
- Vitest + backend tests for AC-1.1 … AC-1.9.

## OUT of scope

- Everything manual: no `ManualRank`, no switch, no migration, no move actions.
- Anything from Epic #4365 — no dependency column, no graph, no detail drawer. The only obligation is
  not to build a layout that would have to be thrown away for it.
- Search, grouping, saved views, virtualised scrolling.

## Learning hypothesis

**Disproves "a global ranked list of Features is worth showing"**, which the rest of this epic *and* the
surface Epic #4365 will land on both assume. Two independent ways it can fail:

1. **The order is noise.** ServiceNow maps `Order = recordNumber` (`ServiceNowWorkItemMapper.cs:123`)
   and the CSV connector's `order` column is optional (`CsvWorkTrackingConnector.cs:202`). If real
   instances carry mostly duplicate or blank values, `FeatureComparer` is resolving them by
   `string.Compare` on arbitrary text — the column reports nothing, D6 has nothing meaningful to seed
   *from*, and the epic's framing flips from *override the default sorting* to *supply an order that
   never existed*.
2. **The list is unusable at size.** If the dogfood instance's list is hundreds of rows with no grouping
   and finding two Features to compare costs more than any reorder saves, the view needs search or
   grouping *before* it needs actions, and slices 03/04 are re-planned behind that.

**Confirms**, if it holds, that D3's single global order is both correct for the forecast and legible to
the person maintaining it — the assumption the whole epic rests on.

## Verify the premise first (30 min, before writing the page)

Against the dev instance (`reference_dev_db_backup_restore`, port 5169, real recorded history) and any
available ServiceNow/CSV instance:

```sql
SELECT "Order", COUNT(*) FROM "Features" GROUP BY "Order" ORDER BY COUNT(*) DESC LIMIT 20;
SELECT COUNT(*) FILTER (WHERE "Order" IS NULL OR "Order" = '') * 100.0 / COUNT(*) FROM "Features";
SELECT COUNT(*) FROM "Features";              -- and the same excluding Done
```

Ties or blanks dominating → hypothesis 1. A few hundred non-Done rows → take hypothesis 2 seriously and
consider whether search lands in this slice rather than after it.

## Acceptance criteria

AC-1.1 … AC-1.9 verbatim from `feature-delta.md`. The three that carry the slice:

- The view lists Features from every Portfolio the user can read and **nothing else** (AC-1.2) — the
  RBAC result-set filter is the difference between this and an admin tool.
- It is reachable on a non-premium instance with manual sorting off (AC-1.3) — this is what makes it a
  general view rather than a sorting page.
- Two consecutive rows may read `4` and `17` (AC-1.5) — proof the number is the global forecast position,
  not a row index.

## Dependencies

None. Ships independently, on every instance, premium or not (D12).

## Dogfood moment

Same day: open the view on the dogfood instance and try to answer one real question — "is anything
important sitting below something unimportant?" Then pick the Feature with the worst 85% date and check
whether its position explains it. If either takes more than a minute, that is the hypothesis firing, and
it is worth writing down before slice 02 builds on this surface.

**Start the D7 clock here.** D7 accepts that new Features append silently — no badge, no notification —
with the stated revisit trigger "if the tail turns out to be where real work hides". That trigger needs
someone actually watching for it, or it is decoration. From this slice onward, note on each dogfood visit
whether Features that arrived since the last sort are being found and placed, or are accumulating
unsorted at the end of the list. Two weeks of accumulation on a real instance reopens D7 **before**
slice 03 ships the move actions that would otherwise be used to dig the tail out by hand.
