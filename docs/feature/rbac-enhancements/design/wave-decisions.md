# Wave Decisions: DESIGN — rbac-enhancements

Date: 2026-05-10
Architect: Morgan (Solution Architect)
Mode: Propose

---

## Summary

This document records all architectural decisions made during the DESIGN wave for the rbac-enhancements feature. Downstream waves (DISTILL, DEVOPS) should treat these as authoritative.

---

## Architectural Pattern

**Ports-and-Adapters (Hexagonal)** — existing pattern, extended (not changed). OOP paradigm on the backend (C#), functional-leaning React (hooks, pure components) on the frontend.

---

## Decision Log

| ID | Decision | Rationale | ADR | Resolves |
|---|---|---|---|---|
| DD-01 | All write controls hidden (not disabled) from Viewers | Disabled controls invite confusion; hidden signals the action doesn't exist in this context. Consistent with existing `visibleTabs` pattern in Settings.tsx. | ADR-001 | WD-06 |
| DD-02 | `ScopedGroupMappingManager` uses scoped endpoint `GET /authorization/teams/{teamId}/group-mappings` | Global endpoint requires `CanManageRbac` (System Admin only); scoped endpoint uses `CanManageTeamMembership` which Team Admins satisfy. | ADR-002 | WD-08 |
| DD-03 | Emergency admin row: "Emergency Admin" label + lock icon + no Revoke/Remove button. `RbacUserSummary.IsEmergencyAdmin` bool added. | Prevents accidental removal; communicates config-managed nature. No revoke endpoint needed. | ADR-003 | WD-02 |
| DD-04 | `useRbac` hook remains the single RBAC state source | Permissive fallback invariant (`PERMISSIVE_SUMMARY`) must live in one place. No component may fetch `/my-summary` independently. | brief.md | Constraint 3 |
| DD-05 | User removal = `DELETE /authorization/users/{userProfileId}` (new endpoint), hard delete | GDPR compliance; cascade-delete of all role assignments. WD-17. | — | US-04 |
| DD-06 | All 16 existing components EXTEND; 0 new components created | Reuse analysis confirms existing abstractions are sufficient for all required changes. | brief.md | Constraint: reuse over reimplementation |
| DD-07 | Access tab condition: `isRbacEnabled AND (isSystemAdmin OR isTeamAdmin(teamId))` | Both gates must be satisfied. RBAC disabled = tab never shown regardless of role. Role insufficient = tab not shown even if RBAC is on. | — | WD-14, WD-15 |
| DD-08 | Deliveries tab visible to Viewers; Add/Edit/Delete delivery actions hidden | Read-only tab is the primary value for Viewers. WD-12. | ADR-001 | US-09 |
| DD-09 | Add Team/Portfolio `disabled` logic: `disabled={!canCreateTeam || (isSystemAdmin && !hasConnections)}` | Non-system-admins cannot see connections (403 → []); the connections check is only meaningful for System Admins. WD-09. | — | US-10 |
| DD-10 | Settings tab in team/portfolio: `isTeamAdmin(teamId)` gates visibility (no `isRbacEnabled` guard) | Settings tab is always shown to Team Admins regardless of RBAC state. The RBAC enabled guard applies only to the Access tab (which is an RBAC-specific feature). | — | WD-15 |
| DD-11 | `GET /authorization/portfolios/{portfolioId}/group-mappings` added symmetrically with team variant | Both team and portfolio scopes must support SSO group mapping self-service for their respective admins. | ADR-002 | WD-08 |

---

## New Backend Items Confirmed

| Item | Type | Endpoint / Method | Blocks |
|---|---|---|---|
| User hard-delete | New endpoint | `DELETE /authorization/users/{userProfileId}` | US-04 |
| Team scoped group mapping read | New endpoint | `GET /authorization/teams/{teamId}/group-mappings` | US-08 |
| Portfolio scoped group mapping read | New endpoint | `GET /authorization/portfolios/{portfolioId}/group-mappings` | US-08 (portfolio scope) |
| `IsEmergencyAdmin` field | New model property | `RbacUserSummary.IsEmergencyAdmin bool` | US-02 |
| `DeleteUserAsync` | New service method | `IRbacAdministrationService.DeleteUserAsync` | US-04 |
| `GetTeamGroupMappingsAsync` | New service method | `IRbacAdministrationService.GetTeamGroupMappingsAsync` | US-08 |
| `GetPortfolioGroupMappingsAsync` | New service method | `IRbacAdministrationService.GetPortfolioGroupMappingsAsync` | US-08 (portfolio) |

---

## Constraints Carried Forward to DISTILL

1. **RBAC-disabled mode zero-regression**: All gating behind `isRbacEnabled`. When false, app behaves as before.
2. **Permissive fallback invariant**: `useRbac` hook must default to `PERMISSIVE_SUMMARY` on error. Must not change.
3. **No new state management layer**: `useRbac` is the only RBAC state source.
4. **Emergency admin is config-only**: No UI to create or revoke. `isEmergencyAdmin` flag is display-only.
5. **Group-based rights invariant**: E2E scenario 7 must verify group-based rights produce identical behaviour to individual rights.

---

## Test Environment Requirements (for DEVOPS Wave)

The RBAC E2E suite requires:
- 4 dedicated Keycloak test users with configurable group memberships:
  - `AUTH_TEST_USER_USERNAME` — bootstrap test user / emergency admin fallback user
  - A "team reader" user (Viewer for a test team)
  - A "team admin" user (TeamAdmin for a test team)
  - A "new sys admin" user (System Admin via group mapping)
- Emergency admin subject must be configured in `appsettings.json` for the E2E test instance.
- Keycloak realm must support group membership changes between test scenarios (or use separate pre-configured users per scenario).

---

## External Integrations

The OIDC provider (Keycloak in the test environment; Keycloak/Entra ID/Google/Auth0 in production) is an external integration.

Contract test recommendation: consumer-driven contracts via PactNet (.NET) for the OIDC `/userinfo` endpoint and group claims format, to detect breaking changes in group claim structure before production. This is especially relevant if the Keycloak version is upgraded or if the tenant configuration changes.

---

## Handoff Package for DISTILL Wave

1. `docs/product/architecture/brief.md` — full architecture document
2. `docs/product/architecture/c4-diagrams.md` — C4 L1, L2, L3 diagrams
3. `docs/product/architecture/adr-001-rbac-ui-gating-strategy.md`
4. `docs/product/architecture/adr-002-scoped-group-mapping-endpoint.md`
5. `docs/product/architecture/adr-003-emergency-admin-display.md`
6. `docs/feature/rbac-enhancements/feature-delta.md` — DESIGN wave sections appended
7. This file: `docs/feature/rbac-enhancements/design/wave-decisions.md`
8. Slice briefs: `docs/feature/rbac-enhancements/slices/slice-0{1-4}-*.md` (from DISCUSS wave, unchanged)
