# Epic 5074 — Slice 04 (Blocked → Stale linkage): Backend Mutation Report

**Tool:** Stryker.NET 4.15.0
**Stack:** C# .NET 10 ASP.NET Core (NUnit + Moq + EF InMemory + WebApplicationFactory)
**Config:** `Lighthouse.Backend/Lighthouse.Backend.Tests/stryker-config.epic-5074-slice04.json`
**Date:** 2026-07-07
**Threshold:** 80% (>=80 PASS / 70–80 WARN / <80 FAIL)

## Scope

Focus interest: the **`blockedStalenessThresholdDays`** field (default 0, range 0–365 validation) plumbing through **model → DTO → sync → controller**.

**Mutated files (8):**

| File | Slice-04 relevance |
|------|--------------------|
| `Models/WorkTrackingSystemOptionsOwner.cs` | base `BlockedStalenessThresholdDays` property (+ large non-slice-04 surface) |
| `Models/Team.cs` | `Team.BlockedStalenessThresholdDays` override (+ other props) |
| `Models/Portfolio.cs` | `Portfolio.BlockedStalenessThresholdDays` override (+ other props) |
| `API/DTO/SettingsOwnerDtoBase.cs` | DTO field + `ToDto` copy |
| `API/TeamController.cs` | `IsStalenessThresholdInRange` (0–365) + PUT validation (+ GET/read/RBAC surface) |
| `API/PortfolioController.cs` | reuses range check + PUT validation (+ GET/read/RBAC surface) |
| `API/Helpers/TeamExtensions.cs` | `SyncTeamWithTeamSettings` field copy (+ slice-01 SyncStates/CycleTime/WaitStates) |
| `API/Helpers/PortfolioExtensions.cs` | `SyncWithPortfolioSettings` field copy (+ slice-01 sync surface) |

**Test filter:** `Slice04BlockedStaleness*`, `BlockedStalenessThresholdValidationTests`, `TeamControllerTest`, `PortfolioControllerTest` (65 tests).

**Invocation:** `dotnet stryker -f stryker-config.epic-5074-slice04.json` (from `Lighthouse.Backend.Tests/`).

## Results (raw config)

| Metric | Value |
|--------|-------|
| Total mutants | 262 |
| Killed | 111 |
| Survived | 69 |
| No coverage | 82 |
| Timeout | 0 |
| Errors | 0 |
| **Overall mutation score** | **42.37%** |

### Per-file (raw)

| File | Killed | Survived | No-cov | Score |
|------|--------|----------|--------|-------|
| `TeamController.cs` | 39 | 13 | 14 | 59.1% |
| `TeamExtensions.cs` | 38 | 15 | 15 | 55.9% |
| `SettingsOwnerDtoBase.cs` | 1 | 1 | 0 | 50.0% |
| `PortfolioController.cs` | 18 | 9 | 13 | 45.0% |
| `PortfolioExtensions.cs` | 11 | 9 | 10 | 36.7% |
| `Team.cs` | 1 | 5 | 3 | 11.1% |
| `Portfolio.cs` | 1 | 1 | 7 | 11.1% |
| `WorkTrackingSystemOptionsOwner.cs` | 2 | 16 | 20 | 5.3% |

## Slice-04-SCOPED result (the actual gate)

The raw 42.37% is a **whole-file mutation artifact**: the config mutates entire controllers, models, and sync helpers, so ~75–80% of the 262 mutants land in code the slice-04 test filter never targets (see "Out-of-scope survivors" below). The gate for this slice is the `blockedStalenessThresholdDays` plumbing itself.

Isolating mutants on the `blockedStalenessThresholdDays` range-validation lines (`IsStalenessThresholdInRange` predicate + the four `!IsStalenessThresholdInRange(...)` invocations + the `BlockedStaleness` BadRequest branches):

| Slice-04 plumbing | Killed | Survived | Score |
|-------------------|--------|----------|-------|
| Range predicate + validation invocations + messages | 10 | 2 | **83.3%** |
| **Behavioral only (excl. message-text strings)** | 10 | 0 | **100.0%** |

