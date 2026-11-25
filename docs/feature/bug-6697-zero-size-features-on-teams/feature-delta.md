# Bug #6697: Features with a default size of 0 show on every team's Features tab

Reported by Steve Pereira. Route: `/nw-bugfix` (nwdes M/bugfix route, no lane), run in AFK mode.

## Root cause

`WorkItemService.AssignExtrapolatedWorkToTeams` gives every open Feature without child Work Items a
`FeatureWork` row per owning team, sized by its estimate or the Portfolio's default. When that size is 0 it
still writes a 0/0 row. With no child Work Items, the Portfolio has no teams of its own, so the fallback is
**every** team. A Team's Features tab lists any Feature with a row for it (`FeatureRepository.FeaturesOfTeam`),
so the Feature shows on every team.

## Maintainer decisions (2026-11-25, before AFK)

1. **Trigger:** only when the size Lighthouse gives a Feature without child Work Items works out to 0 (no
   estimate above 0, and a default of 0). A Feature with an estimate above 0 still goes to its team(s).
2. **Portfolio look:** unchanged. Same 0/0 bar and default-size warning; the Feature stays marked as sized by
   the default (`IsUsingDefaultFeatureSize`), so Delivery metrics that count default-sized Features do not move.
   No UI change, so no sketch.
3. **Named owners:** no exception. Size 0 means no team row for the fallback teams, the Portfolio's Owning
   Team, or teams named in the Feature Owner field.

## Rigor profile

`standard` for the one slice: crafter and acceptance designer on `sonnet`, reviewers and examiner on `haiku`.

## AFK session options

Slice wrap-up: refactor + review (mutation once, in the final gates). Push: per slice, autonomous, with
`/clean-ci` after the push; rebasing this run's own unpushed commits onto a moved `origin/main` is allowed.

## Context map and usage data

- Contexts: Work Items (default sizing) only. No new term, concept or domain event. No UI surface changes.
- Usage-data event: N/A, because this is a bug fix of existing behaviour, not a new capability to count.
