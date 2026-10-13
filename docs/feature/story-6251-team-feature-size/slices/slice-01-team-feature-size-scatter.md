# Slice 01: The Feature Size scatter on the Team

**Goal:** a Team's Portfolio & Features category shows the Feature Size scatter, sized by the Team's share of
each Feature.

## IN scope
- First, its own `refactor` commit: lift `WasClosedBetween` and the size-chart window rule from the Portfolio service
  into `BaseMetricsService`, with the Portfolio's numbers unchanged (DDD-3).
- Team routes `allFeaturesForSizeChart` and `sizePercentiles`, mirroring the Portfolio's. They are not cached (DDD-5). They return
  Features under D2 (tracked, with this Team's share above zero, not default-sized, excluded explicitly per DDD-4),
  with Done Features closed in the range plus every open Feature (D6). `sizePercentiles` moves here from slice 02,
  because the scatter's percentile lines and status footer read it.
- The share (D1) as the plotted size: `FeatureDto.Size` set from `Feature.SizeFor(teamId)` (DDD-1, DDD-2, ADR-236).
- `IFeatureSizeMetricsService` with its implementations in the shared frontend metrics base, a `providesFeatureSize` predicate in
  `useMetricsData.ts`, and the `featureSize` portfolio-only gate lifted in `categoryMetadata.ts` (DDD-8, DDD-9).
- Cycle Time and Closed Date modes only. No Estimation mode (D8). There is no size-target line to remove (DDD-14).
- "Counts this Team's Work Items only" label and tooltip wording, from the DISTILL sketch (D5).
- **The scatter frame is always placed, on the Team and on the Portfolio** (DDD-10, Changed Assumptions): a lone
  spinner while loading, the could-not-load note on failure, and an empty state when there are no Features. The Team's
  empty copy says Features come from a Portfolio (AC-1.6). The Portfolio's says there are no Features in the
  selected range. The copy for both comes from the DISTILL sketch.

## OUT of scope
- The percentiles widget and its status (slice 02).
- Feature Size PBC and the over-time charts on the Team.
- Tracking parents no Portfolio loads.

## Learning hypothesis
- **Disproves** "a Team's share reads as ours" if, shown OE-002 on Gravity and then on the Ocean Explorer
  Portfolio, the maintainer or Steve reads the smaller number as a bug despite the label.
- **Confirms** it if they read it as Gravity's part without being told, so the copy can carry over to slice 02.

## Acceptance criteria
AC-1.1 … AC-1.8 in `../feature-delta.md` (US-01). Per "Wave: DESIGN / [REF] Changed Assumptions", AC-1.6 and AC-1.8 also
hold on the Portfolio, with its own empty copy.

## Dependencies
Bug #6203 is on `main`. Slice 00 (owner-scoped metrics cache) ships first.

## Effort
About 7–8 hours: the refactor commit, `SizeFor`, two endpoints, the frontend interface and gate lift, the
always-placed frame with two empty states, labels and docs-ready copy.
Reference class: Story 6248 (an existing widget extended into a new context).

## Dogfood
On demo data, open Gravity, Zenith and Equinox and compare OE-002's point on each with its size on Project Ocean
Explorer. Real history doesn't help here: the dev instance (`:5169`) has one Team, so it has no multi-team Feature.