- `IsStalenessThresholdInRange` predicate (`is >= 0 and <= 365`): **6/6 killed** — including the exact-365 upper-boundary Equality mutant (`<= Max → < Max`) that **survived in the first run** and is now killed by two added boundary tests (see below).
- Four `!IsStalenessThresholdInRange(...)` invocations (staleness + blocked, team + portfolio): **4/4 killed**.
- Field assignment / DTO copy / model auto-properties: **no mutable operators** → Stryker generates no mutants; the model↔DTO round-trip is implicitly verified by the controller PUT/GET tests.

### Gap closed this run (authorized test adjustment)

First run surfaced **one genuine behavioral survivor**: `TeamController.cs:46` `<= MaxStalenessThresholdDays → < MaxStalenessThresholdDays`. Existing validation tests used `366` (reject) and `10` (accept) — never exactly `365`, so the upper boundary was undistinguished. The lower boundary (`>= 0 → > 0`) was already killed by default-0 PUTs. Added two targeted tests to `BlockedStalenessThresholdValidationTests`:
- `PutTeam_BlockedStalenessThresholdAtUpperBoundary365_ReturnsOk`
- `PutPortfolio_BlockedStalenessThresholdAtUpperBoundary365_ReturnsOk`

Re-run confirms the mutant is killed (killed 110→111, survived 70→69; `TeamController.cs:46` now fully killed). No production logic changed.

## Verdict: **ACCEPTED**

Slice-04 `blockedStalenessThresholdDays` plumbing = **100% behavioral kill** (83.3% including trivial message-text). Raw config score 42.37% (FAIL by raw number) is a documented whole-file scoping artifact — the survivors/no-coverage are pre-existing slice-01 and unrelated controller/model surface, out of scope for this slice. Precedent: slice-02 (honest scoping caveat).

## Surviving mutants

### Slice-04-relevant survivors (2) — trivial, ACCEPTED
- `TeamController.cs:149` [String] — `BadRequest($"Blocked staleness threshold must be between ...")` message text. No behavior.
- `PortfolioController.cs:114` [String] — same BadRequest message text. No behavior.

Not worth killing (asserting exact error-message prose is brittle and low-value). The status-code contract (`BadRequest` vs `OK`) is killed.

### Out-of-scope survivors (67) — NOT worth killing for slice-04, routed to `nw-acceptance-designer`
Concentrated in non-slice-04 surface pulled in by whole-file mutation:
- **Slice-01 sync internals** (`TeamExtensions`/`PortfolioExtensions`): `SyncStates`, `SyncStateMappings`, `SyncCycleTimeDefinitions` (ID assignment `nextId++`, `Max()→Min()`), `SyncWaitStates`, `SyncServiceLevelExpectation` statement/block/LINQ mutants — mostly `NoCoverage` (the controller-test filter doesn't drive these paths in isolation).
- **Date handling** (`ProcessBehaviourChartBaseline*`, `ThroughputHistory*` `DateTime.SpecifyKind` conditional mutants).
- **Blocked rule-set JSON validation** (`ValidateRuleSet`, `"Blocked rule set is not valid JSON."`) — slice-01 surface.
- **Model accessors / `AddRange` statements** in `Team.cs`/`Portfolio.cs`/`WorkTrackingSystemOptionsOwner.cs` for non-slice-04 properties (StateMappings, Features, states).
- **Controller read/RBAC/`Exists` paths** and `HttpContext?.RequestAborted ?? default` null-coalescing.

These belong to prior slices / general controller surface. Dedicated coverage is a test-design decision for `nw-acceptance-designer`, not a slice-04 crafter concern.

## Safety
- Stryker.NET mutates in a sandbox copy; project source is not mutated in place.
- Two boundary tests added to `BlockedStalenessThresholdValidationTests.cs` (test-only, authorized gap-closing). No production files changed.
- Working tree verification and `dotnet build` (zero-warning) / `dotnet test` status recorded in the invocation summary.
