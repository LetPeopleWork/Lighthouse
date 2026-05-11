# Feature Delta: api-keys-for-all-users

<!-- markdownlint-disable MD024 MD041 -->

Wave: DISTILL | Date: 2026-05-11 | Density: lean (per ~/.nwave/global-config.json)

Feature goal: make the **API Keys** settings tab visible to every authenticated
user, and let every authenticated user create their own API keys. Today the tab
is gated behind `rbac.isSystemAdmin`, even though the backend
`ApiKeyController` already scopes create / list / delete to the caller's stable
subject. The UI gate is the contradiction; this feature unrolls it.

This is a fast-tracked DISTILL: DISCUSS / DESIGN / DEVOPS waves are not run as
separate sessions. The change is purely a permission relaxation on an existing,
already-scoped backend surface. The relevant prior decisions live in
`rbac-ui-completeness/D4` (which this feature **overrides**) and the existing
per-subject scoping in `Lighthouse.Backend/API/ApiKeyController.cs`. No
infrastructure change, no new endpoint, no schema change, no migration.

---

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| `Lighthouse.Backend/API/ApiKeyController.cs` | `ApiKeyController` requires `[Authorize]` only; create / list / delete are already scoped to the caller's stable subject (`sub` or `oid` claim) | n/a | The backend contract is already per-user; no controller-level change required by this feature. |
| `Lighthouse.Backend/Services/Implementation/Auth/ApiKeyService.cs` | `GetApiKeysByOwnerSubject(string)` returns only the caller's keys; `DeleteApiKey(int, string)` returns false unless the caller owns the row | n/a | Per-user isolation is enforced server-side. No tenancy bug is introduced by widening the UI gate. |
| `rbac-ui-completeness/D4` (overridden) | API Keys tab is gated on `isSystemAdmin`; tab value `"40"` is in `systemAdminTabValues` | n/a | **Overridden.** D4 itself flagged "if keys become per-user in the future, revisit" — they already are. See Wave-decision reconciliation below. |
| `rbac-ui-completeness/US-04` (overridden) | When a Team Admin views `/settings`, the API Keys tab is NOT in the DOM | n/a | **Overridden.** Acceptance criterion AC3 of US-04 is replaced by the inverse: API Keys tab IS in the DOM for any authenticated non-SystemAdmin. |
| `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx` | When auth is disabled at runtime, the panel shows the "Enable authentication to manage API keys" alert and disables the "New API Key" button | n/a | Behaviour preserved verbatim. Widening the tab gate does not break the auth-disabled UX; it just makes the panel reachable. |
| `docs/product/architecture/brief.md` | All RBAC business-logic decisions flow through `IRbacAdministrationService`; no component fetches `/api/latest/authorization/my-summary` directly — all UI gating derives from `useRbac()` | n/a | The visibility filter in `Settings.tsx` is the only port involved. No new RBAC port is introduced; the existing `useRbac()` hook keeps its current shape (this feature removes a *consumer-side* check, not a hook-side one). |

---

## Wave: DISTILL / [REF] Wave-decision reconciliation

This feature **overrides** one prior wave decision:

| Prior decision | Was | Now |
|---|---|---|
| `rbac-ui-completeness/D4` (2026-05-11) | API Keys tab is gated on `isSystemAdmin`; tab value `"40"` is in `systemAdminTabValues` in `Settings.tsx` | API Keys tab is visible to every authenticated user. Tab value `"40"` is removed from `systemAdminTabValues`. |
| `rbac-ui-completeness/US-04` AC3 (2026-05-11) | When a Team Admin views `/settings`, the API Keys tab is NOT in the DOM | **Inverted.** When any authenticated user (Team Admin, Portfolio Admin, Viewer) views `/settings`, the API Keys tab IS in the DOM. |

**Reason for override**: D4's rationale was "API keys are a system-wide admin surface; mirrors Configuration, Demo Data, Database, RBAC tabs" — with the caveat "(If keys become per-user in the future, revisit.)". The caveat already applies: `ApiKeyController` scopes create / list / delete by `stableSubject` (the user's `sub` / `oid` claim). The "system-wide admin surface" mental model is false at the controller level today. D4 patched the UI to match a controller behaviour that did not exist — this feature unrolls D4 to align the UI with what the backend actually does.

