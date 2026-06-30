# DESIGN Wave Decisions — slice-08 RESCOPE ADDENDUM (#5205 merge-only release)

> **FOLD-IN INSTRUCTION (orchestrator)**: append everything below the `---` into
> `design/wave-decisions.md` as a new top-level `## RESCOPE ADDENDUM — slice-08 (#5205 merge-only release)`
> section after the `## Open Questions` block. (In-place Edit was blocked this session by the lean-ctx shadow
> read-tracking conflict; this is the authoritative content.)

---

## RESCOPE ADDENDUM — slice-08 (#5205 merge-only release)

> **Wave**: DESIGN (rescope, brownfield). **Mode**: PROPOSE. **Date**: 2026-06-30. **Architect**: Titan.
> **Scope**: PRIVATE-repo GitOps + CI only; public #5199 chart byte-unchanged. Designed ON the LIVE slice-08
> substrate (matrix appset + per-record `chartVersion` override + fleet `promotedVersion`). **ADRs**: 094..097.
> Full detail: `feature-delta.md` → `## Wave: DESIGN / [REF] slice-08 RESCOPE`.

### Key Decisions (resolved O-08-*)

| # | Decision | Chosen option | ADR |
|---|----------|---------------|-----|
| O-08-1 | Tenant-Zero auto-canary mechanism (headline) | **Renovate auto-merges a TZ-scoped `chartVersion`-override PR** (everything in git); fleet `promotedVersion` PR never auto-merged. Mutable `latest` tag rejected (registry-mutable; ArgoCD reconciles git; breaks rollback clarity); argocd-image-updater rejected (new controller; wrong target — knob is chart version, not image tag) | 094 |
| O-08-2 | Migration-before-API ordering | **No pre-upgrade Job / sync-wave.** Emergent from readiness-gated rolling update + on-boot advisory-lock `Database.Migrate()` + expand-only guard. A Job would break zero-downtime and add a failure mode | 095 |
| O-08-3 | Smoke-test surface + alert channel | **Version-stamped per-tenant ArgoCD PostSync hook Job** asserts served version + health; on failure opens/updates a **GitHub issue** (user-locked) naming tenant + version. Token via ESO/OpenBao (shared `platform` ns recommended) | 096 |
| O-08-4 | Renovate hosting + watch scope + automerge | **Mend Renovate GitHub App** (user-locked). Watch the published `lighthouse` chart (lpw `chartVersion` + fleet `promotedVersion`) + tracked platform components. Automerge ONLY the TZ `chartVersion`; fleet + components no-automerge | 097 |
| O-08-5 | Rollback posture | **Operator-initiated `git revert`** (proven in substrate). Smoke-test is detect+alert only. Auto-rollback OUT (flagged future) | 096 |

### Reuse Analysis (rescope)

**Zero unjustified CREATE NEW.** REUSE: the matrix appset + `promotedVersion`/`chartVersion` knobs, the
`ExpandOnlyMigrationGuard` (epic-5305 ADR-077), on-boot `Database.Migrate()` + advisory lock, readiness-gated
rolling update + drain, ESO/OpenBao (ADR-087), the epic-5305 chart health/version probe. EXTEND: the
`tenant-runtime` overlay (+ PostSync smoke-test Job + GitHub-token ESO), `applicationset-runtime.yaml` (fold
in `promotedVersion` to version-stamp the smoke-test), `validate-tenants.yml` (+ `renovate-config-validator`),
the lpw record (re-add the `chartVersion` canary anchor). CREATE (justified): `renovate.json` (no prior config;
merge-only entry point) + the PostSync smoke-test Job (no prior post-upgrade health gate; thin standalone
alert since slice-09 Alertmanager is absent). The workload `applicationset.yaml` is unchanged.

### Scale note

Control-plane, not data-plane: ~tens of tenants, ~weekly releases; a handful of Jobs + ≤1 issue-API call per
roll; GitHub 5000/h limit never approached. Binding constraints are latency budgets (KPI-2/4/5) and
correctness/ordering — not throughput. No new scaling component justified.

### Earned-Trust (rescope probes)

`renovate.validate` (automerge gated on the `validate-tenants` required check — a malformed record cannot
auto-canary) · `upgrade.smoketest` (the PostSync Job demonstrates the expected version actually serves healthy
in the real cluster; emits a structured GitHub issue naming tenant+version+code when a Synced tenant lies) ·
the smoke-test is version-stamped so it **re-probes on every version bump** (self-application) · optional
`renovate-config-validator` keeps the watch-scope config honest after edits.
