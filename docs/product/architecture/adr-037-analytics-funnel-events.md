# ADR-037: Analytics funnel events behind a swappable sink + UTM/source CTA tagging

> **Scope: WEBSITE repo (`/storage/repos/website`), NOT the Lighthouse product.** Authored for ADO Epic #5123.

## Status

Accepted (DESIGN wave, 2026-05-30)

## Context

The 8 outcome KPIs are funnel-leading (D1). They require funnel events: `assessment-start`, `teaser-view`, `email-capture`, `cta-click` (with band + destination), and a `breakdown-dwell`/scroll-depth signal (KPI 8 value-proxy). The measurement plan sources KPIs 1/2/4 from "website analytics + Supabase rows", 3/5/8 from "website analytics (events) + Supabase". KPIs 6/7 (Community signup, consulting booking) rely on **manual CRM attribution** because self-hosted Lighthouse instances do not phone home (MEMORY: Epic 5015 telemetry gap) — so those depend on UTM/source tagging on the outbound CTAs, not in-product telemetry.

This DESIGN must (a) name the event sink and (b) keep it swappable; and (c) fix a UTM/source convention for CTAs.

## Decision

- **An `AnalyticsSink` driven port** (ADR-035) — `track(event: AssessmentEvent): void` — with the concrete adapter chosen at the composition root. v1 emits to **whatever website analytics is already present** (detected at DELIVER; if none is wired, the adapter is a no-op/console stub and the funnel is reconstructed from Supabase `responses` rows + the `leads` table, which already timestamp every completion and capture). Because the events are behind a port, swapping in a real analytics provider (Plausible/GA/PostHog) later is one adapter change, no call-site edits.
- **Supabase rows are the durable funnel backbone**: `responses.created_at` (completion), `leads.created_at` (email-capture) already give completion and capture counts and band distribution without any analytics vendor. Analytics events add the *teaser-view*, *cta-click*, and *dwell* signals that have no row. This dual sourcing matches the measurement plan and means KPIs 1/3/4 are computable from Supabase alone even if analytics is absent.
- **CTA UTM/source convention** (for KPIs 6/7 manual attribution): every outbound CTA href carries `?utm_source=assessment&utm_medium=readiness&utm_campaign=forecasting-readiness&band=<band-slug>`. The Community signup and consulting links thus arrive tagged, so manual CRM reconciliation can attribute a signup/booking to the assessment and to the band. The `band` param lets KPI 5's per-band segmentation (≥30% Flow-aware) be reconstructed from destination analytics even without an event.
- **`cta-click` events** also carry `{ band, destination }` so per-band click-through (KPI 5) is measurable client-side independent of the destination's analytics.

## Alternatives Considered

- **Direct calls to an analytics global in components**: rejected — couples every surface to a specific vendor and makes the no-vendor-yet reality (and the Supabase-only fallback) impossible to model cleanly; the port keeps call sites vendor-agnostic and testable (assert events emitted via a fake sink).
- **Persist funnel events to a Supabase `events` table**: rejected for v1 — `responses`/`leads` rows already carry the durable completion/capture timestamps; a generic events table adds write volume and a schema for signals that a real analytics tool models better. Revisit if a vendor is declined long-term and richer funnel analysis is needed.
- **Rely solely on in-product Lighthouse telemetry for KPIs 6/7**: impossible — the Epic 5015 telemetry gap means self-hosted instances do not report; manual CRM attribution via UTM tags is the only v1 path, hence the tagging convention.

## Consequences

- **Positive**: funnel measurable from Supabase rows even with no analytics vendor; event sink swappable behind a port; CTA UTM tagging makes manual KPI 6/7 attribution possible and band-segmentable.
- **Negative**: KPIs 6/7 remain manual/lagging in v1 (inherent to the telemetry gap, not a design deficiency); the precise analytics vendor is a DELIVER detail (flagged) — the port makes that deferral safe.
- **External integration note**: if a third-party analytics provider is selected at DELIVER, it is an external integration — but a fire-and-forget client beacon, not a contract the app depends on for correctness, so no consumer-driven contract test is warranted; degrade-silent (a failed `track` never affects the visitor).
