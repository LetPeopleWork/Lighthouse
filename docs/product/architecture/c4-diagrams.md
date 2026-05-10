# C4 Architecture Diagrams — rbac-enhancements

Feature: rbac-enhancements
Wave: DESIGN
Date: 2026-05-10
Architect: Morgan (Solution Architect)

---

## C4 Level 1 — System Context

```mermaid
C4Context
    title System Context — Lighthouse RBAC Enhancements

    Person(sysAdmin, "System Admin", "Manages users, grants/revokes roles, bootstraps RBAC")
    Person(scopedAdmin, "Team/Portfolio Admin", "Administers their own team or portfolio scope")
    Person(viewer, "Viewer", "Reads forecast and delivery data for assigned teams/portfolios")
    Person(firstAdmin, "First-Time Admin", "IT lead bootstrapping RBAC on first deployment")

    System(lighthouse, "Lighthouse", "Software delivery forecasting tool with RBAC-gated access control. Provides role-based views of teams, portfolios, and forecasts.")

    System_Ext(oidc, "OIDC Provider", "Keycloak / Entra ID / Google / Auth0. Issues JWT tokens with group claims used for automatic role elevation.")

    Rel(sysAdmin, lighthouse, "Manages users and roles via")
    Rel(scopedAdmin, lighthouse, "Manages team/portfolio membership via")
    Rel(viewer, lighthouse, "Reads forecasting data via")
    Rel(firstAdmin, lighthouse, "Bootstraps RBAC via")
    Rel(lighthouse, oidc, "Validates JWT tokens and reads group claims from")
```

---

## C4 Level 2 — Container

```mermaid
C4Container
    title Container Diagram — Lighthouse

    Person(user, "Authenticated User", "Any of: System Admin, Team/Portfolio Admin, Viewer")
    System_Ext(oidc, "OIDC Provider", "Keycloak / Entra ID / Google / Auth0")

    Container(spa, "Frontend SPA", "React 18 + TypeScript + Material UI", "Renders role-gated UI. Derives all gating decisions from useRbac hook. No gating logic embedded in individual components.")
    Container(api, "Backend API", "C# .NET 8 ASP.NET Core Web API", "Enforces authorisation at every endpoint via RbacGuard and IRbacAdministrationService. Ports-and-adapters architecture.")
    ContainerDb(db, "Database", "SQLite (dev/test) / PostgreSQL (prod) via EF Core", "Stores UserPermission, RbacGroupMapping, UserProfile entities.")
    Container(e2e, "E2E Test Runner", "Playwright + TypeScript", "Exercises RBAC flows end-to-end using 4 dedicated Keycloak test users with group memberships.")

    Rel(user, spa, "Navigates application via", "HTTPS")
    Rel(spa, api, "Calls REST endpoints via", "HTTPS / JSON")
    Rel(api, oidc, "Validates JWT tokens and reads group claims from", "OIDC / JWKS")
    Rel(api, db, "Reads and writes RBAC entities via", "EF Core")
    Rel(e2e, spa, "Drives browser interactions against", "Playwright CDP")
    Rel(e2e, api, "Calls API helpers for test setup against", "HTTPS / JSON")
```

---

## C4 Level 3 — Component: Authorization Domain

```mermaid
C4Component
    title Component Diagram — Authorization Domain

    Container_Boundary(api, "Backend API") {
        Component(authCtrl, "AuthorizationController", "ASP.NET Core ApiController", "Handles all /authorization/* endpoints. Routes: bootstrap, system-admin CRUD, team/portfolio member CRUD, group-mapping CRUD, user delete, scoped group-mapping reads.")
        Component(rbacSvc, "RbacAdministrationService", "C# class implementing IRbacAdministrationService", "Implements all RBAC business logic: permission checks, role grants/revocations, scoped membership, group mapping evaluation, emergency admin detection.")
        Component(rbacPort, "IRbacAdministrationService", "C# interface (driven port)", "Defines all RBAC operations as an abstraction. AuthorizationController depends on this interface, never on the concrete class.")
        Component(rbacGuard, "RbacGuard / RbacGuardRequirement", "ASP.NET Core IAuthorizationHandler", "Enforces per-endpoint authorisation using IRbacAdministrationService. Applied via [Authorize] policy attributes.")
        Component(efCtx, "LighthouseDbContext (RBAC entities)", "EF Core DbContext", "Persists UserPermission, RbacGroupMapping, UserProfile. Driven adapter for the database port.")
    }

    Container_Boundary(spa, "Frontend SPA") {
        Component(useRbac, "useRbac hook", "React custom hook (TypeScript)", "Single source of RBAC state. Fetches /authorization/my-summary on mount. Returns isRbacEnabled, isSystemAdmin, isTeamAdmin(id), isPortfolioAdmin(id), canCreateTeam, canCreatePortfolio. Falls back to PERMISSIVE_SUMMARY on error.")
        Component(rbacService, "RbacService", "TypeScript service class implementing IRbacService", "HTTP adapter for /authorization/* endpoints. Consumed by useRbac and page components.")
        Component(rbacSettings, "RbacSettings", "React component (TSX)", "System Admin management UI. Renders bootstrap button, user table with Emergency Admin display, SSO group mappings, RBAC Status disclosure panel.")
        Component(scopedMgr, "ScopedMembershipManager", "React component (TSX)", "Per-team/portfolio member management. Shown in Access tab of team/portfolio detail pages.")
        Component(scopedGrpMgr, "ScopedGroupMappingManager", "React component (TSX)", "Per-team/portfolio SSO group mapping. Calls scoped endpoint /authorization/teams/{teamId}/group-mappings.")
    }

    Rel(authCtrl, rbacPort, "invokes business logic via")
    Rel(rbacSvc, rbacPort, "implements")
    Rel(rbacGuard, rbacPort, "checks permissions via")
    Rel(rbacSvc, efCtx, "reads and writes RBAC entities via")
    Rel(useRbac, rbacService, "fetches authorization summary via")
    Rel(rbacSettings, rbacService, "manages system admins and groups via")
    Rel(scopedMgr, rbacService, "manages scoped members via")
    Rel(scopedGrpMgr, rbacService, "manages scoped group mappings via")
```
