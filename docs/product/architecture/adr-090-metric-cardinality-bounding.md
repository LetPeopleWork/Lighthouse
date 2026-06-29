# ADR-090: Fleet Observability on kube-prometheus-stack — `tenant` Is the *One* Bounded High-Value Label; Unbounded Labels Are Dropped at Scrape; Per-Tenant Recording Rules Pre-Aggregate the Fleet Dashboard; a Cardinality-Budget Alert Guards the TSDB

**Status**: **ACCEPTED** (2026-06-29, Benjamin) — O-4: design cardinality budget for ≥20 tenants/cluster, start ~5–10
**Date**: 2026-06-29
**Feature**: epic-5306-productization-platform (ADO Epic #5306, story #5206 observability) — resolves the **metric-cardinality red card**
**Decider**: Benjamin (product owner) + Titan (System Designer, PROPOSE)
**Relationship to prior work**: REUSES the epic-5305 off-by-default `/metrics` + Serilog-JSON + OTel hooks (ADR-078) — the chart sets `telemetry.enabled=true` per hosted tenant; the standalone product stays telemetry-off (D0). Composes with ADR-086 (kube-prometheus-stack is a `platform/` component) and the CC-6 `tenant` label derived from the single identifier.

---

## Context

US-08 needs per-tenant *and* fleet health in one stack, with per-tenant attribution — but a `tenant` label naively multiplied across already-high-cardinality series is the classic Prometheus blow-up: `series = tenants × paths × statuses × pods × …` explodes the TSDB head and the scrape budget. The red card: bound per-tenant cardinality without losing per-tenant visibility.

## Back-of-envelope (the numbers that set the budget)

- Density target ≥20 tenants/cluster; design headroom **~200 tenants/cluster**.
- Lighthouse API base series per instance ≈ a few hundred (HTTP server, runtime, EF). Call it **~500 series/instance**.
- Per tenant: ~1–2 API replicas + 1 CNPG primary. Effective ~**1.5× exporters/tenant**.
- Naive (with full path/status/pod labels left on): 200 tenants × 500 × 1.5 ≈ **150K series** — survivable but wasteful, and one careless unbounded label (raw URL path, user id) turns 500 into tens of thousands → millions of series. **The risk is the unbounded labels, not the `tenant` label itself.**

## Decision

1. **`tenant` is the single, bounded, high-value dimension.** It is sourced from the CC-6 id (≤ low hundreds), injected as an external label per scrape target (ServiceMonitor/PodMonitor namespaceSelector → the namespace *is* the tenant). It never multiplies with another unbounded label because of rule 2.
2. **Drop unbounded labels at scrape** via `metric_relabel_configs`: no raw request `path` (use route templates / bounded handler names only), no `user`/`id`/`url`, drop noisy per-pod labels where the pod identity adds no operational value. The label set per metric is fixed and reviewed.
3. **Recording rules pre-aggregate the per-tenant rollups** the fleet dashboard reads: e.g. `tenant:http_requests:rate5m`, `tenant:http_errors:ratio5m`, `tenant:http_latency:p99` — one low-cardinality series per tenant per signal. The fleet dashboard queries the *recorded* series, never the raw high-cardinality ones, so a 200-tenant dashboard is ~200 series per tile, not 200×N.
4. **A standing cardinality-budget alert**: `prometheus_tsdb_head_series` > a configured budget (sized from the math above with headroom) fires before the TSDB is endangered — the cardinality bound is *enforced and observable*, not just intended (Earned-Trust: the store proves it is within budget).
5. **Per-tenant alerts** ride the recorded series (`tenant:http_errors:ratio5m > threshold`), so a degraded tenant flags its dashboard tile red and fires an alert routed with the `tenant` label.

## Consequences

- **Positive**: full per-tenant visibility at a bounded, predictable series count; the fleet dashboard stays fast (queries pre-aggregated series); a careless unbounded label is caught by the budget alert before it hurts; standalone stays telemetry-off (gate intact).
- **Negative / cost**: route templating discipline is required in scrape config (no raw-path labels); recording rules are a small maintained artifact; very-deep per-request drill-down is intentionally *not* in metrics (that is logs/traces' job — Serilog-JSON + OTel, also per-tenant labelled but sampled).
- **Standalone gate**: untouched — telemetry stays off by default in the single-container product (verified by the off-by-default gate); per-tenant scraping is a hosted-platform overlay.

## Alternatives considered

1. **`tenant` label on raw, unaggregated series for dashboards** — rejected: dashboards would query tenants × high-cardinality series, slow and TSDB-heavy.
2. **A Prometheus/Thanos instance per tenant** — rejected: strong isolation but multiplies the operational surface ~200× against the density goal; one stack with a bounded `tenant` label + recording rules meets the need.
3. **No per-tenant label, aggregate-only** — rejected: fails US-08 ("which tenant is unhealthy?") — per-tenant attribution is the whole point.
