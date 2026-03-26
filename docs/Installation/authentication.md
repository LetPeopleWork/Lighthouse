---
title: Authentication
layout: home
parent: Configuration
nav_order: 1
---

Lighthouse supports **OpenID Connect (OIDC)** authentication to protect your instance. When enabled, users must sign in through an identity provider before accessing Lighthouse.

{: .note}
Authentication is a **Premium** feature. A valid Premium license is required to enable it.

- TOC
{:toc}

---

## Overview

By default, Lighthouse is accessible to anyone who can reach the URL. Enabling authentication adds an authorization layer in front of the application — every request is checked for a valid session, and unauthenticated users are redirected to the sign-in screen.

### Authentication Flow

When authentication is enabled, users interact with the following screens depending on their state:

#### Sign-In Screen

Users who are not yet signed in see the Lighthouse sign-in screen. Clicking **Sign In** redirects them to the configured identity provider.

![Sign-In Screen](../../assets/authentication/signin.png)

#### Authenticated — App Loaded

After a successful sign-in, users with a valid **Premium** license are taken directly into the Lighthouse application.

#### Blocked — Premium License Required

If authentication is working but no Premium license is configured, users see the **Premium License Required** screen. They can upload a license directly from this screen, or sign out.

![Premium License Required](../../assets/authentication/blocked.png)

#### Authentication Misconfigured

If authentication is enabled but the configuration is invalid (for example, no `Authority` is set), Lighthouse displays an error screen explaining what is wrong. This is a deliberate fail-closed design — Lighthouse will not start with a broken authentication configuration.

![Authentication Misconfigured](../../assets/authentication/misconfigured.png)

#### Session Expired

If a user's session expires while they are using Lighthouse, they are shown the **Session Expired** screen. Clicking **Sign In Again** returns them to the identity provider to re-authenticate.

![Session Expired](../../assets/authentication/session_expired.png)

---

## Configuration Reference

All authentication settings live under the `Authentication` key in `appsettings.json`.

| Property | Type | Default | Description |
|---|---|---|---|
| `Enabled` | `bool` | `false` | Enables or disables authentication. |
| `Authority` | `string` | — | The OIDC issuer URL of your identity provider. **Required** when `Enabled` is `true`. |
| `ClientId` | `string` | — | The client ID registered with your identity provider. |
| `ClientSecret` | `string` | — | The client secret for the registered application. |
| `CallbackPath` | `string` | `/api/auth/callback` | The path the identity provider redirects back to after sign-in. |
| `SignedOutCallbackPath` | `string` | `/api/auth/signout-callback` | The path the identity provider redirects back to after sign-out. |
| `Scopes` | `string[]` | `["openid","profile","email"]` | OIDC scopes to request. |
| `AllowedOrigins` | `string[]` | `[]` | CORS origins allowed for authenticated API access. |
| `SessionLifetimeMinutes` | `int` | `480` | How long a session is valid before the user must sign in again (8 hours by default). |
| `RequireHttpsMetadata` | `bool` | `true` | Whether the OIDC metadata endpoint must be served over HTTPS. Set to `false` only for local development setups that do not use TLS. |
| `MetadataAddress` | `string` | — | Optional override for the OIDC discovery document URL, if different from `{Authority}/.well-known/openid-configuration`. |
| `TrustedProxies` | `string[]` | `[]` | IP addresses of trusted reverse proxies for forwarded-headers support. |
| `TrustedNetworks` | `string[]` | `[]` | CIDR network ranges of trusted networks for forwarded-headers support. |

---

## Provider Guides

### Keycloak

