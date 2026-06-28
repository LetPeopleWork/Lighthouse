# Lighthouse Helm chart

Flow metrics and probabilistic forecasting for Kubernetes. Postgres-only (ADR-080); the chart
brings the whole stack up — API (SPA served in-process), bundled or external Postgres, optional MCP
workload and OIDC — with one command.

- **Chart version:** `0.1.1`
- **App image (appVersion):** `26.6.21.1`

> This README's **Values** section is generated from `values.yaml` by [`helm-docs`](https://github.com/norwoodj/helm-docs).
> Edit the `# --` comments in `values.yaml`, then regenerate (`helm-docs --chart-search-root chart --skip-version-footer -s file --ignore-non-descriptions`).
> The `config-ref drift` CI gate fails the build if this file is stale.

## Install from the published Helm repo (no source checkout)

```sh
helm repo add letpeoplework https://docs.lighthouse.letpeople.work/charts
helm repo update
helm search repo lighthouse          # shows CHART 0.1.1 / APP 26.6.21.1
helm install l8e letpeoplework/lighthouse --version 0.1.1 -f values-enterprise.yaml
```

The default values render the standalone-parity shape (`frontend.mode=embedded`, one API workload,
bundled Postgres). For production, copy [`values-enterprise.yaml`](./values-enterprise.yaml), fill the
REQUIRED values (host, TLS secret, Redis when scaling, OIDC, MCP, external DB) and pass it with `-f`.

## Install from a source checkout (development)

```sh
helm install l8e ./chart -f chart/values-enterprise.yaml
```

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
| ingress.className | string | `""` | Ingress class name (e.g. traefik, nginx). Empty uses the cluster default. |
| ingress.host | string | `"lighthouse.local"` | Public hostname the app is served on (drives the OIDC callback + NOTES.txt URL). |
| ingress.tls | bool | `false` | Enable TLS on the Ingress (host must be set). |
| ingress.tlsSecretName | string | `""` | TLS secret name (when tls=true and you bring your own cert). |
| resources | object | `{"limits":{"memory":"1Gi"},"requests":{"cpu":"100m","memory":"256Mi"}}` | Resource requests/limits for the API container. |
| postgresql.enabled | bool | `true` | Deploy a bundled in-chart Postgres StatefulSet (ADR-080). Set false to bring your own (slice-03). |
| postgresql.image | string | `"postgres:17"` | Bundled Postgres image (official, vendor-neutral). |
| postgresql.auth.database | string | `"lighthouse"` | Database name. |
| postgresql.auth.username | string | `"lighthouse"` | Database user. |
| postgresql.auth.password | string | `""` | Database password. REQUIRED — no default (ADR-082, explicit password). |
| postgresql.persistence.size | string | `"8Gi"` | PVC size for the bundled Postgres data volume. |
| postgresql.persistence.storageClass | string | `""` | StorageClass for the PVC. Empty uses the cluster default. |
| shutdownTimeoutSeconds | int | `30` | Bounded graceful-shutdown drain window (seconds); maps to Shutdown:TimeoutSeconds + terminationGracePeriodSeconds (epic-5305 #5309). |
| telemetry.enabled | bool | `false` | Enable OpenTelemetry /metrics + JSON logs (epic-5305 #5312). Off by default (self-hoster). |
| redis.connectionString | string | `""` | Redis connection string (ConnectionStrings:Redis). REQUIRED when replicaCount>1 — enables the    epic-5305 #5304 SignalR backplane + single-instance background work so the fleet syncs once.    Operator-provided (the chart bundles no Redis; vendor-neutral). Empty = single-replica only. |
| externalDatabase | object | `{"database":"","host":"","password":"","port":5432,"user":""}` | Bring-your-own Postgres (used when postgresql.enabled=false). Vendor-neutral (managed / CNPG / RDS / Azure). |
| oidc.enabled | bool | `false` | Enable OIDC login (Authentication:*). Off = no auth (standalone parity). Needs forwarded-headers behind ingress. |
| oidc.issuer | string | `""` | OIDC authority / issuer URL. |
| oidc.clientId | string | `""` | OIDC client id. |
| oidc.clientSecret | string | `""` | OIDC client secret (REQUIRED when oidc.enabled). |
| oidc.audience | string | `""` | API audience / resource identifier (Authentication:Audience). When set, the backend validates the    JWT `aud` claim on bearer tokens, and the MCP server advertises it as the RFC 9728 protected    resource (LIGHTHOUSE_OAUTH_RESOURCE). REQUIRED when mcp.auth.mode=oauth. Empty = no audience    validation (browser cookie login still works). Deployment-specific — set it to the API's resource    identifier registered in your IdP (e.g. an Entra Application ID URI, or a Keycloak audience). |
| oidc.callbackPath | string | `"/api/auth/callback"` | OIDC callback path. |
| oidc.requireHttpsMetadata | bool | `true` | Require the OIDC issuer/metadata to be served over HTTPS (Authentication:RequireHttpsMetadata).    Keep true in production (Entra, Keycloak-behind-TLS, etc.). Set false ONLY for a plain-HTTP    issuer in local/dev clusters — the backend otherwise refuses to load HTTP OIDC metadata. |
| oidc.allowedOrigins | list | `[]` | Browser-facing origins allowed to call the API under auth (Authentication:AllowedOrigins).    The backend fails closed if auth is on and this is empty (no wildcard CORS). Empty list =    derive the single ingress origin (scheme+host) from ingress.host/ingress.tls automatically.    Override only to allow extra origins (e.g. a separate SPA host). |
| app.proxy.trustedProxies | list | `[]` | Trusted reverse-proxy IPs (epic-5305 #5311) so OIDC redirect URIs + secure cookies are correct behind the ingress. |
| app.proxy.trustedNetworks | list | `[]` | Trusted proxy CIDR networks. |
| mcp.enabled | bool | `false` | Deploy the optional MCP HTTP server workload (ADR-085). Orthogonal to frontend.mode. |
| mcp.image | string | `"ghcr.io/letpeoplework/lighthouse-clients/mcp-http:latest"` | MCP server image (lighthouse-clients mcp-http). Pin a real tag in production. |
| mcp.auth.mode | string | `"apikey"` | Inbound-auth model (ADR-079): apikey (caller's X-Api-Key pass-through) or oauth (IdP Bearer pass-through). |
