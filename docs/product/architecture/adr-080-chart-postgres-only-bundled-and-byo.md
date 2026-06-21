# ADR-080: The Public Helm Chart Is Postgres-Only — a Bundled In-Chart Postgres StatefulSet (official image, gated `postgresql.enabled`) OR Bring-Your-Own (`externalDatabase.*`); No SQLite in the Chart; Bitnami Rejected

**Status**: **Accepted** (2026-06-21 — accepted by Benjamin)
**Date**: 2026-06-21
**Feature**: epic-5306-k8s-productization (ADO Epic #5306, story #5199) — the publishable public Helm chart
**Decider**: Benjamin (product owner) + System Designer (PROPOSE)
**Relationship to prior work**: The chart consumes the epic-5305 `DatabaseConfigurator` provider-switch (Postgres path already shipped and supported). This ADR fixes the chart's **database posture**; it does not change any backend code.

---

## Context

The standalone/server product image defaults to **SQLite** and that is sacrosanct (planning-stage D4 — the standalone image is byte-unchanged). The question here is the **chart's** database posture, which is a *separate* deployment target. Two facts drive it:

1. Kubernetes is chosen for availability/rollout-safety and (optionally) horizontal scale. SQLite on a single `ReadWriteOnce` PVC is a hostile fit: it cannot be shared across replicas, complicates rolling updates, and is not the production database the hosted SaaS and serious self-hosters will run. The product already ships full Postgres support (`Lighthouse.Migrations.Postgres`, `pg_dump` in the image).
2. The chart must be a **public, vendor-neutral** artifact. The historical default for a bundled DB subchart — **Bitnami `postgresql`** — became a supply/licensing risk in August 2025 when Broadcom/VMware moved the free Bitnami catalog to a frozen `bitnamilegacy` namespace and put current tags behind a paid "Bitnami Secure Images" subscription. Depending on it would couple a public OSS chart to a restricted catalog.

## Decision

**The chart is Postgres-only. It ships no SQLite path. It offers exactly two database modes, selected by values:**

- **Bundled (default for one-command UX): an in-chart Postgres `StatefulSet`.** The chart templates a minimal `StatefulSet` + headless `Service` + `PersistentVolumeClaim` + `Secret` using the **official `postgres` image** (vendor-neutral), gated `postgresql.enabled` (default **on**). No third-party subchart dependency — the templates are ours, fully auditable, ~4 small files. Not HA; it is the convenience/small-self-host database.
- **Bring-your-own (production): `externalDatabase.*`.** Set `postgresql.enabled: false` and point `externalDatabase.host/port/database/user/password` at a managed or operator-run Postgres (CNPG, RDS, Azure Database, etc.). The chart renders only the connection wiring; it provisions no DB.

The chart's `ConnectionStrings:Default` / `Database:Provider=Postgres` is always rendered (from bundled or external values). **`Database:Provider=SQLite` is never produced by the chart.**

**Bitnami `postgresql` subchart is rejected** (supply/licensing risk for a public chart). **CloudNativePG as a bundled dependency is rejected** for the bundled path (it is a cluster-scoped operator + CRDs → breaks the single `helm install`) but is the **recommended documented target for the BYO path** in production.

## Consequences

- **Positive**: one `helm install` brings up a working, production-shaped stack (API + Postgres) with no external DB prerequisite; production operators get a clean BYO seam to a managed/HA Postgres; zero third-party subchart supply risk; the chart stays vendor-neutral (official image only).
- **Negative / cost**: we own and maintain ~4 DB templates (StatefulSet/Service/PVC/Secret); the bundled DB is single-replica (no HA) — acceptable because serious operators use BYO. The bundled-DB password is **required explicitly** (ADR-082) — no auto-generated secret, so first install needs a password set.
- **Standalone gate (D4)**: untouched. This ADR governs only the chart; the standalone image keeps SQLite, byte-identical.
- **Back-propagation**: the DISCUSS walking skeleton (slice-01) implied an API-only first cut with Postgres deferred to slice-03. Because the chart is Postgres-only, **slice-01 now brings up API + bundled Postgres** (still one command). Recorded in `design/upstream-changes.md`.

## Alternatives considered

1. **Mirror the standalone SQLite default in the chart.** Rejected — SQLite cannot back a multi-replica/rolling-update k8s deployment and is not the production DB; it would make the chart's default a dead end.
2. **Bitnami `postgresql` subchart.** Rejected — Broadcom 2025 catalog restriction = public-chart supply/licensing risk.
3. **CloudNativePG operator as the bundled DB.** Rejected as *bundled* (operator + CRDs break one-command install); **adopted as the documented production BYO target**.
4. **BYO-only, no bundled DB.** Rejected — defeats the "whole stack with one command" walking skeleton (slice-01/03); an operator would need a Postgres standing before the first install.
