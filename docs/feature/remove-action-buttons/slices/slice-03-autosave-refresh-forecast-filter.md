# Slice 03 — Auto-save + auto-refresh Forecast Filter (D-RELOAD, premium)

Story: US-5077-03 | job: job-commit-intent-no-button | Must Have | Premium

## Goal
Editing forecast filter rules auto-saves AND offers a one-click throughput reload; the
`forecast-filter-takeeffect-hint` Alert (commit 53e6287e) is removed — closing the convergence
loop the user called out.

## IN scope
- Forecast-filter rule-set auto-save via Slice-1 mechanism.
- **D-RELOAD (expensive → one-click):** in-place "Reload throughput now" action embedded in the
  alert (NOT silent auto-refresh, because the throughput recompute is expensive).
- Remove the `forecast-filter-takeeffect-hint` Alert (`ForecastSettingsComponent.tsx` 100-109).
- Preserve premium gating + `isTeamAdmin(teamId)` read-only gating.

## OUT of scope
- Portfolio (Slice 4), forecasts (5/6).

## Learning hypothesis
**Confirms** the convergence the convergence-note predicted: once auto-save exists, the stopgap
hint is pure deletion, and a one-click reload replaces the two-step dance for the expensive surface.
**Disproves** the one-click-reload choice if users expect the throughput to update silently like
mappings — then reconsider auto vs one-click for this surface (cost permitting).

## Acceptance criteria
- [ ] Valid filter-rule edit auto-saves (debounced), no Save button.
- [ ] In-place alert carries one-click "Reload throughput now"; no "go elsewhere" instruction.
- [ ] Clicking it recomputes throughput chart + forecasts against the filter.
- [ ] `forecast-filter-takeeffect-hint` Alert removed (grep + test confirm absence).
- [ ] Non-team-admin: editor read-only, no auto-save (RBAC parity).
- [ ] Invalid rule (unknown field key) rejected with inline error; prior rule set intact.

## Dependencies
Slice 1 (auto-save mechanism). Builds the throughput-reload wiring Slice 2 echoes.

## Effort estimate
~1 day.

## Reference class
`ForecastFilterEditor.tsx` (readOnly gate line 70), `ForecastSettingsComponent.tsx` (Alert removed);
`filter-forecast-throughput` journey `step-configure-filter-rules` (PUT + Saved toast → auto-save +
one-click reload). D-RELOAD expensive analog: any "apply changes" action behind an explicit click.

## Cross-cutting
- RBAC: `isTeamAdmin(teamId)` read-only gate; suppress auto-save identically (err-non-team-admin-attempts-edit). No new authz.
- Clients: N/A (no API change; reuses `PUT /api/teams/{id}` for the rule-set field).
- Website: N/A (premium flow already marketed; polish only).
- License downgrade preserves persisted rule set (err-license-downgrade) — unchanged.

## Dogfood moment
Add an "Exclude Type = Bug" rule on the Atlas team (premium-seeded local instance); confirm
throughput chart + forecasts reflect it via one click, with no save/refresh ceremony and no hint.
