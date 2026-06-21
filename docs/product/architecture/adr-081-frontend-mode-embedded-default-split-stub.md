# ADR-081: `frontend.mode: embedded` Is the Chart Default and the Horizontal-Scale Shape (scale via `replicaCount` + the #5304 Redis backplane); `frontend.mode: split` Is a Loud `fail` Stub Deferred to Band D

**Status**: **Accepted** (2026-06-21 — accepted by Benjamin)
**Date**: 2026-06-21
**Feature**: epic-5306-k8s-productization (ADO Epic #5306, story #5199)
**Decider**: Benjamin (product owner) + System Designer (PROPOSE)
**Relationship to prior work**: Realises planning-stage **D4** ("standalone stays single-image; the k8s version may split via `frontend.mode`") and **Q4** ("build the toggle, default it off"). The scaled embedded path consumes the epic-5305 **#5304 Redis backplane** (ADR-075) as a chart value.

---

## Context

The product image builds the React SPA into the backend's `wwwroot`; the API serves SPA + `/api` + `/hub` in one process. Two questions arose: (a) should the k8s chart default to a *split* topology (separate nginx SPA pod) because "k8s is for scaling"? (b) how is the split toggle expressed without forking the chart?

The scaling premise is a misconception worth recording: **the frontend split does not improve API scalability.** The backend scales horizontally by `replicaCount: N` behind a Service regardless of who serves the SPA; the SPA is static and cached at the ingress, so serving it from N API pods costs nothing. Splitting the SPA into nginx buys throughput nowhere. Its *only* real payoff is the **SaaS multi-tenant** case (planning-stage hypothesis #6): one shared tenant-agnostic frontend serving every tenant subdomain while backends stay per-tenant. For a single self-hoster that is zero gain and double the images/versioning. Split also requires artefacts that do not exist today: a separately published static-frontend image, runtime API-base-URL injection, and path-based ingress (`/`→frontend; `/api,/hub,/mcp`→backend) — a Band-D body of work, explicitly out of scope here.

## Decision

**`frontend.mode: embedded` is the chart default and the recommended shape for every topology this epic serves, including horizontally-scaled deployments. Horizontal scale is delivered by `replicaCount: N` (+ `ConnectionStrings:Redis` for the #5304 backplane when `N>1`), NOT by splitting the frontend.**

- **`embedded` (default)** renders only the API `Deployment` + `Service` (the API serves the SPA in-process) → topology identical to the standalone image. `replicaCount` scales it; with `N>1` the operator sets the Redis values so SignalR fan-out and single-instance background work degrade-safely per epic-5305.
- **`split`** is a **stub only**. The values key + `values.schema.json` enum exist so the future drop-in is purely additive (no chart restructuring), but any template branch guarded `{{- if eq .Values.frontend.mode "split" }}` renders a **`fail`** with a clear message — *"frontend.mode=split is not implemented in this chart version; use embedded"* — rather than a silent no-op. Full split wiring (nginx Deployment + path-ingress + runtime API base) is deferred to Band D.

## Consequences

- **Positive**: the default is the simplest correct shape and it scales; the standalone↔chart parity is preserved (embedded = one app workload, same as the image); the split seam is reserved without dead template code that pretends to work; nobody silently gets a no-op (fail-fast).
- **Negative / cost**: an operator who explicitly wants split today is told "not yet" — acceptable, it is a SaaS-tier optimisation with no self-hoster benefit.
- **Standalone gate (D4)**: embedded at default values is byte-identical to the standalone topology (one app workload serving the SPA).

## Alternatives considered

1. **Default the chart to `split`.** Rejected — split does not improve API scalability (the misconception above), needs a non-existent frontend image + runtime API-base + path-ingress, and benefits only the multi-tenant SaaS. Wrong default for a self-hoster chart.
2. **Omit the `split` key entirely until Band D.** Rejected — adding it later would restructure templates and break the values schema's stability; a reserved-but-failing toggle is cheaper and signals intent.
3. **`split` as a silent no-op when set.** Rejected — a values key that silently does nothing is a trap; `fail` is the honest behaviour.
