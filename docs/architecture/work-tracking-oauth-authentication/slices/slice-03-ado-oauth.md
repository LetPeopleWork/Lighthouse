# Slice 03 — Azure DevOps OAuth

**Feature**: work-tracking-oauth-authentication
**ADO stories rolled in**: #4969
**Effort estimate**: 0.5 day of crafter dispatch
**Reference class**: second concretion of an abstraction already proven by Slice 01

## Goal
A `connector-admin` configures an Azure DevOps connection authenticated via Entra ID / Azure DevOps OAuth, end-to-end. The slice is honest *only if* the diff is dominated by the new provider class and the docs — not by changes to the controller, the credential entity, or persistence.

## IN scope
- `AdoOAuthProvider` concrete implementation of `IOAuthProvider`.
- New auth-method key `ado.oauth` registered in `AuthenticationMethodSchema` (premium-gated).
- DI registration for the new provider.
- `AzureDevOpsWorkTrackingConnector` extended to use `Bearer {accessToken}` when the connection's auth method is `ado.oauth`.
- Docs page: *Setting up Azure DevOps OAuth* (Entra ID app registration, required API permissions, redirect URI, HTTPS warning).
- Frontend: ADO connector auth-type dropdown gains the new option (reuses Slice 01's `OAuthAuthForm` and `AuthMethodDropdown` components unchanged).
- HTTPS-warning rendering for the ADO form when `BaseUrl` is HTTP.
- E2E: same happy-path scenario shape as Slice 01, for ADO.

## OUT scope
- Refresh — already done in Slice 02, applies "for free".
- Standalone-mode guard — Slice 04.
- Multi-tenant / Personal Microsoft / GitHub login support — only Entra ID + Azure DevOps OAuth.
- Auto-discovery of the user's ADO organisations.

## Learning hypothesis
**Disproves if it fails**: "the `IOAuthProvider` abstraction from Slice 01 is genuinely provider-agnostic" — i.e., if shipping ADO requires touching `OAuthCredential`, `OAuthController`, the refresh service, or the migration history, the abstraction was a lie and Slice 01's `feature-delta.md` AC #8 was a false negative.

**Confirms if it succeeds**: "future providers (Linear, GitHub, etc.) are roughly the same shape and size of work as this slice."

## Acceptance criteria
See US-03 AC #1–5 in `feature-delta.md`. **AC #5 is the load-bearing invariant for this slice** — diff review at PR time must confirm no change to `OAuthCredential`, `OAuthConfiguration`, `OAuthController`, or `OAuthTokenRefreshService`.

## Production-data requirement
Manual smoke against a real Entra ID app + real `dev.azure.com` organisation, syncing at least one real ADO work item.

## Dogfood moment
Same-day: convert the dev environment's PAT-based ADO connection (the one that mirrors *this very project*) to OAuth. If the next `Update All` works, the slice is done.

## Dependencies
- Slices 01 and 02 landed.
- A registered Entra ID app with `vso.work` / `vso.work_write` scopes accessible from a developer machine.

## Pre-slice SPIKE
**Yes — ~1 hour**: Azure DevOps OAuth has documented quirks (audience claim, multi-tenant vs. single-tenant choice, `resource` parameter on token endpoint). Validate against a real Entra ID app that the chosen tenant configuration actually returns a usable token before committing to the slice.

## Carpaccio taste tests
- *4+ new components?* One: `AdoOAuthProvider`. PASS.
- *Every slice depends on a new abstraction?* No new abstractions in this slice.
- *Disproves a pre-commitment?* Yes — disproves the provider-agnosticism claim if the diff bleeds outside the new provider class.
- *Synthetic data only?* No — real ADO sync.
- *Identical-at-scale to another slice?* Resembles Slice 01 structurally; passes because the *concrete shape* differs (different provider, different scopes, different consent URL, HTTPS warning vs. no warning).

**Verdict**: PASS.
