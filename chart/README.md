# Lighthouse Helm chart

Flow metrics and probabilistic forecasting for Kubernetes. Postgres-only (ADR-080); the chart
brings the whole stack up — API (SPA served in-process), bundled or external Postgres, optional MCP
workload and OIDC — with one command.

- **Chart version:** `0.1.14`
- **App image (appVersion):** `26.9.1.6`

> This README's **Values** section is generated from `values.yaml` by [`helm-docs`](https://github.com/norwoodj/helm-docs).
> Edit the `# --` comments in `values.yaml`, then regenerate (`helm-docs --chart-search-root chart --skip-version-footer -s file --ignore-non-descriptions`).
> The `config-ref drift` CI gate fails the build if this file is stale.

## Install from the published Helm repo (no source checkout)

```sh
helm repo add letpeoplework https://docs.lighthouse.letpeople.work/charts
helm repo update
helm search repo lighthouse          # shows CHART 0.1.14 / APP 26.9.1.6
helm install l8e letpeoplework/lighthouse --version 0.1.14 -f values-enterprise.yaml \
  --set encryption.key=$(openssl rand -base64 32)
```

The default values render the standalone-parity shape (`frontend.mode=embedded`, one API workload,
bundled Postgres). For production, copy [`values-enterprise.yaml`](./values-enterprise.yaml), fill the
REQUIRED values (host, TLS secret, Redis when scaling, OIDC, MCP, external DB) and pass it with `-f`.

## Install from a source checkout (development)

```sh
helm install l8e ./chart -f chart/values-enterprise.yaml \
  --set encryption.key=$(openssl rand -base64 32)
```

## The encryption key

Lighthouse encrypts the credentials it stores for your work tracking systems. **An install that names
neither `encryption.key` nor `encryption.existingSecret` is refused at render**, and the chart never
generates a key of its own. That is deliberate: the only mechanism Helm offers for "generate once"
asks the cluster what already exists, and that question comes back empty on every render with no
cluster to ask — which is every `helm template` and therefore every ArgoCD sync. A chart that
generated would mint a fresh key on each sync and leave every credential stored under the previous
one unreadable. A key you do not supply is a key nobody owns.

There are two ways to own it.

**You hold it.** Pass it as a value, and this release keeps it in a Secret of its own:

```sh
--set encryption.key=$(openssl rand -base64 32)
```

**Your secret store holds it.** Point the chart at a Secret that External Secrets Operator, OpenBao
or an operator fills. The Secret must carry the key `keys`:

```sh
kubectl create secret generic lighthouse-encryption \
  --from-literal=keys="k-2026-08-17-01:$(openssl rand -base64 32)"
helm install l8e letpeoplework/lighthouse --set encryption.existingSecret=lighthouse-encryption
```

Set one or the other, never both.

The key reaches the container as a **mounted file**, not an environment variable — unlike the
database password and the OIDC client secret, which still travel as environment variables. The
difference is on purpose and worth not "aligning" away: an environment variable is readable in a
process dump, and it cannot change under a running process, which would make the rotation below
impossible without restarting the pod.

### Rotating the key

The ring is one line, `id:base64[,id:base64]*`, and the **first entry is the key new secrets are
written under**. Every later entry is only ever read from. Lighthouse never writes to your Secret and
holds no permission to, so all four steps are yours:

1. **Add the new key in front of the old one** in your Secret:
   `keys: k-new:BASE64NEW,k-old:BASE64OLD`
2. **Wait for the instance to pick it up.** It re-reads the file about every thirty seconds and logs
   `encryption.keyring.reloaded` with the key ids it now holds. Rolling the pod also works, but is
   not needed.
3. **Re-encrypt onto the new key** from Settings → Encryption. Existing credentials move to the first
   entry; nothing has to be re-entered.
4. **Drop the old entry** from your Secret, leaving `keys: k-new:BASE64NEW`.

Do step 4 before step 3 and the credentials still on the old key report as unreadable rather than
failing against your work tracking system — recoverable by putting the old entry back and re-running
step 3. A file that does not parse is refused outright: the keys already in force stay in force and
the reason is logged.

Set `encryption.keysReloadSeconds` if thirty seconds is the wrong interval for your secret store.

## Versioning (ADR-083)

The **chart version** (`Chart.yaml: version`) is the single source of truth for the package and is
bumped on every publish — the publish step refuses to overwrite an already-published version. The
**appVersion** mirrors `image.tag` (the Lighthouse image the chart ships by default). The publish
guard (`scripts/version-guard.sh`) asserts both chains agree across `Chart.yaml`, this README, the
in-cluster `NOTES.txt`, the published index and `values-enterprise.yaml` before any publish.

