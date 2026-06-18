# Slice 05: App observability hooks (/metrics + structured logging + traces)

**Feature**: epic-5305-k8s-readiness
**Story**: US-05 (ADO #5312) → job-operator-observe-in-cluster
**Estimate**: ~1.5 crafter days
**Reference class**: new instrumentation wiring (OpenTelemetry .NET + Prometheus exporter + structured logging provider)

## Goal
Instrument the app for cluster observability: expose a Prometheus `/metrics` endpoint, emit structured JSON logs to stdout, and add OpenTelemetry traces — in-app instrumentation only, low-overhead / off-by-default where appropriate so the single-container self-hoster pays nothing.

## IN scope
- Prometheus `/metrics` endpoint (request rate / error rate / latency at minimum) via OpenTelemetry metrics + the Prometheus exporter.
- Structured JSON logging to stdout (configurable), preserving today's log content but in queryable JSON.
- OpenTelemetry tracing (ASP.NET Core + HttpClient + EF instrumentation) exporting via OTLP, exporter off/no-op unless configured.

## OUT scope
- The cluster-side Prometheus / Grafana / Loki stack — Productization epic #5306, story 16.
- Per-tenant metric labelling / multi-tenant dashboards → #5306.
- Business KPI instrumentation (those live in `docs/product/kpi-contracts.yaml`); this slice is operational telemetry, not product KPIs.

## Learning hypothesis
**Confirms if it succeeds**: a local Prometheus scrapes `/metrics` and a local Grafana shows Lighthouse request/error/latency; JSON logs parse field-wise in Loki; a slow request is traceable.
**Disproves if it fails**: always-on instrumentation imposes measurable overhead on the single container, forcing a stricter off-by-default posture (and documentation that self-hosters must opt in).

## Acceptance criteria
See US-05 in `../feature-delta.md`. Key: an integration test asserts `/metrics` returns Prometheus-format output including HTTP server metrics; logs emitted in the JSON shape contain the expected fields; with telemetry disabled, no exporter runs and log/format behaviour matches the configured default (standalone gate — no perf change).

## Dependencies
None. Can land any time; valuable before slice 07's multi-replica work (so the operator isn't flying blind during scale-out).

## Production data requirement
**Recommended.** Scrape the dev instance with a real local Prometheus and confirm a dashboard renders; not strictly required for the unit-level acceptance.

## Dogfood moment
Operator points a local Prometheus + Grafana at the dev instance and sees a live Lighthouse dashboard within the day.

## Cross-cutting checklist (confirmed in feature-delta)
RBAC: confirm whether `/metrics` needs gating (it can leak request paths); default to unauthenticated cluster-internal surface but DESIGN must decide exposure (Sonar/security). Clients: N/A. Website: N/A.

## Pre-slice spike candidates
- Pick the metrics surface (OpenTelemetry.Exporter.Prometheus vs. prometheus-net) and confirm it coexists with our logging. (~1 hr)
- Measure overhead of always-on ASP.NET Core + EF tracing to decide the default. (~1 hr)
