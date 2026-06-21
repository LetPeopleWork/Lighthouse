# DISTILL Decisions — epic-5306-k8s-productization

> Acceptance scenarios for the chart + docs. Date: 2026-06-21. `.feature` SSOT at `chart/tests/acceptance/`. Reconciliation gate: passed — 0 contradictions.

## Key Decisions

- [D1] **Tier A only** — config-shaped feature (Helm chart install/schema/one-shot); Tier B (state-machine PBT) skipped per Mandate 10 (no domain-rich chained journey).
- [D2] **Example-only, no PBT** — Helm template/install is layer-3 (real adapter/subprocess); sad paths enumerated, not generated.
- [D3] **Mechanism**: render-layer via **helm-unittest** (`chart/tests/unit/*_test.yaml`), WS/integration via **kind** `helm install` (DEVOPS install-test job), publish guards via release-stage shell, drift via helm-docs `git diff`. No pytest (no app code in this feature).
- [D4] **Structural RED** — `chart/` doesn't exist yet; `.feature` + helm-unittest specs are RED with reason `MISSING_FUNCTIONALITY` until DELIVER slice-01 creates the chart. No `src/` scaffold stubs (none apply).
- [D5] **`@requires_external`** for OIDC login + live MCP call — run in per-slice dogfood, skipped in CI (self-hosted chart; no IdP/MCP in the CI render/kind layer).

## Port Treatment (Architecture of Reference)

| Port | Class | Treatment |
|---|---|---|
| helm install / template | Driving | real (helm template render + kind install) |
| Postgres bundled | Driven internal | real (kind) |
| Postgres external (BYO) | Driven internal | render-only in CI; real = `@requires_external` |
| OIDC, Redis, MCP image | Driven external | OIDC/MCP `@requires_external`; Redis real in multi-replica kind scenario |
| publish (index + guards) | Driving (CI) | real (release-stage shell) |

## Scenario Coverage

19 scenarios across walking-skeleton / install-and-configure / publish-and-docs; 1 `@walking_skeleton`; ~21% error/guard scenarios (split-fail, missing-value, overwrite-refuse, drift). Every driving + driven adapter covered (table in feature-delta DISTILL sections).

## Constraints Established

- `.feature` files are the acceptance SSOT; DELIVER unskips/authors the executable helm-unittest specs + kind job, does not re-author the scenarios.
- helm-unittest is a new DELIVER-slice-01 tooling dependency (render-test harness).
- Standalone-gate is acceptance-tested: default values → embedded + one API workload (render assertion).

## Upstream Changes

None. No `distill/upstream-issues.md` — scenarios trace cleanly to US-01/US-02 + slice ACs.

## Pending: Final Wave Review Gate

The mandatory consolidated 4-reviewer gate (Eclipse/DISCUSS + Architect/DESIGN + Forge/DEVOPS + Sentinel/DISTILL, parallel, Haiku) is NOT yet run — held for user go-ahead (4-agent fan-out; subagents flaky this session). Must be APPROVED/CONDITIONALLY_APPROVED before DELIVER handoff.

## Back-propagation — slice-05 (DELIVER, 2026-06-21)

Reconciled the architecture-diagram AC with the shipped chart. The DISTILL/DISCUSS-era scenario said
"Ingress to oauth2-proxy to API", but slice-03 shipped **in-app OIDC** (`oidc.*` → `Authentication:*`)
and ADR-085/D6 explicitly states MCP is *not behind oauth2-proxy* — the chart deploys no oauth2-proxy
at all. Drawing it would document a component that does not exist (a phantom, the very thing the
config-ref drift gate forbids). User decision (slice-05 review): **honest-to-chart**. Updated
`publish-and-docs.feature`, `feature-delta.md` (elevator/UAT/AC), and `slice-05-*.md` to the real
topology: **Ingress → API (in-app OIDC vs external IdP) + optional MCP + Postgres (+ Redis when scaled)**.
D6/ADR-085 unchanged (already correct).
