---
title: Configuring BaseUrl for OAuth callback registration
layout: home
nav_order: 4
parent: Installation and Configuration
---

This page covers `Lighthouse:BaseUrl` — a single server-side setting that controls the OAuth callback URL Lighthouse displays in the connection form and uses at runtime. If you are setting up OAuth for [Jira](../concepts/worktrackingsystems/oauth-jira.html) or [Azure DevOps](../concepts/worktrackingsystems/oauth-ado.html), configure `Lighthouse:BaseUrl` before you register your OAuth app with the identity provider.

- TOC
{:toc}

# Why BaseUrl matters for OAuth

When Lighthouse runs behind a reverse proxy — the typical Server-edition deployment — the host that ASP.NET Core observes internally (`http://lighthouse:8080` inside the container) is **not** the public host the user's browser sees (`https://lighthouse.example.com`). If Lighthouse derived the OAuth callback URL from `Request.Host`, the form would display a URL that the identity provider's redirect cannot reach, and the admin would register a callback that doesn't match what Lighthouse actually sends as `redirect_uri` at runtime.

`Lighthouse:BaseUrl` is the explicit, server-configured public origin Lighthouse uses to compute its callback URL. The same value is used in three places:

1. The OAuth form displays `{BaseUrl}/api/oauth/callback` as the **read-only Callback URL** the admin must paste into the IdP's app registration.
2. The OAuth controller sends `{BaseUrl}/api/oauth/callback` as the `redirect_uri` parameter when it constructs the authorisation URL.
3. The callback handler computes the 302 target back to the Lighthouse connection settings page.

When all three agree, the consent dance works. When they disagree, the IdP rejects the callback and the user lands on a generic IdP error page.

The decision to use a single server-config setting (rather than deriving from `Request.Host` or letting the admin override per connection) is captured in [ADR-009](../product/architecture/adr-009-oauth-baseurl-callback.md).

# How to configure `Lighthouse:BaseUrl`

`Lighthouse:BaseUrl` is a **scalar string** — a single absolute URL with scheme and host. Set it via either `appsettings.json` or an environment variable.

**Default value**: unset. When unset, Lighthouse falls back to `{Request.Scheme}://{Request.Host}/api/oauth/callback` for display and shows a non-blocking warning on the OAuth form: *"Your callback URL may be incorrect. Set `Lighthouse:BaseUrl` in your server configuration to guarantee OAuth registration works."*

## Option 1 — `appsettings.json`

Add a `Lighthouse` section (top-level) with the `BaseUrl` property:

```json
{
  "Lighthouse": {
    "BaseUrl": "https://lighthouse.example.com"
  }
}
```

The value MUST be an absolute URL. Omit any trailing slash — Lighthouse appends `/api/oauth/callback` directly.

## Option 2 — Environment variable

For Docker deployments and CI, prefer environment variables. Use double-underscore (`__`) as the section delimiter, per the standard ASP.NET Core convention:

```bash
Lighthouse__BaseUrl=https://lighthouse.example.com
```

In `docker run` form:

```bash
docker run -e "Lighthouse__BaseUrl=https://lighthouse.example.com" ghcr.io/letpeoplework/lighthouse:latest
```

In `docker-compose.yml`:

```yaml
services:
  lighthouse:
    image: ghcr.io/letpeoplework/lighthouse:latest
    environment:
      Lighthouse__BaseUrl: https://lighthouse.example.com
```

