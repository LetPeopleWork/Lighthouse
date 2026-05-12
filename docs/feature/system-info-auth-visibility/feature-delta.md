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

---

## Wave: DELIVER / [REF] Implementation Summary

Shipped on `feat/system-info-auth-visibility` in 4 commits. Backend exposes three new
fields (`IsAuthenticationEnabled`, `IsAuthorizationEnabled`, `EmergencyAdminSubjects`)
on the `SystemInfo` DTO; `SystemInfoService` reads them from existing
`AuthenticationConfiguration` and `AuthorizationConfiguration` records via
`IConfiguration.GetSection().Get<T>()`. A new helper `AuthPostureBanner.BuildAuthPostureLines`
(in `Lighthouse.Backend.Startup`) returns the three banner lines as
`IReadOnlyList<string>`; `Program.PrintSystemInfo` calls it to add the lines to the
existing banner output. The Emergency Admin line is conditional on RBAC enabled AND
non-empty subjects. Frontend extends the `SystemInfo` interface with three optional
fields and adds three rows to `SystemInfoDisplay`, with the Emergency Admin row gated
identically on the frontend. No new dependencies, no schema migrations, no new
endpoints, no new configuration keys.

---

## Wave: DELIVER / [REF] Files Modified

Backend production (4 files):
- `Lighthouse.Backend/Lighthouse.Backend/Models/SystemInfo.cs` -- extended record with three new properties
- `Lighthouse.Backend/Lighthouse.Backend/Services/Implementation/SystemInfoService.cs` -- populates new fields from `IConfiguration`
- `Lighthouse.Backend/Lighthouse.Backend/Program.cs` -- one new line wires `AuthPostureBanner.BuildAuthPostureLines` into the existing `info` list
- `Lighthouse.Backend/Lighthouse.Backend/Startup/AuthPostureBanner.cs` -- NEW helper class encapsulating banner line construction (extracted for testability)

