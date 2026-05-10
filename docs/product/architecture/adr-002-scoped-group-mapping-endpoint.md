# ADR-002: Scoped vs Global Endpoint for Group Mapping Reads

**Status**: Accepted
**Date**: 2026-05-10
**Feature**: rbac-enhancements
**Decider**: Morgan (Solution Architect) based on WD-08 (DISCUSS wave)

---

## Context

`ScopedGroupMappingManager` is a shared React component used inside the Access tab of both team and portfolio detail pages. It shows SSO group mappings for the currently viewed scope (team or portfolio) and allows Team/Portfolio Admins to manage them.

In the current (buggy) implementation, the component calls the global `GET /authorization/group-mappings` endpoint and then filters the result client-side by `scopeType` and `scopeId`. This endpoint requires `CanManageRbac` (System Admin only). Team Admins receive a 403, which surfaces as "Failed to load team access groups."

Two options for the fix were evaluated.

---

## Decision

**Use the scoped endpoint `GET /authorization/teams/{teamId}/group-mappings` (and the symmetric portfolio variant) for all reads in `ScopedGroupMappingManager`.**

The `ScopedGroupMappingManager` component will accept the group mappings data and any loading/error state from its parent (TeamDetail or PortfolioDetail). The parent is responsible for fetching via the appropriate scoped endpoint. The component itself remains a presentation component.

`GET /authorization/teams/{teamId}/group-mappings` is gated on `CanManageTeamMembership`, which Team Admins satisfy. The symmetric `GET /authorization/portfolios/{portfolioId}/group-mappings` endpoint is gated on `CanManagePortfolioMembership`.

Both scoped read endpoints are added to `AuthorizationController` and backed by new methods on `IRbacAdministrationService`.

---

## Alternatives Considered

### Option A: Keep global endpoint, elevate Team Admin permission to CanManageRbac (rejected)

Allow Team Admins to call the global `GET /authorization/group-mappings` endpoint by relaxing the `CanManageRbac` check.

**Rejected because**:
- The global endpoint returns all group mappings for all scopes (teams, portfolios, system). A Team Admin seeing other teams' mappings violates least-privilege.
- `CanManageRbac` is explicitly reserved for System Admin operations. Diluting it undermines the permission model.
- The response payload would require client-side filtering regardless — scoped endpoints return only the relevant data, reducing payload size.

### Option B: Global endpoint with scope filter parameter (rejected)

Add a `?scopeType=Team&scopeId={teamId}` query parameter to the global endpoint, still gated on `CanManageRbac` but returning only the requested scope.

**Rejected because**:
- Still requires `CanManageRbac` (System Admin), so Team Admins remain blocked.
- Query-parameter-filtered access to a System Admin endpoint is a confusing design: the route implies global access even though the response is scoped.

### Option C: Scoped endpoints per scope type (selected)

`GET /authorization/teams/{teamId}/group-mappings` gated on `CanManageTeamMembership`.
`GET /authorization/portfolios/{portfolioId}/group-mappings` gated on `CanManagePortfolioMembership`.

**Accepted because**:
- Permissions are granular and correctly scoped: Team Admins access only their teams' mappings.
- Consistent with the existing membership endpoints (`GET /authorization/teams/{teamId}/members` etc.), which already follow this pattern.
- No change to the component interface: parent fetches, component displays. Clean separation.

---

## Consequences

**Positive**:
- Team/Portfolio Admins can manage SSO group mappings for their scope without System Admin involvement (KPI: eliminate escalations).
- Endpoint security is properly scoped: no cross-scope data leakage.
- "Failed to load team access groups" error is eliminated for legitimate Team Admins.

**Negative**:
- Two new backend endpoints are required. Accepted: they follow a well-established pattern in `AuthorizationController` and require minimal implementation in `RbacAdministrationService` (a filtered read on the existing `GetGroupMappingsAsync` result).
- `ScopedGroupMappingManager` no longer fetches its own data — it must receive data via props. This is a minor structural change but improves testability (pure presentation component).

Note: The portfolio-scoped endpoint `GET /authorization/portfolios/{portfolioId}/group-mappings` is added symmetrically — see DD-11 in `docs/feature/rbac-enhancements/design/wave-decisions.md`.
