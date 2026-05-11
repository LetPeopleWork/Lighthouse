# Feature Delta: system-info-auth-visibility

<!-- markdownlint-disable MD024 -->

Wave: DISTILL | Date: 2026-05-11 | Density: lean (per ~/.nwave/global-config.json)

Feature goal: surface the configured authentication, authorization (RBAC) and emergency-admin
posture in two places that already exist: (1) the ASCII startup banner printed by
`Program.PrintSystemInfo`, (2) the System Info display rendered by `<SystemInfoDisplay />`
in Settings.

This is a fast-tracked DISTILL: prior waves (DISCUSS, DESIGN, DEVOPS) were not run as
separate sessions because the change is purely additive observability over existing,
already-resolved auth/RBAC configuration. The relevant prior decisions live in
`rbac-enhancements` (Q2 emergency-admin display) and `docs/product/architecture/adr-003`.
No contradictions detected with those decisions -- this feature exposes the same data on
a different surface (operator-facing system info rather than RBAC admin table).

---

## Wave: DISTILL / [REF] Inherited commitments

| Origin | Commitment | DDD | Impact |
|--------|------------|-----|--------|
| n/a | New `SystemInfo` fields `authenticationEnabled`, `authorizationEnabled`, `emergencyAdminSubjects` must be added without removing any existing field. | n/a | Backend `SystemInfo` record gains three properties; existing OS/Runtime/Architecture/ProcessId/Database/LogPath callers stay green. |
| rbac-enhancements#Q2 | Emergency admin must be visually distinguished from a normally-granted System Admin. | ADR-003 | This feature reuses the configured `Authorization:EmergencySystemAdminSubjects` source; it does not introduce a second source of truth for emergency admins. |
| docs/product/architecture/brief.md | `IRbacAdministrationService` is the single inbound port for RBAC business logic; controllers depend only on the interface. | n/a | `SystemInfoService` reads the *configuration* directly (it is not making RBAC decisions). No new RBAC port created; no existing port mutated. |
| Lighthouse.Backend/Models/Auth/AuthorizationConfiguration.cs | Emergency admin subjects are stored in `EmergencySystemAdminSubjects: IReadOnlyList<string>`; auth is gated by `AuthenticationConfiguration.Enabled` and RBAC by `AuthorizationConfiguration.Enabled`. | n/a | The DTO contract reads from this existing config; no new config keys. |

---

## Wave: DISTILL / [REF] Acceptance scenarios

Scenario SSOT lives in `docs/feature/system-info-auth-visibility/acceptance/*.feature`.
Below is the index with tags. Each scenario maps to ONE TDD cycle in DELIVER.

