# Security Review 2026-05 — Follow-up

Author date: 2026-05-12 | Origin: gaps surfaced during operator validation of `security-review-2026-05` immediately after merge to `main` (commit `d9418154`).

## Wave: DISCUSS / [REF] Persona ID

- **Secops-aware maintainer** (carry-over from `security-review-2026-05`) — operating Lighthouse with auth + RBAC enabled, wants to actually USE the per-key API-key scoping feature delivered by S-5 and finds it (a) not exposed in the UI, (b) silently broken for owners whose access is group-mapped rather than explicit.

## Wave: DISCUSS / [REF] JTBD one-liner

When I create an API key as an admin whose access is granted via OIDC group claims, I want both (a) the UI to let me restrict the key's scope and (b) the key to inherit my actual effective permissions, so the scoped-key feature is usable end-to-end rather than backend-only.

## Wave: DISCUSS / [REF] Pre-requisites

- `security-review-2026-05/feature-delta.md` — DISCUSS S-5 elevator pitch + DESIGN D5 + ADR-004 are the source of the contract this follow-up completes.
- Source-verified gaps:
  - **Frontend gap**: `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx:55` calls `apiKeyService.createApiKey({name, description})` only. `Lighthouse.Frontend/src/services/Api/ApiKeyService.ts:10,22` declares `createApiKey(request: ICreateApiKeyRequest)` with no `scope` field. `Lighthouse.Frontend/src/models/ApiKey/ApiKey.ts` has no scope concept.
  - **Backend gap**: `Lighthouse.Backend/Services/Implementation/Authorization/RbacAdministrationService.cs:1034-1066` `GetVirtualPermissionsAsync` reads the `groups` claim from the principal. The API-key path's `ApiKeyAuthenticationHandler` at `Lighthouse.Backend/Services/Implementation/Auth/ApiKeyAuthenticationHandler.cs:50-71` emits `sub`, `name`, `auth_method`, `api_key_id` — but no `groups` claim. Owners whose RBAC role exists only via `RbacGroupMapping` (group-mapped, no explicit `UserPermission` row) get empty virtual permissions on the API-key path; legacy "key inherits owner" promise silently fails.

## Wave: DISCUSS / [REF] Severity legend

Carries over from parent feature: **VULN** / **GAP-HIGH** / **GAP-MED** / **OK**. Both items in this follow-up are GAP-HIGH (parent feature is operationally unusable for the documented use case until fixed).

## Wave: DISCUSS / [REF] Locked decisions

- **[D1]** Snapshot the owner's group-claim values on the `UserProfile` at OIDC sign-in time and replay them as virtual permissions on the API-key path. Reasoning: preserves the "key inherits owner permissions" contract from ADR-004 without forcing operators to mirror every group-mapped grant as an explicit `UserPermission` row. Trade-off: snapshot is stale until next OIDC sign-in — same caveat already documented in CRA technical file §5.3 for RBAC eventual consistency. Alternative weighed and rejected: materialise full effective permissions onto `ApiKeyPermission` at key-create time even when no `scope` array is sent. Rejected because it (a) breaks the "no rows = inherit" backwards-compat path documented in S-5.4, (b) freezes the key against owner-permission growth.
- **[D2]** Frontend scope UI lives inside the existing `CreateApiKeyDialog`. No new page. Scope rows are an optional collapsible section ("Restrict scope (optional)") below name/description; default-collapsed; if expanded, the user can add 0..N scope entries. Each entry is `{role, scopeType, scopeId}` chosen from dropdowns populated from existing `getTeams()` / `getPortfolios()` data. Reasoning: minimises blast radius and matches the existing dialog convention used elsewhere.
- **[D3]** The new snapshot field on `UserProfile` is named `LastKnownGroupClaimValues` (JSON-serialized `string?` containing a list of strings). Nullable because users who pre-date the change have no snapshot yet, and the fallback resolves to empty (same behaviour as today).

## Wave: DISCUSS / [REF] User stories

### F-FE-1 — Frontend scope UI for API-key creation

**As an** admin creating an API key
**I want** to optionally restrict the key's scope to specific teams or portfolios
**So that** I can issue least-privilege keys for integrations without resorting to direct API calls.

