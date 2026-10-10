# Slice 03: A Feature's child items show their progress at a glance

**Goal:** the child-items dialog (Delivery section, Team and Portfolio feature lists) shows Started, Closed,
Age / Cycle Time and Estimate; the Delivery timeline keeps Warnings and gains the catalogue.

## IN scope
- Defaults-map entries for the `featureChildren` and `deliveryTimeline` contexts (D4).
- Estimate on child work items from each item's own Team (DDD-4, already on the row since 01a).
- The child items fetched through a TanStack query keyed by Feature id in `DeliverySection`, `TeamFeatureList` and
  `PortfolioFeatureList`, with the loading look (S6) and could-not-load message (S5) (DDD-14, AC-3.4).
- `FeatureSchema` adds `currentStateEnteredAt` only (DDD-12, R2), so Time in State can fill on the timeline.
- `highlightColumn` deleted from the dialog's props, so a forgotten caller fails `tsc -b`.

## OUT of scope
- The feature list grids themselves; only the dialog they open.
- Estimate (M3) and Blocked since (R2) on the Delivery timeline.

## Learning hypothesis
- **Disproves** "child items carry enough to explain progress" if the delivery lead still opens the tracker to
  see what is moving.
- **Confirms** it if the not-started large item is visible from the dialog alone.

## Acceptance criteria
AC-3.1 to AC-3.4 in `../feature-delta.md` (US-03).

## Dependencies
Slice 01b (01a for the estimate values).

## Effort
About 1 to 1.5 days: three callers move to a keyed query with two looks, plus the schema field and the prop removal.

## Dogfood
Demo data, Ocean Explorer Portfolio, Deliveries: a Feature's progress opens its child items with dates and age (and
an Estimate on any child item of Team Zenith). With the browser throttled, the dialog shows its loading look, not "No items".