| Scenario | File | Tags | TDD slice |
|---|---|---|---|
| Operator sees auth posture in the terminal startup banner | `acceptance/walking-skeleton.feature` | `@walking_skeleton @real-io @driving_adapter` | WS |
| Auth enabled and RBAC enabled with one emergency admin -- API | `acceptance/milestone-1-api-exposes-auth-fields.feature` | `@real-io @adapter-integration @milestone-1` | M1.1 |
| Auth disabled and RBAC disabled with no emergency admins -- API | `acceptance/milestone-1-api-exposes-auth-fields.feature` | `@real-io @adapter-integration @milestone-1` | M1.2 |
| Multiple emergency admins are reported verbatim -- API | `acceptance/milestone-1-api-exposes-auth-fields.feature` | `@real-io @adapter-integration @milestone-1 @error` | M1.3 |
| Emergency admin configured while RBAC is disabled -- API | `acceptance/milestone-1-api-exposes-auth-fields.feature` | `@real-io @adapter-integration @milestone-1 @error` | M1.4 |
| All three rows present when auth and RBAC enabled with emergency admin -- UI | `acceptance/milestone-2-ui-renders-auth-rows.feature` | `@in-memory @milestone-2 @driving_adapter` | M2.1 |
| Authentication and Authorization rows display "Disabled" -- UI | `acceptance/milestone-2-ui-renders-auth-rows.feature` | `@in-memory @milestone-2` | M2.2 |
| Emergency Admin row hidden when no subjects configured -- UI | `acceptance/milestone-2-ui-renders-auth-rows.feature` | `@in-memory @milestone-2 @error` | M2.3 |
| Emergency Admin row hidden when RBAC disabled -- UI | `acceptance/milestone-2-ui-renders-auth-rows.feature` | `@in-memory @milestone-2 @error` | M2.4 |
| Multiple emergency admins render as comma-separated value -- UI | `acceptance/milestone-2-ui-renders-auth-rows.feature` | `@in-memory @milestone-2` | M2.5 |
| Banner labels and API fields agree (auth on, RBAC on, emergency admin) | `acceptance/milestone-4-cross-layer-consistency.feature` | `@real-io @cross-layer @milestone-4 @driving_adapter` | M4.1 |
| Banner labels and API fields agree (auth off, RBAC off, no emergency admin) | `acceptance/milestone-4-cross-layer-consistency.feature` | `@real-io @cross-layer @milestone-4 @error` | M4.2 |
| Banner includes Authentication line "Enabled" | `acceptance/milestone-3-terminal-banner.feature` | `@real-io @driving_adapter @milestone-3` | M3.1 |
| Banner includes Authentication line "Disabled" | `acceptance/milestone-3-terminal-banner.feature` | `@real-io @driving_adapter @milestone-3` | M3.2 |
| Banner includes Authorization line "Enabled" | `acceptance/milestone-3-terminal-banner.feature` | `@real-io @driving_adapter @milestone-3` | M3.3 |
| Emergency Admin banner line omitted when no subjects | `acceptance/milestone-3-terminal-banner.feature` | `@real-io @driving_adapter @milestone-3 @error` | M3.4 |
| Emergency Admin banner line lists multiple subjects | `acceptance/milestone-3-terminal-banner.feature` | `@real-io @driving_adapter @milestone-3` | M3.5 |
| Emergency Admin banner line omitted when RBAC disabled | `acceptance/milestone-3-terminal-banner.feature` | `@real-io @driving_adapter @milestone-3 @error` | M3.6 |

Counts:
- Walking skeleton: 1
- Milestone 1 (API): 4 (2 happy, 2 error)
- Milestone 2 (UI): 5 (2 happy, 3 error/edge)
- Milestone 3 (Terminal banner): 6 (3 happy, 3 error/edge)
- Milestone 4 (Cross-layer journey): 2 (1 happy, 1 error/edge)
- Total: 18 scenarios. Error/edge ratio: 9 / 18 = 50% (above the 40% target).

---

## Wave: DISTILL / [REF] Walking skeleton strategy

Strategy: **C (Real local)**.

Rationale: the feature has no costly external dependencies. Both driving adapters
(process stdout banner, HTTP endpoint) are local and in-process. Real-I/O tests are
cheap and catch the actual integration risks (config wiring, banner formatting,
DTO serialisation, hexagonal boundary).

- WS scenario is tagged `@real-io @driving_adapter @walking_skeleton`.
- Backend integration tests for milestone-1 use `WebApplicationFactory<Program>` with
  `IConfiguration` overrides (existing pattern; see
  `Lighthouse.Backend.Tests/API/Integration/`).
- Terminal banner tests for milestone-3 capture the `ILogger`/`Console` output during
  startup of a real `WebApplicationBuilder` configured against test-only settings.
- Frontend tests for milestone-2 use Vitest + React Testing Library with the existing
  `createMockSystemInfoService` factory (already established for `SystemInfoDisplay`).

No container option is taken: the feature is self-contained within the Lighthouse
process; testcontainers would add latency without uncovering new failure modes.

---

## Wave: DISTILL / [REF] Adapter coverage

Every driven adapter touched by this feature has at least one `@real-io` scenario or
a contract test.

