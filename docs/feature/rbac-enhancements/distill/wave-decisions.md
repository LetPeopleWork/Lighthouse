# Wave Decisions: DISTILL — rbac-enhancements

Date: 2026-05-10
Designer: Quinn (Acceptance Test Designer)
Mode: Execute (subagent)

---

## Summary

This document records all acceptance-test design decisions made during the DISTILL wave for the rbac-enhancements feature. Downstream waves (DELIVER) should treat these as authoritative for the test suite's structure and scope.

---

## Walking Skeleton Strategy

**Strategy C — Real local**: All adapters real (Playwright browser, Keycloak OIDC, .NET API, SQLite DB). No mocks, no stubs.

**Rationale**: RBAC is a security feature. The primary failure modes are wiring failures — the scoped endpoint being called instead of the global one, the permissive fallback not triggering, the emergency admin not being displayed differently. These failures are invisible to stub-based tests. Only a real E2E flow through Keycloak and the API can catch them.

**Walking Skeleton** = Scenario 1: A user with no prior System Admin assignment self-bootstraps via the `POST /authorization/bootstrap/system-admin` endpoint (invoked through the UI button), then adds an SSO group mapping. Observable user value: the user appears in the System Admins table and the SSO group mapping row is visible.

---

## Decision Log

| ID | Decision | Rationale |
|---|---|---|
| WD-D01 | Scenario 1 is the only enabled test; all others use `test.skip()` | One scenario enabled at a time is the outer loop discipline. Scenario 1 is the walking skeleton — must go green before any other test is enabled. |
| WD-D02 | All 7 scenarios expressed in a single `@RBAC E2E` describe block with nested describe groups | Mirrors the project's existing Auth.spec.ts convention; keeps the RBAC suite self-contained. |
| WD-D03 | `testWithAuth` fixture used for all tests (not `test` or `testWithData`) | RBAC tests need the login page as the entry point — they log in as specific users. `testWithAuth` provides `loginPage`. `test` and `testWithData` auto-log in as the default user and skip the login page. |
| WD-D04 | Scenario state is sequential — each scenario depends on prior state | The bootstrap/admin/revoke/fallback flow is inherently stateful. Scenarios 1-4 must run in order. `test.skip()` on later scenarios documents this dependency explicitly. |
| WD-D05 | Page Object Models placed in `tests/models/auth/rbac/` | RBAC-specific POMs are a new sub-domain. Mirroring the existing `tests/models/auth/` convention with a sub-directory keeps them co-located with auth models while preventing cluttering `auth/` itself. |
| WD-D06 | `RbacSettingsPage` models the System Settings → Access tab surface | Single POM per UI surface. Wraps all `data-testid` locators from the RBAC components (`rbac-bootstrap-button`, `rbac-users-table`, `rbac-group-mappings-table`, etc.). |
| WD-D07 | `ScopedAccessPage` models the team/portfolio detail Access tab | Separate from `RbacSettingsPage` because the surface is different: scoped member assignments and scoped group mappings via the new scoped endpoints. |
| WD-D08 | `goToTeam` and `goToPortfolio` on `OverviewPage` reused for navigation | Existing methods handle search + click. No new navigation helpers added for RBAC tests — avoids duplication. |
| WD-D09 | Write controls asserted with `.not.toBeVisible()` — never `.toBeDisabled()` | WD-06 / DD-01: hidden controls, not disabled controls. The acceptance tests must enforce this decision at the test level. |
| WD-D10 | Scenario 7 assertions are explicit copies of Scenario 6 assertions (not shared helpers) | The group-vs-individual equivalence invariant (WD-07) is the point. Making the assertions visually identical in the spec makes the invariant auditable — a reviewer can confirm the assertions match without tracing through a shared helper. |
| WD-D11 | Emergency admin display assertions: `toBeVisible()` on "Emergency Admin" text, `.not.toBeVisible()` on Revoke button | WD-02, DD-03. The test is the specification: no Revoke button must be present on the emergency admin row. |
| WD-D12 | Scoped group mappings load assertion: `groupMappingsErrorMessage.not.toBeVisible()` | US-08 / WD-08 bug fix. The acceptance test verifies the absence of the "Failed to load team access groups" error — observable user outcome, not internal endpoint call. |

---

## Scenario Coverage Map

