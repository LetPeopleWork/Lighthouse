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

---

# C4 Architecture Diagrams — work-tracking-oauth-authentication

Feature: work-tracking-oauth-authentication
Wave: DESIGN
Date: 2026-05-14
Architect: Morgan (Solution Architect)

---

## C4 Level 1 — System Context (delta)

Existing System Context (RBAC) still applies. This feature adds **outbound** OAuth client relationships to external IdPs and extends the user persona set with the existing `connector-admin`.

```mermaid
C4Context
    title System Context — Lighthouse OAuth for Work-Tracking Connections

    Person(connectorAdmin, "Connector Admin", "Configures Jira / ADO connections for the Lighthouse instance")
    Person(sysAdmin, "Lighthouse System Admin", "May or may not be the same human as the connector-admin; both roles required to configure premium-gated OAuth")

    System(lighthouse, "Lighthouse", "Software delivery forecasting tool. Acts as an OAuth 2.0 client to fetch work items on behalf of the connection.")

    System_Ext(jiraIdP, "Atlassian Identity (Jira Cloud OAuth)", "auth.atlassian.com. Issues access + refresh tokens for the Jira 3LO flow.")
    System_Ext(jiraApi, "Jira Cloud REST API", "Receives bearer-authenticated work-item queries.")
    System_Ext(adoIdP, "Microsoft Entra ID / Azure DevOps OAuth", "login.microsoftonline.com. Issues access + refresh tokens for the ADO OAuth flow.")
    System_Ext(adoApi, "Azure DevOps REST API", "Receives bearer-authenticated work-item queries.")

    Rel(connectorAdmin, lighthouse, "Configures OAuth connection via")
    Rel(sysAdmin, lighthouse, "Holds Premium license + SystemAdmin role required to create OAuth connections")
    Rel(lighthouse, jiraIdP, "Authorizes user (browser redirect) + exchanges code + refreshes tokens via", "HTTPS / OAuth 2.0 3LO")
    Rel(lighthouse, jiraApi, "Reads work items with Bearer token via", "HTTPS / REST")
    Rel(lighthouse, adoIdP, "Authorizes user (browser redirect) + exchanges code + refreshes tokens via", "HTTPS / OAuth 2.0 auth code")
    Rel(lighthouse, adoApi, "Reads work items with Bearer token via", "HTTPS / REST")
```

---

## C4 Level 2 — Container (delta)

The existing Backend API / Frontend SPA / Database containers are unchanged in shape. This feature adds outbound HTTPS calls from the Backend API to two new external IdP / API systems, and adds a new persistence concern (one new table) to the existing DB.

```mermaid
C4Container
    title Container Diagram — Lighthouse OAuth (delta over rbac-enhancements baseline)

    Person(user, "Connector Admin (also SystemAdmin)", "Configures the OAuth-authenticated connection")
    System_Ext(idp, "External IdP", "Atlassian Identity OR Microsoft Entra ID. Provider selected by AuthenticationMethodKey.")
    System_Ext(wts, "Work-Tracking REST API", "Jira Cloud OR Azure DevOps. Receives bearer-authenticated requests.")

    Container(spa, "Frontend SPA", "React 18 + TypeScript + MUI", "Renders OAuth form (clientId/clientSecret + callback URL display), Reconnect banner. Reads server BaseUrl via system-info endpoint.")
    Container(api, "Backend API", "C# .NET 8 ASP.NET Core", "OAuthController + OAuthService + IOAuthProvider implementations + OAuthBearerAuthStrategy + IWorkTrackingAuthStrategyFactory. Ports-and-adapters.")
    ContainerDb(db, "Database", "SQLite / PostgreSQL via EF Core", "Adds OAuthCredentials table (1:1 with WorkTrackingSystemConnections, cascade delete). clientId/clientSecret continue to live in WorkTrackingSystemConnectionOptions (existing pattern).")

    Rel(user, spa, "Configures OAuth via", "HTTPS")
    Rel(spa, api, "POST /api/oauth/{providerKey}/connect → authorizationUrl", "HTTPS / JSON")
    Rel(spa, idp, "Browser-level redirect to consent screen", "HTTPS / OAuth 2.0")
    Rel(idp, api, "GET /api/oauth/callback?code&state (browser-driven 302)", "HTTPS")
    Rel(api, idp, "POST token endpoint (code exchange + refresh, server-to-server)", "HTTPS / OAuth 2.0")
    Rel(api, wts, "GET work items with Authorization: Bearer (server-to-server)", "HTTPS / REST")
    Rel(api, db, "Persists OAuthCredential + reads/writes connection Options", "EF Core")
```

