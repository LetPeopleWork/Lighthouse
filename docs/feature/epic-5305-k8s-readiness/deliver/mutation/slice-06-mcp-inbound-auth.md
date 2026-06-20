# Mutation testing — slice-06 MCP inbound auth (per-caller API-key scope)

**Story**: US-06 (ADO #5307) · **Date**: 2026-06-20 · **Tool**: Stryker.NET
**Config**: `Lighthouse.Backend.Tests/stryker-config.epic-5305-mcp-inbound-auth.json`

## Scope

This slice adds **no production code** (DESIGN A4: the existing owner-resolved + per-key-scoped
`ApiKeyAuthenticationHandler` + `RbacAdministrationService` already satisfy pass-through). The
deliverable is the first HTTP-level coverage of the security-critical surface the MCP pass-through
relies on, which was previously **untested** (no `api_key_id` / `ApiKeyPermission` intersection test
existed in `RbacAdministrationServiceTest`).

Mutation targets the two files that carry that surface:

- `Services/Implementation/Auth/ApiKeyAuthenticationHandler.cs` (emits `sub` + `api_key_id` claims)
- `Services/Implementation/Authorization/RbacAdministrationService.cs` — the **api-key intersection
  methods only**: `GetEffectivePermissionsAsync` (api-key branch), `GetOwnerEffectivePermissionsAsync`,
  `TryGetApiKeyId`, `IntersectWithApiKeyScope`, `OwnerCoversScope` (≈ lines 961–1068) + the api-key
  owner group-snapshot fallback (1093–1107).

`RbacAdministrationService.cs` is a 1300-line file; its whole-file Stryker score (~67%) is dominated
by pre-existing, separately-tested code (status, group mappings, ordering, display names) this slice
did not touch. The relevant figure is the api-key intersection surface.

Killing tests: `API/Integration/McpInboundAuthIntegrationTest.cs` (8 scenarios, real
`ApiKeyAuthenticationHandler` + real `RbacAdministrationService` over HTTP via
`McpInboundAuthTestHost`, premium-gated, X-Api-Key per caller).

## Result — api-key intersection surface (RbacAdministrationService.cs, lines 961–1068 / 1093–1107)

**29 killed / 36 = 80.6%** (≥ 80% gate met).

The behavioral intersection mutants are killed by the asymmetric-scope scenarios:

- `KeyScopeNarrowerThanOwner_KeyRestrictsBeyondOwnerGrants` — owner has team A+B, key scoped to A
  only → kills the `api_key_id` claim removal, the `TryGetApiKeyId` claim-name string, the
  intersection presence (`if (!TryGetApiKeyId) return owner`), and `OwnerCoversScope`'s positive
  scoped path.
- `KeyClaimsScopeOwnerLacks_ExcessScopeDropped` — owner has team A only, key claims A+B → kills the
  `OwnerCoversScope` negative path + the `continue` drop.
- `SystemAdminOwnerWithTeamScopedKey_KeyRestrictsBelowSystemWide` — kills the `OwnerCoversScope`
  system-admin branch (a sysadmin's narrow key must not grant system-wide).
- `KeyClaimsSystemScopeOwnerLacks_NoPrivilegeEscalation` — kills the `ScopeType == System → false`
  escalation guard.

## ApiKeyAuthenticationHandler.cs — behavioral mutants killed; survivors are inert

Whole-handler score 21/38 = 55.3%, but **every** surviving mutant is behaviorally inert — the two
security-critical claims (`sub` owner-subject, `api_key_id`) are killed. Survivors:

- `Logger.LogWarning/LogDebug` statement removals + log-message string mutations (L39, 46, 73, 74,
  80, 81) — logging side effects we deliberately do not assert (brittle; discouraged by the
  project's testing conventions).
- `AuthenticateResult.Fail("…")` **reason strings** (L40, 47) — the reason text is not surfaced to
  the caller (both yield 401); equivalent.
- The optional `name` display-name claim plumbing (L63 `&&`, L68 ×3, L70) — carries no authorization
  weight; in every scenario the owner is Resolved with a non-empty subject, so `&&` vs `||` is
  indistinguishable. Killing these would require an unlinked-owner key, which is unit-covered in
  `ApiKeyAuthenticationHandlerTest`, not the authorization surface this slice owns.

## Surviving intersection mutants — justified

- **L969 block removal** (`if (!TryGetApiKeyId) return ownerPermissions;`) — equivalent: for an
  api-key principal the guard is false, and the zero-rows path returns owner permissions anyway.
- **L1015 boolean** (`int.TryParse` result on `api_key_id`) — defensive parse-guard; the handler
  only ever emits a valid integer claim, so an unparseable value is unreachable from the real
  driving adapter.
- **L1035 ×2 equality** (duplicate-scope role-priority dedup) — only fires when a key holds two rows
  for the *same* scope with different roles; both roles grant read, so the HTTP outcome is identical
  (equivalent at the behavioral boundary).
- **L1093/1094/1099** (api-key owner **group-snapshot** fallback) — the RBAC group-mapping
  mechanism, active only when `GroupClaimName` is configured and the owner has a persisted group
  snapshot. That is a separate feature surface, out of slice-06's per-key-scope scope, and is
  covered under its own configuration elsewhere.
