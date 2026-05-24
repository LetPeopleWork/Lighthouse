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

---

# C4 Architecture Diagrams — time-in-state-and-staleness

Feature: time-in-state-and-staleness (Epic 4144 MVP bundle, slice A+B1+D)
Wave: DESIGN
Date: 2026-05-24
Architect: Morgan (Solution Architect), interaction mode = PROPOSE

---

## C4 Level 1 — System Context (delta)

The Lighthouse system context (RBAC + OAuth + filter-forecast-throughput baselines) is unchanged. This feature adds **no** new external actors and **no** new external systems. It introduces a focus persona — `Flow Coach` — already represented across the RBAC personas, and clarifies the read-side relationship with the existing work-tracking systems: Lighthouse now CAPTURES state-transition history (where the source exposes it) rather than only deriving Started / Closed boundary dates.

```mermaid
C4Context
    title System Context — Time in State and Staleness

    Person(coach, "Flow Coach", "Team lead / agile coach / scrum master / RTE. Runs flow reviews; needs to spot stuck items at a glance.")
    Person(teamAdmin, "Team Admin", "Configures the team's staleness threshold (days).")
    Person(portfolioAdmin, "Portfolio Admin", "Configures the portfolio's staleness threshold (days).")

    System(lighthouse, "Lighthouse", "Software delivery forecasting tool. New: persisted state-transition history per work item; per-item Time-in-State badge with red emphasis above a Team/Portfolio-configured threshold. Data foundation for two sibling MVP features (aging-pace-percentiles, state-time-cumulative-view).")

    System_Ext(wts, "Work-Tracking System", "Jira / Azure DevOps / Linear / CSV. Source of WorkItems; for Jira/ADO/Linear the source-system also exposes per-item state-transition history (changelog / revisions / GraphQL history) used by Lighthouse as source-of-truth. CSV has no history; Lighthouse falls back to sync-cadence delta capture.")

    Rel(coach, lighthouse, "Reads team / portfolio work-item table; spots red badges via")
    Rel(teamAdmin, lighthouse, "Configures Team staleness threshold via")
    Rel(portfolioAdmin, lighthouse, "Configures Portfolio staleness threshold via")
    Rel(lighthouse, wts, "Reads work items AND state-transition history (where exposed) from", "HTTPS / GraphQL / file system")
```

---

## C4 Level 2 — Container (delta)

No new containers. The existing Frontend SPA, Backend API, Database, and E2E Test Runner all gain additive responsibilities. The Database container gains one new table (`WorkItemStateTransitions`) and two new columns (`WorkItems.CurrentStateEnteredAt`, `WorkTrackingSystemOptionsOwner.StalenessThresholdDays`). The Backend API extends the existing sync-path inside `WorkItemService.RefreshWorkItems` and the existing `WorkItemDto` projection. The Frontend SPA extends the existing work-item-table column rendering and adds a `Staleness Threshold (days)` input on the Team and Portfolio settings forms.