---

## C4 Level 3 — Component: OAuth Domain

```mermaid
C4Component
    title Component Diagram — OAuth Domain (Backend)

    Container_Boundary(api, "Backend API") {
        Component(oauthCtrl, "OAuthController", "ASP.NET Core ApiController", "Routes: POST /api/oauth/{providerKey}/connect (Auth+SystemAdmin+Premium), GET /api/oauth/callback (AllowAnonymous; state-token CSRF), POST /api/oauth/{providerKey}/disconnect (Auth+SystemAdmin+Premium).")
        Component(oauthSvc, "OAuthService", "C# class implementing IOAuthService", "Owns the OAuth flow: Initiate, Complete, Disconnect, EnsureFreshToken. Holds the single-flight semaphore dictionary keyed on OAuthCredential.Id.")
        Component(oauthPort, "IOAuthService", "C# interface (inbound port)", "OAuthController depends on this interface; OAuthBearerAuthStrategy depends on EnsureFreshTokenAsync via this interface.")
        Component(providerRegistry, "IOAuthProviderRegistry + OAuthProviderRegistry", "C# port + impl", "Resolves IOAuthProvider by string key (AuthenticationMethodKey). Throws OAuthProviderNotFoundException on miss; startup self-check fails the app boot if a registered method key has no provider.")
        Component(jiraProvider, "JiraOAuthProvider : IOAuthProvider", "C# class", "ProviderKey = jira.oauth. Atlassian 3LO endpoints. Builds auth URL, exchanges code, refreshes tokens.")
        Component(adoProvider, "AdoOAuthProvider : IOAuthProvider", "C# class", "ProviderKey = ado.oauth. Entra ID + Azure DevOps OAuth endpoints. Builds auth URL, exchanges code, refreshes tokens.")
        Component(stateIssuer, "IOAuthStateTokenIssuer + OAuthStateTokenIssuer", "C# port + impl", "HMAC-SHA256-signed self-verifying state token. No session store. CSRF protection on the OAuth dance per DDD-8.")
        Component(authStrategy, "IWorkTrackingAuthStrategy (3 impls: PAT, JiraCloudBasic, OAuthBearer)", "C# strategy port + 3 adapters", "Resolved per-request from AuthenticationMethodKey. OAuthBearer calls IOAuthService.EnsureFreshTokenAsync and sets Authorization: Bearer on the outbound request.")
        Component(jiraConn, "JiraWorkTrackingConnector (existing, EXTENDED)", "C# class", "Now delegates auth-header construction to IWorkTrackingAuthStrategy resolved from connection.AuthenticationMethodKey.")
        Component(adoConn, "AzureDevOpsWorkTrackingConnector (existing, EXTENDED)", "C# class", "Same delegation as Jira.")
        Component(credRepo, "IRepository<OAuthCredential> via LighthouseAppContext", "EF Core DbSet", "Persists OAuthCredential with encrypted AccessToken/RefreshToken columns. Cascade delete with WorkTrackingSystemConnection.")
        Component(crypto, "ICryptoService (existing)", "C# service", "AES-encrypts secret columns at rest. Reused — no new crypto code.")
        Component(licenseGuard, "LicenseGuardAttribute (existing)", "ASP.NET Core ActionFilterAttribute", "Enforces Premium gate at the controller-action boundary. Applied to /connect and /disconnect.")
    }

    Container_Boundary(spa, "Frontend SPA") {
        Component(oauthFeService, "OAuthService.ts", "TypeScript HTTP adapter", "initiateConnect + disconnect. Triggers browser redirect to authorizationUrl returned by /connect.")
        Component(oauthForm, "OAuthAuthForm.tsx", "React component", "clientId + clientSecret + read-only callback URL display (sourced from server-info BaseUrl). Surfaces BaseUrl + HTTPS warnings per ADR-009.")
        Component(authDropdown, "AuthMethodDropdown.tsx (existing, EXTENDED)", "React component", "Renders new entries with IsPremium badge + standalone-mode disabled state per US-04.")
        Component(reconnectBanner, "ReconnectBanner.tsx", "React component", "Visible when connection.requiresReconnect == true. Reconnect button re-invokes initiateConnect.")
    }

    Rel(oauthCtrl, oauthPort, "invokes OAuth flow via")
    Rel(oauthSvc, oauthPort, "implements")
    Rel(oauthSvc, providerRegistry, "resolves provider for connection via")
    Rel(providerRegistry, jiraProvider, "returns")
    Rel(providerRegistry, adoProvider, "returns")
    Rel(oauthSvc, stateIssuer, "issues/verifies state tokens via")
    Rel(oauthSvc, credRepo, "persists tokens via")
    Rel(oauthCtrl, licenseGuard, "enforced by")
    Rel(authStrategy, oauthPort, "OAuthBearer impl calls EnsureFreshTokenAsync via")
    Rel(jiraConn, authStrategy, "delegates auth-header construction to")
    Rel(adoConn, authStrategy, "delegates auth-header construction to")
    Rel(credRepo, crypto, "value-converted AccessToken/RefreshToken via")
    Rel(oauthForm, oauthFeService, "POST /connect via")
    Rel(authDropdown, oauthForm, "renders when key in {jira.oauth, ado.oauth}")
    Rel(reconnectBanner, oauthFeService, "POST /disconnect then /connect via")
```

