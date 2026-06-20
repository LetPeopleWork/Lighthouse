# Slice 08 (backend story): Lighthouse API accepts IdP JWT bearer tokens (hosted MCP OAuth)

**Feature**: epic-5305-k8s-readiness · **Epic**: ADO #5305 · **Story**: ADO #5329 (Active) · **ADR**: ADR-079 · **Status**: DELIVER in progress
**Pairs with**: ADO #5330 — `mcp-http` advertises MCP OAuth protected-resource metadata (separate `lighthouse-clients` repo).

## User story

As a **platform-operator running Lighthouse in a cluster**, I want each MCP user to authenticate with
their **own OIDC token** (no API key), so every MCP-driven call runs with that user's own RBAC scope
and audit — using the same identity provider the browser already uses.

## Goal

Add a **JWT Bearer** credential to the smart-auth selector: validate the IdP's access token on API
requests against the **same OIDC authority**, map its claims through the existing
`CurrentUserProfileService` + RBAC. The MCP HTTP server already forwards the caller's Bearer
(epic-5305 slice-06); this is the server-side half that makes it work end to end.

## IN scope (backend)

- `AddJwtBearer("LighthouseJwtBearer", …)` configured from the existing `Authentication:Authority` /
  metadata; validate issuer + **audience (= Lighthouse API)** + signature (JWKS) + lifetime;
  `MapInboundClaims = false`.
- Extend the `LighthouseSmartAuth` `ForwardDefaultSelector`: `Authorization: Bearer …` → the JWT
  scheme (X-Api-Key and cookie paths unchanged).
- Reuse `CurrentUserProfileService.GetOrCreateFromPrincipalAsync` (sub→UserProfile) + the existing
  RBAC permission derivation — **no bearer-specific authorization logic**.
- Standalone gate (D1): register the JWT scheme only inside the existing `authConfig.Enabled` +
  authority-present branch; byte-identical when no authority is configured.

## OUT scope

- The clients-side MCP OAuth protected-resource metadata (separate clients story).
- oauth2-proxy (rejected, ADR-079).
- IdP app-registration / audience-scope setup (deployment prerequisite, documented with the #5306 chart).
- SPA adopting bearer (possible later; not required here).

## Acceptance criteria

- **AC1**: a request with a valid IdP access token (correct issuer + audience + signature) in
  `Authorization: Bearer` authenticates and resolves to that token's `sub` → its `UserProfile` →
  that user's RBAC scope (two distinct tokens each see only their own scoped data).
- **AC2**: a token with a **wrong/missing audience** (e.g. an `id_token`, or a token for another API)
  is rejected (401), not silently accepted.
- **AC3**: an expired or bad-signature token is rejected (401).
- **AC4** (precedence): `X-Api-Key` still routes to the API-key handler even if an `Authorization`
  header is also present (selector order unchanged).
- **AC5 (D1)**: with no OIDC authority configured, no JWT scheme is registered and behaviour is
  byte-identical to today (standalone single container).

## Cross-cutting (DoR gate — record explicitly)

- **RBAC**: central — bearer principals flow through the **same** `IRbacAdministrationService` path as
  cookie principals (sub + group claims → `UserPermission` / group-mapping virtual permissions). No
  new RBAC port.
- **Lighthouse-Clients**: the matching clients story makes `mcp-http` advertise MCP OAuth metadata so
  clients obtain the token; the Bearer **forwarding** is already shipped (slice-06). Version-gate the
  OAuth path `> v26.6.16.14` per ADR-079.
- **Website**: N/A — security/packaging for hosted deployments, not a marketed UI surface.

## Step-by-step TDD plan (build incrementally)

1. **Selector routing (unit).** A request with `Authorization: Bearer` selects `LighthouseJwtBearer`;
   `X-Api-Key` still selects the api-key scheme even when both headers are present (AC4). Drives the
   `ForwardDefaultSelector` change. Assert via DI/scheme resolution — **do not** GET an unmapped path
   from `WebApplicationFactory` (SPA-trap rule, ci-learnings).
2. **Token validation config (integration).** Stand up a test JWT signer (test RSA key + a static
   `ConfigurationManager`/JWKS, mirroring `PreSeededOidcConfiguration` in
   `ForwardedHeadersOidcTestHost`). Valid token → 200 on a scoped endpoint (AC1); wrong audience →
   401 (AC2); expired/bad-signature → 401 (AC3). Reuse `McpInboundAuthTestHost`'s seeding shape
   (UserProfile + UserPermission + premium gate) but present a Bearer instead of `X-Api-Key`.
3. **Per-user scope (integration).** Two distinct tokens (distinct `sub`, each with its own
   `UserPermission`) each see only their own team (200 own / 404 other) — the bearer analogue of the
   slice-06 #32/#33 tests.
4. **Standalone gate (integration).** With no authority configured, assert (via `EndpointDataSource`
   / scheme registration) that no JWT scheme exists and a normal request path is unchanged (AC5).
5. **Mutation** ≥80% on the new selector + validation wiring; justify equivalents.
6. **Live dogfood**: a real IdP (Keycloak/Entra per story-05 lineage) issues an API-audience access
   token; drive `mcp-http` → Lighthouse with it; confirm per-user RBAC + audit. Smoke a wrong-audience
   token → 401.

## Notes / prerequisites

- IdP must expose a **Lighthouse API audience/scope**; MCP client must use **resource indicators
  (RFC 8707)** so `aud` is correct (ADR-079 prerequisites). These are deployment-config, surfaced
  with the #5306 chart docs.
- Parented under epic #5305 (sibling of the slice-06 MCP-auth story #5307), keeping the MCP-auth thread together.
