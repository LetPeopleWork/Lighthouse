# Slice 01: The Feature Size scatter on the Team

**Goal:** a Team's Portfolio & Features category shows the Feature Size scatter, sized by the Team's share of
each Feature.

## IN scope
- First, characterisation tests over the Portfolio's closed-in-range outputs and the Team's closed Work Items, green
  on today's code. Then its own `refactor` commit: lift **one** closed-in-range helper into `BaseMetricsService`,
  delete both private `WasClosedBetween` copies (Portfolio and Team), and lift the size-chart window rule. The
  Portfolio's numbers stay unchanged (DDD-3). (amended: one helper and tests first, 2026-10-13)
- Team routes `allFeaturesForSizeChart` and `sizePercentiles`, mirroring the Portfolio's, in a new Team Feature Size
  controller with the same route prefix and guard, exactly `[RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]`
  (DDD-7; amended: attribute written out, review F-1, 2026-10-13). Each `FeatureDto` is filled as `featuresInProgress`
  fills it: the same forecast window and blackout days, the same Blocked rule, and the other Teams' fields (AFK-9;
  added: review F-4/F-11/F-14, 2026-10-13). They are not cached (DDD-5), and they name only
  the Portfolios the reader may read (AFK-6). They return Features under D2 (tracked, with this Team's share above
  zero, not default-sized, excluded explicitly per DDD-4), with Done Features closed in the range plus every open
  Feature (D6). `sizePercentiles` moves here from slice 02, because the scatter's percentile lines and status footer
  read it. A query-count budget test covers both routes.
- The share (D1) as the plotted size: `FeatureDto.Size` set from `Feature.SizeFor(teamId)` (DDD-1, DDD-2, ADR-236).
  `featuresInProgress` sets it too, so the "Features worked on" dialog's Size column shows the same share (AC-1.9,
  AFK-5). (added 2026-10-13)
- `IFeatureSizeMetricsService` with its implementations in the shared frontend metrics base, a `providesFeatureSize` predicate in
  `useMetricsData.ts`, and the `featureSize` portfolio-only gate lifted in `categoryMetadata.ts` (DDD-8, DDD-9).
- Cycle Time and Closed Date modes only. No Estimation mode (D8). There is no size-target line to remove (DDD-14).
- Team axis label `Size (this {Team}'s {Work Items})`, tooltip `{n} {Features} with {size} of this {Team}'s {Work Items}`,
  the matching screen-reader label (UI-1, AFK-2) and the Team info description for the scatter. (amended: copy from UI-1, 2026-10-13)
- **The scatter frame is always placed, on the Team and on the Portfolio** (DDD-10, Changed Assumptions): a lone
  spinner while loading, the could-not-load note on failure, and `No {Features} to show.` when there are no
  Features, **the same on both owners** (UI-2). The chart renders it itself. (amended: was a Team copy saying Features
  come from a Portfolio and a Portfolio copy about the selected range, superseded by UI-2, 2026-10-13)
- DELIVER exit gates: one manual run of both routes on the Postgres provider, with the responses recorded (AFK-3),
  and a by-hand throttled-network check of the looks on both owners.
- The DISTILL scaffold in `Feature.SizeFor` (answers -1 under `// SCAFFOLD:`) is gone by the end of this slice:
  `grep -rn "SCAFFOLD:" Lighthouse.Backend/Lighthouse.Backend` is empty. (added: review L-1/F-13, 2026-10-13)

## OUT of scope
- The percentiles widget and its status (slice 02).
- Feature Size PBC and the over-time charts on the Team.
- Tracking parents no Portfolio loads.
- Naming only readable Portfolios on `featuresInProgress` (a follow-up, AFK-6; FU-1 in DESIGN).
- A Size column header that names the Team's share. An open copy question for the maintainer (added: review F-12, 2026-10-13).

## Learning hypothesis
- **Disproves** "a Team's share reads as ours" if, shown OE-002 on Gravity and then on the Ocean Explorer
  Portfolio, the maintainer or Steve reads the smaller number as a bug despite the label.
- **Confirms** it if they read it as Gravity's part without being told, so the copy can carry over to slice 02.

## Acceptance criteria
AC-1.1 … AC-1.10 in `../feature-delta.md` (US-01). AC-1.10 is the Portfolio's always-placed frame. (amended: AC-1.9
and AC-1.10 added, 2026-10-13)

(amended: the ACs are now spread over five stories after the DISCUSS split, review C-01, 2026-10-13)
- US-01: AC-1.1, AC-1.2, AC-1.9
- US-03: AC-1.6, AC-1.8, AC-1.10
- US-04: AC-1.7
- US-05: AC-1.3
- US-06: AC-1.4, AC-1.5

## Dependencies
Bug #6203 is on `main`. Slice 00 (owner-scoped metrics cache) is ordered first but is **not a dependency**: the
Feature Size routes are not cached, so this slice would work without it. (amended 2026-10-13)

## Effort
About 7–8 hours: the characterisation tests and refactor commit, `SizeFor`, two endpoints in a new controller, the
`featuresInProgress` share, the frontend interface and gate lift, the always-placed frame with its empty state, and
copy. Thicker than DISCUSS planned, and accepted.
Reference class: Story 6248 (an existing widget extended into a new context).

## Dogfood
On demo data (the "Dependencies" scenario), open Equinox, Gravity, Lightspeed and Zenith, and compare OE-002's point
on each with its size on Project Ocean Explorer: per the demo CSVs, 8 + 4 + 4 + 3 = 19. OE-001 (Gravity, Meridian,
Zenith), OE-007 (Gravity, Lightspeed, Zenith) and OE-008 (Gravity, Zenith) are the other multi-Team Features.
Real history doesn't help here: the dev instance (`:5169`) has one Team, so it has no multi-team Feature.
(amended: Lightspeed and the other Features added from the demo CSVs, 2026-10-13)