---

## OAuth flow sequence (Mermaid)

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Connector Admin (browser)
    participant FE as Frontend SPA
    participant API as Backend API (OAuthController + OAuthService)
    participant IdP as External IdP (Atlassian / Entra ID)
    participant DB as DB (OAuthCredentials)
    participant Connector as JiraWorkTrackingConnector / ADO connector
    participant WTS as Work-Tracking REST API

    Note over Admin,IdP: One-time setup (out of band)
    Admin->>IdP: Register OAuth app, copy clientId/clientSecret

    Note over Admin,DB: Initial connection
    Admin->>FE: Open connector form, paste clientId/clientSecret, click Connect
    FE->>API: POST /api/oauth/jira.oauth/connect { connectionId }
    API->>API: Issue HMAC state token (connectionId, providerKey, nonce, exp)
    API->>FE: 200 { authorizationUrl }
    FE->>IdP: 302 → authorizationUrl (with redirect_uri = BaseUrl/api/oauth/callback)
    Admin->>IdP: Consent
    IdP->>API: GET /api/oauth/callback?code&state (browser-driven)
    API->>API: Verify state HMAC (CSRF; no session store)
    API->>IdP: POST token endpoint (server-to-server) { code, clientId, clientSecret }
    IdP->>API: { accessToken, refreshToken, expiresIn }
    API->>DB: Persist OAuthCredential (Status=Valid, tokens encrypted)
    API->>FE: 302 → /settings/connections/{id}?oauth=success

    Note over Connector,WTS: Subsequent outbound sync
    Connector->>API: EnsureFreshTokenAsync(connectionId)
    alt expiresAt - now > 5 min
        API->>Connector: return cached accessToken
    else expiry imminent
        API->>API: Acquire semaphore on OAuthCredential.Id (timeout 30s)
        API->>DB: Re-read credential (double-check)
        alt now-fresh
            API->>Connector: return cached accessToken
        else still expired
            API->>IdP: POST token refresh { refreshToken, clientId, clientSecret }
            alt refresh succeeds
                IdP->>API: new tokens
                API->>DB: Update OAuthCredential atomically
                API->>Connector: return new accessToken
            else refresh fails
                API->>DB: Status = RefreshFailed
                API-->>Connector: throw OAuthRefreshFailedException
            end
        end
    end
    Connector->>WTS: GET work items (Authorization: Bearer {accessToken})
    WTS->>Connector: work items