**Smallest blast radius**: the change is a single-line edit in `Settings.tsx` (`systemAdminTabValues` no longer includes `"40"`) plus the test rewrites in `Settings.test.tsx`. No backend change. No new claim / role / scope is introduced.

**Open question recorded for the user**: should there be an *organisation-wide cap* on the number of keys a single user can create (e.g. max 10 per user)? Today there is no per-user cap. The acceptance tests in this wave are written against the uncapped model. **Explicit backlog item**: if the user decides a cap is required, the follow-up feature `api-keys-per-user-quota` would add it. Until that feature ships, this gate is uncapped — operators who run multi-tenant deployments and want a tighter cap must rely on Lighthouse's existing log infrastructure (see Observability below) to detect abuse.

---

## Wave: DISTILL / [REF] User stories (implicit DISCUSS)

Recorded here because no separate DISCUSS wave was run. These user stories frame the scope and the emotional arc the acceptance scenarios are built against.

**US-1 — Non-admin creates a personal API key**
As a Lighthouse user without System Admin rights, I want to create my own API key from Settings → API Keys, so that I can authenticate a personal CLI / MCP client without asking my System Admin for one. Emotional arc: from "blocked by admin queue" → "self-served in 30 seconds". JTBD: "Get my CLI talking to Lighthouse." Covered by walking skeleton + milestone-2 happy.

**US-2 — Viewer discovers the API Keys tab**
As a Lighthouse user with only Viewer rights, I want the API Keys tab to be visible to me, so that I know personal API keys exist as a self-service option. Emotional arc: from "unaware the capability exists" → "discoverable surface in Settings". JTBD: "Find out what I can do." Covered by milestone-1 scenarios M1.4. Note for operators: this is the surprise vector — operators upgrading from a release where the tab was admin-only should be aware that Viewers now see it.

**US-3 — Non-admin only sees their own keys**
As a Lighthouse user, I do NOT want to see API keys created by other users, so that the listing remains relevant and credentials do not leak across accounts. JTBD: "Manage my own credentials without distraction." Covered by milestone-2 scoping pins M2.2, M2.3.

**US-4 — Operator running with auth disabled**
As an operator running Lighthouse with authentication disabled (single-user / kiosk mode), I want the API Keys tab to remain visible but inert, so that I do not lose the discoverability surface and so that no key can be created without a stable owner. Covered by milestone-3 graceful-degradation scenarios.

**Breaking-change note for the DELIVER commit message and release notes**:

> **BREAKING (UI)**: The "API Keys" tab in System Settings is now visible to every authenticated user. Previously it was gated on the System Admin role. Per-user scoping is unchanged — every user sees and manages only their own keys. Operators who relied on the previous System-Admin-only visibility as a soft restriction should consider whether their threat model needs an explicit per-user quota (tracked as `api-keys-per-user-quota`).

---

## Wave: DISTILL / [REF] Observability (implicit DEVOPS)

This feature widens the population that can create API keys. With no per-user quota in this iteration, operators need a way to detect abuse (e.g. a non-admin account creating dozens of keys in minutes — credential-stuffing precursor or compromised principal).

**Existing log signal** (no new code required): `ApiKeyService.cs` already logs each key creation via the standard `ILogger<ApiKeyService>` instance. Operators ingesting Lighthouse logs into their observability stack can derive a `rate(api_key.created)` metric grouped by `OwnerSubject` and alert on a configurable threshold (e.g. > 5 keys per 5 minutes per subject).

**Documented risk acceptance**: until `api-keys-per-user-quota` ships, this feature deliberately ships without an in-process quota. The acceptance of this risk is conditional on the existing log signal being available; operators running Lighthouse without log ingestion (e.g. local dev) carry the residual risk.

**Acceptance**: no new test scenarios are added for this. The signal is the standard logger output already present in `ApiKeyService.CreateApiKeyAsync`. A separate observability feature would be required to assert specific log shapes — that is explicitly out of scope here.

---

