---
title: Setting up Azure DevOps OAuth
layout: home
nav_order: 6
parent: Work Tracking Systems
grand_parent: Concepts
---

This page walks a `connector-admin` through wiring a Lighthouse → Azure DevOps connection using OAuth 2.0 against an Entra ID app registration instead of a Personal Access Token. OAuth is **Premium**-gated and applies to **Azure DevOps Services** (`dev.azure.com`); on-premises Azure DevOps Server continues to use PATs.

- TOC
{:toc}

# Why OAuth for Azure DevOps

Personal Access Tokens work, but they pin the connection to one human's credential. When that person leaves, rotates their token, or has their org permissions narrowed, the Lighthouse → ADO sync breaks silently. OAuth lets you authorise Lighthouse as an Entra ID application: the token is governed centrally in the Azure portal, scoped to exactly what Lighthouse needs, and revocable without touching the connection record inside Lighthouse.

The end-state matches Story #4969: a connector-admin completes the Microsoft consent dance once, and Lighthouse syncs Azure DevOps work items using the issued bearer token — no shared PAT to rotate, no service account to manage.

# Register an Entra ID app

You need an Entra ID app registration in the same tenant that owns the Azure DevOps organisation you want to connect.

1. Sign in to the [Azure portal](https://portal.azure.com) with an account that has permission to create app registrations in the tenant (typically `Application Developer` or higher).
2. Open **Microsoft Entra ID** → **App registrations** → **New registration**.
3. Give the app a name (e.g. *Lighthouse — `<org>`*).
4. Pick a supported account type:

    - **Accounts in this organizational directory only (single tenant)** — the safe default for a self-hosted Lighthouse that serves one Entra tenant. The consent dance stays inside your tenant; users from other tenants cannot sign in.
    - **Accounts in any organizational directory (multi-tenant)** — choose this only if a single Lighthouse instance serves Azure DevOps organisations across multiple Entra tenants. Multi-tenant adds a verified-publisher requirement for the consent UX and broadens the surface area of who can sign in.

5. Leave **Redirect URI** blank for now — you will set it after copying the canonical Lighthouse callback shape in the next section.
6. Click **Register**.
7. On the app's **Overview** page, copy:

    - **Application (client) ID**
    - **Directory (tenant) ID**

    You will paste both into Lighthouse later.

## Set the redirect URI

1. In the app registration, open **Authentication** → **Add a platform** → **Web**.
2. Set the **Redirect URI** to:

    ```
    {your-lighthouse-base-url}/api/oauth/callback
    ```

    Substitute the public URL your operators have configured as `Lighthouse:BaseUrl` — see [Configuring BaseUrl for OAuth callback registration](../../Installation/oauth-baseurl.html). The path `/api/oauth/callback` is fixed; only the host/scheme are yours to set.

3. The redirect URI MUST be `https://` in any production deployment. Entra ID accepts `http://localhost` for local development, but every other host must use HTTPS — Microsoft will reject the consent request otherwise.
4. Save the platform configuration.

## Add API permissions

1. In the app registration, open **API permissions** → **Add a permission** → **APIs my organization uses** → search for **Azure DevOps** → select it.
2. Pick **Delegated permissions** and request the following scopes (exact strings, case-sensitive):

    - `vso.work_write` — read and write work items, queries, boards, and area/iteration paths in the Azure DevOps organisation.
    - `offline_access` — required to receive a `refresh_token` alongside the access token. Without this, Lighthouse can complete the first sync but the access token expires after roughly an hour and the connection drops.

3. Back on the **API permissions** page, click **Grant admin consent for `<tenant>`**. Tenant-wide admin consent is required so the connector-admin (who may not themselves be a tenant admin) does not get blocked at the consent dance with `AADSTS65001`.

## Create a client secret

1. In the app registration, open **Certificates & secrets** → **Client secrets** → **New client secret**.
2. Give it a description (e.g. *Lighthouse — production*) and pick an expiry. Microsoft caps secret lifetimes at 24 months.
3. Click **Add**. Copy the secret's **Value** immediately — the portal only displays it once.
4. Record the expiry date somewhere your operators will see it. Lighthouse does **not** auto-rotate Entra ID client secrets; when the secret expires the connection moves to `RefreshFailed` and the admin must mint a new secret in the portal and paste it back into the connection form.

# Configure the connection in Lighthouse

1. In Lighthouse, open **Settings → Connections** and click **New Azure DevOps connection** (or **Edit** on an existing one if you are migrating off a PAT).
2. Set **Authentication** to **Azure DevOps (OAuth)**. This option is hidden if your instance does not have a Premium licence — see [Licensing](../../licensing/licensing.html).
3. Paste:

    - **Application (client) ID** — from the Entra ID app **Overview** page.
    - **Directory (tenant) ID** — from the same **Overview** page.
    - **Client secret** — the **Value** you copied from **Certificates & secrets**.

4. Verify the **Callback URL** field. It is read-only and is computed server-side from `Lighthouse:BaseUrl`. It MUST match exactly what you registered in the Entra ID app's **Authentication** blade — including scheme (`https://`), host, and the `/api/oauth/callback` path.

    {: .important}
    If the callback URL displays a warning ("Your callback URL may be incorrect…" or an HTTP-vs-HTTPS warning), do not click **Connect** yet. Set `Lighthouse:BaseUrl` on the server to the public `https://` URL first (see [Configuring BaseUrl for OAuth callback registration](../../Installation/oauth-baseurl.html)) and reload the page. Entra ID rejects non-HTTPS redirect URIs for every host except `localhost`.

5. Click **Connect**. A browser popup opens against `login.microsoftonline.com`; sign in if prompted, then **Accept** the requested permissions.
6. On success the popup closes, the connection's status moves to **Connected**, and the next scheduled `Update All` pulls Azure DevOps work items.

{: .note}
A future revision of this page may include screenshots of the Entra ID app registration flow. For now, the step-by-step above mirrors the [Microsoft identity platform documentation for OAuth 2.0 authorization code flow](https://learn.microsoft.com/azure/active-directory/develop/v2-oauth2-auth-code-flow).

# What to expect operationally

The OAuth surface for Azure DevOps reuses the same refresh, reconnect, and health-indicator machinery shipped with Jira in Slice 01–02. Design rationale lives in [ADR-007](../../product/architecture/adr-007-oauth-provider-registry.md) (provider registry), [ADR-008](../../product/architecture/adr-008-oauth-credential-separation.md) (credential separation), [ADR-006](../../product/architecture/adr-006-connection-list-payload-shape.md) (connection-list payload shape — the `requiresReconnect` flag), [ADR-010](../../product/architecture/adr-010-oauth-single-flight-refresh.md) (single-flight refresh), and [ADR-011](../../product/architecture/adr-011-oauth-popup-flow.md) (popup-based consent UX). The connector-admin's mental model is identical to the Jira flow — see [*Setting up Jira OAuth* → What to expect operationally](./oauth-jira.html#what-to-expect-operationally) for the full description of silent refresh, the `RefreshFailed` state, the reconnect banner, and the OAuth health indicator. The only ADO-specific differences are the consent host (`login.microsoftonline.com` instead of `auth.atlassian.com`) and the scope strings (`vso.work_write` + `offline_access` instead of the Jira trio).

# Troubleshooting

| Symptom in the UI | Likely cause | What to check |
|---|---|---|
| `AADSTS50011: Reply URL mismatch` after consent | Redirect URI registered in the Entra ID app does not exactly match `{BaseUrl}/api/oauth/callback`. | Confirm `Lighthouse:BaseUrl`. Entra ID compares scheme + host + port + path **exactly** — a trailing slash or wrong scheme breaks the match. |
| `AADSTS65001: The user or administrator has not consented` | The Azure DevOps API permission is missing **admin consent** on the app registration, or the connector-admin lacks consent rights. | Re-open the app, **API permissions** → **Grant admin consent for `<tenant>`**, retry **Connect**. |
| `AADSTS70011: invalid scope` | The `vso.work_write` or `offline_access` permission is missing or mistyped on the app registration. | Re-open the app, **API permissions**, add the missing delegated permission, grant admin consent, retry. |
| Popup blocked when clicking **Connect** | The browser blocked the popup against `login.microsoftonline.com`. | Allow popups for the Lighthouse origin and retry. See [ADR-011](../../product/architecture/adr-011-oauth-popup-flow.md) for the popup-flow rationale. |
| Lighthouse banner: *Invalid state token* | The state cookie expired, was blocked by a third-party cookie policy, or the admin took longer than the consent window. | Retry the **Connect** flow in a single browser session. Ensure third-party cookies are allowed for the `login.microsoftonline.com` and Lighthouse origins for the duration of the dance. |
| Auth-type dropdown does not show **Azure DevOps (OAuth)** | The instance has no Premium licence, or the licence has lapsed. | Confirm the licence status in **Settings → Licensing**. |
| Yellow *"Reconnect required — the OAuth refresh token is no longer valid"* banner on a previously-syncing connection | Silent refresh attempted and failed (client secret expired, refresh grant revoked, scope tightened, IdP-side rotation, persistent network timeout). The credential is now in `RefreshFailed`. | If the client secret expired, mint a new one in **Certificates & secrets** and paste the new **Value** into the connection. Otherwise click **Reconnect** on the banner and complete the Microsoft consent flow again. |
| Reconnect banner reappears immediately after a successful reconnect | The Entra ID app no longer grants `offline_access`, so no refresh token is being issued — Microsoft only returned a short-lived access token. | Re-open the app in the Azure portal, confirm `offline_access` is present under **API permissions** with admin consent, save, and retry the reconnect. |

For deeper issues, capture the failing callback URL from the browser address bar (it contains the OAuth `error=` / `error_description=` parameters along with an AADSTS code) and include it when reporting the problem.

# See also

- [Setting up Jira OAuth](./oauth-jira.html) — the sibling provider, same UX shape
- [Configuring BaseUrl for OAuth callback registration](../../Installation/oauth-baseurl.html)
- [Azure DevOps](./azuredevops.html) — the original ADO connection page (URL format, query, additional fields)
- [ADR-006 — Connection-list payload shape](../../product/architecture/adr-006-connection-list-payload-shape.md) (the additive `requiresReconnect` field)
- [ADR-007 — OAuth provider registry](../../product/architecture/adr-007-oauth-provider-registry.md)
- [ADR-008 — OAuth credential separation](../../product/architecture/adr-008-oauth-credential-separation.md)
- [ADR-009 — OAuth BaseUrl callback](../../product/architecture/adr-009-oauth-baseurl-callback.md)
- [ADR-010 — OAuth single-flight refresh](../../product/architecture/adr-010-oauth-single-flight-refresh.md)
- [ADR-011 — OAuth popup-based consent flow](../../product/architecture/adr-011-oauth-popup-flow.md)
