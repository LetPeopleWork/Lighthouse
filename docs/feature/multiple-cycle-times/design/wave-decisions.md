# DESIGN Wave Decisions - multiple-cycle-times (Epic 5251)

Architect: Morgan (Solution Architect). Date: 2026-06-08. Interaction mode: PROPOSE.
Paradigm: OOP (C# .NET 8 backend), functional-leaning React/TS. Pattern: ports-and-adapters (existing).
ADRs: adr-061, adr-062, adr-063, adr-064 (`docs/product/architecture/`).

## Architecture summary

This feature adds Premium **named cycle times** (`{ name, startState, endState }`, ordered-boundary
semantics over `AllStates`) by EXTENDING existing metrics ports, endpoints, the settings aggregate, and
the scatter/cumulative charts. No new architectural style; no new computation engine, endpoint route,
mapping resolver, or chart. The named ordered-boundary duration is computed in the metrics layer by a
new pure `NamedCycleTimeDays` helper that REUSES the existing `CompletedVisits` transition-ordering
primitive; the hot `WorkItemBase.CycleTime` is untouched. Reads ride the EXISTING `cycleTimeData` /
`cycleTimePercentiles` / `cumulativeStateTime` endpoints via an additive optional `definitionId` (same
`WorkItemDto`/`PercentileValue` contract, so the FE scatter render path is unchanged), which means
**zero new client version-gate touch-points** and graceful degrade on old servers. Definition validity
(D5) has a SINGLE source of truth - one method on the settings aggregate, stamped as `IsValid` into
every read DTO - retiring the DISCUSS HIGH cross-surface-consistency risk by construction.

## Three open forks (PROVISIONAL - pending user confirmation)

| Fork | Recommendation | Rejected alternatives |
|------|----------------|-----------------------|
| 1. Computation placement | **Metrics-layer `NamedCycleTimeDays` helper reusing `CompletedVisits`; `WorkItemBase.CycleTime` untouched** (ADR-061 Option A) | B generalise the model property (model->settings coupling, blast radius on hot paths); C standalone calculator (duplicates ordering, breaks cross-surface consistency) |
| 2. Read-endpoint contract | **Extend existing `cycleTimeData`/`cycleTimePercentiles` with optional `definitionId`** (ADR-062 Option c) | A new definition-by-id endpoint (new client gate, opaque 404, duplicate scaffolding); B inline start/end states (boundaries on the wire, bypasses D5, still gated) |
| 3. Validity SSOT (D5) | **One method on the settings aggregate, stamped `IsValid` into every DTO, one mirroring TS predicate** (ADR-063 Option i) | ii domain service (wraps the aggregate anyway, DI seam for a one-liner); iii ad hoc per surface (the silent-divergence failure mode D5 forbids) |

## Key decisions (LOCKED in DESIGN)

- DES-4 persistence: `CycleTimeDefinition` owned collection mirroring `StateMappings`; additive
  `CycleTimeDefinitionDto` (stamped `IsValid`) on `SettingsOwnerDtoBase`; tokened settings write
  (epic-5121 concurrency inherited). ADR-064.
- DES-5 US-04 cumulative scope: additive `definitionId` on `cumulativeStateTime`; half-open
  `[enter start..enter end)` window (D10) reusing the scatter boundary resolution -> span agrees by
  construction. ADR-063 section 4.

## Reuse table (verdicts)

EXTEND/REUSE everywhere except three genuinely-absent UI/persistence artifacts. See feature-delta
"Wave: DESIGN / [REF] Reuse Analysis" for the full evidence table. CREATE-NEW (justified):
`CycleTimeDefinition` entity + `CycleTimeDefinitionDto`; cycle-time config editor; scatter selector +
cumulative scope switch+selector; one TS validity predicate.

## Tech stack (no new technology)

EF Core multi-provider owned-collection (mirror `StateMappings`); ASP.NET Core controllers + `RbacGuard`
+ existing cycle-time endpoint scaffolding; MUI-X scatter (unchanged) + MUI Select/Autocomplete selector
and mapping-aware boundary picker (ADR-056 idiom); Zod at the settings + metrics boundaries;
Stryker.NET / Stryker FE (>=80% per-feature).

## Constraints / DELIVER flags

- **EF migration** for `CycleTimeDefinitions` via the `CreateMigration` PowerShell script (all
  providers), NOT `dotnet ef migrations add`; InMemory tests miss persisted-model migrations ->
  explicit real-provider read-your-writes test required. Mind the `--no-incremental` stale-migration-DLL
  rebuild trap (prior memory).
- **Cross-surface consistency** (HIGH risk): config list + scatter selector + cumulative scope must
  read the SAME `IsValid`; enforced by the single aggregate method + stamped flag (ADR-063) and a
  cross-surface integration test.
- **Premium + RBAC**: named branch premium-gated server-side (defence-in-depth) behind `useRbac()` UI
  gating; no new authz surface; rides `IRbacAdministrationService`-governed settings write.
- **Lighthouse-Clients**: NO new version gate (additive `definitionId` + additive settings field);
  clients pass `definitionId` to the existing read if/when they expose named reads - record N/A in the
  clients repo at wrap-or-skip time. Contrast ADR-055 (new endpoint -> gate).
- **Telemetry** (KPI 1/2): self-hosted instances do not phone home (Epic 5015 blocker); KPI 2
  needs a `definitionId`-keyed selector-change event (the server-side definition id supports this).
  Flagged for DEVOPS.
- **Docs + per-theme `@screenshot` + website** (premium feature) at finalization (CLAUDE.md).

## Upstream DISCUSS changes

NONE. D1-D10 honoured as locked. D8's "NEW read endpoint" is realised as an additive `definitionId`
on existing endpoints (ADR-062) - satisfies D8's intent (per-definition read; no new write contract)
while improving the client-gate posture. Recorded as a refinement of D8's mechanism, not a decision
change; no story rewritten.

## Outcome Collision Check

SKIPPED - `docs/product/outcomes/registry.yaml` / `nwave-ai` CLI not present in this repo
(nWave-tooling artifact). Per instruction, documented and not blocking.