| Scenario | US Covered | Tags | State |
|---|---|---|---|
| 1: Bootstrap first admin + SSO group | US-01, US-11 | `@walking-skeleton`, `@RBAC E2E` | ENABLED |
| 2a: Team reader restricted in System Settings | US-09, US-11 | `@RBAC E2E` | SKIPPED |
| 2b: Team reader restricted in team detail | US-06, US-07, US-09, US-11 | `@RBAC E2E` | SKIPPED |
| 3: New sys admin sees all tabs + revokes test user | US-11 | `@RBAC E2E` | SKIPPED |
| 4: Emergency admin fallback active | US-02, US-11 | `@RBAC E2E` | SKIPPED |
| 5: Sys admin creates team/portfolio + assigns roles | US-12 | `@RBAC E2E` | SKIPPED |
| 6a: Team reader — individual rights | US-06, US-07, US-09, US-12 | `@RBAC E2E` | SKIPPED |
| 6b: Team admin — individual rights | US-06, US-07, US-08, US-12 | `@RBAC E2E` | SKIPPED |
| 6c: Portfolio reader — individual rights | US-06, US-07, US-09, US-12 | `@RBAC E2E` | SKIPPED |
| 6d: Portfolio admin — individual rights | US-06, US-07, US-08, US-12 | `@RBAC E2E` | SKIPPED |
| 7: Setup switch to group-based rights | US-12 | `@RBAC E2E` | SKIPPED |
| 7a: Team reader — group rights | US-12 (WD-07 invariant) | `@RBAC E2E` | SKIPPED |
| 7b: Team admin — group rights | US-12 (WD-07 invariant) | `@RBAC E2E` | SKIPPED |
| 7c: Portfolio reader — group rights | US-12 (WD-07 invariant) | `@RBAC E2E` | SKIPPED |
| 7d: Portfolio admin — group rights | US-12 (WD-07 invariant) | `@RBAC E2E` | SKIPPED |

**Total scenarios**: 15 (1 enabled, 14 skipped)
**Error/restriction path ratio**: 10/15 = 67% (above 40% threshold)

---

## Error Path Coverage

| Error / Restriction | Scenario | Design Decision |
|---|---|---|
| Team reader cannot see Settings tab | 2b, 6a, 7a | WD-06, DD-01, DD-07 |
| Team reader cannot see Access tab | 2b, 6a, 7a | WD-06, DD-07 |
| Team reader cannot see write controls | 2b, 6a, 7a | WD-06, DD-01 |
| Viewer cannot see System Admins tab | 2a | WD-14, DD-07 |
| Viewer cannot see Log Level | 2a | WD-15 |
| Emergency admin has no Revoke button | 4 | WD-02, DD-03 |
| "Failed to load" error absent for Team Admin | 6b, 7b | WD-08, DD-02 |
| Portfolio reader cannot see Add Delivery | 6c, 7c | WD-12, DD-08 |
| Portfolio reader cannot see Settings/Access tabs | 6c, 7c | WD-06, DD-07 |
| Portfolio admin Deliveries visible + Add Delivery visible | 6d, 7d | DD-08 |

---

## Adapter Coverage

| Adapter | Scenario exercising it | Coverage type |
|---|---|---|
| Playwright browser | All | Real I/O |
| Keycloak OIDC (authentication) | All | Real I/O |
| .NET API — AuthorizationController | 1, 3, 5, 7 | Real I/O via browser |
| .NET API — Teams/Portfolios CRUD | 5 | Real I/O via browser |
| SQLite DB (via EF Core) | 1, 3, 5 | Real I/O (implicit via API) |
| New scoped endpoint: `GET /authorization/teams/{id}/group-mappings` | 6b, 7b | Real I/O via browser |
| New scoped endpoint: `GET /authorization/portfolios/{id}/group-mappings` | 6d, 7d | Real I/O via browser |
| New endpoint: `DELETE /authorization/users/{id}` | 3 | Real I/O via browser |

---

## Pre-requisites (DEVOPS — not yet run)

The following test environment requirements were identified during DISTILL. They must be addressed before Scenarios 2-7 can be enabled:

1. Keycloak realm must have the following users pre-provisioned with stable subjects:
   - `test@user.com` — also configured as emergency admin in `appsettings.json`
   - `systemadmin@user.com` — member of `system-admins` Keycloak group
   - `teamreader@user.com` — member of `team-readers` Keycloak group
   - `teamadmin@user.com` — member of `team-admins` Keycloak group
   - `portfolioreader@user.com` — member of `portfolio-readers` Keycloak group
   - `portfolioadmin@user.com` — member of `portfolio-admins` Keycloak group

2. All 5 group names must match `TestConfig` constants:
   - `SYSTEMADMIN_GROUP_NAME = "system-admins"`
   - `TEAMADMIN_GROUP_NAME = "team-admins"`
   - `TEAMREADER_GROUP_NAME = "team-readers"`
   - `PORTFOLIOADMIN_GROUP_NAME = "portfolio-admins"`
   - `PORTFOLIOREADER_GROUP_NAME = "portfolio-readers"`

3. `appsettings.Development.json` must configure the emergency admin subject to match the Keycloak subject of `test@user.com`.

4. The test environment database must be reset between full RBAC E2E runs to ensure Scenario 1 (bootstrap) finds no pre-existing System Admin.

---

## Open Questions Inherited from DESIGN Wave

| OQ | Question | Impact on Tests |
|---|---|---|
| OQ-01 | Does `GET /authorization/portfolios/{portfolioId}/group-mappings` exist? | Scenario 6d, 7d depend on it. If missing, those tests will fail at the API level. |
| OQ-03 | Are the 4 Keycloak test users pre-provisioned? | All Scenarios 2-7 depend on this. Blocks enablement. |
| OQ-04 | Is `isEmergencyAdmin` only in `RbacUserSummary` (not `/my-summary`)? | Scenario 4 asserts the emergency admin row display. If the flag is absent from the user list, the assertion will fail. |
