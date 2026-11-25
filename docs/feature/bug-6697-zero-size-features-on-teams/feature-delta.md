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

## Whole-candidate review: consequences decided in AFK (maintainer may revisit)

The review accepted the candidate with four non-blocking findings. Decisions taken:

1. **A Team's share of 0 still makes a 0/0 row: left as it is, follow-up.** `SplitIntoBuckets` gives a
   size smaller than the Team count a 0 share for some Teams (size 1 over 2 Teams is 1 and 0), and that
   Team still lists the Feature. Same confusion, but outside the agreed trigger (the Feature's size is 0),
   so it is a follow-up for the maintainer, not part of this fix.
2. **A Portfolio whose Features are all 0-size no longer shows on a Team's page.** A Team's Portfolios
   come from the Features' work rows (`Portfolio.Teams`). With no rows, the Team has no link to that
   Portfolio. This follows from "not on any Team" and is accepted.
3. **Fallback split no longer depends on Feature order.** The Teams of a Portfolio with no real child work
   used to grow while the default-size pass ran, because each 0-size Feature added rows. A later sized
   Feature could then split over a Team that a 0-size Feature with a Feature Owner value had named. It now
   splits over the Portfolio's own Teams or, failing that, all Teams, as the rule says. Accepted, no new case.
4. **The "No teams available" warning is not logged for a 0-size Feature.** There is nothing to assign,
   so the warning would be noise. Accepted.

## Context map and usage data

- Contexts: Work Items (default sizing) only. No new term, concept or domain event. No UI surface changes.
- Usage-data event: N/A, because this is a bug fix of existing behaviour, not a new capability to count.