```

---

# C4 Architecture Diagrams — filter-forecast-throughput

Feature: filter-forecast-throughput (Epic 4896)
Wave: DESIGN
Date: 2026-05-20
Architect: Morgan (Solution Architect)

---

## C4 Level 1 — System Context (delta)

The Lighthouse system context (RBAC baseline) is unchanged. This feature adds **no** new external actors and **no** new external systems. It introduces a new internal persona — `Delivery Forecaster` — and tightens the conversation between the existing Team Admin and the existing Premium License gate.

```mermaid
C4Context
    title System Context — Filter Forecast Throughput

    Person(forecaster, "Delivery Forecaster", "Owns the WHEN / HOW MUCH conversation with leadership. Needs honest percentile dates.")
    Person(teamAdmin, "Team Admin", "Configures the team's forecast-throughput filter rule set. Often the same person as the Delivery Forecaster.")
    Person(viewer, "Viewer", "Sees forecasts + chip indicating filter active; can flip per-view toggles read-only; cannot edit rules.")

    System(lighthouse, "Lighthouse", "Software delivery forecasting tool. New: per-team forecast-throughput filter, premium-gated. Reuses the existing DeliveryRuleSet rule-engine; new field schema operates on WorkItem fields. Affects forecast surfaces and (opt-in) throughput charts.")

    System_Ext(wts, "Work-Tracking System", "Jira / Azure DevOps / Linear. Source of WorkItems whose closed-history feeds Monte Carlo throughput sampling.")

    Rel(forecaster, lighthouse, "Reviews forecasts; flips per-view / per-run filter toggles via")
    Rel(teamAdmin, lighthouse, "Configures forecast-throughput rule set via")
    Rel(viewer, lighthouse, "Reads forecasts with filtered/raw chip annotation via")
    Rel(lighthouse, wts, "Reads closed WorkItems for throughput sampling from")
```

---

## C4 Level 2 — Container (delta)

No new containers. The existing Frontend SPA, Backend API, Database, and E2E Test Runner all gain additive responsibilities — they exchange one new DTO shape (`forecastFilterRuleSetJson` on the team-settings round trip) and one new endpoint path (`/forecast-filter/schema`). The Backend API's persistence container gains one nullable JSON column on the `Teams` table.

```mermaid
C4Container
    title Container Diagram — Filter Forecast Throughput

    Person(user, "Authenticated User", "Delivery Forecaster / Team Admin / Viewer")

    Container(spa, "Frontend SPA", "React 18 + TypeScript + Material UI", "Renders new Forecast Filter (Premium) section embedding the existing DeliveryRuleBuilder. Renders Filtered Throughput chip on five surfaces. Per-view toggle on Run Chart (client-side filter) and PBC chart (sends ?view=filtered).")
    Container(api, "Backend API", "C# .NET 8 ASP.NET Core Web API", "Persists Team.ForecastFilterRuleSetJson. Applies filter inside ITeamMetricsService at two seams (DDD-4). Returns filter schema (D9 WorkItem fields). Surfaces filterApplied + excludedSummary on forecast / backtest responses.")
    ContainerDb(db, "Database", "SQLite (dev/test) / PostgreSQL (prod) via EF Core", "Teams table gains nullable ForecastFilterRuleSetJson column. No other schema changes.")
    Container(e2e, "E2E Test Runner", "Playwright + TypeScript", "One @premium E2E covers the full journey: configure rule → see chip on feature forecast → flip chart toggle → flip team-forecast toggle → flip backtest toggle.")

    System_Ext(wts, "Work-Tracking System")

    Rel(user, spa, "Navigates application via", "HTTPS")
    Rel(spa, api, "GET /api/team/{id}/forecast-filter/schema, PUT /api/team/{id}, POST /api/forecast/manual/{id}, POST /api/forecast/backtest/{id}, GET /api/teamMetrics/{id}/throughput, GET /api/teamMetrics/{id}/throughput/pbc?view=", "HTTPS / JSON")
    Rel(api, db, "Reads/writes Team entity (incl. new JSON column) via", "EF Core")
    Rel(api, wts, "Polls WorkItems via existing connector path (unchanged)", "HTTPS")
    Rel(e2e, spa, "Drives browser interactions against", "Playwright CDP")
    Rel(e2e, api, "Calls API helpers for test setup against", "HTTPS / JSON")
