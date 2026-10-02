# Wave Decisions — DEVOPS — epic-5510-5881-refinement

**Agent**: Apex (`nw-platform-architect`) · **Date**: 2026-10-02 · **Mode**: autonomous subagent, documents only.
Full reasoning: `feature-delta.md` → `## Wave: DEVOPS / …`. Machine artifact: `environments.yaml`.

## The nine decisions (brownfield — read, not asked)

| # | Decision | Answer |
|---|---|---|
| 1 | Deployment target | Existing artifacts: standalone packages / Tauri desktop, Docker image, Helm chart. Hosted platform torn down — nothing to deploy there now |
| 2 | Orchestration | Unchanged; no chart change |
| 3 | CI/CD | Existing GitHub Actions; no workflow added or edited |
| 4 | Existing infrastructure | All reused; no new component |
| 5 | Observability | Structured logging + *Recent problems* sink + opt-in usage data (browser detects, backend forwards, PostHog EU) |
| 6 | Deployment strategy | Calver release; startup migrations; Recreate on hosted; rollback = previous release, migrations left in place |
| 7 | Continuous learning | The usage-data catalogue; exposure is per Team by construction (tab disabled until configured) |
| 8 | Branching | Trunk-based on `main` |
| 9 | Mutation testing | `per-feature`, ≥ 80% (unchanged); once per Epic at finalize, acceptance suite excluded |

## Decisions taken in this wave

- **DVO-1** Usage events (append in delivery order; the name is the contract, the integer is whatever is next when the
  slice lands): `TeamRefinementConfigured` 12 (slice 01, name-only) · `TeamSizingVoteCast` 13 (11, `sizingMoment`) ·
  `TeamSizingReadinessReached` 14 (13, `sizingMoment`) · `TeamRefinementDayVerdictShown` 15 (05, `refinementVerdict`,
  Refinement days only) · `TeamRefinementPresented` 16 (18, name-only). Route key `TeamDetail_Refinement` = 10
  (`/teams/:id/refinement`) on the existing `TeamTabOpened`, slice 02.
- **DVO-2** Two closed enums only: `UsageDataSizingMoment` {NoCadence, OnRefinementDay, OnOtherDay, InLiveSession} and
  `UsageDataRefinementVerdict` {Below, In, Above, None}. One moment enum replaces DISCUSS's two dimensions (live is never
  async).
- **DVO-3** KPIs phrased per Team or instance are measured per browser (K1, K2, K3, K6, K7 proxies) — the pipe carries
  no Team or instance identity, by design. K4/K5 count browser-cast votes only; CLI/MCP actions are never reported.
- **DVO-4** M1/M2 expand-only via `Create-Migration.ps1`; `HistoricalSchemaPatch` gains `Teams.RefinementSettings`;
  M2's cascade / SET NULL asserted as database constraints; rollback rehearsal against the previous release once per
  migration.
- **DVO-5** Rate limit `RefinementContribution` 30 / 60 s, partitioned by presented handle (voter key / API key /
  bearer / cookie, hashed) else address, plus the existing address ceiling widened to this policy; configured in
  `appsettings.json` and tested as configured (an unconfigured policy is silently unlimited).
- **DVO-6** Logging: `vote-needs-a-person` at Warning; `voter-name-required`, `voter-key-required`,
  `work-item-not-in-refinement` at Information; appends at Debug. Fields `Reason`, `TeamId`, `Channel` (+ `Kind`).
  Never the name, voter key, subject, comment or answer.
- **DVO-7** E2E: two walking skeletons (E1 list + disabled tab; E3 vote → Ready), run by `ci_verifysqlite` and
  `ci_verifypostgres`. No new `@auth` spec.
- **DVO-8** Lighthouse-Clients: minor for 09, minor for 17a+17b; manual `pnpm release:version` before the release run.

## Upstream change

One: ADR-217's partition "by subject" is unreachable (limiter runs before authentication). `devops/upstream-changes.md`.

## For the maintainer

- Accept the per-browser proxies for K1, K2, K3, K6, K7 (or drop those targets as unmeasurable) — DVO-3.
- Accept that K4/K5 exclude CLI/MCP votes.
- Confirm the rate-limit partition correction (DVO-5) over the alternative of moving `UseRateLimiter` after
  authentication, which changes every existing policy.

## Artefacts

| Path | What |
|---|---|
| `feature-delta.md` → `## Wave: DEVOPS / …` | environment matrix, CI/CD outline, monitoring contracts, deployment + rollback, rate limiting, observability, mutation, branching, coexistence, pre-requisites, handoff, changed assumptions |
| `environments.yaml` | environments, scenario axes per slice, coexistence, deployment assumptions |
| `devops/upstream-changes.md` | the ADR-217 partition correction |
| `docs/product/kpi-contracts.yaml` | `OUT-5510-*` entries appended |