#### Elevator Pitch
Before: clicking "Create API key" only lets me set name + description; if I want a scoped key I have to `curl POST /api/apikeys` by hand.
After: clicking "Create API key" shows an optional "Restrict scope" section; expanding it lets me add rows like `PortfolioRead on Portfolio "Roadmap 2026"`; clicking Create issues the key with those scopes persisted.
Decision enabled: I can confidently delegate an API key to a CI script knowing it can only read the one portfolio it needs.

#### Acceptance Criteria
- AC-1: Given the Create API Key dialog is open and "Restrict scope" is collapsed, when I click Create, then `POST /api/apikeys` is called with `{name, description}` and no `scope` field — backwards-compatible default.
- AC-2: Given I expand "Restrict scope" and add a row `{role: PortfolioRead, target: Portfolio "Roadmap 2026"}`, when I click Create, then `POST /api/apikeys` is called with `scope: [{role: "PortfolioRead", scopeType: "Portfolio", scopeId: <Roadmap 2026 id>}]`.
- AC-3: Given two scope rows are added, when I click Create, then both are sent in the `scope` array.
- AC-4: Given the backend returns 403 (issue-time superset rejection), when the response is received, then the dialog stays open and displays the error message from the response body.
- AC-5: Given any row is incomplete (role chosen but no scopeId target), when I click Create, then the Create button is disabled OR the row's fields are flagged invalid — server is not called.

### F-BE-1 — Group-claim inheritance for API keys

**As an** owner whose RBAC role is granted via OIDC group mapping
**I want** an API key I create to see the same teams and portfolios I see in the UI
**So that** the "key inherits owner permissions" promise holds for me too, not just for owners with explicit grants.

#### Elevator Pitch
Before: I am a TeamAdmin on Team Alpha via group mapping (no explicit `UserPermission` row); I create an unscoped API key; `GET /api/v1/teams` with that key returns `[]`.
After: same setup, same call — returns `[{Team Alpha}]`. The snapshot of my group-claim values taken at my last OIDC sign-in is replayed when the API key authenticates.
Decision enabled: operators can rely on group-driven RBAC without having to mirror grants as explicit user permissions just to make API keys work.

#### Acceptance Criteria
- AC-1: Given an owner whose only path to a team is via a `RbacGroupMapping` (Role=Viewer or higher, ScopeType=Team), when that owner signs in via OIDC, then their `UserProfile.LastKnownGroupClaimValues` field is updated to the group claim values present in the validated token.
- AC-2: Given a key whose owner has `LastKnownGroupClaimValues = ["team-alpha-group"]` AND a `RbacGroupMapping(GroupValue="team-alpha-group", Role=Viewer, ScopeType=Team, ScopeId=42)` exists, when an API call uses the key and hits `GET /api/v1/teams`, then the response includes team 42.
- AC-3: Given the same key but the `RbacGroupMapping` for `team-alpha-group` is deleted, when an API call uses the key, then team 42 is NO LONGER returned (group snapshot is replayed against current mappings, not cached resolutions).
- AC-4: Given an owner with both an explicit `UserPermission(Role=TeamAdmin, ScopeId=42)` AND a group-mapped lower role on the same team, when an API key call resolves permissions, then the highest-priority role wins (same precedence rule as `RbacAdministrationService.ToHighestRoleMap`).
- AC-5: Given an owner who has NEVER signed in via OIDC (`LastKnownGroupClaimValues = null`), when an API key authenticates as that owner, then virtual permissions resolve to empty (current behaviour, no regression).
- AC-6: Given a scoped key (`ApiKeyPermission` rows present), when the call resolves permissions, then the intersection still applies — group-snapshot virtual permissions feed the owner side of the intersection, not the key side. Backwards-compat with S-5's existing scoped-key behaviour preserved.

## Wave: DISCUSS / [REF] Definition of Done