## Wave: DISTILL / [REF] Acceptance scenarios

Scenario SSOT lives in `docs/feature/api-keys-for-all-users/acceptance/*.feature`.
Below is the index with tags. Each scenario maps to ONE TDD cycle in DELIVER.

| Scenario | File | Tags | TDD slice |
|---|---|---|---|
| Authenticated non-admin opens Settings, sees API Keys tab, and creates a key end-to-end | `acceptance/walking-skeleton.feature` | `@walking_skeleton @real-io @driving_adapter` | WS |
| API Keys tab is visible to a System Admin (regression pin) | `acceptance/milestone-1-tab-visibility.feature` | `@in-memory @milestone-1 @driving_adapter` | M1.1 |
| API Keys tab is visible to an authenticated Team Admin (no system-admin role) | `acceptance/milestone-1-tab-visibility.feature` | `@in-memory @milestone-1 @driving_adapter` | M1.2 |
| API Keys tab is visible to an authenticated Portfolio Admin (no system-admin role) | `acceptance/milestone-1-tab-visibility.feature` | `@in-memory @milestone-1 @driving_adapter` | M1.3 |
| API Keys tab is visible to an authenticated Viewer (no admin role at all) | `acceptance/milestone-1-tab-visibility.feature` | `@in-memory @milestone-1 @driving_adapter` | M1.4 |
| API Keys tab is visible when RBAC is disabled (regression pin) | `acceptance/milestone-1-tab-visibility.feature` | `@in-memory @milestone-1` | M1.5 |
| Non-admin lands on the first visible tab when their previously selected tab is now hidden | `acceptance/milestone-1-tab-visibility.feature` | `@in-memory @milestone-1 @error` | M1.6 |
| Non-admin authenticated user creates a key via the controller, sees it in the listing, and deletes it | `acceptance/milestone-2-non-admin-create.feature` | `@real-io @adapter-integration @milestone-2` | M2.1 |
| Non-admin authenticated user cannot see another user's keys (per-user scoping pin) | `acceptance/milestone-2-non-admin-create.feature` | `@real-io @adapter-integration @milestone-2 @error` | M2.2 |
| Non-admin authenticated user cannot delete another user's key (per-user scoping pin) | `acceptance/milestone-2-non-admin-create.feature` | `@real-io @adapter-integration @milestone-2 @error` | M2.3 |
| Unauthenticated request to `POST /api/latest/apikeys` is rejected (regression pin) | `acceptance/milestone-2-non-admin-create.feature` | `@real-io @adapter-integration @milestone-2 @error` | M2.4 |
| Empty key name returns 400 (regression pin) | `acceptance/milestone-2-non-admin-create.feature` | `@real-io @adapter-integration @milestone-2 @error` | M2.5 |
| Tab is visible but panel shows "Enable authentication" alert when auth is disabled (UI graceful degradation) | `acceptance/milestone-3-auth-disabled.feature` | `@in-memory @milestone-3 @error` | M3.1 |
| Create button is disabled when auth is disabled | `acceptance/milestone-3-auth-disabled.feature` | `@in-memory @milestone-3 @error` | M3.2 |

Counts:

- Walking skeleton: 1
- Milestone 1 (tab visibility): 6 (5 happy, 1 error/edge)
- Milestone 2 (non-admin create end-to-end + scoping pins): 5 (1 happy, 4 error/edge — 3 are existing-invariant pins, 1 is a contract pin)
- Milestone 3 (auth-disabled graceful degradation): 2 (0 happy, 2 error/edge)
- Total: 14 scenarios. Error/edge ratio: 7 / 14 = 50% (above the 40% target).

---

## Wave: DISTILL / [REF] Walking skeleton strategy

Strategy: **C (Real local)**.

Rationale: the feature touches two driving adapters — the React `<Settings />` tab filter and the HTTP `ApiKeyController`. Both are local and in-process; there are no costly externals. Real-I/O tests are cheap and catch the actual integration risks (UI gating consumer, HTTP serialisation, EF persistence, per-user scoping).

