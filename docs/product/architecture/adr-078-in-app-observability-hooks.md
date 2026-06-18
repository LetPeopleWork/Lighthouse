# ADR-078: In-App Observability Hooks — OpenTelemetry .NET (ASP.NET Core Instrumentation + Prometheus `/metrics` Exporter + OTLP Traces) + Serilog JSON-to-stdout, ALL Off by Default and Config-Gated; Overhead SPIKE-Measured; Degrades to Zero Overhead at the Standalone Container

**Status**: **Proposed (overhead SPIKE-measured before defaulting; metrics library leaning OTel, confirmed by the slice-05 SPIKE)** (2026-06-16 — Morgan, Solution Architect; interaction mode PROPOSE. Inherits System Decision 6 / A5.)
**Date**: 2026-06-16
**Feature**: epic-5305-k8s-readiness (ADO Epic #5305)
**Decider**: Morgan (Solution Architect), confirming the system-designer's Decision 6; metrics-library pick deferred to the slice-05 SPIKE
**Relationship to prior ADRs**: AMENDS ADR-027 (single-instance default stands; this adds config-gated, off-by-default observability). EXTENDS the existing Serilog pipeline (`ConfigureLogging`, `Program.cs:977-999`). Honours D1 (standalone byte-identical, zero overhead).

---

## Context

Lighthouse logs via **Serilog**, fully configured from appsettings (`ConfigureLogging`, `Program.cs:977-999`) — Console + File sinks, an `ExpressionTemplate`, a dynamic level switch. There is **no OpenTelemetry, no metrics, no `/metrics` endpoint** (grep: zero). In a cluster, Lighthouse is therefore a black box: no Prometheus target, unstructured text logs that Loki cannot query cleanly, no traces (US-05).

Lighthouse is self-hosted with **no central telemetry** (memory: self-hosted-telemetry-gap) — so all observability is *operator-facing* (the operator scrapes/queries their own stack), never phoned home. The D1 gate requires the standalone single-container product to be byte-identical with **zero added overhead** when telemetry is off.

`/metrics` can leak request paths (a security-hotspot surface), so exposure must be a conscious operator decision, not an always-on default.

## Decision

**Add OpenTelemetry .NET (`OpenTelemetry.Extensions.Hosting` + ASP.NET Core instrumentation + a Prometheus exporter for `/metrics` + OTLP for traces), plus a Serilog JSON-to-stdout sink — ALL off by default, config-gated, with overhead SPIKE-measured before the "on" posture is blessed.**

- **Metrics + traces (CREATE, justified — no existing seam)**: OpenTelemetry gives **one instrumentation surface for both metrics and traces**, vendor-neutral OTLP, future-proof (A5). `GET /metrics` returns Prometheus-format HTTP server metrics (request count / error rate / latency histograms — US-05 AC1). Traces export via OTLP. **Wired only when `Telemetry:Enabled` (or the presence of `Telemetry:*` exporter config) is set**; absent, no OTel services are registered, no exporter runs, and `/metrics` is **not mapped**.
- **Structured logs (EXTEND the Serilog pipeline)**: add a **JSON stdout sink** so logs ship as queryable JSON to Loki (US-05 AC2). The Serilog pipeline, `ExpressionTemplate`, and dynamic level switch are reused; the JSON sink is config-selected (e.g. `Serilog:WriteTo` JSON-stdout entry, or a `Logging:Format: json` gate) and the existing Console + File sinks remain the default for the standalone product.
- **Config idiom**: `Telemetry:*` follows the established `Configure<T>(...)` / `builder.Configuration[...]` convention; `__` bridges colons for env vars.

```
if (telemetryConfig.Enabled)
{
    builder.Services.AddOpenTelemetry()
        .WithMetrics(m => m.AddAspNetCoreInstrumentation().AddPrometheusExporter())
        .WithTracing(t => t.AddAspNetCoreInstrumentation().AddOtlpExporter(/* endpoint from config */));
}
// ... and only then, in ConfigureApp:
if (telemetryConfig.Enabled) { app.MapPrometheusScrapingEndpoint("/metrics"); }
```

**Metrics-library pick (SPIKE-confirmed)**: the slice-05 SPIKE measures OTel overhead. If OTel's overhead is unacceptable for the *off-by-default-but-occasionally-on* posture, the documented fallback is `prometheus-net` for metrics-only — lighter for just `/metrics`, but a *second* mechanism for traces. OTel is the lean default precisely to avoid two telemetry stacks; the SPIKE confirms it.

## Security (cross-cutting, decided here)

`/metrics` can leak request paths → **default cluster-internal / unauthenticated, but exposure is a conscious config call** (the DISCUSS cross-cutting checklist flagged this as a Sonar security-hotspot to resolve in DESIGN). The endpoint is **OFF unless telemetry is enabled** and is documented as "expose only on a trusted network / behind the metrics scrape network policy." The network policy itself is a Productization #5306 concern; in-app, the conscious gate (off by default, mapped only when enabled) is the control.

## Standalone Degradation (D1 / US-05 AC3)

Telemetry disabled by default ⇒ **no OTel services registered, no exporter runs, `/metrics` is not mapped, Serilog stays on its current Console + File sinks** ⇒ **zero behaviour or performance change** for the single container. The standalone product ships exactly ADR-027's logging posture; observability is purely additive and config-selected.

## Alternatives Considered

**Chosen: OpenTelemetry (metrics + traces) + Serilog JSON stdout, off-by-default, config-gated.**
- Pros: one instrumentation surface for metrics + traces; vendor-neutral OTLP (no backend lock-in); reuses the existing Serilog pipeline for JSON logs; zero overhead when off (D1).
- Cons: heavier setup than metrics-only; overhead must be SPIKE-measured to justify the off-by-default posture and confirm "on" is acceptable.

**Rejected (held as fallback): `prometheus-net` for metrics only.** Lighter for just `/metrics`, but a *second* mechanism for traces — rejected to avoid two telemetry stacks. Reconsidered only if the slice-05 SPIKE shows OTel overhead is unacceptable.

**Rejected: a managed APM SDK (Application Insights / Datadog agent).** Couples the self-hostable product to a vendor backend; contradicts the vendor-neutral, self-hosted posture and the no-phone-home constraint. Forbidden as proprietary without an explicit requirement.

## Consequences

**Positive**:
- Lighthouse appears as a first-class Prometheus target with JSON logs and OTLP traces (US-05 AC1/AC2) when the operator opts in.
- Zero overhead for the standalone product (D1 / US-05 AC3); observability is purely additive.
- Vendor-neutral — the operator points OTLP/Prometheus at whatever backend they run.

**Negative**:
- OTel overhead must be SPIKE-measured before "on" is blessed; the metrics-library pick is provisional until the SPIKE reports.
- `/metrics` is a security-hotspot surface — mitigated by off-by-default + a documented trusted-network exposure rule (network policy is #5306).

**Neutral**:
- Metrics + traces are net-new (the only genuinely-new subsystem in the epic), justified by US-05 with no existing seam to extend; logging is an EXTEND of the existing Serilog pipeline.

## Architectural Enforcement

| Rule | Mechanism |
|---|---|
| `/metrics` returns Prometheus-format HTTP server metrics when telemetry enabled (US-05 AC1) | Integration test with `Telemetry:Enabled` → scrape `/metrics`, assert Prometheus exposition format + HTTP server metric names |
| Logs are structured JSON to stdout with expected fields (US-05 AC2) | Integration test with JSON sink enabled → assert stdout lines parse as JSON with the expected fields |
| Telemetry off ⇒ no exporter, `/metrics` unmapped, no behaviour/perf change (US-05 AC3) | Integration test with telemetry disabled → `GET /metrics` is 404; existing Console+File logging unchanged; SPIKE measures overhead delta ≈ 0 |
| `/metrics` exposure is a conscious off-by-default call (security-hotspot resolved) | Endpoint mapped only when enabled; documented trusted-network rule; Sonar hotspot reviewed |
| No managed APM SDK introduced | Grep/dependency check: no Application Insights / vendor-agent package |
