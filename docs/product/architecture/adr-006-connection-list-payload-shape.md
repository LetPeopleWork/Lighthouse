# ADR-006: Connection-List Payload Shape for Non-SystemAdmin Callers

**Status**: Accepted
**Date**: 2026-05-12
**Feature**: security-review-2026-05
**Decider**: Morgan (Solution Architect) based on Story S-2 (DISCUSS wave)

---

## Context

`GET /api/v1/worktrackingsystemconnections` (`WorkTrackingSystemConnectionsController.cs:51-58`) currently returns the full `WorkTrackingSystemConnectionDto` to any authenticated user. Secret option values are blanked by `WorkTrackingSystemConnectionOptionDto:16` (verified — the "secret leak" claim from the initial scan was false), but the response still exposes admin-only configuration metadata: `AuthenticationMethodKey`, `AuthenticationMethodDisplayName`, `AvailableAuthenticationMethods`, `AdditionalFieldDefinitions`, `WriteBackMappingDefinitions`, and the list of connection IDs and names. A low-privilege Viewer can enumerate every Jira / ADO / Linear tenant the org has connected.

A hard 403 on the GET (the original draft) was rejected during DISCUSS: scoped admins **can edit teams and portfolios** after commit `6e23f31b`, and the team/portfolio edit flow calls `getConfiguredWorkTrackingSystems()` to populate the connection dropdown. Locking the existing endpoint to SystemAdmin would break legitimate scoped-admin workflows.

The frontend dropdown (`GeneralSettingsComponent.tsx:141-156`) consumes only `id`, `name`, and `workTrackingSystem` (the last used to resolve the wizard schema and data-retrieval display name). The full DTO is needed only on the connection-edit page (`EditConnection.tsx:40`), which is already SystemAdmin-only by route guard.

Two payload-shape strategies were evaluated.

---

## Decision

**Add a new `GET /api/v1/worktrackingsystemconnections/summary` endpoint returning a lean DTO `{ id, name, workTrackingSystem }`. Lock the existing `GET /api/v1/worktrackingsystemconnections` to SystemAdmin via `[RbacGuard(RbacGuardRequirement.SystemAdmin)]`.**

Concretely:

- New action `GetWorkTrackingSystemConnectionSummary()` on `WorkTrackingSystemConnectionsController`, route `[HttpGet("summary")]`, guarded by a new `RbacGuardRequirement.AnyScopedAdmin` (true if `isSystemAdmin || any TeamAdmin || any PortfolioAdmin`). Returns a new `WorkTrackingSystemConnectionSummaryDto`.
- Existing `GetWorkTrackingSystemConnections()` action gains `[RbacGuard(RbacGuardRequirement.SystemAdmin)]`.
- New DTO `WorkTrackingSystemConnectionSummaryDto(int Id, string Name, WorkTrackingSystems WorkTrackingSystem)` — record type, immutable, no constructor logic.
- Frontend: `getConfiguredWorkTrackingSystems()` in `WorkTrackingSystemService.ts` calls the new summary endpoint; `EditConnection.tsx` keeps calling the existing full endpoint (it is SystemAdmin-only anyway).
- Viewers (no scoped admin role) receive 403 on both endpoints — admin-only configuration metadata is no longer enumerable by Viewers.

---

## Alternatives Considered

### Option A: Single endpoint, server-side filter on caller role, two response shapes (rejected)

Keep `GET /api/v1/worktrackingsystemconnections` as the only route. Inside the controller, check the caller's role and return either the full DTO (SystemAdmin) or the lean DTO (scoped admin). Viewer → 403.

**Rejected because**:
- The endpoint's OpenAPI / Swagger schema would advertise a union of two shapes for one route. Swagger UI and code generators handle this poorly; consumers (including the frontend's typed client) lose the type guarantee that "this field is always present."
- Permission-conditional payload shapes are a known anti-pattern in API design: behaviour depends on the caller, not the request. Testing and documentation suffer.
- Frontend would need a runtime discriminator (e.g., "is `authenticationMethodKey` present?") to decide whether to render the edit form — fragile.

### Option B: New `/summary` endpoint, lock the existing endpoint to SystemAdmin (selected)

Two endpoints, two stable shapes, two clean Swagger definitions, two unambiguous auth contracts.

**Accepted because**:
- OpenAPI clarity: each endpoint has one and only one response shape.
- Auth contract is local to each route: `[RbacGuard(SystemAdmin)]` on the full endpoint, `[RbacGuard(AnyScopedAdmin)]` on the summary endpoint. No conditional branching inside the controller.
- Frontend change is small and explicit: two call sites, two service methods. Type-safe at the boundary.
- Symmetric with existing precedent: `GET /authorization/users` (SystemAdmin) vs `GET /authorization/teams/{teamId}/members` (TeamAdmin-scoped) — the codebase already separates by route, not by conditional response.

---

## Consequences

**Positive**:
- Viewers can no longer enumerate connection configuration metadata.
- Scoped admins (TeamAdmin / PortfolioAdmin) keep editing teams and portfolios — the dropdown populates from the summary endpoint.
- Both endpoints have a single, documentable response shape; no Swagger ambiguity.
- The `WorkTrackingSystemConnectionSummaryDto` is a record (immutable per `code-style.instructions.md`).

**Negative**:
- One new endpoint, one new DTO, one new `RbacGuardRequirement` enum value (`AnyScopedAdmin`). The new requirement requires a corresponding switch arm in `RbacAdministrationService.CanSatisfyRequirementAsync` (`RbacAdministrationService.cs:305-323`).
- Frontend `WorkTrackingSystemService.ts` gains one new method and one updated method (the existing `getConfiguredWorkTrackingSystems()` is repointed to the summary endpoint).
- Two-file frontend refactor: `WorkTrackingSystemService.ts` plus a verification that `EditConnection.tsx` still calls the full endpoint (it does — route is SystemAdmin-only).

**Quality attribute impact**:
- Security: improved (admin-only metadata is now admin-only).
- API clarity: improved (one route, one shape).
- Maintainability: improved (no conditional response logic in the controller).
