---
title: Setting up Jira OAuth
layout: home
nav_order: 5
parent: Work Tracking Systems
grand_parent: Concepts
---

This page walks a `connector-admin` through wiring a Lighthouse → Jira Cloud connection using OAuth 2.0 (3LO) instead of a static API token. OAuth is **Premium**-gated and applies to **Jira Cloud only** (Jira Data Center continues to use Personal Access Tokens).

- TOC
{:toc}

# Why OAuth for Jira

Personal API tokens and PATs work, but they pin the connection to a single human's credential. When that person leaves, rotates their token, or has their permissions changed, the Lighthouse → Jira sync breaks silently. OAuth lets you authorise Lighthouse as an application in your Atlassian tenant: the token is governed centrally in the Atlassian developer console, scoped to exactly what Lighthouse needs, and revocable without touching the connection record inside Lighthouse.

The end-state matches Story #4967 (epic #2438): a connector-admin completes the Atlassian consent dance once, and Lighthouse syncs Jira issues using the issued bearer token — no shared token to rotate, no service account to manage.

# Register an OAuth 2.0 (3LO) app in the Atlassian developer console

1. Sign in to [`developer.atlassian.com`](https://developer.atlassian.com) with an Atlassian account that has admin access to the Jira Cloud tenant you want to connect.
2. Open **My apps** → **Create** → **OAuth 2.0 integration**. Give the app a name (e.g. *Lighthouse — `<tenant>`*).
3. In the app's **Authorization** section, set the **Callback URL** to:

    ```
    {your-lighthouse-base-url}/api/oauth/callback
    ```

    Substitute the public URL your operators have configured as `Lighthouse:BaseUrl` — see [Configuring BaseUrl for OAuth callback registration](../../Installation/oauth-baseurl.html). The path `/api/oauth/callback` is fixed; only the host/scheme are yours to set.

4. In **Permissions** → **Jira API**, request the following scopes (exact strings, case-sensitive):

    - `read:jira-work`
    - `read:jira-user`
    - `offline_access`

    The `offline_access` scope is what lets Atlassian return a `refresh_token` alongside the access token. Without it, Lighthouse can complete the first sync but cannot keep the connection alive after the access token expires.

5. Copy the **Client ID** and **Client Secret** from the **Settings** page of your new app. Treat the Client Secret like a password — paste it into Lighthouse over an encrypted channel and never commit it to source control.

{: .note}
A future revision of this page may include screenshots of the developer-console flow. For now, the step-by-step above mirrors the [Atlassian OAuth 2.0 (3LO) apps documentation](https://developer.atlassian.com/cloud/jira/platform/oauth-2-3lo-apps/).

# Configure the connection in Lighthouse

1. In Lighthouse, open **Settings → Connections** and click **New Jira connection** (or **Edit** on an existing one if you are migrating off a PAT).
2. Set **Authentication** to **Jira Cloud (OAuth)**. This option is hidden if your instance does not have a Premium licence — see [Licensing](../../licensing/licensing.html).
3. Paste the **Client ID** and **Client Secret** from the Atlassian developer console.
4. Verify the **Callback URL** field. It is read-only and is computed server-side from `Lighthouse:BaseUrl`. It MUST match exactly what you registered with Atlassian — including scheme (`https://` vs `http://`), host, and the `/api/oauth/callback` path.

    {: .important}
    If the callback URL displays a warning ("Your callback URL may be incorrect…"), do not click **Connect** yet. Set `Lighthouse:BaseUrl` on the server first (see [Configuring BaseUrl for OAuth callback registration](../../Installation/oauth-baseurl.html)) and reload the page.

5. Click **Connect**. Your browser is redirected to Atlassian; sign in if prompted, then **Accept** the requested scopes.
6. Atlassian redirects you back to Lighthouse. On success the connection's status moves to **Connected** and the next scheduled `Update All` pulls Jira issues.

# What to expect operationally

The Slice 01 surface is intentionally minimal — see [ADR-007](../../product/architecture/adr-007-oauth-provider-registry.md) and [ADR-008](../../product/architecture/adr-008-oauth-credential-separation.md) for the design rationale.

Today (Slice 01):
- The connection list page reflects the OAuth connection's current status (`Connected`, `Error`, etc.).
- The first access token is used for sync until it expires.
- After expiry, the connection moves to an error state and an admin must reconnect manually.

Coming in Slice 02:
- Silent token refresh using the `refresh_token` returned via `offline_access`.
- A reconnect-banner UX that prompts the admin only when the refresh itself fails (revoked grant, scope change, IdP-side rotation).

If your Slice-02 refresh flow lands before this page is updated, treat the [release notes](../../releasenotes) as the source of truth.

# Troubleshooting

| Symptom in the UI | Likely cause | What to check |
|---|---|---|
| HTTP 400 from Atlassian after consent (*invalid_redirect_uri*) | Callback URL registered with Atlassian doesn't exactly match `{BaseUrl}/api/oauth/callback`. | Confirm `Lighthouse:BaseUrl`. Atlassian compares scheme + host + port + path **exactly**. |
| HTTP 400 from Atlassian (*invalid_scope*) | One of `read:jira-work`, `read:jira-user`, `offline_access` is missing from the app permissions or is mistyped. | Re-open the app in the Atlassian developer console, add the missing scope, retry. |
| Lighthouse banner: *Invalid state token* | The state cookie expired, was blocked by a third-party cookie policy, or the admin took longer than the consent window. | Retry the **Connect** flow in a single browser session. Ensure third-party cookies are allowed for the Atlassian and Lighthouse origins for the duration of the dance. |
| Auth-type dropdown does not show **Jira Cloud (OAuth)** | The instance has no Premium licence, or the licence has lapsed. | Confirm the licence status in **Settings → Licensing**. |
| Sync succeeds at first then fails with `401 Unauthorized` after ~1 hour | Access token has expired and refresh has not yet shipped (Slice 02). | Click **Reconnect** on the connection. Slice 02 will make this automatic. |
| Connection reads as **Connected** but no boards appear in the board wizard | Boards endpoint does not support OAuth 2.0 (3LO) — Atlassian limitation, mirrored from scoped-token behaviour. | Configure teams/portfolios manually. See the [Jira](./jira.html#scopes) page for the same caveat with scoped API tokens. |

For deeper issues, capture the failing callback URL from the browser address bar (it contains the OAuth `error=` / `error_description=` parameters) and include it when reporting the problem.

# See also

- [Configuring BaseUrl for OAuth callback registration](../../Installation/oauth-baseurl.html)
- [Jira](./jira.html) — the original Jira connection page (covers JQL, projects, board wizard)
- [ADR-007 — OAuth provider registry](../../product/architecture/adr-007-oauth-provider-registry.md)
- [ADR-008 — OAuth credential separation](../../product/architecture/adr-008-oauth-credential-separation.md)
- [ADR-009 — OAuth BaseUrl callback](../../product/architecture/adr-009-oauth-baseurl-callback.md)
