# DEVOPS Decisions — epic-5306-k8s-productization

> Operationalizes the DESIGN (chart CI/CD + publish). All Decisions 1–9 user-confirmed as derived from the existing setup + DESIGN. Date: 2026-06-21. Detail in `feature-delta.md` → `## Wave: DEVOPS / [REF] …` and `environments.yaml`.

## Key Decisions

- [D1] **Deploy target**: operator's own conformant Kubernetes; the *artifact* publishes to GitHub Pages (`docs/charts/`, ADR-083).
- [D2] **Orchestration**: Kubernetes (the chart's purpose).
- [D3] **CI/CD**: GitHub Actions, **extending existing workflows** (CI-consolidation; ADR-083).
- [D4] **Existing infra**: yes, both — existing GH Actions + the artifact-based Pages deploy are extended, not replaced.
- [D5] **Observability**: none new — the chart exposes epic-5305's vendor-neutral OTel/`/metrics`/Serilog-JSON as off-by-default values; no chart-owned telemetry stack.
- [D6] **Deployment strategy**: Rolling update, made safe by epic-5305 probes + graceful-shutdown + expand-only migrations; rollback = `helm rollback` (no schema rollback needed — additive migrations). No blue-green/canary (Band D).
- [D7] **Continuous learning**: no (self-hosted, no central telemetry).
- [D8] **Branching**: trunk-based (push to `main`), chart CI on the same pushes.
- [D9] **Mutation testing**: per-feature (project default) — **N/A this feature** (Helm/YAML, no mutatable C#/TS); test-quality surface = `ct lint` + `helm template` + schema validation + standalone-gate render guard + kind install smoke-test. No CLAUDE.md change.
- [D-CI] **CI depth (user call)**: lint + template + **kind install-test** on every chart change (real `helm install` smoke into an ephemeral cluster), not lint-only.

## Infrastructure Summary

- **Deployment**: operator Kubernetes, rolling update; chart artifact on GitHub Pages (`docs/charts/`).
- **CI/CD**: GitHub Actions (extended); trunk-based; stages = chart-lint → standalone-gate render guard → kind chart-install-test → config-ref drift gate → (release stage) package + no-overwrite/version guard + publish.
- **Observability**: epic-5305 telemetry exposed as off-by-default chart values; none added.
- **Mutation testing**: per-feature (N/A this Helm feature).

## Constraints Established

- One Pages source per repo → the Helm repo coexists under `docs/charts/` in the existing artifact deploy (no `gh-pages`, no chart-releaser).
- Extend existing workflows, never add a parallel one.
- CI must verify the standalone gate (default values → embedded, one API workload) and that the chart actually installs (kind smoke).
- Publish must refuse silent overwrite and assert Chart.yaml == index == appVersion == image.tag.
- Vendor-neutral (official images only); Redis operator-provided.

## Upstream Changes

None. DEVOPS introduced no change to DESIGN/DISCUSS assumptions (the publish mechanism was already locked in ADR-083). No `devops/upstream-changes.md`.
