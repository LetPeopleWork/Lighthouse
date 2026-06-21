# DESIGN Decisions — epic-5306-k8s-productization

> Scope: stories #5199 (public Helm chart) + #5200 (enterprise docs). System/infrastructure architect (PROPOSE). Date: 2026-06-21. Full detail in `docs/product/architecture/brief.md` → `## System Architecture — epic-5306-k8s-productization` and ADR-080..085.

## Key Decisions

- [D1] **Postgres-only chart, no SQLite**: bundled in-chart Postgres StatefulSet (official `postgres` image, `postgresql.enabled` default on) OR BYO `externalDatabase.*`. Bitnami subchart rejected (Broadcom 2025 catalog restriction = public-chart supply risk). (ADR-080)
- [D2] **`frontend.mode: embedded` is the default and the scalable shape** — horizontal scale = `replicaCount: N` (+ #5304 Redis backplane at N>1); the frontend split gives no API-scaling benefit and is a SaaS-multi-tenant optimisation. `split` = loud `fail` stub, deferred to Band D. (ADR-081)
- [D3] **Fail-fast required values**: `values.schema.json` (structure/types/enums/unconditional-required) + `{{ required }}` (conditional, e.g. DB password); explicit DB password, no auto-gen. No partial release on a missing key. (ADR-082)
- [D4] **Publish via the docs tree on the existing Pages**: `docs/charts/` served by the current artifact-based `pages.yml`, packaged in the existing release stage; **no `gh-pages` branch, no chart-releaser** (GitHub Pages allows one source per repo; the existing docs deploy already owns it). Chart.yaml `version` single source; no-silent-overwrite version guard. (ADR-083)
- [D5] **helm-docs single-source config reference**: config table generated from `values.yaml` comments; CI `git diff` drift gate → 0 phantom keys by construction. Narrative docs (diagram/quick-start/walkthrough) hand-authored. (ADR-084)
- [D6] **MCP server is an optional `mcp.enabled` workload**, orthogonal to `frontend.mode`; inbound-auth per ADR-079 (X-Api-Key pass-through or IdP JWT Bearer); not behind oauth2-proxy. (ADR-085)
- [D7] **Single chart, no third-party subchart dependency**, one-chart-config-selected-branches (no fork) — mirrors ADR-027/epic-5305.

## Architecture Summary

- **Pattern**: single Helm chart (`apiVersion: v2`) rendering Kubernetes workloads, parameterised by `values.yaml` + `values.schema.json`; config-selected branches, no fork; default values = the simple shape (embedded, bundled Postgres, MCP off).
- **Paradigm**: N/A (no application code; OOP/.NET project unchanged) — this is YAML/Helm packaging + docs.
- **Key components**: API Deployment+Service, Ingress, bundled Postgres StatefulSet (or external), optional MCP Deployment+Service, ConfigMap/Secret, NOTES.txt, `values.schema.json`.

## Reuse Analysis

| Existing Component | File | Overlap | Decision | Justification |
|-------------------|------|---------|----------|---------------|
| epic-5305 runtime capabilities | `Lighthouse.Backend/...` (probes/headers/drain/migration-lock/backplane/telemetry/MCP-auth) | the chart needs all of these | REUSE (config surface) | all shipped + config-gated; chart sets values, changes no code |
| Pages deploy | `.github/workflows/pages.yml` | publishing static artifacts | EXTEND | Helm index under `docs/charts/` ships through the existing `docs/**` trigger; no new Pages source/workflow |
| Release workflow | `.github/workflows/*` (release stage) | versioned publish | EXTEND | add `helm package`+`helm repo index --merge`+no-overwrite guard step (CI-consolidation rule) |
| per-feature docs/screenshot discipline | `docs/` (CLAUDE.md DELIVER) | narrative enterprise docs | REUSE | author via existing discipline; only the config table is generated |
| `chart/` templates + bundled Postgres + helm-docs config gen | (new) | — | CREATE (justified) | no chart/bundled-DB/values↔docs-single-source exists; standard Helm/tooling, no bespoke mechanism |

Zero unjustified CREATE NEW.

## Technology Stack

- Helm 3.x · Chart `apiVersion: v2` · `values.schema.json` (JSON Schema): the chart + its validation contract.
- Official `postgres` image StatefulSet (bundled DB) — no Bitnami, no subchart dependency: vendor-neutral, owned, auditable.
- `helm-docs`: config-reference generation + drift gate.
- `chart-testing` (`ct`): lint + template render in CI.
- GitHub Pages (existing artifact-based deploy, `docs/charts/`): the public Helm repo.

## Constraints Established

- The chart is **additive**; it changes nothing in epic-5305 and nothing in the standalone image (the standalone image keeps SQLite, byte-unchanged).
- **Standalone gate**: default values render the simple shape (embedded frontend = one app workload serving the SPA); a render guard test asserts this.
- **Vendor-neutral**: official images only; no Bitnami; no cloud-service lock-in; substrate/DB/identity (Q1/Q2/Q3) stay the operator's values; Redis operator-provided.
- **One Pages source per repo** — the Helm repo must coexist with the existing docs deploy under `docs/charts/` (no `gh-pages`).
- **Chart.yaml `version` is the single source of truth**; `appVersion == values.image.tag` asserted; no silent overwrite on publish.

## Upstream Changes

See `design/upstream-changes.md`. Summary: slice-01 walking skeleton now brings up API + bundled Postgres (chart is Postgres-only, ADR-080); publish mechanism refined to docs-tree/Pages (ADR-083); "single-container shape" clarified to mean embedded frontend, not "no DB workload". No story/AC requires rewriting; the slice-01 in/out-of-scope wording needs a product-owner touch.