## Publish (maintainer)

```sh
chart/scripts/publish.sh            # guard → helm package → helm repo index --merge into docs/charts/
git add docs/charts chart && git commit && git push   # pages.yml serves docs/charts/ on the existing Pages
```

## Values

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| replicaCount | int | `1` | Number of API replicas. >1 requires Redis (ConnectionStrings:Redis); see slice-02/03. |
| image.repository | string | `"ghcr.io/letpeoplework/lighthouse"` | Lighthouse API image repository. |
| image.tag | string | `""` | Image tag. Empty string falls back to Chart.appVersion (ADR-083 consistency). |
| image.pullPolicy | string | `"IfNotPresent"` | Image pull policy. |
| frontend.mode | string | `"embedded"` | Frontend topology: embedded (API serves the SPA, default/standalone parity) or split.    split is NOT implemented in this chart version and fails loud (ADR-081). |
| ingress.enabled | bool | `true` | Render an Ingress for the API. |
| ingress.annotations | object | `{}` | Extra annotations for the Ingress (controller-specific). When OIDC is enabled behind    ingress-nginx you MUST raise the proxy buffer, or the large Set-Cookie response on the OIDC    callback overflows the default 4k buffer and the login round-trip fails with 502. Example:    annotations:      nginx.ingress.kubernetes.io/proxy-buffer-size: "16k" |
| ingress.className | string | `""` | Ingress class name (e.g. traefik, nginx). Empty uses the cluster default. |
| ingress.host | string | `"lighthouse.local"` | Public hostname the app is served on (drives the OIDC callback + NOTES.txt URL). |
| ingress.tls | bool | `false` | Enable TLS on the Ingress (host must be set). |
| ingress.tlsSecretName | string | `""` | TLS secret name (when tls=true and you bring your own cert). |
| resources | object | `{"limits":{"memory":"1Gi"},"requests":{"cpu":"100m","memory":"256Mi"}}` | Resource requests/limits for the API container. |
| postgresql.enabled | bool | `true` | Deploy a bundled in-chart Postgres StatefulSet (ADR-080). Set false to bring your own (slice-03). |
| postgresql.image | string | `"postgres:17"` | Bundled Postgres image (official, vendor-neutral). |
| postgresql.auth.database | string | `"lighthouse"` | Database name. |
| postgresql.auth.username | string | `"lighthouse"` | Database user. |
| postgresql.auth.password | string | `""` | Database password. REQUIRED — no default (ADR-082, explicit password). Ignored when    existingSecret is set. |
| postgresql.auth.existingSecret | string | `""` | Read DB credentials from a pre-existing Secret instead of rendering them from auth.password.    The Secret MUST provide both keys the chart consumes: `Database__ConnectionString` (the full    Npgsql string) and `postgres-password` (POSTGRES_PASSWORD for the bundled StatefulSet). Lets an    operator (slice-03) or an external secret store (ESO/OpenBao, slice-04) own the credential    without passing it as a Helm value (CC-3). When set, the ADR-082 render-time password `required`    is relaxed. Empty = the chart renders its own Secret from auth.password. |
| postgresql.persistence.size | string | `"8Gi"` | PVC size for the bundled Postgres data volume. |
| postgresql.persistence.storageClass | string | `""` | StorageClass for the PVC. Empty uses the cluster default. |
| shutdownTimeoutSeconds | int | `30` | Bounded graceful-shutdown drain window (seconds); maps to Shutdown:TimeoutSeconds + terminationGracePeriodSeconds (epic-5305 #5309). |
| telemetry.enabled | bool | `false` | Enable OpenTelemetry /metrics + JSON logs (epic-5305 #5312). Off by default (self-hoster). |
| encryption.key | string | `""` | The key stored credentials are encrypted with, as one line: `id:base64[,id:base64]*`, first    entry active. REQUIRED unless existingSecret is set — this chart never generates one. Make one    with `openssl rand -base64 32`. |
| encryption.existingSecret | string | `""` | Read the key from a pre-existing Secret an external store (ESO/OpenBao) owns, instead of    rendering it from encryption.key. The Secret MUST provide the key `keys`, in the same one-line    form. Reaches the container as a mounted file, not an environment variable. |
| encryption.keysReloadSeconds | string | `""` | How often the mounted key file is re-read, in seconds. Empty uses 30. Raise it if your secret    store is slow to materialise a change; a key an operator adds is picked up on the next read,    without the pod being restarted. |
| redis.connectionString | string | `""` | Redis connection string (ConnectionStrings:Redis). REQUIRED when replicaCount>1 — enables the    epic-5305 #5304 SignalR backplane + single-instance background work so the fleet syncs once.    Operator-provided (the chart bundles no Redis; vendor-neutral). Empty = single-replica only. |
| externalDatabase | object | `{"database":"","host":"","password":"","port":5432,"user":""}` | Bring-your-own Postgres (used when postgresql.enabled=false). Vendor-neutral (managed / CNPG / RDS / Azure). |
| oidc.enabled | bool | `false` | Enable OIDC login (Authentication:*). Off = no auth (standalone parity). Needs forwarded-headers behind ingress. |
| oidc.issuer | string | `""` | OIDC authority / issuer URL. |
| oidc.clientId | string | `""` | OIDC client id. |
| oidc.clientSecret | string | `""` | OIDC client secret (REQUIRED when oidc.enabled, unless existingSecret is set). |
| oidc.existingSecret | string | `""` | Read the OIDC client secret from a pre-existing Secret instead of rendering it from    oidc.clientSecret. The Secret MUST provide the key `Authentication__ClientSecret`. Lets an    external secret store (ESO/OpenBao, slice-04) own the OIDC credential without passing it as a    Helm value (CC-3). When set, the render-time clientSecret `required` is relaxed. Empty = the    chart renders the OIDC key into its own Secret from oidc.clientSecret. Mirrors    postgresql.auth.existingSecret (slice-03) for the OIDC client secret. |
| oidc.audience | string | `""` | API audience / resource identifier (Authentication:Audience). When set, the backend validates the    JWT `aud` claim on bearer tokens, and the MCP server advertises it as the RFC 9728 protected    resource (LIGHTHOUSE_OAUTH_RESOURCE). REQUIRED when mcp.auth.mode=oauth. Empty = no audience    validation (browser cookie login still works). Deployment-specific — set it to the API's resource    identifier registered in your IdP (e.g. an Entra Application ID URI, or a Keycloak audience). |
| oidc.callbackPath | string | `"/api/auth/callback"` | OIDC callback path. |
| oidc.requireHttpsMetadata | bool | `true` | Require the OIDC issuer/metadata to be served over HTTPS (Authentication:RequireHttpsMetadata).    Keep true in production (Entra, Keycloak-behind-TLS, etc.). Set false ONLY for a plain-HTTP    issuer in local/dev clusters — the backend otherwise refuses to load HTTP OIDC metadata. |
| oidc.allowedOrigins | list | `[]` | Browser-facing origins allowed to call the API under auth (Authentication:AllowedOrigins).    The backend fails closed if auth is on and this is empty (no wildcard CORS). Empty list =    derive the single ingress origin (scheme+host) from ingress.host/ingress.tls automatically.    Override only to allow extra origins (e.g. a separate SPA host). |
| app.embed.enabled | bool | `false` | Serve the embed surface (`/embed/start`, `/embed/handshake`, `/embed/enter`) that signs a viewer into a framed Lighthouse — what the Jira app uses. Off unless you frame Lighthouse somewhere: the handshake nonce is not yet bound to whoever requested it, so on an instance that opts in, a crafted link can hand a viewer's session to someone else (Epic #5674). |
| app.timeZone | string | `""` | Instance time zone as an IANA id (e.g. `Europe/Zurich`) used for every calendar day — "today", metric window bounds, the day a snapshot is filed under (Bug #5567). Empty keeps the pod's zone, which is UTC, so set this if your team is not on UTC. An id that cannot be resolved fails startup rather than falling back. |
| app.proxy.trustedProxies | list | `[]` | Trusted reverse-proxy IPs (epic-5305 #5311) so OIDC redirect URIs + secure cookies are correct behind the ingress. |
| app.proxy.trustedNetworks | list | `[]` | Trusted proxy CIDR networks. |
| mcp.enabled | bool | `false` | Deploy the optional MCP HTTP server workload (ADR-085). Orthogonal to frontend.mode. |
| mcp.image | string | `"ghcr.io/letpeoplework/lighthouse-clients/mcp-http:latest"` | MCP server image (lighthouse-clients mcp-http). Pin a real tag in production. |
| mcp.auth.mode | string | `"apikey"` | Inbound-auth model (ADR-079): apikey (caller's X-Api-Key pass-through) or oauth (IdP Bearer pass-through). |