{: .note}
`Lighthouse:BaseUrl` is a single string, not a list, so it does NOT have the indexed-key footgun that affects list-typed settings like `Authentication__AllowedOrigins`. A scalar `Lighthouse__BaseUrl=https://...` binds correctly to the configuration property. For background on the list-binding trap, see the *2026-05-13* entry in [`ci-learnings.md`](https://github.com/LetPeopleWork/Lighthouse/blob/main/docs/ci-learnings.md).

## Option 3 — Command line

Override at startup:

```bash
Lighthouse.exe --Lighthouse:BaseUrl="https://lighthouse.example.com"
```

# Worked examples for reverse-proxy setups

The three examples below show the minimum proxy configuration needed for Lighthouse's OAuth callback to work. In each case, the value you set in `Lighthouse:BaseUrl` is the public URL — exactly what the user's browser sees, exactly what you register with the IdP.

Assume:
- Public URL: `https://lighthouse.example.com`
- Lighthouse internal host: `lighthouse` (Docker service name) on port `8080`

## nginx

Minimal `server` block:

```nginx
server {
    listen 443 ssl http2;
    server_name lighthouse.example.com;

    ssl_certificate     /etc/nginx/certs/lighthouse.example.com.crt;
    ssl_certificate_key /etc/nginx/certs/lighthouse.example.com.key;

    location / {
        proxy_pass         http://lighthouse:8080;
        proxy_set_header   Host              $host;
        proxy_set_header   X-Real-IP         $remote_addr;
        proxy_set_header   X-Forwarded-For   $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

Then on the Lighthouse side:

```bash
Lighthouse__BaseUrl=https://lighthouse.example.com
```

## Caddy

Caddyfile entry:

```caddyfile
lighthouse.example.com {
    reverse_proxy lighthouse:8080
}
```

Caddy handles TLS via Let's Encrypt automatically and sets the forwarded headers without extra configuration. Lighthouse-side:

```bash
Lighthouse__BaseUrl=https://lighthouse.example.com
```

## Traefik

Labels-based config on the Lighthouse service in `docker-compose.yml`:

```yaml
services:
  lighthouse:
    image: ghcr.io/letpeoplework/lighthouse:latest
    environment:
      Lighthouse__BaseUrl: https://lighthouse.example.com
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.lighthouse.rule=Host(`lighthouse.example.com`)"
      - "traefik.http.routers.lighthouse.entrypoints=websecure"
      - "traefik.http.routers.lighthouse.tls.certresolver=letsencrypt"
      - "traefik.http.services.lighthouse.loadbalancer.server.port=8080"
    networks:
      - traefik

networks:
  traefik:
    external: true
```

Traefik forwards the `Host` header by default. The `Lighthouse:BaseUrl` MUST still be set explicitly — Lighthouse does not introspect Traefik's labels.

# Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| OAuth form shows a yellow warning about the callback URL | `Lighthouse:BaseUrl` is unset. | Set it via `appsettings.json` or env var, restart Lighthouse, reload the form. |
| Callback URL displays with the wrong scheme (e.g. `http://` behind an HTTPS proxy) | Lighthouse fell back to `Request.Scheme` because `Lighthouse:BaseUrl` is unset. | Set `Lighthouse:BaseUrl` to the public `https://` URL explicitly. |
| OAuth flow fails with *redirect_uri mismatch* even though `Lighthouse:BaseUrl` looks correct | The value registered in the IdP's app config drifted (trailing slash, port number, scheme). | Re-copy the callback URL from the OAuth form into the IdP — it MUST match exactly, character-for-character. |
| `Lighthouse:BaseUrl` ignored after setting it via env var | Container was not restarted, or the env var was set in the host shell but not passed into the container (`-e Lighthouse__BaseUrl=...`). | Restart the container and confirm the var is in the container's environment (`docker exec lighthouse env \| grep -i baseurl`). |

# See also

- [Setting up Jira OAuth](../concepts/worktrackingsystems/oauth-jira.html)
- [Setting up Azure DevOps OAuth](../concepts/worktrackingsystems/oauth-ado.html)
- [ADR-009 — OAuth callback URL derived from a server-configured BaseUrl](../product/architecture/adr-009-oauth-baseurl-callback.md)
- [Configuration](./configuration.html) — full list of server-side settings
- [`ci-learnings.md` — environment-variable binding gotchas](https://github.com/LetPeopleWork/Lighthouse/blob/main/docs/ci-learnings.md)
