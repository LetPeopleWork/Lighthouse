# Slice 01 — Faster Updates without a switch

**Goal:** Every refresh a connector can make cheap is cheap, and the *Faster Updates* row is gone from
Settings → System, including on instances that had it switched off.

## IN scope
- `OptionalFeatureSeeder`: `DeltaSyncKey` added to the deprecated keys, removed from the seeded list.
- `SyncModeResolver.Resolve`: the opt-in parameter and its branch removed.
- `WorkItemService`: `TheOperatorAskedForTheCheaperRefresh()` and the `IRepository<OptionalFeature>` dependency removed; the team, portfolio and parent-Feature fetch deciders always attempt the scan.
- The test rework listed in `feature-delta.md` → "Test Surface Handed to DISTILL".
- `ARCHITECTURE.md` §Background refresh and `docs/settings/configuration.md` "Faster Updates".

## OUT scope
- Any kill switch, config key or startup warning (D1, D2).
- ServiceNow / Linear delta.
- The update log line, `SyncOutcome`, the removal rule, staleness evaluation, the fetch fingerprint.
- `optionalfeatures.png` regeneration (finalization).

## Learning hypothesis
Confirms: nothing in the refresh depended on the flag except the one branch it controlled. The Epic #5687
scenarios stay green once their opt-in step is removed.
Disproves the slicing if: a scenario that used to pass *only because* it opted in turns red after the step
is removed. That would mean a second reader of the flag exists somewhere, or the default-on seed was
covering for a path that never scans.

## Acceptance criteria
US-01, AC-1.1 … AC-1.6 (`feature-delta.md`). Production-data check: start the dev instance on this build,
confirm the Settings → System list has no *Faster Updates* row, and read the next `Update completed` line for
a Jira or Azure DevOps team as `mode=Delta`.

## Dependencies
Epic #5687 shipped. No other story in flight touches the seeder or `SyncModeResolver`.

## Effort
Under a day. Production change is small; most of the effort is removing or rewording the opt-in scenarios.

## Dogfood moment
Same day: restart the local dev instance on the new build and read the log.
