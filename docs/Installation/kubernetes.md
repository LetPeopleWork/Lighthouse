---
title: Kubernetes (Helm)
layout: home
parent: Server Installation
nav_order: 3
---

# Run Lighthouse on Kubernetes with Helm

{: .warning }
> ## 🚧 Preview
>
> **Kubernetes / Helm support is a preview feature and under active development.** The chart works and
> is dogfooded end to end (simple install, OIDC login, horizontal scaling, MCP with OAuth
> auto-discovery), but values and defaults may still change between versions.
> Validate it in a non-production environment before you rely on it, and pin a specific chart + image
> version. Feedback welcome.

Lighthouse ships an official Helm chart so you can run the **Server** edition on any Kubernetes
cluster — bundled or external PostgreSQL, optional OIDC login, an optional MCP server, and horizontal
scaling — from a public chart repository, with no source checkout and no sales call.

{: .note}
The chart is **PostgreSQL-only** — SQLite is a desktop/standalone concern. By default the chart brings
up a bundled PostgreSQL so a single `helm install` gives you a working instance; for production you
point it at your own managed database.

The chart is published at **`https://docs.lighthouse.letpeople.work/charts`**. The full, always-current
**configuration reference** (every value, type, default and description) lives in the chart's generated
[`README.md`](https://github.com/LetPeopleWork/Lighthouse/blob/main/chart/README.md#values) — that table
is generated from the chart's `values.yaml` and verified against it by CI, so it never drifts from the
real keys.

## Architecture

A production deployment looks like this. The chart deploys everything except the Ingress controller,
the external identity provider, and (when you scale) Redis — those are cluster/operator concerns you
bring.

```mermaid
flowchart LR
    user(["User / browser"])
    idp[("External IdP<br/>OIDC issuer")]
    redis[("Redis<br/>backplane")]

    subgraph cluster[Kubernetes cluster]
        ingress[Ingress]
        api["Lighthouse API<br/>(embedded SPA, in-app OIDC)"]
        mcp["MCP server<br/>(optional)"]
        pg[("PostgreSQL<br/>bundled or external")]
    end

    user -->|HTTPS| ingress
    ingress -->|/| api
    ingress -->|/mcp| mcp
    api -->|OIDC login redirect| idp
    api --> pg
    mcp -->|LIGHTHOUSE_URL| api
    api -.->|when replicaCount > 1| redis
```

- **Ingress → API.** The API serves the React SPA in-process (`frontend.mode=embedded`, the
  standalone-parity shape). Authentication is **in-app OIDC** (`oidc.*` → `Authentication:*`); there is
  no separate auth proxy — the API validates the IdP itself. Forwarded-headers (`app.proxy.*`) make the
  redirect URIs and secure cookies correct behind the Ingress. OIDC login is a **Premium** feature and
  needs a valid licence — see [Login (OIDC)](#login-oidc).
- **Ingress → MCP** (optional, `mcp.enabled`). The MCP HTTP server is an independent workload on the
  `/mcp` path; inbound auth is pass-through (`mcp.auth.mode` = `apikey` or `oauth`).
- **API → PostgreSQL.** Bundled (`postgresql.enabled=true`, a StatefulSet) or external
  (`externalDatabase.*`, e.g. a managed/CNPG/RDS/Azure instance).
- **API ⇢ Redis.** Only when you scale past one replica: Redis is the SignalR backplane and the
  single-instance background-work lock, so the fleet syncs once. The chart bundles no Redis — you
  provide a connection string.

## Prerequisites

- A Kubernetes cluster (v1.27+) and `kubectl` pointed at it.
- [Helm](https://helm.sh/docs/intro/install/) v3.12+.
- An Ingress controller (e.g. ingress-nginx or Traefik) **for production**. The quick-start below skips
  the Ingress and uses `kubectl port-forward` so you can try the chart on any cluster.
- For production: a hostname + TLS secret, and — if you enable login — an OIDC identity provider.

## Quick-start

This gets you a responding Lighthouse instance on any cluster (including a local
[kind](https://kind.sigs.k8s.io/) or minikube), bundled PostgreSQL, no Ingress:

```sh
helm repo add letpeoplework https://docs.lighthouse.letpeople.work/charts
helm repo update
helm search repo lighthouse          # CHART 0.1.1 / APP 26.6.21.1

helm install l8e letpeoplework/lighthouse \
  --set postgresql.auth.password='change-me' \
  --set ingress.enabled=false \
  --wait --timeout 5m
```

When the install returns, reach the app:

```sh
kubectl rollout status deploy -l app.kubernetes.io/instance=l8e
kubectl port-forward svc/l8e-lighthouse-api 8080:80
# open http://localhost:8080 — you should see the Lighthouse landing page
```

**Observable output:** the API and PostgreSQL pods report `Running` / `1/1`, `GET /health/ready`
returns `200`, and `GET /` returns the SPA (`<title>Lighthouse</title>`).

{: .important}
`postgresql.auth.password` has no default — the chart fails fast without it (ADR-082). Use a real
secret in production, not `--set` on the command line.

## Production install

Copy the chart's [`values-enterprise.yaml`](https://github.com/LetPeopleWork/Lighthouse/blob/main/chart/values-enterprise.yaml)
production-reference values, fill the REQUIRED fields (host, TLS secret, database, and — if you want
login — OIDC), and install with `-f`:

```sh
helm install l8e letpeoplework/lighthouse --version 0.1.1 -f values-enterprise.yaml
```

See the [configuration reference](https://github.com/LetPeopleWork/Lighthouse/blob/main/chart/README.md#values)
for every option. The common production knobs:

| Concern | Values |
|---|---|
| **Public URL + TLS** | `ingress.host`, `ingress.tls=true`, `ingress.tlsSecretName` |
| **External database** | `postgresql.enabled=false`, `externalDatabase.{host,port,database,user,password}` |
| **Login (OIDC)** | `oidc.enabled=true`, `oidc.issuer`, `oidc.clientId`, `oidc.clientSecret`, plus `app.proxy.trustedProxies`/`trustedNetworks`. See [Login (OIDC)](#login-oidc) — **Premium**. |
| **MCP server** | `mcp.enabled=true`, `mcp.image`, `mcp.auth.mode` |
| **Horizontal scaling** | `replicaCount: N` **and** `redis.connectionString` (required together) |

### Login (OIDC)

OIDC login is a **Premium feature**. With `oidc.enabled=true` the chart wires the IdP correctly, but
until the instance has a **valid Premium licence** it stays in *blocked* mode (`/api/latest/auth/mode`
returns `Blocked`) and no one can sign in.

{: .important}
**Import your licence _before_ you enable OIDC.** The licence-import API requires an authenticated
system admin, but with OIDC on and no valid Premium licence yet there is no way to authenticate —
a chicken-and-egg. So: install with auth off → open the app → import the licence (Settings → Licence)
→ *then* `helm upgrade --set oidc.enabled=true`.

Key OIDC values:

| Value | Default | Notes |
|---|---|---|
| `oidc.issuer` / `oidc.clientId` / `oidc.clientSecret` | — | Your IdP. Register the redirect URI `https://<ingress.host>/api/auth/callback` (most IdPs require HTTPS for non-`localhost` hosts). |
| `oidc.audience` | _(empty)_ | The API's resource/audience identifier in your IdP. When set, the API validates the JWT `aud` on bearer tokens; the MCP server advertises it as the RFC 9728 protected resource. **Required when `mcp.auth.mode=oauth`** — the MCP server needs both issuer and resource. Configure it once; it feeds both the API and the MCP server. |
| `oidc.requireHttpsMetadata` | `true` | Keep `true` for production HTTPS issuers (Entra, Keycloak-behind-TLS). Set `false` **only** for a plain-HTTP issuer in a dev cluster, or the API refuses to load the OIDC metadata. |
| `oidc.allowedOrigins` | _(auto)_ | Browser-facing origins permitted under auth. Defaults to your ingress origin (`scheme://ingress.host`) automatically — override only to allow additional origins. The API fails closed if this ends up empty. |
| `app.proxy.trustedProxies` / `trustedNetworks` | `[]` | Needed behind the Ingress so redirect URIs and secure cookies use the right scheme/host. |

The same `oidc.*` block drives any OIDC provider — Keycloak, Microsoft Entra, Auth0, Okta — and is
reused by the MCP server (`mcp.auth.mode=oauth`); you configure the issuer once.

{: .important}
**Behind ingress-nginx, raise the proxy buffer for OIDC.** The OIDC callback returns a large
`Set-Cookie` (the session holds the IdP tokens), which overflows ingress-nginx's default 4&nbsp;KB
response-header buffer — the login round-trip then fails with **502 Bad Gateway** on
`/api/auth/callback`. Set it via `ingress.annotations`:

```yaml
ingress:
  className: nginx
  annotations:
    nginx.ingress.kubernetes.io/proxy-buffer-size: "16k"
```

Other controllers (Traefik, etc.) have their own equivalent; `ingress.annotations` passes any through.

## How-to: the four scenarios

A progressive walkthrough that builds a full deployment one capability at a time. Each scenario is a
`helm upgrade --reuse-values` on top of the previous one, so you can stop at the shape you need:

1. **[Simple](#scenario-1--simple-no-auth)** — bundled Postgres + the backend, no auth.
2. **[Login](#scenario-2--add-login-oidc)** — add OIDC sign-in (Keycloak, Entra, Auth0, …).
3. **[Scale out](#scenario-3--scale-out)** — multiple replicas behind a Redis backplane.
4. **[MCP server](#scenario-4--mcp-server-oidc-oauth)** — expose the MCP HTTP server with OAuth.

### Scenario 1 — simple, no auth

The smallest working instance: one API workload (it serves the SPA in-process) and a bundled Postgres.
No Ingress, no identity provider — reach it with a port-forward.

```sh
helm install l8e letpeoplework/lighthouse \
  --set postgresql.auth.password='change-me' \
  --set ingress.enabled=false --wait --timeout 5m

kubectl port-forward svc/l8e-lighthouse-api 8080:80
# open http://localhost:8080
```

**You should see:** `l8e-lighthouse-api-*` and `l8e-lighthouse-postgres-0` both `Running` (`1/1`), and
the Lighthouse landing page with **no login prompt**. (An init container waits for Postgres first, so
the API does not crash-loop on a cold database.)

### Scenario 2 — add login (OIDC)

Turn on sign-in against your identity provider. This is a **Premium** feature, and the order matters —
read [Login (OIDC)](#login-oidc) for the full why. In short:

**Step 1 — import your licence while auth is still off** (Settings → Licence in the app from Scenario 1).
Without a valid Premium licence the instance stays in *blocked* mode and nobody can sign in; and once
OIDC is on you can no longer reach the licence import unauthenticated. So licence first, OIDC second.

**Step 2 — enable OIDC + the Ingress** (and TLS for any real IdP — Entra and most providers reject
non-HTTPS redirect URIs):

```sh
helm upgrade l8e letpeoplework/lighthouse --reuse-values \
  --set oidc.enabled=true \
  --set oidc.issuer='https://your-idp.example/realms/lighthouse' \
  --set oidc.clientId='lighthouse' \
  --set oidc.clientSecret='<client-secret>' \
  --set ingress.enabled=true --set ingress.className=nginx \
  --set ingress.host='lighthouse.example.com' \
  --set ingress.tls=true --set ingress.tlsSecretName='lighthouse-tls' \
  --set 'app.proxy.trustedNetworks[0]=10.0.0.0/8' \
  --set 'ingress.annotations.nginx\.ingress\.kubernetes\.io/proxy-buffer-size=16k'
  # plain-HTTP dev issuer only: add --set oidc.requireHttpsMetadata=false
```

Register the redirect URI **`https://<ingress.host>/api/auth/callback`** in your IdP.

{: .important }
The `proxy-buffer-size` annotation is **required behind ingress-nginx** — the OIDC callback's large
`Set-Cookie` overflows the default 4 KB buffer and login fails with **502**. See
[Login (OIDC)](#login-oidc).

**You should see:** `/api/latest/auth/mode` returns `Enabled`; opening `https://<ingress.host>` redirects
you to the IdP, and after sign-in you land back in Lighthouse authenticated. The **same `oidc.*` block**
works for any provider — only the values change.

### Scenario 3 — scale out

Run more than one API replica behind a Redis backplane. Redis is the SignalR backplane, the
single-instance background-work lock (so the fleet syncs once), **and** the shared Data Protection key
store — that last part is what lets a login cookie issued by one pod be read by another, so OIDC keeps
working across replicas. The chart wires all three automatically once `redis.connectionString` is set.

```sh
helm upgrade l8e letpeoplework/lighthouse --reuse-values \
  --set replicaCount=2 \
  --set redis.connectionString='redis-master.redis.svc.cluster.local:6379'
kubectl rollout status deploy -l app.kubernetes.io/instance=l8e
```

**You should see:** two API pods running side by side, a zero-downtime rolling update, and — still able
to sign in (the login round-trip survives requests landing on either pod). Background sync runs once
across the fleet.

{: .note}
`replicaCount > 1` **requires** `redis.connectionString` — the chart rejects the install otherwise, so it
never brings up a split-brain fleet.

### Scenario 4 — MCP server (OIDC oauth)

Expose the optional MCP HTTP server so AI clients can query your flow data. With `mcp.auth.mode=oauth`
the MCP server reuses the **same** `oidc.issuer` + `oidc.audience` from Scenario 2 — you configure the
identity once. Callers present their own IdP Bearer token, which the MCP server forwards to the API; the
API validates it. No-auth and shared-API-key modes are not used here.

```sh
helm upgrade l8e letpeoplework/lighthouse --reuse-values \
  --set mcp.enabled=true --set mcp.auth.mode=oauth \
  --set mcp.image='ghcr.io/letpeoplework/lighthouse-clients/mcp-http:1.3.2'
  # mcp.auth.mode=oauth requires oidc.audience (set in Scenario 2) — the server needs issuer AND resource
kubectl rollout status deploy/l8e-lighthouse-mcp
```

**You should see:** the `l8e-lighthouse-mcp` Deployment available on the `/mcp` Ingress path; the MCP
server advertises RFC 9728 protected-resource metadata at `/.well-known/oauth-protected-resource/mcp`
(the chart routes that root well-known path to the MCP server; it names your IdP as the authorization
server and `oidc.audience` as the resource); and a tool call without a valid Bearer is rejected with
`401` + a `WWW-Authenticate` challenge whose `resource_metadata` points at that `https://` URL — so an
external MCP client can auto-discover the IdP and run the browser OAuth flow. Auth is enforced end to end.

> **Note (IdP support).** Auto-discovery follows RFC 9728/8414: the client reads the authorization server
> from the protected-resource metadata, then fetches that server's metadata and (if needed) registers a
> client. IdPs that serve their metadata at the issuer's well-known and support dynamic client
> registration (e.g. Keycloak) work out of the box; Microsoft Entra needs a pre-registered public client
> (no DCR) and serves its metadata under the tenant path, so configure the client app explicitly there.

## Uninstall

```sh
helm uninstall l8e
kubectl delete pvc -l app.kubernetes.io/instance=l8e   # bundled-Postgres data volume, if you want it gone
```
