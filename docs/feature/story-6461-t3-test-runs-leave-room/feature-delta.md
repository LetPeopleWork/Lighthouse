# Feature: story-6461-t3-test-runs-leave-room

Story #6461 · T3 Test runs leave room for parallel sessions: local worker caps · part of Epic #6264
([workspace](../epic-6264-modular-monolith/feature-delta.md)) ·
brief: [`slice-43-t3-test-runs-leave-room.md`](../epic-6264-modular-monolith/slices/slice-43-t3-test-runs-leave-room.md)

DISCUSS: covered at Epic level (see the Epic's Wave Decisions, D-6264-3); waves start at DESIGN.

## Wave: PICK-UP / [REF] Inherited

From the Epic's `feature-delta.md`, by heading. Linked, not copied:

- [REF] How to pick up a Story: the recurring per-Story checklist (copied below to tick).
- [REF] Stories → Test-infrastructure track → T3.
- [REF] Story Map → Pick-up groups → G0b (order T3 → T1 → T2 with FE7 → T4) and the dependency graph
  (`T3 (independent)`).
- [REF] Cross-Cutting Impact Checklist (answered for this Story below).
- [REF] Outcome KPIs + guardrails: no KPI row is touched. Guardrail: CI test behaviour and duration unchanged.
- [REF] Wave Decisions and [REF] Risks / Open questions: nothing specific to T3.
- [REF] Improvement log: nothing since 2026-11-01 that changes T3.
- ADR-270: T3 touches no bounded context (test tooling only, Platform-level). No new term, concept or edge;
  no domain event published or consumed.

## Wave: PICK-UP / [REF] DoR re-run

| DoR item | Status | Evidence |
|---|---|---|
| 1 Problem statement | PASS | Neither stack caps its workers locally; each parallel session adds a full set (re-count below). |
| 2 Persona | PASS | `lighthouse-maintainer` and the parallel Claude sessions on the 12-core, 31 GB development machine; the CI build as the thing that must not change. |
| 3 Domain examples | PASS | Vitest's 11 thread workers; NUnit's one-fixture-worker-per-core; the resident `VBCSCompiler`; 2026-11-01 a worktree session's backend run alongside this one. |
| 4 UAT G/W/T | PASS | The brief's three AC; DISTILL writes the scenarios. |
| 5 AC derived from UAT | PASS | 1:1 with the brief's AC. |
| 6 Right-sized | PASS | Size S; likely one or two slices (one per stack). |
| 7 Technical notes + cross-cutting | PASS | Below. |
| 8 Dependencies | PASS | None (brief and graph agree). |
| 9 Outcome measurable | PASS | Wall time and peak memory per stack at both worker settings, same machine. |

## Wave: PICK-UP / [REF] Baseline re-count (2026-11-01)

| Brief says | Today |
|---|---|
| `vitest.config.ts`: `pool: "threads"`, `maxWorkers: undefined` → 11 workers on 12 cores | **Confirmed.** Vitest `^5.0.5`; `pool: "threads"`, `isolate: true`, `maxWorkers: undefined`, `fileParallelism: true`. `nproc` = 12. |
| NUnit `[assembly: Parallelizable(ParallelScope.Fixtures)]`, default `LevelOfParallelism` | **Confirmed**, `Lighthouse.Backend.Tests/GlobalUsings.cs:3`. No `LevelOfParallelism` or `NumberOfTestWorkers` anywhere, so one fixture worker per core (12). |
| runsettings | `Lighthouse.Backend.Tests/lighthouse.runsettings` exists (TZ pin only), wired through `<RunSettingsFilePath>` so a bare `dotnet test` reads it. A natural home for a worker setting. |
| CI uses the full runner | CI passes `RunConfiguration.MaxCpuCount=0` (process-level; there is one test assembly, so it does not change NUnit's worker count). `ubuntu-latest` runners have 4 cores, so "full count" in CI = 4. GitHub sets `CI=true`. |
| `VBCSCompiler` resident at about 5.3 GB | **2.4 GB** at the time of the re-count (after a build ~6 min earlier); it varies with what was built. |
| "Reduce memory usage on CI" comment beside `bail: 1` | **Still there** (`vitest.config.ts`, CI spread block). |
| Docs for the variable and `dotnet build-server shutdown` | **None yet**: no mention in CLAUDE.md, `.claude/` or `docs/`. |
| Frontend test files (FE7 brief: 577) | **576** tracked `*.test.ts(x)` files. |
| `BackendTestParallelizationGuardTest` | 7 allow-listed serial fixtures. T3 does not change parallel scope, only the worker count. |

## Wave: PICK-UP / [REF] Cross-cutting re-check

- RBAC: N/A, because no code path or authorization rule changes; only test-runner configuration.
- Lighthouse-Clients: N/A, because no route, DTO or serialized response changes.
- Website: N/A, because nothing user-visible changes.
- Usage-data event: provisionally N/A, because it is behaviour-free test tooling nobody uses in the product;
  DEVOPS confirms.
- UI sketch: N/A, because nothing a user sees changes.
- Terminology: N/A, because no user-facing words change.

## Wave: PICK-UP / Open questions for DESIGN

1. NUnit: an assembly attribute cannot read an environment variable. Options: runsettings
   `NUnit.NumberOfTestWorkers` (does `dotnet test` expand env vars in runsettings?), a CLI pass-through, or an
   `MSBuild` property that writes the runsettings value. DESIGN picks one that a bare `dotnet test` honours.
2. Does "local default" mean half the cores (6), or the measured knee? The measurement decides.
3. The backend peak memory is dominated by T1's leak (up to 18 GB). The backend default chosen now is
   re-measured after T1 lands; this item records that as a follow-up rather than waiting.
4. Playwright (`Lighthouse.EndToEndTests`) is outside the brief ("both stacks" = Vitest + NUnit). Confirm
   at DESIGN that it stays out.

## Wave: PICK-UP / Checklist

- [ ] Behaviour-free: no test added, removed or changed in what it asserts; CI worker count and duration
  unchanged (shown in the CI logs).
- [ ] Ratchet in the same change, if one applies (for example a test pinning that CI uses the full count),
  or "N/A, because …" stated at DESIGN.
- [ ] Moves separate from edits (N/A expected: configuration only; stated at DESIGN).
- [ ] Re-count before designing (done above).
- [ ] Mutation last, on frozen code: N/A expected (configuration, no production or helper logic); stated at
  DESIGN.
- [ ] Architecture docs in the same change: CLAUDE.md quality gates and/or `/run-dev`; ARCHITECTURE.md
  only if it describes test running.
- [ ] Payoff proved: wall time and peak memory at both settings, both stacks, same machine, quiet slot.
- [ ] No user-facing release notes.
- [ ] Cross-cutting re-check answered (above).
- [ ] Terminology: N/A (no user-facing names).