```mermaid
C4Container
    title Container Diagram — Time in State and Staleness

    Person(user, "Authenticated User", "Flow Coach / Team Admin / Portfolio Admin (existing RBAC roles)")

    Container(spa, "Frontend SPA", "React 18 + TypeScript + Material UI", "Renders new TimeInStateBadge per work-item row (extends WorkItemsDialog). Renders Staleness Threshold input on Team and Portfolio settings forms (gated by useRbac).")
    Container(api, "Backend API", "C# .NET 8 ASP.NET Core Web API", "Captures state transitions inside WorkItemService.RefreshWorkItems (per-connector capability flag). Persists WorkItemStateTransition rows and WorkItem.CurrentStateEnteredAt. Surfaces currentStateEnteredAt + approximate on WorkItemDto. Round-trips stalenessThresholdDays via TeamSettingDto / PortfolioSettingDto. Ports-and-adapters.")
    ContainerDb(db, "Database", "SQLite (dev/test) / PostgreSQL (prod) via EF Core", "Adds WorkItemStateTransitions table (FK→WorkItem cascade delete; composite index on (WorkItemId, TransitionedAt)). Adds WorkItems.CurrentStateEnteredAt nullable column. Adds WorkTrackingSystemOptionsOwner.StalenessThresholdDays column (defaults 7 / 14 applied via entity initialiser).")
    Container(e2e, "E2E Test Runner", "Playwright + TypeScript", "One @flow-coach E2E covers the badge appearance and threshold-change-on-render. Per-connector integration tests live in NUnit (faster, deterministic).")

    System_Ext(wts, "Work-Tracking System", "Jira / ADO / Linear / CSV")

    Rel(user, spa, "Navigates application via", "HTTPS")
    Rel(spa, api, "GET /api/v1/teams/{id}/metrics/wip, GET /api/v1/teams/{id}/metrics/cycleTimeData (extended WorkItemDto); PUT /api/v1/teams/{id} (extended TeamSettingDto); PUT /api/v1/portfolios/{id} (extended PortfolioSettingDto)", "HTTPS / JSON")
    Rel(api, db, "Reads WorkItems + WorkItemStateTransitions; writes both atomically per sync via", "EF Core")
    Rel(api, wts, "Polls WorkItems AND state-transition history per existing connector path (extended)", "HTTPS / GraphQL / file system")
    Rel(e2e, spa, "Drives browser interactions against", "Playwright CDP")
    Rel(e2e, api, "Calls API helpers for test setup against", "HTTPS / JSON")
```

---

## C4 Level 3 — Component: Transition Capture and Time-in-State Domain

The transition-capture dispatch (ADR-017) is the architecturally significant subsystem of this feature. This diagram makes the capability-flag dispatch, the single-seam invariant (`WorkItemService.RefreshWorkItems` is the only writer), and the consumer-facing repository surface explicit.