```

---

## C4 Level 3 — Component: Rule-Engine and Forecast-Filter Domain

The rule-engine generalisation (ADR-012) is the architecturally significant subsystem of this feature. This diagram makes the new ports / adapters and the delegation paths explicit, and shows the single-seam filter application inside `TeamMetricsService` (DDD-4).

```mermaid
C4Component
    title Component Diagram — Rule-Engine and Forecast-Filter Domain (Backend)

    Container_Boundary(api, "Backend API") {
        Component(teamCtrl, "TeamController (existing, EXTENDED)", "ASP.NET Core ApiController", "PUT /api/team/{id} accepts forecastFilterRuleSetJson; GET /api/team/{id}/forecast-filter/schema is new. Both gated by [RbacGuard].")
        Component(metricsCtrl, "TeamMetricsController (existing, EXTENDED)", "ASP.NET Core ApiController", "GET /throughput unchanged; GET /throughput/pbc gains ?view=raw|filtered.")
        Component(forecastCtrl, "ForecastController (existing, EXTENDED)", "ASP.NET Core ApiController", "RunManualForecastAsync + RunBacktest accept optional applyFilterOverride; responses gain filterApplied + excludedSummary.")

        Component(filterSvc, "ForecastFilterRuleService : IForecastFilterRuleService", "C# class (NEW)", "GetSchema(team); GetEffectiveRuleSet(team) returns null on free tenant / null JSON / zero conditions (DDD-8 + DDD-9); Filter(items, ruleSet) excludes matched (D8); ValidateRuleSet(ruleSet, team).")
        Component(deliverySvc, "DeliveryRuleService : IDeliveryRuleService (existing, REFACTORED)", "C# class", "Public surface preserved (GetRuleSchema, GetMatchingFeaturesForRuleset, RecomputeRuleBasedDeliveries). Internals now delegate to RuleEvaluator<Feature>.")

        Component(evaluator, "RuleEvaluator<T> : IRuleEvaluator<T>", "C# generic class (NEW)", "Pure function. Match(ruleSet, items, fieldProvider) → matched items. IsValid(ruleSet, schema) → bool. NO I/O dependencies (enforced by ArchUnitNET).")
        Component(featureProv, "FeatureFieldProvider : IRuleFieldProvider<Feature>", "C# class (NEW)", "Field keys: feature.type / feature.state / feature.name / feature.referenceid / feature.parentreferenceid / feature.tags / additionalField.{id}.")
        Component(workItemProv, "WorkItemFieldProvider : IRuleFieldProvider<WorkItem>", "C# class (NEW)", "Field keys: workitem.type / workitem.state / workitem.name / workitem.referenceid / workitem.parentreferenceid / workitem.tags / additionalField.{id}.")

        Component(metricsSvc, "TeamMetricsService : ITeamMetricsService (existing, EXTENDED)", "C# class", "GetCurrentThroughputForTeamForecast(team, mode) AND GetBlackoutAwareThroughputForTeam(team, start, end, mode) apply the filter when mode demands it. Single seam (DDD-4). Cache key includes mode.")

        Component(licenseSvc, "LicenseService : ILicenseService (existing)", "C# class", "CanUsePremiumFeatures() consulted by ForecastFilterRuleService.GetEffectiveRuleSet.")
        Component(efCtx, "LighthouseAppContext (existing, EXTENDED)", "EF Core DbContext", "Team entity adds nullable ForecastFilterRuleSetJson column. Migration generated via CreateMigration PowerShell (Sqlite + Postgres in lockstep).")
    }

    Rel(teamCtrl, filterSvc, "validates rule set / fetches schema via")
    Rel(metricsCtrl, metricsSvc, "queries throughput PBC via (passes mode)")
    Rel(forecastCtrl, metricsSvc, "fetches throughput vector via (passes mode)")

    Rel(filterSvc, evaluator, "matches WorkItems via")
    Rel(filterSvc, workItemProv, "uses for field access")
    Rel(filterSvc, licenseSvc, "checks premium gate via (DDD-9)")
    Rel(filterSvc, efCtx, "reads Team.ForecastFilterRuleSetJson via")

    Rel(deliverySvc, evaluator, "matches Features via")
    Rel(deliverySvc, featureProv, "uses for field access")

    Rel(metricsSvc, filterSvc, "applies filter at the two seams (DDD-4)")
    Rel(metricsSvc, efCtx, "reads closed WorkItems via")
```

The diagram makes three architectural commitments visible:

1. **Single shared evaluator** (`RuleEvaluator<T>`) — the same algorithmic code path handles both Feature and WorkItem rule evaluation; bug fixes / operator additions land in one place (ADR-012).
2. **Single filter seam** (`TeamMetricsService` — and nothing else — calls `IForecastFilterRuleService.Filter`); enforced by ArchUnitNET (DDD-4).
3. **Single license gate for this feature** (`ForecastFilterRuleService.GetEffectiveRuleSet`); enforced by ArchUnitNET (DDD-9).

