# Slice 01: Time-in-State MVP

**Feature**: time-in-state-and-staleness
**Stories shipped**: US-01
**Estimate**: ~3–4 crafter days
**Reference class**: similar to RBAC EXTEND tasks (extends existing read endpoints + adds a column)

## Goal
Ship the per-item "Time in State" badge end-to-end for Jira and ADO teams. Validate that the source-of-truth transition capture matches Jira/ADO history closely enough that flow coaches trust the number.

## IN scope
- New persistence: `WorkItemStateTransition` rows (at minimum: `workItemId`, `fromState`, `toState`, `transitionedAt`, `source` = `"jira" | "ado" | "linear" | "csv-fallback"`).
- Jira connector: extend the existing transition-reading logic (currently used for Started/Closed dates) to capture all state changes.
- ADO connector: same extension.
- Read API: include `currentStateEnteredAt: ISO8601` per work item in `GET /api/teams/{teamId}/work-items`.
- UI: "Time in State" column in `TeamDetail` work-item table, format `<N>d in <stateName>`, sortable.

## OUT scope (deferred to later slices in this feature, or to later features)
- Staleness threshold + red colour treatment → slice 02
- Linear connector → slice 02 if DESIGN confirms cheap; else its own slice
- CSV fallback (sync-side delta) → slice 02 alongside Linear
- Portfolio work-item view column → slice 02 (keeps Team-only dependency clear in slice 01)
- Historical breakdown, cumulative views, pace percentiles → future features (Epic 4144 slices B2, B3, F)

## Learning hypothesis
**Confirms if it succeeds**: flow coaches use the badge to identify items for follow-up; source-system transition history matches our derived `currentStateEnteredAt` within 1 day on real fixtures.
**Disproves if it fails**: either the source history doesn't expose enough granularity (force fallback to be primary), or the bare badge isn't actionable without threshold colour (force slices 01 + 02 to merge into one shippable unit).

## Acceptance criteria
See US-01 in `../feature-delta.md`. Acceptance fixture: at least one Jira issue and one ADO work item with ≥3 state transitions on record; integration test asserts derived `currentStateEnteredAt` equals the last transition timestamp from the recorded API response.

## Dependencies
None. Foundation slice — nothing in this feature depends on anything else from this feature.

## Production data requirement
**Required.** No synthetic-only acceptance. DEVOPS smoke against at least one real Jira instance and one real ADO instance.

## Dogfood moment
The project's own Lighthouse ADO team (the one currently feeding the development instance) sees the "Time in State" column populated within one sync cycle of deploy. Tester observes a known stuck item (`Active` > 5 days) reflected with correct count.

## Pre-slice spike candidates
- Jira: confirm the transition-reading code path picks up all state changes vs. just first/last. (~1 hr)
- ADO: same confirmation. (~1 hr)
- Decide whether `WorkItemStateTransition` is a flat table or part of an existing `WorkItem` history relation. (~30 min)