```mermaid
C4Component
    title Component Diagram — Transition Capture and Time-in-State Domain (Backend)

    Container_Boundary(api, "Backend API") {
        Component(teamMetricsCtrl, "TeamMetricsController (existing, EXTENDED)", "ASP.NET Core ApiController", "GET /metrics/wip and /metrics/cycleTimeData unchanged routes; WorkItemDto payload gains currentStateEnteredAt + approximate.")
        Component(teamCtrl, "TeamController (existing, EXTENDED)", "ASP.NET Core ApiController", "PUT /api/v1/teams/{id} accepts stalenessThresholdDays via TeamSettingDto.")
        Component(portfolioCtrl, "PortfolioController (existing, EXTENDED)", "ASP.NET Core ApiController", "PUT /api/v1/portfolios/{id} accepts stalenessThresholdDays via PortfolioSettingDto.")

        Component(workItemSvc, "WorkItemService.RefreshWorkItems (existing, EXTENDED)", "C# class", "ONLY writer of WorkItemStateTransitions and ONLY mutator of WorkItem.CurrentStateEnteredAt. Branches on IWorkTrackingConnector.SupportsTransitionHistory: true = persist SyncedTransitions + derive CurrentStateEnteredAt from them; false = sync-delta synthetic transition with TransitionedAt = UtcNow. All writes flush in one SaveChangesAsync.")

        Component(connectorPort, "IWorkTrackingConnector (existing, EXTENDED)", "C# interface", "Adds bool SupportsTransitionHistory. Existing GetWorkItemsForTeam(Team) now also populates WorkItemBase.SyncedTransitions on each item when capability is true.")
        Component(jiraConn, "JiraWorkTrackingConnector (existing, EXTENDED) + IssueFactory", "C# class", "SupportsTransitionHistory=true. Extends GetTransitionDate / ExtractDateOfStateTransitionFromHistory to emit every status transition (not just boundary). Source-of-truth: changelog histories field.")
        Component(adoConn, "AzureDevOpsWorkTrackingConnector (existing, EXTENDED)", "C# class", "SupportsTransitionHistory=true. Extends GetStateTransitionDateThrottled to emit every (fromState, toState, changedDate) from witClient.GetRevisionsAsync.")
        Component(linearConn, "LinearWorkTrackingConnector (existing, EXTENDED)", "C# class", "SupportsTransitionHistory=true with per-connection runtime downgrade if GraphQL history field unsupported. Extends IssueNode query with history { nodes { fromState toState createdAt } }.")
        Component(csvConn, "CsvWorkTrackingConnector (existing, EXTENDED)", "C# class", "SupportsTransitionHistory=false. SyncedTransitions always empty; WorkItemService runs sync-delta.")

        Component(transitionRepo, "IWorkItemStateTransitionRepository + WorkItemStateTransitionRepository", "C# port + impl (NEW)", "Extends IRepository<WorkItemStateTransition>. Sole abstraction layer for sibling MVP consumers (aging-pace-percentiles, state-time-cumulative-view).")
        Component(workItemRepo, "IWorkItemRepository (existing, NO CHANGE)", "C# port", "Continues to expose WorkItem read/write — WorkItem.CurrentStateEnteredAt is a plain property, no new repository method needed.")
        Component(efCtx, "LighthouseAppContext (existing, EXTENDED)", "EF Core DbContext", "Adds DbSet<WorkItemStateTransition> with FK→WorkItem cascade delete and composite index (WorkItemId, TransitionedAt). Existing UtcDateTimeConverter applied automatically.")

        Component(workItemDto, "WorkItemDto (existing, EXTENDED)", "C# record/class", "Gains CurrentStateEnteredAt: DateTime?, Approximate: bool.")
        Component(settingsBase, "SettingsOwnerDtoBase (existing, EXTENDED)", "C# class", "Gains StalenessThresholdDays: int.")
    }

    Rel(teamMetricsCtrl, workItemRepo, "reads via")
    Rel(teamMetricsCtrl, workItemDto, "projects via")
    Rel(teamCtrl, settingsBase, "via TeamSettingDto round-trip")
    Rel(portfolioCtrl, settingsBase, "via PortfolioSettingDto round-trip")

    Rel(workItemSvc, connectorPort, "polls SyncedTransitions + State via")
    Rel(workItemSvc, transitionRepo, "writes new transition rows via")
    Rel(workItemSvc, workItemRepo, "updates WorkItem.CurrentStateEnteredAt via")

    Rel(jiraConn, connectorPort, "implements")
    Rel(adoConn, connectorPort, "implements")
    Rel(linearConn, connectorPort, "implements")
    Rel(csvConn, connectorPort, "implements")

    Rel(transitionRepo, efCtx, "reads / writes WorkItemStateTransitions via")
    Rel(workItemRepo, efCtx, "reads / writes WorkItems via")
```

The diagram makes four architectural commitments visible:

1. **Single capture seam** — `WorkItemService.RefreshWorkItems` is the only mutator of `WorkItem.CurrentStateEnteredAt` and the only writer of `WorkItemStateTransition` rows (ADR-016 + ADR-017; ArchUnitNET-enforced).
2. **Capability-flagged dispatch** — `IWorkTrackingConnector.SupportsTransitionHistory` is the single branch point. Adding a 5th connector means implementing the interface and setting the flag — zero touches to `WorkItemService.RefreshWorkItems` (ADR-017).
3. **Standalone transition entity** — `WorkItem` holds NO transition navigation; the work-item-table read path loads zero transition rows (ADR-015; ArchUnitNET-enforced).
4. **Consumer-facing surface is the repository, not a service** — sibling MVP DESIGNs (`aging-pace-percentiles`, `state-time-cumulative-view`) consume `IWorkItemStateTransitionRepository` directly. No shared `IPerStateAggregationService` is introduced (ADR-018; rationale documented for future readers).

---

# C4 Architecture Diagrams — aging-pace-percentiles

Feature: aging-pace-percentiles (Epic 4144 MVP bundle, slice F)
Wave: DESIGN
Date: 2026-05-24
Architect: Morgan (Solution Architect), interaction mode = PROPOSE

---