- WS scenario is tagged `@walking_skeleton @real-io @driving_adapter`.
- Frontend tests for milestone-1 use Vitest + React Testing Library with a mocked `IRbacService` returning `isSystemAdmin: false`. The driving port is the rendered `<Settings />` component — the tests render it and assert `getByTestId("api-keys-tab")` is in the DOM. Precedent: existing `Settings.test.tsx`.
- Backend tests for milestone-2 use `WebApplicationFactory<Program>` with a test authentication scheme that injects controllable `sub` claims, and `LighthouseAppContext` on the EF in-memory provider. Precedent: existing controller integration tests under `Lighthouse.Backend.Tests/API/`.
- The walking skeleton scenario combines both: it renders `<Settings />` as a non-admin user, asserts the tab is visible, then asserts that the underlying `ApiKeyService.createApiKey()` request succeeds and the key appears in the table.

No container option is taken: the feature is fully covered by in-process React tests + in-process WebApplicationFactory tests; testcontainers would add latency without uncovering new failure modes.

---

## Wave: DISTILL / [REF] Adapter coverage

Every driven / driving adapter touched by this feature has at least one `@real-io` scenario or a contract pin.

| Adapter | `@real-io` scenario | Covered by |
|---------|---------------------|------------|
| `<Settings />` tab visibility filter (driving adapter, React) | YES | Milestone-1 scenarios render the real `<Settings />` component with a mocked `IRbacService` and assert tab DOM presence. |
| `POST /api/latest/apikeys` (driving adapter, HTTP) | YES | Milestone-2 happy scenario issues a real HTTP `POST` via `WebApplicationFactory.CreateClient()` with a non-admin test principal; asserts `201 Created`. |
| `GET /api/latest/apikeys` (driving adapter, HTTP) | YES | Milestone-2 happy scenario lists keys for the creator; per-user scoping pin asserts the listing excludes another user's key. |
| `DELETE /api/latest/apikeys/{id}` (driving adapter, HTTP) | YES | Milestone-2 happy scenario deletes the just-created key; scoping pin asserts a non-owner gets `404`. |
| `ApiKeyService` (in-process port) | covered transitively | Exercised by every milestone-2 scenario via the HTTP path. |
| `ApiKeyRepository` (EF persistence) | covered transitively | Exercised by every milestone-2 scenario via the HTTP path; EF in-memory provider used. |
| `ApiKeysSettings.tsx` panel (driving adapter, React) | YES | Milestone-3 scenarios render the real panel with a mocked `IAuthService.getRuntimeAuthStatus()` returning `AuthMode.Disabled`; assert alert + disabled create button. |
| `IRbacService.getAuthorizationSummary` (driven, mocked at boundary) | YES | Mocked at the React boundary (`createMockRbacService` factory); this is the trust boundary for the UI tests. The hook's real behaviour is pinned by existing `useRbac.test.ts`. |

No `NO -- MISSING` rows.

---

## Wave: DISTILL / [REF] RED-ready scaffolds (Mandate 7)

This feature does not introduce a new production module. All production code paths that the acceptance tests exercise already exist:

- `Lighthouse.Frontend/src/pages/Settings/Settings.tsx` — the `visibleTabs` filter and the `systemAdminTabValues` set already exist. The DELIVER change is a one-line removal of `"40"` from the set.
- `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx` — the panel already renders the auth-disabled alert and the disabled create button. No new component needed.
- `Lighthouse.Backend/API/ApiKeyController.cs` — `[Authorize]`-only controller; no changes needed for the create / list / delete paths.

**Scaffold policy**: because there is no missing module, there is nothing to scaffold with `__SCAFFOLD__` markers. The DELIVER step 0 baseline is "all tests currently green except the new RED scenarios". When DELIVER starts, the new tests are added and the production change is then minimal:

- Frontend: delete `"40"` from `systemAdminTabValues` in `Settings.tsx`. Update `Settings.test.tsx` test cases that assert API Keys hidden for non-admin (those assertions are inverted).
- Backend: no production change. The milestone-2 scenarios are pinning + contract tests against existing behaviour.