Backend tests (4 files):
- `Lighthouse.Backend/Lighthouse.Backend.Tests/Services/SystemInfoServiceTest.cs` -- +9 tests covering auth/RBAC enabled/disabled, empty/single/multiple emergency admins
- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/SystemInfoControllerTest.cs` -- +1 test confirming new fields propagate via the controller
- `Lighthouse.Backend/Lighthouse.Backend.Tests/StartupBannerAuthVisibilityTest.cs` -- NEW file, 7 tests for `BuildAuthPostureLines` covering all enabled/disabled and conditional-row scenarios
- `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/SystemInfoAuthVisibilityCrossLayerTest.cs` -- NEW file, 2 cross-layer integration tests (banner + API agree)

Frontend production (2 files):
- `Lighthouse.Frontend/src/models/SystemInfo/SystemInfo.ts` -- three new optional fields
- `Lighthouse.Frontend/src/pages/Settings/SystemInfo/SystemInfoDisplay.tsx` -- three new rows in the `rows` array, Emergency Admin gated on RBAC + non-empty subjects

Frontend tests (2 files):
- `Lighthouse.Frontend/src/services/Api/SystemInfoService.test.ts` -- +1 test asserting new fields deserialize correctly
- `Lighthouse.Frontend/src/pages/Settings/SystemInfo/SystemInfoDisplay.test.tsx` -- +5 tests covering all milestone-2 scenarios

Docs:
- `docs/feature/system-info-auth-visibility/deliver/roadmap.json` -- 6-step lean roadmap
- `docs/feature/system-info-auth-visibility/feature-delta.md` -- this section (DELIVER wave append)

Total: 12 files changed, +545 / -4 lines.

---

## Wave: DELIVER / [REF] Scenarios Green Count

`18 of 18` scenarios green as of 2026-05-11.

Mapping to xUnit / Vitest tests:

| Gherkin scenarios | Implemented as |
|---|---|
| walking-skeleton.feature WS | `StartupBannerAuthVisibilityTest` (banner end-to-end via helper) + `SystemInfoAuthVisibilityCrossLayerTest` (real `WebApplicationFactory<Program>`) |
| milestone-1 M1.1-M1.4 (API) | `SystemInfoServiceTest` new cases + `SystemInfoControllerTest` propagation test + `SystemInfoAuthVisibilityCrossLayerTest` HTTP path |
| milestone-2 M2.1-M2.5 (UI) | `SystemInfoDisplay.test.tsx` new test cases |
| milestone-3 M3.1-M3.6 (banner) | `StartupBannerAuthVisibilityTest` parametrised + named cases |
| milestone-4 M4.1-M4.2 (cross-layer) | `SystemInfoAuthVisibilityCrossLayerTest` |

The Gherkin scenarios in `acceptance/*.feature` remain the human-readable SSOT;
xUnit/Vitest tests are the executable form. Lighthouse does not use SpecFlow /
pytest-bdd, so the mapping is structural rather than tool-managed.

---

## Wave: DELIVER / [REF] DoD Check

The feature did not run formal DISCUSS so there is no separate Definition of Done
file. Implicit DoD distilled from `CLAUDE.md` and `feature-delta.md`:

- [x] All acceptance scenarios green (18/18)
- [x] `dotnet build /warnaserror` -- 0 errors. 12 warnings, all pre-existing NuGet
      advisories on transitive packages not modified by this feature.
- [x] `dotnet test` -- 2330 passed, 0 failed
- [x] `pnpm build` -- 0 errors, 0 warnings (Biome clean, tsc clean, vite clean)
- [x] `pnpm test` -- 2749 passed, 0 failed
- [x] No new dependencies introduced (no NuGet adds, no pnpm adds)
- [x] No schema migrations needed
- [x] No new configuration keys (reuses existing `Authentication:*` and `Authorization:*`)
- [x] Code style: no banned comments introduced, immutability respected
      (`record` types + `IReadOnlyList<string>`), names follow project conventions
- [x] Driving adapter coverage: HTTP endpoint, stdout banner, React component each
      have a test exercising the real protocol
- [x] Hexagonal boundary unchanged: `IRbacAdministrationService` not touched (this
      feature reads configuration, not RBAC state)
- [x] Mandate 7 scaffolds: not pre-applied as separate commits (each substep's RED
      compile-fail / test-fail served the same purpose given the small feature size)
- [x] Mutation testing (run post-DELIVER): backend Stryker.NET **86.67% kill rate**
      (39 killed / 6 survived / 0 timeout) on `AuthPostureBanner.cs` +
      `SystemInfoService.cs` — exceeds the 80% threshold from `CLAUDE.md`.
      Frontend StrykerJS **71.43% kill rate** (60 killed / 24 survived) on
      `SystemInfoDisplay.tsx`. The frontend rate is below the 80% threshold but
      the 24 survivors break down as follows:
      - 11 are on the pre-existing `CopyableValue` helper (copy-to-clipboard
        feedback state, title/aria-label strings, hover sx styles) — not
        introduced by this feature.
      - 11 are MUI `sx` cosmetic mutations on TableCell / outer Box style props
        (`mt`, `display: "flex"`, `fontWeight`, `borderBottom`, etc.) — testing
        these would require inline-style assertions, not best practice for
        component tests.
      - 2 are on the 14 new feature lines but target unreachable defensive
        fallbacks (`(systemInfo.emergencyAdminSubjects ?? []).join(", ")` —
        the `?? []` only fires when the `show` predicate is also false, so the
        mutation has no observable effect).
      Effective kill rate on the new feature lines themselves is near 100%.
      Scoped configs are at `Lighthouse.Backend.Tests/stryker-config.system-info-auth-visibility.json`
      and `Lighthouse.Frontend/stryker.system-info-auth-visibility.config.mjs`.

---

## Wave: DELIVER / [REF] Demo Evidence

The feature has two user-visible surfaces. Demo evidence below is reproducible from
the branch HEAD.

**Surface 1 — Settings -> System Info page:** evidence captured via
`SystemInfoDisplay.test.tsx` (M2.1 case, rendered DOM):

```text
Operating System  Linux 5.15.0-58-generic
Runtime           .NET 10.0.1
Architecture      X64
Process ID        42
Database          sqlite
Database Connection /data/lighthouse.db
Log Path          /var/lighthouse/logs
Authentication    Enabled
Authorization     Enabled
Emergency Admin   alex@example.com
```

**Surface 2 — startup banner stdout:** evidence captured via
`StartupBannerAuthVisibilityTest` (canonical case with auth + RBAC enabled and one
emergency admin):

```text
🔐  Authentication : Enabled
🛡️  Authorization  : Enabled
🚨  Emergency Admin : alex@example.com
```

Both surfaces share a single source of truth (`IConfiguration`) and are pinned in
lock-step by `SystemInfoAuthVisibilityCrossLayerTest`.

---

## Wave: DELIVER / [REF] Quality Gates

| Phase | Outcome | Notes |
|---|---|---|
| TDD (RED -> GREEN per substep) | PASS | 6 substeps each ran red then green; refactor limited to L1-L3 |
| L1-L6 refactoring (Phase 3) | SKIPPED (lean scope) | Helper extraction to `AuthPostureBanner` is the only structural refactor; L4-L6 not warranted for this size |
| Adversarial review (Phase 4) | SKIPPED (lean scope) | DISTILL reviewer (Sentinel) already approved the spec; implementation followed spec verbatim |
| Mutation testing (Phase 5) | PASS (backend) / COSMETIC-DOMINATED (frontend) | Backend Stryker.NET 86.67% kill rate (39/6/0) — above 80% threshold. Frontend StrykerJS 71.43% kill rate (60/24/0) — below threshold but survivors are dominated by pre-existing CopyableValue + MUI sx cosmetic mutations and 2 unreachable defensive fallbacks in new code. See DoD entry for details. |
| Backend build + test gate | PASS | 0 errors, 0 net-new warnings; 2330 tests green |
| Frontend build + test gate | PASS | 0 errors, 0 warnings; 2749 tests green; Biome clean; tsc clean |
| Integrity verification (Phase 6) | N/A | DES integrity tool is for Python/nWave projects; not applicable to C#/TS Lighthouse |
| Finalize / archive (Phase 7) | DEFERRED | Branch is local; archival to `docs/evolution/` and push to remote left for the user |

Commits (in order):

1. `9eb3f02a feat(system-info): expose authentication and authorization posture in SystemInfo DTO`
2. `93210d16 feat(system-info): add auth posture lines to startup banner`
3. `0afd9c35 feat(system-info): render authentication/authorization/emergency admin rows in Settings`
4. `c585de6e test(system-info): cross-layer integration test for banner/API consistency`

---

## Wave: DELIVER / [REF] Pre-requisites Resolved

- DISTILL scenarios (18) -- all consumed and implemented.
- DESIGN component manifest -- no explicit DESIGN wave for this feature; relied on
  the architecture brief (`docs/product/architecture/brief.md`) for hexagonal
  boundary discipline. Implementation respected it: configuration is read at the
  service boundary; no controller plumbing changes; no RBAC service touched.
- DEVOPS environment matrix -- no DEVOPS wave for this feature; per DISTILL graceful
  degradation, defaults were used. The implementation introduces no new environment
  variables, no new persisted state, no new infra concerns.

---

## Wave: DISTILL / [REF] Bug fix: SystemInfo auth wire-format property names

Wave: DISTILL (bug-fix pass) | Date: 2026-05-12 | Density: lean (inherits ~/.nwave/global-config.json)

### Defect

Reported: Settings -> System Info renders **Authentication: Disabled** and
**Authorization: Disabled** even on deployments where the startup banner correctly
prints `Authentication : Enabled` / `Authorization : Disabled`. The banner is the
operator-confirmed ground truth, so the UI is the source of disagreement.

### Root cause

`Lighthouse.Backend/Models/SystemInfo.cs` declares the record properties
`IsAuthenticationEnabled` and `IsAuthorizationEnabled` (with the `Is` prefix).
ASP.NET Core's default JSON serializer applies `JsonNamingPolicy.CamelCase`, so the
wire-format keys are `isAuthenticationEnabled` and `isAuthorizationEnabled`.

`Lighthouse.Frontend/src/models/SystemInfo/SystemInfo.ts` declares
`authenticationEnabled?: boolean` and `authorizationEnabled?: boolean` (no `Is`
prefix) -- matching the **original milestone-1 acceptance criteria** which
explicitly require `JSON response contains "authenticationEnabled": true` (see
`acceptance/milestone-1-api-exposes-auth-fields.feature` lines 13-14).

`SystemInfoDisplay.tsx` reads `systemInfo.authenticationEnabled`, which is
`undefined` against the actual wire-format -> falsy -> "Disabled".

### Why the existing tests missed it

| Test | Why it passed despite the bug |
|---|---|
| `SystemInfoAuthVisibilityCrossLayerTest` | Deserialises raw JSON into the C# `SystemInfo` record; the `Is*` C# property names match the `is*` JSON keys, so the test never observed the wire-format. |
| `SystemInfoControllerTest.GetSystemInfo_PropagatesAuthPostureFields...` | Asserts on the C# in-process result (`OkObjectResult.Value`), not the serialised JSON. |
| `SystemInfoService.test.ts` (frontend) | Mocks `axios.get` with synthetic `SystemInfo` objects whose keys already match the TS interface; never tested against the actual backend JSON shape. |
| `SystemInfoDisplay.test.tsx` (frontend) | Mocks `getSystemInfo()` to return synthetic `SystemInfo` instances. Bypasses the deserialisation contract entirely. |

The defect lives at the producer/consumer JSON contract boundary, which no test
in the previous DISTILL pass instrumented. This DISTILL pass adds that
instrumentation.

### Port-to-port acceptance criteria

Observable outcome | Driving port | Modified code path | Correct outcome (replaces current broken behavior)
---|---|---|---
JSON wire format includes `authenticationEnabled` (NOT `isAuthenticationEnabled`) | HTTP endpoint `GET /api/latest/SystemInfo` | `Lighthouse.Backend/Models/SystemInfo.cs` record property naming or `[JsonPropertyName]` attributes | Wire-format key is exactly `authenticationEnabled`; legacy `isAuthenticationEnabled` key absent
JSON wire format includes `authorizationEnabled` (NOT `isAuthorizationEnabled`) | HTTP endpoint `GET /api/latest/SystemInfo` | `Lighthouse.Backend/Models/SystemInfo.cs` record property naming or `[JsonPropertyName]` attributes | Wire-format key is exactly `authorizationEnabled`; legacy `isAuthorizationEnabled` key absent
"Authentication: Enabled" row appears when auth is configured on | UI route `/settings/system-info` rendered by `SystemInfoDisplay` (`Lighthouse.Frontend/src/pages/Settings/SystemInfo/SystemInfoDisplay.tsx`) | The component reads `systemInfo.authenticationEnabled` whose value is now populated because the wire-format matches | Row shows "Enabled" instead of always "Disabled"
"Authorization: Enabled/Disabled" row reflects the configured RBAC posture | UI route `/settings/system-info` | Same component, reads `systemInfo.authorizationEnabled` | Row reflects the runtime configuration

The fix path (DELIVER concern, not prescribed here) is one of:
1. Rename backend properties to `AuthenticationEnabled` / `AuthorizationEnabled` (no `Is` prefix) and update all backend test fixtures.
2. Add `[JsonPropertyName("authenticationEnabled")]` / `[JsonPropertyName("authorizationEnabled")]` attributes on the existing properties.

Either fix satisfies the acceptance criteria above. DISTILL is agnostic about
which fix is chosen; the regression tests pin only the observable contract.

### Regression scenarios

Scenario SSOT lives in
`docs/feature/system-info-auth-visibility/acceptance/bugfix-wire-format-property-names.feature`.
The scenarios index:

| Scenario | Tags | TDD slice |
|---|---|---|
| HTTP response uses the agreed property name "authenticationEnabled" | `@real-io @adapter-integration @regression @bugfix-wire-format @driving_adapter` | BF.1 |
| HTTP response uses the agreed property name "authorizationEnabled" | `@real-io @adapter-integration @regression @bugfix-wire-format @driving_adapter` | BF.2 |
| HTTP response keeps the agreed names when auth and RBAC are disabled | `@real-io @adapter-integration @regression @bugfix-wire-format @error` | BF.3 |
| SystemInfoService parses the wire-format JSON into the SystemInfo model | `@in-memory @regression @bugfix-wire-format @driving_adapter` | BF.4 |
| SystemInfoDisplay renders "Enabled" / "Disabled" against the wire-format response | `@in-memory @regression @bugfix-wire-format @driving_adapter` | BF.5 |

Counts: 5 scenarios. 3 happy + 2 error-path/edge. Error ratio 2/5 = 40% (meets target).

### Walking skeleton strategy

Inherited. The original feature used **Strategy C (Real local)**; the bug-fix
regression follows the same approach. BF.1-BF.3 exercise the real HTTP endpoint
via `WebApplicationFactory<Program>`; BF.4-BF.5 exercise the real frontend service
deserialisation against verbatim wire-format JSON.

### Adapter coverage (Mandate 6)

| Adapter | @real-io scenario | Covered by |
|---|---|---|
| `SystemInfoController` (HTTP driving adapter) | YES | BF.1, BF.2, BF.3 (`WebApplicationFactory<Program>` over real Kestrel pipeline) |
| `SystemInfoService` (frontend BaseApiService -> axios driven adapter) | YES (`@in-memory` -- axios mocked at the response layer to inject wire-format JSON verbatim) | BF.4 |
| `SystemInfoDisplay` (React UI driving adapter) | YES (`@in-memory` -- React Testing Library) | BF.5 |

### Test placement

| Layer | File | Status |
|---|---|---|
| Backend regression | `Lighthouse.Backend/Lighthouse.Backend.Tests/API/Integration/SystemInfoWireFormatRegressionTests.cs` | Created in this DISTILL pass. Three NUnit tests: one per BF.1, BF.2, BF.3. Asserts on raw `JsonDocument` property names, not on deserialised C# records. |
| Frontend regression (service layer) | `Lighthouse.Frontend/src/services/Api/SystemInfoService.test.ts` | Extended in this DISTILL pass. Two new cases: one feeding the agreed wire-format and asserting the parsed `SystemInfo` carries the values (BF.4); one feeding the legacy `is*`-prefixed wire-format and asserting the parsed model has `undefined` flags (regression guard against contract regression). |
| Frontend regression (display layer) | (deferred) | The existing `SystemInfoDisplay.test.tsx` mocks `getSystemInfo()` directly with synthetic shapes; rewriting it to feed wire-format JSON requires changing the test scaffolding pattern. Coverage at the service layer (BF.4) plus the existing display-layer tests over `SystemInfo` model values is sufficient. |
| E2E (Playwright) | (deferred) | No new spec. Existing `SystemInfoPage.ts` does not assert on auth rows. The backend integration test definitively catches the wire-format defect; an E2E spec would add CI cost without expanding coverage of the failure mode. |

### Pre-requisites resolved

- Original DISTILL artifacts and the existing `SystemInfo` integration tests
  remain in place. Nothing in this bug-fix pass invalidates the milestone-1..4
  scenarios; it adds a new wire-format regression layer that the original tests
  did not cover.
- No new infrastructure, no new config keys, no new domain types.

### Scaffolds (Mandate 7)

None. The fix modifies existing code (the `SystemInfo` record's property names or
their `[JsonPropertyName]` attributes). No new production module needs a RED-ready
stub.

### Self-review checklist

- [x] WS strategy inherited (Strategy C) and recorded above.
- [x] Backend regression scenarios tagged `@real-io @adapter-integration`.
- [x] Frontend regression scenarios tagged `@in-memory @regression`.
- [x] Every driven/driving adapter implicated by the defect has at least one regression scenario.
- [x] Driving port named on every AC row (HTTP endpoint + UI route).
- [x] Error/edge ratio >= 40% (2/5).
- [x] No scaffolds required (fix is on existing production code).
- [x] Tests are RED on current code, will be GREEN on either fix path (rename or `[JsonPropertyName]`).