| Adapter | `@real-io` scenario | Covered by |
|---------|---------------------|------------|
| `IConfiguration` (reads `Authentication:Enabled`, `Authorization:Enabled`, `Authorization:EmergencySystemAdminSubjects`) | YES | Milestone-1 API scenarios use a real `WebApplicationFactory` with overridden `IConfiguration` -- the production binding path is exercised end-to-end. |
| Process stdout (terminal banner) | YES | Walking skeleton + milestone-3 scenarios capture real `Log.Logger.Information("\n{StartupBanner}", ...)` output from `Program.PrintSystemInfo`. |
| `GET /api/latest/SystemInfo` HTTP endpoint | YES | Milestone-1 scenarios issue real HTTP requests via `WebApplicationFactory<Program>.CreateClient()`. |
| `SystemInfoService.GetSystemInfo()` (in-process port) | covered transitively | Exercised by every milestone-1 scenario via the HTTP path. |
| Frontend `SystemInfoService` HTTP client | YES (extension required) | DELIVER step 0 MUST extend `Lighthouse.Frontend/src/services/Api/SystemInfoService.test.ts` to assert that `authenticationEnabled`, `authorizationEnabled`, and `emergencyAdminSubjects` are present and correctly typed in the deserialised response (boolean/boolean/string[]). The existing test only asserts pre-feature fields, so without this extension a server contract drift would go silent. |
| Cross-layer (banner stdout + HTTP API on the same process) | YES | Milestone-4 scenarios start a real `WebApplicationFactory<Program>`, capture the banner output via the Serilog test sink, then issue an HTTP request and assert equivalence of all three values. |

No `NO -- MISSING` rows.

---

## Wave: DISTILL / [REF] RED-ready scaffolds (Mandate 7)

