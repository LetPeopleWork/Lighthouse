# Slice 03 — Full production stack via values-enterprise.yaml

> Story: #5199 · Persona: platform-operator (self-hoster) · Size: ≤1 day · Release: R1 (configurable)

## Learning hypothesis
We believe a self-hoster can bring up the WHOLE production-grade stack — API + Postgres + (optional)
MCP + OIDC, behind ingress — from one `values-enterprise.yaml`. We will know it is true when
`helm install l8e ./chart -f values-enterprise.yaml` of the real image stands up every workload Ready
and the app authenticates via OIDC behind the configured hostname.

## Elevator Pitch
- **Before:** the production wiring (DB, MCP, OIDC, secure cookies behind a proxy) is the operator's burden to assemble.
- **After:** they run `helm install l8e ./chart -f values-enterprise.yaml` and `kubectl get pods` shows `l8e-api`, `l8e-postgres`, and (toggle on) `l8e-mcp` all Ready; logging in routes through OIDC over the configured HTTPS host.
- **Decision enabled:** "Lighthouse is production-deployable as a unit — DB, auth and MCP included — so I can run it for real."

## In scope
- values for: Postgres connection (creds/host/db), `mcp.enabled` toggle, OIDC config (issuer/client/secret/callback), `frontend.mode: embedded|split` toggle (default embedded).
- Fill out `values-enterprise.yaml` end to end as the production reference, every key commented.
- Consume epic-5305 pre-reqs as config surface: forwarded-headers (behind-proxy), expand-only migrations (startup), graceful shutdown — wired through values, NOT redesigned.
- Vendor-neutral: no hard-coded LPW substrate/DB/identity choices (Q1/Q2/Q3 stay the operator's values).

## Out of scope
- Publishing to the Helm repo (slice 04). Docs prose / diagram (slice 05). Anything in the private gitops overlay (ArgoCD/DNS/ESO — out of feature scope).

## Production-data acceptance (real, not synthetic)
- A real `helm install l8e ./chart -f values-enterprise.yaml` of the real image brings up API + Postgres + (mcp.enabled) MCP, all Ready.
- A user completes an OIDC login over the configured HTTPS hostname with no redirect loop (epic-5305 forwarded-headers pre-req honoured behind the ingress/proxy).
- `mcp.enabled: false` yields no MCP workload; `true` yields a Ready MCP workload.
- `frontend.mode` left at `embedded` preserves the single-container shape (standalone gate).

## Dogfood moment
Stand up the full stack from values-enterprise.yaml against k3s + a test OIDC (e.g. Keycloak), log in, hit MCP.

## Embedded AC (Given/When/Then)
- Given values-enterprise.yaml with Postgres + OIDC + `mcp.enabled: true`, When the operator installs, Then API, Postgres and MCP workloads all reach Ready.
- Given the app is behind the ingress with OIDC configured, When a user logs in over the HTTPS host, Then the OIDC callback uses https + the public host and the session persists (no loop).
- Given `mcp.enabled: false`, When the chart renders, Then no MCP workload is created.
- Given a required production value (e.g. Postgres password) is omitted, When the operator installs, Then rendering fails fast naming the missing key — not a half-broken release.
- Given `frontend.mode: embedded` (default), When the chart renders, Then the API serves the SPA in-process (standalone gate preserved).
