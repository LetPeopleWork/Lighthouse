# Slice 01a: Every work-item row carries its estimate

**Goal:** every work-item and Feature row from the Team and Portfolio metrics endpoints, and a Feature's child items,
carries its estimate normalised by the code the Estimation chart uses, and demo data has an estimation field so the
chart and the later dialog can be seen on it. No dialog change yet. (Split from slice 01 in review round 1, R1.)

## IN scope
- `EstimateNormalizer` gains one `TryGetValue` reader and `EstimateOf(owner, item)` / `EstimatesOf(owner, items)`
  (DDD-2); both estimation chart builders move to `EstimatesOf` in a behaviour-preserving refactor commit first.
- `WorkItemEstimateDto(Value, DisplayValue, Unit)` with `For(owner, item)`; `WorkItemDto` gains an init-only
  `Estimate`, inherited by `FeatureDto` and both run-chart DTOs; no constructor changes (DDD-1).
- `Estimate` set at the 12 owner-known sites only (DDD-4): Team metrics with the route's Team, Portfolio metrics with
  the route's Portfolio, a Feature's child items with each item's own Team. The 4 other sites leave it `null`.
- Only estimates the chart can place get a display value; unmapped or invalid ones are empty (DDD-3, M1).
- Demo data (S7): Team Zenith's CSV gains a `Story Points` column (Fibonacci values), the demo CSV connection
  registers it as an additional field, and Team Zenith uses it as its estimation field with unit `Story Points`.
  Other demo Teams unchanged.
- The ArchUnit seam test: nothing outside `EstimateNormalizer` calls `Normalize` / `NormalizeBatch`.

## OUT of scope
- Any frontend change, including the `IWorkItem` type (slice 01b).
- An estimate on the 4 sites with no single owner (DDD-4, M3).
- Clients (D5, M5): the field is additive.

## Learning hypothesis
- **Disproves** "one normalisation path serves chart and row" if a row's estimate ever differs from its chart
  point's value in the integration fixture, or if a site needs an owner it cannot know.
- **Confirms** it if the row and the point agree for every plotted item, Team and Portfolio.

## Acceptance criteria
- Every item the Estimation chart plots has the same display and numeric value on its `cycleTimeData` row (D2).
- Where the owner has no estimation field, rows carry `"estimate": null` (AC-1.5, D6).
- A Feature's child items each carry their own Team's estimate (AC-3.1, DDD-4).
- On demo data, Team Zenith shows the Estimation vs. Cycle Time chart out of the box (S7).
- Scenarios: the backend files of slice 01a in "Wave: DISTILL / Scenario list with tags".

## Dependencies
None.

## Effort
About a day: the normaliser refactor, one DTO, 12 one-line wirings, the demo CSV and settings. No close reference
class; story 5884 (one column added to this dialog) is smaller.

## Dogfood
Demo data, Team Zenith: the Estimation vs. Cycle Time chart is shown, and `/api/latest/teams/{id}/metrics/cycleTimeData`
rows carry `estimate.displayValue` equal to the bubble each item sits in. Dev instance (`:5169`) with an estimation
field set on a Team: same check against real history.
