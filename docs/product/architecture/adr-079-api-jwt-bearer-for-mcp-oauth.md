# ADR-079: Lighthouse API Accepts IdP JWT Bearer Tokens for Non-Browser Callers (Hosted MCP OAuth Pass-Through), Validated Against the Same OIDC Provider — Added as a Third Credential in the Smart-Auth Selector, Off Unless an Authority Is Configured; oauth2-proxy Rejected; Degrades to Standalone

**Status**: **Accepted** (2026-06-20 — accepted by Benjamin; supersedes the "OAuth pass-through to Lighthouse is blocked / deferred" framing recorded during epic-5305 slice-06). Backend story ADO #5329 (Active); clients story ADO #5330. Both children of epic #5305.
**Date**: 2026-06-20
**Feature**: epic-5305-k8s-readiness (ADO Epic #5305) — hosted-cluster MCP authentication; the no-API-key flow
**Decider**: Benjamin (product owner) + Morgan (Solution Architect, PROPOSE)
**Relationship to prior work**: BUILDS ON epic-5305 slice-06 (#5307), which shipped (a) the first HTTP-level coverage of Lighthouse's owner-resolved + per-key-scoped API-key authorization, and (b) `mcp-http` forwarding the caller's own credential (`X-Api-Key` **or** `Authorization: Bearer`) instead of a shared baked key. This ADR adds the **server-side** half that makes the Bearer path usable end-to-end. AMENDS the authentication wiring in `Program.cs` (`ConfigureAuthentication`, the `LighthouseSmartAuth` policy scheme). Honours D1 (standalone byte-identical when no OIDC authority is configured).

---

## Context

Lighthouse authenticates API requests today by exactly two mechanisms (`Program.cs` `ConfigureAuthentication`, the `LighthouseSmartAuth` policy scheme):

1. **Browser session (cookie).** The SPA does an interactive OIDC **authorization-code flow**: the server's `OpenIdConnect` handler exchanges the code, validates the `id_token` **once at login**, then mints Lighthouse's **own** cookie (`.Lighthouse.Session`). Every subsequent SPA→API call carries that cookie; the API validates a cookie **it issued itself** — it never re-checks an IdP token.
2. **`X-Api-Key`.** The smart-auth selector forwards requests carrying `X-Api-Key` to `ApiKeyAuthenticationHandler`, which owner-resolves the key (`ApiKey.OwnerSubject → sub`) and the RBAC layer intersects the owner's permissions with the per-key `ApiKeyPermission` scope.

For the **hosted Kubernetes** scenario the product goal is **no API key at all**: a user connects their MCP client, authenticates through the browser (OAuth 2.1 / the MCP Authorization spec rev 2025-06-18) against the **same OIDC provider Lighthouse already uses**, the client stores the resulting **access token**, sends it as `Authorization: Bearer` to the MCP HTTP server, and the MCP server forwards it to Lighthouse so the call runs **with that user's rights**.

The blocker: Lighthouse has **no code path that validates an inbound Bearer access token** on an API request. An MCP client is not a browser — it has no cookie jar and cannot perform Lighthouse's redirect/callback dance — so the cookie scheme cannot apply, and the `OpenIdConnect` handler only drives the interactive challenge/callback, not inbound-token validation. A non-`X-Api-Key`, non-cookie request currently falls through to the cookie/OIDC challenge (a 401/redirect), so a forwarded Bearer is simply ignored.

This is **not** "an OAuth server inside Lighthouse." It is the API learning to **accept the IdP's access token directly**, the same trust root the browser path already relies on — just validated per request instead of consumed once at login.

## Decision

**Add a third credential to the smart-auth selector: a JWT Bearer scheme that validates the IdP's access token on API requests, against the same OIDC authority, and maps its claims through the existing `CurrentUserProfileService` + RBAC. It is wired only when an OIDC authority is configured (the existing `Authentication:*` gate); absent, behaviour is byte-identical to today.**

- **Smart-auth selector (AMEND `LighthouseSmartAuth`).** Extend the `ForwardDefaultSelector`:
  - `X-Api-Key` present → `LighthouseApiKey` (unchanged).
  - `Authorization: Bearer …` present → **`LighthouseJwtBearer`** (new).
  - else → cookie (unchanged).
- **JWT validation (CREATE via `Microsoft.AspNetCore.Authentication.JwtBearer`).** `AddJwtBearer("LighthouseJwtBearer", …)` configured from the **same** `Authentication:Authority` / metadata as the OIDC handler. Validate **issuer**, **audience** (must equal the Lighthouse API audience — see prerequisites), **signature** (JWKS from the authority, keys cached/rotated by the handler), and **lifetime**. `MapInboundClaims = false` to keep raw claim types, mirroring the OIDC handler.
- **Identity mapping (REUSE — no new RBAC surface).** On a validated token, the principal carries `sub` (+ group claim). The existing `CurrentUserProfileService.GetOrCreateFromPrincipalAsync` resolves/creates the `UserProfile` from `sub`, and `RbacAdministrationService` derives effective permissions from explicit `UserPermission` rows + group-mapping virtual permissions — exactly as for the browser principal. No bearer-specific authorization logic.
- **Standalone gate (D1).** JWT bearer is registered only inside the `authConfig.Enabled` + authority-present branch that already gates OIDC. With no authority configured (the standalone single container), no JWT scheme is registered and the selector never routes to it — byte-identical to today.

## oauth2-proxy — rejected

An earlier option placed an `oauth2-proxy` in front of `mcp-http`. Rejected: MCP clients drive OAuth **themselves** (they present a Bearer per the MCP Authorization spec); a redirect-cookie proxy is the wrong shape for a programmatic client and would be a **redundant** edge gate once Lighthouse validates the token. A variant where the proxy validates and injects a trusted `X-Forwarded-User` identity header is also rejected: it only works **behind** the proxy (breaks the D1 standalone path), is still a backend change, and trusts a header instead of a cryptographically verified token. Lighthouse validating the JWT itself works standalone, behind any ingress, and is reusable by the SPA later.

## Prerequisites (IdP configuration)

- The IdP app-registration must **expose a Lighthouse API audience/scope** so issued **access tokens** carry `aud = <Lighthouse API>`. (The browser path validates an `id_token` whose audience is the client app — a different token; do not conflate them.)
- The MCP client must request a token **scoped to Lighthouse** via OAuth **resource indicators (RFC 8707)** so the `aud` is correct end to end.
- Same authority/issuer as the existing `Authentication:Authority`.

## Consequences

- **Positive**: the hosted no-API-key flow works end to end with per-user RBAC + audit, no shared secret, no confused deputy, no second auth system (same IdP, same claim→rights mapping). `mcp-http` already forwards the Bearer (slice-06), so the client side is ready; the SPA could later adopt bearer too.
- **Negative / cost**: token validated on **every** request (cheap — JWKS cached); requires the IdP audience/scope config above; audience misconfiguration is the likely first-failure mode (validate `aud` explicitly and log a clear reason). Access-token revocation is bounded by token lifetime (standard OAuth trade-off).
- **Clients version gate**: once the clients wrap a server-version-dependent OAuth path, pin it `> v26.6.16.14` in `FEATURE_REQUIRES_SERVER_NEWER_THAN` so an old server fails with a clear "upgrade Lighthouse" message rather than an opaque 401/404 (the `X-Api-Key` path needs no gate — it reuses the long-existing endpoint).

## Alternatives considered

1. **Keep X-Api-Key only (status quo + slice-06).** Stays the model for CLI, stdio MCP, and standalone (no OAuth infra). Does not meet the hosted "no API key" goal — retained as the fallback, not the hosted answer.
2. **oauth2-proxy edge termination.** Rejected (above).
3. **mcp-http maps OIDC `sub` → a stored per-user API key.** Adds a key store + lookup-by-owner and still issues keys; defeats the "no API key" goal. Rejected.

## Scope / sequencing

Out of slice-06 (which is zero-backend). Delivered as a **new backend story** ("API accepts IdP JWT bearer") + a **clients story** ("`mcp-http` advertises MCP OAuth protected-resource metadata"), built **step by step**. Likely sits under epic-5305 (cluster-readiness) or Productization #5306; the audience/scope IdP setup is a deployment prerequisite documented with the chart.
