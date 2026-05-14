# ADR-009: OAuth Callback URL Derived From a Server-Configured BaseUrl

**Status**: Accepted
**Date**: 2026-05-14
**Feature**: work-tracking-oauth-authentication
**Decider**: Morgan (Solution Architect) based on DISCUSS D4 + Story 4972

---

## Context

The OAuth callback URL is the address the IdP redirects the user's browser to after consent. It must match — exactly — the URL the connector-admin registers with the IdP (Atlassian developer console for Jira; Entra ID app registration for ADO). A mismatch means the IdP rejects the redirect; the user sees a generic IdP error and has no path back to Lighthouse.

ASP.NET Core's `Request.Host` / `Request.Scheme` are not safe sources for this URL because Lighthouse is typically deployed behind a reverse proxy. The internally observed host (`http://lighthouse:8080` inside the container) differs from the public host the user's browser sees (`https://lighthouse.example.com`). Using `Request.Host` would display a callback URL the IdP cannot reach.

Two configuration strategies were evaluated.

---

## Decision

**Introduce a single server-side setting `Lighthouse:BaseUrl` (in `appsettings.json` / environment variables). The OAuth callback URL displayed in the form is computed as `{BaseUrl}/api/oauth/callback`. The same value is used at runtime when the controller computes the `redirect_uri` parameter sent to the IdP and when the callback handler computes the 302 target back to the connection settings page.**

Surface:
- New property `IServiceConfig.BaseUrl { get; }` (`Lighthouse.Backend/Lighthouse.Backend/Services/Interfaces/IServiceConfig.cs`).
- Bound to `Lighthouse:BaseUrl` via the existing `IOptions<>` pattern.
- The frontend reads it via a new field on the existing system-info endpoint (additive, follows `adr-006-connection-list-payload-shape.md` precedent).
- When `BaseUrl` is unset:
  - The OAuth form renders a **non-blocking** validation warning: *"Your callback URL may be incorrect. Set `Lighthouse:BaseUrl` in your server configuration to guarantee OAuth registration works."*
  - The form remains usable (the admin may have configured the BaseUrl indirectly via `ASPNETCORE_URLS` and accept the fallback).
  - The fallback display is `{Request.Scheme}://{Request.Host}/api/oauth/callback`, with the warning making the imprecision explicit.
- For ADO (`ado.oauth`) **only**, an additional warning appears when `BaseUrl` is HTTP (not HTTPS): *"Azure DevOps requires HTTPS callback URLs in production."*

---

## Consequences

**Positive**

- Single source of truth for the callback URL display (US-01 AC #6, US-03 AC #2).
- Works correctly behind reverse proxies without any X-Forwarded-* header dance.
- The warning is honest: Lighthouse cannot detect whether the admin's proxy is correctly forwarding traffic, so it advises explicit configuration without blocking.
- The HTTPS warning for ADO is provider-specific knowledge that lives in the form (admin-visible) and in the docs; the API does not refuse to connect, because that is the IdP's call at registration time.

**Negative**

- One more server-config setting to document. Mitigated by a dedicated docs page (US-01 acceptance criterion — *Configuring your server's public URL for OAuth callback registration*) with worked examples for nginx, Caddy, and Traefik.
- A misconfigured BaseUrl produces a confusing error at IdP-redirect time (the user is bounced to a Lighthouse URL that does not match the registered one and sees a generic IdP error). Mitigated by the warning on the form *before* the admin clicks **Connect**.

---

## Alternatives considered

### Alternative A — Derive callback from `Request.Host` / `Request.Scheme`

- Rejected: incorrect behind reverse proxies (the default Lighthouse deployment shape). Would require X-Forwarded-* trust configuration AND admin-side proxy configuration to be exactly right.

### Alternative B — Per-connection callback URL override

Let the admin paste an arbitrary callback URL per connection.

- Rejected: this is what the admin *registers with the IdP*, not configures inside Lighthouse. The Lighthouse callback handler is at a fixed route (`/api/oauth/callback`); only the public origin is configurable. Allowing arbitrary callback URLs would let a misconfiguration silently route the OAuth dance to a non-existent endpoint.

### Alternative C — Discover at startup via outbound HTTP self-probe

Lighthouse calls its own public hostname at boot, reads the response, and caches the BaseUrl.

- Rejected: requires Lighthouse to know its own public hostname before it can probe it — circular. Also requires outbound HTTP from the deployment, which some air-gapped/restricted deployments forbid.

---

## References

- DISCUSS D4: "Callback URL is derived from a server-configured BaseUrl setting, NOT from the request origin."
- Story 4972: "The callback URL is always correct for my setup"
- US-01 AC #6-7, US-03 AC #2
- Precedent for additive system-info payload: `adr-006-connection-list-payload-shape.md`