The following production files require minimal stubs so the acceptance tests are RED
(not BROKEN) on first run. See per-file diff plan below; scaffold markers use
`// __SCAFFOLD__` (C#) and `export const __SCAFFOLD__ = true` (TS).

**Scaffolds are PLANNED but DEFERRED to DELIVER step 0**. The DISTILL session was run
on `fix/bug-4975-manual-delivery-date-update` (a bug-fix branch unrelated to this
feature); applying scaffolds there would entangle this feature with that bug fix.
DELIVER must branch from `main` for this feature and create the scaffolds as the
first commit before red-bar tests are added. The scaffold plan and acceptance
criteria for "first DELIVER commit" are recorded here so the crafter can act
without re-reading the whole feature.

Backend (C#):
- `Lighthouse.Backend/Lighthouse.Backend/Models/SystemInfo.cs` -- extend `SystemInfo`
  record with three new properties; default values let existing call sites compile
  unchanged.
- `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/SystemInfoService.cs`
  -- read the three new config sources; for the scaffold, leave the population
  throwing `AssertionFailedException` (xUnit equivalent: `Assert.Fail(...)`).
- `Lighthouse.Backend/Lighthouse.Backend/Program.cs` -- a private
  `PrintAuthPostureLines(IConfiguration, List<string> info)` helper. Scaffold body
  throws `Assert.Fail(...)`.

Frontend (TypeScript):
- `Lighthouse.Frontend/src/models/SystemInfo/SystemInfo.ts` -- extend interface with
  three optional fields.
- `Lighthouse.Frontend/src/pages/Settings/SystemInfo/SystemInfoDisplay.tsx` -- add
  three new row entries gated on the new fields. Scaffold version simply maps
  `undefined` so the test asserts the new rows are missing and fails for the right
  reason.

NOTE on the assertion-error policy: this project does not use Python's
`AssertionError`/`NotImplementedError` Red Gate Snapshot. The Lighthouse
quality gates (xUnit `dotnet test`, Vitest, ESLint, Biome) classify a failing test
the same way regardless of which exception is raised. The intent of Mandate 7
is preserved by raising an exception that is unambiguously a failed expectation
(not a missing module), and by ensuring the tests compile and execute. We use
`throw new System.NotImplementedException("RED scaffold")` for C# and
`throw new Error("RED scaffold")` in TypeScript, with a `// __SCAFFOLD__` marker
on the surrounding declaration.

---

## Wave: DISTILL / [REF] Test placement

Backend acceptance tests (xUnit, .NET 8):
- API integration: `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/SystemInfoControllerAuthVisibilityTests.cs`
  -- one fixture per scenario from `milestone-1-api-exposes-auth-fields.feature`.
  Precedent: `BlackoutPeriodsControllerAuthorizationTests.cs`,
  `TeamDeletionIntegrationTest.cs` already in the same directory.
- Service unit: `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/SystemInfoServiceTest.cs`
  -- extend the existing file with three new `[Test]` methods. Precedent:
  same file currently covers OS/Runtime/Architecture/Database in the identical
  pattern.
- Terminal banner: `Lighthouse.Backend/Lighthouse.Backend.Tests/Integration/StartupBannerAuthVisibilityTest.cs`
  -- captures `Log.Logger` output during `WebApplicationFactory` startup. New file;
  no existing test exercises `PrintSystemInfo`. Precedent for `WebApplicationFactory`
  usage is in `Lighthouse.Backend.Tests/API/Integration/`.

Frontend acceptance tests (Vitest + Testing Library):
- `Lighthouse.Frontend/src/pages/Settings/SystemInfo/SystemInfoDisplay.test.tsx`
  -- extend with five new test cases covering milestone-2 scenarios. Precedent:
  existing file already contains the conditional-row pattern for
  `databaseConnection` and `logPath`.
- `Lighthouse.Frontend/src/services/Api/SystemInfoService.test.ts` -- update
  the canned response shape; existing test already covers the HTTP-deserialisation
  contract.

E2E (Playwright): no E2E test is added. Rationale: the feature is single-page,
purely informational, and fully covered by milestone-2 component tests plus
milestone-1 API integration tests. Adding a Playwright spec would not surface
additional failure modes given the existing coverage.

---

## Wave: DISTILL / [REF] Driving adapter coverage

Per RCA fix P1 (2026-04-10), every CLI/endpoint/hook adapter in DESIGN has at least
one scenario exercising it via its real protocol.

| Driving adapter | Real-protocol scenario | Verifies |
|---|---|---|
| `GET /api/latest/SystemInfo` (HTTP) | milestone-1 + milestone-4 scenarios via `HttpClient` | HTTP status, JSON shape (camelCase fields), authentication still required, new fields present |
| Process startup banner (stdout via Serilog) | walking-skeleton + milestone-3 + milestone-4 scenarios capture banner text | banner formatting, label/value layout, conditional omission of Emergency Admin line |
| `<SystemInfoDisplay />` React component | milestone-2 scenarios via Testing Library `render()` | DOM nodes for the three new rows, conditional visibility |
| Cross-layer journey (banner + UI consistency) | milestone-4 scenarios | banner and API agree on the same configuration -- a single source of truth for the operator |

No scope was reduced to bypass the user's actual invocation path -- the API is hit
via HTTP, the banner via real Serilog output, the UI via real component render.

---

## Wave: DISTILL / [REF] Pre-requisites

From DESIGN (architecture brief):
- `AuthenticationConfiguration` and `AuthorizationConfiguration` records already exist
  in `Lighthouse.Backend/Models/Auth/`. No changes required.
- `IConfiguration` is already injected into `SystemInfoService` (current constructor).
  The service can read the two `Enabled` flags and the
  `EmergencySystemAdminSubjects` list via the same `configuration` field.

From DEVOPS: no infrastructure changes. The new fields are read at request-time
from in-memory `IConfiguration`; no new environment variables, no new secrets,
no new persisted state, no new migration.

Migrations: none.

Feature flags: none. The new banner lines and UI rows are unconditional once shipped.
Operators who do not want them visible can disable the upstream feature (auth/RBAC)
or filter the log.

---

## Wave: DISTILL / [REF] Self-review checklist

- [x] WS strategy declared (Strategy C -- Real local)
- [x] WS scenarios tagged correctly (`@walking_skeleton @real-io @driving_adapter`)
- [x] Every driven adapter has at least one `@real-io` scenario (see adapter coverage table)
- [x] InMemory doubles documented: only `IConfiguration` for the `SystemInfoService` unit
      tests (existing pattern); cannot model real binding errors -- those are caught by
      milestone-1 integration tests
- [x] Container preference documented (none required)
- [x] Mandate 7 scaffolds: production files extended with scaffold markers; methods
      throw `NotImplementedException("RED scaffold")` / `throw new Error("RED scaffold")`
- [x] Driving adapter coverage: HTTP endpoint, stdout banner, React component each
      have a scenario exercising the real protocol
- [x] Every `@when` step references a driving port (HTTP endpoint, process startup,
      user interaction with the React component) -- never the driven adapter directly
- [x] Error path coverage 50% (>= 40% target)
- [x] No prior-wave contradictions (see introductory paragraph)
- [x] Outcomes registry: feature introduces operational disclosure, not a new typed
      contract -- methodology-only/observability-only, registration not required
      (per D-6 gate-scoping; the existing `GET /SystemInfo` operation gains fields
      but is not a new operation)