**RED-vs-BROKEN clarification** (called out per DISTILL reviewer): the milestone-1 frontend tests will assert `getByTestId("api-keys-tab")` is in the DOM for a non-admin user. Against the CURRENT `Settings.tsx` (which still has `"40"` in `systemAdminTabValues`), the test will fail at the `getByTestId` line with Testing Library's "Unable to find element with testId" — an `Error` thrown from inside the assertion, semantically equivalent to a failed assertion. This is RED (the test fails for the right reason: the production behaviour does not match the new contract), NOT BROKEN (the test compiles, imports resolve, the module is present). Mandate 7's purpose is to prevent `ImportError` / missing-symbol crashes; both prerequisites here (module exists, type signatures match) are satisfied. The one-line production edit then takes the test from RED to GREEN in the standard TDD cycle.

The intent of Mandate 7 (RED, not BROKEN, on first run) is preserved: every new test compiles, imports, and runs against an existing module — failing only on the assertion line, not on a missing symbol.

---

## Wave: DISTILL / [REF] Test placement

Frontend acceptance tests (Vitest + Testing Library):

- `Lighthouse.Frontend/src/pages/Settings/Settings.test.tsx` — extend with milestone-1 scenarios. The existing file at lines 144–164 already has a `"hide system-admin tabs (including API Keys)"` test case that must be **renamed and inverted**: `"shows API Keys tab even when not system admin (other system-admin tabs still hidden)"`. Add new test cases for Team Admin, Portfolio Admin, Viewer. Precedent: same file's existing role-table test cases.
- `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.test.tsx` — extend with milestone-3 (auth-disabled) scenarios. Precedent: same file already covers the existing happy path + create-key flow.

Backend acceptance tests (xUnit, .NET 8):

- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ApiKeyControllerNonAdminAccessTests.cs` — NEW file. One fixture per milestone-2 scenario. Uses `WebApplicationFactory<Program>` with a test auth scheme that injects a non-admin `ClaimsPrincipal` with a configurable `sub` claim. Precedent for `WebApplicationFactory` with a custom auth scheme: existing integration tests under `Lighthouse.Backend.Tests/API/Integration/`.
- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/ApiKeyControllerTest.cs` — existing file; do NOT duplicate the controller-unit assertions. The milestone-2 scoping pins (`@error` rows M2.2, M2.3) live in the new integration test file because they require a real authn principal switch.

E2E (Playwright): one E2E assertion is added to the existing `@rbac` Playwright suite — as a Team Reader, `api-keys-tab` testId IS in the DOM (currently the suite asserts it is NOT — this assertion is inverted). No new Playwright spec file. Rationale: the existing suite already walks the role table; piggy-backing the new assertion keeps the role-traversal infrastructure DRY.

**Explicit DELIVER task for the E2E inversion** (called out per DEVOPS reviewer): the assertion to invert lives in `Lighthouse.EndToEndTests/tests/specs/auth/RoleBasedAccessControl.spec.ts` around lines 399–400 (the Team Reader role step). This must be one of the FIRST DELIVER commits — landing the Settings.tsx production change without inverting this assertion would make the `@rbac` suite red in CI and block all subsequent DELIVER steps. The DELIVER roadmap MUST surface this as a named step (e.g. "M1.0 — Invert RoleBasedAccessControl.spec.ts Team Reader assertion").

---

## Wave: DISTILL / [REF] Driving adapter coverage

Per RCA fix P1 (2026-04-10), every CLI / endpoint / hook adapter in scope has at least one scenario exercising it via its real protocol.

| Driving adapter | Real-protocol scenario | Verifies |
|---|---|---|
| `<Settings />` React component (tab visibility) | Milestone-1 scenarios via Testing Library `render()` | API Keys tab DOM presence as a function of `useRbac()` summary; default-tab selection when System Info is not the only visible tab |
| `POST /api/latest/apikeys` (HTTP) | Milestone-2 scenarios via `HttpClient` | HTTP status (201 happy, 400 empty name, 401 unauthenticated, 403 forbidden if `sub` missing), JSON shape, per-user `stableSubject` recorded on the row |
| `GET /api/latest/apikeys` (HTTP) | Milestone-2 scenarios via `HttpClient` | Per-user listing (caller sees only own keys); other user's key is absent from response |
| `DELETE /api/latest/apikeys/{id}` (HTTP) | Milestone-2 scenarios via `HttpClient` | 204 happy, 404 for non-owner (no cross-user deletion possible) |
| `<ApiKeysSettings />` React panel | Milestone-3 scenarios via Testing Library `render()` | Auth-disabled alert visible; "New API Key" button disabled |

