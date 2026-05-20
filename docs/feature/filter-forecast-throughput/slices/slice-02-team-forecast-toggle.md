# Slice 02: Team-Forecast Per-Run Filter Override

**Feature**: filter-forecast-throughput
**Stories shipped**: US-04, US-03 chip (extended to the Team Forecast surface)
**Estimate**: ~0.5 crafter day
**Reference class**: form-state toggle + service flag pass-through (well-trodden pattern in the existing forecast pages).

## Goal
Honour the Epic 4896 directive: "Forecast Throughput should be used for all feature forecasts, but for the Team Forecast, user may be able to chose." Give the user a per-run toggle on the Team Forecast (How Many / When) page so they can ask both questions — "honest feature pace" and "total team capacity" — from the same screen.

## IN scope
- Team Forecast page (`/teams/{teamId}/forecast/howmany` and `/when`): new toggle "Apply forecast-throughput filter" in the forecast input form.
  - Defaulted ON when the team has a non-empty filter configured.
  - Hidden when the team has no filter, or the tenant is non-premium.
- `POST /api/forecast/team/{teamId}/howmany` and `/when` accept optional `applyFilterOverride: bool` body field. Default behaviour (omitted/null): treat as `true` on premium tenants with a filter configured; `false` otherwise.
- Backend forecast service: when override is `false`, sample from UNFILTERED throughput regardless of the team's persisted rule set.
- Forecast result includes `filterApplied: bool` and `excludedSummary` so the UI can show or hide the chip per run.
- The US-03 chip (introduced in Slice 01 on Feature Forecasts) is now also rendered on the Team Forecast result view when `filterApplied: true`.

## OUT scope
- Toggle on Feature Forecasts → Feature Forecasts always use the filter (D3 locks this).
- Toggle on Portfolio-level forecasts → out of feature.
- "Choose which rules to skip per run" — out of scope; runtime choice is binary (apply / don't apply the persisted rule set as a whole).

## Learning hypothesis
**Confirms if it succeeds**: forecasters report flipping the toggle in the same session ("here's the honest feature dates, then here's what we could do if we did everything") via community feedback or interview within 30 days.
**Disproves if it fails**: toggle is never observed to leave its default → no user actually needs the per-run choice; cheap rollback (UI-only change; backend optional field becomes vestigial but harmless).

## Acceptance criteria
See US-04 in `../feature-delta.md` and the chip ACs in US-03.

## Dependencies
**Slice 01** — depends on the persisted rule set, the `IForecastFilterRuleService`, the chip pattern, and the premium gate.

## Production data requirement
**Required.** Smoke against the project's own Lighthouse team forecast page, both positions of the toggle, confirming percentile difference is observable.

## Dogfood moment
Configure a non-trivial rule set, open Team Forecast (How Many), toggle off → larger N (more items); toggle on → smaller N. Both numbers explainable to a stakeholder.

## Pre-slice spike candidates
None. Pattern is a direct extension of Slice 01's filter-application code.