## C4 Level 1 — System Context (delta)

The Lighthouse system context (RBAC + OAuth + filter-forecast-throughput + time-in-state-and-staleness baselines) is unchanged. This feature adds **no** new external actors and **no** new external systems. The `Flow Coach` persona already introduced by sibling 1 retains the same chart-glance relationship with Lighthouse; the secondary `Delivery Forecaster` persona (already present in `docs/product/personas/`) consumes the same chart in forecast-conversation contexts. The chart-glance question this feature enables ("which in-flight items are pacing slower than 85% of historical items for their current state?") sits inside the existing read-only relationship; no new outbound integration.

No L1 diagram is added — the System Context from sibling 1 (time-in-state-and-staleness) covers this feature's actors and systems unchanged.

---

## C4 Level 2 — Container (delta)

No new containers. The existing Frontend SPA and Backend API gain additive responsibilities — Backend API gains two new endpoints (team + portfolio); Frontend SPA gains a new chart overlay, a new legend chip group, and a new per-dot tooltip annotation. The Database container is unchanged — no schema additions (sibling 1's `WorkItemStateTransitions` table and `WorkItem.CurrentStateEnteredAt` column are consumed read-only). The E2E Test Runner gains one new spec.

```mermaid
C4Container
    title Container Diagram — Aging Pace Percentiles (delta over time-in-state-and-staleness baseline)

    Person(coach, "Flow Coach", "Reads team/portfolio Work Item Aging chart; spots pace outliers via per-state bands")
    Person(forecaster, "Delivery Forecaster", "Reads the same chart in forecast-conversation contexts")

    Container(spa, "Frontend SPA", "React 18 + TypeScript + MUI + MUI-X-charts", "Existing WorkItemAgingChart extended with per-state SVG band overlay. PercentileLegend extended with second chip group. useMetricsData hook extended with parallel fetch. New tooltip annotation on chart dots.")
    Container(api, "Backend API", "C# .NET 8 ASP.NET Core Web API", "New endpoints: GET /api/teams/{id}/metrics/ageInStatePercentiles + portfolio equivalent. Computation inside TeamMetricsService / PortfolioMetricsService, delegating to protected helper on BaseMetricsService. Reads WorkItemStateTransition rows via sibling 1's IWorkItemStateTransitionRepository. Reuses PercentileCalculator + GetWorkItemsClosedInDateRange. Cache via existing GetFromCacheIfExists.")
    ContainerDb(db, "Database", "SQLite (dev/test) / PostgreSQL (prod) via EF Core", "NO schema changes. Reads sibling 1's WorkItemStateTransitions table + WorkItem.CurrentStateEnteredAt column.")
    Container(e2e, "E2E Test Runner", "Playwright + TypeScript", "One new spec covers happy-path chart-rendering + legend chip toggle. Per-state algorithm correctness lives in NUnit (faster).")

    Rel(coach, spa, "Reads aging chart with per-state bands via", "HTTPS")
    Rel(forecaster, spa, "Reads aging chart with per-state bands via", "HTTPS")
    Rel(spa, api, "GET /api/teams/{id}/metrics/ageInStatePercentiles, GET /api/portfolios/{id}/metrics/ageInStatePercentiles, (existing) GET /api/teams/{id}/metrics/cycleTimePercentiles", "HTTPS / JSON")
    Rel(api, db, "Reads WorkItemStateTransition + WorkItem via", "EF Core")
    Rel(e2e, spa, "Drives browser interactions against", "Playwright CDP")
    Rel(e2e, api, "Calls API helpers for test setup against", "HTTPS / JSON")
```

---

## C4 Level 3 — Component: Per-State Percentile Computation and Chart-Overlay Domain

The per-state percentile computation (ADR-019) + the SVG-overlay chart rendering (ADR-020) + the ADR-018 disposition (ADR-021) are the three architecturally significant decisions for this feature. This diagram makes the consumer-side surfaces explicit: the per-state computation flows through the existing `BaseMetricsService` inheritance; the chart-overlay flows through the existing `<ChartsContainer>` coordinate system; the repository seam (sibling 1's `IWorkItemStateTransitionRepository`) is the only data-layer touchpoint.

```mermaid
C4Component
    title Component Diagram — Aging Pace Percentiles (Backend + Frontend)

    Container_Boundary(api, "Backend API") {
        Component(teamMetricsCtrl, "TeamMetricsController (existing, EXTENDED)", "ASP.NET Core ApiController", "GET /metrics/cycleTimePercentiles unchanged. New: GET /metrics/ageInStatePercentiles?startDate&endDate. Validation + auth mirror cycleTimePercentiles. RbacGuard(TeamRead).")
        Component(portfolioMetricsCtrl, "PortfolioMetricsController (existing, EXTENDED)", "ASP.NET Core ApiController", "Mirror endpoint for portfolio scope. RbacGuard(PortfolioRead).")
        Component(teamMetricsSvc, "TeamMetricsService (existing, EXTENDED)", "C# class", "New method GetAgeInStatePercentilesForTeam: scope-specific 'completed items in window' query via existing GetWorkItemsClosedInDateRange, then delegates to BaseMetricsService.ComputeAgeInStatePercentiles, wraps in GetFromCacheIfExists with cache key AgeInStatePercentiles_{startDate}_{endDate}.")
        Component(portfolioMetricsSvc, "PortfolioMetricsService (existing, EXTENDED)", "C# class", "Mirror method for portfolio scope.")
        Component(baseMetricsSvc, "BaseMetricsService (existing, EXTENDED)", "C# class", "New PROTECTED helper ComputeAgeInStatePercentiles(completedItems, doingStates, requestedPercentiles): walks transitions via IWorkItemStateTransitionRepository.GetAllByPredicate, pairs entry→next-exit per state per ADR-019, buckets observations, calls PercentileCalculator per state per percentile. INTRA-INHERITANCE ONLY — not exposed via interface (ADR-021).")
        Component(percentileCalc, "PercentileCalculator (existing, REUSE-AS-IS)", "C# static class", "Nearest-rank algorithm with clamp. Same function used by cycleTimePercentiles — algorithmic parity per ADR-019.")
        Component(transitionRepo, "IWorkItemStateTransitionRepository (sibling 1, REUSE-AS-IS)", "C# port", "Consumed via GetAllByPredicate. No new methods added to the repository — DESIGN sibling 1 explicitly deferred extension to consumer DESIGNs; this DESIGN chose not to extend.")
        Component(workItemRepo, "IWorkItemRepository (existing, REUSE-AS-IS)", "C# port", "Used to resolve the team/portfolio's completed items in window via the existing GetWorkItemsClosedInDateRange predicate.")
        Component(ageInStateDto, "AgeInStatePercentilesDto (NEW)", "C# record", "record AgeInStatePercentilesDto(string State, int SampleSize, IReadOnlyList<PercentileValue> Percentiles). Returned by both endpoints as IReadOnlyList<>.")
        Component(percentileValue, "PercentileValue (existing, REUSE-AS-IS)", "C# class", "{ Percentile, Value }. Same type used by cycleTimePercentiles response.")
    }

    Container_Boundary(spa, "Frontend SPA") {
        Component(metricsServiceTs, "MetricsService.ts (existing, EXTENDED)", "TypeScript HTTP adapter", "New method getAgeInStatePercentiles(id, startDate, endDate) — mirrors getCycleTimePercentiles shape. Added to IMetricsService interface.")
        Component(useMetricsData, "useMetricsData (existing, EXTENDED)", "React hook", "Parallel fetch of ageInStatePercentiles alongside existing cycleTimePercentiles. New ctx field perStatePercentileValues.")
        Component(baseMetricsView, "BaseMetricsView (existing, EXTENDED)", "React component", "Passes perStatePercentileValues={ctx.perStatePercentileValues} to <WorkItemAgingChart>. Shared between team and portfolio routes.")
        Component(agingChart, "WorkItemAgingChart (existing, EXTENDED)", "React component", "New optional prop perStatePercentileValues. Custom SVG <line> overlay inside <ChartsContainer> per ADR-020 — anchored to state column index, dashed style matching CT bands. New tooltip annotation per dot per US-03 (client-side bucket computation from daysInState + per-state values).")
        Component(percentileLegend, "PercentileLegend (existing, EXTENDED)", "React component", "Renders SECOND chip group `Age-in-State %iles (per state)` alongside existing `Cycle Time %iles (overall)` group. Independent toggle state.")
        Component(useChartVis, "useChartVisibility (existing, EXTENDED)", "React hook", "Manages two independent visiblePercentiles maps (either signature-extended OR invoked twice — DDD-8).")
        Component(perStateModel, "IPerStatePercentileValues TS model (NEW)", "TypeScript interface", "{ state, sampleSize, percentiles: IPercentileValue[] }. Mirrors backend DTO.")
    }

    Rel(teamMetricsCtrl, teamMetricsSvc, "delegates to via GetEntityByIdAnExecuteAction")
    Rel(portfolioMetricsCtrl, portfolioMetricsSvc, "delegates to via GetEntityByIdAnExecuteAction")
    Rel(teamMetricsSvc, baseMetricsSvc, "calls ComputeAgeInStatePercentiles (inherited protected) via")
    Rel(portfolioMetricsSvc, baseMetricsSvc, "calls ComputeAgeInStatePercentiles (inherited protected) via")
    Rel(teamMetricsSvc, workItemRepo, "resolves completed-items-in-window via GetWorkItemsClosedInDateRange")
    Rel(portfolioMetricsSvc, workItemRepo, "resolves completed-items-in-window via")
    Rel(baseMetricsSvc, transitionRepo, "walks transitions via GetAllByPredicate")
    Rel(baseMetricsSvc, percentileCalc, "computes per-state percentiles via CalculatePercentile")
    Rel(baseMetricsSvc, percentileValue, "constructs response elements via")
    Rel(teamMetricsSvc, ageInStateDto, "projects response via")
    Rel(portfolioMetricsSvc, ageInStateDto, "projects response via")

    Rel(useMetricsData, metricsServiceTs, "fetches via getAgeInStatePercentiles + (existing) getCycleTimePercentiles in parallel")
    Rel(baseMetricsView, agingChart, "passes perStatePercentileValues prop to")
    Rel(agingChart, percentileLegend, "renders with two chip-group prop sets")
    Rel(agingChart, useChartVis, "manages two independent visibility maps via")
    Rel(useMetricsData, perStateModel, "populates ctx field as IPerStatePercentileValues[]")
    Rel(agingChart, perStateModel, "consumes via new prop")
```

The diagram makes four architectural commitments visible:

1. **Repository-only data seam** — `BaseMetricsService.ComputeAgeInStatePercentiles` reads transitions exclusively via sibling 1's `IWorkItemStateTransitionRepository` (`GetAllByPredicate`). No direct `DbSet<WorkItemStateTransition>` access; no raw SQL. ArchUnitNET-enforced (ADR-021 extending the ADR-015 rule).
2. **Inheritance-bound computation, not a new service** — the per-state walk lives as a `protected` helper inside the existing `BaseMetricsService`, consumed only by the two existing derived classes. No new interface, no new service, no `IPerStateAggregationService`. ArchUnitNET + NUnit-reflection-enforced (ADR-021).
3. **Algorithmic parity with `cycleTimePercentiles`** — same `PercentileCalculator`, same `GetWorkItemsClosedInDateRange` predicate, same `GetFromCacheIfExists` cache mechanism, same `PercentileValue` response type. NUnit-enforced (ADR-019).
4. **Backwards-compatible chart extension** — `WorkItemAgingChart` with `perStatePercentileValues` undefined renders identically to today; the per-state band SVG overlay is rendered inside the existing `<ChartsContainer>` coordinate system, not absolute-positioned over the chart. Vitest-enforced (ADR-020).

