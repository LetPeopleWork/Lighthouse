# DESIGN Wave Decisions — work-item-age-percentiles (Story #5257)

Architect: Morgan (Solution Architect) · Mode: PROPOSE (D8 subsequently overridden by user) · Date: 2026-06-09 · Density: lean (Tier-1 [REF])
Outcome Collision Check: **skipped, `nwave-ai` CLI absent** (same as multiple-cycle-times).
Per-wave review: **skipped** (consolidated review fires at DISTILL).

> **Correction pass (2026-06-09):** the user overrode the prior client-side recommendation for D8 — *"WIA percentiles should be calculated in the BACKEND, with an extension to the API (and thus also the client packages). We want to do as little production work in the frontend."* D8 is now **backend compute + new endpoint × 2 scopes + version-gated client wrappers**. ADR-065 rewritten (client-side demoted to rejected alternative); ADR-066 mechanism revised (two server-fetched arrays). This file reflects the corrected verdict.

## Verdict summary

| Fork / decision | Verdict | ADR |
|-----------------|---------|-----|
| **D8 — compute location** | **Server-side.** New `GET …/metrics/workItemAgePercentiles` (Team + Portfolio) returning `IEnumerable<PercentileValue>`, mirroring `cycleTimePercentiles`. Service method = existing in-progress selection → `WorkItemAge` → `BuildPercentiles`/`PercentileCalculator`. Reuses `PercentileValue` (no new DTO). Client-side derivation REJECTED (user directive: minimal FE production work; percentile uniformity). | ADR-065 |
| Aging-chart CT↔WIA mechanism | **Swap the line *source*** between two **server-fetched** `IPercentileValue[]` arrays (CT from `cycleTimePercentiles`, WIA from the new endpoint) — one derived `activePercentiles` feeds the existing single `ChartsReferenceLine` block; mutual exclusivity is structural. | ADR-066 |
| Lighthouse-Clients | **AFFECTED — version-gated wrappers.** A NEW endpoint × 2 scopes ⇒ CLI + MCP add `getWorkItemAgePercentiles` that pre-checks server version (opaque 404 on old server) and records a `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry pinned strictly-newer-than the last release. Separate repo. (Reverses the prior "unaffected".) | ADR-065 |
| RBAC | **N/A** — non-premium; new endpoints ride the existing class-level `[RbacGuard(TeamRead/PortfolioRead)]`; no `ILicenseService` on the read path; no `useRbac()` change. | — |
| Website | Marketing N/A; `docs/metrics/` + per-theme `@screenshot` at finalization (DELIVER discipline). | — |
| Persistence / EF / premium gate | **None.** No new DTO, no persistence, no EF migration, no premium gate. | — |

## Rationale (D8)

The user directed backend compute to keep production logic out of the frontend and to keep percentile computation **uniform** with the other percentile surfaces. The endpoint mirrors `cycleTimePercentiles` exactly and composes existing primitives — the current in-progress selection (`TeamMetricsService.GetWipSnapshotForTeam` `:563` / `PortfolioMetricsService.GetInProgressFeaturesForPortfolio` `:224`, the same set that feeds the aging-chart dots), each item's `WorkItemBase.WorkItemAge` `:73`, and `BaseMetricsService.BuildPercentiles` `:304` → `PercentileCalculator` (50/70/85/95). Response reuses `PercentileValue` (flat list, NOT per-state like `ageInStatePercentiles`). `startDate`/`endDate` are kept on the signature for parity (shared 400-guard + date-keyed cache) but `startDate` does NOT filter the population — WIA is a snapshot keyed on `endDate` only. The accepted cost is the NEW-endpoint version-gate in the separate clients repo (ADR-055/062 pattern). The prior client-side recommendation (cheaper contract, no Clients work) is documented as the rejected alternative in ADR-065.

## Ports

- **Driving**: 2 NEW HTTP endpoints (`GET …/metrics/workItemAgePercentiles`, Team + Portfolio; existing `[RbacGuard]`). FE driving surface: WIA card + chart CT↔WIA toggle.
- **Driven**: none new — the service reads existing repositories through the existing in-progress selections + existing cache. **No new driven adapter ⇒ no probe contract / no contract tests owed** at the platform-architect handoff (new endpoints read existing repositories already under integration coverage).

## Architecture style / enforcement

Ports-and-adapters / hexagonal — unchanged. OOP backend (2 thin service methods + 2 controller actions, reuse-maximal), functional-leaning React frontend. Enforcement: NUnit service tests (golden percentiles over a known in-progress fixture; population equals the `/wip` / in-progress-features set) + WebApplicationFactory integration tests (response shape == `cycleTimePercentiles`; date-range-invariance with differing `startDate`/same `endDate`; empty-WIP `0`-valued set / single-item WIP; no `ILicenseService` on the path). FE Vitest/Stryker: toggle round-trip + mutual-exclusivity + orthogonal-pace-band; empty-WIP chart snapshot. Clients-repo: version-gate test (old server ⇒ upgrade error; dev/unparseable never blocked).

## Artifacts written / revised (this correction pass)

- ADRs: `docs/product/architecture/adr-065-*.md` (REWRITTEN — verdict flipped to backend; client-side demoted), `adr-066-*.md` (mechanism revised — two server-fetched arrays)
- Brief: `docs/product/architecture/brief.md` → `## Application Architecture — work-item-age-percentiles (Story #5257)` (REVISED to backend endpoints + ports + reuse; siblings untouched)
- C4: `docs/product/architecture/c4-diagrams.md` → `# C4 Architecture Diagrams — work-item-age-percentiles` (System Context unchanged; Container delta now = **2 new endpoints**; Level 3 = server compute + line-source swap)
- Feature-delta: `docs/feature/work-item-age-percentiles/feature-delta.md` → DESIGN `[REF]` sections + `## Changed Assumptions` (REVISED to backend; Clients reversal recorded)

## Changed Assumptions (this pass)

- **DESIGN D8 verdict flipped: client-side → server-side endpoint** (user directive, 2026-06-09). No story/AC changes — both compute paths satisfy the ACs; only the WHERE moved. No `design/upstream-changes.md` needed (DISCUSS D8 was already locked to backend by the user; this pass aligns the DESIGN outputs to it).
- **Lighthouse-Clients: UNAFFECTED → AFFECTED (version-gated).** Supersedes the prior pass's conclusion (quoted in feature-delta `## Changed Assumptions`). A new endpoint ⇒ opaque-404-on-old-server ⇒ version-gated wrapper + `FEATURE_REQUIRES_SERVER_NEWER_THAN` entry, tracked in the separate clients repo.

## Deferred

- DISTILL: per-story Gherkin (date-range-invariance of the endpoint, toggle round-trip/exclusivity, empty/single-item WIP); 4-reviewer gate.
- DELIVER: toggle affordance (switch vs chip-pair, AC-neutral, decided live); the **Lighthouse-Clients (separate repo)** version-gated wrapper + registry bump; mutation kill on the new service methods (`GetWorkItemAgePercentilesForTeam`/`…ForPortfolio` — the `> 0` age filter, the `endDate`-only cache key, the empty-population path); `docs/metrics/` + `@screenshot` at finalization.
