# DISTILL RED classification — multiple-cycle-times (Epic 5251)

Date: 2026-06-08. Pre-DELIVER fail-for-the-right-reason gate.

This project is C#/.NET (NUnit + `WebApplicationFactory<Program>`) + React/TS + Playwright — NOT the
Python/Hypothesis pilot (per `docs/architecture/atdd-infrastructure-policy.md`). The acceptance scaffolds
are NUnit integration tests with the `[Ignore]` skip marker; the E2E walking skeleton is authored and
run live in DELIVER Slice 01 (POM-dependent, per the project "run-before-commit" rule).

## Structural RED verification (performed in DISTILL)

`dotnet build Lighthouse.Backend.Tests.csproj` ⇒ **Build succeeded, 0 warnings, 0 errors.**

Every scaffold references ONLY existing symbols (`NUnit.Framework`), so the assembly compiles. Each
`[Test]` is `[Ignore(...)]` (pending, one-at-a-time per DELIVER cycle) and its body is a single
`Assert.Fail("<GWT + contract + seed strategy>")`. When the crafter un-`[Ignore]`s a test at the start
of its slice's RED phase, it fails by **assertion** (`MISSING_FUNCTIONALITY` — correct RED), never by
`ImportError`/compile/fixture error (`BROKEN`). This is the C#/NUnit analogue of the Mandate-7 rule
"method bodies MUST raise an assertion error, not NotImplementedError".

DELIVER procedure per scenario: un-`[Ignore]` → flesh the body (seed via the
`CumulativeStateTimeReadApiIntegrationTest` / `ForecastFilterTeamSettingsIntegrationTest` idioms,
drive real HTTP, parse JSON) → confirm it fails because the behaviour is missing (404/missing-field/
wrong-value), NOT because of setup → implement → GREEN.

## Per-file classification (all RED-ready)

| Scaffold file | Slice / Story | Scenarios | Classification |
|---|---|---|---|
| `API/Integration/NamedCycleTimeReadApiIntegrationTest.cs` | 01 / US-01 | 8 | RED — `definitionId` is ignored by today's endpoint ⇒ default series returned where the named series is asserted; premium/RBAC branches absent |
| `API/Integration/CycleTimeDefinitionSettingsIntegrationTest.cs` | 02 / US-02 | 8 | RED — `CycleTimeDefinitions` field absent on the settings contract ⇒ round-trip/validation assertions fail; no compile dep (JSON) |
| `API/Integration/CycleTimeDefinitionValidityIntegrationTest.cs` | 03 / US-03 | 6 | RED — no `IsValid` stamp / invalid-signal yet ⇒ cross-surface validity assertions fail |
| `API/Integration/NamedCycleTimeCumulativeScopeIntegrationTest.cs` | 04 / US-04 | 5 | RED — `cumulativeStateTime` ignores `definitionId` ⇒ window-restriction assertions fail |
| `API/Integration/NamedCycleTimePortfolioIntegrationTest.cs` | 05 / US-05 | 6 | RED — Portfolio twins absent |
| `Architecture/NamedCycleTimeSeamArchUnitTest.cs` | ADR-061/063 | 4 | RED — `NamedCycleTimeDays` / `IsCycleTimeDefinitionValid` not yet present ⇒ reflection rules fail |

Total: 37 backend acceptance/seam scenarios scaffolded RED + 1 E2E walking-skeleton (DELIVER Slice 01).

No scenario is in the BROKEN (import/fixture/setup) or WRONG-SHAPE (couples to internal struct)
categories. The integration scaffolds assert only at the driving port (HTTP status + response JSON) and
the seam scaffolds assert only public/protected reflection shape — refactoring-safe Universes.

**Gate result: PASS — handoff to DELIVER unblocked.**