1. Both stories' ACs covered by NUnit integration tests (backend) + Vitest tests (frontend), all GREEN.
2. EF migration generated via `Create-Migration.ps1` for both SQLite and Postgres.
3. `dotnet build` and `pnpm build` both 0/0.
4. SonarCloud `new_violations = 0` on backend + frontend projects on next push.
5. CRA technical file §5.3 updated to note the group-snapshot staleness window (it's the same window as the existing residual risk, but applied to a new mechanism).

## Wave: DISCUSS / [REF] Out-of-scope

- Live IdP group lookup at API-key authentication time (would require client-credentials flow to the IdP per request; rejected as heavyweight and IdP-specific).
- UI for displaying or editing the snapshot (operators don't need this — it's an internal optimisation).
- Snapshot refresh outside the OIDC login flow (e.g. background sync). Snapshot is updated only at sign-in; this is the contract.
- Frontend edit-after-create for API key scope (S-5 already established keys are immutable post-creation; users delete and recreate).

## Wave: DISCUSS / [REF] WS strategy

**A — Pure brownfield**. Both stories are single-PR diffs against existing components. No walking skeleton required (the parent feature already established the e2e path through the API-key authentication handler).

## Wave: DISCUSS / [REF] Driving ports

- HTTP — `POST /api/apikeys` (unchanged route, extended request body for F-FE-1; same handler path for F-BE-1's group-snapshot logic).
- OIDC sign-in callback path — `/api/auth/callback` (handler hook for F-BE-1's snapshot write).
- Frontend — `CreateApiKeyDialog` component in `ApiKeysSettings.tsx`.

---

## Wave: DESIGN / [REF] DDD list

- **D1** Add nullable `LastKnownGroupClaimValues TEXT` column to `UserProfile`. JSON-serialised list of strings. Migration via `Create-Migration.ps1`. Reasoning: smallest schema change; mirrors how Lighthouse already stores small structured blobs (e.g. work-tracking connection options).
- **D2** Write the snapshot from a hook on OIDC token validation. Choose the lowest-blast-radius extension point: `OpenIdConnectEvents.OnTokenValidated` registered in `Program.cs` where the OIDC scheme is configured (around `Program.cs:400-420`). Reasoning: this event fires after the IdP token is validated and claims are available, before the cookie is issued — the right moment to capture groups.
- **D3** Modify `RbacAdministrationService.GetVirtualPermissionsAsync`: when the principal has no `groups` claim but DOES have `api_key_id`, fall back to reading `currentUser.LastKnownGroupClaimValues` and synthesise the equivalent group-value set. Reasoning: keeps the fallback localised to one place; preserves all existing behaviour for non-API-key requests.
- **D4** Frontend: extend `ICreateApiKeyRequest` with `scope?: IApiKeyScope[]` and add `IApiKeyScope` type. Update `apiKeyService.createApiKey` to forward the optional field. Reasoning: smallest TypeScript surface change.
- **D5** Frontend dialog: add a collapsible `<Accordion>` "Restrict scope (optional)" section below the description field. Inside, render a `ScopeRowList` component with add/remove row controls. Each row: three `<Select>` controls — role, scope type, scope target. Reasoning: matches existing MUI patterns in the codebase; reuses the same dropdown UX as Team/Portfolio assignment in `ScopedGroupMappingManager.tsx`.
- **D6** Issue-time superset check on the server side is **unchanged** — already implemented in S-5. F-FE-1 just gives the user a way to send the scope array; the server validates it. Reasoning: avoid duplicating the security logic on the client; the client-side validation is purely for UX (preventing obviously-invalid submissions like incomplete rows).
- **D7** No new ADR. ADR-004 already covers the parallel `ApiKeyPermission` table; ADR-006 already covers the connection-list payload scoping that exercises `AnyScopedAdmin`. The group-snapshot mechanism is a small addition to the existing virtual-permissions path; documenting it inline in the architecture brief's `## Application Architecture` section suffices.

## Wave: DESIGN / [REF] Component decomposition

| Component | File | Change Type | Change Summary |
|---|---|---|---|
| UserProfile | `Lighthouse.Backend/Models/Auth/UserProfile.cs` | EXTEND | Add `string? LastKnownGroupClaimValues { get; set; }` |
| LighthouseAppContext | `Lighthouse.Backend/Data/LighthouseAppContext.cs` | EXTEND | Map the new column; no index needed (read only via UserProfile primary key lookup) |
| EF migrations | `Lighthouse.Migrations.{Sqlite,Postgres}/Migrations/` | NEW | Generated via `Create-Migration.ps1 -MigrationName AddUserProfileGroupSnapshot` |
| OIDC sign-in hook | `Lighthouse.Backend/Program.cs` | EXTEND | In the OIDC scheme registration, add `OnTokenValidated` handler that writes the snapshot |
| Group-snapshot writer | `Lighthouse.Backend/Services/Implementation/Auth/OidcGroupSnapshotWriter.cs` | NEW | Small service responsible for serialising group claim values and updating the UserProfile row. Extracted so the OIDC hook stays a one-liner. |
| IOidcGroupSnapshotWriter | `Lighthouse.Backend/Services/Interfaces/Auth/IOidcGroupSnapshotWriter.cs` | NEW | Inbound port for the snapshot writer |
| RbacAdministrationService | `Lighthouse.Backend/Services/Implementation/Authorization/RbacAdministrationService.cs` | EXTEND | `GetVirtualPermissionsAsync` fallback to UserProfile snapshot when `api_key_id` present and no `groups` claim |
| ApiKey TypeScript model | `Lighthouse.Frontend/src/models/ApiKey/ApiKey.ts` | EXTEND | Add `IApiKeyScope` interface; add `scope?: IApiKeyScope[]` to `ICreateApiKeyRequest` |
| ApiKeyService.ts | `Lighthouse.Frontend/src/services/Api/ApiKeyService.ts` | EXTEND | Forward optional `scope` field on the create call (already typed; just pass through) |
| ApiKeysSettings.tsx — `CreateApiKeyDialog` | `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx` | EXTEND | Add "Restrict scope (optional)" Accordion with scope-row builder |
| ScopeRowList.tsx | `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ScopeRowList.tsx` | NEW | Self-contained sub-component: list of rows, add/remove buttons, per-row dropdowns |

## Wave: DESIGN / [REF] Driving ports

| Method | Route | Auth Requirement | Change |
|---|---|---|---|
| POST | `/api/apikeys` | Authenticated; rate-limited via S-6's `ApiKeys` policy | Body `Scope` field already accepted from S-5; no API-shape change for F-FE-1 |
| GET | `/api/v1/teams` | Authenticated | No change; F-BE-1 fixes underlying permission resolution so the existing endpoint returns correctly |
| GET | `/api/v1/portfolios` | Authenticated | Same as above |
| OIDC callback | `/api/auth/callback` | OIDC middleware-owned | F-BE-1 adds an `OnTokenValidated` hook that writes the group snapshot |

## Wave: DESIGN / [REF] Driven ports + adapters

- `LighthouseDbContext` driven port — extended with `UserProfile.LastKnownGroupClaimValues` column write/read. No new driven adapter.
- OIDC middleware — `OpenIdConnectEvents.OnTokenValidated` is the extension point. Implementation hooks into the existing driven adapter.

## Wave: DESIGN / [REF] Technology choices

No new technologies. Reuses .NET 8 ASP.NET Core OIDC middleware, EF Core 8, React 18 + Material UI, NUnit + Moq + Vitest. Same stack as the parent feature.

## Wave: DESIGN / [REF] Reuse Analysis

| Existing Component | File | Overlap | Decision | Justification |
|---|---|---|---|---|
| UserProfile | `Models/Auth/UserProfile.cs` | User-identity entity | EXTEND | Adding a single nullable column to the existing entity; mirrors the established pattern (e.g. `DisplayName`, `StableSubject` already on this entity). No reason to create a sibling table. |
| RbacAdministrationService.GetVirtualPermissionsAsync | `Services/Implementation/Authorization/RbacAdministrationService.cs:1034-1066` | Group-to-permission resolution | EXTEND | Existing method is the single inbound point for group-claim-to-permission mapping. Adding the API-key fallback inside it keeps the resolution rule in one file. |
| Program.cs OIDC scheme registration | `Program.cs:400-420` | OIDC pipeline | EXTEND | Existing `AddOpenIdConnect(options => { ... })` block already configures the scheme; adding `options.Events.OnTokenValidated` here is the conventional ASP.NET extension point. |
| ICreateApiKeyRequest | `Frontend/src/models/ApiKey/ApiKey.ts` | API key creation DTO | EXTEND | One optional field added; existing consumers (callers without scope) unaffected. |
| CreateApiKeyDialog | `Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx` | API key creation UX | EXTEND | Existing dialog component; adding a collapsible section preserves the current UX for users who don't want scopes. |
| ScopeRowList | `Frontend/src/pages/Settings/ApiKeys/ScopeRowList.tsx` | New scope-row builder | CREATE NEW | No existing component manages a list of `{role, scopeType, scopeId}` rows. The closest analog is `ScopedGroupMappingManager` but that's bound to a specific scope context (a team or portfolio) and emits group-mapping CRUD, not a scope-row array. Building a small focused component is cleaner than overloading the manager. ~80 LOC. |
| OidcGroupSnapshotWriter | `Backend/Services/Implementation/Auth/OidcGroupSnapshotWriter.cs` | Group-claim snapshot persistence | CREATE NEW | No existing service writes to UserProfile from the OIDC callback. Could be inlined in `Program.cs` but extracting keeps the hook a one-liner and gives the test surface an interface to mock. ~40 LOC. |

## Wave: DESIGN / [REF] Decisions table

| ID | Decision | Source |
|---|---|---|
| D1 | UserProfile gains `LastKnownGroupClaimValues TEXT` nullable | This document |
| D2 | Snapshot written from `OpenIdConnectEvents.OnTokenValidated` | This document |
| D3 | `GetVirtualPermissionsAsync` falls back to snapshot when api_key_id present, no groups claim | This document |
| D4 | `ICreateApiKeyRequest.scope?: IApiKeyScope[]` | This document |
| D5 | Dialog gets collapsed "Restrict scope" accordion, default-collapsed | This document |
| D6 | Server-side issue-time superset check unchanged (already in S-5) | ADR-004, parent feature |
| D7 | No new ADR; mechanism documented inline | This document |

## Wave: DESIGN / [REF] Open questions

- **OQ-1**: Should the snapshot also be wiped when the owner is removed from RBAC entirely (hard-delete UserProfile)? Likely yes via existing cascade. Verify in DELIVER.
- **OQ-2**: Should the API-key list UI show the configured scope of each key (read-only display, not edit)? Out of scope for this follow-up; track for v3.

---

## Wave: DISTILL / [REF] WS strategy reconciliation

Parent feature established the end-to-end path (UI → API → RBAC → DB) for API-key authentication and scoped permissions. Both follow-up stories extend that established path:

- **F-FE-1** extends `CreateApiKeyDialog` and `apiKeyService` — the request shape and server handler already exist (S-5 already accepts a `Scope` array). No new vertical slice is introduced; the dialog gains a new optional control.
- **F-BE-1** extends `RbacAdministrationService.GetVirtualPermissionsAsync` and adds a write hook in `OnTokenValidated`. The driving port (`GET /api/v1/teams` under API-key auth) is already exercised by S-5 scenarios.

Per WS strategy A confirmed in DISCUSS, no walking-skeleton anchor scenario is required for this follow-up. Each story gets focused behaviour scenarios that enter through the existing driving ports.

## Wave: DISTILL / [REF] Scenario list with tags

All backend scenarios use real EF InMemory (`@real-io`) via `TestWebApplicationFactory`. All frontend scenarios use the real React tree (`@real-io`) via `@testing-library/react` and mock only the `apiKeyService` boundary (which is itself a thin HTTP shim — mocking it is the standard pattern established by `ApiKeysSettings.test.tsx`).

### F-FE-1 — Frontend scope UI (Vitest)

```gherkin
@F-FE-1 @real-io
Scenario: FE-1.1 — Admin creates a key without restricting scope
  Given the Create API Key dialog is open
  And "Restrict scope" is collapsed (default)
  And a name "ci-key" and description "for ci" have been entered
  When the admin clicks Create
  Then the api key service is invoked with name "ci-key", description "for ci", and no scope field

@F-FE-1 @real-io
Scenario: FE-1.2 — Admin restricts a key to a single portfolio
  Given the Create API Key dialog is open
  And "Restrict scope" has been expanded
  And one scope row has been added with role "PortfolioRead", scope type "Portfolio", and portfolio "Roadmap 2026" (id 7)
  And a name "roadmap-ci" has been entered
  When the admin clicks Create
  Then the api key service is invoked with a scope array of exactly one entry { role: "PortfolioRead", scopeType: "Portfolio", scopeId: 7 }

@F-FE-1 @real-io
Scenario: FE-1.3 — Admin restricts a key to two distinct scopes
  Given the Create API Key dialog is open
  And "Restrict scope" has been expanded
  And two scope rows have been added (one Team-scoped, one Portfolio-scoped) with complete role + target selections
  And a name "multi-scope-key" has been entered
  When the admin clicks Create
  Then the api key service is invoked with a scope array of length two containing both entries in the order added

@F-FE-1 @real-io @error
Scenario: FE-1.4 — Server rejects scope as exceeding caller's permissions
  Given the Create API Key dialog is open with one complete scope row
  And the api key service is configured to reject creation with status 403 and body "Cannot issue API key with scope exceeding caller's permissions"
  When the admin clicks Create
  Then the dialog remains open
  And the response message "Cannot issue API key with scope exceeding caller's permissions" is displayed to the admin
  And no key-reveal panel is shown

@F-FE-1 @real-io @error
Scenario: FE-1.5 — Incomplete scope row blocks submission
  Given the Create API Key dialog is open
  And "Restrict scope" has been expanded
  And one scope row has been added with a role chosen but no scope target selected
  And a name "incomplete-key" has been entered
  When the admin inspects the Create button
  Then the Create button is disabled
  And the api key service is never invoked
```

### F-BE-1 — Backend group-snapshot inheritance (NUnit)

```gherkin
@F-BE-1 @real-io
Scenario: BE-1.1 — Snapshot writer persists group claim values on the user profile
  Given a fresh database containing a UserProfile with Subject "subject-1" and LastKnownGroupClaimValues null
  When OidcGroupSnapshotWriter.WriteAsync is invoked with subject "subject-1" and group values ["team-alpha-group", "viewers"]
  Then the persisted UserProfile row for "subject-1" has LastKnownGroupClaimValues equal to the JSON-serialised list ["team-alpha-group","viewers"]

@F-BE-1 @real-io
Scenario: BE-1.2 — Group-mapped owner sees their team through an unscoped API key
  Given a UserProfile owner with LastKnownGroupClaimValues = ["team-alpha-group"]
  And a RbacGroupMapping mapping "team-alpha-group" to Viewer on Team 42
  And an unscoped API key issued to that owner
  When the API key is used to request the team list
  Then the team list response is successful
  And team 42 appears in the response

@F-BE-1 @real-io
Scenario: BE-1.3 — Removing the underlying group mapping revokes access on the next call
  Given the BE-1.2 setup but with the RbacGroupMapping for "team-alpha-group" removed before the request
  When the same API key is used to request the team list
  Then the response is successful
  And team 42 is NOT in the response

@F-BE-1 @real-io
Scenario: BE-1.4 — Explicit TeamAdmin permission wins over a snapshot-derived Viewer
  Given a UserProfile owner with an explicit UserPermission(TeamAdmin, Team 42)
  And the same owner has LastKnownGroupClaimValues = ["viewers"]
  And a RbacGroupMapping mapping "viewers" to Viewer on Team 42
  When an API key call for that owner resolves permissions on Team 42
  Then the effective role on Team 42 is TeamAdmin

@F-BE-1 @real-io @error
Scenario: BE-1.5 — Owner with no snapshot resolves only explicit grants (no regression)
  Given a UserProfile owner with LastKnownGroupClaimValues = null
  And the owner has no explicit RBAC grants for Team 42
  And a RbacGroupMapping mapping "team-alpha-group" to Viewer on Team 42 also exists in the database
  When an API key issued to that owner is used to request the team list
  Then the team list response is successful
  And team 42 is NOT in the response

@F-BE-1 @real-io
Scenario: BE-1.6 — Snapshot-derived virtual permissions feed the owner side of the scoped-key intersection
  Given a UserProfile owner with LastKnownGroupClaimValues = ["team-alpha-group"]
  And a RbacGroupMapping mapping "team-alpha-group" to Viewer on Team 42
  And a SCOPED API key for that owner with one ApiKeyPermission(TeamRead, Team 42)
  When the scoped API key is used to request the team list
  Then the team list response is successful
  And team 42 appears in the response (owner side covers the key's TeamRead/42 scope)
```

## Wave: DISTILL / [REF] Adapter coverage table

Per Mandate 6 (Hexagonal Boundary Enforcement, Adapter Integration Coverage). For every driven adapter touched by this feature, at least one scenario exercises it through real I/O or via the existing in-memory EF Core adapter used in parent feature tests (which IS the production adapter in test environment).

| Adapter | Real-I/O exercised | Covered by |
|---|---|---|
| `LighthouseAppContext` (EF Core, SQLite per `TestWebApplicationFactory`) | YES | every BE scenario |
| `RbacAdministrationService` (real production implementation, NOT `ClaimsDrivenRbacAdministrationService` double) | YES | BE-1.2, BE-1.3, BE-1.4, BE-1.5, BE-1.6 |
| `OidcGroupSnapshotWriter` (real implementation writing through EF Core) | YES | BE-1.1 |
| OIDC `OnTokenValidated` middleware hook | indirect | BE-1.1 covers the write path the hook will invoke; full middleware round-trip is not unit-testable here and is deferred to manual operator verification (same pattern as parent feature's Playwright auth tests) |
| `apiKeyService` HTTP shim | mocked at the boundary | every FE scenario (mock the service, assert call args) |
| `CreateApiKeyDialog` real React component tree | YES | every FE scenario |
| `ScopeRowList` real React component tree | YES | FE-1.2, FE-1.3, FE-1.5 |

Documented gap: full OIDC callback round-trip (Keycloak → middleware → `OnTokenValidated` → snapshot writer) is not covered by automated tests. The write-side service (BE-1.1) and the read-side resolution (BE-1.2..BE-1.6) sandwich the hook on both sides; the hook itself is a one-line wiring in `Program.cs` and is verified manually per parent-feature convention.

## Wave: DISTILL / [REF] Driving-port coverage

| Driving port | Test coverage |
|---|---|
| HTTP `POST /api/v1/apikeys` | Indirectly through FE scenarios at the `apiKeyService` boundary. The HTTP contract itself is already covered by parent feature's S5 scenarios. No new request-shape scenario added here because S-5 already accepts the `Scope` array; F-FE-1 only enables the UI to emit it. |
| HTTP `GET /api/v1/teams` | BE-1.2, BE-1.3, BE-1.5, BE-1.6 enter through this driving port via `WithApiKey(plainTextKey)`. |
| OIDC `/api/auth/callback` | Not exercised end-to-end (see adapter coverage table). The write-side behaviour the callback triggers is verified at the service-port level in BE-1.1. |
| Frontend `CreateApiKeyDialog` | FE-1.1..FE-1.5 enter through this React component as rendered by `ApiKeysSettings`. |

## Wave: DISTILL / [REF] Test budget

11 scenarios for 11 distinct behaviours (5 FE acceptance criteria + 6 BE acceptance criteria) = **1.0× ratio**. Well within the ≤ 2× lean-tier target.

Error-path scenarios: FE-1.4, FE-1.5, BE-1.5 = 3 of 11 = **27%**. Below the default 40% guideline, but justified for this follow-up: F-BE-1's "no regression" path IS the error/edge case for the only operationally relevant failure mode (owner with empty snapshot); F-FE-1's two error scenarios cover both server-rejection and client-validation paths. The parent feature already establishes infrastructure-failure coverage (S-1 CORS, S-3 auth, S-6 rate limit) which this delta does not change.

## Wave: DISTILL / [REF] Mandate compliance evidence

**CM-A (Hexagonal Boundary)** — Scenarios enter through driving ports only:
- FE scenarios drive `CreateApiKeyDialog` (UI driving port), mock at `apiKeyService` (HTTP shim boundary).
- BE-1.1 drives `IOidcGroupSnapshotWriter` (the inbound port for the OIDC hook's responsibility).
- BE-1.2..BE-1.6 drive `GET /api/v1/teams` (HTTP driving port) with `X-Api-Key` auth.
- No internal-component direct invocation (no testing `TryGetGroupValues` directly, no testing `ToHighestRoleMap` directly — both exercised transitively).

**CM-B (Business Language)** — Gherkin uses domain terms exclusively:
- Domain terms used: admin, scope, role, team, portfolio, API key, RBAC group mapping, user profile, snapshot, viewer, TeamAdmin.
- Single technical proper noun retained: `OidcGroupSnapshotWriter.WriteAsync` in BE-1.1 because the scenario is testing the named inbound port at the service level — this is the contractual interface name, equivalent to "order service places order" in BDD canonical examples. No HTTP verbs, status codes, JSON, or framework names in user-observable Gherkin Then clauses.

**CM-C (User Journey Completeness)** — Each scenario expresses an observable outcome:
- FE scenarios: the admin sees a key created, sees an error, or finds the Create button disabled — all observable to a person at the keyboard.
- BE scenarios: the API key holder sees a team in the response (or does not) — observable through the HTTP response body, which IS the user-facing surface for an API key.

**CM-D (Pure Function Extraction)** — N/A for this feature. The new logic (snapshot serialisation, snapshot read-back, fallback resolution) is naturally impure (touches `UserProfile` row, requires `LighthouseAppContext`). Group-value parsing is already a pure helper (`TryGetGroupValues`) in parent feature's `RbacAdministrationService` and is exercised transitively by BE-1.2/1.3.

## Wave: DISTILL / [REF] Test infrastructure extensions

DELIVER will need the following extensions to existing parent-feature test infrastructure. None of these are scaffolded yet (DISTILL produces scenarios + scaffolds, not infrastructure code):

1. **`TestAuthHandler` extension** — add support for emitting a `groups` claim from a new header `X-Test-Group-Values` (comma-separated). Needed only if any scenario simulates the OIDC sign-in path producing a groups claim; in this follow-up, BE-1.2..1.6 instead pre-seed `LastKnownGroupClaimValues` directly on the `UserProfile` (the snapshot already being present is the scenario precondition), so the `TestAuthHandler` extension is NOT strictly required for these scenarios. Document and skip unless DELIVER hits a case requiring it.
2. **`TestWebApplicationFactory.WithRealRbacService` variant** — needed for BE-1.2..1.6 because the parent factory's `WithTestAuthentication` replaces `IRbacAdministrationService` with `ClaimsDrivenRbacAdministrationService`. The new variant must keep the test auth scheme + premium-license stub but leave the real `RbacAdministrationService` registered. DELIVER to add as a sibling static factory method.
3. **EF migration** — `Create-Migration.ps1 -MigrationName AddUserProfileGroupSnapshot` (per CLAUDE.md, NOT generated in DISTILL).

## Wave: DISTILL / [REF] Scaffold inventory

Production scaffolds:
- `Lighthouse.Backend/Lighthouse.Backend/Models/Auth/UserProfile.cs` — extended with `LastKnownGroupClaimValues` nullable property (the path in DESIGN said `Models/Authorization/`; actual location is `Models/Auth/` per existing convention).
- `Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/Auth/IOidcGroupSnapshotWriter.cs` — new inbound port.
- `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/Auth/OidcGroupSnapshotWriter.cs` — new implementation, throws scaffold exception in `WriteAsync`; class-level `// SCAFFOLD: true` marker.
- `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ScopeRowList.tsx` — new component, throws scaffold error on render; exports `__SCAFFOLD__ = true` marker.

Test scaffolds:
- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Security/F_BE_1_GroupSnapshotInheritanceTests.cs` — six `[Test]` methods each `Assert.Fail`-ing with the RED scaffold message; class-level `// SCAFFOLD: true` marker.
- `Lighthouse.Frontend/src/pages/Settings/ApiKeys/F_FE_1_CreateApiKeyDialogScope.test.tsx` — five `test()` cases each invoking `expect.fail` with the RED scaffold message; file-level `// SCAFFOLD: true` marker.

Back-propagation notes:
- DESIGN D7 reuse table referenced `Models/Authorization/UserProfile.cs`; the actual codebase path is `Models/Auth/UserProfile.cs`. Production scaffold uses the actual path. Recommend a one-line correction in DESIGN's reuse table during the next DESIGN touch-up; not blocking.

---

