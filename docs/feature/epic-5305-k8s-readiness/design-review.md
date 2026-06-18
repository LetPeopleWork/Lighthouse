# DESIGN Review — epic-5305-k8s-readiness

Reviewer: nw-solution-architect-reviewer | Date: 2026-06-18 | Scope: full DESIGN (system + DDD + application layers, ADR-075..078, feature-delta APP section)

## Verdict: APPROVED — handoff to DISTILL ready

Zero blocking issues. Layer coherence, the D1 standalone gate, reuse-gate discipline, ADR quality, cross-cutting completeness, and DISTILL-readiness all verified against the artifacts and the actual backend code (`UpdateQueueService`, `UpdateNotificationHub`, `updateStatuses` singleton, `DatabaseConfigurator`, `ApiKeyAuthenticationHandler`, `UseForwardedHeaders`). ADR-076 (cluster-aware queue) is correctly left OPEN/SPIKE-gated (D5) with both options documented and neither pre-committed.

## Non-blocking nits

1. **ADR-076 Earned-Trust probe — contract vs implementation.** The section already states the probe is a DELIVER concern; made the contract-vs-implementation distinction explicit. **Resolved** (ADR-076 "Earned-Trust Probe").
2. **ADR-078 `/metrics` "cluster-internal".** Reframed "cluster-internal" as a deployment expectation (network policy = #5306), not a code-enforced boundary; the only in-app control is the off-by-default gate. **Resolved** (ADR-078 "Security").
3. **ADR-076 Option B timer-updater gating.** Whether the timer updaters need a leader-gate under Option B (N replicas × N timer threads vs the per-entity lock) is correctly a SPIKE concern (D5) — no DESIGN change. **Carried to the slice-07 SPIKE**: the SPIKE must measure connector call counts at N=3 and confirm the per-entity lock alone prevents N× external syncs.

## Notes carried to DISTILL / DELIVER

- The **slice-07 ADR-076 SPIKE** is the critical path: prototype both options against real Postgres + Redis at N=3 under concurrent timer + manual-refresh load; confirm INV-4 (single sync per entity) and INV-1 (no regressed `UpdateProgress`) hold before committing the queue shape.
- The Earned-Trust probes (ADR-075/076/077) must run at startup before the cluster-aware path serves; the probe contract is shared with ADR-077's migration lock.
- Standalone degradation (D1) is a mechanical property — acceptance tests should run the N=1 path and confirm it is byte-identical to today.
