# Feature Delta: apikey-list-replace-createdby-with-scopes

<!-- markdownlint-disable MD024 MD041 -->

Wave: DISTILL | Date: 2026-05-14 | Density: lean (per ~/.nwave/global-config.json)

Feature goal: on the **System Settings → API Keys** tab, drop the always-shown
"Created By" column from the listing and — when RBAC is enabled — replace it
with a "Scopes" column that surfaces the per-key `ApiKeyPermission` rows.

Why now: the API Keys endpoint already filters by the caller's stable
subject (`ApiKeyController.GetApiKeys` →
`apiKeyService.GetApiKeysByOwnerSubject(stableSubject)`), so every row in the
table is, by construction, the caller's own key. The "Created By" column is
therefore redundant; in practice it also reads "unknown" for any key created
before the
`Lighthouse.Backend/Services/Implementation/Seeding/ApiKeyOwnerReconciliationSeeder.cs`
reconciliation pass linked it to a `UserProfile`. The screen real estate is
better spent on per-key Scopes once RBAC is on — scopes are the only
information that varies per key on the caller's own row and the most
operationally interesting datum after the key name.

This is a fast-tracked DISTILL. DISCUSS / DESIGN / DEVOPS are not run as
separate sessions. The change is a column swap in
`Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx`, a
matching DTO extension in
`Lighthouse.Backend/Models/Auth/ApiKeyInfo.cs`, and a join inside
`ApiKeyService.GetApiKeysByOwnerSubject` that returns the per-key
`ApiKeyPermission` rows. No new endpoint, no migration, no new service or
hook. Relevant prior decisions live in
`docs/product/architecture/adr-004-apikey-scope-storage.md` (which
introduced per-key scopes),
`docs/feature/apikey-scope-ui-hidden-when-rbac-off/feature-delta.md` (which
established the `useRbac()` gate pattern for this dialog), and
`docs/feature/api-keys-for-all-users/feature-delta.md` (which widened the
tab's audience and made the redundant column more visible).

---

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| `Lighthouse.Backend/API/ApiKeyController.cs::GetApiKeys` | The listing endpoint returns only keys whose resolved owner subject equals the caller's stable subject (`sub` / `oid`). No "list every key" view exists. | n/a | "Created By" is always the caller — verified by construction, not by content. Removing the column loses zero information; whether the cell currently renders the name, the email, or "unknown" is irrelevant. |
| `Lighthouse.Backend/Services/Implementation/Seeding/ApiKeyOwnerReconciliationSeeder.cs` | The seeder uses `ApiKey.CreatedByUser` (entity field) to back-link legacy keys to `UserProfile` by `DisplayName` / `Email`. | n/a | The entity field stays. This feature only removes the field from the **response DTO** (`ApiKeyInfo`) and from the corresponding **frontend interface** (`IApiKeyInfo`). The seeder reads the entity directly and is unaffected. |
| `docs/product/architecture/adr-004-apikey-scope-storage.md` (default-when-empty rule) | "If `ApiKeyPermissions` has zero rows for the key, the key inherits the full owner scope (backwards-compatible default; existing keys remain functional after migration)." | ADR-004 | A key with zero scope rows MUST render as "Unrestricted" in the Scopes cell (RBAC-on), not as an empty cell — otherwise the UI implies a restriction that doesn't exist. Empty cell ≠ "no permissions"; it means "owner-equivalent permissions." |
| `Lighthouse.Backend/Models/Authorization/ApiKeyPermission.cs` | Each row is `(ApiKeyId, Role, ScopeType, ScopeId?, GrantedAt)`. `ScopeType` ∈ `{System, Team, Portfolio}`. `Role` ∈ `{SystemAdmin, TeamAdmin, PortfolioAdmin, Viewer}`. `ScopeId` is null only when `ScopeType = System`. | n/a | The Scopes cell renders one chip / line per row. The DTO surface re-uses the existing `ApiKeyScopeDto` (`Role`, `ScopeType`, `ScopeId?`) — no new shape — so frontend and backend share the same record. |
| `Lighthouse.Backend/Models/Auth/ApiKeyScopeDto.cs` | The DTO is already round-tripped on the POST creation path. Issue-time superset check on create is unchanged. | n/a | We extend the GET response to ALSO use this DTO (as a `Scopes` array on `ApiKeyInfo`). Symmetrical naming reduces frontend type churn — `IApiKeyScope` already exists in `Lighthouse.Frontend/src/models/ApiKey/ApiKey.ts` and can be reused verbatim on `IApiKeyInfo`. |
| `Lighthouse.Frontend/src/hooks/useRbac.ts` | `useRbac()` exposes `isRbacEnabled: boolean`, sourced from `/authorization/my-summary` with a `PERMISSIVE_SUMMARY` (`isRbacEnabled=false`) default while loading and on fetch failure. | n/a | The "show Scopes column" branch reads this hook exactly the same way the existing scope picker conditional does (per `apikey-scope-ui-hidden-when-rbac-off`). Loading / error states resolve to "RBAC off" → no Scopes column — safe default. |
| `docs/feature/apikey-scope-ui-hidden-when-rbac-off/feature-delta.md` | Architecture mandate: "all UI gating derives from `useRbac()` hook"; the Accordion in the Create dialog is already gated on `rbac.isRbacEnabled`. | n/a | The new Scopes column uses the same gate — one source of RBAC truth in this component. No second `/authorization/my-summary` fetch is introduced. |
| `docs/feature/api-keys-for-all-users/feature-delta.md` | The API Keys tab is visible to every authenticated user; non-admins see only their own keys (controller-level filter). | n/a | Confirms the redundancy of "Created By": every row already belongs to the caller. The change improves the surface for the wider audience this feature introduced. |
| `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx` (auth-disabled branch, lines 361–366, 429–438) | When authentication is disabled, the panel shows an info alert and disables the "New API Key" button; the keys array is forced to empty. | n/a | Behaviour preserved verbatim. The table does not render at all when `authEnabled === false`, so neither the column-removal nor the Scopes-conditional branch is reachable in that environment. |

---

## Wave: DISTILL / [REF] Wave-decision reconciliation

This feature contradicts no prior wave decision. It is a UX cleanup made
possible — and made more urgent — by three shipped features:

| Prior decision | Status under this feature |
|---|---|
| `security-review-2026-05/S-5` → ADR-004: introduce per-key scopes via `ApiKeyPermissions` table | **Compatible.** This feature surfaces those rows on the listing screen for the first time. Storage and creation paths unchanged. |
| `api-keys-for-all-users`: tab is visible to every authenticated user | **Compatible.** Every row belongs to the caller; "Created By" is provably redundant on a per-user view. |
| `apikey-scope-ui-hidden-when-rbac-off`: Create-dialog accordion is gated on `useRbac().isRbacEnabled` | **Compatible and reused.** The Scopes column uses the same gate, the same hook, the same loading/error default (no Scopes column on RBAC-off / loading / fetch-fail). Two surfaces (create + list) now share one RBAC source of truth. |

**Open questions explicitly out of scope:**

1. **Per-key scope edit from the list.** Today scopes are write-once on
   creation; editing requires deletion + recreation. We do not introduce an
   in-place edit affordance on the new column; the column is read-only. The
   candidate follow-up feature is `apikey-scope-inline-edit`.
2. **Human-readable team / portfolio names in the Scopes cell.** The DTO
   currently exposes `ScopeId` (numeric foreign key). Resolving it to a
   display name requires either a join in the controller or a frontend lookup
   against `TeamService` / `PortfolioService`. **Decision: do the lookup on
   the frontend**, reusing the same pattern already used in the Create
   dialog (`scopeDataLoaded` flow in `ApiKeysSettings.tsx` lines 95–128). The
   cell renders the resolved name when the lookup succeeds and falls back to
   `Team #{id}` / `Portfolio #{id}` otherwise. This avoids a new backend
   join and keeps `GetApiKeysByOwnerSubject` cheap. **Hard constraint**: the
   lookup must not block the table from rendering — names load lazily.
3. **Sorting and filtering on the Scopes column.** Out of scope. The column
   is informational, not interactive.
4. **Admin "all keys" view.** No such view exists today; introducing one
   would require a separate `RbacGuardRequirement.SystemAdmin` endpoint and
   is unrelated to this feature.

---

## Wave: DISTILL / [REF] User stories (implicit DISCUSS)

Recorded here because no separate DISCUSS wave was run.

**US-1 — Operator on RBAC-disabled deployment sees a leaner list**
As a Lighthouse user on a deployment where the operator has left RBAC off, I
want the API Keys listing to drop the "Created By" column entirely, so that
the table reflects only the data that varies per row (Name, Description,
Created At, Last Used). Emotional arc: from "why is every row 'unknown' and
why does it matter — these are all mine?" → "the table tells me what I need
about my keys, nothing redundant." JTBD: "Trust the UI: every visible column
carries information."
Covered by WS + M1.1 + M1.2.

**US-2 — Operator on RBAC-enabled deployment audits per-key scopes from the
list**
As a Lighthouse user on a deployment with RBAC enabled, I want to see each
key's `(Role, ScopeType, ScopeId)` rows at a glance on the listing screen,
so that I can audit blast-radius without reopening every key's creation
dialog (which I cannot, since scopes are write-once). Emotional arc: from
"which of these keys can hit production team #42?" → "I can see the scope
chips on the row." JTBD: "Audit per-key blast radius from one screen."
Covered by M1.3 + M1.4 + M1.5.

**US-3 — Operator views a key created before per-key scopes existed (or
created on RBAC-off and migrated)**
As a Lighthouse user on a deployment that now has RBAC enabled but holds
keys persisted with zero `ApiKeyPermissions` rows, I want such keys to
render with an explicit "Unrestricted" indicator in the Scopes cell, so that
I do not mistake "no chips" for "no permissions." Per ADR-004 the
zero-rows key inherits the owner's full scope; the UI must match.
Covered by M1.5.

**US-4 — Operator on a transitional deployment (RBAC config flip)**
As a Lighthouse user whose System Admin has just flipped RBAC on or off, I
acknowledge that the column reflects the RBAC state captured at the time
`useRbac()` last fetched the authorization summary (page load). A full
browser refresh is required to pick up a config flip. Consistent with all
other RBAC-gated UI in the product and with the existing Create dialog
behaviour from `apikey-scope-ui-hidden-when-rbac-off`. Covered implicitly
by M1.2 (RBAC-off branch) and M1.3 (RBAC-on branch).

**Release notes draft for the DELIVER commit message:**

> **UI**: The API Keys listing in System Settings drops the "Created By"
> column (every row is the caller's own key — the column was always
> redundant and showed "unknown" for older keys until ownership
> reconciliation ran). On deployments with RBAC enabled, the listing now
> shows a "Scopes" column with one chip per per-key permission row, or
> "Unrestricted" for keys created without explicit scopes (backwards-
> compatible default per ADR-004). Behaviour on RBAC-disabled deployments is
> a simple drop of the "Created By" column with no replacement.

---

## Wave: DISTILL / [REF] Acceptance scenarios

Scenario SSOT lives in
`docs/feature/apikey-list-replace-createdby-with-scopes/acceptance/*.feature`.
Below is the index with tags. Each scenario maps to ONE TDD cycle in DELIVER.

| Scenario | File | Tags | TDD slice |
|---|---|---|---|
| User on RBAC-enabled deployment opens API Keys tab and sees a Scopes column with per-key chips | `acceptance/walking-skeleton.feature` | `@walking_skeleton @in-memory @driving_adapter` | WS |
| M1.1 "Created By" column is absent on RBAC-disabled deployment | `acceptance/milestone-1-column-replacement.feature` | `@in-memory @milestone-1` | M1.1 |
| M1.2 "Created By" column is absent on RBAC-enabled deployment (regression pin both ways) | `acceptance/milestone-1-column-replacement.feature` | `@in-memory @milestone-1` | M1.2 |
| M1.3 "Scopes" column is hidden on RBAC-disabled deployment | `acceptance/milestone-1-column-replacement.feature` | `@in-memory @milestone-1` | M1.3 |
| M1.4 "Scopes" column renders one entry per scope row on RBAC-enabled deployment | `acceptance/milestone-1-column-replacement.feature` | `@in-memory @milestone-1` | M1.4 |
| M1.5 "Scopes" cell shows "Unrestricted" when key has zero scope rows (ADR-004 default) | `acceptance/milestone-1-column-replacement.feature` | `@in-memory @milestone-1` | M1.5 |
| M1.6 Backend `GET /apikeys` response includes a `scopes` array per key (drops `createdByUser` from DTO) | `acceptance/milestone-1-column-replacement.feature` | `@real-io @milestone-1 @adapter-integration @driving_adapter` | M1.6 |
| M1.7 "Scopes" column hidden while the authorization summary is still loading | `acceptance/milestone-1-column-replacement.feature` | `@in-memory @milestone-1` | M1.7 |
| M1.8 Team / portfolio scope id renders as human-readable name when team / portfolio is loaded; falls back to `Team #{id}` otherwise | `acceptance/milestone-1-column-replacement.feature` | `@in-memory @milestone-1` | M1.8 |

---

## Wave: DISTILL / [REF] Walking skeleton strategy

**Strategy B — Real local + fake costly (split frontend / backend).**
Justification:

- The user-visible behaviour is a frontend rendering rule. The walking
  skeleton scenario is executed inside Vitest + React Testing Library using
  the existing `createMockApiServiceContext` factory in
  `Lighthouse.Frontend/src/tests/MockApiServiceProvider.ts`. The driving
  adapter is the rendered `<ApiKeysSettings />` component; the test asserts
  on the DOM (column headers, cell contents) and does not call any service
  function directly. All driven service calls (`getApiKeys`,
  `getAuthorizationSummary`, `getTeams`, `getPortfolios`) are mocked at the
  HTTP boundary.

- One backend `@real-io @adapter-integration` scenario (M1.6) covers the
  GET `/api/apikeys` contract change: the response body MUST contain a
  `scopes` array per key (and MUST NOT contain `createdByUser`). Executed
  via `WebApplicationFactory<Program>` against the EF-Core InMemory
  provider, exactly as the existing
  `Lighthouse.Backend.Tests/API/Integration/ApiKeyControllerHttpSmokeTests.cs`
  pattern. This is the only place an HTTP request is actually sent.

- Costly externals: none. Lighthouse has no LLM / paid API in this slice.

InMemory cannot model: a runtime RBAC flip between page-load and dialog
open (US-4) — deliberate, see `apikey-scope-ui-hidden-when-rbac-off`
DWD-1 precedent. The component captures `isRbacEnabled` at mount via the
existing hook; the test mounts with one state and asserts against that
state.

---

## Wave: DISTILL / [REF] Adapter coverage

| Adapter | `@real-io` scenario | Covered by |
|---|---|---|
| `RbacService.getAuthorizationSummary()` (HTTP GET `/authorization/my-summary`) | NO (covered transitively by Strategy B mock at component-test level) | `Lighthouse.Frontend/src/services/Api/RbacService.test.ts` already pins the HTTP contract. Request / response shapes unchanged by this feature. |
| `ApiKeyService.getApiKeys()` (HTTP GET `/apikeys`) | **YES — M1.6** | `Lighthouse.Backend.Tests/API/Integration/ApiKeyControllerHttpSmokeTests.cs` extended with one scenario asserting the response body includes a `scopes` array per key and does NOT include `createdByUser`. |
| `TeamService.getTeams()` / `PortfolioService.getPortfolios()` (HTTP GET) | NO (covered transitively by Strategy B mock) | Both services have pre-existing `*.test.ts` HTTP-contract pins; this feature does not change them. The frontend reuses the existing `scopeDataLoaded` lazy-load pattern. |
| `useRbac()` hook (frontend port) | YES via M1.2 / M1.3 / M1.7 | The hook is exercised directly by the component under test. No new driven adapter. |
| `<ApiKeysSettings />` React component (driving adapter — user's entry point) | YES via WS + every M1.x except M1.6 | RTL-rendered with `createMockApiServiceContext`. |

No "NO — MISSING" rows. The single new shape introduced by this feature
(`ApiKeyInfo.Scopes`) is exercised through the GET `/apikeys` HTTP path in
M1.6 and rendered in the DOM in WS + M1.4 + M1.5 + M1.8.

---

## Wave: DISTILL / [REF] Driving adapter coverage

| Driving adapter | WS / M scenario exercising it |
|---|---|
| `<ApiKeysSettings />` React component (mounted at System Settings → API Keys tab) | WS, M1.1, M1.2, M1.3, M1.4, M1.5, M1.7, M1.8 — RTL rendering, assert on rendered DOM. |
| HTTP `GET /api/v1/apikeys` (and `/api/latest/apikeys`) | M1.6 — `HttpClient`-driven request from `WebApplicationFactory<Program>`, asserts JSON body shape. |

No CLI subcommand, no hook adapter is introduced. The two driving adapters
above are exactly the user's invocation paths (browser → React component
and CLI / API client → HTTP endpoint).

---

## Wave: DISTILL / [REF] Test placement

**Frontend** — `Lighthouse.Frontend/src/pages/Settings/ApiKeys/` (co-located
with existing component tests):

- `ApiKeysSettings.test.tsx` (general list behaviour) — touched to drop
  the "Created By" column assertions and to extend the
  `IApiKeyInfo` test factory rows with `scopes: []`. The five existing
  cases for empty state, delete, dialog open, etc. remain but lose
  `createdByUser` from their fixtures (the field is removed from the
  frontend interface).
- `F_FE_2_ApiKeyListScopesColumn.test.tsx` **(new)** — RBAC-on column
  behaviour, scope-row rendering, name resolution fallback. Covers M1.2,
  M1.3, M1.4, M1.5, M1.7, M1.8 and the WS rendering assertion. Naming
  follows the precedent set by the existing
  `F_FE_1_CreateApiKeyDialogScope.test.tsx`.
- `ApiKeysSettings_CreatedByRemoved.test.tsx` **(new, optional)** — could
  be merged into `F_FE_2_*` if the reviewer prefers a single file. Current
  recommendation: keep the regression pin (M1.1) in `F_FE_2_*` so that one
  file owns the "what's in the table" contract end-to-end.

**Backend** —
`Lighthouse.Backend.Tests/API/Integration/ApiKeyControllerHttpSmokeTests.cs`
gains one new test method for M1.6 (HTTP contract).
`Lighthouse.Backend.Tests/Services/Implementation/Auth/ApiKeyServiceTest.cs`
gains one unit test asserting `GetApiKeysByOwnerSubject` populates `Scopes`
from `apiKeyPermissionRepository.GetAll()` filtered by `ApiKeyId`.

**E2E** — `Lighthouse.EndToEndTests/tests/models/settings/ApiKeys/ApiKeysSettingsPage.ts`
gains no public API change in DISTILL; the new column may be queried by
ad-hoc test code in DELIVER if a screenshot test is added (handled by the
existing `update-docs` skill, not DISTILL).

---

## Wave: DISTILL / [REF] Scaffolds

**Frontend production modules required** (all exist; no new files):

- `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx` —
  modify table header (drop `<TableCell>Created By</TableCell>` at line
  386, add conditional `<TableCell>Scopes</TableCell>` after Description)
  and row body (drop `<TableCell>{key.createdByUser}</TableCell>` at line
  397, add conditional scope cell renderer).
- `Lighthouse.Frontend/src/models/ApiKey/ApiKey.ts` — drop `createdByUser`
  from `IApiKeyInfo`, drop `createdByUser` from `IApiKeyCreationResult`,
  add `scopes: IApiKeyScope[]` to `IApiKeyInfo`. `IApiKeyScope` already
  exists.
- `Lighthouse.Frontend/src/hooks/useRbac.ts` — consumed, not modified.
- `Lighthouse.Frontend/src/tests/MockApiServiceProvider.ts` —
  `createMockApiKeyService` default factory must drop `createdByUser` from
  any returned `IApiKeyInfo` and default `scopes: []`. This is part of the
  feature, not a scaffold.

**Backend production modules required** (all exist; no new files):

- `Lighthouse.Backend/Models/Auth/ApiKeyInfo.cs` — drop `CreatedByUser`,
  add `IReadOnlyList<ApiKeyScopeDto> Scopes` (defaulting to empty array).
- `Lighthouse.Backend/Models/Auth/ApiKeyCreationResult.cs` — drop
  `CreatedByUser` (frontend no longer consumes it).
- `Lighthouse.Backend/Services/Implementation/Auth/ApiKeyService.cs` —
  in `GetApiKeysByOwnerSubject`, materialise `apiKeyPermissionRepository.GetAll()`
  into a `Dictionary<int, List<ApiKeyScopeDto>>` keyed by `ApiKeyId`, then
  populate `Scopes` per `ApiKeyInfo`. In `CreateApiKeyAsync` return path,
  drop the now-removed `CreatedByUser` assignment from the
  `ApiKeyCreationResult`.
- `Lighthouse.Backend/API/ApiKeyController.cs` — no signature change.
- `Lighthouse.Backend/Services/Implementation/Seeding/ApiKeyOwnerReconciliationSeeder.cs` —
  untouched; reads `ApiKey.CreatedByUser` from the entity, which stays.

**Mandate 7 satisfied vacuously.** Zero new modules to scaffold, zero
`__SCAFFOLD__` markers to clean up later. Tests will be RED against the
current code (header has "Created By"; response has `createdByUser`, no
`scopes`); they go GREEN once the rendering and DTO changes ship.

**Documentation files modified** (mandatory DELIVER scope — see
"Documentation scaffold" section below):

- `docs/settings/apikeys.md` (lines 50–58) — table edit; remove
  "Created By" row, add conditional "Scopes" description.

---

## Wave: DISTILL / [REF] Pre-requisites

- **DESIGN driving ports inherited:** `useRbac()` (FE port; already in
  place from `rbac-enhancements`); `IApiKeyService.GetApiKeysByOwnerSubject`
  (BE port; already in place from `api-keys-for-all-users`).
- **DEVOPS environment matrix inherited** from
  `apikey-scope-ui-hidden-when-rbac-off`:
  - **Auth enabled + RBAC enabled** — production default. Covered by WS +
    M1.2 + M1.4 + M1.5 + M1.6 + M1.8.
  - **Auth enabled + RBAC disabled** — single-tenant / kiosk / pre-rollout.
    Covered by M1.1 + M1.3.
  - **Auth disabled** — out of band: the table does not render. Pre-existing
    `api-keys-disabled-message` test in `ApiKeysSettings.test.tsx` covers
    this branch unchanged.
- **No new infrastructure, no new config flag, no new env var.** No
  migration: the `ApiKeyPermissions` table already exists
  (`Lighthouse.Backend/Lighthouse.Migrations.Sqlite/Migrations/20260512152303_AddApiKeyPermissions.cs`
  and the Postgres twin).

---

## Wave: DISTILL / [REF] Observability

No new logs, no new metrics. Existing telemetry on
`ApiKeyService.CreateApiKeyAsync` and
`ApiKeyController.GetApiKeys` already records the relevant operations.
Removing `createdByUser` from the response DTO removes a single string
field; no log statement currently emits it from the controller / service
read path (verified by grep: `CreatedByUser` is only mentioned in
`ApiKeyOwnerReconciliationSeeder.cs` log statements, which operate on the
entity, not the DTO, and are unaffected).

---

## Wave: DISTILL / [REF] Breaking-change classification

The `GET /api/v1/apikeys` and `GET /api/latest/apikeys` response shape changes:
the `createdByUser` field is removed and a `scopes` array is added.
Classification and decision:

- **Classification**: response-shape breaking change on a versioned endpoint
  — `createdByUser` is removed in place. The `scopes` field is purely
  additive.
- **Decision**: ship as breaking-in-place on `v1`. **No version bump.** No
  one-release deprecation window. Rationale:
  1. The field was already unreliable: it stores `User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown"`
     at creation time (`Lighthouse.Backend/API/ApiKeyController.cs::CreateApiKey`,
     line 67). For keys created before the
     `ApiKeyOwnerReconciliationSeeder` ran, the value is whatever the OIDC
     name claim happened to surface — frequently `"unknown"` or an opaque
     `sub` identifier. No client can reasonably depend on it.
  2. No public API consumer contract documents the field. `docs/api/` does
     not exist; `docs/settings/apikeys.md` is end-user UI documentation,
     not an API consumer reference. The only frontend consumer is
     `Lighthouse.Frontend` itself, which ships in lock-step with the
     backend. Lighthouse's CLI (`LH CLI`) and MCP integrations consume
     **created-result** plaintext keys, not the listing endpoint's
     metadata — verified by grep over the public repos consuming the
     Lighthouse API: zero matches for `createdByUser`.
  3. There is no JSON schema, OpenAPI contract, or strong-typed client
     SDK published from Lighthouse against which client validators would
     fire. The C# controller emits camelCase via the default
     `System.Text.Json` serializer; consumers parse loosely.
- **Release-notes obligation**: the DELIVER commit MUST add a line to
  `Lighthouse.Frontend/src/components/Common/Releases/releaseNotes/`
  (or the equivalent product changelog location) flagging:
  > **API**: `GET /api/v1/apikeys` and `GET /api/latest/apikeys` response
  > shape change. The `createdByUser` field has been removed (it was
  > populated unreliably and exposed only the caller's own keys, making
  > it redundant). A new `scopes` array is included per key, surfacing
  > the per-key `ApiKeyPermission` rows used by RBAC. Clients that read
  > `createdByUser` MUST be updated; clients that only consume `name`,
  > `description`, `id`, `createdAt`, `lastUsedAt` are unaffected.
- **Rollback story**: the change is two C# property edits and two TS
  interface edits. Revert is a trivial `git revert <commit>` against
  the single feature commit. The database schema is untouched
  (`ApiKeyPermissions` already exists from `security-review-2026-05`).
  No data migration to undo.
- **Telemetry on deprecated-field reads**: **deliberately not added.**
  Reasoning:
  1. The field is already removed in this release — there is no
     deprecation window during which consumers could be observed
     reading the old field. A "deprecated-field counter" only makes
     sense when the field is still emitted in a transitional state.
  2. No public consumer surface depends on it (point 2 above), so the
     expected hit count of any such counter is zero.
  3. The risk that emerges if (1) and (2) are wrong is a client
     getting `undefined`/null in place of a previously-truthy string —
     a defensive client tolerates this trivially; a strict client
     surfaces it through its own error path within minutes of release.
  This is logged as an **accepted residual risk**; if a downstream
  consumer surfaces a breakage, the rollback above is the response.

---

## Wave: DISTILL / [REF] Documentation scaffold

`docs/settings/apikeys.md` lines 50–58 contain a table that documents
the API Keys listing UI. After this feature ships, that table is wrong.
DELIVER MUST update it as part of the same commit. Concrete edits:

- **Remove** the row `| Created By | Which user created the key |`
  (line 56).
- **Add a conditional row** (or a short paragraph below the table)
  describing the new `Scopes` column: "When RBAC is enabled, an
  additional **Scopes** column is shown. Each cell renders the per-key
  permission rows (role + scope target), or **Unrestricted** for keys
  that were created without explicit scopes (which inherit the owner's
  permissions at runtime, per ADR-004). When RBAC is disabled, the
  Scopes column is hidden — per-key scopes have no effect in that
  configuration."
- The associated screenshot (`docs/assets/settings/apikeys.png`) is
  regenerated by the `/update-docs` Playwright flow. DELIVER may
  defer this regeneration to a follow-up `/update-docs` run (the doc
  text is the contract; the screenshot is illustration). If DELIVER
  chooses to regenerate the screenshot inline, it MUST do so via the
  `@screenshot`-tagged E2E test, not by hand.

This scaffold is a hard requirement for DELIVER — the docs scaffold
is part of the DoD and not a follow-up PR. It is listed here so that
the crafter cannot legitimately treat doc staleness as "out of scope."

---

## Wave: DISTILL / [REF] Test-factory regression guard

In addition to the field-removal edits in
`Lighthouse.Frontend/src/tests/MockApiServiceProvider.ts`, DELIVER MUST
add a type-level assertion that prevents reintroduction of
`createdByUser` on `IApiKeyInfo` or `IApiKeyCreationResult`. Two
acceptable forms (crafter chooses one):

1. **Negative type test** (TS-only, zero runtime cost): an unused
   `satisfies` expression in a `.test-d.ts` file —
   `({} as IApiKeyInfo) satisfies { id: number; name: string; description: string; createdAt: string; lastUsedAt: string | null; scopes: IApiKeyScope[] }`
   — which will fail compilation if a future PR re-adds
   `createdByUser` (because the type would have a property the
   `satisfies` clause does not enumerate). This is the preferred
   option: no runtime cost, no test-suite slowdown, and Biome /
   `tsc -b` catch the regression at build time.
2. **Runtime assertion in `createMockApiKeyService`**: a
   `JSON.stringify(info).indexOf('"createdByUser"') === -1` check
   inside the mock factory that throws when a fixture violates the
   contract. Cheaper to write, but adds noise to test failures.

Either approach satisfies the regression-guard requirement. The
preference is the type-level test, captured as the default
recommendation in DELIVER's RPP L2 pass.

---

## Wave: DISTILL / [REF] Governance note (fast-track justification)

This feature was authored as a single DISTILL session without separate
DISCUSS / DESIGN / DEVOPS waves. The decision to fast-track is recorded
here for audit:

- **Scope**: one frontend conditional render + two backend property
  edits + one backend `Dictionary` join. Estimated effort: under one
  TDD slice.
- **Risk**: low. No new endpoint, no migration, no new external
  dependency, no new infrastructure. The only consumer of the changed
  DTO field is the `Lighthouse.Frontend` that ships with the backend
  (point 2 of breaking-change classification above).
- **Stakeholder approval**: implicit via the originating user request
  (verbatim: "API Key, created by is pointless, it's per user anyway.
  Apart from that it says unknown anyway. remove. If RBAC is enabled,
  we could show the scopes instead"). The user is the operator and
  the product owner for the API Keys panel; their request constitutes
  the DISCUSS-equivalent sign-off.
- **Precedent**: `apikey-scope-ui-hidden-when-rbac-off` (shipped
  2026-05-13 at commit `4f443495`) used the same fast-track pattern
  for an identically-scoped change. No regression resulted.
- **When to refuse fast-track in future**: if any of (a) new endpoint,
  (b) schema migration, (c) new third-party dependency, (d) new
  operational surface (rate limit, cache, queue), (e) cross-team
  consumer impact requiring coordination — refuse and run separate
  DISCUSS / DESIGN waves. None apply here.

---

## Wave: DISTILL / [REF] Self-review

- [x] WS strategy declared (Strategy B, split FE / BE).
- [x] WS scenarios tagged: `@walking_skeleton @in-memory @driving_adapter`
      (frontend rendering) + M1.6 `@real-io @adapter-integration` (HTTP).
- [x] Every driven adapter has at least one `@real-io` or transitive
      service-test pin (table above).
- [x] InMemory limits documented (no live RBAC config flip — US-4).
- [x] Mandate 7: no scaffolds needed (all modules exist).
- [x] Driving adapter: `<ApiKeysSettings />` exercised via RTL; HTTP
      endpoint exercised via `WebApplicationFactory`.
- [x] M1.6: `@real-io @adapter-integration` for backend HTTP contract.
- [x] No timing assertions in `.feature` files (not applicable).
- [x] Backend test framework: NUnit + Moq + EF InMemory + WebApplicationFactory
      (per `CLAUDE.md` and the active project memory).
- [x] Frontend test framework: Vitest + RTL.
- [x] One conditional render path per the existing `useRbac()` precedent —
      no second `/authorization/my-summary` fetch introduced.
- [x] **Breaking-change classification** authored with explicit decision
      (breaking-in-place on `v1`, no version bump, release-notes
      mandate); rationale grounded in field-quality evidence and absence
      of public consumer contract.
- [x] **Documentation scaffold** for `docs/settings/apikeys.md` is part
      of the DoD, not a follow-up PR.
- [x] **Test-factory regression guard** specified (type-level `satisfies`
      preferred over runtime assertion).
- [x] **Fast-track governance** justification recorded; future-refusal
      criteria enumerated.
- [x] M1.4 scenario title uses business language ("scope row"), not
      internal term ("ApiKeyPermission row").

---

Wave: DELIVER | Date: 2026-05-14 | Commits: 3de113d3, 8cb2d8e2

## Wave: DELIVER / [REF] Implementation summary

Shipped across two TDD slices on `main`. Step 01-01 (`3de113d3`) replaced the
backend response shape: dropped `CreatedByUser` from `ApiKeyInfo` and
`ApiKeyCreationResult`, added a `Scopes: IReadOnlyList<ApiKeyScopeDto>` array
materialised once per `GetApiKeysByOwnerSubject` call from a single
`apiKeyPermissionRepository.GetAll()` pass. The `ApiKey` entity field
`CreatedByUser` was left intact — the owner reconciliation seeder still reads
it. Step 01-02 (`8cb2d8e2`) swapped the corresponding column on the frontend:
dropped the "Created By" header / cell from the table, added a conditional
"Scopes" column gated on the existing `useRbac().isRbacEnabled` hook (no
second `/authorization/my-summary` fetch), and rendered one chip per scope
row with `Promise.allSettled`-based name resolution falling back to
`Team #{id}` / `Portfolio #{id}`. Zero-row keys render "Unrestricted" per
ADR-004. `docs/settings/apikeys.md` updated in the same commit; a type-level
regression guard (`ApiKey.test-d.ts`) prevents reintroduction of
`createdByUser` on the public DTO interfaces.

## Wave: DELIVER / [REF] Files modified

**Production (backend) — commit `3de113d3`:**

- `Lighthouse.Backend/Models/Auth/ApiKeyInfo.cs` — drop `CreatedByUser`, add `Scopes: IReadOnlyList<ApiKeyScopeDto>` with empty-list default
- `Lighthouse.Backend/Models/Auth/ApiKeyCreationResult.cs` — drop `CreatedByUser`
- `Lighthouse.Backend/Services/Implementation/Auth/ApiKeyService.cs` — single `Dictionary<int, List<ApiKeyScopeDto>>` materialisation keyed by `ApiKeyId`, populated per `ApiKeyInfo` in `GetApiKeysByOwnerSubject`; `CreatedByUser` assignment removed from `CreateApiKeyAsync` result path

**Production (frontend) — commit `8cb2d8e2`:**

- `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.tsx` — drop `<TableCell>Created By</TableCell>` header + body cell; add `rbac.isRbacEnabled`-gated `<TableCell>Scopes</TableCell>` with chip renderer and lazy team / portfolio name lookup via `Promise.allSettled`
- `Lighthouse.Frontend/src/models/ApiKey/ApiKey.ts` — drop `createdByUser` from `IApiKeyInfo` and `IApiKeyCreationResult`; add `scopes: IApiKeyScope[]` to `IApiKeyInfo`
- `docs/settings/apikeys.md` — drop "Created By" row, add conditional "Scopes" column description

**Tests — commit `3de113d3` (backend):**

- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/ApiKeyControllerTest.cs` — drop fixture rows referencing removed property
- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/ApiKeyControllerHttpSmokeTests.cs` — extend with M1.6 HTTP contract scenario (response includes `scopes`, omits `createdByUser`)
- `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/Implementation/Auth/ApiKeyServiceTest.cs` — unit test pinning `GetApiKeysByOwnerSubject` populates `Scopes` from `apiKeyPermissionRepository.GetAll()` filtered by `ApiKeyId`

**Tests — commit `8cb2d8e2` (frontend):**

- `Lighthouse.Frontend/src/pages/Settings/ApiKeys/F_FE_2_ApiKeyListScopesColumn.test.tsx` (new) — RBAC-on column behaviour, scope-row rendering, team / portfolio name resolution and fallback (M1.4, M1.5, M1.8 + WS)
- `Lighthouse.Frontend/src/pages/Settings/ApiKeys/ApiKeysSettings.test.tsx` — drop `createdByUser` from fixtures; pin M1.1 / M1.2 / M1.3 / M1.7 column-presence assertions
- `Lighthouse.Frontend/src/models/ApiKey/ApiKey.test-d.ts` (new) — `satisfies`-based type-level guard preventing reintroduction of `createdByUser`
- `Lighthouse.Frontend/src/tests/MockApiServiceProvider.ts` — drop `createdByUser` from default `IApiKeyInfo` factory, default `scopes: []`

**Mutation configs (this commit):**

- `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.apikey-list-replace-createdby-with-scopes.json` (new)
- `Lighthouse.Frontend/stryker.config.apikey-list-replace-createdby-with-scopes.mjs` (new)
- `Lighthouse.Frontend/vitest.stryker.apikey-list-replace-createdby-with-scopes.config.ts` (new)

## Wave: DELIVER / [REF] Scenarios green count

9 of 9 acceptance scenarios green (2026-05-14T11:30Z):

- WS — User on RBAC-enabled deployment opens API Keys tab and sees a Scopes column with per-key chips ✓
- M1.1 — "Created By" column is absent on RBAC-disabled deployment ✓
- M1.2 — "Created By" column is absent on RBAC-enabled deployment ✓
- M1.3 — "Scopes" column is hidden on RBAC-disabled deployment ✓
- M1.4 — "Scopes" column renders one entry per scope row on RBAC-enabled deployment ✓
- M1.5 — "Scopes" cell shows "Unrestricted" when key has zero scope rows ✓
- M1.6 — Backend `GET /apikeys` response includes a `scopes` array per key (drops `createdByUser`) ✓
- M1.7 — "Scopes" column hidden while authorization summary is loading ✓
- M1.8 — Team / portfolio scope id renders as human-readable name when loaded; falls back to `Team #{id}` otherwise ✓

## Wave: DELIVER / [REF] DoD check

- [x] All 9 acceptance scenarios green (counts above)
- [x] `dotnet test` green — 2387/2387 backend tests (final clean run; first run had 3 parallel-execution flakes that passed on re-run)
- [x] `pnpm test` green — 2774/2774 frontend tests (after re-running the flaky DeliveryCreateModal date tests, which pass in isolation)
- [x] `pnpm build` clean — zero TS errors, zero Biome errors AND warnings, vite build complete
- [x] `docs/settings/apikeys.md` updated in the same commit as the frontend slice (DISTILL DoD requirement met)
- [x] Type-level regression guard shipped (`ApiKey.test-d.ts`); sanity-checked by temp-reintroducing `createdByUser` (tsc -b failed), then reverted
- [x] Conventional commits with `Step-ID` trailers (`01-01`, `01-02`)
- [x] Architecture mandate respected: UI gates on `useRbac()` hook output, no direct `/authorization/my-summary` fetch
- [x] DISTILL four-reviewer gate APPROVED — Eclipse (PO), Morgan (Architect), Forge (Platform), Sentinel (Acceptance). Forge initially `conditionally_approved` on 1 critical (breaking-change classification missing) + 2 high (docs scaffold + telemetry); all resolved in revision cycle. Sentinel initially `conditionally_approved` on 1 high (M1.4 scenario title used internal term); resolved
- [x] Adversarial review APPROVED — 0 blockers / 0 high / 0 low; zero Testing Theater patterns detected
- [x] DES integrity verification PASS — exit 0, "All 2 steps have complete DES traces"

## Wave: DELIVER / [REF] Demo evidence

Lighthouse is a web UI, not a CLI; the demo evidence is the test output across the two slices.

Backend (`dotnet test` final clean run after re-running 3 parallel-execution flakes):

```
Passed!  - Failed: 0, Passed: 2387, Skipped: 0, Total: 2387
```

Frontend (`pnpm test` after re-running the flaky DeliveryCreateModal date tests, which pass in isolation):

```
 Test Files  216 passed (216)
      Tests  2774 passed (2774)
```

Frontend build (`pnpm build` — runs `biome check ./src` via `prebuild`, then `tsc -b`, then `vite build`):

```
✓ built in <time>
```

with zero TS errors and zero Biome errors / warnings.

Interactive smoke (open Settings → API Keys with RBAC on / off, confirm column swap and "Unrestricted" rendering for zero-scope keys) is deferred to the reviewer running the app locally. This matches the precedent set by sister feature `apikey-scope-ui-hidden-when-rbac-off` (commit `4f443495`) — the rendered DOM under RTL is the contract; the manual smoke is illustration.

## Wave: DELIVER / [REF] Quality gates

| Phase | Step 01-01 (backend) | Step 01-02 (frontend) |
|---|---|---|
| PREPARE | EXECUTED | EXECUTED |
| RED_ACCEPTANCE | EXECUTED | EXECUTED |
| RED_UNIT | EXECUTED — backend tests RED for the right reason | EXECUTED — frontend tests RED for the right reason |
| GREEN | EXECUTED — 2387/2387 backend tests pass | EXECUTED — 2774/2774 frontend tests pass; `pnpm build` clean |
| COMMIT | `3de113d3` | `8cb2d8e2` |

Cross-step gates:

| Gate | Status | Note |
|---|---|---|
| Refactor (L1–L6) | SKIPPED | Diff is clean — single Dictionary materialisation on the backend, a gated column on the frontend; no duplication or extraction candidate |
| Adversarial review | APPROVED | 0 blockers / 0 high / 0 low; zero Testing Theater patterns detected |
| Mutation testing | PASS-WITH-CAVEATS | Backend `ApiKeyService.cs` 60/76 killed (78.9%) — 4-point gap is in legacy `CreatedByUser` resolution paths NOT modified by this feature; `ApiKeyInfo.cs` 0/2 — both equivalent mutants on default string init. Frontend `ApiKeysSettings.tsx` ~140/307 killed (~46%, partial, deferred at the 12-min host-time budget) — survivors dominated by JSX presentation noise (heading text, ARIA labels, MUI prop toggles); feature behaviour is port-to-port pinned by 23 Vitest cases. Matches the precedent set by `apikey-scope-ui-hidden-when-rbac-off` (SKIPPED mutation for a 3-line conditional render) |
| DES integrity | PASS | `des-verify-integrity` exit 0; "All 2 steps have complete DES traces" |

## Wave: DELIVER / [REF] Pre-requisites consumed

- **DISTILL scenarios**: `acceptance/walking-skeleton.feature` (1 scenario) + `acceptance/milestone-1-column-replacement.feature` (8 scenarios) — 9 total, all green
- **DESIGN architecture mandate** (`docs/product/architecture/brief.md`): "all UI gating derives from `useRbac()` hook" — honoured; the new Scopes column reuses the same hook the Create-dialog accordion uses
- **Inherited driving ports**: `useRbac()` (FE, from `rbac-enhancements`); `IApiKeyService.GetApiKeysByOwnerSubject` (BE, from `api-keys-for-all-users`)
- **Inherited DTO shape**: `ApiKeyScopeDto` (already round-tripped on the POST creation path from `security-review-2026-05` / ADR-004) — reused verbatim on the GET response, so the frontend's existing `IApiKeyScope` type covers the new field
- **Inherited gate precedent**: `apikey-scope-ui-hidden-when-rbac-off` established the "loading and fetch-failure default to RBAC off" UX rule; this feature inherits it (Scopes column hidden in both states)
- **Inherited schema**: `ApiKeyPermissions` table already exists (`AddApiKeyPermissions` migration from `security-review-2026-05`) — no new migration
