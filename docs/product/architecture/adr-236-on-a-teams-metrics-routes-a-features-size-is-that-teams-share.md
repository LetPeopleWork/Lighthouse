# ADR-236: On a Team's Metrics Routes, a Feature's Size Is That Team's Share

**Status**: Accepted (maintainer, 2026-10-13); decisions 5-6 and the AFK-6/AFK-8/AFK-9 consequences pending ratification (amended 2026-10-13, review L-R3-1)
**Date**: 2026-10-13
**Feature**: story-6251-team-feature-size (ADO User Story #6251)
**Decider**: Morgan (Solution Architect), interaction mode = PROPOSE. The maintainer confirmed it on 2026-10-13. That the
Team counts its own share, not the whole Feature, was decided by the maintainer in DISCUSS (D1).

---

## Context

A Feature's size is the number of its child Work Items across every Team (`Feature.Size`, the sum of
`FeatureWork.TotalWorkItems`, or 0 when the Feature uses its Portfolio's Default Feature Size). The Feature Size
scatter, its status rule and the Work Items dialog it opens all read the `size` of a `FeatureDto`. The percentiles are
computed on the server from the same sizes.

Story #6251 brings the scatter and the percentiles widget to the Team. There, a Feature's size is that Team's own
share: the number of its child Work Items that belong to the Team, already stored per Team in `FeatureWork`. The
question is how that number reaches the client. `FeatureDto` is a shared contract, built in 7 production places, and
`Size` means the whole Feature everywhere today.

## Decision

1. **One definition.** A pure method on the Feature, `SizeFor(teamId)`, returns this Team's share: 0 when the Feature
   uses a Default Feature Size, and otherwise the sum of `TotalWorkItems` over this Team's `FeatureWork` rows. The Team's
   scatter values and its percentiles both use it.
2. **The share travels in `Size`, on every Team metrics route that returns a Feature.** `FeatureDto.Size` becomes
   init-only. Every Team metrics route that returns a Feature sets `Size = feature.SizeFor(team.Id)` in the object
   initializer, the way `Estimate` is set. Two routes return Features: the new `allFeaturesForSizeChart` and the
   existing `featuresInProgress`, whose "Features worked on" dialog offers a Size column. The two new percentile routes
   return numbers, which are computed from `SizeFor` too. There is no exception. Every other route leaves `Size` at the whole Feature's size,
   so its meaning is decided by whose metrics page asked. (amended 2026-10-13: widened from the size-chart route to
   every Team route, so the Team page never shows two different sizes for one Feature; review round, AFK-5; "today"
   dropped from "two routes return Features", because the first of them is new, review F-8)
3. **Default-sized Features never reach the Team.** Their `FeatureWork` rows on a Team are made up by extrapolation,
   not counted. The Team's query excludes them explicitly, and `SizeFor` returns 0 for them as a second guard.
4. **The UI says whose number it is.** On a Team, the scatter's size axis and tooltip name the size as this Team's
   Work Items, and the percentiles widget says so in its info description (maintainer's sketches at DISTILL).
5. **The routes name only the Portfolios the reader may read.** A Feature from a Portfolio the reader cannot read
   still appears (`TeamRead` is enough to see it, DISCUSS D4), but that Portfolio is not named. (added 2026-10-13, AFK-6, pending
   maintainer ratification; `featuresInProgress` keeps today's naming as a follow-up)
6. **Where the routes live and what else they fill.** (added 2026-10-13, review round 2, F-1/F-4/F-11/F-14) The three new
   routes sit in a new controller under the same route prefix as `TeamMetricsController`, with the same class-level
   attribute, exactly `[RbacGuard(RbacGuardRequirement.TeamRead, ScopeIdRouteKey = "teamId")]`. A reader without
   `TeamRead` gets 404. The controller builds each `FeatureDto` the way `featuresInProgress` does: the same forecast
   window and blackout days (`FeatureForecastWindow.EndFor`), and the same Blocked rule (blocked if any of the Feature's
   Portfolios counts it blocked). The other Teams on a Feature stay on it, as on `featuresInProgress` (AFK-9):
   `OwningTeam`, `TeamsWithoutForecast`, `TeamForecasts`, and `RemainingWork` / `TotalWork` keyed by every Team on
   the Feature are filled exactly as there. Only `Projects` is narrowed to the readable Portfolios, and `Size` carries
   the share.

## Alternatives considered

- **A field of its own (`TeamShare`) or a Team-specific Feature DTO.** Rejected. The scatter, the status input, the
  dialog's Size column and the view-data would each have to choose a field per owner, which means about five frontend
  places to keep in step. A separate DTO would copy about twenty properties.
- **Compute it in the browser from `totalWork[teamId]`.** Rejected. The server still has to select the Features and
  compute the percentiles from the share, so the share would be defined twice, once in each stack, and the endpoint
  could not be tested for the number the chart plots.
- **Send the whole size unchanged.** Rejected by D1. The Team's question is how much of a Feature it takes on, and
  the whole size counts other Teams' Work Items too.
- **Share on the new routes only, whole size on `featuresInProgress`.** Rejected in the review round. The same Team
  page would show two sizes for one Feature in two dialogs.

## Consequences

- Positive: no change to the scatter, the status rule or the dialog. One method defines the share, and the scatter
  and percentiles cannot disagree.
- Positive: the JSON stays the same shape. Lighthouse-Clients do not call the new routes. Any client that
  reads `featuresInProgress` will now get the Team's share in `Size`.
- Negative: `Size` on a `FeatureDto` now depends on the route. A consumer that merges Feature rows from a Team route
  with rows from a Portfolio or feature-list route would mix two meanings. The rule is simple to state: on a Team
  route, `Size` is always the Team's share.
- Negative: the share changes when a Portfolio refreshes, and the Team's metrics page keys its queries on the Team's
  last update. An open page shows the old share until it is remounted, or its window or Team changes. Focusing the
  window does not refetch. This is accepted and stated in the docs.
- Negative: `IsUsingDefaultFeatureSize` is a property of the Feature, not of the Portfolio. Each Portfolio's refresh
  resets it to false, then sets it to true if that Portfolio extrapolates the Feature (`Lighthouse.Backend/Services/Implementation/WorkItems/WorkItemService.cs:576-635`; full path amended, review L-R3-2, 2026-10-13).
  For a Feature in two Portfolios that size it differently, the value from the last Portfolio to refresh wins. So
  whether such a Feature counts on a Team can change with refresh order. Today the Portfolio's own percentiles have
  the same limitation. It is accepted, not fixed here. (added 2026-10-13, review L-3)
- Negative: a `TeamRead` reader learns which other Teams work on a Feature their Team works on, with those Teams'
  forecasts and work totals (decision 6, AFK-9). That matches `featuresInProgress` today, and blanking them would leave
  the dialog's Forecasts column partly empty. Pending maintainer ratification. (added 2026-10-13, review F-4)
- Negative: the Blocked flag can come from a Portfolio the reader may not read, since it is "blocked in any of its
  Portfolios", as on `featuresInProgress`. It says nothing about which Portfolio. Accepted under AFK-6 and AFK-9,
  pending maintainer ratification. (added 2026-10-13, review F-14)
- Negative: two rules for Features from unreadable Portfolios now sit side by side. The feature list
  (`FeaturesController`) hides a Feature none of whose Portfolios the reader may read (`FeatureReadability.IsReadableBy`).
  The Team routes show it, because DISCUSS D4 makes `TeamRead` enough, and only drop the Portfolio's name. A reader with
  `TeamRead` alone can therefore see a Feature on the Team page that the feature list does not show them. (added
  2026-10-13, review F-5)
- Negative: `featuresInProgress` still names every Portfolio of a Feature, readable or not, while the three new routes
  name only readable ones. One Team page therefore names Portfolios in one dialog that it withholds in another. This is
  a known gap, recorded as a follow-up, not fixed here. (added 2026-10-13, review F-6)
- Cross-refs: ADR-233 (the loading and could-not-load looks the widgets reuse), ADR-235 (the init-only property idiom
  on the same DTO).