[Keycloak](https://www.keycloak.org/) is an open-source identity provider you can self-host. It is a good choice for local or on-premise deployments.

#### 1. Start Keycloak

The quickest way to run Keycloak locally is with Docker:

```bash
docker run -p 38080:8080 \
  -e KC_BOOTSTRAP_ADMIN_USERNAME=admin \
  -e KC_BOOTSTRAP_ADMIN_PASSWORD=admin \
  quay.io/keycloak/keycloak:latest start-dev
```

Open `http://localhost:38080` and sign in as `admin` / `admin`.

{: .note}
The example stack in `examples/keycloak/` in the Lighthouse repository provides a ready-made `docker-compose.yml` including a pre-configured realm export.

#### 2. Create a Realm

1. In the top-left dropdown, click **Create realm**.
2. Enter a **Realm name** (e.g. `Lighthouse`) and click **Create**.

#### 3. Create a Client

1. Go to **Clients → Create client**.
2. Set **Client type** to `OpenID Connect`.
3. Set **Client ID** to `lighthouse-app` (or any ID you prefer).
4. Enable **Client authentication** (this makes it a confidential client).
5. Under **Valid redirect URIs**, add the callback URL for your Lighthouse instance, e.g. `http://localhost:5169/*`.
6. Save the client.
7. Open the **Credentials** tab and copy the **Client secret**.

#### 4. Create a User

1. Go to **Users → Add user**.
2. Fill in the username and email, then click **Create**.
3. On the **Credentials** tab, set a password and disable the **Temporary** toggle.

#### 5. Configure Lighthouse

```json
{
  "Authentication": {
    "Enabled": true,
    "Authority": "http://localhost:38080/realms/Lighthouse",
    "ClientId": "lighthouse-app",
    "ClientSecret": "your-client-secret",
    "Scopes": ["openid", "profile", "email"],
    "AllowedOrigins": ["http://localhost:5169"],
    "RequireHttpsMetadata": false
  }
}
```

{: .important}
`RequireHttpsMetadata` must be `false` when Keycloak is running without TLS (for example, in `start-dev` mode). Do **not** disable this in production.

Docker environment variables equivalent:

```bash
docker run \
  -e Authentication__Enabled=true \
  -e Authentication__Authority=http://keycloak:38080/realms/Lighthouse \
  -e Authentication__ClientId=lighthouse-app \
  -e Authentication__ClientSecret=your-client-secret \
  -e Authentication__RequireHttpsMetadata=false \
  ghcr.io/letpeoplework/lighthouse:latest
```

---

### Microsoft Entra ID

[Microsoft Entra ID](https://www.microsoft.com/en-us/security/business/identity-access/microsoft-entra-id) (formerly Azure Active Directory) is Microsoft's cloud identity platform, available to all Microsoft 365 and Azure customers.

#### 1. Register an Application

1. In the [Azure Portal](https://portal.azure.com), go to **Microsoft Entra ID → App registrations → New registration**.
2. Give the application a name (e.g. `Lighthouse`).
3. Under **Supported account types**, choose the appropriate option for your organisation.
4. Under **Redirect URI**, select **Web** and enter: `https://lighthouse.example.com/api/auth/callback`
5. Click **Register** and note the **Application (client) ID** and **Directory (tenant) ID**.

#### 2. Create a Client Secret

1. Go to **Certificates & secrets → New client secret**.
2. Set a description and expiry, then click **Add**.
3. Copy the generated secret value immediately — it will not be shown again.

#### 3. Configure Lighthouse

```json
{
  "Authentication": {
    "Enabled": true,
    "Authority": "https://login.microsoftonline.com/{tenant-id}/v2.0",
    "ClientId": "{application-client-id}",
    "ClientSecret": "{client-secret-value}",
    "Scopes": ["openid", "profile", "email"],
    "AllowedOrigins": ["https://lighthouse.example.com"],
    "SessionLifetimeMinutes": 480
  }
}
```

Replace `{tenant-id}` with your Directory (tenant) ID, `{application-client-id}` with the Application (client) ID, and `{client-secret-value}` with the secret you copied.

---

### Google

[Google Identity](https://developers.google.com/identity) can be used as an OIDC provider, allowing users to sign in with their Google accounts.

#### 1. Create OAuth 2.0 Credentials

1. Open the [Google Cloud Console](https://console.cloud.google.com).
2. Select or create a project.
3. Go to **APIs & Services → OAuth consent screen** and configure the consent screen (App name, support email, and any required scopes).
4. Go to **APIs & Services → Credentials → Create credentials → OAuth client ID**.
5. Select **Web application** as the application type.
6. Under **Authorised redirect URIs**, add: `https://lighthouse.example.com/api/auth/callback`
7. Click **Create** and copy the **Client ID** and **Client Secret**.

#### 2. Configure Lighthouse

```json
{
  "Authentication": {
    "Enabled": true,
    "Authority": "https://accounts.google.com",
    "ClientId": "{your-client-id}.apps.googleusercontent.com",
    "ClientSecret": "{your-client-secret}",
    "Scopes": ["openid", "profile", "email"],
    "AllowedOrigins": ["https://lighthouse.example.com"]
  }
}
```

{: .note}
Google does not support RP-Initiated Logout (the user is signed out of Lighthouse, but their Google session remains active in the browser). Users who "Sign Out" of Lighthouse will need to sign out of Google separately if they want to prevent automatic re-login.

---

### Auth0

[Auth0](https://auth0.com/) is a cloud-based identity platform that supports a wide range of social and enterprise identity providers.

#### 1. Create an Application

1. In the [Auth0 Dashboard](https://manage.auth0.com), go to **Applications → Create Application**.
2. Choose **Regular Web Applications** and click **Create**.
3. Under **Settings**, copy the **Domain**, **Client ID**, and **Client Secret**.
4. Under **Allowed Callback URLs**, add: `https://lighthouse.example.com/api/auth/callback`
5. Under **Allowed Logout URLs**, add: `https://lighthouse.example.com`
6. Save changes.

#### 2. Configure Lighthouse

```json
{
  "Authentication": {
    "Enabled": true,
    "Authority": "https://{your-auth0-domain}",
    "ClientId": "{your-client-id}",
    "ClientSecret": "{your-client-secret}",
    "Scopes": ["openid", "profile", "email"],
    "AllowedOrigins": ["https://lighthouse.example.com"]
  }
}
```

Replace `{your-auth0-domain}` with your Auth0 tenant domain (e.g. `my-tenant.eu.auth0.com`).

{: .note}
Auth0 supports RP-Initiated Logout, so signing out of Lighthouse will also end the Auth0 session.

---

## Troubleshooting

### "Authentication Misconfigured" screen is shown

The `Authority` value is missing or the OIDC discovery document cannot be reached. Check that:

- `Authentication__Authority` is set and points to the correct issuer URL.
- The identity provider is reachable from the Lighthouse server.
- If using HTTP (no TLS), `Authentication__RequireHttpsMetadata` is set to `false`.

### Redirect URI mismatch error from the identity provider

The callback URL registered with your identity provider does not match what Lighthouse is sending. Ensure the **redirect URI** in your identity provider is set to:

```
{LighthouseBaseUrl}/api/auth/callback
```

For example: `https://lighthouse.example.com/api/auth/callback`

### Users are immediately shown "Session Expired"

The session lifetime may be too short relative to the identity provider token lifetime, or the Lighthouse server clock is out of sync. Check:

- `Authentication__SessionLifetimeMinutes` (default: 480 minutes).
- Server time synchronisation (NTP).

### "Premium License Required" screen after sign-in

Authentication is working correctly, but no Premium license has been uploaded. Use the **Upload License** button on the blocked screen, or navigate to the Lighthouse settings to upload a license.

### Sign-out does not redirect to the identity provider

Some identity providers (e.g. Google) do not support RP-Initiated Logout. After signing out of Lighthouse, users may be automatically signed back in when they next click **Sign In** because their identity provider session is still active. To fully sign out, users must also sign out of the identity provider directly.
