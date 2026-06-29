# Slice 09 — fleet-observability

- **ADO story**: #5206 (Observability)
- **job_id**: `job-saas-operator-observe-fleet`
- **Band**: Fleet operations

## Learning hypothesis

> epic-5305's off-by-default OTel / `/metrics` / Serilog-JSON, turned **on per tenant** and scraped
> into one stack with **per-tenant labels**, gives a fleet dashboard that answers "is everyone OK?"
> at a glance and pinpoints the one degraded tenant — without per-tenant cardinality overwhelming the
> store, and without touching the standalone product (telemetry stays off there). If true, a small
> team can operate many tenants and honour SLAs.

## Elevator Pitch

- **Before**: Each tenant is a black box; finding the sick one means logging into each.
- **After**: Benjamin opens one Grafana fleet dashboard → sees per-tenant request/error/latency tiles and a red tile on the one degraded tenant.
- **Decision enabled**: "Which tenant is unhealthy right now, and is the fleet OK?" — answered at a glance.

## In / Out

- **IN**: Telemetry enabled per hosted tenant; one Prometheus/Loki/Grafana (or OTel collector) with per-tenant labels; a fleet dashboard + per-tenant drill-down; per-tenant + fleet alerts; bounded label cardinality.
- **OUT**: Customer-facing status page; long-term metrics warehousing; the standalone self-hoster (telemetry stays OFF by default there — standalone gate).

## Dogfood moment

Tenant Zero (LPW production) is the first instance on the fleet dashboard — we watch our own production through the same lens customers get.

## Thin end-to-end path

Enable telemetry values per tenant → deploy the monitoring stack via ArgoCD → scrape with per-tenant labels → build the fleet dashboard → induce a fault on a demo tenant → its tile goes red + an alert fires.

## Done = observable

- One dashboard shows all tenants' health with per-tenant attribution.
- A degraded tenant is visibly flagged and alerts fire (per-tenant + fleet).
- The standalone product is unaffected (telemetry off-by-default verified).

## Depends on

- slice-07 (a fleet to observe), epic-5305 telemetry signals.
