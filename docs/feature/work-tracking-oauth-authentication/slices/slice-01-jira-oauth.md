# Slice 01 — Jira OAuth (foundation slice)

**Feature**: work-tracking-oauth-authentication
**ADO stories rolled in**: #4967 (Jira OAuth) + #4971 (provider-agnostic abstraction) + #4972 (BaseUrl)
**Effort estimate**: 1 day of crafter dispatch (~6 hours)
**Reference class**: extension of `AuthenticationMethodSchema` + new `IOAuthProvider` port + new HTTP routes

## Goal
A `connector-admin` configures a Jira connection authenticated via OAuth 2.0 (3LO) end-to-end against a real Atlassian OAuth app, completes the consent dance, and Lighthouse syncs Jira work items using the issued bearer token.

## IN scope
- New auth-method keys `jira.oauth` (premium-gated) registered in `AuthenticationMethodSchema`.
- `IOAuthProvider` port (`BuildAuthorizationUrl`, `ExchangeCode`, `RefreshToken`, `ProviderName`, `DefaultScopes`) + DI registry keyed by provider name (string).
- `JiraOAuthProvider` concrete implementation.
- `OAuthConfiguration` and `OAuthCredential` EF entities + migration (SQLite + PostgreSQL via existing `CreateMigration` script).
- HTTP routes `POST /api/oauth/jira/connect`, `GET /api/oauth/callback`, `POST /api/oauth/jira/disconnect`.
- Frontend: OAuth option in the Jira connector auth-type dropdown (premium-gated); OAuth form (clientId, clientSecret, read-only callback URL); redirect-back success/error state.
- `Lighthouse:BaseUrl` server setting + non-blocking validation warning when unset.
- `JiraWorkTrackingConnector` extended to use `Bearer {accessToken}` when the connection's auth method is `jira.oauth`.
- Three docs pages: *Setting up Jira OAuth*, *Configuring BaseUrl for OAuth callback registration*, and a contributor-facing *Adding a new OAuth provider*.
- E2E: one happy-path scenario using a stub `IOAuthProvider` (the abstraction's own test fixture) plus a manual smoke against a real Atlassian sandbox app (recorded in the PR description).

## OUT scope
- Token refresh — Slice 02.
- ADO provider — Slice 03.
- Standalone-mode guard — Slice 04.
- Per-user OAuth, OAuth-app auto-registration, third-provider concretions.
- Audit-log surface beyond what existing `LighthouseDbContext` persistence already emits.

## Learning hypothesis
**Disproves if it fails**: "the `IOAuthProvider` abstraction (string-keyed, DI-resolved, separated configuration vs. credential storage) is honest end-to-end". The truth test is in AC #8 — a developer can add a stub provider without touching the controller, the credential entity, or the persistence migrations. If AC #8 cannot be met without backend changes, the abstraction was decoration and the slice must be reshaped.

**Confirms if it succeeds**: "the Jira → Lighthouse OAuth contract is correctly modelled (separate config + credential, BaseUrl-derived callback, premium-gated, server-only)". Slices 02 and 03 then become low-risk additions on the same shape.

## Acceptance criteria
See US-01 AC #1–8 in `feature-delta.md`.

## Production-data requirement
Acceptance does NOT permit synthetic-only data: the manual smoke MUST run against a real Atlassian Cloud sandbox OAuth app, with the resulting work-item sync pulling at least one real Jira issue from that sandbox project into a Lighthouse team. (Recorded as a screenshot or short loom in the PR.)

## Dogfood moment
Same-day: the engineer who lands the slice replaces the dev environment's own PAT-based Jira connection with the new OAuth flow and runs `Update All`. If it doesn't survive a real-team sync, slice 01 is not done.

## Dependencies
- Premium license model live (`LicenseGuardAttribute`).
- `AuthenticationMethodSchema` SSOT — no parallel auth-method registry.
- `ICryptoService` for at-rest encryption of `clientSecret`, `accessToken`, `refreshToken`.
- A registered Atlassian sandbox OAuth app accessible from a developer machine that can reach a public callback URL (use ngrok or the team's dev tunnel).

## Pre-slice SPIKE (if uncertainty is high)
**Yes — ~2 hours**: validate that Atlassian's Jira Cloud 3LO flow, *with the specific scopes Lighthouse needs* (`read:jira-work`, `read:jira-user`, `offline_access`), actually issues a usable bearer token against the Jira REST API endpoints that `JiraWorkTrackingConnector` already calls. If a scope is missing or named differently than the docs imply, slice 01 must absorb the scope-fix; better to find that before committing to an end-to-end day.

## Carpaccio taste tests
- *4+ new components?* This slice introduces 3 new backend components (`IOAuthProvider`, `JiraOAuthProvider`, `OAuthController`) and 2 new entities (`OAuthConfiguration`, `OAuthCredential`). Marginal — but every one of them is *required* by the AC, none is speculative.
- *Every slice depends on a new abstraction?* The new abstraction (`IOAuthProvider`) ships HERE alongside its first concrete user (`JiraOAuthProvider`). Slices 02–04 depend on it being shipped, but they do not introduce *more* abstractions.
- *Disproves a pre-commitment?* Yes — AC #8 disproves the claim that the abstraction is provider-agnostic.
- *Synthetic data only?* No — the dogfood moment forces real-Jira-issue sync.
- *Identical-at-scale to another slice?* No.

**Verdict**: PASS.
