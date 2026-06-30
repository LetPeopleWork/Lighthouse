# ADR-095: Migration-Before-API Ordering Is an Emergent Property of On-Boot `Database.Migrate()` (Advisory-Lock Coordinated) + Readiness-Gated Rolling Update + the Expand-Only Guard — No Dedicated Pre-Upgrade Migration Job / Sync-Wave Is Warranted

**Status**: **PROPOSED** (2026-06-30, Titan — PROPOSE mode, awaiting Benjamin)
**Date**: 2026-06-30
**Feature**: epic-5306-productization-platform (ADO Epic #5306, story #5205 RESCOPE slice-08b, US-08b-1) — resolves open question **O-08-2**
**Decider**: Benjamin (product owner) + Titan (System Designer)
**Relationship to prior work**: REUSES epic-5305 wholesale — on-boot `Database.Migrate()`, the Postgres advisory-lock migration coordination + expand-only CI guard (ADR-077, `ExpandOnlyMigrationGuard.cs`), readiness probes (#5310), graceful drain (#5309). Composes with ADR-093 (the expand-only pre-flight) and ADR-083 (image tag = `Chart.appVersion`).

---

## Context

US-08b-1 requires that during a tenant upgrade the **schema migration is applied before the new API version serves traffic** — no window where a new API runs against an un-migrated schema. The question (O-08-2) is whether the existing on-boot `Database.Migrate()` + expand-only guarantee + rolling update already deliver this, or whether a dedicated **pre-upgrade migration Job** (an earlier ArgoCD sync-wave, or a Helm pre-upgrade hook) is needed.

## Reasoning

Walk the rolling update of one tenant from version `vN` to `vN+1`:

1. The old pod (`vN`, old schema) keeps serving — it is `Ready` and behind the Service.
2. The new pod (`vN+1`) starts. **On boot it runs `Database.Migrate()`** inside its startup path, coordinated by the Postgres **advisory lock** (ADR-077) so only one pod migrates and others wait — this is in place for `replicaCount>1`.
3. The migration is **expand-only** (additive columns/tables/indexes only) — guaranteed because `ExpandOnlyMigrationGuard` fails `dotnet test` (the release build) on any post-baseline Drop/Rename, so a destructive migration **never reaches a tenant** (ADR-093). Additive changes leave the old pod's queries valid: `vN` keeps serving correctly against the expanded schema.
4. Only **after** the migration completes does the new pod pass its **readiness probe** and join the Service. The readiness gate is what enforces ordering: traffic reaches `vN+1` *only after* its boot (including `Migrate()`) finished.
5. The old pod is drained gracefully (#5309) and removed.

At no point does a new API serve against an un-migrated schema: the new pod migrates **before** it is `Ready`, and the old pod tolerates the migrated (additive) schema **because** it is expand-only. The single case where strict pre-ordering would matter — a destructive migration — is structurally impossible here (blocked in CI before any roll).

A dedicated pre-upgrade Job / sync-wave would be **strictly worse**: it adds a second DB-access surface and a new failure mode, and it *breaks* the zero-downtime property — a pre-upgrade hook Job gates the Deployment, creating a window where the Job runs while only old pods exist, and a Job failure strands the sync. It buys nothing the readiness-gated, advisory-lock-coordinated, expand-only path does not already give.

## Decision

**Do NOT add a pre-upgrade migration Job or a migration-specific ArgoCD sync-wave.** Migration-before-API ordering is an **emergent, proven property** of the composed epic-5305 primitives:

> readiness-gated rolling update (traffic only after boot) **+** on-boot advisory-lock-coordinated `Database.Migrate()` (migration during boot, once) **+** expand-only guard (old pods tolerate the new schema; destructive changes blocked pre-flight).

The observable assertion (US-08b-1 AC: "continuous successful responses through the roll") is the property's proof and is covered by the slice-08b smoke-test and the DELIVER live-poll-through-the-roll.

## Consequences

- **Positive**: zero new components (Critical Rule 1 — no "just in case" infrastructure); zero new failure modes; keeps the zero-downtime rolling update intact; the ordering guarantee is reducible to primitives already proven LIVE in epic-5305/slice-08.
- **Negative / cost**: the guarantee is *emergent*, not a single explicit gate, so it must be **documented and probed** (the smoke-test's continuous-200-through-the-roll assertion) rather than read off a manifest; it is contingent on the expand-only guard staying in the release build (already true, ADR-077/093).
- **Standalone gate**: untouched — this is a reasoning/ADR decision; no chart or workflow change.

### Earned-Trust note

The ordering is not asserted by faith — it is *demonstrated* every roll by the post-sync smoke-test (ADR-096) polling the tenant for continuous successful responses and the expected served version. The expand-only guard is itself the probe that the migration substrate cannot present a destructive change to the ordering path.

## Alternatives considered

1. **Pre-upgrade migration Job as an earlier ArgoCD sync-wave** — rejected (breaks zero-downtime, adds a failure mode, redundant with readiness gating; see Reasoning).
2. **Helm pre-upgrade hook running `Database.Migrate()` standalone** — rejected: would require the chart (public, byte-unchanged) to grow a hook; duplicates the app's own boot migration; same zero-downtime regression.
3. **Status quo + explicit probe (CHOSEN)** — the composed primitives plus the smoke-test as the empirical check.

## Open questions for DELIVER

- Confirm the chart's health/info endpoint exposes the **served version** so the smoke-test can assert `vN+1` is actually serving before judging health (feeds ADR-096).
