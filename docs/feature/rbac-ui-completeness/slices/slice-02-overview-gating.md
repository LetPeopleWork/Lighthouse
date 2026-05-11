# Slice 02 — Overview Row Actions and Onboarding

**Feature**: rbac-ui-completeness
**Stories**: US-02 (Overview row actions), US-03 (OnboardingStepper), US-08 (Add Team/Portfolio)
**Estimate**: ≤6 hours

## Goal (one sentence)

In the Overview dashboard, hide management controls (Edit, Clone, Delete per row; Add Team/Portfolio; OnboardingStepper) from users who do not own the entity or are not System Admin.

## IN scope

- `DataOverviewTable.tsx`: per-row conditional rendering of Edit, Clone, Delete icons; reads `useRbac()` and `api` prop to pick the right admin predicate (`isTeamAdmin(row.id)` / `isPortfolioAdmin(row.id)` for Edit/Delete; `isSystemAdmin` for Clone and for Connections-api Edit/Delete)
- `OverviewDashboard.tsx:359`: change OnboardingStepper render condition to `rbac.isSystemAdmin`
- `OverviewDashboard.tsx:400,430`: change Add Team / Add Portfolio gates to `rbac.isSystemAdmin`
- Update existing `OverviewDashboard.test.tsx:652` test rule to match new behavior
- Add a Vitest case for `DataOverviewTable` showing/hiding icons by role
- E2E: extend `@rbac` test to assert TeamReader sees only Details icon on Team Zenith row; TeamAdmin sees Edit + Delete but no Clone on Team Zenith; sees only Details on other team rows

## OUT scope

- Connections section of Overview — already sysadmin-gated at the section level (`OverviewDashboard.tsx:482`); the connections DataOverviewTable invocation is unreachable for non-sysadmins. Defensive per-row gate is a 5-line bonus but not the main scope.
- Backend `RbacGuardRequirement.CanCreateTeam` — explicitly deferred per D8.

## Learning hypothesis

Disproves: that scoped admins want to act on entities they do not own. (If users push back: "I'm a Team Admin for Team A, but I want a 'Request access to Team B' button" — that's a separate feature, not a gate change.)
Confirms: that per-row admin gating in a generic table component is feasible without coupling the table to the RBAC model (the call site passes `rbac` or a predicate function).

## Acceptance criteria (from feature-delta US-02, US-03, US-08)

- [ ] `DataOverviewTable` Edit icon hidden when user is not admin for that row's entity
- [ ] Delete icon hidden when user is not admin for that row's entity
- [ ] Clone icon hidden when user is not SystemAdmin
- [ ] Details icon visible to all
- [ ] OnboardingStepper only renders when `rbac.isSystemAdmin === true`
- [ ] Add Team / Add Portfolio buttons only render when `rbac.isSystemAdmin === true`
- [ ] No regression on non-RBAC deployments (`isRbacEnabled === false` → `PERMISSIVE_SUMMARY` keeps everything visible)
- [ ] E2E asserts TeamReader, TeamAdmin, PortfolioReader, PortfolioAdmin, and SystemAdmin all see the correct subset on the Overview

## Dependencies

Slice 01 (Slice 01 doesn't strictly block, but if shipped first, the E2E setup will be cleaner — Slice 01's tests prove the gate plumbing works).

## Pre-slice spike

Not required. `useRbac()` exposure to `DataOverviewTable` is straightforward (one hook call inside the component, or pass `rbac` as a prop from the call site if we want to keep the component agnostic).

## Production-data acceptance

E2E uses real seeded "Team Zenith" / "Copy of Team Zenith" / "Project Apollo" entities. Real Keycloak users.

## Dogfood

Maintainer logs in as each of the four scoped users + sysadmin via local Keycloak; verifies the row actions and onboarding stepper visibility. Same-day.

## Effort estimate

~3-4 hours implementation (DataOverviewTable is generic — need to pass per-row admin predicate cleanly) + 1 hour unit tests + 1 hour E2E. Bigger than slice 01 because of the table genericity.

## Reference class

Comparable to rbac-enhancements step 03-05 (commit `aa9b4eb8` — viewer-clean Overview, +253/-113 lines). Similar diff size expected.
