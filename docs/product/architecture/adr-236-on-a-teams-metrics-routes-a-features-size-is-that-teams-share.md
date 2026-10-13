# ADR-236: On a Team's Metrics Routes, a Feature's Size Is That Team's Share

**Status**: Accepted (maintainer, 2026-10-13)
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
2. **The share travels in `Size`.** `FeatureDto.Size` becomes init-only. The Team's `allFeaturesForSizeChart` action sets
   `Size = feature.SizeFor(team.Id)` in the object initializer, the way `Estimate` is set. Every other route leaves it at
   the whole Feature's size, so its meaning is decided by the route: whose metrics page asked.
3. **Default-sized Features never reach the Team.** Their `FeatureWork` rows on a Team are made up by extrapolation,
   not counted. The Team's query excludes them explicitly, and `SizeFor` returns 0 for them as a second guard.
4. **The UI says whose number it is.** The Team's scatter and percentiles widget state that they count this Team's
   Work Items only, and a point's tooltip names the size as this Team's share (copy pinned at DISTILL).

## Alternatives considered

- **A field of its own (`TeamShare`) or a Team-specific Feature DTO.** Rejected. The scatter, the status input, the
  dialog's Size column and the view-data would each have to choose a field per owner, which means about five frontend
  places to keep in step. A separate DTO would copy about twenty properties.
- **Compute it in the browser from `totalWork[teamId]`.** Rejected. The server still has to select the Features and
  compute the percentiles from the share, so the share would be defined twice, once in each stack, and the endpoint
  could not be tested for the number the chart plots.
- **Send the whole size unchanged.** Rejected by D1. The Team's question is how much of a Feature it takes on, and
  the whole size counts other Teams' Work Items too.

## Consequences

- Positive: no change to the scatter, the status rule or the dialog. One method defines the share, and the scatter
  and percentiles cannot disagree.
- Positive: the JSON stays the same shape. Lighthouse-Clients do not call these routes.
- Negative: `Size` on a `FeatureDto` now depends on the route. A future consumer that merges Feature rows from a Team
  route with rows from another route would mix two meanings. The Team routes are the only place this happens, and
  they are named after their owner.
- Negative: the share changes when a Portfolio refreshes, and the Team's metrics page keys its queries on the Team's
  last update, so an open page can lag until its queries go stale. This is accepted and stated in the docs.
- Cross-refs: ADR-233 (the loading and could-not-load looks the widgets reuse), ADR-235 (the init-only property idiom
  on the same DTO).