No scope was reduced to bypass the user's actual invocation path -- the UI is rendered, the HTTP endpoints are hit via real `HttpClient`, the panel is rendered with the real auth-status branch.

---

## Wave: DISTILL / [REF] Pre-requisites

From the existing codebase (no DESIGN wave was run separately):

- `useRbac()` hook returns `{ isRbacEnabled, isSystemAdmin, canCreateTeam, canCreatePortfolio, ... }`. No new field required. The visibility decision is "tab is visible iff user is authenticated", and authentication is enforced by the surrounding `/settings` route (already guarded by the existing auth middleware) — so once `"40"` is removed from `systemAdminTabValues`, the tab is visible to every authenticated user.
- `ApiKeyController` is at `[Authorize]` only; no `[Authorize(Roles = "SystemAdmin")]` or `[RbacGuard]` attribute is present. Verified at `Lighthouse.Backend/API/ApiKeyController.cs:12`.
- `IApiKeyService.CreateApiKeyAsync(name, description, userName, stableSubject)` accepts the caller's subject; `GetApiKeysByOwnerSubject(stableSubject)` filters by it. Verified at `Lighthouse.Backend/Services/Implementation/Auth/ApiKeyService.cs`.
- The existing `@rbac` Playwright suite walks Team Reader → Team Admin → Portfolio Admin → System Admin in a single test. Reusing its role-traversal pattern is cheaper than adding a new spec.

From DEVOPS (no separate wave): no infrastructure changes. The feature is a UI gate relaxation + new test cases. No new environment variables, no new secrets, no new persisted state, no migration.

Migrations: none.

Feature flags: none. The relaxed gate is unconditional once shipped. Operators who want to restrict API key creation to System Admins can disable Lighthouse authentication entirely (the panel then shows the "Enable authentication" alert), or — if a per-user cap is later introduced — set the cap to zero for non-admins.

---

## Wave: DISTILL / [REF] Self-review checklist

- [x] WS strategy declared (Strategy C — Real local)
- [x] WS scenarios tagged correctly (`@walking_skeleton @real-io @driving_adapter`)
- [x] Every driven / driving adapter has at least one `@real-io` scenario (see adapter coverage table)
- [x] InMemory doubles documented: only `IRbacService` (mocked at the React trust boundary via `createMockRbacService`) and `IAuthService.getRuntimeAuthStatus` (mocked at the React trust boundary). Cannot model real authentication-handler wiring — that is caught by milestone-2 integration tests under `WebApplicationFactory<Program>`.
- [x] Container preference documented (none required)
- [x] Mandate 7 scaffolds: not required — no new production module is introduced; tests run against existing code. Mandate 7 intent (RED not BROKEN) preserved because every new test compiles against existing imports.
- [x] Driving adapter coverage: `<Settings />` tab filter, `POST/GET/DELETE /api/latest/apikeys`, `<ApiKeysSettings />` panel each have a scenario exercising the real protocol.
- [x] Every `@when` step references a driving port (rendering `<Settings />`, issuing `HttpClient` requests, rendering `<ApiKeysSettings />`) — never the driven adapter directly.
- [x] Error path coverage 50% (>= 40% target).
- [x] Prior-wave contradictions: 1 contradiction with `rbac-ui-completeness/D4` and US-04 AC3. **Resolved** via the explicit override recorded in "Wave-decision reconciliation" above; the prior-wave caveat ("If keys become per-user in the future, revisit") already applies.
- [x] Outcomes registry: feature relaxes an existing UI gate; it does not introduce a new typed contract surface. Per D-6 gate-scoping, methodology / observability / permission-relaxation features do not need OUT-N registration. The existing `POST/GET/DELETE /api/latest/apikeys` operations are unchanged.
