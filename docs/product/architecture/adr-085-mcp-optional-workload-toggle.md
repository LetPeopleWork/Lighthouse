# ADR-085: The MCP HTTP Server Is an Optional Chart Workload Gated by `mcp.enabled` (Orthogonal to `frontend.mode`); When On It Renders a Deployment + Service Using the lighthouse-clients `mcp-http` Image With Its Inbound-Auth Surface per ADR-079

**Status**: **Accepted** (2026-06-21 — accepted by Benjamin)
**Date**: 2026-06-21
**Feature**: epic-5306-k8s-productization (ADO Epic #5306, story #5199)
**Decider**: Benjamin (product owner) + System Designer (PROPOSE)
**Relationship to prior work**: Realises DISCUSS slice-03 (`mcp.enabled` toggle) and wires the **ADR-079** inbound-auth model (X-Api-Key pass-through for CLI/stdio; IdP JWT Bearer for the hosted OAuth flow). The MCP server binary lives in the **lighthouse-clients** repo (`mcp-http` image); the chart deploys it, it is not Lighthouse backend code.

---

## Context

The operator asked for a clean "branch" so they decide whether the MCP HTTP server is part of the deployment — included and wired when wanted, absent otherwise. The MCP server is a genuine *second* workload (planning-stage D4), separate from the API, published as the `mcp-http` container by the clients repo. ADR-079 settled its inbound-auth: the caller presents its **own** credential (X-Api-Key, or an IdP-issued `Authorization: Bearer` for the hosted no-API-key flow), which `mcp-http` forwards to Lighthouse — no shared baked key, no confused deputy.

## Decision

**`mcp.enabled` (boolean) gates the MCP server as an optional, independent chart workload.**

- **`mcp.enabled: false` (default-able)** → the chart renders **no MCP objects at all** (no Deployment, no Service). The stack is API + Postgres only.
- **`mcp.enabled: true`** → the chart renders an MCP `Deployment` + `Service` from the `mcp.image.*` values (the clients `mcp-http` image), wired to reach the in-cluster Lighthouse API, with the **inbound-auth surface from ADR-079** exposed as `mcp.auth.*` values:
  - **X-Api-Key pass-through** (CLI/stdio/standalone model) — the default, reuses Lighthouse's owner-resolved + per-key-scoped API keys; the caller sends its own key.
  - **IdP OAuth Bearer** (hosted model) — `mcp-http` advertises RFC 9728 protected-resource metadata and forwards the caller's `Authorization: Bearer`; Lighthouse validates the IdP JWT (the ADR-079 `LighthouseJwtBearer` scheme). The live end-to-end OAuth dogfood (IdP audience/scope, RFC 8707 resource indicators, the server version gate `> v26.6.16.14`) is the deployment prerequisite ADR-079 defers to this epic, documented with the chart.
- **Orthogonality**: `mcp.enabled` is independent of `frontend.mode`, `replicaCount`, and `database.*` — any combination renders coherently. `oauth2-proxy` is **not** placed in front of `/mcp` (ADR-079 — the MCP client drives OAuth itself; a proxy would be a redundant edge gate and would break standalone parity).

## Consequences

- **Positive**: operators opt in/out of MCP with one value; no MCP attack surface or workload when off; when on, the secure no-shared-key auth model (ADR-079) is the wired default; the toggle composes cleanly with every other chart dimension.
- **Negative / cost**: the chart depends on the externally-versioned `mcp-http` image (clients repo) — the config reference documents the compatible image/version and the server version gate; the live OAuth dogfood needs real IdP config (the ADR-079 readiness checklist), surfaced in the enterprise docs.
- **Standalone gate**: N/A for the standalone image (no MCP workload there); within the chart, `mcp.enabled: false` is the no-MCP shape.

## Alternatives considered

1. **Always deploy MCP.** Rejected — forces an attack surface + an image dependency on operators who don't use MCP.
2. **Bake a single shared API key into the MCP server (today's `mcp-http` default).** Rejected per ADR-079 — confused-deputy ambient authority; the chart wires the pass-through models instead.
3. **`oauth2-proxy` in front of `/mcp`.** Rejected per ADR-079 — redundant edge gate for a programmatic client; breaks standalone parity.
