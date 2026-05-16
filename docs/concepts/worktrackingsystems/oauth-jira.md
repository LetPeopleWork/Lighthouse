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

The OAuth surface is designed to stay out of an admin's way until something actually needs human attention. Design rationale lives in [ADR-007](../../product/architecture/adr-007-oauth-provider-registry.md) (provider registry), [ADR-008](../../product/architecture/adr-008-oauth-credential-separation.md) (credential separation), [ADR-006](../../product/architecture/adr-006-connection-list-payload-shape.md) (connection-list payload shape — the `requiresReconnect` flag), and [ADR-010](../../product/architecture/adr-010-oauth-single-flight-refresh.md) (single-flight refresh).

## Silent token refresh

Atlassian's access tokens expire on the order of an hour. At outbound-request time, when `ExpiresAt - now` falls below 5 minutes, Lighthouse calls Atlassian's `/oauth/token` endpoint with `grant_type=refresh_token`, persists the new access + refresh token pair, and the outbound sync call proceeds against the freshly-refreshed credential. The admin sees nothing: no banner, no status flicker, no log noise beyond the standard sync log. Concurrent outbound requests against the same connection coalesce into a single refresh call (see [ADR-010](../../product/architecture/adr-010-oauth-single-flight-refresh.md) for the single-flight design).

## When refresh fails: `RefreshFailed` and the reconnect banner

A refresh attempt can fail for reasons Lighthouse cannot recover from on its own: the refresh grant was revoked at Atlassian, the OAuth app's scopes were tightened (e.g. `offline_access` removed), the IdP rotated the refresh token without Lighthouse observing the new value, or the call timed out at the network layer. When that happens:

- The `OAuthCredential.Status` transitions to `RefreshFailed`.
- The connection-list payload for that connection gains `requiresReconnect: true` (additive field — see [ADR-006](../../product/architecture/adr-006-connection-list-payload-shape.md)).
- A yellow Alert banner appears on the connection's card on the Connections grid and on the connection-settings page, with the exact copy *"Reconnect required — the OAuth refresh token is no longer valid"* and a **Reconnect** button beside it.
- Background syncs against that connection stop until the credential is restored.

The admin's job is to click **Reconnect** and complete the Atlassian consent dance again. On success the credential's `Status` returns to `Valid`, the banner disappears, `requiresReconnect` drops to `false`, and the next scheduled sync resumes normally.

## OAuth health indicator

When at least one OAuth connection exists, system admins see a small **cloud icon** in the application header that reports the health of OAuth connections at a glance.

| State | Icon | Tooltip |
|---|---|---|
| All OAuth connections healthy | Cloud (success colour) | *"All OAuth connections healthy"* |
| One or more connections need reconnect | Cloud-off (warning colour, with badge count) | *"N OAuth connection(s) need reconnect"* |

Clicking the icon navigates to **System Settings**, where the affected connection's edit dialog will show the orange reconnect banner. The icon is hidden when no OAuth connections exist on the instance.

# Troubleshooting

| Symptom in the UI | Likely cause | What to check |
|---|---|---|
| HTTP 400 from Atlassian after consent (*invalid_redirect_uri*) | Callback URL registered with Atlassian doesn't exactly match `{BaseUrl}/api/oauth/callback`. | Confirm `Lighthouse:BaseUrl`. Atlassian compares scheme + host + port + path **exactly**. |
| HTTP 400 from Atlassian (*invalid_scope*) | One of `read:jira-work`, `read:jira-user`, `offline_access` is missing from the app permissions or is mistyped. | Re-open the app in the Atlassian developer console, add the missing scope, retry. |
| Lighthouse banner: *Invalid state token* | The state cookie expired, was blocked by a third-party cookie policy, or the admin took longer than the consent window. | Retry the **Connect** flow in a single browser session. Ensure third-party cookies are allowed for the Atlassian and Lighthouse origins for the duration of the dance. |
| Auth-type dropdown does not show **Jira Cloud (OAuth)** | The instance has no Premium licence, or the licence has lapsed. | Confirm the licence status in **Settings → Licensing**. |
| Yellow *"Reconnect required — the OAuth refresh token is no longer valid"* banner appears on a connection that was previously syncing fine | Silent refresh attempted and failed (revoked grant, scope change, IdP-side rotation, persistent network timeout). The credential is now in `RefreshFailed`. | Click **Reconnect** on the banner and complete the Atlassian consent flow again. The credential returns to `Valid` and the banner clears. |
| Reconnect banner reappears immediately after a successful reconnect | The Atlassian OAuth app no longer grants `offline_access`, so no refresh token is being issued — Atlassian only returned a short-lived access token. | Re-open the app in the Atlassian developer console, confirm `offline_access` is present under **Permissions → Jira API**, save, and retry the reconnect. |
| Connection reads as **Connected** but no boards appear in the board wizard | Boards endpoint does not support OAuth 2.0 (3LO) — Atlassian limitation, mirrored from scoped-token behaviour. | Configure teams/portfolios manually. See the [Jira](./jira.html#scopes) page for the same caveat with scoped API tokens. |

For deeper issues, capture the failing callback URL from the browser address bar (it contains the OAuth `error=` / `error_description=` parameters) and include it when reporting the problem.

# See also

- [Setting up Azure DevOps OAuth](./oauth-ado.html) — the sibling provider, same UX shape
- [Configuring BaseUrl for OAuth callback registration](../../Installation/oauth-baseurl.html)
- [Jira](./jira.html) — the original Jira connection page (covers JQL, projects, board wizard)
- [ADR-006 — Connection-list payload shape](../../product/architecture/adr-006-connection-list-payload-shape.md) (the additive `requiresReconnect` field)
- [ADR-007 — OAuth provider registry](../../product/architecture/adr-007-oauth-provider-registry.md)
- [ADR-008 — OAuth credential separation](../../product/architecture/adr-008-oauth-credential-separation.md)
- [ADR-009 — OAuth BaseUrl callback](../../product/architecture/adr-009-oauth-baseurl-callback.md)
- [ADR-010 — OAuth single-flight refresh](../../product/architecture/adr-010-oauth-single-flight-refresh.md)
