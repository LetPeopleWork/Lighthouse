# Slice 01: Reverse-proxy forwarded headers

**Feature**: epic-5305-k8s-readiness
**Story**: US-01 (ADO #5311) → job-operator-correct-behind-proxy
**Estimate**: ~0.5–1 crafter day
**Reference class**: config-gated startup wiring, similar to `auth-allowedorigins-envvar-binding-fix` (env-bound ASP.NET Core middleware config, off unless declared)

## Goal
Make Lighthouse honour `X-Forwarded-Proto` / `-Host` / `-For` from a declared, trusted reverse proxy so HTTPS redirects, secure cookies, OIDC callback URLs and SignalR negotiation use the real public scheme + host — config-gated and OFF unless a proxy is declared.

## IN scope
- `UseForwardedHeaders` wired with a `ForwardedHeadersOptions` populated from configuration: known proxies / known networks (CIDR), forwarded-header count limit.
- A single config switch (env var + appsettings) that turns forwarded-header trust on and declares the trusted proxy set; default OFF.
- OIDC callback URL + `RequireHttpsMetadata`/redirect behaviour derive from the forwarded scheme/host when trust is on.
- Secure-cookie + HTTPS-redirect behaviour consistent with the forwarded scheme.

## OUT scope
- The Ingress / Traefik manifests themselves (Productization epic #5306, chart story 09).
- Edge auth (oauth2-proxy) — north-star, not this slice.
- Health-check endpoints → slice 02.

## Learning hypothesis
**Confirms if it succeeds**: a real OIDC login through a TLS-terminating proxy completes first try (no http:// callback, no redirect loop, secure cookie persists).
**Disproves if it fails**: ASP.NET Core forwarded-header handling is insufficient for our SignalR negotiation path and we need per-endpoint handling rather than one global middleware.

## Acceptance criteria
See US-01 in `../feature-delta.md`. Key: with trust ON and a simulated `X-Forwarded-Proto: https` + `X-Forwarded-Host`, an integration test asserts the generated OIDC redirect/callback URL is `https://<public-host>/...`; with trust OFF (no proxy declared), behaviour is byte-identical to today (standalone gate).

## Dependencies
None. Foundation slice — unblocks correct auth on any proxied deployment; should land before any cluster auth testing.

## Production data requirement
**Required.** Smoke a real OIDC login (Keycloak or the configured provider) through an actual reverse proxy (local Traefik/nginx), not just a unit test with synthetic headers.

## Dogfood moment
The dev instance, placed behind a local Traefik with TLS, logs in via OIDC over the HTTPS hostname within the same day.

## Cross-cutting checklist (confirmed in feature-delta)
RBAC: N/A — no authorization surface changes; only how the app derives scheme/host. Clients: N/A — no API contract change. Website: N/A — operational, not a marketed surface.

## Pre-slice spike candidates
- Confirm SignalR negotiation respects `UseForwardedHeaders` ordering relative to other middleware. (~1 hr)
- Verify the existing OIDC setup reads the request scheme/host (not a hardcoded base URL) so forwarded headers actually flow through. (~30 min)
